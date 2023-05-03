using H3MP.Tracking;
using UnityEngine;

namespace H3MP
{
    public class TrackedItemReference : MonoBehaviour
    {
        public TrackedItem trackedItemRef;
        public int refIndex = -1;

        private void OnDestroy()
        {
            if(refIndex != -1)
            {
                TrackedItem.trackedItemReferences[refIndex] = null;
                TrackedItem.trackedItemRefObjects[refIndex] = null;
                TrackedItem.availableTrackedItemRefIndices.Add(refIndex);
            }
        }
    }
}
