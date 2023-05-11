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

        public static List<uint> unknownDestroyTrackedIDs = new List<uint>();
        public static Dictionary<uint, int> unknownControlTrackedIDs = new Dictionary<uint, int>();
        public static Dictionary<uint, KeyValuePair<uint, bool>> unknownTrackedIDs = new Dictionary<uint, KeyValuePair<uint, bool>>();
        public static Dictionary<uint, List<uint>> unknownParentWaitList = new Dictionary<uint, List<uint>>();
        public static Dictionary<uint, List<int>> unknownParentTrackedIDs = new Dictionary<uint, List<int>>();

        public bool awoken; // Whether this object has awoken yet
        public bool sendOnAwake; // Whether to send this object upon awakening

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipDestroyProcessing;
        public bool skipFullDestroy;
        public bool dontGiveControl;

        public virtual void Awake()
        {
            if (data != null)
            {
                data.Update(true);
            }

            awoken = true;
            if (sendOnAwake)
            {
                Mod.LogInfo(gameObject.name + " awoken");
                if (ThreadManager.host)
                {
                    // This will also send a packet with the object to be added in the client's global object list
                    Server.AddTrackedObject(data, 0);
                }
                else
                {
                    // Tell the server we need to add this object to global tracked objects
                    data.localWaitingIndex = Client.localObjectCounter++;
                    Client.waitingLocalObjects.Add(data.localWaitingIndex, data);
                    ClientSend.TrackedObject(data);
                }
            }
        }

        private void OnTransformParentChanged()
        {
            if (data.ignoreParentChanged > 0)
            {
                return;
            }

            Transform currentParent = transform.parent;
            TrackedObject parentTrackedObject = null;
            while (currentParent != null)
            {
                parentTrackedObject = currentParent.GetComponent<TrackedObject>();
                if (parentTrackedObject != null)
                {
                    break;
                }
                currentParent = currentParent.parent;
            }
            if (parentTrackedObject != null)
            {
                // Handle case of unknown tracked IDs
                //      If ours is not yet known, put our waiting index in a wait dict with value as parent's LOCAL tracked ID if it is under our control
                //      and the actual tracked ID if not, when we receive the tracked ID we set the parent
                //          Note that if the parent is under our control, we need to store the local tracked ID because we might not have its tracked ID yet either
                //          If it is not under our control then we have guarantee that is has a tracked ID
                //      If the parent's tracked ID is not yet known, put it in a wait dict where key is the local tracked ID of the parent,
                //      and the value is a list of all children that must be attached to this parent once we know the parent's tracked ID
                //          Note that if we do not know the parent's tracked ID, it is because it is under our control
                bool haveParentID = parentTrackedObject.data.trackedID > -1;
                if (data.trackedID == -1)
                {
                    KeyValuePair<uint, bool> parentIDPair = new KeyValuePair<uint, bool>(haveParentID ? (uint)parentTrackedObject.data.trackedID : parentTrackedObject.data.localWaitingIndex, haveParentID);
                    if (unknownTrackedIDs.ContainsKey(data.localWaitingIndex))
                    {
                        unknownTrackedIDs[data.localWaitingIndex] = parentIDPair;
                    }
                    else
                    {
                        unknownTrackedIDs.Add(data.localWaitingIndex, parentIDPair);
                    }
                    if (!haveParentID)
                    {
                        if (unknownParentWaitList.TryGetValue(parentTrackedObject.data.localWaitingIndex, out List<uint> waitList))
                        {
                            waitList.Add(data.localWaitingIndex);
                        }
                        else
                        {
                            unknownParentWaitList.Add(parentTrackedObject.data.localWaitingIndex, new List<uint>() { data.localWaitingIndex });
                        }
                    }
                }
                else
                {
                    if (haveParentID)
                    {
                        if (parentTrackedObject.data.trackedID != data.parent)
                        {
                            if (data.controller == GameManager.ID)
                            {
                                // We have a parent trackedItem and it is new
                                // Update other clients
                                if (ThreadManager.host)
                                {
                                    ServerSend.ObjectParent(data.trackedID, parentTrackedObject.data.trackedID);
                                }
                                else
                                {
                                    ClientSend.ObjectParent(data.trackedID, parentTrackedObject.data.trackedID);
                                }

                                // Do the following after sending itemParent order in case updateParentFunc is not set for the item and is therefore dependent on 
                                // an update to attach itself to the parent properly
                                // Call an update on the item so we can send latest data considering the new parent
                                data.Update();

                                // Send latest data through TCP to make sure others mount the item properly
                                if (ThreadManager.host)
                                {
                                    ServerSend.ObjectUpdate(data);
                                }
                                else
                                {
                                    ClientSend.ObjectUpdate(data);
                                }
                            }

                            // Update local
                            data.SetParent(parentTrackedObject.data, false);
                        }
                    }
                    else
                    {
                        if (data.controller == GameManager.ID)
                        {
                            if (unknownParentTrackedIDs.ContainsKey(parentTrackedObject.data.localWaitingIndex))
                            {
                                unknownParentTrackedIDs[parentTrackedObject.data.localWaitingIndex].Add(data.trackedID);
                            }
                            else
                            {
                                unknownParentTrackedIDs.Add(parentTrackedObject.data.localWaitingIndex, new List<int>() { data.trackedID });
                            }
                        }
                    }
                }
            }
            else if (data.parent != -1)
            {
                if (data.trackedID == -1)
                {
                    if (data.controller == GameManager.ID)
                    {
                        if (unknownTrackedIDs.TryGetValue(data.localWaitingIndex, out KeyValuePair<uint, bool> entry))
                        {
                            if (!entry.Value && unknownParentWaitList.TryGetValue(entry.Key, out List<uint> waitlist))
                            {
                                waitlist.Remove(data.localWaitingIndex);
                            }
                        }
                        unknownTrackedIDs.Remove(data.localWaitingIndex);
                    }
                }
                else
                {
                    if (data.controller == GameManager.ID)
                    {
                        Mod.LogInfo(name + " was unparented, sending to others");
                        // We were detached from current parent
                        // Update other clients
                        if (ThreadManager.host)
                        {
                            ServerSend.ObjectParent(data.trackedID, -1);
                        }
                        else
                        {
                            ClientSend.ObjectParent(data.trackedID, -1);
                        }
                    }

                    // Update locally
                    data.SetParent(null, false);
                }
            }
        }

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
                                    ServerSend.GiveObjectControl(data.trackedID, otherPlayer, new List<int>() { GameManager.ID });
                                }
                                else
                                {
                                    ClientSend.GiveObjectControl(data.trackedID, otherPlayer, new List<int>() { GameManager.ID });
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
                    if (ThreadManager.host)
                    {
                        ServerSend.DestroyObject(data.trackedID, data.removeFromListOnDestroy);
                    }
                    else
                    {
                        ClientSend.DestroyObject(data.trackedID, data.removeFromListOnDestroy);
                    }
                }

                // Remove from globals lists if we want
                if (data.removeFromListOnDestroy)
                {
                    if (ThreadManager.host)
                    {
                        Server.objects[data.trackedID] = null;
                        Server.availableObjectIndices.Add(data.trackedID);
                    }
                    else
                    {
                        Client.objects[data.trackedID] = null;
                    }

                    // TODO: Customization make this propagate to sub types so we can remove them from their own lists?
                    GameManager.objectsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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

        public virtual void SecondaryDestroy() { }

        public virtual void BeginInteraction(FVRViveHand hand) { }

        public virtual void EndInteraction(FVRViveHand hand) { }

        public virtual void EnsureUncontrolled() { }
    }
}
