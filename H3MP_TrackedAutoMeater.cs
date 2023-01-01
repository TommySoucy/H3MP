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
        public bool awoken;
        public bool sendOnAwake;
        public string sendScene;
        public int sendInstance;

        // Unknown tracked ID queues
        public static Dictionary<int, int> unknownControlTrackedIDs = new Dictionary<int, int>();
        public static List<int> unknownDestroyTrackedIDs = new List<int>();

        public bool sendDestroy = true; // To prevent feeback loops

        private void Awake()
        {
            awoken = true;
            if (sendOnAwake)
            {
                Debug.Log(gameObject.name + " awoken");
                if (H3MP_ThreadManager.host)
                {
                    // This will also send a packet with the AutoMeater to be added in the client's global AutoMeater list
                    H3MP_Server.AddTrackedAutoMeater(data, sendScene, sendInstance, 0);
                }
                else
                {
                    // Tell the server we need to add this item to global tracked AutoMeaters
                    H3MP_ClientSend.TrackedAutoMeater(data, sendScene, sendInstance);
                }
            }
        }

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

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy)
                            {
                                H3MP_ServerSend.DestroyAutoMeater(data.trackedID);
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            H3MP_Server.autoMeaters[data.trackedID] = null;
                            H3MP_Server.availableAutoMeaterIndices.Add(data.trackedID);
                        }
                        else
                        {
                            H3MP_ServerSend.GiveAutoMeaterControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroyAutoMeater(data.trackedID);
                    }
                    else
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
                bool removeFromLocal = true;
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy)
                            {
                                if (data.trackedID == -1)
                                {
                                    if (!unknownDestroyTrackedIDs.Contains(data.localTrackedID))
                                    {
                                        unknownDestroyTrackedIDs.Add(data.localTrackedID);
                                    }

                                    // We want to keep it in local until we give destruction order
                                    removeFromLocal = false;
                                }
                                else
                                {
                                    H3MP_ClientSend.DestroyAutoMeater(data.trackedID);

                                    H3MP_Client.autoMeaters[data.trackedID] = null;
                                }
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.trackedID != -1)
                            {
                                H3MP_Client.autoMeaters[data.trackedID] = null;
                            }
                        }
                        else
                        {
                            if (data.trackedID == -1)
                            {
                                if (unknownControlTrackedIDs.ContainsKey(data.localTrackedID))
                                {
                                    unknownControlTrackedIDs[data.localTrackedID] = otherPlayer;
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localTrackedID, otherPlayer);
                                }

                                // We want to keep it in local until we give control
                                removeFromLocal = false;
                            }
                            else
                            {
                                H3MP_ClientSend.GiveAutoMeaterControl(data.trackedID, otherPlayer);

                                // Also change controller locally
                                data.controller = otherPlayer;
                            }
                        }
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        if (data.trackedID == -1)
                        {
                            if (!unknownDestroyTrackedIDs.Contains(data.localTrackedID))
                            {
                                unknownDestroyTrackedIDs.Add(data.localTrackedID);
                            }

                            // We want to keep it in local until we give destruction order
                            removeFromLocal = false;
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyAutoMeater(data.trackedID);

                            H3MP_Client.autoMeaters[data.trackedID] = null;
                        }
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if(data.trackedID != -1)
                    {
                        H3MP_Client.autoMeaters[data.trackedID] = null;
                    }
                }
                if (removeFromLocal && data.localTrackedID != -1)
                {
                    data.RemoveFromLocal();
                }
            }
        }
    }
}
