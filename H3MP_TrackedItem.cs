using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public H3MP_TrackedItemData data;

        public bool sendDestroy = true; // To prevent feeback loops

        private void OnDestroy()
        {
            if (H3MP_ThreadManager.host)
            {
                if (sendDestroy)
                {
                    H3MP_ServerSend.DestroyItem(data.trackedID);
                }

                H3MP_Server.items.Remove(data.trackedID);
                if(data.controller == 0)
                {
                    H3MP_GameManager.items.Remove(data.trackedID);
                }
            }
            else
            {
                if (sendDestroy)
                {
                    H3MP_ClientSend.DestroyItem(data.trackedID);
                }

                H3MP_Client.items.Remove(data.trackedID);
                if (data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items.Remove(data.trackedID);
                }
            }
        }
    }
}
