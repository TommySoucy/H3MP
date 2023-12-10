using FistVR;
using H3MP.Scripts;
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

            GameObject trackedBrutBlockSystemRef = new GameObject();
            trackedBrutBlockSystemRef.transform.parent = transform;
            TrackedObjectReference refScript = trackedBrutBlockSystemRef.AddComponent<TrackedObjectReference>();
            trackedBrutBlockSystemRef.SetActive(false);

            CheckReferenceSize();
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferenceObjects[refIndex] = trackedBrutBlockSystemRef;
            trackedReferences[refIndex] = this;
            trackedBrutBlockSystemRef.name = refIndex.ToString();
            refScript.refIndex = refIndex;
            GetComponent<BrutBlockSystem>().BlockPointUppers.Add(trackedBrutBlockSystemRef.transform);
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
