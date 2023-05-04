using FistVR;
using H3MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP.Tracking
{
    public abstract class TrackedObject: MonoBehaviour
    {
        public TrackedObjectData data; // The data corresponding to this tracked object
        public MonoBehaviour physical; // The physical component corresponding to this tracked object

        public bool awoken; // Whether this object has awoken yet
        public bool sendOnAwake; // Whether to send this object upon awakening

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipDestroyProcessing;
        public bool skipFullDestroy;
        public bool dontGiveControl;

        protected virtual void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Have a flag in case we don't actually want to remove it from local after processing
            // In case we can't detroy gobally because we are still waiting for a tracked ID for example
            bool removeFromLocal = true;

            // Check if we want to process sending, giving control, etc.
            // We might want to skip just this part if the object was refused by the server
            if (!skipDestroyProcessing)
            {
                // Check if we want to give control of any destroyed objects
                // This would be the case while we change scene, objects will be destroyed but if there are other clients
                // in our previous scene/instance, we don't want to destroy the object globally, we want to give control of it to one of them
                // We might receive an order to destroy an object while we have giveControlOfDestroyed > 0, if so dontGiveControl flag 
                // explicitly says to destroy
                if (GameManager.giveControlOfDestroyed == 0 || dontGiveControl)
                {
                    DestroyGlobally(ref removeFromLocal);
                }
                else // We want to give control of this object instead of destroying it globally
                {
                    if (data.controller == GameManager.ID)
                    {
                        // Find best potential host
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, GameManager.playersAtLoadStart);
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
                                if (ThreadManager.host)
                                {
                                    ServerSend.GiveControl(data.trackedID, otherPlayer, new List<int>() { GameManager.ID });
                                }
                                else
                                {
                                    ClientSend.GiveControl(data.trackedID, otherPlayer, new List<int>() { GameManager.ID });
                                }

                                // Also change controller locally
                                data.SetController(otherPlayer);
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
    }
}
