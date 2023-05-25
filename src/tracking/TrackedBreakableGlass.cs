using H3MP.Tracking;

namespace H3MP.src.tracking
{
    public class TrackedBreakableGlass : TrackedObject
    {
        public BreakableGlass physicalBreakableGlass;
        public TrackedBreakableGlassData breakableGlassData;

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedBreakableGlassByBreakableGlass.Remove(physicalBreakableGlass);
            GameManager.trackedBreakableGlassByBreakableGlassDamager.Remove(breakableGlassData.damager);

            base.OnDestroy();
        }
    }
}
