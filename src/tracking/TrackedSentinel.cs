using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSentinel : TrackedObject
    {
        cont from here, need to review all this sentinel shit and implement it
        public Construct_Sentinel physicalSentinel;
        public TrackedSentinelData sentinelData;

        public static List<uint> unknownSentinelShatter = new List<uint>();
        public static Dictionary<uint, byte> unknownSentinelPlateShatter = new Dictionary<uint, byte>();

        public override void Awake()
        {
            base.Awake();

            GameObject trackedItemRef = new GameObject();
            trackedItemRef.transform.parent = transform;
            trackedItemRef.SetActive(false);
            if (availableTrackedRefIndices.Count == 0)
            {
                GameObject[] tempRefs = trackedReferenceObjects;
                trackedReferenceObjects = new GameObject[tempRefs.Length + 100];
                for (int i = 0; i < tempRefs.Length; ++i)
                {
                    trackedReferenceObjects[i] = tempRefs[i];
                }
                TrackedObject[] tempItems = trackedReferences;
                trackedReferences = new TrackedObject[tempItems.Length + 100];
                for (int i = 0; i < tempItems.Length; ++i)
                {
                    trackedReferences[i] = tempItems[i];
                }
                for (int i = tempItems.Length; i < trackedReferences.Length; ++i)
                {
                    availableTrackedRefIndices.Add(i);
                }
            }
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferenceObjects[refIndex] = trackedItemRef;
            trackedReferences[refIndex] = this;
            trackedItemRef.name = refIndex.ToString();
            GetComponent<Construct_Sentinel>().SpawnOnSplode.Add(trackedItemRef);
        }

        public override bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            TODO: // Take a look at TrackedIRis handle shatter, we should prob do the same for plate index and such
            if (received)
            {
                ++UberShatterableShatterPatch.skip;
                physicalSentinel.GetComponentInChildren<UberShatterable>().Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;

                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(sentinelData.trackedID, point, dir, intensity, data, clientID);
                }
            }
            else
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
            GameManager.trackedObjectByDamageable.Remove(physicalSentinel);
            GameManager.trackedObjectByDamageable.Remove(physicalSentinel.GetComponentInChildren<Construct_Sentinel_Core>());

            base.OnDestroy();
        }
    }
}
