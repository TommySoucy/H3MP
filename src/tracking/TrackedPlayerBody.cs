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
            physicalPlayerBody.handsToFollow = new Transform[2];
            if (playerBodyData.controller == GameManager.ID)
            {
                physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                physicalPlayerBody.handsToFollow[0] = GM.CurrentPlayerBody.LeftHand;
                physicalPlayerBody.handsToFollow[1] = GM.CurrentPlayerBody.RightHand;
                physicalPlayerBody.SetHeadVisible(false);
                physicalPlayerBody.SetCollidersEnabled(false);
            }
            else
            {
                physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                physicalPlayerBody.handsToFollow[0] = GameManager.players[playerBodyData.controller].leftHand;
                physicalPlayerBody.handsToFollow[1] = GameManager.players[playerBodyData.controller].rightHand;
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
    }
}
