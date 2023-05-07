using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedItemData : TrackedObjectData
    {
        public TrackedItem physicalItem;

        public bool underActiveControl;
        public bool previousActiveControl;

        // Data
        public string itemID;
        public byte[] identifyingData;
        public byte[] additionalData;

        // State
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 velocity = Vector3.zero;

        public byte[] data; // Generic data that may need to be passed every update
        public byte[] previousData;
        public int[] toPutInSosigInventory;

        public TrackedItemData()
        {

        }

        public TrackedItemData(Packet packet) : base(packet)
        {
            // Full
            itemID = packet.ReadString();
            int identifyingDataLength = packet.ReadInt();
            if (identifyingDataLength > 0)
            {
                identifyingData = packet.ReadBytes(identifyingDataLength);
            }
            int additionalDataLen = packet.ReadInt();
            if (additionalDataLen > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLen);
            }

            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            int dataLength = packet.ReadInt();
            if (dataLength > 0)
            {
                data = packet.ReadBytes(dataLength);
            }
            underActiveControl = packet.ReadBool();
        }

        public static bool IfOfType(Transform t)
        {
            FVRPhysicalObject physicalObject = t.GetComponent<FVRPhysicalObject>();
            if (physicalObject != null)
            {
                return (physicalObject.ObjectWrapper != null && IM.OD.ContainsKey(physicalObject.ObjectWrapper.ItemID)) ||
                       (physicalObject.IDSpawnedFrom != null && (IM.OD.ContainsKey(physicalObject.IDSpawnedFrom.name) || IM.OD.ContainsKey(physicalObject.IDSpawnedFrom.ItemID))) ||
                       physicalObject.GetComponent<TNH_ShatterableCrate>() != null;
            }

            return false;
        }

        public static TrackedItem MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedItem trackedItem = root.gameObject.AddComponent<TrackedItem>();
            TrackedItemData data = new TrackedItemData();
            trackedItem.data = data;
            data.physicalItem = trackedItem;
            data.physical = data.physicalItem;
            data.physicalItem.physicalItem = root.GetComponent<FVRPhysicalObject>();
            data.physical.physical = data.physicalItem.physicalItem;

            GameManager.trackedItemByItem.Add(data.physicalItem.physicalItem, trackedItem);
            if (data.physicalItem.physicalItem is SosigWeaponPlayerInterface)
            {
                GameManager.trackedItemBySosigWeapon.Add((data.physicalItem.physicalItem as SosigWeaponPlayerInterface).W, trackedItem);
            }
            GameManager.trackedObjectByObject.Add(data.physicalItem.physicalItem, trackedItem);

            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<TrackedObjectData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);
            }
            data.SetItemIdentifyingInfo();
            data.position = trackedItem.transform.position;
            data.rotation = trackedItem.transform.rotation;
            data.active = trackedItem.gameObject.activeInHierarchy;
            data.underActiveControl = data.IsControlled();

            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || GameManager.inPostSceneLoadTrack;

            data.CollectExternalData();

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedItem.updateFunc != null)
            {
                trackedItem.updateFunc();
            }

            return trackedItem;
        }

        private void CollectExternalData()
        {
            TNH_ShatterableCrate crate = physical.GetComponent<TNH_ShatterableCrate>();
            if (crate != null)
            {
                additionalData = new byte[5];

                additionalData[0] = TNH_SupplyPointPatch.inSpawnBoxes ? (byte)1 : (byte)0;
                if (TNH_SupplyPointPatch.inSpawnBoxes)
                {
                    BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(additionalData, 1);
                }

                identifyingData[3] = crate.m_isHoldingHealth ? (byte)1 : (byte)0;
                identifyingData[4] = crate.m_isHoldingToken ? (byte)1 : (byte)0;
            }
            else if (physicalItem.physicalItem is GrappleThrowable)
            {
                GrappleThrowable asGrappleThrowable = (GrappleThrowable)physicalItem.physicalItem;
                additionalData = new byte[asGrappleThrowable.finalRopePoints.Count * 12 + 2];

                additionalData[0] = asGrappleThrowable.m_hasLanded ? (byte)1 : (byte)0;
                additionalData[1] = (byte)asGrappleThrowable.finalRopePoints.Count;
                if (asGrappleThrowable.finalRopePoints.Count > 0)
                {
                    for (int i = 0; i < asGrappleThrowable.finalRopePoints.Count; ++i)
                    {
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].x).CopyTo(additionalData, i * 12 + 2);
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].y).CopyTo(additionalData, i * 12 + 6);
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].z).CopyTo(additionalData, i * 12 + 10);
                    }
                }
            }
        }

        public void SetItemIdentifyingInfo()
        {
            if (physicalItem.physicalItem.ObjectWrapper != null)
            {
                itemID = physicalItem.physicalItem.ObjectWrapper.ItemID;
                return;
            }
            if (physicalItem.physicalItem.IDSpawnedFrom != null)
            {
                if (IM.OD.ContainsKey(physicalItem.physicalItem.IDSpawnedFrom.name))
                {
                    itemID = physicalItem.physicalItem.IDSpawnedFrom.name;
                }
                else if (IM.OD.ContainsKey(physicalItem.physicalItem.IDSpawnedFrom.ItemID))
                {
                    itemID = physicalItem.physicalItem.IDSpawnedFrom.ItemID;
                }
                return;
            }
            TNH_ShatterableCrate crate = physicalItem.physicalItem.GetComponent<TNH_ShatterableCrate>();
            if (crate != null)
            {
                itemID = "TNH_ShatterableCrate";
                identifyingData = new byte[1];
                if (crate.name[9] == 'S') // Small
                {
                    identifyingData[0] = 2;
                }
                else if (crate.name[9] == 'M') // Medium
                {
                    identifyingData[0] = 1;
                }
                else // Large
                {
                    identifyingData[0] = 0;
                }

                return;
            }
        }

        public override IEnumerator Instantiate()
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
                physicalItem = itemObject.AddComponent<TrackedItem>();
                physical = physicalItem;
                awaitingInstantiation = false;
                physical.data = this;
                physicalItem.physicalItem = itemObject.GetComponent<FVRPhysicalObject>();
                physical.physical = physicalItem.physicalItem;

                if (GameManager.trackedItemByItem.TryGetValue(physicalItem.physicalItem, out TrackedItem t))
                {
                    Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical item object already exists in trackedItemByItem\n\tTrackedID: "+ t.data.trackedID);
                }
                else
                {
                    GameManager.trackedItemByItem.Add(physicalItem.physicalItem, physicalItem);
                }
                if (physicalItem.physicalItem is SosigWeaponPlayerInterface)
                {
                    GameManager.trackedItemBySosigWeapon.Add((physicalItem.physicalItem as SosigWeaponPlayerInterface).W, physicalItem);
                }

                if (GameManager.trackedObjectByObject.TryGetValue(physicalItem.physicalItem, out TrackedObject to))
                {
                    Mod.LogError("Error at instantiation of: " + itemID + ": Item's physical object already exists in trackedObjectByObject\n\tTrackedID: " + to.data.trackedID);
                }
                else
                {
                    GameManager.trackedObjectByObject.Add(physicalItem.physicalItem, physicalItem);
                }

                // See Note in GameManager.SyncTrackedObjects
                // Unfortunately this doesn't necessarily help us in this case considering we need the parent to have been instantiated
                // by now, but since the instantiation is a coroutine, we are not guaranteed to have the parent's physObj yet
                if (parent != -1)
                {
                    // Add ourselves to the parent's children
                    TrackedObjectData parentObject = (ThreadManager.host ? Server.objects : Client.objects)[parent];

                    if (parentObject.physical == null)
                    {
                        parentObject.childrenToParent.Add(trackedID);
                    }
                    else
                    {
                        // Physically parent
                        ++ignoreParentChanged;
                        itemObject.transform.parent = parentObject.physical.transform;
                        --ignoreParentChanged;
                    }
                }

                // Set as kinematic if not in control
                if (controller != GameManager.ID)
                {
                    Mod.SetKinematicRecursive(physical.transform, true);
                }

                // Initially set itself
                UpdateFromData(this);

                // Process the initialdata. This must be done after the update so it can override it
                ProcessAdditionalData();

                // Process childrenToParent
                for (int i = 0; i < childrenToParent.Count; ++i)
                {
                    TrackedObjectData childObject = (ThreadManager.host ? Server.objects : Client.objects)[childrenToParent[i]];
                    if (childObject != null && childObject.parent == trackedID && childObject.physical != null)
                    {
                        // Physically parent
                        ++childObject.ignoreParentChanged;
                        childObject.physical.transform.parent = physical.transform;
                        --childObject.ignoreParentChanged;

                        // Call update on child in case it needs to process its new parent somehow
                        // This is needed for attachments that did their latest update before we got their parent's phys
                        // Calling this update will let them mount themselves to their mount properly
                        childObject.UpdateFromData(childObject);
                    }
                }
                childrenToParent.Clear();

                // Add to sosig inventory if necessary
                if(toPutInSosigInventory != null)
                {
                    TrackedSosigData trackedSosig = (ThreadManager.host ? Server.objects[toPutInSosigInventory[0]] : Client.objects[toPutInSosigInventory[0]]) as TrackedSosigData;
                    if(trackedSosig != null && trackedSosig.inventory[toPutInSosigInventory[1]] == trackedID)
                    {
                        ++SosigPickUpPatch.skip;
                        ++SosigPlaceObjectInPatch.skip;
                        if (toPutInSosigInventory[1] == 0)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
                        }
                        else if (toPutInSosigInventory[1] == 1)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
                        }
                        else
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[toPutInSosigInventory[1] - 2].PlaceObjectIn(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
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
            TNH_ShatterableCrate crate = physical.GetComponent<TNH_ShatterableCrate>();
            if (crate != null)
            {
                if (Mod.currentTNHInstance != null && Mod.currentlyPlayingTNH)
                {
                    if (additionalData[0] == 1)
                    {
                        Mod.currentTNHInstance.manager.SupplyPoints[BitConverter.ToInt16(additionalData, 1)].m_spawnBoxes.Add(physical.gameObject);
                    }
                    if (additionalData[3] == 1)
                    {
                        crate.SetHoldingHealth(Mod.currentTNHInstance.manager);
                    }
                    if (additionalData[4] == 1)
                    {
                        crate.SetHoldingToken(Mod.currentTNHInstance.manager);
                    }
                }
            }
            else if(physical.physical is GrappleThrowable)
            {
                if (additionalData[0] == 1 && data[0] == 1)
                {
                    GrappleThrowable asGrappleThrowable = physical.physical as GrappleThrowable;
                    asGrappleThrowable.RootRigidbody.isKinematic = true;
                    asGrappleThrowable.m_isRopeFree = true;
                    asGrappleThrowable.BundledRope.SetActive(false);
                    asGrappleThrowable.m_hasBeenThrown = true;
                    asGrappleThrowable.m_hasLanded = true;
                    if (asGrappleThrowable.m_ropeLengths.Count > 0)
                    {
                        for (int i = asGrappleThrowable.m_ropeLengths.Count - 1; i >= 0; i--)
                        {
                            UnityEngine.Object.Destroy(asGrappleThrowable.m_ropeLengths[i]);
                        }
                        asGrappleThrowable.m_ropeLengths.Clear();
                    }
                    asGrappleThrowable.finalRopePoints.Clear();
                    asGrappleThrowable.FakeRopeLength.SetActive(false);

                    int count = additionalData[1];
                    Vector3 currentRopePoint = new Vector3(BitConverter.ToSingle(additionalData, 2), BitConverter.ToSingle(additionalData, 6), BitConverter.ToSingle(additionalData, 10));
                    for (int i = 1; i < count; ++i)
                    {
                        Vector3 newPoint = new Vector3(BitConverter.ToSingle(additionalData, i * 12 + 2), BitConverter.ToSingle(additionalData, i * 12 + 6), BitConverter.ToSingle(additionalData, i * 12 + 10));
                        Vector3 vector = newPoint - currentRopePoint;

                        GameObject gameObject = UnityEngine.Object.Instantiate(asGrappleThrowable.RopeLengthPrefab, newPoint, Quaternion.LookRotation(-vector, Vector3.up));
                        gameObject.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
                        FVRHandGrabPoint fvrhandGrabPoint = null;
                        if (asGrappleThrowable.m_ropeLengths.Count > 0)
                        {
                            fvrhandGrabPoint = asGrappleThrowable.m_ropeLengths[asGrappleThrowable.m_ropeLengths.Count - 1].GetComponent<FVRHandGrabPoint>();
                        }
                        FVRHandGrabPoint component = gameObject.GetComponent<FVRHandGrabPoint>();
                        asGrappleThrowable.m_ropeLengths.Add(gameObject);
                        if (fvrhandGrabPoint != null && component != null)
                        {
                            fvrhandGrabPoint.ConnectedGrabPoint_Base = component;
                            component.ConnectedGrabPoint_End = fvrhandGrabPoint;
                        }
                        asGrappleThrowable.finalRopePoints.Add(newPoint);
                        currentRopePoint = newPoint;
                    }
                }
            }
        }

        public virtual GameObject GetItemPrefab()
        {
            if (itemID.Equals("TNH_ShatterableCrate") && GM.TNH_Manager != null)
            {
                return GM.TNH_Manager.Prefabs_ShatterableCrates[identifyingData[0]];
            }
            return null;
        }

        public override void UpdateFromData(TrackedObjectData updatedObject)
        {
            base.UpdateFromData(updatedObject);

            TrackedItemData updatedItem = updatedObject as TrackedItemData;
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedItem.rotation;
            if (physical != null)
            {
                if (!TrackedItem.interpolated)
                {
                    if (parent == -1)
                    {
                        physical.transform.position = updatedItem.position;
                        physical.transform.rotation = updatedItem.rotation;
                    }
                    else
                    {
                        // If parented, the position and rotation are relative, so set it now after parenting
                        physical.transform.localPosition = updatedItem.position;
                        physical.transform.localRotation = updatedItem.rotation;
                    }
                }

                previousActiveControl = underActiveControl;
                underActiveControl = updatedItem.underActiveControl;
            }

            UpdateData(updatedItem.data);
        }

        public override bool Update(bool full = false)
        {
            // Phys could be null if we were given control of the item while we were loading and we haven't instantiated it on our side yet
            if(physical == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            if (parent == -1)
            {
                position = physical.transform.position;
                rotation = physical.transform.rotation;
            }
            else
            {
                position = physical.transform.localPosition;
                rotation = physical.transform.localRotation;
            }

            previousActiveControl = underActiveControl;
            underActiveControl = IsControlled();

            // Note: UpdateData() must be done first in this expression, otherwise, if active/position/rotation is different,
            // it will return true before making the call
            return UpdateData() || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public static bool IsControlled(Transform root)
        {
            FVRPhysicalObject physObj = root.GetComponent<FVRPhysicalObject>();
            if(physObj != null)
            {
                return physObj.m_hand != null || physObj.QuickbeltSlot != null;
            }
            return false;
        }

        public bool IsControlled()
        {
            return physicalItem.physicalItem.m_hand != null || physicalItem.physicalItem.QuickbeltSlot != null;
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !DataEqual();
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

        public override void OnTrackedIDReceived()
        {
            base.OnTrackedIDReceived();

            if (localTrackedID != -1 && TrackedItem.unknownCrateHolding.TryGetValue(localWaitingIndex, out byte option))
            {
                bool health = option == 0 || option == 2;
                bool token = option == 1 || option == 2;
                if (ThreadManager.host)
                {
                    if (health)
                    {
                        ServerSend.ShatterableCrateSetHoldingHealth(trackedID);
                    }
                    if (token)
                    {
                        ServerSend.ShatterableCrateSetHoldingToken(trackedID);
                    }
                }
                else
                {
                    if (health)
                    {
                        ClientSend.ShatterableCrateSetHoldingHealth(trackedID);
                    }
                    if (token)
                    {
                        ClientSend.ShatterableCrateSetHoldingToken(trackedID);
                    }
                }

                TrackedItem.unknownCrateHolding.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedItem.unknownSosigInventoryItems.TryGetValue(localWaitingIndex, out KeyValuePair<TrackedSosigData, int> entry))
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
                            ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, true);
                        }
                        else if (entry.Value == 1)
                        {
                            ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, false);
                        }
                        else
                        {
                            ClientSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                        }
                    }
                    // else, sosig does not yet have a tracked ID, this interaction will be sent upon it receiving its ID
                }
                // else, sosig has been destroyed

                TrackedItem.unknownSosigInventoryItems.Remove(localWaitingIndex);
            }
        }

        public override void OnTracked()
        {
            if (physicalItem.physicalItem is SosigWeaponPlayerInterface &&
                TrackedItem.unknownSosigInventoryObjects.TryGetValue((physicalItem.physicalItem as SosigWeaponPlayerInterface).W, out KeyValuePair<TrackedSosigData, int> entry))
            {
                if(entry.Key.physicalObject != null)
                {
                    if (trackedID == -1)
                    {
                        // Item tracked but don't have tracked ID yet, add to other unknown
                        TrackedItem.unknownSosigInventoryItems.Add(localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(entry.Key, entry.Value));
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
                                if (ThreadManager.host)
                                {
                                    ServerSend.SosigPickUpItem(entry.Key.trackedID, trackedID, true);
                                }
                                else
                                {
                                    ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, true);
                                }
                            }
                            else if (entry.Value == 1)
                            {
                                if (ThreadManager.host)
                                {
                                    ServerSend.SosigPickUpItem(entry.Key.trackedID, trackedID, false);
                                }
                                else
                                {
                                    ClientSend.SosigPickUpItem(entry.Key.physicalObject, trackedID, false);
                                }
                            }
                            else
                            {
                                if (ThreadManager.host)
                                {
                                    ServerSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                                }
                                else
                                {
                                    ClientSend.SosigPlaceItemIn(entry.Key.trackedID, entry.Value - 2, trackedID);
                                }
                            }
                        }
                        // else, sosig does not yet have a tracked ID, this interaction will be sent upon it receiving its ID
                    }
                }
                // else, sosig has been destroyed

                TrackedItem.unknownSosigInventoryObjects.Remove((physicalItem.physicalItem as SosigWeaponPlayerInterface).W);
            }
        }

        public override void RemoveFromLocal()
        {
            // Manage unknown lists
            if (trackedID == -1)
            {
                if (TrackedItem.unknownTrackedIDs.TryGetValue(localWaitingIndex, out KeyValuePair<uint, bool> entry))
                {
                    if (!entry.Value && TrackedItem.unknownParentWaitList.TryGetValue(entry.Key, out List<uint> waitlist))
                    {
                        waitlist.Remove(localWaitingIndex);
                    }
                }
                TrackedItem.unknownTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownParentTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownControlTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownDestroyTrackedIDs.Remove(localWaitingIndex);
                TrackedItem.unknownCrateHolding.Remove(localWaitingIndex);
                TrackedItem.unknownSosigInventoryItems.Remove(localWaitingIndex);
                if (physical != null && physical.physical is SosigWeaponPlayerInterface)
                {
                    TrackedItem.unknownSosigInventoryObjects.Remove((physical.physical as SosigWeaponPlayerInterface).W);
                }
            }

            base.RemoveFromLocal();
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if(newController == GameManager.ID) // Gain control
            {
                if (physical != null && parent == -1)
                {
                    Mod.SetKinematicRecursive(physical.transform, false);
                }
            }
            else if(controller == GameManager.ID) // Lose control
            {
                if (physical != null)
                {
                    physical.EnsureUncontrolled();

                    Mod.SetKinematicRecursive(physical.transform, true);
                }
            }
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            if (full)
            {
                packet.Write(itemID);
                if (identifyingData == null || identifyingData.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(identifyingData.Length);
                    packet.Write(identifyingData);
                }
                packet.Write(parent);
                if (additionalData == null || additionalData.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(additionalData.Length);
                    packet.Write(additionalData);
                }
            }
            else
            {
                if (incrementOrder)
                {
                    packet.Write(order++);
                }
                else
                {
                    packet.Write(order);
                }
            }

            packet.Write(position);
            packet.Write(rotation);
            if (data == null || data.Length == 0)
            {
                packet.Write(0);
            }
            else
            {
                packet.Write(data.Length);
                packet.Write(data);
            }
            packet.Write(underActiveControl);
        }

        public override void ParentChanged()
        {
            // Call updateParent delegate on item if it has one
            if (physicalItem.updateParentFunc != null)
            {
                physicalItem.updateParentFunc();
            }
        }
    }
}
