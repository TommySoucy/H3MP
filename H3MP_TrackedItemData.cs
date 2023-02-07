using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP
{
    public class H3MP_TrackedItemData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this item's data packet used to ensure we process this data in the correct order

        public int trackedID = -1; // This item's unique ID to identify it across systems (index in global items arrays)
        public int localTrackedID = -1; // This item's index in local items list
        public int controller = 0; // Client controlling this item, 0 for host
        public bool active;
        private bool previousActive;
        public bool underActiveControl;
        private bool previousActiveControl;
        public string scene;
        public int instance;

        // Data
        public string itemID; // The ID of this item so it can be spawned by clients and host
        public byte[] identifyingData;
        public byte[] previousData;
        public byte[] data;
        public byte[] additionalData;

        // State
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 velocity = Vector3.zero;
        public H3MP_TrackedItem physicalItem;

        public int parent = -1; // The tracked ID of item this item is attached to
        public List<H3MP_TrackedItemData> children; // The items attached to this item
        public int childIndex = -1; // The index of this item in its parent's children list
        public int ignoreParentChanged;
        public bool removeFromListOnDestroy = true;

        public IEnumerator Instantiate()
        {
            GameObject itemPrefab = GetItemPrefab();
            if (itemPrefab == null)
            {
                yield return IM.OD[itemID].GetGameObjectAsync();
                itemPrefab = IM.OD[itemID].GetGameObject();
            }
            if (itemPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate {itemID} sent from {controller} but failed to get item prefab.");
                yield break;
            }

            // Here, we can't simply skip the next instantiate
            // Since Awake() will be called on the object upon instantiation, if the object instantiates something in Awake(), like firearm,
            // it will consume the skipNextInstantiate, so we instead skipAllInstantiates during the current instantiation
            //++Mod.skipNextInstantiates;

            try
            {
                ++Mod.skipAllInstantiates;
                GameObject itemObject = GameObject.Instantiate(itemPrefab, position, rotation);
                --Mod.skipAllInstantiates;
                physicalItem = itemObject.AddComponent<H3MP_TrackedItem>();
                physicalItem.data = this;
                physicalItem.physicalObject = itemObject.GetComponent<FVRPhysicalObject>();

                if (H3MP_GameManager.trackedItemByItem.TryGetValue(physicalItem.physicalObject, out H3MP_TrackedItem t))
                {
                    Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical object already exists in trackedItemByItem\n\tTrackedID: "+ t.data.trackedID);
                }
                else
                {
                    H3MP_GameManager.trackedItemByItem.Add(physicalItem.physicalObject, physicalItem);
                }
                if (physicalItem.physicalObject is SosigWeaponPlayerInterface)
                {
                    H3MP_GameManager.trackedItemBySosigWeapon.Add((physicalItem.physicalObject as SosigWeaponPlayerInterface).W, physicalItem);
                }

                // See Note in H3MP_GameManager.SyncTrackedItems
                if (parent != -1)
                {
                    // Add ourselves to the prent's children
                    H3MP_TrackedItemData parentItem = (H3MP_ThreadManager.host ? H3MP_Server.items : H3MP_Client.items)[parent];
                    if (parentItem.children == null)
                    {
                        parentItem.children = new List<H3MP_TrackedItemData>();
                    }
                    childIndex = parentItem.children.Count;
                    parentItem.children.Add(this);

                    // Physically parent
                    ++ignoreParentChanged;
                    itemObject.transform.parent = parentItem.physicalItem.transform;
                    --ignoreParentChanged;
                }

                // Set as kinematic if not in control
                if (controller != H3MP_GameManager.ID)
                {
                    Mod.SetKinematicRecursive(physicalItem.transform, true);
                }

                ProcessAdditionalData();

                // Initially set itself
                Update(this, true);
            }
            catch(Exception e)
            {
                Mod.LogError("Error while trying to instantiate item: " + itemID+":\n"+e.Message+"\n"+e.StackTrace);
            }
        }

        // MOD: This will be called at the end of instantiation so mods can use it to process the additionalData array
        private void ProcessAdditionalData()
        {
        }

        // MOD: If a mod keeps its item prefabs in a different location than IM.OD, this is what should be patched to find it
        //      If this returns null, it will try to find the item in IM.OD
        private GameObject GetItemPrefab()
        {
            if (itemID.Equals("TNH_ShatterableCrate") && GM.TNH_Manager != null)
            {
                return GM.TNH_Manager.Prefabs_ShatterableCrates[identifyingData[0]];
            }
            return null;
        }

        public void Update(H3MP_TrackedItemData updatedItem, bool initial = false)
        {
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedItem.rotation;
            if (physicalItem != null)
            {
                if (!H3MP_TrackedItem.interpolated)
                {
                    if (parent == -1)
                    {
                        physicalItem.transform.position = updatedItem.position;
                        physicalItem.transform.rotation = updatedItem.rotation;
                    }
                    else
                    {
                        // If parented, the position and rotation are relative, so set it now after parenting
                        physicalItem.transform.localPosition = updatedItem.position;
                        physicalItem.transform.localRotation = updatedItem.rotation;
                    }
                }

                previousActive = active;
                active = updatedItem.active;
                if (active)
                {
                    if (!physicalItem.gameObject.activeSelf)
                    {
                        physicalItem.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physicalItem.gameObject.activeSelf)
                    {
                        physicalItem.gameObject.SetActive(false);
                    }
                }

                previousActiveControl = underActiveControl;
                underActiveControl = updatedItem.underActiveControl;

                if (initial)
                {
                    SetInitialData();
                }
            }

            UpdateData(updatedItem.data);
        }

        // MOD: This will be called in the initial Update of the item, at the end of its instantiation
        //      This method is meant to be used to intialize the item's data based on additional identifying info
        //      As we do for TNHShatterableCrate here to init. its contents
        private void SetInitialData()
        {
            if (physicalItem != null && GM.TNH_Manager != null && itemID.Equals("TNH_ShatterableCrate"))
            {
                TNH_ShatterableCrate crate = physicalItem.gameObject.GetComponent<TNH_ShatterableCrate>();
                if(crate != null)
                {
                    if (identifyingData[1] == 1)
                    {
                        crate.SetHoldingHealth(GM.TNH_Manager);
                    }
                    if (identifyingData[2] == 1)
                    {
                        crate.SetHoldingToken(GM.TNH_Manager);
                    }
                }
            }
        }

        public bool Update(bool full = false)
        {
            previousPos = position;
            previousRot = rotation;
            if (parent == -1)
            {
                position = physicalItem.transform.position;
                rotation = physicalItem.transform.rotation;
            }
            else
            {
                position = physicalItem.transform.localPosition;
                rotation = physicalItem.transform.localRotation;
            }

            previousActive = active;
            active = physicalItem.gameObject.activeInHierarchy;
            previousActiveControl = underActiveControl;
            underActiveControl = H3MP_GameManager.IsControlled(physicalItem.physicalObject);

            // Note: UpdateData() must be done first in this expression, otherwise, if active/position/rotation is different,
            // it will return true before making the call
            return UpdateData() || previousActive != active || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public bool NeedsUpdate()
        {
            return previousActive != active || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !DataEqual();
        }

        public void SetParent(H3MP_TrackedItemData newParent, bool physicallyParent)
        {
            if (newParent == null)
            {
                if (parent != -1) // We had parent before, need to unparent
                {
                    H3MP_TrackedItemData previousParent = null;
                    int clientID = -1;
                    if (H3MP_ThreadManager.host)
                    {
                        previousParent = H3MP_Server.items[parent];
                        clientID = 0;
                    }
                    else
                    {
                        previousParent = H3MP_Client.items[parent];
                        clientID = H3MP_Client.singleton.ID;
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                    parent = -1;
                    childIndex = -1;

                    // Physically unparent if necessary
                    if (physicallyParent && physicalItem != null)
                    {
                        ++ignoreParentChanged;
                        physicalItem.transform.parent = GetGeneralParent();
                        --ignoreParentChanged;

                        // If in control, we want to enable rigidbody
                        if (controller == clientID)
                        {
                            Mod.SetKinematicRecursive(physicalItem.transform, false);
                        }

                        // Call updateParent delegate on item if it has one
                        if(physicalItem.updateParentFunc != null)
                        {
                            physicalItem.updateParentFunc();
                        }
                    }
                }
                // Already unparented, nothing changes
            }
            else // We have new parent
            {
                if (parent != -1) // We had parent before, need to unparent first
                {
                    H3MP_TrackedItemData previousParent = null;
                    if (H3MP_ThreadManager.host)
                    {
                        previousParent = H3MP_Server.items[parent];
                    }
                    else
                    {
                        previousParent = H3MP_Client.items[parent];
                    }
                    previousParent.children[childIndex] = previousParent.children[previousParent.children.Count - 1];
                    previousParent.children[childIndex].childIndex = childIndex;
                    previousParent.children.RemoveAt(previousParent.children.Count - 1);
                    if (previousParent.children.Count == 0)
                    {
                        previousParent.children = null;
                    }
                }

                // Set new parent
                parent = newParent.trackedID;
                if (newParent.children == null)
                {
                    newParent.children = new List<H3MP_TrackedItemData>();
                }
                childIndex = newParent.children.Count;
                newParent.children.Add(this);

                // Physically parent
                if (physicallyParent && physicalItem != null)
                {
                    ++ignoreParentChanged;
                    physicalItem.transform.parent = newParent.physicalItem.transform;
                    --ignoreParentChanged;

                    // Set Controller to parent's
                    SetController(newParent.controller);

                    // If in control, we want to enable rigidbody
                    if (controller == H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(physicalItem.transform, false);
                    }

                    // Call updateParent delegate on item if it has one
                    if (physicalItem.updateParentFunc != null)
                    {
                        physicalItem.updateParentFunc();
                    }
                }
            }
        }

        public void SetParent(int trackedID)
        {
            if (trackedID == -1)
            {
                SetParent(null, true);
            }
            else
            {
                if (H3MP_ThreadManager.host)
                {
                    SetParent(H3MP_Server.items[trackedID], true);
                }
                else
                {
                    SetParent(H3MP_Client.items[trackedID], true);
                }
            }
        }

        // MOD: When unparented, an item will have its transform's parent set to null
        //      If you want it to be set to a specific transform, patch this to return the transform you want
        public Transform GetGeneralParent()
        {
            return null;
        }

        private bool DataEqual()
        {
            if(data == null && previousData == null)
            {
                return true;
            }
            if((data == null && previousData != null)||(data != null && previousData == null)||data.Length != previousData.Length)
            {
                return true;
            }
            for(int i=0; i < data.Length; ++i)
            {
                if (data[i] != previousData.Length)
                {
                    return true;
                }
            }
            return false;
        }

        private bool UpdateData(byte[] newData = null)
        {
            previousData = data;

            return physicalItem == null ? false : physicalItem.UpdateItemData(newData);
        }

        public void OnTrackedIDReceived()
        {
            if (H3MP_TrackedItem.unknownDestroyTrackedIDs.Contains(localTrackedID))
            {
                H3MP_ClientSend.DestroyItem(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                H3MP_Client.items[trackedID] = null;

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownControlTrackedIDs.ContainsKey(localTrackedID))
            {
                int newController = H3MP_TrackedItem.unknownControlTrackedIDs[localTrackedID];

                H3MP_ClientSend.GiveControl(trackedID, newController);

                // Also change controller locally
                SetController(newController);

                H3MP_TrackedItem.unknownControlTrackedIDs.Remove(localTrackedID);

                // Remove from local
                if (H3MP_GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownTrackedIDs.ContainsKey(localTrackedID))
            {
                KeyValuePair<int, bool> parentPair = H3MP_TrackedItem.unknownTrackedIDs[localTrackedID];
                if (parentPair.Value)
                {
                    H3MP_TrackedItemData parentItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentItemData = H3MP_Server.items[parentPair.Key];
                    }
                    else
                    {
                        parentItemData = H3MP_Client.items[parentPair.Key];
                    }
                    if (parentItemData != null)
                    {
                        if (parentItemData.trackedID != parent)
                        {
                            // We have a parent trackedItem and it is new
                            // Update other clients
                            if (H3MP_ThreadManager.host)
                            {
                                H3MP_ServerSend.ItemParent(trackedID, parentItemData.trackedID);
                            }
                            else
                            {
                                H3MP_ClientSend.ItemParent(trackedID, parentItemData.trackedID);
                            }

                            // Update local
                            SetParent(parentItemData, false);
                        }
                    }
                }
                else
                {
                    if(parentPair.Key == -1)
                    {
                        // We were detached from current parent
                        // Update other clients
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.ItemParent(trackedID, -1);
                        }
                        else
                        {
                            H3MP_ClientSend.ItemParent(trackedID, -1);
                        }

                        // Update locally
                        SetParent(null, false);
                    }
                    else // We received our tracked ID but not our parent's
                    {
                        if (H3MP_TrackedItem.unknownParentTrackedIDs.ContainsKey(parentPair.Key))
                        {
                            H3MP_TrackedItem.unknownParentTrackedIDs[parentPair.Key].Add(trackedID);
                        }
                        else
                        {
                            H3MP_TrackedItem.unknownParentTrackedIDs.Add(parentPair.Key, new List<int>() { trackedID });
                        }
                    }
                }

                H3MP_TrackedItem.unknownTrackedIDs.Remove(localTrackedID);
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownParentTrackedIDs.ContainsKey(localTrackedID))
            {
                List<int> childrenList = H3MP_TrackedItem.unknownParentTrackedIDs[localTrackedID];
                H3MP_TrackedItemData[] arrToUse = null;
                if (H3MP_ThreadManager.host)
                {
                    arrToUse = H3MP_Server.items;
                }
                else
                {
                    arrToUse = H3MP_Client.items;
                }
                foreach(int childID in childrenList)
                {
                    if (arrToUse[childID] != null)
                    {
                        // Update other clients
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.ItemParent(arrToUse[childID].trackedID, trackedID);
                        }
                        else
                        {
                            H3MP_ClientSend.ItemParent(arrToUse[childID].trackedID, trackedID);
                        }

                        // Update local
                        arrToUse[childID].SetParent(this, false);
                    }
                }
                H3MP_TrackedItem.unknownParentTrackedIDs.Remove(localTrackedID);
            }

            if(localTrackedID != -1)
            {
                // Add to item tracking list
                if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(instance, out List<int> itemList))
                    {
                        itemList.Add(trackedID);
                    }
                    else
                    {
                        relevantInstances.Add(instance, new List<int>() { trackedID });
                    }
                }
                else
                {
                    Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                    newInstances.Add(instance, new List<int>() { trackedID });
                    H3MP_GameManager.itemsByInstanceByScene.Add(scene, newInstances);
                }
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            H3MP_TrackedItem.unknownTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownParentTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownControlTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownDestroyTrackedIDs.Remove(localTrackedID);

            // Remove from temp lists if in there
            if (!H3MP_ThreadManager.host && H3MP_Client.tempLocalItemOriginalIDs.ContainsKey(localTrackedID))
            {
                H3MP_Client.tempLocalItems.Remove(H3MP_Client.tempLocalItemOriginalIDs[localTrackedID]);
                H3MP_Client.tempLocalItemOriginalIDs.Remove(localTrackedID);
            }

            // Remove from actual local items list and update the localTrackedID of the item we are moving
            H3MP_GameManager.items[localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
            int oldLocalTrackedID = H3MP_GameManager.items[localTrackedID].localTrackedID;
            H3MP_GameManager.items[localTrackedID].localTrackedID = localTrackedID;
            H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
            if (H3MP_GameManager.items.Count > 1 && H3MP_GameManager.items[localTrackedID].trackedID == -1)
            {
                int originalLocalTrackedID = -1;
                if (H3MP_Client.tempLocalItemOriginalIDs.ContainsKey(oldLocalTrackedID))
                {
                    originalLocalTrackedID = H3MP_Client.tempLocalItemOriginalIDs[oldLocalTrackedID];
                    H3MP_Client.tempLocalItemOriginalIDs.Remove(oldLocalTrackedID);
                    H3MP_Client.tempLocalItems.Remove(oldLocalTrackedID);
                }
                else
                {
                    originalLocalTrackedID = oldLocalTrackedID;
                }
                H3MP_Client.tempLocalItemOriginalIDs.Add(localTrackedID, originalLocalTrackedID);
                H3MP_Client.tempLocalItems.Add(originalLocalTrackedID, H3MP_GameManager.items[localTrackedID]);
            }
            localTrackedID = -1;
        }

        public void SetController(int newController)
        {
            SetControllerRecursive(this, newController);
        }

        private void SetControllerRecursive(H3MP_TrackedItemData otherTrackedItem, int newController)
        {
            otherTrackedItem.controller = newController;

            if(otherTrackedItem.children != null)
            {
                foreach(H3MP_TrackedItemData child in otherTrackedItem.children)
                {
                    SetControllerRecursive(child, newController);
                }
            }
        }
    }
}
