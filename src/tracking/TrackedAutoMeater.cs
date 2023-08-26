using FistVR;
using H3MP.Networking;

namespace H3MP.Tracking
{
    public class TrackedAutoMeater : TrackedObject
    {
        public AutoMeater physicalAutoMeater;
        public TrackedAutoMeaterData autoMeaterData;

        public override void Awake()
        {
            base.Awake();

            GameManager.OnInstanceJoined += OnInstanceJoined;
        }

        protected override void OnDestroy()
        {
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeater);
            FVRPhysicalObject[] pos = GetComponentsInChildren<FVRPhysicalObject>();
            for (int i = 0; i < pos.Length; ++i)
            {
                GameManager.trackedObjectByInteractive.Remove(pos[i]);
            }
            AutoMeaterHitZone[] tempHitZones = physicalAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
            for (int i = 0; i < tempHitZones.Length; ++i)
            {
                GameManager.trackedObjectByDamageable.Remove(tempHitZones[i]);
            }

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            EnsureUncontrolled();

            base.OnDestroy();
        }

        public override void EnsureUncontrolled()
        {
            if (physicalAutoMeater.PO != null && physicalAutoMeater.PO.m_hand != null)
            {
                physicalAutoMeater.PO.ForceBreakInteraction();
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GiveObjectControl(data.trackedID, GameManager.ID, null);
                }
                else
                {
                    ClientSend.GiveObjectControl(data.trackedID, GameManager.ID, null);
                }

                data.controller = GameManager.ID;
                data.localTrackedID = GameManager.objects.Count;
                GameManager.objects.Add(data);
            }
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            // Need to make sure that we give control of the sosig back to the controller of a the current TNH instance if there is one
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GiveObjectControl(data.trackedID, Mod.currentTNHInstance.controller, null);
                }
                else
                {
                    ClientSend.GiveObjectControl(data.trackedID, Mod.currentTNHInstance.controller, null);
                }

                // Update locally
                data.RemoveFromLocal();
            }
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            // Since AutoMeaters can't go across scenes, we only process an instance change if we are not currently loading into a new scene
            if (!GameManager.sceneLoading)
            {
                TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
                data.ShouldBring(false, ref bring);

                ++GameManager.giveControlOfDestroyed;

                if (bring == TrackedObjectData.ObjectBringType.Yes)
                {
                    // Want to bring everything with us
                    // What we are interacting with, we will bring with us completely, destroying it on remote sides
                    // Whet we do not interact with, we will make a copy of in the new instance
                    if (data.IsControlled(out int interactionID))
                    {
                        data.SetInstance(instance, true);
                    }
                    else // Not interacting with
                    {
                        DestroyImmediate(this);

                        GameManager.SyncTrackedObjects(transform, true, null);
                    }
                }
                else if (bring == TrackedObjectData.ObjectBringType.OnlyInteracted && data.IsControlled(out int interactionID))
                {
                    data.SetInstance(instance, true);
                }
                else // Don't want to bring, destroy
                {
                    DestroyImmediate(gameObject);
                }

                --GameManager.giveControlOfDestroyed;
            }
        }
    }
}
