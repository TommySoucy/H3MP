using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBlisterData : TrackedObjectData
    {
        public TrackedBlister physicalBlister;

        public Vector3 position;
        public Quaternion rotation;

        public TrackedBlisterData()
        {

        }

        public TrackedBlisterData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update

            // Full
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Blister>() != null;
        }

        private static TrackedBlister MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedBlister trackedBlister = root.gameObject.AddComponent<TrackedBlister>();
            TrackedBlisterData data = new TrackedBlisterData();
            trackedBlister.blisterData = data;
            trackedBlister.data = data;
            data.physicalBlister = trackedBlister;
            data.physical = trackedBlister;
            Construct_Blister blisterScript = root.GetComponent<Construct_Blister>();
            data.physicalBlister.physicalBlister = blisterScript;
            data.physical.physical = blisterScript;

            data.typeIdentifier = "TrackedBlisterData";
            data.active = trackedBlister.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedBlisterByBlister.Add(trackedBlister.physicalBlister, trackedBlister);
            GameManager.trackedObjectByObject.Add(trackedBlister.physicalBlister, trackedBlister);
            GameManager.trackedObjectByShatterable.Add(trackedBlister.GetComponentInChildren<UberShatterable>(), trackedBlister);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            Mod.LogInfo("Made blister " + trackedBlister.name + " tracked", false);

            return trackedBlister;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            if (full)
            {
                packet.Write(position);
                packet.Write(rotation);
            }
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating blister at " + trackedID, false);
            GameObject prefab = null;
            Construct_Blister_Volume blisterVolume = GameObject.FindObjectOfType<Construct_Blister_Volume>();
            if (blisterVolume == null)
            {
                Mod.LogError("Failed to instantiate blister: " + trackedID+": Could not find suitable blister volume to get prefab from");
                yield break;
            }
            else
            {
                prefab = blisterVolume.Blister_Prefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject blisterInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalBlister = blisterInstance.AddComponent<TrackedBlister>();
            physical = physicalBlister;
            physicalBlister.physicalBlister = blisterInstance.GetComponent<Construct_Blister>();
            physical.physical = physicalBlister.physicalBlister;
            awaitingInstantiation = false;
            physicalBlister.blisterData = this;
            physicalBlister.data = this;

            GameManager.trackedBlisterByBlister.Add(physicalBlister.physicalBlister, physicalBlister);
            GameManager.trackedObjectByObject.Add(physicalBlister.physicalBlister, physicalBlister);
            GameManager.trackedObjectByShatterable.Add(physicalBlister.GetComponentInChildren<UberShatterable>(), physicalBlister);

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedBlisterData updatedBlister = updatedObject as TrackedBlisterData;

            if (full)
            {
                position = updatedBlister.position;
                rotation = updatedBlister.rotation;
            }

            // Set physically
            if (physicalBlister != null)
            {
                if (full)
                {
                    physicalBlister.physicalBlister.transform.position = position;
                    physicalBlister.physicalBlister.transform.rotation = rotation;
                }
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            if (full)
            {
                position = packet.ReadVector3();
                rotation = packet.ReadQuaternion();
            }

            // Set physically
            if (physicalBlister != null)
            {
                if (full)
                {
                    physicalBlister.physicalBlister.transform.position = position;
                    physicalBlister.physicalBlister.transform.rotation = rotation;
                }
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalBlister == null)
            {
                return false;
            }

            if (full)
            {
                position = physicalBlister.physicalBlister.transform.position;
                rotation = physicalBlister.physicalBlister.transform.rotation;
                updated = true;
            }

            return updated;
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalBlister != null && physicalBlister.physicalBlister != null)
                {
                    GameManager.trackedBlisterByBlister.Remove(physicalBlister.physicalBlister);
                    GameManager.trackedObjectByShatterable.Remove(physicalBlister.GetComponentInChildren<UberShatterable>());
                }
            }
        }
    }
}
