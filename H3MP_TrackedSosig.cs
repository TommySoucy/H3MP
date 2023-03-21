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
        public static Dictionary<uint, Sosig.SosigOrder> unknownCurrentOrder = new Dictionary<uint, Sosig.SosigOrder>();
        public static Dictionary<uint, SosigConfigTemplate> unknownConfiguration = new Dictionary<uint, SosigConfigTemplate>();

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
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Call SosigDies so it can be processed by the game properly
            // Only call sosig dies if not scene loading, otherwise their body get destroyed before the script(?) and we get null refs 
            // TODO: Review: We might want to instead just check if their body still exists, and only call SosigDies if it does
            if (!H3MP_GameManager.sceneLoading)
            {
                // Set sosig as dead before destroying so it gets processed properly
                ++SosigActionPatch.sosigDiesSkip;
                physicalSosigScript.SosigDies(Damage.DamageClass.Abstract, Sosig.SosigDeathType.Unknown);
                --SosigActionPatch.sosigDiesSkip;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.trackedSosigBySosig.Remove(physicalSosigScript);

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            for(int i=0; i < physicalSosigScript.Links.Count; ++i)
            {
                if (physicalSosigScript.Links[i] != null)
                {
                    H3MP_GameManager.EnsureUncontrolled(physicalSosigScript.Links[i].O);
                }
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
                                H3MP_ServerSend.GiveSosigControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });

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
                        H3MP_ServerSend.DestroySosig(data.trackedID, data.removeFromListOnDestroy);
                    }
                    else
                    {
                        H3MP_ClientSend.DestroySosig(data.trackedID, data.removeFromListOnDestroy);
                    }
                }

                // Remove from globals lists if we want
                if (data.removeFromListOnDestroy)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_Server.sosigs[data.trackedID] = null;
                        H3MP_Server.availableSosigIndices.Add(data.trackedID);
                    }
                    else
                    {
                        H3MP_Client.sosigs[data.trackedID] = null;
                    }

                    H3MP_GameManager.sosigsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
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
