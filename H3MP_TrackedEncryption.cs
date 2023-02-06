using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP
{
    public class H3MP_TrackedEncryption : MonoBehaviour
    {
        public TNH_EncryptionTarget physicalEncryptionScript;
        public H3MP_TrackedEncryptionData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<int, int> unknownControlTrackedIDs = new Dictionary<int, int>();
        public static List<int> unknownDestroyTrackedIDs = new List<int>();
        public static Dictionary<int, List<int>> unknownInit = new Dictionary<int, List<int>>();
        public static Dictionary<int, List<int>> unknownSpawnSubTarg = new Dictionary<int, List<int>>();
        public static Dictionary<int, List<int>> unknownDisableSubTarg = new Dictionary<int, List<int>>();
        public static Dictionary<int, List<KeyValuePair<int, Vector3>>> unknownSpawnGrowth = new Dictionary<int, List<KeyValuePair<int, Vector3>>>();
        public static Dictionary<int, List<KeyValuePair<int, Vector3>>> unknownResetGrowth = new Dictionary<int, List<KeyValuePair<int, Vector3>>>();

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipFullDestroy;

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
                    H3MP_ClientSend.TrackedEncryption(data);
                }
            }

            TNH_EncryptionTarget targetScript = GetComponent<TNH_EncryptionTarget>();
            if (targetScript.SpawnPoints == null)
            {
                targetScript.SpawnPoints = new List<Transform>();
            }
            GameObject trackedItemRef = new GameObject("TrackedEncryptionRef");
            if (availableTrackedEncryptionRefIndices.Count == 0)
            {
                H3MP_TrackedEncryption[] tempItems = trackedEncryptionReferences;
                trackedEncryptionReferences = new H3MP_TrackedEncryption[tempItems.Length + 100];
                for (int i = 0; i < tempItems.Length; ++i)
                {
                    trackedEncryptionReferences[i] = tempItems[i];
                }
                for (int i = tempItems.Length; i < trackedEncryptionReferences.Length; ++i)
                {
                    availableTrackedEncryptionRefIndices.Add(i);
                }
            }
            trackedEncryptionReferences[availableTrackedEncryptionRefIndices.Count - 1] = this;
            trackedItemRef.hideFlags = HideFlags.HideAndDontSave + availableTrackedEncryptionRefIndices[availableTrackedEncryptionRefIndices.Count - 1];
            availableTrackedEncryptionRefIndices.RemoveAt(availableTrackedEncryptionRefIndices.Count - 1);
            targetScript.SpawnPoints.Add(trackedItemRef.transform);
        }

        private void OnDestroy()
        {
            if (skipFullDestroy)
            {
                return;
            }

            H3MP_GameManager.trackedEncryptionByEncryption.Remove(physicalEncryptionScript);

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our Encryptions to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0 && H3MP_GameManager.TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance actualInstance))
                    {
                        int otherPlayer = -1;
                        for(int i=0; i < actualInstance.currentlyPlaying.Count; ++i)
                        {
                            if (actualInstance.currentlyPlaying[i] != H3MP_GameManager.ID)
                            {
                                otherPlayer = actualInstance.currentlyPlaying[i];
                                break;
                            }
                        }

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy)
                            {
                                H3MP_ServerSend.DestroyEncryption(data.trackedID);
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.removeFromListOnDestroy && H3MP_Server.encryptions[data.trackedID] != null)
                            {
                                H3MP_Server.encryptions[data.trackedID] = null;
                                H3MP_Server.availableEncryptionIndices.Add(data.trackedID);
                                H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            H3MP_ServerSend.GiveEncryptionControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroyEncryption(data.trackedID);
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if (data.removeFromListOnDestroy && H3MP_Server.encryptions[data.trackedID] != null)
                    {
                        H3MP_Server.encryptions[data.trackedID] = null;
                        H3MP_Server.availableEncryptionIndices.Add(data.trackedID);
                        H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.encryptions[data.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                    H3MP_GameManager.encryptions[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
                    data.localTrackedID = -1;
                }
            }
            else
            {
                bool removeFromLocal = true;
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID && H3MP_GameManager.TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance actualInstance))
                    {
                        int otherPlayer = -1;
                        for (int i = 0; i < actualInstance.currentlyPlaying.Count; ++i)
                        {
                            if (actualInstance.currentlyPlaying[i] != H3MP_GameManager.ID)
                            {
                                otherPlayer = actualInstance.currentlyPlaying[i];
                                break;
                            }
                        }

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
                                    H3MP_ClientSend.DestroyEncryption(data.trackedID);

                                    H3MP_Client.encryptions[data.trackedID] = null;
                                    H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                                }
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.trackedID != -1)
                            {
                                H3MP_Client.encryptions[data.trackedID] = null;
                                H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
                                H3MP_ClientSend.GiveEncryptionControl(data.trackedID, otherPlayer);

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
                            H3MP_ClientSend.DestroyEncryption(data.trackedID);

                            if (data.removeFromListOnDestroy)
                            {
                                H3MP_Client.encryptions[data.trackedID] = null;
                                H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if(data.removeFromListOnDestroy && data.trackedID != -1)
                    {
                        H3MP_Client.encryptions[data.trackedID] = null;
                        H3MP_GameManager.encryptionsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
