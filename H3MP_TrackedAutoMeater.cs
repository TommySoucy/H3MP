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
        public bool skipDestroyProcessing;
        public bool skipFullDestroy;
        public bool dontGiveControl;

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
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Remove(physicalAutoMeaterScript);

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.EnsureUncontrolled(physicalAutoMeaterScript.PO);

            // Have a flag in case we don't actually want to remove it from local after processing
            // In case we can't detroy gobally because we are still waiting for a tracked ID for example
            bool removeFromLocal = true;

            // Check if we want to process sending, giving control, etc.
            // We might want to skip just this part if the object was refused by the server
            if (skipDestroyProcessing)
            {
                // Check if we want to give control of any destroyed objects
                // This would be the case while we change scene, objects will be destroyed but if there are other clients
                // in our previous scene/instance, we don't want to destroy the object globally, we want to give control of it to one of them
                // We might receive an order to destroy an object while we have giveControlOfDestroyed > 0, if so dontGiveControl flag 
                // explicitly says to destroy
                if (H3MP_GameManager.giveControlOfDestroyed == 0 || dontGiveControl)
                {
                    DestroyGlobally(ref removeFromLocal);
                }
                else // We want to give control of this object instead of destroying it globally
                {
                    if (data.controller == H3MP_GameManager.ID)
                    {
                        // Find best potential host
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, H3MP_GameManager.playersAtLoadStart);
                        if (otherPlayer == -1)
                        {
                            // No other best potential host, destroy globally
                            DestroyGlobally(ref removeFromLocal);
                        }
                        else // We have a potential new host to give control to
                        {
                            // Check if can give control
                            if (data.trackedID > -1)
                            {
                                // Give control with us as debounce because we know we are no longer eligible to control this object
                                H3MP_ServerSend.GiveAutoMeaterControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

                                // Also change controller locally
                                data.controller = otherPlayer;
                            }
                            else // trackedID == -1, note that it cannot == -2 because DestroyGlobally will never get called in that case due to skipDestroyProcessing flag
                            {
                                // Tell destruction we want to keep this in local for later
                                removeFromLocal = false;

                                // Keep the control change in unknown so we can send it to others if we get a tracked ID
                                if (unknownControlTrackedIDs.TryGetValue(data.localWaitingIndex, out int val))
                                {
                                    if (val != otherPlayer)
                                    {
                                        unknownControlTrackedIDs[data.localWaitingIndex] = otherPlayer;
                                    }
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localWaitingIndex, otherPlayer);
                                }
                            }
                        }
                    }
                    // else, we don't control this object, it will simply be destroyed physically on our side
                }
            }

            // If we control this item, remove it from local lists
            // Which has to be done no matter what OnDestroy because we will not have a physicalObject to control after
            // We have either destroyed it or given control of it above
            if (data.localTrackedID != -1 && removeFromLocal)
            {
                data.RemoveFromLocal();
            }

            // Reset relevant flags
            data.removeFromListOnDestroy = true;
            sendDestroy = true;
            skipDestroyProcessing = false;
        }

        private void DestroyGlobally(ref bool removeFromLocal)
        {
            // Check if can destroy globally
            if (data.trackedID > -1)
            {
                // Check if want to send destruction
                // Used to prevent feedback loops
                if (sendDestroy)
                {
                    // Send destruction
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.DestroyAutoMeater(data.trackedID, data.removeFromListOnDestroy);
                    }
                    else
                    {
                        H3MP_ClientSend.DestroyAutoMeater(data.trackedID, data.removeFromListOnDestroy);
                    }
                }

                // Remove from globals lists if we want
                if (data.removeFromListOnDestroy)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_Server.autoMeaters[data.trackedID] = null;
                        H3MP_Server.availableAutoMeaterIndices.Add(data.trackedID);
                    }
                    else
                    {
                        H3MP_Client.autoMeaters[data.trackedID] = null;
                    }

                    H3MP_GameManager.autoMeatersByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                }
            }
            else // trackedID == -1, note that it cannot == -2 because DestroyGlobally will never get called in that case due to skipDestroyProcessing flag
            {
                // Tell destruction we want to keep this in local for later
                removeFromLocal = false;

                // Keep the destruction in unknown so we can send it to others if we get a tracked ID
                if (!unknownDestroyTrackedIDs.Contains(data.localWaitingIndex))
                {
                    unknownDestroyTrackedIDs.Add(data.localWaitingIndex);
                }
            }
        }
    }
}
