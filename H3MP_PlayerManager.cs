using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_PlayerManager : MonoBehaviour
    {
        public int ID;
        public string username;

        // Player transforms and state data
        public Transform head;
        public Transform torso;
        public Transform leftHand;
        public int leftHandTrackedID;
        public FVRInteractiveObject leftHandItem;
        public Transform rightHand;
        public int rightHandTrackedID;
        public FVRInteractiveObject rightHandItem;

        public string scene;

        private void Awake()
        {
            head = transform.GetChild(0);
            torso = transform.GetChild(1);
            leftHand = transform.GetChild(2);
            rightHand = transform.GetChild(3);
        }

        public void UpdateState(H3MP_Player playerData)
        {
            transform.position = playerData.position;
            transform.rotation = playerData.rotation;
            head.position = playerData.headPos;
            head.rotation = playerData.headRot;
            leftHand.position = playerData.leftHandPos;
            leftHand.rotation = playerData.leftHandRot;
            rightHand.position = playerData.rightHandPos;
            rightHand.rotation = playerData.rightHandRot;
        }
    }
}
