using FistVR;
using UnityEngine;

namespace H3MP
{
    public class PlayerHitbox : MonoBehaviour, IFVRDamageable
    {
        public PlayerManager manager;
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
