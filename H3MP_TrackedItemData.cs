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

        // Data
        public string itemID; // The ID of this item so it can be spawned by clients and host
        public byte[] identifyingData;
        public byte[] previousData;
        public byte[] data;

        // Item type specific data structure:
        /**
         * FVRFireArmMagazine
         *  0: 
         *  1: Loaded round count (n)
         *      0:
         *      1: Round class
         *      ...
         *      n * 2 - 2:
         *      n * 2 - 1: Round class
         *  2: Loaded in parent
         */
        /**
         * FVRFireArmClip
         *  0: 
         *  1: Loaded round count (n)
         *      0:
         *      1: Round class
         *      ...
         *      n * 2 - 2:
         *      n * 2 - 1: Round class
         *  2: Loaded in parent
         */
        /**
         * Speedloader
         *  0:
         *  1: Chamber Round class (-1 for null)
         *  ...
         *  Chambers.Count * 2 - 2:
         *  Chambers.Count * 2 - 1: Round class
         */
        /**
         * ClosedBoltWeapon
         *  0: m_fireSelectorMode index
         *  1: m_CamBurst
         *  2: m_isHammerCocked
         *  3:
         *  4: Chamber Round class (-1 for null)
         */
        /**
         * BoltActionRifle
         *  0: m_fireSelectorMode index
         *  1: m_isHammerCocked
         *  2:
         *  3: Chamber Round class (-1 for null)
         *  4: CurBoltHandleState
         *  5: BoltHandle.HandleRot
         */
        /**
         * Handgun
         *  0: m_fireSelectorMode index
         *  1: m_CamBurst
         *  2: m_isHammerCocked
         *  3:
         *  4: Chamber Round class (-1 for null)
         */
        /**
         * TubeFedShotgun
         *  0: m_fireSelectorMode index
         *  1: m_isHammerCocked
         *  2:
         *  3: Chamber Round class (-1 for null)
         *  4: Bolt.CurPos
         *  5: Handle.CurPos
         */
        /**
         * Speedloader
         *  0:
         *  1: Round class
         *  ...
         *  Chambers.Count * 2 - 2:
         *  Chambers.Count * 2 - 1: Round class
         */

        // State
        public Vector3 position;
        public Quaternion rotation;
        private Vector3 previousPos;
        private Quaternion previousRot;
        public H3MP_TrackedItem physicalItem;

        public int parent = -1; // The tracked ID of item this item is attached to
        public List<H3MP_TrackedItemData> children; // The items attached to this item
        public int childIndex = -1; // The index of this item in its parent's children list
        public int ignoreParentChanged;

        public IEnumerator Instantiate()
        {
            Debug.Log("Instantiating item " + trackedID);
            GameObject itemPrefab = GetItemPrefab();
            if (itemPrefab == null)
            {
                yield return IM.OD[itemID].GetGameObjectAsync();
                itemPrefab = IM.OD[itemID].GetGameObject();
            }
            if (itemPrefab == null)
            {
                Debug.LogError($"Attempted to instantiate {itemID} sent from {controller} but failed to get item prefab.");
                yield break;
            }

            // Here, we can't simply skip the next instantiate
            // Since Awake() will be called on the object upon instantiation, if the object instantiates something in Awake(), like firearm,
            // it will consume the skipNextInstantiate, so we instead skipAllInstantiates during the current instantiation
            //++Mod.skipNextInstantiates;

            try
            {
                ++Mod.skipAllInstantiates;
                GameObject itemObject = GameObject.Instantiate(itemPrefab);
                --Mod.skipAllInstantiates;
                physicalItem = itemObject.AddComponent<H3MP_TrackedItem>();
                physicalItem.data = this;
                physicalItem.physicalObject = itemObject.GetComponent<FVRPhysicalObject>();

                H3MP_GameManager.trackedItemByItem.Add(physicalItem.physicalObject, physicalItem);

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

                // Store and destroy RB if not in control
                if (controller != H3MP_GameManager.ID)
                {
                    physicalItem.physicalObject.StoreAndDestroyRigidbody();
                }

                // Initially set itself
                Update(this);
            }
            catch(Exception e)
            {
                Debug.LogError("Error while trying to instantiate item: " + itemID+":\n"+e.Message+"\n"+e.StackTrace);
            }
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

        public void Update(H3MP_TrackedItemData updatedItem)
        {
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            if (physicalItem != null)
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
            }

            UpdateData(updatedItem.data);
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

            // Note: UpdateData() must be done first in this expression, otherwise, if active/position/rotation is different,
            // it will return true before making the call
            return UpdateData() || previousActive != active || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public bool NeedsUpdate()
        {
            return previousActive != active || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !DataEqual();
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
                            physicalItem.physicalObject.RecoverRigidbody();
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

                    // If in control, we want to enable rigidbody
                    if (controller == H3MP_GameManager.ID)
                    {
                        physicalItem.physicalObject.StoreAndDestroyRigidbody();
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
                return false;
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
                controller = newController;

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
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            H3MP_TrackedItem.unknownTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownParentTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownControlTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedItem.unknownDestroyTrackedIDs.Remove(localTrackedID);

            // Remove
            H3MP_GameManager.items[localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
            H3MP_GameManager.items[localTrackedID].localTrackedID = localTrackedID;
            H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
            localTrackedID = -1;
        }
    }
}
