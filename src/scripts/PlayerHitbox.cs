using FistVR;
using System;
using UnityEngine;

namespace H3MP.Scripts
{
    public class PlayerHitbox : MonoBehaviour, IFVRDamageable
    {
        [NonSerialized]
        public PlayerManager manager;

        public bool isHead;
        public float damageMultiplier = 1;

        public void Damage(Damage dam)
        {
            if(manager == null)
            {
                Mod.LogError("PlayerHitbox "+name+" was damaged but is missing ref to manager.");
            }
            else
            {
                manager.Damage(damageMultiplier, isHead, dam);
            }
        }
    }
}
