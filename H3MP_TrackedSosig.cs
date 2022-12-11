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
        public Sosig physicalSosigScript;
        public H3MP_TrackedSosigData data;

        public bool sendDestroy = true; // To prevent feeback loops
        public static int skipDestroy;

        private void OnDestroy()
        {
            H3MP_GameManager.trackedSosigBySosig.Remove(physicalSosigScript);

            // Set dead body state even if we are destroying because vanilla may try to process damage on it still
            // It being Dead will prevent it from doing that
            physicalSosigScript.BodyState = Sosig.SosigBodyState.Dead;

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer != -1)
                        {
                            H3MP_ServerSend.GiveSosigControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ServerSend.DestroySosig(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
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
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer != -1)
                        {
                            H3MP_ClientSend.GiveSosigControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ClientSend.DestroySosig(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
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
