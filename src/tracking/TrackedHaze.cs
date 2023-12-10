using FistVR;
using H3MP.Scripts;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedHaze : TrackedObject
    {
        public Construct_Haze physicalHaze;
        public TrackedHazeData hazeData;

        public override void Awake()
        {
            base.Awake();

            GameObject trackedItemRef = new GameObject();
            trackedItemRef.transform.parent = transform;
            TrackedObjectReference refScript = trackedItemRef.AddComponent<TrackedObjectReference>();
            ParticleSystem PSScript = trackedItemRef.AddComponent<ParticleSystem>();
            trackedItemRef.SetActive(false);

            CheckReferenceSize();
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferenceObjects[refIndex] = trackedItemRef;
            trackedReferences[refIndex] = this;
            trackedItemRef.name = refIndex.ToString();
            refScript.refIndex = refIndex;
            GetComponent<Construct_Haze>().PSystem2 = PSScript;
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedHazeByHaze.Remove(physicalHaze);
            GameManager.trackedObjectByDamageable.Remove(physicalHaze);

            base.OnDestroy();
        }
    }
}
