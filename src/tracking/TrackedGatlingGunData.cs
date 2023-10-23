using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedGatlingGunData : TrackedObjectData
    {
        public TrackedGatlingGun physicalGatlingGun;

        public static bool firstInScene;
        public static List<wwGatlingGun> sceneGatlingGuns = new List<wwGatlingGun>();

        public int index;

        public Quaternion controlBracketRotation;
        public Quaternion previousControlBracketRotation;
        public Quaternion controlRotation;
        public Quaternion previousControlRotation;
        public Vector3 crankRotation;
        public Vector3 previousCrankRotation;
        public Quaternion baseRotation;
        public Quaternion previousBaseRotation;

        public wwGatlingControlHandle controlHandle;
        public wwGatlingGunHandle crankHandle;
        public wwGatlingGunBaseHandle baseHandle;

        public TrackedGatlingGunData()
        {

        }

        public TrackedGatlingGunData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            controlBracketRotation = packet.ReadQuaternion();
            controlRotation = packet.ReadQuaternion();
            crankRotation = packet.ReadVector3();
            baseRotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<wwGatlingGun>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            FVRInteractiveObject handle = root.GetComponentInChildren<wwGatlingControlHandle>();
            if(handle != null && handle.m_hand != null)
            {
                return true;
            }
            handle = root.GetComponentInChildren<wwGatlingGunHandle>();
            if(handle != null && handle.m_hand != null)
            {
                return true;
            }
            handle = root.GetComponentInChildren<wwGatlingGunBaseHandle>();
            if(handle != null && handle.m_hand != null)
            {
                return true;
            }
            return false;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneGatlingGuns.Clear();
                firstInScene = false;
            }
            sceneGatlingGuns.Add(t.GetComponent<wwGatlingGun>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedGatlingGun MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedGatlingGun trackedGatlingGun = root.gameObject.AddComponent<TrackedGatlingGun>();
            TrackedGatlingGunData data = new TrackedGatlingGunData();
            trackedGatlingGun.data = data;
            trackedGatlingGun.gatlingGunData = data;
            data.physicalGatlingGun = trackedGatlingGun;
            data.physical = trackedGatlingGun;
            data.physicalGatlingGun.physicalGatlingGun = root.GetComponent<wwGatlingGun>();
            data.physical.physical = data.physicalGatlingGun.physicalGatlingGun;

            data.typeIdentifier = "TrackedGatlingGunData";
            data.active = trackedGatlingGun.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneGatlingGuns.Clear();
                firstInScene = false;
            }
            data.index = sceneGatlingGuns.Count;
            sceneGatlingGuns.Add(data.physicalGatlingGun.physicalGatlingGun);

            // Get references to related scripts
            data.controlHandle = data.physicalGatlingGun.GetComponentInChildren<wwGatlingControlHandle>();
            data.crankHandle = data.physicalGatlingGun.GetComponentInChildren<wwGatlingGunHandle>();
            data.baseHandle = data.physicalGatlingGun.GetComponentInChildren<wwGatlingGunBaseHandle>();

            GameManager.trackedGatlingGunByGatlingGun.Add(data.physicalGatlingGun.physicalGatlingGun, trackedGatlingGun);
            GameManager.trackedObjectByObject.Add(data.physicalGatlingGun.physicalGatlingGun, trackedGatlingGun);
            GameManager.trackedObjectByInteractive.Add(data.controlHandle, trackedGatlingGun);
            GameManager.trackedObjectByInteractive.Add(data.crankHandle, trackedGatlingGun);
            GameManager.trackedObjectByInteractive.Add(data.baseHandle, trackedGatlingGun);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedGatlingGun.awoken)
            {
                trackedGatlingGun.data.Update(true);
            }

            return trackedGatlingGun;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating gatling gun");
            // Get instance
            wwGatlingGun physicalGatlingGunScript = null;
            if (sceneGatlingGuns.Count > index)
            {
                physicalGatlingGunScript = sceneGatlingGuns[index];
                if(physicalGatlingGunScript == null)
                {
                    Mod.LogError("Attempted to instantiate wwGatlingGun "+index+" sent from "+controller+" but list at index is null.");
                    yield break;
                }
            }
            else
            {
                Mod.LogError("Attempted to instantiate wwGatlingGun " + index + " sent from " + controller + " but index does not fit in scene gatling gun list.");
                yield break;
            }
            GameObject gatlingGunInstance = physicalGatlingGunScript.gameObject;

            physicalGatlingGun = gatlingGunInstance.AddComponent<TrackedGatlingGun>();
            physical = physicalGatlingGun;
            physicalGatlingGun.physicalGatlingGun = physicalGatlingGunScript;
            physical.physical = physicalGatlingGunScript;
            awaitingInstantiation = false;
            physicalGatlingGun.gatlingGunData = this;
            physical.data = this;

            // Get references to related scripts
            controlHandle = physicalGatlingGun.GetComponentInChildren<wwGatlingControlHandle>();
            crankHandle = physicalGatlingGun.GetComponentInChildren<wwGatlingGunHandle>();
            baseHandle = physicalGatlingGun.GetComponentInChildren<wwGatlingGunBaseHandle>();

            GameManager.trackedGatlingGunByGatlingGun.Add(physicalGatlingGun.physicalGatlingGun, physicalGatlingGun);
            GameManager.trackedObjectByObject.Add(physicalGatlingGun.physicalGatlingGun, physicalGatlingGun);
            GameManager.trackedObjectByInteractive.Add(controlHandle, physicalGatlingGun);
            GameManager.trackedObjectByInteractive.Add(crankHandle, physicalGatlingGun);
            GameManager.trackedObjectByInteractive.Add(baseHandle, physicalGatlingGun);

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedGatlingGunData updatedGatlingGun = updatedObject as TrackedGatlingGunData;

            previousControlBracketRotation = controlBracketRotation;
            controlBracketRotation = updatedGatlingGun.controlBracketRotation;
            previousControlRotation = controlRotation;
            controlRotation = updatedGatlingGun.controlRotation;
            previousCrankRotation = crankRotation;
            crankRotation = updatedGatlingGun.crankRotation;
            previousBaseRotation = baseRotation;
            baseRotation = updatedGatlingGun.baseRotation;

            if (physicalGatlingGun != null)
            {
                controlHandle.MountingBracket.rotation = controlBracketRotation;
                controlHandle.transform.rotation = controlRotation;
                crankHandle.transform.rotation = Quaternion.LookRotation(crankRotation, crankHandle.YUpTarget.up);
                baseHandle.GunBase.transform.rotation = baseRotation;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousControlBracketRotation = controlBracketRotation;
            controlBracketRotation = packet.ReadQuaternion();
            previousControlRotation = controlRotation;
            controlRotation = packet.ReadQuaternion();
            previousCrankRotation = crankRotation;
            crankRotation = packet.ReadVector3();
            previousBaseRotation = baseRotation;
            baseRotation = packet.ReadQuaternion();

            if (physicalGatlingGun != null)
            {
                controlHandle.MountingBracket.rotation = controlBracketRotation;
                controlHandle.transform.rotation = controlRotation;
                crankHandle.transform.rotation = Quaternion.LookRotation(crankRotation, crankHandle.YUpTarget.up);
                baseHandle.GunBase.transform.rotation = baseRotation;
            }
        }

        public override bool Update(bool full = false)
        {
            base.Update(full);

            if (physical == null)
            {
                return false;
            }

            previousControlBracketRotation = controlBracketRotation;
            controlBracketRotation = controlHandle.MountingBracket.rotation;
            previousControlRotation = controlRotation;
            controlRotation = controlHandle.transform.rotation;
            previousCrankRotation = crankRotation;
            crankRotation = crankHandle.m_curCrankDir;
            previousBaseRotation = baseRotation;
            baseRotation = baseHandle.GunBase.transform.rotation;

            return NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || !previousControlBracketRotation.Equals(controlBracketRotation) || !previousControlRotation.Equals(controlRotation) 
                   || !previousCrankRotation.Equals(crankRotation) || !previousBaseRotation.Equals(baseRotation);
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(controlBracketRotation);
            packet.Write(controlRotation);
            packet.Write(crankRotation);
            packet.Write(baseRotation);
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalGatlingGun != null && physicalGatlingGun.physicalGatlingGun != null)
                {
                    GameManager.trackedGatlingGunByGatlingGun.Remove(physicalGatlingGun.physicalGatlingGun);
                    GameManager.trackedObjectByInteractive.Remove(controlHandle);
                    GameManager.trackedObjectByInteractive.Remove(crankHandle);
                    GameManager.trackedObjectByInteractive.Remove(baseHandle);
                }
            }
        }
    }
}
