using FistVR;
using H3MP.Scripts;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedPlayerBody : TrackedObject
    {
        public TrackedPlayerBodyData playerBodyData;
        public PlayerBody physicalPlayerBody;

        public virtual void Start()
        {
            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;

            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                if (GM.CurrentPlayerBody != null)
                {
                    physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                    physicalPlayerBody.handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                    physicalPlayerBody.handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                    physicalPlayerBody.SetHeadVisible(false);
                    physicalPlayerBody.SetCollidersEnabled(false);
                }
            }
            else
            {
                physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                physicalPlayerBody.handsToFollow[0] = GameManager.players[playerBodyData.controller].leftHand;
                physicalPlayerBody.handsToFollow[1] = GameManager.players[playerBodyData.controller].rightHand;
                physicalPlayerBody.SetHeadVisible(true);
                physicalPlayerBody.SetCollidersEnabled(true);
            }
        }

        private void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                physicalPlayerBody.headToFollow = playerBody.Head;
                physicalPlayerBody.handsToFollow[0] = playerBody.LeftHand;
                physicalPlayerBody.handsToFollow[1] = playerBody.RightHand;
                physicalPlayerBody.SetHeadVisible(false);
                physicalPlayerBody.SetCollidersEnabled(false);
            }
        }

        public virtual void Update()
        {
            if(physicalPlayerBody.headToFollow == null)
            {
                if (playerBodyData.controller == GameManager.ID)
                {
                    if (GM.CurrentPlayerBody != null)
                    {
                        physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                        physicalPlayerBody.SetHeadVisible(false);
                    }
                }
                else
                {
                    physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                    physicalPlayerBody.SetHeadVisible(true);
                }
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;

            base.OnDestroy();
        }
    }
}
