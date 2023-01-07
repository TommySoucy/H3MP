using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_Player
    {
        public int ID;
        public string username;

        // State vars
        public int IFF;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 torsoPos;
        public Quaternion torsoRot;
        public Vector3 leftHandPos;
        public Quaternion leftHandRot;
        public Vector3 rightHandPos;
        public Quaternion rightHandRot;
        public float health;
        public int maxHealth;

        public string scene;
        public int instance;

        public H3MP_Player(int ID, string username, Vector3 spawnPos, int IFF)
        {
            this.ID = ID;
            this.username = username;
            this.position = spawnPos;
            this.rotation = Quaternion.identity;
            this.IFF = IFF;
        }

        public void UpdateState()
        {
            // TODO: Keep track of which scene players are in and which plaer are in each other's scenes so we can send this update only to those
            H3MP_ServerSend.PlayerState(this);
        }
    }
}
