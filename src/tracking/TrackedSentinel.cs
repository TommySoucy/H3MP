using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSentinel : TrackedObject
    {
        public Construct_Sentinel physicalSentinel;
        public TrackedSentinelData sentinelData;

        public override bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            if (received)
            {
                ++UberShatterableShatterPatch.skip;
                physicalSentinel.Plates[data[0]].Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;

                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(sentinelData.trackedID, point, dir, intensity, data, clientID);
                }
            }
            else
            {
                int index = -1;
                for (int i = 0; i < physicalSentinel.Plates.Count; ++i)
                {
                    if (physicalSentinel.Plates[i] == shatterable)
                    {
                        index = i;
                        break;
                    }
                }

                if (index > -1)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.UberShatterableShatter(sentinelData.trackedID, point, dir, intensity, data);
                    }
                    else if (sentinelData.trackedID != -1)
                    {
                        ClientSend.UberShatterableShatter(sentinelData.trackedID, point, dir, intensity, data);
                    }
                }
                else
                {
                    Mod.LogError("Sentinel HandleShatter, could not find shatterable in plates");
                }
            }

            return true;
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedSentinelBySentinel.Remove(physicalSentinel);
            GameManager.trackedObjectByShatterable.Remove(physicalSentinel.GetComponentInChildren<UberShatterable>());
            for (int i = 0; i < physicalSentinel.Plates.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Remove(physicalSentinel.Plates[i]);
            }

            base.OnDestroy();
        }
    }
}
