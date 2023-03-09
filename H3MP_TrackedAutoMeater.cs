using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedAutoMeater : MonoBehaviour
    {
        public AutoMeater physicalAutoMeaterScript;
        public H3MP_TrackedAutoMeaterData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<uint, int> unknownControlTrackedIDs = new Dictionary<uint, int>();
        public static List<uint> unknownDestroyTrackedIDs = new List<uint>();

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipFullDestroy;

        private void Awake()
        {
            if (data != null)
            {
                data.Update(true);
            }

            awoken = true;
            if (sendOnAwake)
            {
                Mod.LogInfo(gameObject.name + " awoken");
                if (H3MP_ThreadManager.host)
                {
                    // This will also send a packet with the AutoMeater to be added in the client's global AutoMeater list
                    H3MP_Server.AddTrackedAutoMeater(data, 0);
                }
                else
                {
                    // Tell the server we need to add this item to global tracked AutoMeaters
                    data.localWaitingIndex = H3MP_Client.localAutoMeaterCounter++;
                    H3MP_Client.waitingLocalAutoMeaters.Add(data.localWaitingIndex, data);
                    H3MP_ClientSend.TrackedAutoMeater(data);
                }
            }
        }

        private void OnDestroy()
        {
            if (skipFullDestroy)
            {
                return;
            }

            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeaterScript);

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed > 0)
                {
                    // We just want to give control of our auto meaters to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, H3MP_GameManager.playersAtLoadStart);

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

                            if (data.removeFromListOnDestroy && H3MP_Server.autoMeaters[data.trackedID] != null)
                            {
                                H3MP_Server.autoMeaters[data.trackedID] = null;
                                H3MP_Server.availableAutoMeaterIndices.Add(data.trackedID);
                                H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            H3MP_ServerSend.GiveAutoMeaterControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

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

                    if (data.removeFromListOnDestroy && H3MP_Server.autoMeaters[data.trackedID] != null)
                    {
                        H3MP_Server.autoMeaters[data.trackedID] = null;
                        H3MP_Server.availableAutoMeaterIndices.Add(data.trackedID);
                        H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
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
                if (H3MP_GameManager.giveControlOfDestroyed > 0)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, H3MP_GameManager.playersAtLoadStart);

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy)
                            {
                                if (data.trackedID == -1)
                                {
                                    if (!unknownDestroyTrackedIDs.Contains(data.localWaitingIndex))
                                    {
                                        unknownDestroyTrackedIDs.Add(data.localWaitingIndex);
                                    }

                                    // We want to keep it in local until we give destruction order
                                    removeFromLocal = false;
                                }
                                else
                                {
                                    H3MP_ClientSend.DestroyAutoMeater(data.trackedID);

                                    H3MP_Client.autoMeaters[data.trackedID] = null;
                                    H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                                }
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.trackedID != -1 && data.trackedID != -2)
                            {
                                H3MP_Client.autoMeaters[data.trackedID] = null;
                                H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            if (data.trackedID == -1)
                            {
                                if (unknownControlTrackedIDs.ContainsKey(data.localWaitingIndex))
                                {
                                    unknownControlTrackedIDs[data.localWaitingIndex] = otherPlayer;
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localWaitingIndex, otherPlayer);
                                }

                                // We want to keep it in local until we give control
                                removeFromLocal = false;
                            }
                            else if (data.trackedID != -2)
                            {
                                H3MP_ClientSend.GiveAutoMeaterControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

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
                            if (!unknownDestroyTrackedIDs.Contains(data.localWaitingIndex))
                            {
                                unknownDestroyTrackedIDs.Add(data.localWaitingIndex);
                            }

                            // We want to keep it in local until we give destruction order
                            removeFromLocal = false;
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyAutoMeater(data.trackedID);

                            if (data.removeFromListOnDestroy)
                            {
                                H3MP_Client.autoMeaters[data.trackedID] = null;
                                H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if(data.removeFromListOnDestroy && data.trackedID != -1 && data.trackedID != -2)
                    {
                        H3MP_Client.autoMeaters[data.trackedID] = null;
                        H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
                }
                if (removeFromLocal && data.localTrackedID != -1)
                {
                    data.RemoveFromLocal();
                }
            }

            data.removeFromListOnDestroy = true;
        }
    }
}
