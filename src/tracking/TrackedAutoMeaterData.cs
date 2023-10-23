using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedAutoMeaterData : TrackedObjectData
    {
        public TrackedAutoMeater physicalAutoMeater;

        public byte ID; // 0: SMG, 1: Flak, 2: FlameThrower, 3: MachineGun, 4: Supression, 5: MF Blue, 6: MF Red
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public float previousMotorHingeSpringTarget;
        public float motorHingeSpringTarget;
        public Quaternion previousSideToSideRotation;
        public Quaternion sideToSideRotation;
        public float previousMotorUpDownTarget;
        public float motorUpDownTarget;
        public Quaternion previousUpDownMotorRotation;
        public Quaternion upDownMotorRotation;
        public byte previousIFF;
        public byte IFF;
        public byte[] data;

        public Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone> hitZones = new Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone>();

        public TrackedAutoMeaterData()
        {

        }

        public TrackedAutoMeaterData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            IFF = packet.ReadByte();
            motorHingeSpringTarget = packet.ReadFloat();
            sideToSideRotation = packet.ReadQuaternion();
            motorUpDownTarget = packet.ReadFloat();
            upDownMotorRotation = packet.ReadQuaternion();

            // Full
            ID = packet.ReadByte();
            int dataLen = packet.ReadInt();
            if (dataLen > 0)
            {
                data = packet.ReadBytes(dataLen);
            }
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<AutoMeater>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            return root.GetComponent<AutoMeater>().PO.m_hand != null;
        }

        public override bool IsControlled(out int interactionID)
        {
            interactionID = -1;
            if (physicalAutoMeater.physicalAutoMeater.PO.m_hand != null)
            {
                if (physicalAutoMeater.physicalAutoMeater.PO.m_hand.IsThisTheRightHand)
                {
                    interactionID = 2; // Right hand
                }
                else
                {
                    interactionID = 1; // Left hand
                }
            }

            return physicalAutoMeater.physicalAutoMeater.PO.m_hand != null;
        }

        public static TrackedAutoMeater MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedAutoMeater trackedAutoMeater = root.gameObject.AddComponent<TrackedAutoMeater>();
            TrackedAutoMeaterData data = new TrackedAutoMeaterData();
            trackedAutoMeater.data = data;
            trackedAutoMeater.autoMeaterData = data;
            data.physicalAutoMeater = trackedAutoMeater;
            data.physical = trackedAutoMeater;
            AutoMeater autoMeaterScript = root.GetComponent<AutoMeater>(); ;
            data.physicalAutoMeater.physicalAutoMeater = autoMeaterScript;
            data.physical.physical = autoMeaterScript;

            data.typeIdentifier = "TrackedAutoMeaterData";
            data.active = trackedAutoMeater.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedAutoMeaterByAutoMeater.Add(autoMeaterScript, trackedAutoMeater);
            GameManager.trackedObjectByObject.Add(autoMeaterScript, trackedAutoMeater);
            FVRPhysicalObject[] pos = autoMeaterScript.GetComponentsInChildren<FVRPhysicalObject>();
            for(int i=0; i < pos.Length; ++i)
            {
                GameManager.trackedObjectByInteractive.Add(pos[i], trackedAutoMeater);
            }
            AutoMeaterHitZone[] hitZones = autoMeaterScript.GetComponentsInChildren<AutoMeaterHitZone>();
            for(int i = 0; i < hitZones.Length; ++i)
            {
                GameManager.trackedObjectByDamageable.Add(hitZones[i], trackedAutoMeater);
            }

            data.position = autoMeaterScript.transform.position;
            data.rotation = autoMeaterScript.transform.rotation;
            data.IFF = (byte)autoMeaterScript.E.IFFCode;
            if (autoMeaterScript.name.Contains("SMG"))
            {
                data.ID = 0;
            }
            else if (autoMeaterScript.name.Contains("Flak"))
            {
                data.ID = 1;
            }
            else if (autoMeaterScript.name.Contains("Flamethrower"))
            {
                data.ID = 2;
            }
            else if (autoMeaterScript.name.Contains("Machinegun") || autoMeaterScript.name.Contains("MachineGun"))
            {
                data.ID = 3;
            }
            else if (autoMeaterScript.name.Contains("Suppresion") || autoMeaterScript.name.Contains("Suppression"))
            {
                data.ID = 4;
            }
            else if (autoMeaterScript.name.Contains("Blue"))
            {
                data.ID = 5;
            }
            else if (autoMeaterScript.name.Contains("Red"))
            {
                data.ID = 6;
            }
            else
            {
                Mod.LogWarning("Unsupported AutoMeater type tracked");
                data.ID = 7;
            }

            if (autoMeaterScript.m_hasMotor)
            {
                data.motorHingeSpringTarget = autoMeaterScript.Motor.m_hingeJoint.spring.targetPosition;
                data.sideToSideRotation = autoMeaterScript.Motor.m_sideToSideTransform.rotation;
                if (autoMeaterScript.m_usesUpDownTransform)
                {
                    if (autoMeaterScript.Motor.usesUpDownHinger)
                    {
                        data.motorUpDownTarget = autoMeaterScript.Motor.m_upDownJoint.spring.targetPosition;
                    }
                    data.upDownMotorRotation = autoMeaterScript.Motor.m_upAndDownMotor.rotation;
                }
            }

            // Get hitzones
            AutoMeaterHitZone[] hitZoneArr = trackedAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
            foreach (AutoMeaterHitZone hitZone in hitZoneArr)
            {
                data.hitZones.Add(hitZone.Type, hitZone);
            }

            data.CollectExternalData();

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedAutoMeater.awoken)
            {
                trackedAutoMeater.data.Update(true);
            }

            return trackedAutoMeater;
        }

        private void CollectExternalData()
        {
            data = new byte[4];

            // Write TNH context
            data[0] = TNH_HoldPointPatch.inSpawnTurrets ? (byte)1 : (byte)1;
            data[1] = TNH_SupplyPointPatch.inSpawnDefenses ? (byte)1 : (byte)1;
            BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(data, 2);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating AutoMeater " + trackedID, false);
            string itemID = AutoMeaterIDToItemID(ID);
            yield return IM.OD[itemID].GetGameObjectAsync();
            GameObject autoMeaterPrefab = IM.OD[itemID].GetGameObject();
            if (autoMeaterPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate AutoMeater sent from {controller} but failed to get prefab.");
                awaitingInstantiation = false;
                yield break;
            }

            if (!awaitingInstantiation)
            {
                yield break;
            }

            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at automeater instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
            GameObject autoMeaterInstance = GameObject.Instantiate(autoMeaterPrefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalAutoMeater = autoMeaterInstance.AddComponent<TrackedAutoMeater>();
            physical = physicalAutoMeater;
            physicalAutoMeater.physicalAutoMeater = autoMeaterInstance.GetComponent<AutoMeater>();
            physical.physical = physicalAutoMeater.physicalAutoMeater;
            awaitingInstantiation = false;
            physicalAutoMeater.autoMeaterData = this;
            physical.data = this;

            GameManager.trackedAutoMeaterByAutoMeater.Add(physicalAutoMeater.physicalAutoMeater, physicalAutoMeater);
            GameManager.trackedObjectByObject.Add(physicalAutoMeater.physicalAutoMeater, physicalAutoMeater);
            FVRPhysicalObject[] pos = physicalAutoMeater.physicalAutoMeater.GetComponentsInChildren<FVRPhysicalObject>();
            for (int i = 0; i < pos.Length; ++i)
            {
                GameManager.trackedObjectByInteractive.Add(pos[i], physicalAutoMeater);
            }
            AutoMeaterHitZone[] tempHitZones = physicalAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
            for (int i = 0; i < tempHitZones.Length; ++i)
            {
                GameManager.trackedObjectByDamageable.Add(tempHitZones[i], physicalAutoMeater);
            }

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                }
                Rigidbody[] rbs = physicalAutoMeater.GetComponentsInChildren<Rigidbody>();
                for(int i=0; i < rbs.Length; ++i)
                {
                    rbs[i].isKinematic = true;
                }
            }

            // Initially set IFF
            physicalAutoMeater.physicalAutoMeater.E.IFFCode = IFF;

            // Get hitzones
            AutoMeaterHitZone[] hitZoneArr = physicalAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
            foreach(AutoMeaterHitZone hitZone in hitZoneArr)
            {
                hitZones.Add(hitZone.Type, hitZone);
            }

            ProcessData();

            // Initially set itself
            UpdateFromData(this);
        }

        private void ProcessData()
        {
            if (GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                if (data[0] == 1) // TNH_HoldPoint is in spawn turrets
                {
                    GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_activeTurrets.Add(physicalAutoMeater.physicalAutoMeater);
                }
                else if (data[1] == 1) // TNH_SupplyPoint is in Spawn Take Enemy Group
                {
                    GM.TNH_Manager.SupplyPoints[BitConverter.ToInt16(data, 2)].m_activeTurrets.Add(physicalAutoMeater.physicalAutoMeater);
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
                    Mod.LogInfo("AutoMeaterIDToItemID: Invalid auto meater ID: " + ID, false);
                    return "Turburgert_SMG";
            }
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedAutoMeaterData updatedAutoMeater = updatedObject as TrackedAutoMeaterData;

            if (full)
            {
                ID = updatedAutoMeater.ID;
                data = updatedAutoMeater.data;
            }

            // Set data
            previousPos = position;
            previousRot = rotation;
            position = updatedAutoMeater.position;
            rotation = updatedAutoMeater.rotation;
            previousIFF = IFF;
            IFF = updatedAutoMeater.IFF;
            previousMotorHingeSpringTarget = motorHingeSpringTarget;
            motorHingeSpringTarget = updatedAutoMeater.motorHingeSpringTarget;
            previousSideToSideRotation = sideToSideRotation;
            sideToSideRotation = updatedAutoMeater.sideToSideRotation;
            previousMotorUpDownTarget = motorUpDownTarget;
            motorUpDownTarget = updatedAutoMeater.motorUpDownTarget;
            previousUpDownMotorRotation = upDownMotorRotation;
            upDownMotorRotation = updatedAutoMeater.upDownMotorRotation;

            // Set physically
            if (physicalAutoMeater != null)
            {
                physicalAutoMeater.physicalAutoMeater.transform.position = position;
                physicalAutoMeater.physicalAutoMeater.transform.rotation = rotation;
                physicalAutoMeater.physicalAutoMeater.E.IFFCode = IFF;
                if (physicalAutoMeater.physicalAutoMeater.m_hasMotor)
                {
                    JointSpring spring = physicalAutoMeater.physicalAutoMeater.Motor.m_hingeJoint.spring;
                    spring.targetPosition = motorHingeSpringTarget;
                    physicalAutoMeater.physicalAutoMeater.Motor.m_hingeJoint.spring = spring;
                    physicalAutoMeater.physicalAutoMeater.Motor.m_sideToSideTransform.rotation = sideToSideRotation;
                    if (physicalAutoMeater.physicalAutoMeater.m_usesUpDownTransform)
                    {
                        if (physicalAutoMeater.physicalAutoMeater.Motor.usesUpDownHinger)
                        {
                            JointSpring spring0 = physicalAutoMeater.physicalAutoMeater.Motor.m_upDownJoint.spring;
                            spring0.targetPosition = motorUpDownTarget;
                            physicalAutoMeater.physicalAutoMeater.Motor.m_upDownJoint.spring = spring0;
                        }
                        physicalAutoMeater.physicalAutoMeater.Motor.m_upAndDownMotor.rotation = upDownMotorRotation;
                    }
                }
            }
        }

        // TODO: Review: If full updates are ever actually used, should they? Or should be ever only use ObjectUpdate packets
        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            previousRot = rotation;
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            previousIFF = IFF;
            IFF = packet.ReadByte();
            previousMotorHingeSpringTarget = motorHingeSpringTarget;
            motorHingeSpringTarget = packet.ReadFloat();
            previousSideToSideRotation = sideToSideRotation;
            sideToSideRotation = packet.ReadQuaternion();
            previousMotorUpDownTarget = motorUpDownTarget;
            motorUpDownTarget = packet.ReadFloat();
            previousUpDownMotorRotation = upDownMotorRotation;
            upDownMotorRotation = packet.ReadQuaternion();

            if (full)
            {
                ID = packet.ReadByte();
                int dataLen = packet.ReadInt();
                if (dataLen > 0)
                {
                    data = packet.ReadBytes(dataLen);
                }
                else
                {
                    data = null;
                }
            }

            // Set physically
            if (physicalAutoMeater != null)
            {
                physicalAutoMeater.physicalAutoMeater.transform.position = position;
                physicalAutoMeater.physicalAutoMeater.transform.rotation = rotation;
                physicalAutoMeater.physicalAutoMeater.E.IFFCode = IFF;
                if (physicalAutoMeater.physicalAutoMeater.m_hasMotor)
                {
                    JointSpring spring = physicalAutoMeater.physicalAutoMeater.Motor.m_hingeJoint.spring;
                    spring.targetPosition = motorHingeSpringTarget;
                    physicalAutoMeater.physicalAutoMeater.Motor.m_hingeJoint.spring = spring;
                    physicalAutoMeater.physicalAutoMeater.Motor.m_sideToSideTransform.rotation = sideToSideRotation;
                    if (physicalAutoMeater.physicalAutoMeater.m_usesUpDownTransform)
                    {
                        if (physicalAutoMeater.physicalAutoMeater.Motor.usesUpDownHinger)
                        {
                            JointSpring spring0 = physicalAutoMeater.physicalAutoMeater.Motor.m_upDownJoint.spring;
                            spring0.targetPosition = motorUpDownTarget;
                            physicalAutoMeater.physicalAutoMeater.Motor.m_upDownJoint.spring = spring0;
                        }
                        physicalAutoMeater.physicalAutoMeater.Motor.m_upAndDownMotor.rotation = upDownMotorRotation;
                    }
                }
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physicalAutoMeater == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalAutoMeater.physicalAutoMeater.transform.position;
            rotation = physicalAutoMeater.physicalAutoMeater.transform.rotation;

            previousIFF = IFF;
            IFF = (byte)physicalAutoMeater.physicalAutoMeater.E.IFFCode;

            if (physicalAutoMeater.physicalAutoMeater.m_hasMotor)
            {
                previousMotorHingeSpringTarget = motorHingeSpringTarget;
                motorHingeSpringTarget = physicalAutoMeater.physicalAutoMeater.Motor.m_hingeJoint.spring.targetPosition;
                previousSideToSideRotation = sideToSideRotation;
                sideToSideRotation = physicalAutoMeater.physicalAutoMeater.Motor.m_sideToSideTransform.rotation;
                if (physicalAutoMeater.physicalAutoMeater.m_usesUpDownTransform)
                {
                    if (physicalAutoMeater.physicalAutoMeater.Motor.usesUpDownHinger)
                    {
                        previousMotorUpDownTarget = motorUpDownTarget;
                        motorUpDownTarget = physicalAutoMeater.physicalAutoMeater.Motor.m_upDownJoint.spring.targetPosition;
                    }
                    previousUpDownMotorRotation = upDownMotorRotation;
                    upDownMotorRotation = physicalAutoMeater.physicalAutoMeater.Motor.m_upAndDownMotor.rotation;
                }
            }

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousMotorHingeSpringTarget != motorHingeSpringTarget 
                   || !previousSideToSideRotation.Equals(sideToSideRotation)|| previousMotorUpDownTarget != motorUpDownTarget 
                   || !previousUpDownMotorRotation.Equals(upDownMotorRotation);
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
                if (physicalAutoMeater != null && physicalAutoMeater.physicalAutoMeater != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                    }
                    Rigidbody[] rbs = physicalAutoMeater.GetComponentsInChildren<Rigidbody>();
                    for (int i = 0; i < rbs.Length; ++i)
                    {
                        rbs[i].isKinematic = false;
                    }
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physicalAutoMeater != null && physicalAutoMeater.physicalAutoMeater != null) 
                {
                    physicalAutoMeater.EnsureUncontrolled();

                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                    }
                    Rigidbody[] rbs = physicalAutoMeater.GetComponentsInChildren<Rigidbody>();
                    for (int i = 0; i < rbs.Length; ++i)
                    {
                        rbs[i].isKinematic = true;
                    }
                }
            }
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(IFF);
            packet.Write(motorHingeSpringTarget);
            packet.Write(sideToSideRotation);
            packet.Write(motorUpDownTarget);
            packet.Write(upDownMotorRotation);

            if (full)
            {
                packet.Write(ID);
                if (data == null || data.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(data.Length);
                    packet.Write(data);
                }
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalAutoMeater != null && physicalAutoMeater.physicalAutoMeater != null)
                {
                    GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeater.physicalAutoMeater);
                    FVRPhysicalObject[] pos = physicalAutoMeater.GetComponentsInChildren<FVRPhysicalObject>();
                    for (int i = 0; i < pos.Length; ++i)
                    {
                        GameManager.trackedObjectByInteractive.Remove(pos[i]);
                    }
                    AutoMeaterHitZone[] tempHitZones = physicalAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
                    for (int i = 0; i < tempHitZones.Length; ++i)
                    {
                        GameManager.trackedObjectByDamageable.Remove(tempHitZones[i]);
                    }
                }
            }
        }
    }
}
