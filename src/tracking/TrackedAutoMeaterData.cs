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
        public Quaternion previousSideToSideRotation;
        public Quaternion sideToSideRotation;
        public float previousHingeTargetPos;
        public float hingeTargetPos;
        public Quaternion previousUpDownMotorRotation;
        public Quaternion upDownMotorRotation;
        public float previousUpDownJointTargetPos;
        public float upDownJointTargetPos;
        public byte previousIFF;
        public byte IFF;
        public byte[] data;

        public Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone> hitZones = new Dictionary<AutoMeater.AMHitZoneType, AutoMeaterHitZone>();

        public TrackedAutoMeaterData()
        {

        }

        public TrackedAutoMeaterData(Packet packet) : base(packet)
        {
            // Full
            ID = packet.ReadByte();
            int dataLen = packet.ReadInt();
            if (dataLen > 0)
            {
                data = packet.ReadBytes(dataLen);
            }

            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            IFF = packet.ReadByte();
            sideToSideRotation = packet.ReadQuaternion();
            hingeTargetPos = packet.ReadFloat();
            upDownMotorRotation = packet.ReadQuaternion();
            upDownJointTargetPos = packet.ReadFloat();
        }

        public static bool IfOfType(Transform t)
        {
            return t.GetComponent<AutoMeater>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            return root.GetComponent<AutoMeater>().PO.m_hand != null;
        }

        public override bool IsControlled()
        {
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

            GameManager.trackedAutoMeaterByAutoMeater.Add(autoMeaterScript, trackedAutoMeater);
            GameManager.trackedObjectByObject.Add(autoMeaterScript, trackedAutoMeater);
            GameManager.trackedObjectByInteractive.Add(autoMeaterScript.PO, trackedAutoMeater);

            data.position = autoMeaterScript.RB.position;
            data.rotation = autoMeaterScript.RB.rotation;
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
            data.sideToSideRotation = autoMeaterScript.SideToSideTransform.localRotation;
            data.hingeTargetPos = autoMeaterScript.SideToSideHinge.spring.targetPosition;
            data.upDownMotorRotation = autoMeaterScript.UpDownTransform.localRotation;
            data.upDownJointTargetPos = autoMeaterScript.UpDownHinge.spring.targetPosition;

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
            physical.data = this;

            GameManager.trackedAutoMeaterByAutoMeater.Add(physicalAutoMeater.physicalAutoMeater, physicalAutoMeater);
            GameManager.trackedObjectByObject.Add(physicalAutoMeater.physicalAutoMeater, physicalAutoMeater);
            GameManager.trackedObjectByInteractive.Add(physicalAutoMeater.physicalAutoMeater.PO, physicalAutoMeater);

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                }
                physicalAutoMeater.physicalAutoMeater.RB.isKinematic = true;
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
            previousSideToSideRotation = sideToSideRotation;
            sideToSideRotation = updatedAutoMeater.sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            hingeTargetPos = updatedAutoMeater.hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            upDownMotorRotation = updatedAutoMeater.upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            upDownJointTargetPos = updatedAutoMeater.upDownJointTargetPos;

            // Set physically
            if (physicalAutoMeater != null)
            {
                physicalAutoMeater.physicalAutoMeater.RB.position = position;
                physicalAutoMeater.physicalAutoMeater.RB.rotation = rotation;
                physicalAutoMeater.physicalAutoMeater.E.IFFCode = IFF;
                physicalAutoMeater.physicalAutoMeater.SideToSideTransform.localRotation = sideToSideRotation;
                HingeJoint hingeJoint = physicalAutoMeater.physicalAutoMeater.SideToSideHinge;
                JointSpring spring = hingeJoint.spring;
                spring.targetPosition = hingeTargetPos;
                hingeJoint.spring = spring;
                physicalAutoMeater.physicalAutoMeater.UpDownTransform.localRotation = upDownMotorRotation;
                HingeJoint upDownHingeJoint = physicalAutoMeater.physicalAutoMeater.UpDownHinge;
                spring = upDownHingeJoint.spring;
                spring.targetPosition = upDownJointTargetPos;
                upDownHingeJoint.spring = spring;
            }
        }

        // TODO: Review: If full updates are ever actually used, should they? Or should be ever only use ObjectUpdate packets
        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

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

            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            IFF = packet.ReadByte();
            sideToSideRotation = packet.ReadQuaternion();
            hingeTargetPos = packet.ReadFloat();
            upDownMotorRotation = packet.ReadQuaternion();
            upDownJointTargetPos = packet.ReadFloat();

            // Set physically
            if (physicalAutoMeater != null)
            {
                physicalAutoMeater.physicalAutoMeater.RB.position = position;
                physicalAutoMeater.physicalAutoMeater.RB.rotation = rotation;
                physicalAutoMeater.physicalAutoMeater.E.IFFCode = IFF;
                physicalAutoMeater.physicalAutoMeater.SideToSideTransform.localRotation = sideToSideRotation;
                HingeJoint hingeJoint = physicalAutoMeater.physicalAutoMeater.SideToSideHinge;
                JointSpring spring = hingeJoint.spring;
                spring.targetPosition = hingeTargetPos;
                hingeJoint.spring = spring;
                physicalAutoMeater.physicalAutoMeater.UpDownTransform.localRotation = upDownMotorRotation;
                HingeJoint upDownHingeJoint = physicalAutoMeater.physicalAutoMeater.UpDownHinge;
                spring = upDownHingeJoint.spring;
                spring.targetPosition = upDownJointTargetPos;
                upDownHingeJoint.spring = spring;
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
            position = physicalAutoMeater.physicalAutoMeater.RB.position;
            rotation = physicalAutoMeater.physicalAutoMeater.RB.rotation;

            previousIFF = IFF;
            IFF = (byte)physicalAutoMeater.physicalAutoMeater.E.IFFCode;

            previousSideToSideRotation = sideToSideRotation;
            previousHingeTargetPos = hingeTargetPos;
            previousUpDownMotorRotation = upDownMotorRotation;
            previousUpDownJointTargetPos = upDownJointTargetPos;
            sideToSideRotation = physicalAutoMeater.physicalAutoMeater.SideToSideTransform.localRotation;
            hingeTargetPos = physicalAutoMeater.physicalAutoMeater.SideToSideHinge.spring.targetPosition;
            upDownMotorRotation = physicalAutoMeater.physicalAutoMeater.UpDownTransform.localRotation;
            upDownJointTargetPos = physicalAutoMeater.physicalAutoMeater.UpDownHinge.spring.targetPosition;

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || !previousPos.Equals(position) || !previousRot.Equals(rotation) || !previousSideToSideRotation.Equals(sideToSideRotation) ||
                   !previousUpDownMotorRotation.Equals(upDownMotorRotation) || previousHingeTargetPos != hingeTargetPos || previousUpDownJointTargetPos != upDownJointTargetPos;
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
                if (physicalAutoMeater != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                    }
                    physicalAutoMeater.physicalAutoMeater.RB.isKinematic = false;
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physical != null)
                {
                    physicalAutoMeater.EnsureUncontrolled();

                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity(physicalAutoMeater.physicalAutoMeater.E);
                    }
                    physicalAutoMeater.physicalAutoMeater.RB.isKinematic = true;
                }
            }
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            if (full)
            {
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

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(IFF);
            packet.Write(sideToSideRotation);
            packet.Write(hingeTargetPos);
            packet.Write(upDownMotorRotation);
            packet.Write(upDownJointTargetPos);
        }
    }
}
