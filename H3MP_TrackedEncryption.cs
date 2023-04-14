using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedEncryption : MonoBehaviour
    {
        public TNH_EncryptionTarget physicalEncryptionScript;
        public H3MP_TrackedEncryptionData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<uint, int> unknownControlTrackedIDs = new Dictionary<uint, int>();
        public static List<uint> unknownDestroyTrackedIDs = new List<uint>();
        public static Dictionary<uint, KeyValuePair<List<int>, List<Vector3>>> unknownInit = new Dictionary<uint, KeyValuePair<List<int>, List<Vector3>>>();
        public static Dictionary<uint, List<int>> unknownSpawnSubTarg = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, List<int>> unknownDisableSubTarg = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, List<KeyValuePair<int, Vector3>>> unknownSpawnGrowth = new Dictionary<uint, List<KeyValuePair<int, Vector3>>>();
        public static Dictionary<uint, List<KeyValuePair<int, Vector3>>> unknownResetGrowth = new Dictionary<uint, List<KeyValuePair<int, Vector3>>>();

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipDestroyProcessing;
        public bool skipFullDestroy;
        public bool dontGiveControl;

        // TrackedEncryptionReferences array
        // Used by Encryptions who need to get access to their TrackedItem very often (On Update for example)
        // This is used to bypass having to find the item in a datastructure too often
        public static H3MP_TrackedEncryption[] trackedEncryptionReferences = new H3MP_TrackedEncryption[100];
        public static List<int> availableTrackedEncryptionRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                        10,11,12,13,14,15,16,17,18,19,
                                                                                        20,21,22,23,24,25,26,27,28,29,
                                                                                        30,31,32,33,34,35,36,37,38,39,
                                                                                        40,41,42,43,44,45,46,47,48,49,
                                                                                        50,51,52,53,54,55,56,57,58,59,
                                                                                        60,61,62,63,64,65,66,67,68,69,
                                                                                        70,71,72,73,74,75,76,77,78,79,
                                                                                        80,81,82,83,84,85,86,87,88,89,
                                                                                        90,91,92,93,94,95,96,97,98,99};

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
                    // This will also send a packet with the Encryption to be added in the client's global Encryption list
                    H3MP_Server.AddTrackedEncryption(data, 0);
                }
                else
                {
                    // Tell the server we need to add this item to global tracked Encryptions
                    data.localWaitingIndex = H3MP_Client.localEncryptionCounter++;
                    H3MP_Client.waitingLocalEncryptions.Add(data.localWaitingIndex, data);
                    H3MP_ClientSend.TrackedEncryption(data);
                }
            }

            TNH_EncryptionTarget targetScript = GetComponent<TNH_EncryptionTarget>();
            if (targetScript.SpawnPoints == null)
            {
                targetScript.SpawnPoints = new List<Transform>();
            }
            GameObject trackedEncryptionRef = new GameObject();
            trackedEncryptionRef.SetActive(false);
            if (availableTrackedEncryptionRefIndices.Count == 0)
            {
                H3MP_TrackedEncryption[] tempEncryptions = trackedEncryptionReferences;
                trackedEncryptionReferences = new H3MP_TrackedEncryption[tempEncryptions.Length + 100];
                for (int i = 0; i < tempEncryptions.Length; ++i)
                {
                    trackedEncryptionReferences[i] = tempEncryptions[i];
                }
                for (int i = tempEncryptions.Length; i < trackedEncryptionReferences.Length; ++i)
                {
                    availableTrackedEncryptionRefIndices.Add(i);
                }
            }
            int refIndex = availableTrackedEncryptionRefIndices[availableTrackedEncryptionRefIndices.Count - 1];
            availableTrackedEncryptionRefIndices.RemoveAt(availableTrackedEncryptionRefIndices.Count - 1);
            trackedEncryptionReferences[refIndex] = this;
            trackedEncryptionRef.name = refIndex.ToString();
            targetScript.SpawnPoints.Add(trackedEncryptionRef.transform);
        }

        private void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Type specific destruction
            // In the case of encryptions we want to make sure the tendrils and subtargs are also destroyed because they usually are in TNH_EncryptionTarget.Destroy
            // but this will not have been called if we are not the one to have destroyed it
            if(data.controller != H3MP_GameManager.ID && physicalEncryptionScript.UsesRegenerativeSubTarg)
            {
                for (int i = 0; i < physicalEncryptionScript.Tendrils.Count; i++)
                {
                    Destroy(physicalEncryptionScript.Tendrils[i]);
                    Destroy(physicalEncryptionScript.SubTargs[i]);
                }
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.trackedEncryptionByEncryption.Remove(physicalEncryptionScript);

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
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.GiveEncryptionControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });
                                }
                                else
                                {
                                    H3MP_ClientSend.GiveEncryptionControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });
                                }

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
                // We want skip just sending if we know we are the only one with this object, ex.: We destroying because server refused tracking it
                if (sendDestroy)
                {
                    // Send destruction
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.DestroyEncryption(data.trackedID, data.removeFromListOnDestroy);
                    }
                    else
                    {
                        H3MP_ClientSend.DestroyEncryption(data.trackedID, data.removeFromListOnDestroy);
                    }
                }

                // Remove from globals lists if we want
                // We might not want like in the case of the object only being ordered to be destroyed on our side because
                // a client brought it along with them when changing instance
                if (data.removeFromListOnDestroy)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_Server.encryptions[data.trackedID] = null;
                        H3MP_Server.availableEncryptionIndices.Add(data.trackedID);
                    }
                    else
                    {
                        H3MP_Client.encryptions[data.trackedID] = null;
                    }

                    H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
