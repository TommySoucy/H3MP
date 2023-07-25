using FistVR;
using UnityEngine;

namespace H3MP.Scripts
{
    public class PlayerHitbox : MonoBehaviour, IFVRDamageable
    {
        public PlayerManager manager;

        public bool isHead;
        public float damageMultiplier;

        public void Damage(Damage dam)
        {
            manager.Damage(damageMultiplier, isHead, dam);
        }
    }
}
