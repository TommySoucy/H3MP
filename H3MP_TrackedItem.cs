using FistVR;
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
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ServerSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Server.items[data.trackedID] = null;
                    H3MP_Server.availableItemIndices.Add(data.trackedID);
                }
                if(data.controller == 0)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
            else
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ClientSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ClientSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Client.items[data.trackedID] = null;
                    H3MP_Client.availableItemIndices.Add(data.trackedID);
                }
                if (data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
        }
    }
}
