using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedSosigData : TrackedObjectData
    {
        public bool latestUpdateSent = false; // Whether the latest update of this data was sent
        public byte order; // The index of this sosig's data packet used to ensure we process this data in the correct order

        public int trackedID = -1;
        public int controller;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity = Vector3.zero;
        public int[] previousAmmoStores;
        public int[] ammoStores;
        public float[] previousLinkIntegrity;
        public float[] linkIntegrity;
        public float previousMustard;
        public float mustard;
        public SosigConfigTemplate configTemplate;
        public TrackedSosig physicalObject;
        public int localTrackedID;
        public uint localWaitingIndex = uint.MaxValue;
        public int initTracker;
        public bool previousActive;
        public bool active;
        public List<List<string>> wearables;
        public float[][] linkData;
        public byte IFF;
        public bool[] IFFChart;
        public Sosig.SosigBodyPose previousBodyPose;
        public Sosig.SosigBodyPose bodyPose;
        public Sosig.SosigOrder previousOrder;
        public Sosig.SosigOrder currentOrder;
        public Sosig.SosigOrder fallbackOrder;
        public bool removeFromListOnDestroy = true;
        public string scene;
        public int instance;
        public byte[] data;
        public bool sceneInit;
        public bool awaitingInstantiation;
        public Vector3 guardPoint;
        public Vector3 guardDir;
        public bool hardGuard;
        public Vector3 skirmishPoint;
        public Vector3 pathToPoint;
        public Vector3 assaultPoint;
        public Vector3 faceTowards;
        public Vector3 wanderPoint;
        public Sosig.SosigMoveSpeed assaultSpeed;
        public Vector3 idleToPoint;
        public Vector3 idleDominantDir;
        public Vector3 pathToLookDir;
        public int[] inventory; // 0 and 1 primary and other hand, 2..n inventory slots

        public static KeyValuePair<int, TNH_Manager.SosigPatrolSquad> latestSosigPatrolSquad = new KeyValuePair<int, TNH_Manager.SosigPatrolSquad>(-1, null);

        public IEnumerator Instantiate()
        {
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            if (sosigPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate sosig sent from {controller} but failed to get prefab.");
                awaitingInstantiation = false;
                yield break;
            }

            if (!awaitingInstantiation)
            {
                yield break;
            }

            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at sosig instantiation, setting to 1"); Mod.skipAllInstantiates = 1; }
            GameObject sosigInstance = GameObject.Instantiate(sosigPrefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalObject = sosigInstance.AddComponent<TrackedSosig>();
            awaitingInstantiation = false;
            physicalObject.data = this;

            physicalObject.physicalSosigScript = sosigInstance.GetComponent<Sosig>();
            SosigConfigurePatch.skipConfigure = true;
            physicalObject.physicalSosigScript.Configure(configTemplate);

            GameManager.trackedSosigBySosig.Add(physicalObject.physicalSosigScript, physicalObject);

            AnvilManager.Run(EquipWearables());

            // Deregister the AI from the manager if we are not in control
            // Also set CoreRB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosigScript.E);
                }
                physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
            }

            // Initially set IFF
            ++SosigIFFPatch.skip;
            physicalObject.physicalSosigScript.SetIFF(IFF);
            --SosigIFFPatch.skip;

            // Set IFFChart
            physicalObject.physicalSosigScript.Priority.IFFChart = IFFChart;

            // Initially set order
            switch (currentOrder)
            {
                case Sosig.SosigOrder.GuardPoint:
                    physicalObject.physicalSosigScript.CommandGuardPoint(guardPoint, hardGuard);
                    physicalObject.physicalSosigScript.m_guardDominantDirection = guardDir;
                    break;
                case Sosig.SosigOrder.Skirmish:
                    physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                    physicalObject.physicalSosigScript.m_skirmishPoint = skirmishPoint;
                    physicalObject.physicalSosigScript.m_pathToPoint = pathToPoint;
                    physicalObject.physicalSosigScript.m_assaultPoint = assaultPoint;
                    physicalObject.physicalSosigScript.m_faceTowards = faceTowards;
                    break;
                case Sosig.SosigOrder.Investigate:
                    physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                    physicalObject.physicalSosigScript.UpdateGuardPoint(guardPoint);
                    physicalObject.physicalSosigScript.m_hardGuard = hardGuard;
                    physicalObject.physicalSosigScript.m_faceTowards = faceTowards;
                    break;
                case Sosig.SosigOrder.SearchForEquipment:
                case Sosig.SosigOrder.Wander:
                    physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                    physicalObject.physicalSosigScript.m_wanderPoint = wanderPoint;
                    break;
                case Sosig.SosigOrder.Assault:
                    physicalObject.physicalSosigScript.CommandAssaultPoint(assaultPoint);
                    physicalObject.physicalSosigScript.m_faceTowards = faceTowards;
                    physicalObject.physicalSosigScript.SetAssaultSpeed(assaultSpeed);
                    break;
                case Sosig.SosigOrder.Idle:
                    physicalObject.physicalSosigScript.CommandIdle(idleToPoint, idleDominantDir);
                    break;
                case Sosig.SosigOrder.PathTo:
                    physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                    physicalObject.physicalSosigScript.m_pathToPoint = pathToPoint;
                    physicalObject.physicalSosigScript.m_pathToLookDir = pathToLookDir;
                    break;
                default:
                    physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                    break;
            }
            physicalObject.physicalSosigScript.FallbackOrder = fallbackOrder;

            // Setup inventory
            // Make sure sosig hands and inventory are initialized first
            physicalObject.physicalSosigScript.InitHands();
            physicalObject.physicalSosigScript.Inventory.Init();
            TrackedItemData[] arrToUse = ThreadManager.host ? Server.items : Client.items;
            ++SosigPickUpPatch.skip;
            ++SosigPlaceObjectInPatch.skip;
            for (int i=0; i < inventory.Length; ++i)
            {
                if (inventory[i] != -1)
                {
                    if (arrToUse[inventory[i]] == null)
                    {
                        Mod.LogError("Sosig instantiation: inventory[" + i + "] = "+ inventory[i] + " is missing item data!");
                    }
                    else if(arrToUse[inventory[i]].physicalItem == null)
                    {
                        arrToUse[inventory[i]].toPutInSosigInventory = new int[] { trackedID, i };
                    }
                    else
                    {
                        if (i == 0)
                        {
                            physicalObject.physicalSosigScript.Hand_Primary.PickUp(((SosigWeaponPlayerInterface)arrToUse[inventory[i]].physicalItem.physicalObject).W);
                        }
                        else if (i == 1)
                        {
                            physicalObject.physicalSosigScript.Hand_Secondary.PickUp(((SosigWeaponPlayerInterface)arrToUse[inventory[i]].physicalItem.physicalObject).W);
                        }
                        else
                        {
                            physicalObject.physicalSosigScript.Inventory.Slots[i - 2].PlaceObjectIn(((SosigWeaponPlayerInterface)arrToUse[inventory[i]].physicalItem.physicalObject).W);
                        }
                    }
                }
            }
            --SosigPickUpPatch.skip;
            --SosigPlaceObjectInPatch.skip;

            ProcessData();

            // Initially set itself
            Update(this);
        }

        // MOD: This will be called at the end of instantiation so mods can use it to process the data array
        //      Example here is data about the TNH context
        private void ProcessData()
        {
            if (GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                if (data[0] == 1) // TNH_HoldPoint is in spawn enemy group
                {
                    GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_activeSosigs.Add(physicalObject.physicalSosigScript);
                }
                else if (data[1] == 1) // TNH_SupplyPoint is in Spawn Take Enemy Group
                {
                    GM.TNH_Manager.SupplyPoints[BitConverter.ToInt16(data, 2)].m_activeSosigs.Add(physicalObject.physicalSosigScript);
                }
                else if (data[4] == 1 || data[5] == 1) // TNH_Manager is in generate patrol
                {
                    physicalObject.physicalSosigScript.SetAssaultSpeed(Sosig.SosigMoveSpeed.Walking);
                    int patrolIndex = BitConverter.ToInt16(data, 6);
                    if (latestSosigPatrolSquad.Key == patrolIndex)
                    {
                        latestSosigPatrolSquad.Value.Squad.Add(physicalObject.physicalSosigScript);
                    }
                    else
                    {
                        latestSosigPatrolSquad = new KeyValuePair<int, TNH_Manager.SosigPatrolSquad>(patrolIndex, new TNH_Manager.SosigPatrolSquad());
                        latestSosigPatrolSquad.Value.PatrolPoints = new List<Vector3>();
                        int pointCount = data[8];
                        for (int i = 0; i < pointCount; ++i)
                        {
                            int firstIndex = i * 12 + 9;
                            latestSosigPatrolSquad.Value.PatrolPoints.Add(new Vector3(BitConverter.ToSingle(data, firstIndex),
                                                                                      BitConverter.ToSingle(data, firstIndex + 4),
                                                                                      BitConverter.ToSingle(data, firstIndex + 8)));
                        }
                        latestSosigPatrolSquad.Value.Squad.Add(physicalObject.physicalSosigScript);
                        GM.TNH_Manager.m_patrolSquads.Add(latestSosigPatrolSquad.Value);
                    }
                }
            }
        }

        private IEnumerator EquipWearables()
        {
            if (wearables != null)
            {
                for (int i = 0; i < wearables.Count; ++i)
                {
                    for (int j = 0; j < wearables[i].Count; ++j)
                    {
                        if (IM.OD.ContainsKey(wearables[i][j]))
                        {
                            yield return IM.OD[wearables[i][j]].GetGameObjectAsync();
                            ++Mod.skipAllInstantiates;
                            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at equipwearbles, setting to 1"); Mod.skipAllInstantiates = 1; }
                            GameObject outfitItemObject = GameObject.Instantiate(IM.OD[wearables[i][j]].GetGameObject(), physicalObject.physicalSosigScript.Links[i].transform.position, physicalObject.physicalSosigScript.Links[i].transform.rotation, physicalObject.physicalSosigScript.Links[i].transform);
                            --Mod.skipAllInstantiates;
                            SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                            ++SosigLinkActionPatch.skipRegisterWearable;
                            wearableScript.RegisterWearable(physicalObject.physicalSosigScript.Links[i]);
                            --SosigLinkActionPatch.skipRegisterWearable;
                        }
                        else
                        {
                            Mod.LogWarning("TrackedSosigData.EquipWearables: Wearable "+ wearables[i][j]+" not found in OD");
                        }
                    }
                }
            }
            yield break;
        }

        public IEnumerator EquipWearable(int linkIndex, string ID, bool skip = false)
        {
            if (IM.OD.ContainsKey(ID))
            {
                yield return IM.OD[ID].GetGameObjectAsync();
                if(physicalObject == null || physicalObject.physicalSosigScript.Links[linkIndex] == null)
                {
                    // Sosig or sosig link could have been destroyed between iterations
                    yield break;
                }
                ++Mod.skipAllInstantiates;
                if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at equipwearable, setting to 1"); Mod.skipAllInstantiates = 1; }
                GameObject outfitItemObject = GameObject.Instantiate(IM.OD[ID].GetGameObject(), physicalObject.physicalSosigScript.Links[linkIndex].transform.position, physicalObject.physicalSosigScript.Links[linkIndex].transform.rotation, physicalObject.physicalSosigScript.Links[linkIndex].transform);
                --Mod.skipAllInstantiates;
                SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                if (skip)
                {
                    ++SosigLinkActionPatch.skipRegisterWearable;
                }
                wearableScript.RegisterWearable(physicalObject.physicalSosigScript.Links[linkIndex]);
                if (skip)
                {
                    --SosigLinkActionPatch.skipRegisterWearable;
                }
            }
            else
            {
                Mod.LogWarning("TrackedSosigData.EquipWearables: Wearable " + ID + " not found in OD");
            }
            yield break;
        }

        public void Update(TrackedSosigData updatedItem, bool full = false)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedItem.rotation;
            previousAmmoStores = ammoStores;
            ammoStores = updatedItem.ammoStores;
            previousActive = active;
            active = updatedItem.active;
            previousMustard = mustard;
            mustard = updatedItem.mustard;
            previousLinkIntegrity = linkIntegrity;
            linkIntegrity = updatedItem.linkIntegrity;
            previousBodyPose = bodyPose;
            bodyPose = updatedItem.bodyPose;
            fallbackOrder = updatedItem.fallbackOrder;
            previousOrder = currentOrder;
            currentOrder = updatedItem.currentOrder;

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalSosigScript.FallbackOrder = fallbackOrder;
                physicalObject.physicalSosigScript.Mustard = mustard;
                //physicalObject.physicalSosigScript.CoreRB.position = position;
                //physicalObject.physicalSosigScript.CoreRB.rotation = rotation;
                physicalObject.physicalSosigScript.SetBodyPose(bodyPose);
                physicalObject.physicalSosigScript.Inventory.m_ammoStores = ammoStores;
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    if (physicalObject.physicalSosigScript.Links[i] != null)
                    {
                        if (previousLinkIntegrity[i] != linkIntegrity[i])
                        {
                            physicalObject.physicalSosigScript.Links[i].m_integrity = linkIntegrity[i];
                            physicalObject.physicalSosigScript.UpdateRendererOnLink(i);
                        }
                    }
                }

                if (active)
                {
                    if (!physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (physicalObject.gameObject.activeSelf)
                    {
                        physicalObject.gameObject.SetActive(false);
                    }
                }
            }

            if (full)
            {
                linkData = updatedItem.linkData;
                linkIntegrity = updatedItem.linkIntegrity;
                wearables = updatedItem.wearables;
                IFFChart = updatedItem.IFFChart;

                if (physicalObject != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    physicalObject.physicalSosigScript.Configure(configTemplate);

                    ++SosigIFFPatch.skip;
                    physicalObject.physicalSosigScript.SetIFF(IFF);
                    --SosigIFFPatch.skip;

                    AnvilManager.Run(EquipWearables());

                    physicalObject.physicalSosigScript.Priority.IFFChart = IFFChart;
                }
            }
        }

        public bool Update(bool full = false)
        {
            if(physicalObject == null)
            {
                return false;
            }

            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalSosigScript.CoreRB == null ? previousPos : physicalObject.physicalSosigScript.CoreRB.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = physicalObject.physicalSosigScript.CoreRB == null ? previousRot : physicalObject.physicalSosigScript.CoreRB.rotation;
            previousBodyPose = bodyPose;
            bodyPose = physicalObject.physicalSosigScript.BodyPose;
            ammoStores = physicalObject.physicalSosigScript.Inventory.m_ammoStores;
            if (ammoStores != null && previousAmmoStores == null)
            {
                previousAmmoStores = new int[ammoStores.Length];
            }
            bool ammoStoresModified = false;
            for(int i=0; i < ammoStores.Length; ++i)
            {
                if (ammoStores[i] != previousAmmoStores[i])
                {
                    ammoStoresModified = true;
                }
                previousAmmoStores[i] = ammoStores[i];
            }
            previousAmmoStores = ammoStores;
            previousMustard = mustard;
            mustard = physicalObject.physicalSosigScript.Mustard;
            previousLinkIntegrity = linkIntegrity;
            if(linkIntegrity == null || linkIntegrity.Length < physicalObject.physicalSosigScript.Links.Count)
            {
                linkIntegrity = new float[physicalObject.physicalSosigScript.Links.Count];
                previousLinkIntegrity = new float[physicalObject.physicalSosigScript.Links.Count];
            }
            bool modifiedLinkIntegrity = false;
            for(int i=0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
            {
                linkIntegrity[i] = physicalObject.physicalSosigScript.Links[i].m_integrity;
                if(linkIntegrity[i] != previousLinkIntegrity[i])
                {
                    modifiedLinkIntegrity = true;
                }
            }
            fallbackOrder = physicalObject.physicalSosigScript.FallbackOrder;
            previousOrder = currentOrder;
            currentOrder = physicalObject.physicalSosigScript.CurrentOrder;
            if(previousOrder != currentOrder)
            {
                if (ThreadManager.host)
                {
                    // Could still be -1 even on host if this is the initial update, when we make the sosig tracked
                    if (trackedID != -1)
                    {
                        ServerSend.SosigSetCurrentOrder(this, currentOrder);
                    }
                }
                else if(trackedID != -1)
                {
                    ClientSend.SosigSetCurrentOrder(this, currentOrder);
                }
                else
                {
                    if (TrackedSosig.unknownCurrentOrder.ContainsKey(localWaitingIndex))
                    {
                        TrackedSosig.unknownCurrentOrder[localWaitingIndex] = currentOrder;
                    }
                    else
                    {
                        TrackedSosig.unknownCurrentOrder.Add(localWaitingIndex, currentOrder);
                    }
                }
            }

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            if (full)
            {
                configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                configTemplate.AppliesDamageResistToIntegrityLoss = physicalObject.physicalSosigScript.AppliesDamageResistToIntegrityLoss;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosigScript.DoesDropWeaponsOnBallistic;
                configTemplate.TotalMustard = physicalObject.physicalSosigScript.m_maxMustard;
                configTemplate.BleedDamageMult = physicalObject.physicalSosigScript.BleedDamageMult;
                configTemplate.BleedRateMultiplier = physicalObject.physicalSosigScript.BleedRateMult;
                configTemplate.BleedVFXIntensity = physicalObject.physicalSosigScript.BleedVFXIntensity;
                configTemplate.SearchExtentsModifier = physicalObject.physicalSosigScript.SearchExtentsModifier;
                configTemplate.ShudderThreshold = physicalObject.physicalSosigScript.ShudderThreshold;
                configTemplate.ConfusionThreshold = physicalObject.physicalSosigScript.ConfusionThreshold;
                configTemplate.ConfusionMultiplier = physicalObject.physicalSosigScript.ConfusionMultiplier;
                configTemplate.ConfusionTimeMax = physicalObject.physicalSosigScript.m_maxConfusedTime;
                configTemplate.StunThreshold = physicalObject.physicalSosigScript.StunThreshold;
                configTemplate.StunMultiplier = physicalObject.physicalSosigScript.StunMultiplier;
                configTemplate.StunTimeMax = physicalObject.physicalSosigScript.m_maxStunTime;
                configTemplate.HasABrain = physicalObject.physicalSosigScript.HasABrain;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosigScript.DoesDropWeaponsOnBallistic;
                configTemplate.RegistersPassiveThreats = physicalObject.physicalSosigScript.RegistersPassiveThreats;
                configTemplate.CanBeKnockedOut = physicalObject.physicalSosigScript.CanBeKnockedOut;
                configTemplate.MaxUnconsciousTime = physicalObject.physicalSosigScript.m_maxUnconsciousTime;
                configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = physicalObject.physicalSosigScript.m_assaultPointOverridesSkirmishPointWhenFurtherThan;
                configTemplate.ViewDistance = physicalObject.physicalSosigScript.MaxSightRange;
                configTemplate.HearingDistance = physicalObject.physicalSosigScript.MaxHearingRange;
                configTemplate.MaxFOV = physicalObject.physicalSosigScript.MaxFOV;
                configTemplate.StateSightRangeMults = physicalObject.physicalSosigScript.StateSightRangeMults;
                configTemplate.StateHearingRangeMults = physicalObject.physicalSosigScript.StateHearingRangeMults;
                configTemplate.StateFOVMults = physicalObject.physicalSosigScript.StateFOVMults;
                configTemplate.CanPickup_Ranged = physicalObject.physicalSosigScript.CanPickup_Ranged;
                configTemplate.CanPickup_Melee = physicalObject.physicalSosigScript.CanPickup_Melee;
                configTemplate.CanPickup_Other = physicalObject.physicalSosigScript.CanPickup_Other;
                configTemplate.DoesJointBreakKill_Head = physicalObject.physicalSosigScript.m_doesJointBreakKill_Head;
                configTemplate.DoesJointBreakKill_Upper = physicalObject.physicalSosigScript.m_doesJointBreakKill_Upper;
                configTemplate.DoesJointBreakKill_Lower = physicalObject.physicalSosigScript.m_doesJointBreakKill_Lower;
                configTemplate.DoesSeverKill_Head = physicalObject.physicalSosigScript.m_doesSeverKill_Head;
                configTemplate.DoesSeverKill_Upper = physicalObject.physicalSosigScript.m_doesSeverKill_Upper;
                configTemplate.DoesSeverKill_Lower = physicalObject.physicalSosigScript.m_doesSeverKill_Lower;
                configTemplate.DoesExplodeKill_Head = physicalObject.physicalSosigScript.m_doesExplodeKill_Head;
                configTemplate.DoesExplodeKill_Upper = physicalObject.physicalSosigScript.m_doesExplodeKill_Upper;
                configTemplate.DoesExplodeKill_Lower = physicalObject.physicalSosigScript.m_doesExplodeKill_Lower;
                configTemplate.CrawlSpeed = physicalObject.physicalSosigScript.Speed_Crawl;
                configTemplate.SneakSpeed = physicalObject.physicalSosigScript.Speed_Sneak;
                configTemplate.WalkSpeed = physicalObject.physicalSosigScript.Speed_Walk;
                configTemplate.RunSpeed = physicalObject.physicalSosigScript.Speed_Run;
                configTemplate.TurnSpeed = physicalObject.physicalSosigScript.Speed_Turning;
                configTemplate.MovementRotMagnitude = physicalObject.physicalSosigScript.MovementRotMagnitude;
                configTemplate.DamMult_Projectile = physicalObject.physicalSosigScript.DamMult_Projectile;
                configTemplate.DamMult_Explosive = physicalObject.physicalSosigScript.DamMult_Explosive;
                configTemplate.DamMult_Melee = physicalObject.physicalSosigScript.DamMult_Melee;
                configTemplate.DamMult_Piercing = physicalObject.physicalSosigScript.DamMult_Piercing;
                configTemplate.DamMult_Blunt = physicalObject.physicalSosigScript.DamMult_Blunt;
                configTemplate.DamMult_Cutting = physicalObject.physicalSosigScript.DamMult_Cutting;
                configTemplate.DamMult_Thermal = physicalObject.physicalSosigScript.DamMult_Thermal;
                configTemplate.DamMult_Chilling = physicalObject.physicalSosigScript.DamMult_Chilling;
                configTemplate.DamMult_EMP = physicalObject.physicalSosigScript.DamMult_EMP;
                configTemplate.CanBeSurpressed = physicalObject.physicalSosigScript.CanBeSuppresed;
                configTemplate.SuppressionMult = physicalObject.physicalSosigScript.SuppressionMult;
                configTemplate.CanBeGrabbed = physicalObject.physicalSosigScript.CanBeGrabbed;
                configTemplate.CanBeSevered = physicalObject.physicalSosigScript.CanBeSevered;
                configTemplate.CanBeStabbed = physicalObject.physicalSosigScript.CanBeStabbed;
                configTemplate.MaxJointLimit = physicalObject.physicalSosigScript.m_maxJointLimit;
                configTemplate.OverrideSpeech = physicalObject.physicalSosigScript.Speech;
                configTemplate.LinkDamageMultipliers = new List<float>();
                configTemplate.LinkStaggerMultipliers = new List<float>();
                configTemplate.StartingLinkIntegrity = new List<Vector2>();
                configTemplate.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    configTemplate.LinkDamageMultipliers.Add(physicalObject.physicalSosigScript.Links[i].DamMult);
                    configTemplate.LinkStaggerMultipliers.Add(physicalObject.physicalSosigScript.Links[i].StaggerMagnitude);
                    configTemplate.StartingLinkIntegrity.Add(new Vector2(physicalObject.physicalSosigScript.Links[i].m_integrity, physicalObject.physicalSosigScript.Links[i].m_integrity));
                    configTemplate.StartingChanceBrokenJoint.Add(physicalObject.physicalSosigScript.Links[i].m_isJointBroken ? 1 : 0);
                }
                if (physicalObject.physicalSosigScript.Priority != null)
                {
                    configTemplate.TargetCapacity = physicalObject.physicalSosigScript.Priority.m_eventCapacity;
                    configTemplate.TargetTrackingTime = physicalObject.physicalSosigScript.Priority.m_maxTrackingTime;
                    configTemplate.NoFreshTargetTime = physicalObject.physicalSosigScript.Priority.m_timeToNoFreshTarget;
                }
                IFF = (byte)physicalObject.physicalSosigScript.GetIFF();
                linkData = new float[physicalObject.physicalSosigScript.Links.Count][];
                linkIntegrity = new float[linkData.Length];
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    linkData[i] = new float[5];
                    linkData[i][0] = physicalObject.physicalSosigScript.Links[i].StaggerMagnitude;
                    linkData[i][1] = physicalObject.physicalSosigScript.Links[i].DamMult;
                    linkData[i][2] = physicalObject.physicalSosigScript.Links[i].DamMultAVG;
                    linkData[i][3] = physicalObject.physicalSosigScript.Links[i].CollisionBluntDamageMultiplier;
                    if (physicalObject.physicalSosigScript.Links[i] == null)
                    {
                        linkData[i][4] = 0;
                    }
                    else
                    {
                        linkData[i][4] = linkIntegrity[i];
                    }
                }
                wearables = new List<List<string>>();
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    wearables.Add(new List<string>());
                    for (int j = 0; j < physicalObject.physicalSosigScript.Links[i].m_wearables.Count; ++j)
                    {
                        wearables[i].Add(physicalObject.physicalSosigScript.Links[i].m_wearables[j].name);
                        if (wearables[i][j].EndsWith("(Clone)"))
                        {
                            wearables[i][j] = wearables[i][j].Substring(0, wearables[i][j].Length - 7);
                        }
                        if (Mod.sosigWearableMap.ContainsKey(wearables[i][j]))
                        {
                            wearables[i][j] = Mod.sosigWearableMap[wearables[i][j]];
                        }
                        else
                        {
                            Mod.LogError("SosigWearable: " + wearables[i][j] + " not found in map");
                        }
                    }
                }
                IFFChart = physicalObject.physicalSosigScript.Priority.IFFChart;
            }

            return ammoStoresModified || modifiedLinkIntegrity || NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active || previousMustard != mustard;
        }

        public void OnTrackedIDReceived()
        {
            if (TrackedSosig.unknownTNHKills.ContainsKey(localWaitingIndex))
            {
                ClientSend.TNHSosigKill(TrackedSosig.unknownTNHKills[localWaitingIndex], trackedID);

                // Remove from local
                TrackedSosig.unknownTNHKills.Remove(localWaitingIndex);
            }
            if (TrackedSosig.unknownDestroyTrackedIDs.Contains(localWaitingIndex))
            {
                ClientSend.DestroySosig(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                Client.sosigs[trackedID] = null;

                // Remove from sosigsByInstanceByScene
                GameManager.sosigsByInstanceByScene[scene][instance].Remove(trackedID);

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && TrackedSosig.unknownControlTrackedIDs.ContainsKey(localWaitingIndex))
            {
                int newController = TrackedSosig.unknownControlTrackedIDs[localWaitingIndex];

                ClientSend.GiveSosigControl(trackedID, newController, null);

                // Also change controller locally
                controller = newController;

                TrackedSosig.unknownControlTrackedIDs.Remove(localWaitingIndex);

                // Remove from local
                if (GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && TrackedSosig.unknownItemInteract.ContainsKey(localWaitingIndex))
            {
                List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>> upper = TrackedSosig.unknownItemInteract[localWaitingIndex];

                for (int i = 0; i < upper.Count; i++)
                {
                    switch (upper[i].Key)
                    {
                        case 0:
                            if (upper[i].Value.Key.trackedID != -1)
                            {
                                ClientSend.SosigPickUpItem(physicalObject, upper[i].Value.Key.trackedID, upper[i].Value.Value == 0);
                                inventory[upper[i].Value.Value] = upper[i].Value.Key.trackedID;
                            }
                            // else, item does not yet have tracked ID, this interaction will be sent when it receives it
                            break;
                        case 1:
                            if (upper[i].Value.Key.trackedID != -1)
                            {
                                ClientSend.SosigPlaceItemIn(trackedID, upper[i].Value.Value, upper[i].Value.Key.trackedID);
                                inventory[upper[i].Value.Value + 2] = upper[i].Value.Key.trackedID;
                            }
                            // else, item does not yet have tracked ID, this interaction will be sent when it receives it
                            break;
                        case 2:
                            ClientSend.SosigDropSlot(trackedID, upper[i].Value.Value);
                            inventory[upper[i].Value.Value + 2] = -1;
                            break;
                        case 3:
                            ClientSend.SosigHandDrop(trackedID, upper[i].Value.Value == 0);
                            inventory[upper[i].Value.Value] = -1;
                            break;
                    }
                }

                TrackedSosig.unknownItemInteract.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedSosig.unknownSetIFFs.ContainsKey(localWaitingIndex))
            {
                int newIFF = TrackedSosig.unknownSetIFFs[localWaitingIndex];

                ClientSend.SosigSetIFF(trackedID, newIFF);

                TrackedSosig.unknownSetIFFs.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedSosig.unknownSetOriginalIFFs.ContainsKey(localWaitingIndex))
            {
                int newIFF = TrackedSosig.unknownSetOriginalIFFs[localWaitingIndex];

                ClientSend.SosigSetOriginalIFF(trackedID, newIFF);

                TrackedSosig.unknownSetOriginalIFFs.Remove(localWaitingIndex);
            }
            if (localTrackedID != -1 && TrackedSosig.unknownBodyStates.ContainsKey(localWaitingIndex))
            {
                Sosig.SosigBodyState newBodyState = TrackedSosig.unknownBodyStates[localWaitingIndex];

                ClientSend.SosigSetBodyState(trackedID, newBodyState);

                TrackedSosig.unknownBodyStates.Remove(localWaitingIndex);
            }
            if(localTrackedID != -1 && TrackedSosig.unknownIFFChart.ContainsKey(localWaitingIndex))
            {
                ClientSend.SosigPriorityIFFChart(trackedID, TrackedSosig.unknownIFFChart[localWaitingIndex]);

                TrackedSosig.unknownIFFChart.Remove(localWaitingIndex);
            }
            if(localTrackedID != -1 && TrackedSosig.unknownCurrentOrder.ContainsKey(localWaitingIndex))
            {
                ClientSend.SosigSetCurrentOrder(this, TrackedSosig.unknownCurrentOrder[localWaitingIndex]);

                TrackedSosig.unknownCurrentOrder.Remove(localWaitingIndex);
            }
            if(localTrackedID != -1 && TrackedSosig.unknownConfiguration.ContainsKey(localWaitingIndex))
            {
                ClientSend.SosigConfigure(trackedID, TrackedSosig.unknownConfiguration[localWaitingIndex]);

                TrackedSosig.unknownConfiguration.Remove(localWaitingIndex);
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            if (trackedID == -1)
            {
                TrackedSosig.unknownControlTrackedIDs.Remove(localWaitingIndex);
                TrackedSosig.unknownDestroyTrackedIDs.Remove(localWaitingIndex);
                TrackedSosig.unknownItemInteract.Remove(localWaitingIndex);
                TrackedSosig.unknownSetIFFs.Remove(localWaitingIndex);
                TrackedSosig.unknownSetOriginalIFFs.Remove(localWaitingIndex);
                TrackedSosig.unknownBodyStates.Remove(localWaitingIndex);
                TrackedSosig.unknownTNHKills.Remove(localWaitingIndex);
                TrackedSosig.unknownIFFChart.Remove(localWaitingIndex);
                TrackedSosig.unknownCurrentOrder.Remove(localWaitingIndex);
                TrackedSosig.unknownConfiguration.Remove(localWaitingIndex);
            }

            if (localTrackedID > -1 && localTrackedID < GameManager.sosigs.Count)
            {
                // Remove from actual local sosigs list and update the localTrackedID of the sosig we are moving
                GameManager.sosigs[localTrackedID] = GameManager.sosigs[GameManager.sosigs.Count - 1];
                GameManager.sosigs[localTrackedID].localTrackedID = localTrackedID;
                GameManager.sosigs.RemoveAt(GameManager.sosigs.Count - 1);
                localTrackedID = -1;
            }
            else
            {
                Mod.LogWarning("\tlocaltrackedID out of range!:\n" + Environment.StackTrace);
            }
        }

        public void TakeInventoryControl()
        {
            Mod.LogInfo("Taking sosig " + trackedID + " control");
            for(int i=0; i < inventory.Length; ++i)
            {
                Mod.LogInfo("\tChecking inventory "+i);
                if (inventory[i] != -1)
                {
                    TrackedItemData[] arrToUse = ThreadManager.host ? Server.items : Client.items;
                    Mod.LogInfo("\t\tGot ID: " + inventory[i]+", arr to use null?: "+ (arrToUse[inventory[i]] == null)+", controller: "+ arrToUse[inventory[i]].controller);
                    if (arrToUse[inventory[i]] != null && arrToUse[inventory[i]].controller != GameManager.ID)
                    {
                        Mod.LogInfo("\t\t\tTaking control if item "+ inventory[i]);
                        TrackedItemData.TakeControlRecursive(arrToUse[inventory[i]]);
                        if (arrToUse[inventory[i]].physicalItem != null)
                        {
                            Mod.SetKinematicRecursive(arrToUse[inventory[i]].physicalItem.transform, false);
                        }
                    }
                }
            }
        }

        public override void OnControlChanged(int newController)
        {
            base.OnControlChanged(newController);

            // Note that this only gets called when the new controller is different from the old one
            if (newController == GameManager.ID) // Gain control
            {
                TakeInventoryControl();

                if (physical != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity((physical.physical as Sosig).E);
                    }
                    (physical.physical as Sosig).CoreRB.isKinematic = false;
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physical != null)
                {
                    physical.EnsureUncontrolled();

                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity((physical.physical as Sosig).E);
                    }
                    (physical.physical as Sosig).CoreRB.isKinematic = true;
                }
            }
        }
    }
}
