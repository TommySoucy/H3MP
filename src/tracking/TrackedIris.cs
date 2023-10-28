using FistVR;
using System.Collections.Generic;

namespace H3MP.Tracking
{
    public class TrackedIris : TrackedObject
    {
        public Construct_Iris physicalIris;
        public TrackedIrisData irisData;

        public static List<uint> unknownIrisBeginExploding = new List<uint>();
        public static List<uint> unknownIrisExplode = new List<uint>();

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedIrisByIris.Remove(physicalIris);
            GameManager.trackedObjectByDamageable.Remove(physicalIris);
            GameManager.trackedObjectByDamageable.Remove(physicalIris.GetComponentInChildren<Construct_Iris_Core>());

            base.OnDestroy();
        }
    }
}
