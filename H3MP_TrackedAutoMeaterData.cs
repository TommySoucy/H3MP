using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedAutoMeaterData
    {
        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
        public byte order; // The index of this AutoMeater's data packet used to ensure we process this data in the correct order

        public int trackedID = -1;
        public int controller;
        public byte ID; // 0: SMG, 1: Flak, 2: FlameThrower, 3: MachineGun, 4: Supression, 5: MF Blue, 6: MF Red
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion previousSideToSideRotation;
        public Quaternion sideToSideRotation;
        public float previousHingeTargetPos;
        public float hingeTargetPos;
        public Quaternion previousUpDownMotorRotation;
        public Quaternion upDownMotorRotation;
        public float previousUpDownJointTargetPos;
        public float upDownJointTargetPos;
        public H3MP_TrackedAutoMeater physicalObject;
        public int localTrackedID;
        public uint localWaitingIndex = uint.MaxValue;
        public bool previousActive;
        public bool active;
        public byte previousIFF;
        public byte IFF;
        public bool removeFromListOnDestroy = true;
        public string scene;
        public int instance;
        public byte[] data;
        public bool sceneInit;
        public bool awaitingInstantiation;

        public Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone> hitZones = new Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone>();

        public IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating AutoMeater " + trackedID);
            string itemID = AutoMeaterIDToItemID(ID);
            yield return IM.OD[itemID].GetGameObjectAsync();
            GameObject autoMeaterPrefab = IM.OD[itemID].GetGameObject();
            if (autoMeaterPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate AutoMeater sent from {controller} but failed to get prefab.");
                awaitingInstantiation = false;
                yield break;
            }

            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at automeater instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
            GameObject autoMeaterInstance = GameObject.Instantiate(autoMeaterPrefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalObject = autoMeaterInstance.AddComponent<H3MP_TrackedAutoMeater>();
            awaitingInstantiation = false;
            physicalObject.data = this;

            physicalObject.physicalAutoMeaterScript = autoMeaterInstance.GetComponent<AutoMeater>();

            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Add(physicalObject.physicalAutoMeaterScript, physicalObject);

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != H3MP_GameManager.ID)
            {
                GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalAutoMeaterScript.E);
                physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
            }

            // Initially set IFF
            physicalObject.physicalAutoMeaterScript.E.IFFCode = IFF;

            // Get hitzones
            AutoMeaterHitZone[] hitZoneArr = physicalObject.GetComponentsInChildren<AutoMeaterHitZone>();
            foreach(AutoMeaterHitZone hitZone in hitZoneArr)
            {
                hitZones.Add(hitZone.Type, hitZone);
            }

            ProcessData();

            // Initially set itself
            Update(this);
        }

        // MOD: This will be called at the end of instantiation so mods can use it to process the data array
        //      Example here is data about the TNH context
        private void ProcessData()
        {
            if (GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                if (data[0] == 1) // TNH_HoldPoint is in spawn turrets
                {
                    ((List<AutoMeater>)Mod.TNH_HoldPoint_m_activeTurrets.GetValue(GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex])).Add(physicalObject.physicalAutoMeaterScript);
                }
                else if (data[1] == 1) // TNH_SupplyPoint is in Spawn Take Enemy Group
                {
                    ((List<AutoMeater>)Mod.TNH_SupplyPoint_m_activeTurrets.GetValue(GM.TNH_Manager.SupplyPoints[BitConverter.ToInt16(data, 2)])).Add(physicalObject.physicalAutoMeaterScript);
                }
            }
        }

        public static string AutoMeaterIDToItemID(byte ID)
        {
            switch (ID)
            {
                case 0:
                    return "Turburgert_SMG";
                case 1:
                    return "Turburgert_Flak";
                case 2:
                    return "Turburgert_Flamethrower";
                case 3:
                    return "Turburgert_MachineGun";
                case 4:
                    return "Turburgert_Suppression";
                case 5:
                    return "TurburgertMFBlue";
                case 6:
                    return "TurburgertMFRed";
                default:
                    Mod.LogInfo("AutoMeaterIDToItemID: Invalid auto meater ID: " + ID);
                    return "Turburgert_SMG";
            }
        }

        public void Update(H3MP_TrackedAutoMeaterData updatedItem)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            rotation = updatedItem.rotation;
            previousActive = active;
            active = updatedItem.active;
            previousIFF = IFF;
            IFF = updatedItem.IFF;
            previousSideToSideRotation = sideToSideRotation;
            sideToSideRotation = updatedItem.sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            hingeTargetPos = updatedItem.hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            upDownMotorRotation = updatedItem.upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            upDownJointTargetPos = updatedItem.upDownJointTargetPos;

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalAutoMeaterScript.RB.position = position;
                physicalObject.physicalAutoMeaterScript.RB.rotation = rotation;
                physicalObject.physicalAutoMeaterScript.E.IFFCode = IFF;
                physicalObject.physicalAutoMeaterScript.SideToSideTransform.localRotation = sideToSideRotation;
                HingeJoint hingeJoint = physicalObject.physicalAutoMeaterScript.SideToSideHinge;
                JointSpring spring = hingeJoint.spring;
                spring.targetPosition = hingeTargetPos;
                hingeJoint.spring = spring;
                physicalObject.physicalAutoMeaterScript.UpDownTransform.localRotation = upDownMotorRotation;
                HingeJoint upDownHingeJoint = physicalObject.physicalAutoMeaterScript.UpDownHinge;
                spring = upDownHingeJoint.spring;
                spring.targetPosition = upDownJointTargetPos;
                upDownHingeJoint.spring = spring;

                if (active)
                {
                    if (!physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(false);
                    }
                }
            }
        }

        public bool Update(bool full = false)
        {
            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalAutoMeaterScript.RB.position;
            rotation = physicalObject.physicalAutoMeaterScript.RB.rotation;

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;
            previousIFF = IFF;
            IFF = (byte)physicalObject.physicalAutoMeaterScript.E.IFFCode;

            previousSideToSideRotation = sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            sideToSideRotation = physicalObject.physicalAutoMeaterScript.SideToSideTransform.localRotation;
            hingeTargetPos = physicalObject.physicalAutoMeaterScript.SideToSideHinge.spring.targetPosition;
            upDownMotorRotation = physicalObject.physicalAutoMeaterScript.UpDownTransform.localRotation;
            upDownJointTargetPos = physicalObject.physicalAutoMeaterScript.UpDownHinge.spring.targetPosition;

            return NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active || !previousSideToSideRotation.Equals(sideToSideRotation) ||
                   !previousUpDownMotorRotation.Equals(upDownMotorRotation) || previousHingeTargetPos != hingeTargetPos || previousUpDownJointTargetPos != upDownJointTargetPos;
        }

        public void OnTrackedIDReceived()
        {
            if (H3MP_TrackedAutoMeater.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                H3MP_ClientSend.DestroySosig(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                H3MP_Client.autoMeaters[trackedID] = null;

                // Remove from autoMeatersByInstanceByScene
                H3MP_GameManager.autoMeatersByInstanceByScene[scene][instance].Remove(trackedID);

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && H3MP_TrackedAutoMeater.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = H3MP_TrackedAutoMeater.unknownControlTrackedIDs[localWaitingIndex];

                H3MP_ClientSend.GiveAutoMeaterControl(trackedID, newController);

                // Also change controller locally
                controller = newController;

                H3MP_TrackedAutoMeater.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (H3MP_GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }

            if (localTrackedID != -1)
            {
                // Add to autoMeater tracking list
                if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(instance, out List<int> autoMeaterList))
                    {
                        autoMeaterList.Add(trackedID);
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
                    H3MP_GameManager.autoMeatersByInstanceByScene.Add(scene, newInstances);
                }
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            H3MP_TrackedAutoMeater.unknownControlTrackedIDs.Remove(localWaitingIndex);
            H3MP_TrackedAutoMeater.unknownDestroyTrackedIDs.Remove(localWaitingIndex);

            if (localTrackedID > -1 && localTrackedID < H3MP_GameManager.autoMeaters.Count)
            {
                // Remove from actual local items list and update the localTrackedID of the autoMeater we are moving
                H3MP_GameManager.autoMeaters[localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                H3MP_GameManager.autoMeaters[localTrackedID].localTrackedID = localTrackedID;
                H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID out of range!:\n" + Environment.StackTrace);
            }
        }
    }
}
