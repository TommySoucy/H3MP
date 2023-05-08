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

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            EnsureUncontrolled();

            base.OnDestroy();
        }

        public void EnsureUncontrolled()
        {
            if (physicalAutoMeater.PO != null && physicalAutoMeater.PO.m_hand != null)
            {
                physicalAutoMeater.PO.ForceBreakInteraction();
            }
        }
    }
}
