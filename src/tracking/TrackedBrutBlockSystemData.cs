using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBrutBlockSystemData : TrackedObjectData
    {
        public TrackedBrutBlockSystem physicalBrutBlockSystem;

        public static bool firstInScene;
        public static List<BrutBlockSystem> sceneBrutBlockSystems = new List<BrutBlockSystem>();

        public int index;

        public TrackedBrutBlockSystemData()
        {

        }

        public TrackedBrutBlockSystemData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {

        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<BrutBlockSystem>() != null;
        }

        public static bool TrackSkipped(Transform t)
        {
            if (firstInScene)
            {
                sceneBrutBlockSystems.Clear();
                firstInScene = false;
            }
            sceneBrutBlockSystems.Add(t.GetComponent<BrutBlockSystem>());

            // Prevent destruction if tracking is skipped because we are not in control
            return false;
        }

        public static TrackedBrutBlockSystem MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedBrutBlockSystem trackedBrutBlockSystem = root.gameObject.AddComponent<TrackedBrutBlockSystem>();
            TrackedBrutBlockSystemData data = new TrackedBrutBlockSystemData();
            trackedBrutBlockSystem.data = data;
            trackedBrutBlockSystem.brutBlockSystemData = data;
            data.physicalBrutBlockSystem = trackedBrutBlockSystem;
            data.physical = trackedBrutBlockSystem;
            data.physicalBrutBlockSystem.physicalBrutBlockSystem = root.GetComponent<BrutBlockSystem>();
            data.physical.physical = data.physicalBrutBlockSystem.physicalBrutBlockSystem;

            data.typeIdentifier = "TrackedBrutBlockSystemData";
            data.active = trackedBrutBlockSystem.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            if (firstInScene)
            {
                sceneBrutBlockSystems.Clear();
                firstInScene = false;
            }
            data.index = sceneBrutBlockSystems.Count;
            sceneBrutBlockSystems.Add(data.physicalBrutBlockSystem.physicalBrutBlockSystem);

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedBrutBlockSystem.awoken)
            {
                trackedBrutBlockSystem.data.Update(true);
            }

            return trackedBrutBlockSystem;
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating BrutBlockSystem");
            // Get instance
            BrutBlockSystem physicalBrutBlockSystemScript = null;
            if (sceneBrutBlockSystems.Count > index)
            {
                physicalBrutBlockSystemScript = sceneBrutBlockSystems[index];
                if (physicalBrutBlockSystemScript == null)
                {
                    Mod.LogError("Attempted to instantiate BrutBlockSystem " + index + " sent from " + controller + " but list at index is null.");
                    yield break;
                }
            }
            else
            {
                Mod.LogError("Attempted to instantiate BrutBlockSystem " + index + " sent from " + controller + " but index does not fit in scene gatling gun list.");
                yield break;
            }
            GameObject brutBlockSystemInstance = physicalBrutBlockSystemScript.gameObject;

            physicalBrutBlockSystem = brutBlockSystemInstance.AddComponent<TrackedBrutBlockSystem>();
            physical = physicalBrutBlockSystem;
            physicalBrutBlockSystem.physicalBrutBlockSystem = physicalBrutBlockSystemScript;
            physical.physical = physicalBrutBlockSystemScript;
            awaitingInstantiation = false;
            physicalBrutBlockSystem.brutBlockSystemData = this;
            physical.data = this;

            // Initially set itself
            UpdateFromData(this, true);
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalBrutBlockSystem != null && physicalBrutBlockSystem.physicalBrutBlockSystem != null)
                {
                    GameManager.trackedBrutBlockSystemByBrutBlockSystem.Remove(physicalBrutBlockSystem.physicalBrutBlockSystem);
                }
            }
        }
    }
}
