using FistVR;
using H3MP.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace H3MP.Tracking
{
    public class TrackedPlayerBody : TrackedObject
    {
        public TrackedPlayerBodyData playerBodyData;
        public PlayerBody physicalPlayerBody;

        public virtual void Start()
        {
            if(playerBodyData.controller == GameManager.ID)
            {
                physicalPlayerBody.headToFollow = GM.CurrentPlayerBody.Head;
                physicalPlayerBody.SetHeadVisible(false);
            }
            else
            {
                physicalPlayerBody.headToFollow = GameManager.players[playerBodyData.controller].head;
                physicalPlayerBody.SetHeadVisible(true);
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
