using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedIrisData : TrackedObjectData
    {
        public TrackedIris physicalIris;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;

        public TrackedIrisData()
        {

        }

        public TrackedIrisData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Iris>() != null;
        }

        private static TrackedIris MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedIris trackedIris = root.gameObject.AddComponent<TrackedIris>();
            TrackedIrisData data = new TrackedIrisData();
            trackedIris.irisData = data;
            trackedIris.data = data;
            data.physicalIris = trackedIris;
            data.physical = trackedIris;
            Construct_Iris irisScript = root.GetComponent<Construct_Iris>();
            data.physicalIris.physicalIris = irisScript;
            data.physical.physical = irisScript;

            data.typeIdentifier = "TrackedIrisData";
            data.active = trackedIris.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedIrisByIris.Add(trackedIris.physicalIris, trackedIris);
            GameManager.trackedObjectByObject.Add(trackedIris.physicalIris, trackedIris);
            GameManager.trackedObjectByDamageable.Add(trackedIris.physicalIris, trackedIris);
            GameManager.trackedObjectByDamageable.Add(trackedIris.GetComponentInChildren<Construct_Iris_Core>(), trackedIris);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedIris.awoken)
            {
                trackedIris.data.Update(true);
            }

            Mod.LogInfo("Made iris " + trackedIris.name + " tracked", false);

            return trackedIris;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating iris at " + trackedID, false);
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject prefab = null;
            Construct_Iris_Volume irisVolume = GameObject.FindObjectOfType<Construct_Iris_Volume>();
            if (irisVolume == null)
            {
                Mod.LogError("Failed to instantiate iris: " + trackedID + ": Could not find suitable iris volume to get prefab from");
                yield break;
            }
            else
            {
                prefab = irisVolume.Iris_Prefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject irisInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalIris = irisInstance.AddComponent<TrackedIris>();
            physical = physicalIris;
            physicalIris.physicalIris = irisInstance.GetComponent<Construct_Iris>();
            physical.physical = physicalIris.physicalIris;
            awaitingInstantiation = false;
            physicalIris.irisData = this;
            physicalIris.data = this;

            GameManager.trackedIrisByIris.Add(physicalIris.physicalIris, physicalIris);
            GameManager.trackedObjectByObject.Add(physicalIris.physicalIris, physicalIris);
            GameManager.trackedObjectByDamageable.Add(physicalIris.physicalIris, physicalIris);
            GameManager.trackedObjectByDamageable.Add(physicalIris.GetComponentInChildren<Construct_Iris_Core>(), physicalIris);

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedIrisData updatedIris = updatedObject as TrackedIrisData;

            previousPos = position;
            position = updatedIris.position;
            previousRot = rotation;
            rotation = updatedIris.rotation;

            // Set physically
            if (physicalIris != null)
            {
                physicalIris.physicalIris.transform.position = position;
                physicalIris.physicalIris.transform.rotation = rotation;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            position = packet.ReadVector3();
            previousRot = rotation;
            rotation = packet.ReadQuaternion();

            // Set physically
            if (physicalIris != null)
            {
                physicalIris.physicalIris.transform.position = position;
                physicalIris.physicalIris.transform.rotation = rotation;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalIris == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalIris.physicalIris.transform.position;
            rotation = physicalIris.physicalIris.transform.rotation;

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (localTrackedID != -1 && TrackedIris.unknownIrisBeginExploding.Contains(localWaitingIndex))
            {
                ClientSend.IrisBeginExploding(trackedID, true);

                TrackedIris.unknownIrisBeginExploding.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedIris.unknownIrisExplode.Contains(localWaitingIndex))
            {
                ClientSend.IrisExplode(trackedID);

                TrackedIris.unknownIrisExplode.Remove(localWaitingIndex);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalIris != null && physicalIris.physicalIris != null)
                {
                    GameManager.trackedIrisByIris.Remove(physicalIris.physicalIris);
                    GameManager.trackedObjectByDamageable.Remove(physicalIris.physicalIris);
                    GameManager.trackedObjectByDamageable.Remove(physicalIris.GetComponentInChildren<Construct_Iris_Core>());
                }
            }
        }
    }
}
