using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSosig : TrackedObject
    {
        public static float interpolationSpeed = 12f;

        public Sosig physicalSosig;
        public TrackedSosigData sosigData;

        // Unknown tracked ID queues
        public static Dictionary<uint, List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>> unknownItemInteract = new Dictionary<uint, List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>>();
        public static Dictionary<uint, int> unknownSetIFFs = new Dictionary<uint, int>();
        public static Dictionary<uint, int> unknownSetOriginalIFFs = new Dictionary<uint, int>();
        public static Dictionary<uint, Sosig.SosigBodyState> unknownBodyStates = new Dictionary<uint, Sosig.SosigBodyState>();
        public static Dictionary<uint, int> unknownTNHKills = new Dictionary<uint, int>();
        public static Dictionary<uint, int> unknownIFFChart = new Dictionary<uint, int>();
        public static Dictionary<uint, Sosig.SosigOrder> unknownCurrentOrder = new Dictionary<uint, Sosig.SosigOrder>();
        public static Dictionary<uint, SosigConfigTemplate> unknownConfiguration = new Dictionary<uint, SosigConfigTemplate>();
        public static Dictionary<uint, Dictionary<string, List<int>>> unknownWearable = new Dictionary<uint, Dictionary<string, List<int>>>();

        public override void Awake()
        {
            base.Awake();

            GameManager.OnInstanceJoined += OnInstanceJoined;

            GameObject trackedSosigRef = new GameObject();
            Scripts.TrackedObjectReference refScript = trackedSosigRef.AddComponent<Scripts.TrackedObjectReference>();
            trackedSosigRef.SetActive(false);

            CheckReferenceSize();
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferenceObjects[refIndex] = trackedSosigRef;
            trackedReferences[refIndex] = this;
            trackedSosigRef.name = refIndex.ToString();
            refScript.refIndex = refIndex;
            physicalSosig = GetComponent<Sosig>();
            GameObject[] temp = physicalSosig.BuffSystems;
            physicalSosig.BuffSystems = new GameObject[temp.Length + 1];
            for(int i=0; i < temp.Length; ++i)
            {
                physicalSosig.BuffSystems[i] = temp[i];
            }
            physicalSosig.BuffSystems[physicalSosig.BuffSystems.Length - 1] = trackedSosigRef;
        }

        private void FixedUpdate()
        {
            if (physicalSosig != null && physicalSosig.CoreRB != null && data.controller != GameManager.ID && sosigData.position != null && sosigData.rotation != null)
            {
                // NOTE: The velocity magnitude check must be greater than the largest displacement a sosig is able to have in a single fixed frame
                //       (meaning if a sosig moves normally but this normal movement ends up being of more than the threshold in a single frame,
                //       the sosig will be teleported instead of interpolated although interpolation was intended)
                //       while being smaller than the smallest intended teleportation (if there is a sosig teleportation that happens for any reason, for example
                //       a teleportation sosig off mesh link, and this teleportation is less than the threshold, the sosig will instead be interpolated, which could lead to them
                //       trying to move through a wall instead of teleporting through it)
                // Here, for a value of 0.5f, we mean that a sosig should never move more than 0.5m in a single frame, and should never teleport less than 0.5m
                if (sosigData.previousPos != null && sosigData.velocity.magnitude < 0.5f)
                {
                    Vector3 newPosition = Vector3.Lerp(physicalSosig.CoreRB.position, sosigData.position + sosigData.velocity, interpolationSpeed * Time.deltaTime);
                    physicalSosig.Agent.transform.position = newPosition;
                    physicalSosig.CoreRB.position = newPosition;
                }
                else
                {
                    physicalSosig.CoreRB.position = sosigData.position;
                }
                physicalSosig.CoreRB.rotation = Quaternion.Lerp(physicalSosig.CoreRB.rotation, sosigData.rotation, interpolationSpeed * Time.deltaTime);
            }
        }

        public override void EnsureUncontrolled()
        {
            for (int i = 0; i < physicalSosig.Links.Count; ++i)
            {
                if (physicalSosig.Links[i] != null && physicalSosig.Links[i].O.m_hand != null)
                {
                    physicalSosig.Links[i].O.ForceBreakInteraction();
                }
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnInstanceJoined -= OnInstanceJoined;

            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Call SosigDies so it can be processed by the game properly
            // Only call sosig dies if not scene loading, otherwise their body get destroyed before the script(?) and we get null refs 
            // TODO: Review: We might want to instead just check if their body still exists, and only call SosigDies if it does
            if (!GameManager.sceneLoading)
            {
                // Set sosig as dead before destroying so it gets processed properly
                ++SosigPatch.sosigDiesSkip;
                physicalSosig.SosigDies(Damage.DamageClass.Abstract, Sosig.SosigDeathType.Unknown);
                --SosigPatch.sosigDiesSkip;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            GameManager.trackedSosigBySosig.Remove(physicalSosig);
            for (int i = 0; i < physicalSosig.Links.Count; ++i)
            {
                GameManager.trackedObjectByInteractive.Remove(physicalSosig.Links[i].O);
                GameManager.trackedObjectByDamageable.Remove(physicalSosig.Links[i]);
            }

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            EnsureUncontrolled();

            base.OnDestroy();
        }

        public override void SecondaryDestroy()
        {
            foreach (SosigLink link in physicalSosig.Links)
            {
                if (link != null)
                {
                    GameObject.Destroy(link.gameObject);
                }
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GiveObjectControl(data.trackedID, GameManager.ID, null);
                }
                else
                {
                    ClientSend.GiveObjectControl(data.trackedID, GameManager.ID, null);
                }

                data.controller = GameManager.ID;
                data.localTrackedID = GameManager.objects.Count;
                GameManager.objects.Add(data);

                sosigData.TakeInventoryControl();
            }
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            // Need to make sure that we give control of the sosig back to the controller of a the current TNH instance if there is one
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GiveObjectControl(data.trackedID, Mod.currentTNHInstance.controller, null);
                }
                else
                {
                    ClientSend.GiveObjectControl(data.trackedID, Mod.currentTNHInstance.controller, null);
                }

                // Update locally
                data.RemoveFromLocal();
            }
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            // Since Sosigs can't go across scenes, we only process an instance change if we are not currently loading into a new scene
            if (!GameManager.sceneLoading)
            {
                TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
                data.ShouldBring(false, ref bring);

                ++GameManager.giveControlOfDestroyed;

                if (bring == TrackedObjectData.ObjectBringType.Yes)
                {
                    // Want to bring everything with us
                    // What we are interacting with, we will bring with us completely, destroying it on remote sides
                    // Whet we do not interact with, we will make a copy of in the new instance
                    if (data.IsControlled(out int interactionID))
                    {
                        data.SetInstance(instance, true);
                    }
                    else // Not interacting with
                    {
                        DestroyImmediate(this);

                        GameManager.SyncTrackedObjects(transform, true, null);
                    }
                }
                else if (bring == TrackedObjectData.ObjectBringType.OnlyInteracted && data.IsControlled(out int interactionID))
                {
                    data.SetInstance(instance, true);
                }
                else // Don't want to bring, destroy
                {
                    DestroyImmediate(gameObject);
                }

                --GameManager.giveControlOfDestroyed;
            }
        }
    }
}
