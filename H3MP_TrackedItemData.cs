using ErosionBrushPlugin;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItemData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this item's data packet used to ensure we process this data in the correct order

        public int trackedID = -1; // This item's unique ID to identify it across systems (index in global items arrays)
        public int localTrackedID = -1; // This item's index in local items list
        public uint localWaitingIndex = uint.MaxValue; // The unique index this item had while waiting for its tracked ID
        public int initTracker; // The ID of the client who initially tracked this item
        private int _controller = 0; // Client controlling this item, 0 for host
        // TODO: Review: Perhaps do everything about control through this, like set kinematic and so on
        public int controller { get { return _controller; } set { if (_controller != value) { _controller = value; OnControl(); } else { _controller = value; } } }
        public bool active;
        private bool previousActive;
        public bool underActiveControl;
        private bool previousActiveControl;
        public string scene;
        public int instance;
        public bool sceneInit;
        public bool awaitingInstantiation;

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
        public List<int> childrenToParent = new List<int>(); // The items to attach to this item once we instantiate it
        public int childIndex = -1; // The index of this item in its parent's children list
        public int ignoreParentChanged;
        public bool removeFromListOnDestroy = true;
        public int[] toPutInSosigInventory;

        public IEnumerator Instantiate()
        {
            GameObject itemPrefab = GetItemPrefab();
            if (itemPrefab == null)
            {
                if (IM.OD.TryGetValue(itemID, out FVRObject obj))
                {
                    yield return obj.GetGameObjectAsync();
                    itemPrefab = obj.GetGameObject();
                }
            }
            if (itemPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate {itemID} sent from {controller} but failed to get item prefab.");
                awaitingInstantiation = false;
                yield break;
            }

            if (!awaitingInstantiation)
            {
                // Could have cancelled an item instantiation if received destruction order while we were waiting to get the prefab
                yield break;
            }

            // Here, we can't simply skip the next instantiate
            // Since Awake() will be called on the object upon instantiation, if the object instantiates something in Awake(), like firearm,
            // it will consume the skipNextInstantiate, so we instead skipAllInstantiates during the current instantiation
            //++Mod.skipNextInstantiates;

            try
            {
                ++Mod.skipAllInstantiates;
                if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at item instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
                GameObject itemObject = GameObject.Instantiate(itemPrefab, position, rotation);
                --Mod.skipAllInstantiates;
                physicalItem = itemObject.AddComponent<H3MP_TrackedItem>();
                awaitingInstantiation = false;
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
                // Unfortunately this doesn't necessarily help us in this case considering we need the parent to have been instantiated
                // by now, but since the instantiation is a coroutine, we are not guaranteed to have the parent's physObj yet
                if (parent != -1)
                {
                    // Add ourselves to the parent's children
                    H3MP_TrackedItemData parentItem = (H3MP_ThreadManager.host ? H3MP_Server.items : H3MP_Client.items)[parent];

                    if (parentItem.physicalItem == null)
                    {
                        parentItem.childrenToParent.Add(trackedID);
                    }
                    else
                    {
                        // Physically parent
                        ++ignoreParentChanged;
                        itemObject.transform.parent = parentItem.physicalItem.transform;
                        --ignoreParentChanged;
                    }
                }

                // Set as kinematic if not in control
                if (controller != H3MP_GameManager.ID)
                {
                    Mod.SetKinematicRecursive(physicalItem.transform, true);
                }

                ProcessAdditionalData();

                // Initially set itself
                Update(this, true);

                // Process childrenToParent
                for (int i = 0; i < childrenToParent.Count; ++i)
                {
                    H3MP_TrackedItemData childItem = (H3MP_ThreadManager.host ? H3MP_Server.items : H3MP_Client.items)[childrenToParent[i]];
                    if (childItem != null && childItem.parent == trackedID && childItem.physicalItem != null)
                    {
                        // Physically parent
                        ++childItem.ignoreParentChanged;
                        childItem.physicalItem.transform.parent = physicalItem.transform;
                        --childItem.ignoreParentChanged;

                        // Call update on child in case it needs to process its new parent somehow
                        // This is needed for attachments that did their latest update before we got their parent's phys
                        // Calling this update will let them mount themselves to their mount properly
                        childItem.Update(childItem);
                    }
                }
                childrenToParent.Clear();

                // Add to sosig inventory if necessary
                if(toPutInSosigInventory != null)
                {
                    H3MP_TrackedSosigData trackedSosig = H3MP_ThreadManager.host ? H3MP_Server.sosigs[toPutInSosigInventory[0]] : H3MP_Client.sosigs[toPutInSosigInventory[0]];
                    if(trackedSosig != null && trackedSosig.inventory[toPutInSosigInventory[1]] == trackedID)
                    {

                        ++SosigPickUpPatch.skip;
                        ++SosigPlaceObjectInPatch.skip;
                        if (toPutInSosigInventory[1] == 0)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalObject).W);
                        }
                        else if (toPutInSosigInventory[1] == 1)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalObject).W);
                        }
                        else
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[toPutInSosigInventory[1] - 2].PlaceObjectIn(((SosigWeaponPlayerInterface)physicalItem.physicalObject).W);
                        }
                        --SosigPickUpPatch.skip;
                        --SosigPlaceObjectInPatch.skip;
                    }
                }
            }
            catch(Exception e)
            {
                Mod.LogError("Error while trying to instantiate item: " + itemID+":\n"+e.Message+"\n"+e.StackTrace);
            }
        }

        // MOD: This will be called at the end of instantiation so mods can use it to process the additionalData array
        private void ProcessAdditionalData()
        {
            if (Mod.currentTNHInstance != null && Mod.currentlyPlayingTNH)
            {
                if (additionalData[0] == 1)
                {
                    (Mod.TNH_SupplyPoint_m_spawnBoxes.GetValue(Mod.currentTNHInstance.manager.SupplyPoints[BitConverter.ToInt16(additionalData, 1)]) as List<GameObject>).Add(physicalItem.gameObject);
                }
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
            // Phys could be null if we were given control of the item while we were loading and we haven't instantiated it on our side yet
            if(physicalItem == null)
            {
                return false;
            }

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
                Mod.LogInfo("Item " + itemID + " at " + trackedID + " got parented to "+newParent.itemID+" at "+newParent.trackedID);
                if (parent != -1) // We had parent before, need to unparent first
                {
                    Mod.LogInfo("\tAlready had parent");
                    if (newParent.trackedID == parent)
                    {
                        Mod.LogInfo("\t\tAlready same parent, returning");
                        // Already attached to correct parent
                        return;
                    }
                    Mod.LogInfo("\t\tDetaching");

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
                Mod.LogInfo("\t\tAttaching");

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
                    if (newParent.physicalItem == null)
                    {
                        newParent.childrenToParent.Add(trackedID);
                    }
                    else
                    {
                        ++ignoreParentChanged;
                        physicalItem.transform.parent = newParent.physicalItem.transform;
                        --ignoreParentChanged;

                        // Call updateParent delegate on item if it has one
                        if (physicalItem.updateParentFunc != null)
                        {
                            physicalItem.updateParentFunc();
                        }
                    }
                }

                int preController = controller;

                // Set Controller to parent's
                SetController(newParent.controller, true);

                // If newly in control, we want to enable rigidbody and add to list
                if (controller == H3MP_GameManager.ID)
                {
                    if (preController != controller)
                    {
                        Mod.SetKinematicRecursive(physicalItem.transform, false);

                        localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(this);
                    }
                }
                else
                {
                    Mod.SetKinematicRecursive(physicalItem.transform, true);
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
                return false;
            }
            for(int i=0; i < data.Length; ++i)
            {
                if (data[i] != previousData[i])
                {
                    return false;
                }
            }
            return true;
        }

        private bool UpdateData(byte[] newData = null)
        {
            previousData = data;

            if(physicalItem == null)
            {
                data = newData;
                return false;
            }
            else
            {
                return physicalItem.UpdateItemData(newData);
            }
        }

        public void OnTrackedIDReceived()
        {
            if (H3MP_TrackedItem.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                H3MP_ClientSend.DestroyItem(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                H3MP_Client.items[trackedID] = null;

                // Remove from itemsByInstanceByScene
                H3MP_GameManager.itemsByInstanceByScene[scene][instance].Remove(trackedID);

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = H3MP_TrackedItem.unknownControlTrackedIDs[localWaitingIndex];

                H3MP_ClientSend.GiveControl(trackedID, newController, null);

                // Also change controller locally
                SetController(newController, true);

                H3MP_TrackedItem.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (H3MP_GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownTrackedIDs.ContainsKey(localWaitingIndex))
            {
                KeyValuePair<uint, bool> parentPair = H3MP_TrackedItem.unknownTrackedIDs[localWaitingIndex];
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
                            Mod.LogInfo(itemID + " has new parent from unknown: " + parentItemData.itemID + ", sending");
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
                    if (parentPair.Key == uint.MaxValue)
                    {
                        Mod.LogInfo(itemID + " was unparented from unknown, sending to others");
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

                if (!parentPair.Value && H3MP_TrackedItem.unknownParentWaitList.TryGetValue(parentPair.Key, out List<uint> waitlist))
                {
                    waitlist.Remove(localWaitingIndex);
                }
                H3MP_TrackedItem.unknownTrackedIDs.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownParentWaitList.ContainsKey(localWaitingIndex))
            {
                List<uint> waitlist = H3MP_TrackedItem.unknownParentWaitList[localWaitingIndex];
                foreach(uint childID in waitlist)
                {
                    if (H3MP_TrackedItem.unknownTrackedIDs.TryGetValue(childID, out KeyValuePair<uint, bool> childEntry))
                    {
                        H3MP_TrackedItem.unknownTrackedIDs[childID] = new KeyValuePair<uint, bool>((uint)trackedID, true);
                    }
                }
                H3MP_TrackedItem.unknownParentWaitList.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownParentTrackedIDs.ContainsKey(localWaitingIndex))
            {
                List<int> childrenList = H3MP_TrackedItem.unknownParentTrackedIDs[localWaitingIndex];
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
                        Mod.LogInfo(arrToUse[childID].itemID + " has new parent from unknownParentTrackedIDs: " + itemID + ", sending");
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
                H3MP_TrackedItem.unknownParentTrackedIDs.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownCrateHolding.TryGetValue(localWaitingIndex, out byte option))
            {
                bool health = option == 0 || option == 2;
                bool token = option == 1 || option == 2;
                if (H3MP_ThreadManager.host)
                {
                    if (health)
                    {
                        H3MP_ServerSend.ShatterableCrateSetHoldingHealth(trackedID);
                    }
                    if (token)
                    {
                        H3MP_ServerSend.ShatterableCrateSetHoldingToken(trackedID);
                    }
                }
                else
                {
                    if (health)
                    {
                        H3MP_ClientSend.ShatterableCrateSetHoldingHealth(trackedID);
                    }
                    if (token)
                    {
                        H3MP_ClientSend.ShatterableCrateSetHoldingToken(trackedID);
                    }
                }

                H3MP_TrackedItem.unknownCrateHolding.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && H3MP_TrackedItem.unknownSosigInventoryItems.TryGetValue(localWaitingIndex, out KeyValuePair<H3MP_TrackedSosigData, int> entry))
            {
                if (entry.Key.physicalObject != null)
                {
                    if (entry.Key.trackedID != -1)
                    {
                        // Set the value in our data
                        entry.Key.inventory[entry.Value] = trackedID;

                        // Send to others
                        if (entry.Value == 0)
                        {
                            H3MP_ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, true);
                        }
                        else if (entry.Value == 1)
                        {
                            H3MP_ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, false);
                        }
                        else
                        {
                            H3MP_ClientSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                        }
                    }
                    // else, sosig does not yet have a tracked ID, this interaction will be sent upon it receiving its ID
                }
                // else, sosig has been destroyed

                H3MP_TrackedItem.unknownSosigInventoryItems.Remove(localWaitingIndex);
            }
        }

        public void OnItemTracked()
        {
            if (physicalItem.physicalObject is SosigWeaponPlayerInterface &&
                H3MP_TrackedItem.unknownSosigInventoryObjects.TryGetValue((physicalItem.physicalObject as SosigWeaponPlayerInterface).W, out KeyValuePair<H3MP_TrackedSosigData, int> entry))
            {
                if(entry.Key.physicalObject != null)
                {
                    if (trackedID == -1)
                    {
                        // Item tracked but don't have tracked ID yet, add to other unknown
                        H3MP_TrackedItem.unknownSosigInventoryItems.Add(localWaitingIndex, new KeyValuePair<H3MP_TrackedSosigData, int>(entry.Key, entry.Value));
                    }
                    else // The item has a tracked ID
                    {
                        if (entry.Key.trackedID != -1)
                        {
                            // Set the value in our data
                            entry.Key.inventory[entry.Value] = trackedID;

                            // Send to others
                            if (entry.Value == 0)
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.SosigPickUpItem(entry.Key.trackedID, trackedID, true);
                                }
                                else
                                {
                                    H3MP_ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, true);
                                }
                            }
                            else if (entry.Value == 1)
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.SosigPickUpItem(entry.Key.trackedID, trackedID, false);
                                }
                                else
                                {
                                    H3MP_ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, false);
                                }
                            }
                            else
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                                }
                                else
                                {
                                    H3MP_ClientSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                                }
                            }
                        }
                        // else, sosig does not yet have a tracked ID, this interaction will be sent upon it receiving its ID
                    }
                }
                // else, sosig has been destroyed

                H3MP_TrackedItem.unknownSosigInventoryObjects.Remove((physicalItem.physicalObject as SosigWeaponPlayerInterface).W);
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            if (trackedID == -1)
            {
                if (H3MP_TrackedItem.unknownTrackedIDs.TryGetValue(localWaitingIndex, out KeyValuePair<uint, bool> entry))
                {
                    if (!entry.Value && H3MP_TrackedItem.unknownParentWaitList.TryGetValue(entry.Key, out List<uint> waitlist))
                    {
                        waitlist.Remove(localWaitingIndex);
                    }
                }
                H3MP_TrackedItem.unknownTrackedIDs.Remove(localWaitingIndex);
                H3MP_TrackedItem.unknownParentTrackedIDs.Remove(localWaitingIndex);
                H3MP_TrackedItem.unknownControlTrackedIDs.Remove(localWaitingIndex);
                H3MP_TrackedItem.unknownDestroyTrackedIDs.Remove(localWaitingIndex);
                H3MP_TrackedItem.unknownCrateHolding.Remove(localWaitingIndex);
                H3MP_TrackedItem.unknownSosigInventoryItems.Remove(localWaitingIndex);
                if (physicalItem != null && physicalItem.physicalObject is SosigWeaponPlayerInterface)
                {
                    H3MP_TrackedItem.unknownSosigInventoryObjects.Remove((physicalItem.physicalObject as SosigWeaponPlayerInterface).W);
                }
            }

            if (localTrackedID > -1 && localTrackedID < H3MP_GameManager.items.Count)
            {
                // Remove from actual local items list and update the localTrackedID of the item we are moving
                H3MP_GameManager.items[localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[localTrackedID].localTrackedID = localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID out of range!:\n"+Environment.StackTrace);
            }
        }

        public void SetController(int newController, bool recursive = false)
        {
            Mod.LogInfo("\t\t\t\tSetting controller of "+itemID+" at "+trackedID+" to "+newController);
            if (recursive)
            {
                SetControllerRecursive(this, newController);
            }
            else
            {
                controller = newController;
            }
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

        public static void TakeControlRecursive(H3MP_TrackedItemData currentTrackedItem)
        {
            Mod.LogInfo("\t\t\tTakeControlRecursive called on "+currentTrackedItem.itemID+" at "+currentTrackedItem.trackedID);
            // Note: we can return right away if we don't have tracked ID because not tracked ID implies taht this item is already under our control
            // So TakeControlRecursive should never be called without tracked ID in the first place
            if (currentTrackedItem.trackedID < 0)
            {
                return;
            }

            if (H3MP_ThreadManager.host)
            {
                Mod.LogInfo("\t\t\t\tWe are host, sending order to give control");
                H3MP_ServerSend.GiveControl(currentTrackedItem.trackedID, H3MP_GameManager.ID, null);
            }
            else
            {
                Mod.LogInfo("\t\t\t\tWe are client, sending order to give control");
                H3MP_ClientSend.GiveControl(currentTrackedItem.trackedID, H3MP_GameManager.ID, null);
            }
            Mod.LogInfo("\t\t\t\tSetting controller");
            currentTrackedItem.SetController(H3MP_GameManager.ID);
            if (currentTrackedItem.localTrackedID == -1)
            {
                Mod.LogInfo("\t\t\t\t\tAdding to local");
                currentTrackedItem.localTrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(currentTrackedItem);
            }

            if (currentTrackedItem.children != null)
            {
                foreach (H3MP_TrackedItemData child in currentTrackedItem.children)
                {
                    TakeControlRecursive(child);
                }
            }
        }

        public void OnControl()
        {
            insuranceCounter = insuranceCount;
        }
    }
}
