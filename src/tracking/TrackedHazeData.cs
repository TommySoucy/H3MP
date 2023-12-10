using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedHazeData : TrackedObjectData
    {
        public TrackedHaze physicalHaze;

        public Vector3 previousPos;
        public Vector3 position;
        public Quaternion previousRot;
        public Quaternion rotation;
        public float previousKEBattery;
        public float KEBattery;

        public TrackedHazeData()
        {

        }

        public TrackedHazeData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            KEBattery = packet.ReadFloat();
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Construct_Haze>() != null;
        }

        private static TrackedHaze MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedHaze trackedHaze = root.gameObject.AddComponent<TrackedHaze>();
            TrackedHazeData data = new TrackedHazeData();
            trackedHaze.hazeData = data;
            trackedHaze.data = data;
            data.physicalHaze = trackedHaze;
            data.physical = trackedHaze;
            Construct_Haze hazeScript = root.GetComponent<Construct_Haze>();
            data.physicalHaze.physicalHaze = hazeScript;
            data.physical.physical = hazeScript;

            data.typeIdentifier = "TrackedHazeData";
            data.active = trackedHaze.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedHazeByHaze.Add(trackedHaze.physicalHaze, trackedHaze);
            GameManager.trackedObjectByObject.Add(trackedHaze.physicalHaze, trackedHaze);
            GameManager.trackedObjectByDamageable.Add(trackedHaze.physicalHaze, trackedHaze);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedHaze.awoken)
            {
                trackedHaze.data.Update(true);
            }

            Mod.LogInfo("Made haze " + trackedHaze.name + " tracked", false);

            return trackedHaze;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(KEBattery);
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating haze at " + trackedID, false);
            GameObject prefab = null;
            Construct_Haze_Volume hazeVolume = GameObject.FindObjectOfType<Construct_Haze_Volume>();
            if (hazeVolume == null)
            {
                Mod.LogError("Failed to instantiate haze: " + trackedID + ": Could not find suitable haze volume to get prefab from");
                yield break;
            }
            else
            {
                prefab = hazeVolume.Haze_Prefab;
            }

            ++Mod.skipAllInstantiates;
            GameObject hazeInstance = GameObject.Instantiate(prefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalHaze = hazeInstance.AddComponent<TrackedHaze>();
            physical = physicalHaze;
            physicalHaze.physicalHaze = hazeInstance.GetComponent<Construct_Haze>();
            physical.physical = physicalHaze.physicalHaze;
            awaitingInstantiation = false;
            physicalHaze.hazeData = this;
            physicalHaze.data = this;

            GameManager.trackedHazeByHaze.Add(physicalHaze.physicalHaze, physicalHaze);
            GameManager.trackedObjectByObject.Add(physicalHaze.physicalHaze, physicalHaze);
            GameManager.trackedObjectByDamageable.Add(physicalHaze.physicalHaze, physicalHaze);

            // Deregister the AI from the manager if we are not in control
            // Also set RB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalHaze.physicalHaze.E);
                }
                physicalHaze.physicalHaze.RB.isKinematic = true;
            }

            // Initially set itself
            UpdateFromData(this);
        }

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedHazeData updatedHaze = updatedObject as TrackedHazeData;

            previousPos = position;
            position = updatedHaze.position;
            previousRot = rotation;
            rotation = updatedHaze.rotation;
            previousKEBattery = KEBattery;
            KEBattery = updatedHaze.KEBattery;

            // Set physically
            if (physicalHaze != null)
            {
                physicalHaze.physicalHaze.transform.position = position;
                physicalHaze.physicalHaze.transform.rotation = rotation;
                physicalHaze.physicalHaze.KEBattery = KEBattery;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            previousPos = position;
            position = packet.ReadVector3();
            previousRot = rotation;
            rotation = packet.ReadQuaternion();
            previousKEBattery = KEBattery;
            KEBattery = packet.ReadFloat();

            // Set physically
            if (physicalHaze != null)
            {
                physicalHaze.physicalHaze.transform.position = position;
                physicalHaze.physicalHaze.transform.rotation = rotation;
                physicalHaze.physicalHaze.KEBattery = KEBattery;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalHaze == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalHaze.physicalHaze.transform.position;
            rotation = physicalHaze.physicalHaze.transform.rotation;
            previousKEBattery = KEBattery;
            KEBattery = physicalHaze.physicalHaze.KEBattery;

            return updated || !previousPos.Equals(position) || !previousRot.Equals(rotation) || KEBattery != previousKEBattery;
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
                if (physicalHaze != null && physicalHaze.physicalHaze != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(physicalHaze.physicalHaze.E);
                    }
                    physicalHaze.physicalHaze.RB.isKinematic = false;
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physicalHaze != null && physicalHaze.physicalHaze != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity(physicalHaze.physicalHaze.E);
                    }
                    physicalHaze.physicalHaze.RB.isKinematic = true;
                }
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalHaze != null && physicalHaze.physicalHaze != null)
                {
                    GameManager.trackedHazeByHaze.Remove(physicalHaze.physicalHaze);
                    GameManager.trackedObjectByDamageable.Remove(physicalHaze.physicalHaze);
                }
            }
        }
    }
}
