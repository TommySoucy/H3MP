using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
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

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnCollectAdditionalData event
        /// </summary>
        /// <param name="collected">Custom override for whether additional data was collected. If false, H3MP will try to collect</param>
        public delegate void OnCollectAdditionalDataDelegate(ref bool collected);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when collecting an item's additional data
        /// </summary>
        public static event OnCollectAdditionalDataDelegate OnCollectAdditionalData;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnProcessAdditionalData event
        /// </summary>
        /// <param name="processed">Custom override for whether additional data was processed. If false, H3MP will try to process</param>
        public delegate void OnProcessAdditionalDataDelegate(ref bool processed);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when processing an item's additional data on instantiation
        /// </summary>
        public static event OnProcessAdditionalDataDelegate OnProcessAdditionalData;

        public TrackedItemData()
        {

        }

        public TrackedItemData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            int dataLength = packet.ReadInt();
            if (dataLength > 0)
            {
                data = packet.ReadBytes(dataLength);
            }
            underActiveControl = packet.ReadBool();

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
        }

        public static bool IsOfType(Transform t)
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

        public override bool IsIdentifiable()
        {
            return IM.OD.ContainsKey(itemID) || itemID.Equals("TNH_ShatterableCrate");
        }

        public static TrackedItem MakeTracked(Transform root, TrackedObjectData parent)
        {
            Mod.LogInfo("Making item tracked", false);
            TrackedItem trackedItem = root.gameObject.AddComponent<TrackedItem>();
            TrackedItemData data = new TrackedItemData();
            trackedItem.data = data;
            trackedItem.itemData = data;
            data.physicalItem = trackedItem;
            data.physical = trackedItem;
            data.physicalItem.physicalItem = root.GetComponent<FVRPhysicalObject>();
            data.physical.physical = data.physicalItem.physicalItem;

            data.typeIdentifier = "TrackedItemData";
            data.active = trackedItem.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedItemByItem.Add(data.physicalItem.physicalItem, trackedItem);
            if (data.physicalItem.physicalItem is SosigWeaponPlayerInterface)
            {
                GameManager.trackedItemBySosigWeapon.Add((data.physicalItem.physicalItem as SosigWeaponPlayerInterface).W, trackedItem);
            }
            GameManager.trackedObjectByObject.Add(data.physicalItem.physicalItem, trackedItem);
            GameManager.trackedObjectByInteractive.Add(data.physicalItem.physicalItem, trackedItem);

            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<TrackedObjectData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);

                data.position = trackedItem.transform.localPosition;
                data.rotation = trackedItem.transform.localRotation;
            }
            else
            {
                data.position = trackedItem.transform.position;
                data.rotation = trackedItem.transform.rotation;
            }
            data.SetItemIdentifyingInfo();
            Mod.LogInfo("\tItemID: "+data.itemID, false);
            data.underActiveControl = data.IsControlled(out int interactionID);

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
            bool collected = false;
            if(OnCollectAdditionalData != null)
            {
                OnCollectAdditionalData(ref collected);
            }
            if (collected)
            {
                return;
            }

            TNH_ShatterableCrate crate = physical.GetComponent<TNH_ShatterableCrate>();
            if (crate != null)
            {
                additionalData = new byte[5];

                additionalData[0] = TNH_SupplyPointPatch.inSpawnBoxes ? (byte)1 : (byte)0;
                if (TNH_SupplyPointPatch.inSpawnBoxes)
                {
                    BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(additionalData, 1);
                }

                additionalData[3] = crate.m_isHoldingHealth ? (byte)1 : (byte)0;
                additionalData[4] = crate.m_isHoldingToken ? (byte)1 : (byte)0;
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
            else if(physicalItem.dataObject is UberShatterable)
            {
                additionalData = new byte[2];
                additionalData[0] = (physicalItem.dataObject as UberShatterable).m_hasShattered ? (byte)1 : (byte)0;
                additionalData[1] = 0; // Do not have destruction data
            }
            else if(physicalItem.dataObject is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = physicalItem.dataObject as FVRFireArmAttachment;
                if (asAttachment.curMount != null && GameManager.trackedItemByItem.TryGetValue(asAttachment.curMount.MyObject, out TrackedItem myTrackedItem))
                {
                    physicalItem.mountObjectID = myTrackedItem.data.trackedID;
                }
                else
                {
                    physicalItem.mountObjectID = -1;
                }
            }
            else if(physicalItem.dataObject is AttachableFirearm)
            {
                AttachableFirearm asAFA = physicalItem.dataObject as AttachableFirearm;
                if (asAFA.Attachment.curMount != null && GameManager.trackedItemByItem.TryGetValue(asAFA.Attachment.curMount.MyObject, out TrackedItem myTrackedItem))
                {
                    physicalItem.mountObjectID = myTrackedItem.data.trackedID;
                }
                else
                {
                    physicalItem.mountObjectID = -1;
                }
            }
            else if(physicalItem.dataObject is Brut_GasCuboid)
            {
                Brut_GasCuboid asGC = physicalItem.dataObject as Brut_GasCuboid;
                additionalData = new byte[2 + asGC.m_gouts.Count * 24];
                additionalData[0] = asGC.m_isHandleBrokenOff ? (byte)1 : (byte)0;
                additionalData[1] = (byte)asGC.m_gouts.Count;
                for (int i = 0; i < asGC.m_gouts.Count; ++i) 
                {
                    int firstIndex = i * 24 + 2;
                    Vector3 pos = asGC.m_gouts[i].transform.localPosition;
                    byte[] vecBytes = BitConverter.GetBytes(pos.x);
                    for(int j=0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j] = vecBytes[j];
                    }
                    vecBytes = BitConverter.GetBytes(pos.y);
                    for(int j=0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j + 4] = vecBytes[j];
                    }
                    vecBytes = BitConverter.GetBytes(pos.z);
                    for(int j=0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j + 8] = vecBytes[j];
                    }
                    Vector3 rot = asGC.m_gouts[i].transform.localEulerAngles;
                    vecBytes = BitConverter.GetBytes(rot.x);
                    for (int j = 0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j] = vecBytes[j];
                    }
                    vecBytes = BitConverter.GetBytes(rot.y);
                    for (int j = 0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j + 4] = vecBytes[j];
                    }
                    vecBytes = BitConverter.GetBytes(rot.z);
                    for (int j = 0; j < 4; ++j)
                    {
                        additionalData[firstIndex + j + 8] = vecBytes[j];
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
                Mod.LogInfo("Instantiating item at "+trackedID+": "+itemID, false);
                ++Mod.skipAllInstantiates;
                if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at item instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
                GameObject itemObject = GameObject.Instantiate(itemPrefab, position, rotation);
                --Mod.skipAllInstantiates;
                physicalItem = itemObject.AddComponent<TrackedItem>();
                physical = physicalItem;
                awaitingInstantiation = false;
                physicalItem.itemData = this;
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
                GameManager.trackedObjectByInteractive.Add(physicalItem.physicalItem, physicalItem);

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
                            trackedSosig.physicalSosig.physicalSosig.Hand_Primary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
                        }
                        else if (toPutInSosigInventory[1] == 1)
                        {
                            trackedSosig.physicalSosig.physicalSosig.Hand_Secondary.PickUp(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
                        }
                        else
                        {
                            trackedSosig.physicalSosig.physicalSosig.Inventory.Slots[toPutInSosigInventory[1] - 2].PlaceObjectIn(((SosigWeaponPlayerInterface)physicalItem.physicalItem).W);
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

        private void ProcessAdditionalData()
        {
            bool processed = false;
            if(OnProcessAdditionalData != null)
            {
                OnProcessAdditionalData(ref processed);
            }
            if (processed)
            {
                return;
            }

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
            else if(physicalItem.dataObject is UberShatterable)
            {
                if (additionalData[0] == 1)
                {
                    if(additionalData[1] == 0)
                    {
                        ++UberShatterableShatterPatch.skip;
                        (physicalItem.dataObject as UberShatterable).Shatter(Vector3.zero, Vector3.zero, 0);
                        --UberShatterableShatterPatch.skip;
                    }
                    else // We have destruction data
                    {
                        Vector3 point = new Vector3(BitConverter.ToSingle(additionalData, 2), BitConverter.ToSingle(additionalData, 6), BitConverter.ToSingle(additionalData, 10));
                        Vector3 dir = new Vector3(BitConverter.ToSingle(additionalData, 14), BitConverter.ToSingle(additionalData, 18), BitConverter.ToSingle(additionalData, 22));
                        float intensity = BitConverter.ToSingle(additionalData, 26);
                        ++UberShatterableShatterPatch.skip;
                        (physicalItem.dataObject as UberShatterable).Shatter(point, dir, intensity);
                        --UberShatterableShatterPatch.skip;
                    }
                }
            }
            else if(physicalItem.dataObject is Brut_GasCuboid)
            {
                Brut_GasCuboid asGC = physicalItem.dataObject as Brut_GasCuboid;
                asGC.m_isHandleBrokenOff = additionalData[0] == 1;
                asGC.Handle.SetActive(asGC.m_isHandleBrokenOff);
                for (int i = 0; i < additionalData[1]; ++i) 
                {
                    int startIndex = i * 24 + 2;
                    Vector3 pos = new Vector3(BitConverter.ToSingle(additionalData, startIndex), BitConverter.ToSingle(additionalData, startIndex + 4), BitConverter.ToSingle(additionalData, startIndex + 8));
                    Vector3 normal = new Vector3(BitConverter.ToSingle(additionalData, startIndex), BitConverter.ToSingle(additionalData, startIndex + 4), BitConverter.ToSingle(additionalData, startIndex + 8));

                    asGC.hasGeneratedGoutYet = false;
                    asGC.GenerateGout(pos, normal);
                }
            }

            if(physicalItem.integratedLaser != null && controller != GameManager.ID)
            {
                physicalItem.integratedLaser.UsesAutoOnOff = false;
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

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedItemData updatedItem = updatedObject as TrackedItemData;

            if (full)
            {
                itemID = updatedItem.itemID;
                identifyingData = updatedItem.identifyingData;
                additionalData = updatedItem.additionalData;
            }

            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedItem.rotation;
            previousActiveControl = underActiveControl;
            underActiveControl = updatedItem.underActiveControl;
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
            }

            UpdateData(updatedItem.data);
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            previousRot = rotation;
            position = packet.ReadVector3();
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = packet.ReadQuaternion();
            int dataCount = packet.ReadInt();
            byte[] newData = packet.ReadBytes(dataCount);
            previousActiveControl = underActiveControl;
            underActiveControl = packet.ReadBool();

            if (full)
            {
                itemID = packet.ReadString();
                int identifyingDataCount = packet.ReadInt();
                if(identifyingDataCount > 0)
                {
                    identifyingData = packet.ReadBytes(identifyingDataCount);
                }
                else
                {
                    identifyingData = null;
                }
                int additionalDataCount = packet.ReadInt();
                if (additionalDataCount > 0)
                {
                    additionalData = packet.ReadBytes(additionalDataCount);
                }
                else
                {
                    additionalData = null;
                }
            }

            if (physical != null)
            {
                if (!TrackedItem.interpolated)
                {
                    if (parent == -1)
                    {
                        physical.transform.position = position;
                        physical.transform.rotation = rotation;
                    }
                    else
                    {
                        // If parented, the position and rotation are relative, so set it now after parenting
                        physical.transform.localPosition = position;
                        physical.transform.localRotation = rotation;
                    }
                }
            }

            UpdateData(newData);
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

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
            underActiveControl = IsControlled(out int interactionID);

            // Note: UpdateData() must be done first in this expression, otherwise, if active/position/rotation is different,
            // it will return true before making the call
            return UpdateData() || updated || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || previousActiveControl != underActiveControl || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !DataEqual();
        }

        public static bool IsControlled(Transform root)
        {
            FVRPhysicalObject physObj = root.GetComponent<FVRPhysicalObject>();
            if (physObj != null)
            {
                bool inPlayerQBS = physObj.QuickbeltSlot != null && GM.CurrentPlayerBody != null 
                                   && (GM.CurrentPlayerBody.QBSlots_Internal.Contains(physObj.QuickbeltSlot) 
                                       || GM.CurrentPlayerBody.QBSlots_Added.Contains(physObj.QuickbeltSlot));

                return physObj.m_hand != null || inPlayerQBS;
            }
            return false;
        }

        public override bool IsControlled(out int interactionID)
        {
            interactionID = -1;
            bool inQBS = physicalItem.physicalItem.QuickbeltSlot != null && GM.CurrentPlayerBody != null;

            bool inPlayerQBS = false;
            if (inQBS)
            {
                for(int i=0; i< GM.CurrentPlayerBody.QBSlots_Internal.Count; ++i)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Internal[i] == physicalItem.physicalItem.QuickbeltSlot)
                    {
                        interactionID = i + 3; // Internal QBS 3-258
                        inPlayerQBS = true;
                        break;
                    }
                }
                for(int i=0; i< GM.CurrentPlayerBody.QBSlots_Added.Count; ++i)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Added[i] == physicalItem.physicalItem.QuickbeltSlot)
                    {
                        interactionID = i + 259; // Added QBS 259-514
                        inPlayerQBS = true;
                        break;
                    }
                }
            }

            if (!inPlayerQBS && physicalItem.physicalItem.m_hand != null)
            {
                if (physicalItem.physicalItem.m_hand.IsThisTheRightHand)
                {
                    interactionID = 2; // Right hand
                }
                else
                {
                    interactionID = 1; // Left hand
                }
            }

            return physicalItem.physicalItem.m_hand != null || inPlayerQBS;
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

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

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
                if (entry.Key.physicalSosig != null)
                {
                    if (entry.Key.trackedID != -1)
                    {
                        // Set the value in our data
                        entry.Key.inventory[entry.Value] = trackedID;

                        // Send to others
                        if (entry.Value == 0)
                        {
                            ClientSend.SosigPickUpItem(entry.Key.physicalSosig, trackedID, true);
                        }
                        else if (entry.Value == 1)
                        {
                            ClientSend.SosigPickUpItem(entry.Key.physicalSosig, trackedID, false);
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
            if (localTrackedID != -1 && TrackedItem.unknownGasCuboidGout.TryGetValue(localWaitingIndex, out List<KeyValuePair<Vector3, Vector3>> goutList))
            {
                for(int i=0; i< goutList.Count; ++i)
                {
                    ClientSend.GasCuboidGout(trackedID, goutList[i].Key, goutList[i].Value);
                }

                TrackedItem.unknownGasCuboidGout.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedItem.unknownGasCuboidDamageHandle.Contains(localWaitingIndex))
            {
                ClientSend.GasCuboidDamageHandle(trackedID);

                TrackedItem.unknownGasCuboidDamageHandle.Remove(localWaitingIndex);
            }
        }

        public override void OnTracked()
        {
            if (physicalItem.physicalItem is SosigWeaponPlayerInterface &&
                TrackedItem.unknownSosigInventoryObjects.TryGetValue((physicalItem.physicalItem as SosigWeaponPlayerInterface).W, out KeyValuePair<TrackedSosigData, int> entry))
            {
                if(entry.Key.physicalSosig != null)
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
                                    ClientSend.SosigPickUpItem(entry.Key.physicalSosig, trackedID, true);
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
                                    ClientSend.SosigPickUpItem(entry.Key.physicalSosig, trackedID, false);
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
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                TrackedItem.unknownCrateHolding.Remove(localWaitingIndex);
                TrackedItem.unknownSosigInventoryItems.Remove(localWaitingIndex);
                if (physical != null && physical.physical is SosigWeaponPlayerInterface)
                {
                    TrackedItem.unknownSosigInventoryObjects.Remove((physical.physical as SosigWeaponPlayerInterface).W);
                }

                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalItem != null && physicalItem.physicalItem != null)
                {
                    GameManager.trackedItemByItem.Remove(physicalItem.physicalItem);

                    if (physicalItem.physicalItem is SosigWeaponPlayerInterface)
                    {
                        GameManager.trackedItemBySosigWeapon.Remove((physicalItem.physicalItem as SosigWeaponPlayerInterface).W);
                    }

                    GameManager.trackedObjectByInteractive.Remove(physicalItem.physicalItem);

                    if (physicalItem.removeTrackedDamageables != null)
                    {
                        physicalItem.removeTrackedDamageables();
                    }
                }
            }
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if(newController == GameManager.ID) // Gain control
            {
                if(physicalItem != null)
                {
                    if (parent == -1)
                    {
                        Mod.SetKinematicRecursive(physicalItem.transform, false);
                    }

                    if(physicalItem.integratedLaser != null)
                    {
                        physicalItem.integratedLaser.UsesAutoOnOff = physicalItem.usesAutoToggle;
                    }
                }
            }
            else if(controller == GameManager.ID) // Lose control
            {
                if (physicalItem != null)
                {
                    physicalItem.EnsureUncontrolled();

                    Mod.SetKinematicRecursive(physicalItem.transform, true);

                    if (physicalItem.integratedLaser != null)
                    {
                        physicalItem.integratedLaser.UsesAutoOnOff = false;
                    }
                }
            }
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

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
        }

        public override void ParentChanged()
        {
            // Call updateParent delegate on item if it has one
            if (physicalItem.updateParentFunc != null)
            {
                physicalItem.updateParentFunc();
            }

            // If have parent we want to make sure our position is correct
            if(parent != -1)
            {
                physicalItem.transform.localPosition = position;
                physicalItem.transform.localRotation = rotation;
            }
        }
    }
}
