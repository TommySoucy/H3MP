using FistVR;
using System.Collections.Generic;

namespace H3MP.Tracking
{
    public class TrackedFloater : TrackedObject
    {
        public Construct_Floater physicalFloater;
        public TrackedFloaterData floaterData;

        public static List<uint> unknownFloaterBeginExploding = new List<uint>();
        public static List<uint> unknownFloaterExplode = new List<uint>();

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedFloaterByFloater.Remove(physicalFloater);
            GameManager.trackedObjectByDamageable.Remove(physicalFloater);
            GameManager.trackedObjectByDamageable.Remove(physicalFloater.GetComponentInChildren<Construct_Floater_Core>());

            base.OnDestroy();
        }
    }
}
