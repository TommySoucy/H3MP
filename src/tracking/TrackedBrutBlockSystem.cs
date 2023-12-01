using FistVR;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBrutBlockSystem : TrackedObject
    {
        public BrutBlockSystem physicalBrutBlockSystem;
        public TrackedBrutBlockSystemData brutBlockSystemData;

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
            GetComponent<BrutBlockSystem>().BlockPointUppers.Add(trackedItemRef.transform);
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physicalObject anymore
            GameManager.trackedBrutBlockSystemByBrutBlockSystem.Remove(physicalBrutBlockSystem);

            base.OnDestroy();
        }
    }
}
