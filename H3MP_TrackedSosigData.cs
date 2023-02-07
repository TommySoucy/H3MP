using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedSosigData
    {
        private static readonly FieldInfo sosigInvAmmoStores = typeof(SosigInventory).GetField("m_ammoStores", BindingFlags.NonPublic | BindingFlags.Instance);

        public static int insuranceCount = 5; // Amount of times to send the most up to date version of this data to ensure we don't miss packets
        public int insuranceCounter = insuranceCount; // Amount of times left to send this data
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
        public H3MP_TrackedSosig physicalObject;
        public int localTrackedID;
        public bool previousActive;
        public bool active;
        public List<List<string>> wearables;
        public float[][] linkData;
        public byte IFF;
        public bool[] IFFChart;
        public Sosig.SosigBodyPose previousBodyPose;
        public Sosig.SosigBodyPose bodyPose;
        public bool removeFromListOnDestroy = true;
        public string scene;
        public int instance;
        public byte[] data;

        public static KeyValuePair<int, TNH_Manager.SosigPatrolSquad> latestSosigPatrolSquad;

        public IEnumerator Instantiate()
        {
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            if (sosigPrefab == null)
            {
                Mod.LogError($"Attempted to instantiate sosig sent from {controller} but failed to get prefab.");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject sosigInstance = GameObject.Instantiate(sosigPrefab, position, rotation);
            --Mod.skipAllInstantiates;
            physicalObject = sosigInstance.AddComponent<H3MP_TrackedSosig>();
            physicalObject.data = this;

            physicalObject.physicalSosigScript = sosigInstance.GetComponent<Sosig>();
            SosigConfigurePatch.skipConfigure = true;
            physicalObject.physicalSosigScript.Configure(configTemplate);

            H3MP_GameManager.trackedSosigBySosig.Add(physicalObject.physicalSosigScript, physicalObject);

            AnvilManager.Run(EquipWearables());

            // Deregister the AI from the manager if we are not in control
            // Also set CoreRB as kinematic
            if (controller != H3MP_GameManager.ID)
            {
                GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosigScript.E);
                physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
            }

            // Initially set IFF
            ++SosigIFFPatch.skip;
            physicalObject.physicalSosigScript.SetIFF(IFF);
            --SosigIFFPatch.skip;

            // Set IFFChart
            physicalObject.physicalSosigScript.Priority.IFFChart = IFFChart;

            // Check if in temporary lists
            if (GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                if (Mod.temporaryHoldSosigIDs.Contains(trackedID))
                {
                    Mod.temporaryHoldSosigIDs.Remove(trackedID);

                    TNH_HoldPoint curHoldPoint = GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex];
                    List<Sosig> curHoldPointSosigs = (List<Sosig>)Mod.TNH_HoldPoint_m_activeSosigs.GetValue(curHoldPoint);
                    curHoldPointSosigs.Add(physicalObject.physicalSosigScript);
                    data[0] = 0;
                }
                else if(Mod.temporarySupplySosigIDs.ContainsKey(trackedID))
                {
                    TNH_SupplyPoint curSupplyPoint = GM.TNH_Manager.SupplyPoints[Mod.temporarySupplySosigIDs[trackedID]];
                    List<Sosig> curSupplyPointSosigs = (List<Sosig>)Mod.TNH_SupplyPoint_m_activeSosigs.GetValue(curSupplyPoint);
                    curSupplyPointSosigs.Add(physicalObject.physicalSosigScript);
                    data[1] = 0;

                    Mod.temporarySupplySosigIDs.Remove(trackedID);
                }
            }

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
                    ((List<Sosig>)Mod.TNH_HoldPoint_m_activeSosigs.GetValue(GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex])).Add(physicalObject.physicalSosigScript);
                }
                else if (data[1] == 1) // TNH_SupplyPoint is in Spawn Take Enemy Group
                {
                    ((List<Sosig>)Mod.TNH_SupplyPoint_m_activeSosigs.GetValue(GM.TNH_Manager.SupplyPoints[BitConverter.ToInt16(data, 2)])).Add(physicalObject.physicalSosigScript);
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
                        ((List<TNH_Manager.SosigPatrolSquad>)Mod.TNH_Manager_m_patrolSquads.GetValue(GM.TNH_Manager)).Add(latestSosigPatrolSquad.Value);
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

        public void Update(H3MP_TrackedSosigData updatedItem, bool full = false)
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

            // Set physically
            if (physicalObject != null)
            {
                physicalObject.physicalSosigScript.Mustard = mustard;
                //physicalObject.physicalSosigScript.CoreRB.position = position;
                //physicalObject.physicalSosigScript.CoreRB.rotation = rotation;
                Mod.Sosig_SetBodyPose.Invoke(physicalObject.physicalSosigScript, new object[] { bodyPose });
                sosigInvAmmoStores.SetValue(physicalObject.physicalSosigScript.Inventory, ammoStores);
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    if (physicalObject.physicalSosigScript.Links[i] != null)
                    {
                        if (previousLinkIntegrity[i] != linkIntegrity[i])
                        {
                            Mod.SosigLink_m_integrity.SetValue(physicalObject.physicalSosigScript.Links[i], linkIntegrity[i]);
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
            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalSosigScript.CoreRB == null ? previousPos : physicalObject.physicalSosigScript.CoreRB.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = physicalObject.physicalSosigScript.CoreRB == null ? previousRot : physicalObject.physicalSosigScript.CoreRB.rotation;
            previousBodyPose = bodyPose;
            bodyPose = physicalObject.physicalSosigScript.BodyPose;
            ammoStores = (int[])sosigInvAmmoStores.GetValue(physicalObject.physicalSosigScript.Inventory);
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
                linkIntegrity[i] = (float)Mod.SosigLink_m_integrity.GetValue(physicalObject.physicalSosigScript.Links[i]);
                if(linkIntegrity[i] != previousLinkIntegrity[i])
                {
                    modifiedLinkIntegrity = true;
                }
            }

            previousActive = active;
            active = physicalObject.gameObject.activeInHierarchy;

            if (full)
            {
                configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                configTemplate.AppliesDamageResistToIntegrityLoss = physicalObject.physicalSosigScript.AppliesDamageResistToIntegrityLoss;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosigScript.DoesDropWeaponsOnBallistic;
                configTemplate.TotalMustard = (float)typeof(Sosig).GetField("m_maxMustard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.BleedDamageMult = physicalObject.physicalSosigScript.BleedDamageMult;
                configTemplate.BleedRateMultiplier = physicalObject.physicalSosigScript.BleedRateMult;
                configTemplate.BleedVFXIntensity = physicalObject.physicalSosigScript.BleedVFXIntensity;
                configTemplate.SearchExtentsModifier = physicalObject.physicalSosigScript.SearchExtentsModifier;
                configTemplate.ShudderThreshold = physicalObject.physicalSosigScript.ShudderThreshold;
                configTemplate.ConfusionThreshold = (float)typeof(Sosig).GetField("ConfusionThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.ConfusionMultiplier = (float)typeof(Sosig).GetField("ConfusionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.ConfusionTimeMax = (float)typeof(Sosig).GetField("m_maxConfusedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.StunThreshold = (float)typeof(Sosig).GetField("StunThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.StunMultiplier = (float)typeof(Sosig).GetField("StunMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.StunTimeMax = (float)typeof(Sosig).GetField("m_maxStunTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.HasABrain = physicalObject.physicalSosigScript.HasABrain;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosigScript.DoesDropWeaponsOnBallistic;
                configTemplate.RegistersPassiveThreats = physicalObject.physicalSosigScript.RegistersPassiveThreats;
                configTemplate.CanBeKnockedOut = physicalObject.physicalSosigScript.CanBeKnockedOut;
                configTemplate.MaxUnconsciousTime = (float)typeof(Sosig).GetField("m_maxUnconsciousTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = (float)typeof(Sosig).GetField("m_assaultPointOverridesSkirmishPointWhenFurtherThan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.ViewDistance = physicalObject.physicalSosigScript.MaxSightRange;
                configTemplate.HearingDistance = physicalObject.physicalSosigScript.MaxHearingRange;
                configTemplate.MaxFOV = physicalObject.physicalSosigScript.MaxFOV;
                configTemplate.StateSightRangeMults = physicalObject.physicalSosigScript.StateSightRangeMults;
                configTemplate.StateHearingRangeMults = physicalObject.physicalSosigScript.StateHearingRangeMults;
                configTemplate.StateFOVMults = physicalObject.physicalSosigScript.StateFOVMults;
                configTemplate.CanPickup_Ranged = physicalObject.physicalSosigScript.CanPickup_Ranged;
                configTemplate.CanPickup_Melee = physicalObject.physicalSosigScript.CanPickup_Melee;
                configTemplate.CanPickup_Other = physicalObject.physicalSosigScript.CanPickup_Other;
                configTemplate.DoesJointBreakKill_Head = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesJointBreakKill_Upper = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesJointBreakKill_Lower = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesSeverKill_Head = (bool)typeof(Sosig).GetField("m_doesSeverKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesSeverKill_Upper = (bool)typeof(Sosig).GetField("m_doesSeverKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesSeverKill_Lower = (bool)typeof(Sosig).GetField("m_doesSeverKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesExplodeKill_Head = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesExplodeKill_Upper = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.DoesExplodeKill_Lower = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
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
                configTemplate.MaxJointLimit = (float)typeof(Sosig).GetField("m_maxJointLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript);
                configTemplate.OverrideSpeech = physicalObject.physicalSosigScript.Speech;
                FieldInfo linkIntegrityField = typeof(SosigLink).GetField("m_integrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                FieldInfo linkJointBroken = typeof(SosigLink).GetField("m_isJointBroken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configTemplate.LinkDamageMultipliers = new List<float>();
                configTemplate.LinkStaggerMultipliers = new List<float>();
                configTemplate.StartingLinkIntegrity = new List<Vector2>();
                configTemplate.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    configTemplate.LinkDamageMultipliers.Add(physicalObject.physicalSosigScript.Links[i].DamMult);
                    configTemplate.LinkStaggerMultipliers.Add(physicalObject.physicalSosigScript.Links[i].StaggerMagnitude);
                    float actualLinkIntegrity = (float)linkIntegrityField.GetValue(physicalObject.physicalSosigScript.Links[i]);
                    configTemplate.StartingLinkIntegrity.Add(new Vector2(actualLinkIntegrity, actualLinkIntegrity));
                    configTemplate.StartingChanceBrokenJoint.Add(((bool)linkJointBroken.GetValue(physicalObject.physicalSosigScript.Links[i])) ? 1 : 0);
                }
                if (physicalObject.physicalSosigScript.Priority != null)
                {
                    configTemplate.TargetCapacity = (int)typeof(SosigTargetPrioritySystem).GetField("m_eventCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript.Priority);
                    configTemplate.TargetTrackingTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_maxTrackingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript.Priority);
                    configTemplate.NoFreshTargetTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_timeToNoFreshTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosigScript.Priority);
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
                FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < physicalObject.physicalSosigScript.Links.Count; ++i)
                {
                    wearables.Add(new List<string>());
                    List<SosigWearable> sosigWearables = (List<SosigWearable>)wearablesField.GetValue(physicalObject.physicalSosigScript.Links[i]);
                    for (int j = 0; j < sosigWearables.Count; ++j)
                    {
                        wearables[i].Add(sosigWearables[j].name);
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
            if (H3MP_TrackedSosig.unknownTNHKills.ContainsKey(localTrackedID))
            {
                H3MP_ClientSend.TNHSosigKill(H3MP_TrackedSosig.unknownTNHKills[localTrackedID], trackedID);

                // Remove from local
                H3MP_TrackedSosig.unknownTNHKills.Remove(localTrackedID);
            }
            if (H3MP_TrackedSosig.unknownDestroyTrackedIDs.Contains(localTrackedID))
            {
                H3MP_ClientSend.DestroySosig(trackedID);

                // Note that if we receive a tracked ID that was previously unknown, we must be a client
                H3MP_Client.sosigs[trackedID] = null;

                // Remove from local
                RemoveFromLocal();
            }
            if (localTrackedID != -1 && H3MP_TrackedSosig.unknownControlTrackedIDs.ContainsKey(localTrackedID))
            {
                int newController = H3MP_TrackedSosig.unknownControlTrackedIDs[localTrackedID];

                H3MP_ClientSend.GiveSosigControl(trackedID, newController);

                // Also change controller locally
                controller = newController;

                H3MP_TrackedSosig.unknownControlTrackedIDs.Remove(localTrackedID);

                // Remove from local
                if (H3MP_GameManager.ID != controller)
                {
                    RemoveFromLocal();
                }
            }
            if (localTrackedID != -1 && H3MP_TrackedSosig.unknownItemInteractTrackedIDs.ContainsKey(localTrackedID))
            {
                List<KeyValuePair<int, KeyValuePair<int, int>>> upper = H3MP_TrackedSosig.unknownItemInteractTrackedIDs[localTrackedID];

                for(int i = 0; i < upper.Count; i++)
                {
                    switch (upper[i].Key)
                    {
                        case 0:
                            H3MP_ClientSend.SosigPickUpItem(physicalObject, upper[i].Value.Key, upper[i].Value.Value == 1);
                            break;
                        case 1:
                            H3MP_ClientSend.SosigPlaceItemIn(trackedID, upper[i].Value.Value, upper[i].Value.Key);
                            break;
                        case 2:
                            H3MP_ClientSend.SosigDropSlot(trackedID, upper[i].Value.Key);
                            break;
                        case 3:
                            H3MP_ClientSend.SosigHandDrop(trackedID, upper[i].Value.Key == 1);
                            break;
                    }
                }

                H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Remove(localTrackedID);
            }
            if (localTrackedID != -1 && H3MP_TrackedSosig.unknownSetIFFs.ContainsKey(localTrackedID))
            {
                int newIFF = H3MP_TrackedSosig.unknownSetIFFs[localTrackedID];

                H3MP_ClientSend.SosigSetIFF(trackedID, newIFF);

                H3MP_TrackedSosig.unknownSetIFFs.Remove(localTrackedID);
            }
            if (localTrackedID != -1 && H3MP_TrackedSosig.unknownSetOriginalIFFs.ContainsKey(localTrackedID))
            {
                int newIFF = H3MP_TrackedSosig.unknownSetOriginalIFFs[localTrackedID];

                H3MP_ClientSend.SosigSetOriginalIFF(trackedID, newIFF);

                H3MP_TrackedSosig.unknownSetOriginalIFFs.Remove(localTrackedID);
            }
            if (localTrackedID != -1 && H3MP_TrackedSosig.unknownBodyStates.ContainsKey(localTrackedID))
            {
                Sosig.SosigBodyState newBodyState = H3MP_TrackedSosig.unknownBodyStates[localTrackedID];

                H3MP_ClientSend.SosigSetBodyState(trackedID, newBodyState);

                H3MP_TrackedSosig.unknownBodyStates.Remove(localTrackedID);
            }
            if(localTrackedID != -1 && H3MP_TrackedSosig.unknownIFFChart.ContainsKey(localTrackedID))
            {
                H3MP_ClientSend.SosigPriorityIFFChart(trackedID, H3MP_TrackedSosig.unknownIFFChart[localTrackedID]);

                H3MP_TrackedSosig.unknownIFFChart.Remove(localTrackedID);
            }

            if (localTrackedID != -1)
            {
                // Add to sosig tracking list
                if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(instance, out List<int> sosigList))
                    {
                        sosigList.Add(trackedID);
                    }
                    else
                    {
                        relevantInstances.Add(instance, new List<int>() { trackedID });
                    }
                }
                else
                {
                    Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                    newInstances.Add(instance, new List<int>() { trackedID });
                    H3MP_GameManager.sosigsByInstanceByScene.Add(scene, newInstances);
                }
            }
        }

        public void RemoveFromLocal()
        {
            // Manage unknown lists
            H3MP_TrackedSosig.unknownControlTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownDestroyTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownSetIFFs.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownSetOriginalIFFs.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownBodyStates.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownTNHKills.Remove(localTrackedID);
            H3MP_TrackedSosig.unknownIFFChart.Remove(localTrackedID);

            // Remove from temp lists if in there
            if (!H3MP_ThreadManager.host && H3MP_Client.tempLocalSosigOriginalIDs.ContainsKey(localTrackedID))
            {
                H3MP_Client.tempLocalSosigs.Remove(H3MP_Client.tempLocalSosigOriginalIDs[localTrackedID]);
                H3MP_Client.tempLocalSosigOriginalIDs.Remove(localTrackedID);
            }

            // Remove from actual local sosigs list and update the localTrackedID of the sosig we are moving
            H3MP_GameManager.sosigs[localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
            int oldLocalTrackedID = H3MP_GameManager.sosigs[localTrackedID].localTrackedID;
            H3MP_GameManager.sosigs[localTrackedID].localTrackedID = localTrackedID;
            H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
            if (H3MP_GameManager.sosigs.Count > 1 && H3MP_GameManager.sosigs[localTrackedID].trackedID == -1)
            {
                int originalLocalTrackedID = -1;
                if (H3MP_Client.tempLocalSosigOriginalIDs.ContainsKey(oldLocalTrackedID))
                {
                    originalLocalTrackedID = H3MP_Client.tempLocalSosigOriginalIDs[oldLocalTrackedID];
                    H3MP_Client.tempLocalSosigOriginalIDs.Remove(oldLocalTrackedID);
                    H3MP_Client.tempLocalSosigs.Remove(oldLocalTrackedID);
                }
                else
                {
                    originalLocalTrackedID = oldLocalTrackedID;
                }
                H3MP_Client.tempLocalSosigOriginalIDs.Add(localTrackedID, originalLocalTrackedID);
                H3MP_Client.tempLocalSosigs.Add(originalLocalTrackedID, H3MP_GameManager.sosigs[localTrackedID]);
            }
            localTrackedID = -1;
        }
    }
}
