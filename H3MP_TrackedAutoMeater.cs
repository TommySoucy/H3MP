using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedAutoMeater : MonoBehaviour
    {
        public AutoMeater physicalAutoMeaterScript;
        public H3MP_TrackedAutoMeaterData data;

        public bool sendDestroy = true; // To prevent feeback loops
        public static int skipDestroy;

        private void OnDestroy()
        {
            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeaterScript);

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our auto meaters to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer != -1)
                        {
                            H3MP_ServerSend.GiveAutoMeaterControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ServerSend.DestroyAutoMeater(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    H3MP_Server.autoMeaters[data.trackedID] = null;
                    H3MP_Server.availableAutoMeaterIndices.Add(data.trackedID);
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.autoMeaters[data.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                    H3MP_GameManager.autoMeaters[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                    data.localTrackedID = -1;
                }
            }
            else
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer != -1)
                        {
                            H3MP_ClientSend.GiveAutoMeaterControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ClientSend.DestroyAutoMeater(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    H3MP_Client.autoMeaters[data.trackedID] = null;
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.autoMeaters[data.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                    H3MP_GameManager.autoMeaters[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                    data.localTrackedID = -1;
                }
            }
        }
    }
}
