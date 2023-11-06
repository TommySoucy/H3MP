using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBlister : TrackedObject
    {
        public Construct_Blister physicalBlister;
        public TrackedBlisterData blisterData;

        public static List<uint> unknownBlisterShatter = new List<uint>();

        public override bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            if (received)
            {
                ++UberShatterableShatterPatch.skip;
                physicalBlister.GetComponentInChildren<UberShatterable>().Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;

                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(blisterData.trackedID, point, dir, intensity, data, clientID);
                }
            }
            else
            {
                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(blisterData.trackedID, point, dir, intensity, data);
                }
                else if (blisterData.trackedID != -1)
                {
                    ClientSend.UberShatterableShatter(blisterData.trackedID, point, dir, intensity, data);
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
            GameManager.trackedBlisterByBlister.Remove(physicalBlister);
            GameManager.trackedObjectByShatterable.Remove(physicalBlister.GetComponentInChildren<UberShatterable>());

            base.OnDestroy();
        }
    }
}
