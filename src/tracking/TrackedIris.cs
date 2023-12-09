using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedIris : TrackedObject
    {
        public Construct_Iris physicalIris;
        public TrackedIrisData irisData;

        public static Dictionary<uint, List<object[]>> unknownIrisShatter = new Dictionary<uint, List<object[]>>();
        public static Dictionary<uint, Construct_Iris.IrisState> unknownIrisSetState = new Dictionary<uint, Construct_Iris.IrisState>();

        public override void Awake()
        {
            base.Awake();

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
            trackedReferences[refIndex] = this;
            Construct_Iris.BParamType newParamType = new Construct_Iris.BParamType();
            newParamType.Mats = new List<MatBallisticType>();
            newParamType.Pen = refIndex;
            GetComponent<Construct_Iris>().BParams.Add(newParamType);
        }

        public override bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            if (received)
            {
                ++UberShatterableShatterPatch.skip;
                physicalIris.Rings[data[0]].Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;

                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(irisData.trackedID, point, dir, intensity, data, clientID);
                }
            }
            else
            {
                int index = -1;
                for(int i=0; i< physicalIris.Rings.Count; ++i)
                {
                    if (physicalIris.Rings[i] == shatterable)
                    {
                        index = i;
                        break;
                    }
                }

                if(index > -1)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.UberShatterableShatter(irisData.trackedID, point, dir, intensity, data);
                    }
                    else if (irisData.trackedID != -1)
                    {
                        ClientSend.UberShatterableShatter(irisData.trackedID, point, dir, intensity, data);
                    }
                }
                else
                {
                    Mod.LogError("Iris HandleShatter, could not find shatterable in rings");
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
            GameManager.trackedIrisByIris.Remove(physicalIris);
            GameManager.trackedObjectByShatterable.Remove(physicalIris.GetComponentInChildren<UberShatterable>());
            for (int i = 0; i < physicalIris.Rings.Count; ++i)
            {
                GameManager.trackedObjectByShatterable.Remove(physicalIris.Rings[i]);
            }

            base.OnDestroy();
        }
    }
}
