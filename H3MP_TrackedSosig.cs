using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedSosig : MonoBehaviour
    {
        public static float interpolationSpeed = 12f;

        public Sosig physicalSosigScript;
        public H3MP_TrackedSosigData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<uint, int> unknownControlTrackedIDs = new Dictionary<uint, int>();
        public static List<uint> unknownDestroyTrackedIDs = new List<uint>();
        public static Dictionary<uint, List<KeyValuePair<int, KeyValuePair<int, int>>>> unknownItemInteractTrackedIDs = new Dictionary<uint, List<KeyValuePair<int, KeyValuePair<int, int>>>>();
        public static Dictionary<uint, int> unknownSetIFFs = new Dictionary<uint, int>();
        public static Dictionary<uint, int> unknownSetOriginalIFFs = new Dictionary<uint, int>();
        public static Dictionary<uint, Sosig.SosigBodyState> unknownBodyStates = new Dictionary<uint, Sosig.SosigBodyState>();
        public static Dictionary<uint, int> unknownTNHKills = new Dictionary<uint, int>();
        public static Dictionary<uint, int> unknownIFFChart = new Dictionary<uint, int>();

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
                    // This will also send a packet with the sosig to be added in the client's global sosig list
                    H3MP_Server.AddTrackedSosig(data, 0);
                }
                else
                {
                    // Tell the server we need to add this item to global tracked sosigs
                    data.localWaitingIndex = H3MP_Client.localSosigCounter++;
                    H3MP_Client.waitingLocalSosigs.Add(data.localWaitingIndex, data);
                    H3MP_ClientSend.TrackedSosig(data);
                }
            }
        }

        private void FixedUpdate()
        {
            if (physicalSosigScript != null && physicalSosigScript.CoreRB != null && data.controller != H3MP_GameManager.ID && data.position != null && data.rotation != null)
            {
                // NOTE: The velocity magnitude check must be greater than the largest displacement a sosig is able to have in a single fixed frame
                //       (meaning if a sosig moves normally but this normal movement ends up being of more than the threshold in a single frame,
                //       the sosig will be teleported instead of interpolated although interpolation was intended)
                //       while being smaller than the smallest intended teleportation (if there is a sosig teleportation that happens for any reason, for example
                //       a teleportation sosig off mesh link, and this teleportation is less than the threshold, the sosig will instead be interpolated, which could lead to them
                //       trying to move through a wall instead of teleporting through it)
                // Here, for a value of 0.5f, we mean that a sosig should never move more than 0.5m in a single frame, and should never teleport less than 0.5m
                if (data.previousPos != null && data.velocity.magnitude < 0.5f)
                {
                    Vector3 newPosition = Vector3.Lerp(physicalSosigScript.CoreRB.position, data.position + data.velocity, interpolationSpeed * Time.deltaTime);
                    physicalSosigScript.Agent.transform.position = newPosition;
                    physicalSosigScript.CoreRB.position = newPosition;
                }
                else
                {
                    physicalSosigScript.CoreRB.position = data.position;
                }
                physicalSosigScript.CoreRB.rotation = Quaternion.Lerp(physicalSosigScript.CoreRB.rotation, data.rotation, interpolationSpeed * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (skipFullDestroy)
            {
                return;
            }

            H3MP_GameManager.trackedSosigBySosig.Remove(physicalSosigScript);

            // Only call sosig dies if not scene loading, otherwise their body get destroyed before the script(?) and we get null refs 
            // TODO: Review: We might want to instead just check if their body still exists, and only call SosigDies if it does
            if (!H3MP_GameManager.sceneLoading)
            {
                // Set sosig as dead before destroying so it gets processed properly
                ++SosigActionPatch.sosigDiesSkip;
                physicalSosigScript.SosigDies(Damage.DamageClass.Abstract, Sosig.SosigDeathType.Unknown);
                --SosigActionPatch.sosigDiesSkip;
            }

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed > 0)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, H3MP_GameManager.playersAtLoadStart);

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy)
                            {
                                H3MP_ServerSend.DestroySosig(data.trackedID);
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.removeFromListOnDestroy && H3MP_Server.sosigs[data.trackedID] != null)
                            {
                                H3MP_Server.sosigs[data.trackedID] = null;
                                H3MP_Server.availableSosigIndices.Add(data.trackedID);
                                H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            H3MP_ServerSend.GiveSosigControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

                            // Also change controller locally
                            data.controller = otherPlayer;
                        }
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroySosig(data.trackedID);
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if (data.removeFromListOnDestroy && H3MP_Server.sosigs[data.trackedID] != null)
                    {
                        H3MP_Server.sosigs[data.trackedID] = null;
                        H3MP_Server.availableSosigIndices.Add(data.trackedID);
                        H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
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
                                else if (data.trackedID != -2)
                                {
                                    H3MP_ClientSend.DestroySosig(data.trackedID);

                                    H3MP_Client.sosigs[data.trackedID] = null;
                                    H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                                }
                            }
                            else
                            {
                                sendDestroy = true;
                            }

                            if (data.trackedID != -1 && data.trackedID != -2)
                            {
                                H3MP_Client.sosigs[data.trackedID] = null;
                                H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
                                H3MP_ClientSend.GiveSosigControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

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
                            H3MP_ClientSend.DestroySosig(data.trackedID);

                            if (data.removeFromListOnDestroy)
                            {
                                H3MP_Client.sosigs[data.trackedID] = null;
                                H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                    }
                    else
                    {
                        sendDestroy = true;
                    }

                    if (data.removeFromListOnDestroy && data.trackedID != -1 && data.trackedID != -2)
                    {
                        H3MP_Client.sosigs[data.trackedID] = null;
                        H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
