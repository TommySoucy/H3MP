using FistVR;
using H3MP.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedAutoMeater : TrackedObject
    {
        public AutoMeater physicalAutoMeater;
        public TrackedAutoMeaterData autoMeaterData;

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeater);
            GameManager.trackedObjectByInteractive.Remove(physicalAutoMeater.PO);
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
    }
}
