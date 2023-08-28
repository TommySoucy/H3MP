using FistVR;

namespace H3MP.Tracking
{
    public class TrackedGatlingGun : TrackedObject
    {
        public wwGatlingGun physicalGatlingGun;
        public TrackedGatlingGunData gatlingGunData;

        public override void EnsureUncontrolled()
        {
            if (gatlingGunData.controlHandle.m_hand != null)
            {
                gatlingGunData.controlHandle.ForceBreakInteraction();
            }
            if (gatlingGunData.crankHandle.m_hand != null)
            {
                gatlingGunData.crankHandle.ForceBreakInteraction();
            }
            if (gatlingGunData.baseHandle.m_hand != null)
            {
                gatlingGunData.baseHandle.ForceBreakInteraction();
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (data.controller != GameManager.ID)
            {
                // Take control

                // Send to all clients
                data.TakeControlRecursive();
            }
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physicalObject anymore
            GameManager.trackedGatlingGunByGatlingGun.Remove(physicalGatlingGun);
            GameManager.trackedObjectByInteractive.Remove(gatlingGunData.controlHandle);
            GameManager.trackedObjectByInteractive.Remove(gatlingGunData.crankHandle);
            GameManager.trackedObjectByInteractive.Remove(gatlingGunData.baseHandle);

            base.OnDestroy();
        }
    }
}
