using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSentinelData : TrackedObjectData
    {
        public TrackedSentinel physicalSentinel;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;

        public TrackedSentinelData()
        {

        }

        public TrackedSentinelData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Sentinel>() != null;
        }

        private static TrackedSentinel MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedSentinel trackedSentinel = root.gameObject.AddComponent<TrackedSentinel>();
            TrackedSentinelData data = new TrackedSentinelData();
            trackedSentinel.sentinelData = data;
            trackedSentinel.data = data;
            data.physicalSentinel = trackedSentinel;
            data.physical = trackedSentinel;
            Construct_Sentinel sentinelScript = root.GetComponent<Construct_Sentinel>();
            data.physicalSentinel.physicalSentinel = sentinelScript;
            data.physical.physical = sentinelScript;

            data.typeIdentifier = "TrackedSentinelData";
            data.active = trackedSentinel.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedSentinelBySentinel.Add(trackedSentinel.physicalSentinel, trackedSentinel);
            GameManager.trackedObjectByObject.Add(trackedSentinel.physicalSentinel, trackedSentinel);
            GameManager.trackedObjectByShatterable.Add(trackedSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>(), trackedSentinel);
            for (int i = 0; i < trackedSentinel.physicalSentinel.Plates.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Add(trackedSentinel.physicalSentinel.Plates[i], trackedSentinel);
            }

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedSentinel.awoken)
            {
                trackedSentinel.data.Update(true);
            }

            Mod.LogInfo("Made sentinel " + trackedSentinel.name + " tracked", false);

            return trackedSentinel;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating sentinel at " + trackedID, false);
            GameObject prefab = null;
            Construct_Sentinel_Path sentinelVolume = GameObject.FindObjectOfType<Construct_Sentinel_Path>();
            if (sentinelVolume == null)
            {
                Mod.LogError("Failed to instantiate sentinel: " + trackedID + ": Could not find suitable sentinel path to get prefab from");
                yield break;
            }
            else
            {
                prefab = sentinelVolume.SentinelPrefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject sentinelInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalSentinel = sentinelInstance.AddComponent<TrackedSentinel>();
            physical = physicalSentinel;
            physicalSentinel.physicalSentinel = sentinelInstance.GetComponent<Construct_Sentinel>();
            physical.physical = physicalSentinel.physicalSentinel;
            awaitingInstantiation = false;
            physicalSentinel.sentinelData = this;
            physicalSentinel.data = this;

            GameManager.trackedSentinelBySentinel.Add(physicalSentinel.physicalSentinel, physicalSentinel);
            GameManager.trackedObjectByObject.Add(physicalSentinel.physicalSentinel, physicalSentinel);
            GameManager.trackedObjectByShatterable.Add(physicalSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>(), physicalSentinel);
            for (int i = 0; i < physicalSentinel.physicalSentinel.Plates.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Add(physicalSentinel.physicalSentinel.Plates[i], physicalSentinel);
            }

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedSentinelData updatedSentinel = updatedObject as TrackedSentinelData;

            previousPos = position;
            position = updatedSentinel.position;
            previousRot = rotation;
            rotation = updatedSentinel.rotation;

            // Set physically
            if (physicalSentinel != null)
            {
                physicalSentinel.physicalSentinel.transform.position = position;
                physicalSentinel.physicalSentinel.transform.rotation = rotation;
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
            if (physicalSentinel != null)
            {
                physicalSentinel.physicalSentinel.transform.position = position;
                physicalSentinel.physicalSentinel.transform.rotation = rotation;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalSentinel == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalSentinel.physicalSentinel.transform.position;
            rotation = physicalSentinel.physicalSentinel.transform.rotation;

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalSentinel != null && physicalSentinel.physicalSentinel != null)
                {
                    GameManager.trackedSentinelBySentinel.Remove(physicalSentinel.physicalSentinel);
                    GameManager.trackedObjectByShatterable.Remove(physicalSentinel.physicalSentinel.GetComponentInChildren<UberShatterable>());
                    for (int i = 0; i < physicalSentinel.physicalSentinel.Plates.Count; ++i)
                    {
                        GameManager.trackedObjectByShatterable.Remove(physicalSentinel.physicalSentinel.Plates[i]);
                    }
                }
            }
        }
    }
}
