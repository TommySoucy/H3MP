using H3MP.Tracking;
using UnityEngine;

namespace H3MP.Scripts
{
    public class TrackedObjectReference : MonoBehaviour
    {
        public TrackedObject trackedRef;
        public int refIndex = -1;

        private void OnDestroy()
        {
            if(refIndex != -1 && Mod.managerObject != null && TrackedObject.trackedReferences.Length > refIndex)
            {
                TrackedObject.trackedReferences[refIndex] = null;
                TrackedObject.trackedReferenceObjects[refIndex] = null;
                TrackedObject.availableTrackedRefIndices.Add(refIndex);
            }
        }
    }
}
