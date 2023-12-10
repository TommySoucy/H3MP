using FistVR;
using H3MP.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedNode : TrackedObject
    {
        public Construct_Node physicalNode;
        public TrackedNodeData nodeData;

        public static List<uint> unknownInit = new List<uint>();

        public override void Awake()
        {
            base.Awake();

            GameObject trackedNodeRef = new GameObject();
            Scripts.TrackedObjectReference refScript = trackedNodeRef.AddComponent<Scripts.TrackedObjectReference>();
            trackedNodeRef.SetActive(false);

            CheckReferenceSize();
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferences[refIndex] = this;
            trackedNodeRef.name = refIndex.ToString();
            refScript.refIndex = refIndex;
            GetComponent<Construct_Node>().Stems.Add(trackedNodeRef.transform);
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

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedNodeByNode.Remove(physicalNode);
            GameManager.trackedObjectByInteractive.Remove(physicalNode);

            base.OnDestroy();
        }
    }
}
