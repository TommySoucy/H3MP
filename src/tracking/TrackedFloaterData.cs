using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedFloaterData : TrackedObjectData
    {
        public TrackedFloater physicalFloater;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;

        public TrackedFloaterData()
        {

        }

        public TrackedFloaterData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Floater>() != null;
        }

        private static TrackedFloater MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedFloater trackedFloater = root.gameObject.AddComponent<TrackedFloater>();
            TrackedFloaterData data = new TrackedFloaterData();
            trackedFloater.floaterData = data;
            trackedFloater.data = data;
            data.physicalFloater = trackedFloater;
            data.physical = trackedFloater;
            Construct_Floater floaterScript = root.GetComponent<Construct_Floater>();
            data.physicalFloater.physicalFloater = floaterScript;
            data.physical.physical = floaterScript;

            data.typeIdentifier = "TrackedFloaterData";
            data.active = trackedFloater.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedFloaterByFloater.Add(trackedFloater.physicalFloater, trackedFloater);
            GameManager.trackedObjectByObject.Add(trackedFloater.physicalFloater, trackedFloater);
            GameManager.trackedObjectByDamageable.Add(trackedFloater.physicalFloater, trackedFloater);
            GameManager.trackedObjectByDamageable.Add(trackedFloater.GetComponentInChildren<Construct_Floater_Core>(), trackedFloater);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedFloater.awoken)
            {
                trackedFloater.data.Update(true);
            }

            Mod.LogInfo("Made floater " + trackedFloater.name + " tracked", false);

            return trackedFloater;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating floater at " + trackedID, false);
            GameObject prefab = null;
            Construct_Floater_Volume floaterVolume = GameObject.FindObjectOfType<Construct_Floater_Volume>();
            if (floaterVolume == null)
            {
                Mod.LogError("Failed to instantiate floater: " + trackedID + ": Could not find suitable floater volume to get prefab from");
                yield break;
            }
            else
            {
                prefab = floaterVolume.Floater_Prefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject floaterInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalFloater = floaterInstance.AddComponent<TrackedFloater>();
            physical = physicalFloater;
            physicalFloater.physicalFloater = floaterInstance.GetComponent<Construct_Floater>();
            physical.physical = physicalFloater.physicalFloater;
            awaitingInstantiation = false;
            physicalFloater.floaterData = this;
            physicalFloater.data = this;

            GameManager.trackedFloaterByFloater.Add(physicalFloater.physicalFloater, physicalFloater);
            GameManager.trackedObjectByObject.Add(physicalFloater.physicalFloater, physicalFloater);
            GameManager.trackedObjectByDamageable.Add(physicalFloater.physicalFloater, physicalFloater);
            GameManager.trackedObjectByDamageable.Add(physicalFloater.GetComponentInChildren<Construct_Floater_Core>(), physicalFloater);

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalFloater.physicalFloater.E);
                }
                physicalFloater.physicalFloater.RB.isKinematic = true;
            }

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedFloaterData updatedFloater = updatedObject as TrackedFloaterData;

            previousPos = position;
            position = updatedFloater.position;
            previousRot = rotation;
            rotation = updatedFloater.rotation;

            // Set physically
            if (physicalFloater != null)
            {
                physicalFloater.physicalFloater.transform.position = position;
                physicalFloater.physicalFloater.transform.rotation = rotation;
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
            if (physicalFloater != null)
            {
                physicalFloater.physicalFloater.transform.position = position;
                physicalFloater.physicalFloater.transform.rotation = rotation;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalFloater == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalFloater.physicalFloater.transform.position;
            rotation = physicalFloater.physicalFloater.transform.rotation;

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation);
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (localTrackedID != -1 && TrackedFloater.unknownFloaterBeginExploding.Contains(localWaitingIndex))
            {
                ClientSend.FloaterBeginExploding(trackedID, true);

                TrackedFloater.unknownFloaterBeginExploding.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedFloater.unknownFloaterBeginDefusing.Contains(localWaitingIndex))
            {
                ClientSend.FloaterBeginDefusing(trackedID, true);

                TrackedFloater.unknownFloaterBeginDefusing.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedFloater.unknownFloaterExplode.TryGetValue(localWaitingIndex, out bool defusing))
            {
                ClientSend.FloaterExplode(trackedID, defusing);

                TrackedFloater.unknownFloaterExplode.Remove(localWaitingIndex);
            }
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
                if (physicalFloater != null && physicalFloater.physicalFloater != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(physicalFloater.physicalFloater.E);
                    }
                    physicalFloater.physicalFloater.RB.isKinematic = false;
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physicalFloater != null && physicalFloater.physicalFloater != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity(physicalFloater.physicalFloater.E);
                    }
                    physicalFloater.physicalFloater.RB.isKinematic = true;
                }
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalFloater != null && physicalFloater.physicalFloater != null)
                {
                    GameManager.trackedFloaterByFloater.Remove(physicalFloater.physicalFloater);
                    GameManager.trackedObjectByDamageable.Remove(physicalFloater.physicalFloater);
                    GameManager.trackedObjectByDamageable.Remove(physicalFloater.GetComponentInChildren<Construct_Floater_Core>());
                }
            }
        }
    }
}
