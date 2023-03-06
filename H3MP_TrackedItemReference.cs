using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItemReference : MonoBehaviour
    {
        public H3MP_TrackedItem trackedItemRef;
        public int refIndex = -1;

        private void OnDestroy()
        {
            if(refIndex != -1)
            {
                H3MP_TrackedItem.trackedItemReferences[refIndex] = null;
                H3MP_TrackedItem.trackedItemRefObjects[refIndex] = null;
                H3MP_TrackedItem.availableTrackedItemRefIndices.Add(refIndex);
            }
        }
    }
}
