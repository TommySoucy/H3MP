using FistVR;
using UnityEngine;

namespace H3MP
{
    public class H3MP_PlayerHitbox : MonoBehaviour, IFVRDamageable
    {
        public H3MP_PlayerManager manager;
        public enum Part
        {
            Head,
            Torso,
            LeftHand,
            RightHand
        }
        public Part part;

        public void Damage(Damage dam)
        {
            manager.Damage(part, dam);
        }
    }
}
