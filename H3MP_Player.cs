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
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 torsoPos;
        public Quaternion torsoRot;
        public Vector3 leftHandPos;
        public Quaternion leftHandRot;
        public int leftHandTrackedID;
        public Vector3 rightHandPos;
        public Quaternion rightHandRot;
        public int rightHandTrackedID;

        public H3MP_Player(int ID, string username, Vector3 spawnPos)
        {
            this.ID = ID;
            this.username = username;
            this.position = spawnPos;
            this.rotation = Quaternion.identity;
        }

        public void UpdateState()
        {
            H3MP_ServerSend.PlayerState(this);
        }
    }
}
