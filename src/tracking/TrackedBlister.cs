using FistVR;

namespace H3MP.Tracking
{
    public class TrackedBlister : TrackedObject
    {
        public Construct_Blister physicalBlister;
        public TrackedBlisterData blisterData;

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedBlisterByBlister.Remove(physicalBlister);

            base.OnDestroy();
        }
    }
}
