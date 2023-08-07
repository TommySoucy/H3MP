using FistVR;
using H3MP.Tracking;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedBreakableGlass : TrackedObject
    {
        public BreakableGlass physicalBreakableGlass;
        public TrackedBreakableGlassData breakableGlassData;

        public override void Awake()
        {
            base.Awake();

            GameManager.OnInstanceJoined += OnInstanceJoined;
        }

        public void PlayerShatterAudio(int mode)
        {
            if(breakableGlassData.damager != null)
            {
                float delay = Vector3.Distance(GM.CurrentPlayerBody.Head.position, transform.position) / 343;
                FVRPooledAudioType type = breakableGlassData.breakDepth > 0 ? FVRPooledAudioType.Impacts : FVRPooledAudioType.Generic;

                switch (mode)
                {
                    case 0:
                        SM.PlayCoreSoundDelayed(type, breakableGlassData.damager.AudEvent_Shatter_BlowOut, transform.position, delay);
                        break;
                    case 1:
                        SM.PlayCoreSoundDelayed(type, breakableGlassData.damager.AudEvent_Head_Projectile, transform.position, delay);
                        SM.PlayCoreSoundDelayed(type, breakableGlassData.damager.AudEvent_Tail, transform.position, delay);
                        break;
                    case 2:
                        SM.PlayCoreSoundDelayed(type, breakableGlassData.damager.AudEvent_Head_Melee, transform.position, delay);
                        SM.PlayCoreSoundDelayed(type, breakableGlassData.damager.AudEvent_Tail, transform.position, delay);
                        break;
                }
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedBreakableGlassByBreakableGlass.Remove(physicalBreakableGlass);
            GameManager.trackedBreakableGlassByBreakableGlassDamager.Remove(breakableGlassData.damager);
            GameManager.trackedObjectByDamageable.Remove(breakableGlassData.damager);

            base.OnDestroy();
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            if (!GameManager.sceneLoading)
            {
                TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
                data.ShouldBring(false, ref bring);

                ++GameManager.giveControlOfDestroyed;

                // Note: Breakable glass cannot be interacted with, so no need to check taht case with IsControlled
                if (bring == TrackedObjectData.ObjectBringType.Yes)
                {
                    DestroyImmediate(this);

                    GameManager.SyncTrackedObjects(transform, true, null);
                }
                else // Don't want to bring, destroy
                {
                    DestroyImmediate(gameObject);
                }

                --GameManager.giveControlOfDestroyed;
            }
        }
    }
}
