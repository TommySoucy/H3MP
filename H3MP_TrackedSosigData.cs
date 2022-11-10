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

        public int trackedID;
        public int controller;
        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
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
        public Sosig.SosigBodyPose previousBodyPose;
        public Sosig.SosigBodyPose bodyPose;

        public IEnumerator Instantiate()
        {
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            if (sosigPrefab == null)
            {
                Debug.LogError($"Attempted to instantiate sosig sent from {controller} but failed to get prefab.");
                yield break;
            }

            ++Mod.skipAllInstantiates;
            GameObject sosigInstance = GameObject.Instantiate(sosigPrefab);
            --Mod.skipAllInstantiates;
            physicalObject = sosigInstance.AddComponent<H3MP_TrackedSosig>();
            physicalObject.data = this;

            physicalObject.physicalSosig = sosigInstance.GetComponent<Sosig>();
            SosigConfigurePatch.skipConfigure = true;
            physicalObject.physicalSosig.Configure(configTemplate);

            H3MP_GameManager.trackedSosigBySosig.Add(physicalObject.physicalSosig, physicalObject);

            if (H3MP_GameManager.waitingWearables.ContainsKey(trackedID))
            {
                if (wearables == null || wearables.Count == 0)
                {
                    wearables = H3MP_GameManager.waitingWearables[trackedID];
                }
                else
                {
                    List<List<string>> newWearables = H3MP_GameManager.waitingWearables[trackedID];
                    for(int i = 0; i < newWearables.Count; ++i)
                    {
                        for (int j = 0; j < newWearables.Count; ++j)
                        {
                            wearables[i].Add(newWearables[i][j]);
                        }
                    }
                }
                H3MP_GameManager.waitingWearables.Remove(trackedID);
            }

            AnvilManager.Run(EquipWearables());

            // Deregister the AI from the manager if we are not in control
            // Also set CoreRB as kinematic
            if (H3MP_ThreadManager.host)
            {
                if(controller != 0)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosig.E);
                    physicalObject.physicalSosig.CoreRB.isKinematic = true;
                }
            }
            else if(controller != H3MP_Client.singleton.ID)
            {
                GM.CurrentAIManager.DeRegisterAIEntity(physicalObject.physicalSosig.E);
                physicalObject.physicalSosig.CoreRB.isKinematic = true;
            }

            // Initially set IFF
            ++SosigIFFPatch.skip;
            physicalObject.physicalSosig.SetIFF(IFF);
            --SosigIFFPatch.skip;

            // Initially set itself
            Update(this);
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
                            GameObject outfitItemObject = GameObject.Instantiate(IM.OD[wearables[i][j]].GetGameObject(), physicalObject.physicalSosig.Links[i].transform.position, physicalObject.physicalSosig.Links[i].transform.rotation, physicalObject.physicalSosig.Links[i].transform);
                            --Mod.skipAllInstantiates;
                            SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                            ++SosigLinkActionPatch.skipRegisterWearable;
                            wearableScript.RegisterWearable(physicalObject.physicalSosig.Links[i]);
                            --SosigLinkActionPatch.skipRegisterWearable;
                        }
                        else
                        {
                            Debug.LogWarning("TrackedSosigData.EquipWearables: Wearable "+ wearables[i][j]+" not found in OD");
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
                ++Mod.skipAllInstantiates;
                GameObject outfitItemObject = GameObject.Instantiate(IM.OD[ID].GetGameObject(), physicalObject.physicalSosig.Links[linkIndex].transform.position, physicalObject.physicalSosig.Links[linkIndex].transform.rotation, physicalObject.physicalSosig.Links[linkIndex].transform);
                --Mod.skipAllInstantiates;
                SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                if (skip)
                {
                    ++SosigLinkActionPatch.skipRegisterWearable;
                }
                wearableScript.RegisterWearable(physicalObject.physicalSosig.Links[linkIndex]);
                if (skip)
                {
                    --SosigLinkActionPatch.skipRegisterWearable;
                }
            }
            else
            {
                Debug.LogWarning("TrackedSosigData.EquipWearables: Wearable " + ID + " not found in OD");
            }
            yield break;
        }

        public void Update(H3MP_TrackedSosigData updatedItem)
        {
            // Set data
            order = updatedItem.order;
            previousPos = position;
            previousRot = rotation;
            position = updatedItem.position;
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
                physicalObject.physicalSosig.Mustard = mustard;
                physicalObject.physicalSosig.CoreRB.position = position;
                physicalObject.physicalSosig.CoreRB.rotation = rotation;
                Mod.Sosig_SetBodyPose.Invoke(physicalObject.physicalSosig, new object[] { bodyPose });
                sosigInvAmmoStores.SetValue(physicalObject.physicalSosig.Inventory, ammoStores);
                for (int i=0; i < physicalObject.physicalSosig.Links.Count; ++i)
                {
                    if (physicalObject.physicalSosig.Links[i] != null)
                    {
                        if(previousLinkIntegrity[i] != linkIntegrity[i])
                        {
                            Mod.SosigLink_m_integrity.SetValue(physicalObject.physicalSosig.Links[i], linkIntegrity[i]);
                            physicalObject.physicalSosig.UpdateRendererOnLink(i);
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
        }

        public bool Update(bool full = false)
        {
            previousPos = position;
            previousRot = rotation;
            position = physicalObject.physicalSosig.CoreRB.position;
            rotation = physicalObject.physicalSosig.CoreRB.rotation;
            previousBodyPose = bodyPose;
            bodyPose = physicalObject.physicalSosig.BodyPose;
            ammoStores = (int[])sosigInvAmmoStores.GetValue(physicalObject.physicalSosig.Inventory);
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
            mustard = physicalObject.physicalSosig.Mustard;
            previousLinkIntegrity = linkIntegrity;
            if(linkIntegrity == null || linkIntegrity.Length < physicalObject.physicalSosig.Links.Count)
            {
                linkIntegrity = new float[physicalObject.physicalSosig.Links.Count];
                previousLinkIntegrity = new float[physicalObject.physicalSosig.Links.Count];
            }
            bool modifiedLinkIntegrity = false;
            for(int i=0; i < physicalObject.physicalSosig.Links.Count; ++i)
            {
                linkIntegrity[i] = (float)Mod.SosigLink_m_integrity.GetValue(physicalObject.physicalSosig.Links[i]);
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
                configTemplate.AppliesDamageResistToIntegrityLoss = physicalObject.physicalSosig.AppliesDamageResistToIntegrityLoss;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosig.DoesDropWeaponsOnBallistic;
                configTemplate.TotalMustard = (float)typeof(Sosig).GetField("m_maxMustard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.BleedDamageMult = physicalObject.physicalSosig.BleedDamageMult;
                configTemplate.BleedRateMultiplier = physicalObject.physicalSosig.BleedRateMult;
                configTemplate.BleedVFXIntensity = physicalObject.physicalSosig.BleedVFXIntensity;
                configTemplate.SearchExtentsModifier = physicalObject.physicalSosig.SearchExtentsModifier;
                configTemplate.ShudderThreshold = physicalObject.physicalSosig.ShudderThreshold;
                configTemplate.ConfusionThreshold = (float)typeof(Sosig).GetField("ConfusionThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.ConfusionMultiplier = (float)typeof(Sosig).GetField("ConfusionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.ConfusionTimeMax = (float)typeof(Sosig).GetField("m_maxConfusedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.StunThreshold = (float)typeof(Sosig).GetField("StunThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.StunMultiplier = (float)typeof(Sosig).GetField("StunMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.StunTimeMax = (float)typeof(Sosig).GetField("m_maxStunTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.HasABrain = physicalObject.physicalSosig.HasABrain;
                configTemplate.DoesDropWeaponsOnBallistic = physicalObject.physicalSosig.DoesDropWeaponsOnBallistic;
                configTemplate.RegistersPassiveThreats = physicalObject.physicalSosig.RegistersPassiveThreats;
                configTemplate.CanBeKnockedOut = physicalObject.physicalSosig.CanBeKnockedOut;
                configTemplate.MaxUnconsciousTime = (float)typeof(Sosig).GetField("m_maxUnconsciousTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = (float)typeof(Sosig).GetField("m_assaultPointOverridesSkirmishPointWhenFurtherThan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.ViewDistance = physicalObject.physicalSosig.MaxSightRange;
                configTemplate.HearingDistance = physicalObject.physicalSosig.MaxHearingRange;
                configTemplate.MaxFOV = physicalObject.physicalSosig.MaxFOV;
                configTemplate.StateSightRangeMults = physicalObject.physicalSosig.StateSightRangeMults;
                configTemplate.StateHearingRangeMults = physicalObject.physicalSosig.StateHearingRangeMults;
                configTemplate.StateFOVMults = physicalObject.physicalSosig.StateFOVMults;
                configTemplate.CanPickup_Ranged = physicalObject.physicalSosig.CanPickup_Ranged;
                configTemplate.CanPickup_Melee = physicalObject.physicalSosig.CanPickup_Melee;
                configTemplate.CanPickup_Other = physicalObject.physicalSosig.CanPickup_Other;
                configTemplate.DoesJointBreakKill_Head = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesJointBreakKill_Upper = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesJointBreakKill_Lower = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesSeverKill_Head = (bool)typeof(Sosig).GetField("m_doesSeverKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesSeverKill_Upper = (bool)typeof(Sosig).GetField("m_doesSeverKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesSeverKill_Lower = (bool)typeof(Sosig).GetField("m_doesSeverKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesExplodeKill_Head = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesExplodeKill_Upper = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.DoesExplodeKill_Lower = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.CrawlSpeed = physicalObject.physicalSosig.Speed_Crawl;
                configTemplate.SneakSpeed = physicalObject.physicalSosig.Speed_Sneak;
                configTemplate.WalkSpeed = physicalObject.physicalSosig.Speed_Walk;
                configTemplate.RunSpeed = physicalObject.physicalSosig.Speed_Run;
                configTemplate.TurnSpeed = physicalObject.physicalSosig.Speed_Turning;
                configTemplate.MovementRotMagnitude = physicalObject.physicalSosig.MovementRotMagnitude;
                configTemplate.DamMult_Projectile = physicalObject.physicalSosig.DamMult_Projectile;
                configTemplate.DamMult_Explosive = physicalObject.physicalSosig.DamMult_Explosive;
                configTemplate.DamMult_Melee = physicalObject.physicalSosig.DamMult_Melee;
                configTemplate.DamMult_Piercing = physicalObject.physicalSosig.DamMult_Piercing;
                configTemplate.DamMult_Blunt = physicalObject.physicalSosig.DamMult_Blunt;
                configTemplate.DamMult_Cutting = physicalObject.physicalSosig.DamMult_Cutting;
                configTemplate.DamMult_Thermal = physicalObject.physicalSosig.DamMult_Thermal;
                configTemplate.DamMult_Chilling = physicalObject.physicalSosig.DamMult_Chilling;
                configTemplate.DamMult_EMP = physicalObject.physicalSosig.DamMult_EMP;
                configTemplate.CanBeSurpressed = physicalObject.physicalSosig.CanBeSuppresed;
                configTemplate.SuppressionMult = physicalObject.physicalSosig.SuppressionMult;
                configTemplate.CanBeGrabbed = physicalObject.physicalSosig.CanBeGrabbed;
                configTemplate.CanBeSevered = physicalObject.physicalSosig.CanBeSevered;
                configTemplate.CanBeStabbed = physicalObject.physicalSosig.CanBeStabbed;
                configTemplate.MaxJointLimit = (float)typeof(Sosig).GetField("m_maxJointLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig);
                configTemplate.OverrideSpeech = physicalObject.physicalSosig.Speech;
                FieldInfo linkIntegrityField = typeof(SosigLink).GetField("m_integrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                FieldInfo linkJointBroken = typeof(SosigLink).GetField("m_isJointBroken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configTemplate.LinkDamageMultipliers = new List<float>();
                configTemplate.LinkStaggerMultipliers = new List<float>();
                configTemplate.StartingLinkIntegrity = new List<Vector2>();
                configTemplate.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < physicalObject.physicalSosig.Links.Count; ++i)
                {
                    configTemplate.LinkDamageMultipliers.Add(physicalObject.physicalSosig.Links[i].DamMult);
                    configTemplate.LinkStaggerMultipliers.Add(physicalObject.physicalSosig.Links[i].StaggerMagnitude);
                    float actualLinkIntegrity = (float)linkIntegrityField.GetValue(physicalObject.physicalSosig.Links[i]);
                    configTemplate.StartingLinkIntegrity.Add(new Vector2(actualLinkIntegrity, actualLinkIntegrity));
                    configTemplate.StartingChanceBrokenJoint.Add(((bool)linkJointBroken.GetValue(physicalObject.physicalSosig.Links[i])) ? 1 : 0);
                }
                if (physicalObject.physicalSosig.Priority != null)
                {
                    configTemplate.TargetCapacity = (int)typeof(SosigTargetPrioritySystem).GetField("m_eventCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig.Priority);
                    configTemplate.TargetTrackingTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_maxTrackingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig.Priority);
                    configTemplate.NoFreshTargetTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_timeToNoFreshTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(physicalObject.physicalSosig.Priority);
                }
                IFF = (byte)physicalObject.physicalSosig.GetIFF();
                linkData = new float[physicalObject.physicalSosig.Links.Count][];
                linkIntegrity = new float[linkData.Length];
                for (int i = 0; i < physicalObject.physicalSosig.Links.Count; ++i)
                {
                    linkData[i] = new float[5];
                    linkData[i][0] = physicalObject.physicalSosig.Links[i].StaggerMagnitude;
                    linkData[i][1] = physicalObject.physicalSosig.Links[i].DamMult;
                    linkData[i][2] = physicalObject.physicalSosig.Links[i].DamMultAVG;
                    linkData[i][3] = physicalObject.physicalSosig.Links[i].CollisionBluntDamageMultiplier;
                    if (physicalObject.physicalSosig.Links[i] == null)
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
                for (int i = 0; i < physicalObject.physicalSosig.Links.Count; ++i)
                {
                    wearables.Add(new List<string>());
                    List<SosigWearable> sosigWearables = (List<SosigWearable>)wearablesField.GetValue(physicalObject.physicalSosig.Links[i]);
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
                            Debug.LogError("SosigWearable: " + wearables[i][j] + " not found in map");
                        }
                    }
                }
            }

            return ammoStoresModified || modifiedLinkIntegrity || NeedsUpdate();
        }

        public bool NeedsUpdate()
        {
            return !previousPos.Equals(position) || !previousRot.Equals(rotation) || previousActive != active || previousMustard != mustard;
        }
    }
}
