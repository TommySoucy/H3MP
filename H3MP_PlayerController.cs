using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_PlayerController : MonoBehaviour
    {
        public int leftHandTrackedID = -1;
        public int rightHandTrackedID = -1;

        private void FixedUpdate()
        {
            SendStateToServer();
        }

        private void SendStateToServer()
        {
            H3MP_ClientSend.PlayerState(GM.CurrentPlayerBody.transform.position,
                                        GM.CurrentPlayerBody.transform.rotation,
                                        GM.CurrentPlayerBody.headPositionFiltered,
                                        GM.CurrentPlayerBody.headRotationFiltered,
                                        GM.CurrentPlayerBody.Torso,
                                        GM.CurrentPlayerBody.LeftHand.position,
                                        GM.CurrentPlayerBody.LeftHand.rotation,
                                        leftHandTrackedID,
                                        GM.CurrentPlayerBody.RightHand.position,
                                        GM.CurrentPlayerBody.RightHand.rotation,
                                        rightHandTrackedID);
        }
    }
}
