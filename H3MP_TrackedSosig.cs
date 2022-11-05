using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedSosig : MonoBehaviour
    {
        public Sosig physicalSosig;
        public H3MP_TrackedSosigData data;

        public bool sendDestroy = true; // To prevent feeback loops

        private void OnDestroy()
        {
            H3MP_GameManager.trackedSosigBySosig.Remove(physicalSosig);

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
                            if (player.Value.gameObject.activeSelf)
                            {
                                firstPlayerInScene = player.Key;
                            }
                            break;
                        }

                        H3MP_ServerSend.GiveSosigControl(data.trackedID, firstPlayerInScene);

                        // Also change controller locally
                        data.controller = firstPlayerInScene;
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroySosig(data.trackedID);
                        sendDestroy = true;
                    }

                    H3MP_Server.sosigs[data.trackedID] = null;
                    H3MP_Server.availableSosigIndices.Add(data.trackedID);
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.sosigs[data.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                    H3MP_GameManager.sosigs[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                    data.localTrackedID = -1;
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
                            if (player.Value.gameObject.activeSelf)
                            {
                                firstPlayerInScene = player.Key;
                            }
                            break;
                        }

                        H3MP_ClientSend.GiveSosigControl(data.trackedID, firstPlayerInScene);

                        // Also change controller locally
                        data.controller = firstPlayerInScene;
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ClientSend.DestroySosig(data.trackedID);
                        sendDestroy = true;
                    }

                    H3MP_Client.sosigs[data.trackedID] = null;
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.sosigs[data.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                    H3MP_GameManager.sosigs[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                    data.localTrackedID = -1;
                }
            }
        }
    }
}
