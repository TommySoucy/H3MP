using FistVR;
using H3MP.Networking;
using System.Collections.Generic;
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
        public static Dictionary<uint, string> unknownSceneChange = new Dictionary<uint, string>();
        public static Dictionary<uint, int> unknownInstanceChange = new Dictionary<uint, int>();

        // Used by certain objects who need to get access to their TrackedObjects very often (On Update for example)
        // This is used to bypass having to find the objects in a datastructure too often
        public static GameObject[] trackedReferenceObjects = new GameObject[100];
        public static TrackedObject[] trackedReferences = new TrackedObject[100];
        public static List<int> availableTrackedRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                10,11,12,13,14,15,16,17,18,19,
                                                                                20,21,22,23,24,25,26,27,28,29,
                                                                                30,31,32,33,34,35,36,37,38,39,
                                                                                40,41,42,43,44,45,46,47,48,49,
                                                                                50,51,52,53,54,55,56,57,58,59,
                                                                                60,61,62,63,64,65,66,67,68,69,
                                                                                70,71,72,73,74,75,76,77,78,79,
                                                                                80,81,82,83,84,85,86,87,88,89,
                                                                                90,91,92,93,94,95,96,97,98,99};

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

        public virtual void OnTransformParentChanged()
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
            Mod.LogInfo("OnDestroy for object at "+data.trackedID+" with local waiting index: "+data.localWaitingIndex, false);
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            GameManager.trackedObjectByObject.Remove(physical);

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
                    Mod.LogInfo("\tNot giving control", false);
                    DestroyGlobally(ref removeFromLocal);
                }
                else // We want to give control of this object instead of destroying it globally
                {
                    Mod.LogInfo("\tGiving control if we control", false);
                    if (data.controller == GameManager.ID)
                    {
                        Mod.LogInfo("\t\tWe control", false);
                        // Find best potential host
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, GameManager.playersAtLoadStart);
                        if (otherPlayer == -1)
                        {
                            Mod.LogInfo("\t\t\tNo potential host, destroying globally", false);
                            // No other best potential host, destroy globally
                            DestroyGlobally(ref removeFromLocal);
                        }
                        else // We have a potential new host to give control to
                        {
                            Mod.LogInfo("\t\t\tPotential host: "+otherPlayer, false);
                            // Check if can give control
                            if (data.trackedID > -1)
                            {
                                Mod.LogInfo("\t\t\t\tGot trackedID, giving control", false);
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
                                Mod.LogInfo("\t\t\t\tNo trackedID, adding to unknown", false);
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
            Mod.LogInfo("DestroyGlobally for object at " + data.trackedID + " with local waiting index: " + data.localWaitingIndex, false);
            // Check if can destroy globally
            if (data.trackedID > -1)
            {
                Mod.LogInfo("\tGot tracked ID", false);
                // Check if want to send destruction
                // Used to prevent feedback loops
                if (sendDestroy)
                {
                    Mod.LogInfo("\t\tSending destroy", false);
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
                    Mod.LogInfo("\t\tRemoving from lists", false);
                    data.RemoveFromLists();
                }
            }
            else // trackedID == -1, note that it cannot == -2 because DestroyGlobally will never get called in that case due to skipDestroyProcessing flag
            {
                Mod.LogInfo("\tNo tracked ID, adding to unknown", false);
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

        public virtual bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data) { return true; }
    }
}
