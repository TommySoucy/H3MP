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
        public TrackedSosig physicalSosig;

        public Vector3 previousPos;
        public Quaternion previousRot;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 previousAgentPos;
        public Vector3 agentPosition;
        public Vector3 velocity = Vector3.zero;
        public int[] previousAmmoStores;
        public int[] ammoStores;
        public float[] previousLinkIntegrity;
        public float[] linkIntegrity;
        public float previousMustard;
        public float mustard;
        public SosigConfigTemplate configTemplate;
        public List<List<string>> wearables;
        public float[][] linkData;
        public byte IFF;
        public bool[] IFFChart;
        public Sosig.SosigBodyPose previousBodyPose;
        public Sosig.SosigBodyPose bodyPose;
        public Sosig.SosigOrder previousOrder;
        public Sosig.SosigOrder currentOrder;
        public Sosig.SosigOrder fallbackOrder;
        public byte[] data;
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
        public float previousJointLimit;
        public float jointLimit;
        public bool previousUnconscious;
        public bool unconscious;
        public bool previousBlinded;
        public bool blinded;
        public bool previousConfused;
        public bool confused;
        public float previousSuppressionLevel;
        public float suppressionLevel;

        public static KeyValuePair<int, TNH_Manager.SosigPatrolSquad> latestSosigPatrolSquad = new KeyValuePair<int, TNH_Manager.SosigPatrolSquad>(-1, null);

        public TrackedSosigData()
        {

        }

        public TrackedSosigData(Packet packet, string typeID, int trackedID) : base(packet, typeID, trackedID)
        {
            // Update
            position = packet.ReadVector3();
            rotation = packet.ReadQuaternion();
            agentPosition = packet.ReadVector3();
            mustard = packet.ReadFloat();
            byte ammoStoreLength = packet.ReadByte();
            if (ammoStoreLength > 0)
            {
                ammoStores = new int[ammoStoreLength];
                for (int i = 0; i < ammoStoreLength; ++i)
                {
                    ammoStores[i] = packet.ReadInt();
                }
            }
            bodyPose = (Sosig.SosigBodyPose)packet.ReadByte();
            byte sosigLinkIntegrityLength = packet.ReadByte();
            if (sosigLinkIntegrityLength > 0)
            {
                if (linkIntegrity == null)
                {
                    linkIntegrity = new float[sosigLinkIntegrityLength];
                }
                for (int i = 0; i < sosigLinkIntegrityLength; ++i)
                {
                    linkIntegrity[i] = packet.ReadFloat();
                }
            }
            fallbackOrder = (Sosig.SosigOrder)packet.ReadByte();
            currentOrder = (Sosig.SosigOrder)packet.ReadByte();
            jointLimit = packet.ReadFloat();
            unconscious = packet.ReadBool();
            blinded = packet.ReadBool();
            confused = packet.ReadBool();
            suppressionLevel = packet.ReadFloat();

            // Full
            byte sosigLinkDataLength = packet.ReadByte();
            if (sosigLinkDataLength > 0)
            {
                if (linkData == null)
                {
                    linkData = new float[sosigLinkDataLength][];
                }
                for (int i = 0; i < sosigLinkDataLength; ++i)
                {
                    if (linkData[i] == null || linkData[i].Length != 5)
                    {
                        linkData[i] = new float[5];
                    }

                    for (int j = 0; j < 5; ++j)
                    {
                        linkData[i][j] = packet.ReadFloat();
                    }
                }
            }
            IFF = packet.ReadByte();
            configTemplate = packet.ReadSosigConfig();
            byte linkCount = packet.ReadByte();
            wearables = new List<List<string>>();
            for (int i = 0; i < linkCount; ++i)
            {
                wearables.Add(new List<string>());
                byte wearableCount = packet.ReadByte();
                if (wearableCount > 0)
                {
                    for (int j = 0; j < wearableCount; ++j)
                    {
                        wearables[i].Add(packet.ReadString());
                    }
                }
            }
            IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(packet.ReadInt());
            int dataLen = packet.ReadInt();
            if (dataLen > 0)
            {
                data = packet.ReadBytes(dataLen);
            }
            switch (currentOrder)
            {
                case Sosig.SosigOrder.GuardPoint:
                    guardPoint = packet.ReadVector3();
                    guardDir = packet.ReadVector3();
                    hardGuard = packet.ReadBool();
                    break;
                case Sosig.SosigOrder.Skirmish:
                    skirmishPoint = packet.ReadVector3();
                    pathToPoint = packet.ReadVector3();
                    assaultPoint = packet.ReadVector3();
                    faceTowards = packet.ReadVector3();
                    break;
                case Sosig.SosigOrder.Investigate:
                    guardPoint = packet.ReadVector3();
                    hardGuard = packet.ReadBool();
                    faceTowards = packet.ReadVector3();
                    break;
                case Sosig.SosigOrder.SearchForEquipment:
                case Sosig.SosigOrder.Wander:
                    wanderPoint = packet.ReadVector3();
                    break;
                case Sosig.SosigOrder.Assault:
                    assaultPoint = packet.ReadVector3();
                    assaultSpeed = (Sosig.SosigMoveSpeed)packet.ReadByte();
                    faceTowards = packet.ReadVector3();
                    break;
                case Sosig.SosigOrder.Idle:
                    idleToPoint = packet.ReadVector3();
                    idleDominantDir = packet.ReadVector3();
                    break;
                case Sosig.SosigOrder.PathTo:
                    pathToPoint = packet.ReadVector3();
                    pathToLookDir = packet.ReadVector3();
                    break;
            }
            byte inventoryLength = packet.ReadByte();
            inventory = new int[inventoryLength];
            for (int i = 0; i < inventoryLength; ++i)
            {
                inventory[i] = packet.ReadInt();
            }
        }

        public static bool IsOfType(Transform t)
        {
            return t.GetComponent<Sosig>() != null;
        }

        public static bool IsControlled(Transform root)
        {
            Sosig sosig = root.GetComponent<Sosig>();
            if (sosig != null && sosig.Links != null)
            {
                for(int i=0; i<sosig.Links.Count; ++i)
                {
                    if(sosig.Links[i] != null && sosig.Links[i].O.m_hand != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool IsControlled(out int interactionID)
        {
            interactionID = -1;
            if (physicalSosig.physicalSosig != null && physicalSosig.physicalSosig.Links != null)
            {
                for(int i=0; i< physicalSosig.physicalSosig.Links.Count; ++i)
                {
                    if(physicalSosig.physicalSosig.Links[i] != null && physicalSosig.physicalSosig.Links[i].O.m_hand != null)
                    {
                        if (physicalSosig.physicalSosig.Links[i].O.m_hand.IsThisTheRightHand)
                        {
                            interactionID = 2; // Right hand
                        }
                        else
                        {
                            interactionID = 1; // Left hand
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private static TrackedSosig MakeTracked(Transform root, TrackedObjectData parent)
        {
            TrackedSosig trackedSosig = root.gameObject.AddComponent<TrackedSosig>();
            TrackedSosigData data = new TrackedSosigData();
            trackedSosig.sosigData = data;
            trackedSosig.data = data;
            data.physicalSosig = trackedSosig;
            data.physical = trackedSosig;
            Sosig sosigScript = root.GetComponent<Sosig>();
            data.physicalSosig.physicalSosig = sosigScript;
            data.physical.physical = sosigScript;

            data.typeIdentifier = "TrackedSosigData";
            data.active = trackedSosig.gameObject.activeInHierarchy;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = GameManager.instance;
            data.controller = GameManager.ID;
            data.initTracker = GameManager.ID;
            data.sceneInit = GameManager.InSceneInit();

            GameManager.trackedSosigBySosig.Add(sosigScript, trackedSosig);
            GameManager.trackedObjectByObject.Add(sosigScript, trackedSosig);
            for(int i=0; i < sosigScript.Links.Count; ++i)
            {
                GameManager.trackedObjectByInteractive.Add(sosigScript.Links[i].O, trackedSosig);
                GameManager.trackedObjectByDamageable.Add(sosigScript.Links[i], trackedSosig);
            }

            data.configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
            data.configTemplate.AppliesDamageResistToIntegrityLoss = sosigScript.AppliesDamageResistToIntegrityLoss;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.TotalMustard = sosigScript.m_maxMustard;
            data.configTemplate.BleedDamageMult = sosigScript.BleedDamageMult;
            data.configTemplate.BleedRateMultiplier = sosigScript.BleedRateMult;
            data.configTemplate.BleedVFXIntensity = sosigScript.BleedVFXIntensity;
            data.configTemplate.SearchExtentsModifier = sosigScript.SearchExtentsModifier;
            data.configTemplate.ShudderThreshold = sosigScript.ShudderThreshold;
            data.configTemplate.ConfusionThreshold = sosigScript.ConfusionThreshold;
            data.configTemplate.ConfusionMultiplier = sosigScript.ConfusionMultiplier;
            data.configTemplate.ConfusionTimeMax = sosigScript.m_maxConfusedTime;
            data.configTemplate.StunThreshold = sosigScript.StunThreshold;
            data.configTemplate.StunMultiplier = sosigScript.StunMultiplier;
            data.configTemplate.StunTimeMax = sosigScript.m_maxStunTime;
            data.configTemplate.HasABrain = sosigScript.HasABrain;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.RegistersPassiveThreats = sosigScript.RegistersPassiveThreats;
            data.configTemplate.CanBeKnockedOut = sosigScript.CanBeKnockedOut;
            data.configTemplate.MaxUnconsciousTime = sosigScript.m_maxUnconsciousTime;
            data.configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = sosigScript.m_assaultPointOverridesSkirmishPointWhenFurtherThan;
            data.configTemplate.ViewDistance = sosigScript.MaxSightRange;
            data.configTemplate.HearingDistance = sosigScript.MaxHearingRange;
            data.configTemplate.MaxFOV = sosigScript.MaxFOV;
            data.configTemplate.StateSightRangeMults = sosigScript.StateSightRangeMults;
            data.configTemplate.StateHearingRangeMults = sosigScript.StateHearingRangeMults;
            data.configTemplate.StateFOVMults = sosigScript.StateFOVMults;
            data.configTemplate.CanPickup_Ranged = sosigScript.CanPickup_Ranged;
            data.configTemplate.CanPickup_Melee = sosigScript.CanPickup_Melee;
            data.configTemplate.CanPickup_Other = sosigScript.CanPickup_Other;
            data.configTemplate.DoesJointBreakKill_Head = sosigScript.m_doesJointBreakKill_Head;
            data.configTemplate.DoesJointBreakKill_Upper = sosigScript.m_doesJointBreakKill_Upper;
            data.configTemplate.DoesJointBreakKill_Lower = sosigScript.m_doesJointBreakKill_Lower;
            data.configTemplate.DoesSeverKill_Head = sosigScript.m_doesSeverKill_Head;
            data.configTemplate.DoesSeverKill_Upper = sosigScript.m_doesSeverKill_Upper;
            data.configTemplate.DoesSeverKill_Lower = sosigScript.m_doesSeverKill_Lower;
            data.configTemplate.DoesExplodeKill_Head = sosigScript.m_doesExplodeKill_Head;
            data.configTemplate.DoesExplodeKill_Upper = sosigScript.m_doesExplodeKill_Upper;
            data.configTemplate.DoesExplodeKill_Lower = sosigScript.m_doesExplodeKill_Lower;
            data.configTemplate.CrawlSpeed = sosigScript.Speed_Crawl;
            data.configTemplate.SneakSpeed = sosigScript.Speed_Sneak;
            data.configTemplate.WalkSpeed = sosigScript.Speed_Walk;
            data.configTemplate.RunSpeed = sosigScript.Speed_Run;
            data.configTemplate.TurnSpeed = sosigScript.Speed_Turning;
            data.configTemplate.MovementRotMagnitude = sosigScript.MovementRotMagnitude;
            data.configTemplate.DamMult_Projectile = sosigScript.DamMult_Projectile;
            data.configTemplate.DamMult_Explosive = sosigScript.DamMult_Explosive;
            data.configTemplate.DamMult_Melee = sosigScript.DamMult_Melee;
            data.configTemplate.DamMult_Piercing = sosigScript.DamMult_Piercing;
            data.configTemplate.DamMult_Blunt = sosigScript.DamMult_Blunt;
            data.configTemplate.DamMult_Cutting = sosigScript.DamMult_Cutting;
            data.configTemplate.DamMult_Thermal = sosigScript.DamMult_Thermal;
            data.configTemplate.DamMult_Chilling = sosigScript.DamMult_Chilling;
            data.configTemplate.DamMult_EMP = sosigScript.DamMult_EMP;
            data.configTemplate.CanBeSurpressed = sosigScript.CanBeSuppresed;
            data.configTemplate.SuppressionMult = sosigScript.SuppressionMult;
            data.configTemplate.CanBeGrabbed = sosigScript.CanBeGrabbed;
            data.configTemplate.CanBeSevered = sosigScript.CanBeSevered;
            data.configTemplate.CanBeStabbed = sosigScript.CanBeStabbed;
            data.configTemplate.MaxJointLimit = sosigScript.m_maxJointLimit;
            data.configTemplate.OverrideSpeech = sosigScript.Speech;
            data.configTemplate.LinkDamageMultipliers = new List<float>();
            data.configTemplate.LinkStaggerMultipliers = new List<float>();
            data.configTemplate.StartingLinkIntegrity = new List<Vector2>();
            data.configTemplate.StartingChanceBrokenJoint = new List<float>();
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.configTemplate.LinkDamageMultipliers.Add(sosigScript.Links[i].DamMult);
                data.configTemplate.LinkStaggerMultipliers.Add(sosigScript.Links[i].StaggerMagnitude);
                float actualLinkIntegrity = sosigScript.Links[i].m_integrity;
                data.configTemplate.StartingLinkIntegrity.Add(new Vector2(actualLinkIntegrity, actualLinkIntegrity));
                data.configTemplate.StartingChanceBrokenJoint.Add(sosigScript.Links[i].m_isJointBroken ? 1 : 0);
            }
            if (sosigScript.Priority != null)
            {
                data.configTemplate.TargetCapacity = sosigScript.Priority.m_eventCapacity;
                data.configTemplate.TargetTrackingTime = sosigScript.Priority.m_maxTrackingTime;
                data.configTemplate.NoFreshTargetTime = sosigScript.Priority.m_timeToNoFreshTarget;
            }
            data.position = sosigScript.CoreRB.position;
            data.velocity = sosigScript.CoreRB.velocity;
            data.rotation = sosigScript.CoreRB.rotation;
            data.linkData = new float[sosigScript.Links.Count][];
            data.linkIntegrity = new float[data.linkData.Length];
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.linkData[i] = new float[5];
                data.linkData[i][0] = sosigScript.Links[i].StaggerMagnitude;
                data.linkData[i][1] = sosigScript.Links[i].DamMult;
                data.linkData[i][2] = sosigScript.Links[i].DamMultAVG;
                data.linkData[i][3] = sosigScript.Links[i].CollisionBluntDamageMultiplier;
                if (sosigScript.Links[i] == null)
                {
                    data.linkData[i][4] = 0;
                    data.linkIntegrity[i] = 0;
                }
                else
                {
                    data.linkData[i][4] = sosigScript.Links[i].m_integrity;
                    data.linkIntegrity[i] = data.linkData[i][4];
                }
            }

            data.wearables = new List<List<string>>();
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.wearables.Add(new List<string>());
                for (int j = 0; j < sosigScript.Links[i].m_wearables.Count; ++j)
                {
                    data.wearables[i].Add(sosigScript.Links[i].m_wearables[j].name);
                    if (data.wearables[i][j].EndsWith("(Clone)"))
                    {
                        data.wearables[i][j] = data.wearables[i][j].Substring(0, data.wearables[i][j].Length - 7);
                    }
                    if (Mod.sosigWearableMap.ContainsKey(data.wearables[i][j]))
                    {
                        data.wearables[i][j] = Mod.sosigWearableMap[data.wearables[i][j]];
                    }
                    else
                    {
                        Mod.LogError("SosigWearable: " + data.wearables[i][j] + " not found in map");
                    }
                }
            }
            data.ammoStores = sosigScript.Inventory.m_ammoStores;
            data.inventory = new int[2 + sosigScript.Inventory.Slots.Count];
            if (sosigScript.Hand_Primary.HeldObject == null)
            {
                data.inventory[0] = -1;
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Hand_Primary.HeldObject, out trackedItem) ? trackedItem : sosigScript.Hand_Primary.HeldObject.O.GetComponent<TrackedItem>();
                if (trackedItem == null)
                {
                    TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Hand_Primary.HeldObject, new KeyValuePair<TrackedSosigData, int>(data, 0));
                    data.inventory[0] = -1;
                }
                else
                {
                    if (trackedItem.data.trackedID == -1)
                    {
                        TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, 0));
                        data.inventory[0] = -1;
                    }
                    else
                    {
                        data.inventory[0] = trackedItem.data.trackedID;
                    }
                }
            }
            if (sosigScript.Hand_Secondary.HeldObject == null)
            {
                data.inventory[1] = -1;
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Hand_Secondary.HeldObject, out trackedItem) ? trackedItem : sosigScript.Hand_Secondary.HeldObject.O.GetComponent<TrackedItem>();
                if (trackedItem == null)
                {
                    TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Hand_Secondary.HeldObject, new KeyValuePair<TrackedSosigData, int>(data, 1));
                    data.inventory[1] = -1;
                }
                else
                {
                    if (trackedItem.data.trackedID == -1)
                    {
                        TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, 1));
                        data.inventory[1] = -1;
                    }
                    else
                    {
                        data.inventory[1] = trackedItem.data.trackedID;
                    }
                }
            }
            for (int i = 0; i < sosigScript.Inventory.Slots.Count; ++i)
            {
                if (sosigScript.Inventory.Slots[i].HeldObject == null)
                {
                    data.inventory[i + 2] = -1;
                }
                else
                {
                    TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Inventory.Slots[i].HeldObject, out trackedItem) ? trackedItem : sosigScript.Inventory.Slots[i].HeldObject.O.GetComponent<TrackedItem>();
                    if (trackedItem == null)
                    {
                        TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Inventory.Slots[i].HeldObject, new KeyValuePair<TrackedSosigData, int>(data, i + 2));
                        data.inventory[i + 2] = -1;
                    }
                    else
                    {
                        if (trackedItem.data.trackedID == -1)
                        {
                            TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, i + 2));
                            data.inventory[i + 2] = -1;
                        }
                        else
                        {
                            data.inventory[i + 2] = trackedItem.data.trackedID;
                        }
                    }
                }
            }
            data.mustard = sosigScript.Mustard;
            data.bodyPose = sosigScript.BodyPose;
            data.currentOrder = sosigScript.CurrentOrder;
            data.fallbackOrder = sosigScript.FallbackOrder;
            data.IFF = (byte)sosigScript.GetIFF();
            data.IFFChart = sosigScript.Priority.IFFChart;

            // Brain
            // GuardPoint
            data.guardPoint = sosigScript.GetGuardPoint();
            data.guardDir = sosigScript.GetGuardDir();
            data.hardGuard = sosigScript.m_hardGuard;
            // Skirmish
            data.skirmishPoint = sosigScript.m_skirmishPoint;
            data.pathToPoint = sosigScript.m_pathToPoint;
            data.assaultPoint = sosigScript.GetAssaultPoint();
            data.faceTowards = sosigScript.m_faceTowards;
            // SearchForEquipment
            data.wanderPoint = sosigScript.m_wanderPoint;
            // Assault
            data.assaultSpeed = sosigScript.m_assaultSpeed;
            // Idle
            data.idleToPoint = sosigScript.m_idlePoint;
            data.idleDominantDir = sosigScript.m_idleDominantDir;
            // PathTo
            data.pathToLookDir = sosigScript.m_pathToLookDir;

            data.CollectExternalData();

            //// Manage FVRInteractiveObject.All
            //for(int i= 0; i < sosigScript.Links.Count; i++)
            //{
            //    // Remove links from All if necessary
            //    if (sosigScript.Links[i].O.m_hand == null && sosigScript.Links[i].O.m_index != -1)
            //    {
            //        FVRInteractiveObject.All[sosigScript.Links[i].O.m_index] = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
            //        FVRInteractiveObject.All[sosigScript.Links[i].O.m_index].m_index = sosigScript.Links[i].O.m_index;
            //        FVRInteractiveObject.All.RemoveAt(FVRInteractiveObject.All.Count - 1);

            //        sosigScript.Links[i].O.m_index = -1;
            //    }
            //}

            // Add to local list
            data.localTrackedID = GameManager.objects.Count;
            GameManager.objects.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedSosig.awoken)
            {
                trackedSosig.data.Update(true);
            }

            Mod.LogInfo("Made sosig " + trackedSosig.name + " tracked", false);

            return trackedSosig;
        }

        public override void WriteToPacket(Packet packet, bool incrementOrder, bool full)
        {
            base.WriteToPacket(packet, incrementOrder, full);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(agentPosition);
            packet.Write(mustard);
            if (ammoStores != null && ammoStores.Length > 0)
            {
                packet.Write((byte)ammoStores.Length);
                for (int i = 0; i < ammoStores.Length; ++i)
                {
                    packet.Write(ammoStores[i]);
                }
            }
            else
            {
                packet.Write((byte)0);
            }
            packet.Write((byte)bodyPose);
            if (linkIntegrity == null || linkIntegrity.Length == 0)
            {
                packet.Write((byte)0);
            }
            else
            {
                packet.Write((byte)linkIntegrity.Length);
                for (int i = 0; i < linkIntegrity.Length; ++i)
                {
                    packet.Write(linkIntegrity[i]);
                }
            }
            packet.Write((byte)fallbackOrder);
            packet.Write((byte)currentOrder);
            packet.Write(jointLimit);
            packet.Write(unconscious);
            packet.Write(blinded);
            packet.Write(confused);
            packet.Write(suppressionLevel);

            if (full)
            {
                if (linkData == null || linkData.Length == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)linkData.Length);
                    for (int i = 0; i < linkData.Length; ++i)
                    {
                        for (int k = 0; k < 5; ++k)
                        {
                            packet.Write(linkData[i][k]);
                        }
                    }
                }
                packet.Write(IFF);
                packet.Write(configTemplate);
                packet.Write((byte)wearables.Count);
                for (int i = 0; i < wearables.Count; ++i)
                {
                    if (wearables[i] == null || wearables[i].Count == 0)
                    {
                        packet.Write((byte)0);
                    }
                    else
                    {
                        packet.Write((byte)wearables[i].Count);
                        for (int j = 0; j < wearables[i].Count; ++j)
                        {
                            packet.Write(wearables[i][j]);
                        }
                    }
                }
                packet.Write(SosigTargetPrioritySystemPatch.BoolArrToInt(IFFChart));
                if (data == null || data.Length == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(data.Length);
                    packet.Write(data);
                }
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        packet.Write(guardPoint);
                        packet.Write(guardDir);
                        packet.Write(hardGuard);
                        break;
                    case Sosig.SosigOrder.Skirmish:
                        packet.Write(skirmishPoint);
                        packet.Write(pathToPoint);
                        packet.Write(assaultPoint);
                        packet.Write(faceTowards);
                        break;
                    case Sosig.SosigOrder.Investigate:
                        packet.Write(guardPoint);
                        packet.Write(hardGuard);
                        packet.Write(faceTowards);
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                    case Sosig.SosigOrder.Wander:
                        packet.Write(wanderPoint);
                        break;
                    case Sosig.SosigOrder.Assault:
                        packet.Write(assaultPoint);
                        packet.Write((byte)assaultSpeed);
                        packet.Write(faceTowards);
                        break;
                    case Sosig.SosigOrder.Idle:
                        packet.Write(idleToPoint);
                        packet.Write(idleDominantDir);
                        break;
                    case Sosig.SosigOrder.PathTo:
                        packet.Write(pathToPoint);
                        packet.Write(pathToLookDir);
                        break;
                }
                if (inventory == null)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)inventory.Length);
                    for (int i = 0; i < inventory.Length; ++i)
                    {
                        packet.Write(inventory[i]);
                    }
                }
            }
        }

        private void CollectExternalData()
        {
            data = new byte[10 + (12 * ((TNH_ManagerPatch.inGenerateSentryPatrol || TNH_ManagerPatch.inGeneratePatrol) ? (TNH_ManagerPatch.patrolPoints == null ? 0 : TNH_ManagerPatch.patrolPoints.Count) : 0))];

            // Write TNH context
            data[0] = TNH_HoldPointPatch.inSpawnEnemyGroup ? (byte)1 : (byte)0;
            if (TNH_HoldPointPatch.inSpawnEnemyGroup && Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager.m_curHoldPoint != null)
            {
                data[1] = Mod.currentTNHInstance.manager.m_curHoldPoint.m_holdGroupLeader == physicalSosig.physicalSosig ? (byte)1 : (byte)0;
            }
            //trackedSosigData.data[1] = TNH_HoldPointPatch.inSpawnTurrets ? (byte)1 : (byte)0;
            data[2] = TNH_SupplyPointPatch.inSpawnTakeEnemyGroup ? (byte)1 : (byte)0;
            BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(data, 3);
            //trackedSosigData.data[3] = TNH_SupplyPointPatch.inSpawnDefenses ? (byte)1 : (byte)0;
            data[5] = TNH_ManagerPatch.inGenerateSentryPatrol ? (byte)1 : (byte)0;
            data[6] = TNH_ManagerPatch.inGeneratePatrol ? (byte)1 : (byte)0;
            if (TNH_ManagerPatch.inGenerateSentryPatrol || TNH_ManagerPatch.inGeneratePatrol)
            {
                BitConverter.GetBytes((short)TNH_ManagerPatch.patrolIndex).CopyTo(data, 7);
                if (TNH_ManagerPatch.patrolPoints == null || TNH_ManagerPatch.patrolPoints.Count == 0)
                {
                    data[9] = (byte)0;
                }
                else
                {
                    data[9] = (byte)TNH_ManagerPatch.patrolPoints.Count;
                    for (int i = 0; i < TNH_ManagerPatch.patrolPoints.Count; ++i)
                    {
                        int index = i * 12 + 10;
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].x).CopyTo(data, index);
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].y).CopyTo(data, index + 4);
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].z).CopyTo(data, index + 8);
                    }
                }
            }
        }

        public override IEnumerator Instantiate()
        {
            Mod.LogInfo("Instantiating sosig at "+trackedID, false);
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
            physicalSosig = sosigInstance.AddComponent<TrackedSosig>();
            physical = physicalSosig;
            physicalSosig.physicalSosig = sosigInstance.GetComponent<Sosig>();
            physical.physical = physicalSosig.physicalSosig;
            awaitingInstantiation = false;
            physicalSosig.sosigData = this;
            physicalSosig.data = this;

            SosigConfigurePatch.skipConfigure = true;
            physicalSosig.physicalSosig.Configure(configTemplate);

            GameManager.trackedSosigBySosig.Add(physicalSosig.physicalSosig, physicalSosig);
            GameManager.trackedObjectByObject.Add(physicalSosig.physicalSosig, physicalSosig);
            for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; ++i)
            {
                GameManager.trackedObjectByInteractive.Add(physicalSosig.physicalSosig.Links[i].O, physicalSosig);
                GameManager.trackedObjectByDamageable.Add(physicalSosig.physicalSosig.Links[i], physicalSosig);
            }

            AnvilManager.Run(EquipWearables());

            // Deregister the AI from the manager if we are not in control
            // Also set CoreRB as kinematic
            if (controller != GameManager.ID)
            {
                if (GM.CurrentAIManager != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(physicalSosig.physicalSosig.E);
                }
                physicalSosig.physicalSosig.CoreRB.isKinematic = true;
            }

            // Initially set IFF
            ++SosigIFFPatch.skip;
            physicalSosig.physicalSosig.SetIFF(IFF);
            --SosigIFFPatch.skip;

            // Set IFFChart
            physicalSosig.physicalSosig.Priority.IFFChart = IFFChart;

            // Initially set order
            ++SosigPatch.sosigSetCurrentOrderSkip;
            switch (currentOrder)
            {
                case Sosig.SosigOrder.GuardPoint:
                    physicalSosig.physicalSosig.CommandGuardPoint(guardPoint, hardGuard);
                    physicalSosig.physicalSosig.m_guardDominantDirection = guardDir;
                    break;
                case Sosig.SosigOrder.Skirmish:
                    physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                    physicalSosig.physicalSosig.m_skirmishPoint = skirmishPoint;
                    physicalSosig.physicalSosig.m_pathToPoint = pathToPoint;
                    physicalSosig.physicalSosig.m_assaultPoint = assaultPoint;
                    physicalSosig.physicalSosig.m_faceTowards = faceTowards;
                    break;
                case Sosig.SosigOrder.Investigate:
                    physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                    physicalSosig.physicalSosig.UpdateGuardPoint(guardPoint);
                    physicalSosig.physicalSosig.m_hardGuard = hardGuard;
                    physicalSosig.physicalSosig.m_faceTowards = faceTowards;
                    break;
                case Sosig.SosigOrder.SearchForEquipment:
                case Sosig.SosigOrder.Wander:
                    physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                    physicalSosig.physicalSosig.m_wanderPoint = wanderPoint;
                    break;
                case Sosig.SosigOrder.Assault:
                    physicalSosig.physicalSosig.CommandAssaultPoint(assaultPoint);
                    physicalSosig.physicalSosig.m_faceTowards = faceTowards;
                    physicalSosig.physicalSosig.SetAssaultSpeed(assaultSpeed);
                    break;
                case Sosig.SosigOrder.Idle:
                    physicalSosig.physicalSosig.CommandIdle(idleToPoint, idleDominantDir);
                    break;
                case Sosig.SosigOrder.PathTo:
                    physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                    physicalSosig.physicalSosig.m_pathToPoint = pathToPoint;
                    physicalSosig.physicalSosig.m_pathToLookDir = pathToLookDir;
                    break;
                default:
                    physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                    break;
            }
            physicalSosig.physicalSosig.FallbackOrder = fallbackOrder;
            --SosigPatch.sosigSetCurrentOrderSkip;

            // Setup inventory
            // Make sure sosig hands and inventory are initialized first
            physicalSosig.physicalSosig.InitHands();
            physicalSosig.physicalSosig.Inventory.Init();
            TrackedObjectData[] arrToUse = ThreadManager.host ? Server.objects : Client.objects;
            ++SosigPickUpPatch.skip;
            ++SosigPlaceObjectInPatch.skip;
            for (int i=0; i < inventory.Length; ++i)
            {
                if (inventory[i] != -1)
                {
                    TrackedItemData asTrackedItem = arrToUse[inventory[i]] as TrackedItemData;
                    if (asTrackedItem == null)
                    {
                        if (ThreadManager.host)
                        {
                            Mod.LogError("Sosig instantiation: inventory[" + i + "] = " + inventory[i] + " is missing item data!");
                        }
                        else
                        {
                            Mod.LogWarning("Sosig instantiation: inventory[" + i + "] = " + inventory[i] + " is missing item data!");
                        }
                    }
                    else if(asTrackedItem.physicalItem == null)
                    {
                        asTrackedItem.toPutInSosigInventory = new int[] { trackedID, i };
                    }
                    else
                    {
                        if (i == 0)
                        {
                            physicalSosig.physicalSosig.Hand_Primary.PickUp(((SosigWeaponPlayerInterface)asTrackedItem.physicalItem.physicalItem).W);
                        }
                        else if (i == 1)
                        {
                            physicalSosig.physicalSosig.Hand_Secondary.PickUp(((SosigWeaponPlayerInterface)asTrackedItem.physicalItem.physicalItem).W);
                        }
                        else
                        {
                            physicalSosig.physicalSosig.Inventory.Slots[i - 2].PlaceObjectIn(((SosigWeaponPlayerInterface)asTrackedItem.physicalItem.physicalItem).W);
                        }
                    }
                }
            }
            --SosigPickUpPatch.skip;
            --SosigPlaceObjectInPatch.skip;

            //// Manage FVRInteractiveObject.All
            //for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; i++)
            //{
            //    // Remove links from All if necessary
            //    if (physicalSosig.physicalSosig.Links[i].O.m_hand == null && physicalSosig.physicalSosig.Links[i].O.m_index != -1)
            //    {
            //        FVRInteractiveObject.All[physicalSosig.physicalSosig.Links[i].O.m_index] = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
            //        FVRInteractiveObject.All[physicalSosig.physicalSosig.Links[i].O.m_index].m_index = physicalSosig.physicalSosig.Links[i].O.m_index;
            //        FVRInteractiveObject.All.RemoveAt(FVRInteractiveObject.All.Count - 1);

            //        physicalSosig.physicalSosig.Links[i].O.m_index = -1;
            //    }
            //}

            ProcessData();

            // Initially set itself
            UpdateFromData(this);
        }

        private void ProcessData()
        {
            if (GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                if (data[0] == 1) // TNH_HoldPoint is in spawn enemy group
                {
                    GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_activeSosigs.Add(physicalSosig.physicalSosig);
                    if (data[1] == 1) // This sosig is group leader
                    {
                        GM.TNH_Manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_holdGroupLeader = physicalSosig.physicalSosig;
                    }
                }
                else if (data[2] == 1) // TNH_SupplyPoint is in Spawn Take Enemy Group
                {
                    GM.TNH_Manager.SupplyPoints[BitConverter.ToInt16(data, 3)].m_activeSosigs.Add(physicalSosig.physicalSosig);
                    GM.TNH_Manager.RegisterGuard(physicalSosig.physicalSosig);
                }
                else if (data[5] == 1 || data[6] == 1) // TNH_Manager is in generate patrol
                {
                    physicalSosig.physicalSosig.SetAssaultSpeed(Sosig.SosigMoveSpeed.Walking);
                    int patrolIndex = BitConverter.ToInt16(data, 7);
                    if (latestSosigPatrolSquad.Key == patrolIndex)
                    {
                        latestSosigPatrolSquad.Value.Squad.Add(physicalSosig.physicalSosig);
                    }
                    else
                    {
                        latestSosigPatrolSquad = new KeyValuePair<int, TNH_Manager.SosigPatrolSquad>(patrolIndex, new TNH_Manager.SosigPatrolSquad());
                        latestSosigPatrolSquad.Value.PatrolPoints = new List<Vector3>();
                        int pointCount = data[9];
                        for (int i = 0; i < pointCount; ++i)
                        {
                            int firstIndex = i * 12 + 10;
                            latestSosigPatrolSquad.Value.PatrolPoints.Add(new Vector3(BitConverter.ToSingle(data, firstIndex),
                                                                                      BitConverter.ToSingle(data, firstIndex + 4),
                                                                                      BitConverter.ToSingle(data, firstIndex + 8)));
                        }
                        latestSosigPatrolSquad.Value.Squad.Add(physicalSosig.physicalSosig);
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
                            GameObject outfitItemObject = GameObject.Instantiate(IM.OD[wearables[i][j]].GetGameObject(), physicalSosig.physicalSosig.Links[i].transform.position, physicalSosig.physicalSosig.Links[i].transform.rotation, physicalSosig.physicalSosig.Links[i].transform);
                            --Mod.skipAllInstantiates;
                            SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                            ++SosigLinkActionPatch.skipRegisterWearable;
                            wearableScript.RegisterWearable(physicalSosig.physicalSosig.Links[i]);
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
                if(physicalSosig == null || physicalSosig.physicalSosig.Links[linkIndex] == null)
                {
                    // Sosig or sosig link could have been destroyed between iterations
                    yield break;
                }
                ++Mod.skipAllInstantiates;
                if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at equipwearable, setting to 1"); Mod.skipAllInstantiates = 1; }
                GameObject outfitItemObject = GameObject.Instantiate(IM.OD[ID].GetGameObject(), physicalSosig.physicalSosig.Links[linkIndex].transform.position, physicalSosig.physicalSosig.Links[linkIndex].transform.rotation, physicalSosig.physicalSosig.Links[linkIndex].transform);
                --Mod.skipAllInstantiates;
                SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                if (skip)
                {
                    ++SosigLinkActionPatch.skipRegisterWearable;
                }
                wearableScript.RegisterWearable(physicalSosig.physicalSosig.Links[linkIndex]);
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

        public override void UpdateFromData(TrackedObjectData updatedObject, bool full = false)
        {
            base.UpdateFromData(updatedObject, full);

            TrackedSosigData updatedSosig = updatedObject as TrackedSosigData;

            if (full)
            {
                linkData = updatedSosig.linkData;
                IFF = updatedSosig.IFF;
                configTemplate = updatedSosig.configTemplate;
                wearables = updatedSosig.wearables;
                IFFChart = updatedSosig.IFFChart;
                data = updatedSosig.data;
                guardPoint = updatedSosig.guardPoint;
                guardDir = updatedSosig.guardDir;
                hardGuard = updatedSosig.hardGuard;
                skirmishPoint = updatedSosig.skirmishPoint;
                pathToPoint = updatedSosig.pathToPoint;
                assaultPoint = updatedSosig.assaultPoint;
                faceTowards = updatedSosig.faceTowards;
                wanderPoint = updatedSosig.wanderPoint;
                assaultSpeed = updatedSosig.assaultSpeed;
                idleToPoint = updatedSosig.idleToPoint;
                idleDominantDir = updatedSosig.idleDominantDir;
                pathToLookDir = updatedSosig.pathToLookDir;
                inventory = updatedSosig.inventory;
            }

            // Set data
            previousPos = position;
            previousRot = rotation;
            position = updatedSosig.position;
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = updatedSosig.rotation;
            previousAgentPos = agentPosition;
            agentPosition = updatedSosig.agentPosition;
            previousAmmoStores = ammoStores;
            ammoStores = updatedSosig.ammoStores;
            previousMustard = mustard;
            mustard = updatedSosig.mustard;
            previousLinkIntegrity = linkIntegrity;
            linkIntegrity = updatedSosig.linkIntegrity;
            previousBodyPose = bodyPose;
            bodyPose = updatedSosig.bodyPose;
            fallbackOrder = updatedSosig.fallbackOrder;
            previousOrder = currentOrder;
            currentOrder = updatedSosig.currentOrder;
            previousJointLimit = jointLimit;
            jointLimit = updatedSosig.jointLimit;
            previousUnconscious = unconscious;
            unconscious = updatedSosig.unconscious;
            previousBlinded = blinded;
            blinded = updatedSosig.blinded;
            previousConfused = confused;
            confused = updatedSosig.confused;
            previousSuppressionLevel = suppressionLevel;
            suppressionLevel = updatedSosig.suppressionLevel;

            // Set physically
            if (physicalSosig != null)
            {
                physicalSosig.physicalSosig.Agent.transform.position = agentPosition;
                physicalSosig.physicalSosig.FallbackOrder = fallbackOrder;
                physicalSosig.physicalSosig.Mustard = mustard;
                //physicalObject.physicalSosigScript.CoreRB.position = position;
                //physicalObject.physicalSosigScript.CoreRB.rotation = rotation;
                physicalSosig.physicalSosig.SetBodyPose(bodyPose);
                physicalSosig.physicalSosig.Inventory.m_ammoStores = ammoStores;
                for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; ++i)
                {
                    if (physicalSosig.physicalSosig.Links[i] != null)
                    {
                        if (previousLinkIntegrity[i] != linkIntegrity[i])
                        {
                            physicalSosig.physicalSosig.Links[i].m_integrity = linkIntegrity[i];
                            physicalSosig.physicalSosig.UpdateRendererOnLink(i);
                        }
                    }
                }
                if (previousJointLimit != jointLimit)
                {
                    physicalSosig.physicalSosig.UpdateJoints(Mathf.InverseLerp(60f, physicalSosig.physicalSosig.m_maxJointLimit, jointLimit));
                }
                physicalSosig.physicalSosig.m_isUnconscious = unconscious;
                physicalSosig.physicalSosig.m_isBlinded = blinded;
                physicalSosig.physicalSosig.m_isConfused = confused;
                physicalSosig.physicalSosig.m_suppressionLevel = suppressionLevel;
            }
        }

        public override void UpdateFromPacket(Packet packet, bool full = false)
        {
            base.UpdateFromPacket(packet, full);

            int debugStep = 0;

            try
            {
                previousPos = position;
                previousRot = rotation;
                position = packet.ReadVector3();
                velocity = previousPos == null ? Vector3.zero : position - previousPos;
                rotation = packet.ReadQuaternion();
                previousAgentPos = agentPosition;
                agentPosition = packet.ReadVector3();
                previousMustard = mustard;
                mustard = packet.ReadFloat();
                previousAmmoStores = ammoStores;
                byte ammoStoreLength = packet.ReadByte();
                if (ammoStoreLength > 0)
                {
                    ammoStores = new int[ammoStoreLength];
                    for (int i = 0; i < ammoStoreLength; ++i)
                    {
                        ammoStores[i] = packet.ReadInt();
                    }
                }
                ++debugStep;
                previousBodyPose = bodyPose;
                bodyPose = (Sosig.SosigBodyPose)packet.ReadByte();
                byte sosigLinkIntegrityLength = packet.ReadByte();
                previousLinkIntegrity = linkIntegrity;
                if (sosigLinkIntegrityLength > 0)
                {
                    if (linkIntegrity == null)
                    {
                        linkIntegrity = new float[sosigLinkIntegrityLength];
                    }
                    for (int i = 0; i < sosigLinkIntegrityLength; ++i)
                    {
                        linkIntegrity[i] = packet.ReadFloat();
                    }
                }
                fallbackOrder = (Sosig.SosigOrder)packet.ReadByte();
                previousOrder = currentOrder;
                currentOrder = (Sosig.SosigOrder)packet.ReadByte();
                previousJointLimit = jointLimit;
                jointLimit = packet.ReadFloat();
                ++debugStep;
                previousUnconscious = unconscious;
                unconscious = packet.ReadBool();
                previousBlinded = blinded;
                blinded = packet.ReadBool();
                previousConfused = confused;
                confused = packet.ReadBool();
                previousSuppressionLevel = suppressionLevel;
                suppressionLevel = packet.ReadFloat();

                if (full)
                {
                    ++debugStep;
                    byte sosigLinkDataLength = packet.ReadByte();
                    if (sosigLinkDataLength > 0)
                    {
                        if (linkData == null)
                        {
                            linkData = new float[sosigLinkDataLength][];
                        }
                        for (int i = 0; i < sosigLinkDataLength; ++i)
                        {
                            if (linkData[i] == null || linkData[i].Length != 5)
                            {
                                linkData[i] = new float[5];
                            }

                            for (int j = 0; j < 5; ++j)
                            {
                                linkData[i][j] = packet.ReadFloat();
                            }
                        }
                    }
                    ++debugStep;
                    IFF = packet.ReadByte();
                    configTemplate = packet.ReadSosigConfig();
                    byte linkCount = packet.ReadByte();
                    wearables = new List<List<string>>();
                    for (int i = 0; i < linkCount; ++i)
                    {
                        wearables.Add(new List<string>());
                        byte wearableCount = packet.ReadByte();
                        if (wearableCount > 0)
                        {
                            for (int j = 0; j < wearableCount; ++j)
                            {
                                wearables[i].Add(packet.ReadString());
                            }
                        }
                    }
                    ++debugStep;
                    IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(packet.ReadInt());
                    ++debugStep;
                    int dataLen = packet.ReadInt();
                    if (dataLen > 0)
                    {
                        data = packet.ReadBytes(dataLen);
                    }
                    ++debugStep;
                    switch (currentOrder)
                    {
                        case Sosig.SosigOrder.GuardPoint:
                            guardPoint = packet.ReadVector3();
                            guardDir = packet.ReadVector3();
                            hardGuard = packet.ReadBool();
                            break;
                        case Sosig.SosigOrder.Skirmish:
                            skirmishPoint = packet.ReadVector3();
                            pathToPoint = packet.ReadVector3();
                            assaultPoint = packet.ReadVector3();
                            faceTowards = packet.ReadVector3();
                            break;
                        case Sosig.SosigOrder.Investigate:
                            guardPoint = packet.ReadVector3();
                            hardGuard = packet.ReadBool();
                            faceTowards = packet.ReadVector3();
                            break;
                        case Sosig.SosigOrder.SearchForEquipment:
                        case Sosig.SosigOrder.Wander:
                            wanderPoint = packet.ReadVector3();
                            break;
                        case Sosig.SosigOrder.Assault:
                            assaultPoint = packet.ReadVector3();
                            assaultSpeed = (Sosig.SosigMoveSpeed)packet.ReadByte();
                            faceTowards = packet.ReadVector3();
                            break;
                        case Sosig.SosigOrder.Idle:
                            idleToPoint = packet.ReadVector3();
                            idleDominantDir = packet.ReadVector3();
                            break;
                        case Sosig.SosigOrder.PathTo:
                            pathToPoint = packet.ReadVector3();
                            pathToLookDir = packet.ReadVector3();
                            break;
                    }
                    ++debugStep;
                    byte inventoryLength = packet.ReadByte();
                    inventory = new int[inventoryLength];
                    for (int i = 0; i < inventoryLength; ++i)
                    {
                        inventory[i] = packet.ReadInt();
                    }
                    ++debugStep;
                }
            }
            catch(Exception ex)
            {
                Mod.LogError("Sosig "+trackedID+" with local index "+localWaitingIndex+", update from packet with size: "+packet.buffer.Count+" error at step: "+debugStep+": "+ex.Message+":\n"+ex.StackTrace);
            }

            // Set physically
            if (physicalSosig != null)
            {
                physicalSosig.physicalSosig.Agent.transform.position = agentPosition;
                physicalSosig.physicalSosig.FallbackOrder = fallbackOrder;
                physicalSosig.physicalSosig.Mustard = mustard;
                //physicalObject.physicalSosigScript.CoreRB.position = position;
                //physicalObject.physicalSosigScript.CoreRB.rotation = rotation;
                physicalSosig.physicalSosig.SetBodyPose(bodyPose);
                physicalSosig.physicalSosig.Inventory.m_ammoStores = ammoStores;
                for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; ++i)
                {
                    if (physicalSosig.physicalSosig.Links[i] != null)
                    {
                        if (previousLinkIntegrity[i] != linkIntegrity[i])
                        {
                            physicalSosig.physicalSosig.Links[i].m_integrity = linkIntegrity[i];
                            physicalSosig.physicalSosig.UpdateRendererOnLink(i);
                        }
                    }
                }
                if (previousJointLimit != jointLimit)
                {
                    physicalSosig.physicalSosig.UpdateJoints(Mathf.InverseLerp(60f, physicalSosig.physicalSosig.m_maxJointLimit, jointLimit));
                }
                physicalSosig.physicalSosig.m_isUnconscious = unconscious;
                physicalSosig.physicalSosig.m_isBlinded = blinded;
                physicalSosig.physicalSosig.m_isConfused = confused;
                physicalSosig.physicalSosig.m_suppressionLevel = suppressionLevel;
            }
        }

        public override bool Update(bool full = false)
        {
            bool updated = base.Update(full);

            if (physicalSosig == null)
            {
                return false;
            }

            position = physicalSosig.physicalSosig.CoreRB == null ? previousPos : physicalSosig.physicalSosig.CoreRB.position;
            bool updatePosition = false;
            if (Vector3.Distance(previousPos, position) > 0.02f)
            {
                previousPos = position;
                updatePosition = true;
            }
            velocity = previousPos == null ? Vector3.zero : position - previousPos;
            rotation = physicalSosig.physicalSosig.CoreRB == null ? previousRot : physicalSosig.physicalSosig.CoreRB.rotation;
            bool updateRotation = false;
            if (Quaternion.Angle(previousRot, rotation) > 5)
            {
                previousRot = rotation;
                updateRotation = true;
            }
            previousAgentPos = agentPosition;
            agentPosition = physicalSosig.physicalSosig.Agent.transform.position;
            previousBodyPose = bodyPose;
            bodyPose = physicalSosig.physicalSosig.BodyPose;
            ammoStores = physicalSosig.physicalSosig.Inventory.m_ammoStores;
            if (ammoStores != null && previousAmmoStores == null)
            {
                previousAmmoStores = new int[ammoStores.Length];
            }
            bool ammoStoresModified = false;
            for (int i = 0; i < ammoStores.Length; ++i)
            {
                if (ammoStores[i] != previousAmmoStores[i])
                {
                    ammoStoresModified = true;
                }
                previousAmmoStores[i] = ammoStores[i];
            }
            previousAmmoStores = ammoStores;
            previousMustard = mustard;
            mustard = physicalSosig.physicalSosig.Mustard;
            previousLinkIntegrity = linkIntegrity;
            if (linkIntegrity == null || linkIntegrity.Length < physicalSosig.physicalSosig.Links.Count)
            {
                linkIntegrity = new float[physicalSosig.physicalSosig.Links.Count];
                previousLinkIntegrity = new float[physicalSosig.physicalSosig.Links.Count];
            }
            bool modifiedLinkIntegrity = false;
            for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; ++i)
            {
                linkIntegrity[i] = physicalSosig.physicalSosig.Links[i].m_integrity;
                if (linkIntegrity[i] != previousLinkIntegrity[i])
                {
                    modifiedLinkIntegrity = true;
                }
            }
            fallbackOrder = physicalSosig.physicalSosig.FallbackOrder;
            previousOrder = currentOrder;
            currentOrder = physicalSosig.physicalSosig.CurrentOrder;
            if (previousOrder != currentOrder)
            {
                if (ThreadManager.host)
                {
                    // Could still be -1 even on host if this is the initial update, when we make the sosig tracked
                    if (trackedID != -1)
                    {
                        ServerSend.SosigSetCurrentOrder(this, currentOrder);
                    }
                }
                else if (trackedID != -1)
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
            previousJointLimit = jointLimit;
            for(int i = 0; i < physicalSosig.physicalSosig.m_joints.Count; i++)
            {
                if (physicalSosig.physicalSosig.m_joints[i] != null)
                {
                    jointLimit = physicalSosig.physicalSosig.m_joints[i].highTwistLimit.limit;
                    break;
                }
            }
            previousUnconscious = unconscious;
            unconscious = physicalSosig.physicalSosig.IsUnconscious;
            previousBlinded = blinded;
            blinded = physicalSosig.physicalSosig.IsBlinded;
            previousConfused = confused;
            confused = physicalSosig.physicalSosig.IsConfused;
            previousSuppressionLevel = suppressionLevel;
            suppressionLevel = physicalSosig.physicalSosig.SuppressionLevel;

            return updated || ammoStoresModified || modifiedLinkIntegrity || updatePosition || updateRotation || NeedsUpdate();
        }

        public override bool NeedsUpdate()
        {
            return base.NeedsUpdate() || previousAgentPos != agentPosition || previousMustard != mustard || previousJointLimit != jointLimit || previousUnconscious != unconscious
                   || previousBlinded != blinded || previousConfused != confused || previousSuppressionLevel != suppressionLevel;
        }

        public override void OnTrackedIDReceived(TrackedObjectData newData)
        {
            base.OnTrackedIDReceived(newData);

            if (TrackedSosig.unknownTNHKills.ContainsKey(localWaitingIndex))
            {
                ClientSend.TNHSosigKill(TrackedSosig.unknownTNHKills[localWaitingIndex], trackedID);

                // Remove from local
                TrackedSosig.unknownTNHKills.Remove(localWaitingIndex);
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
                                ClientSend.SosigPickUpItem(physicalSosig, upper[i].Value.Key.trackedID, upper[i].Value.Value == 0);
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
            if(localTrackedID != -1 && TrackedSosig.unknownWearable.TryGetValue(localWaitingIndex, out Dictionary<string, List<int>> wearableEntries))
            {
                foreach(KeyValuePair<string, List<int>> entry in wearableEntries)
                {
                    for (int i = 0; i < entry.Value.Count; ++i) 
                    {
                        ClientSend.SosigLinkRegisterWearable(trackedID, entry.Value[i], entry.Key);
                    }
                }

                TrackedSosig.unknownWearable.Remove(localWaitingIndex);
            }
        }

        public override void RemoveFromLocal()
        {
            base.RemoveFromLocal();

            // Manage unknown lists
            if (trackedID == -1)
            {
                TrackedSosig.unknownItemInteract.Remove(localWaitingIndex);
                TrackedSosig.unknownSetIFFs.Remove(localWaitingIndex);
                TrackedSosig.unknownSetOriginalIFFs.Remove(localWaitingIndex);
                TrackedSosig.unknownBodyStates.Remove(localWaitingIndex);
                TrackedSosig.unknownTNHKills.Remove(localWaitingIndex);
                TrackedSosig.unknownIFFChart.Remove(localWaitingIndex);
                TrackedSosig.unknownCurrentOrder.Remove(localWaitingIndex);
                TrackedSosig.unknownConfiguration.Remove(localWaitingIndex);
                TrackedSosig.unknownWearable.Remove(localWaitingIndex);

                // If not tracked, make sure we remove from tracked lists in case object was unawoken
                if (physicalSosig != null && physicalSosig.physicalSosig != null)
                {
                    GameManager.trackedSosigBySosig.Remove(physicalSosig.physicalSosig);
                    for (int i = 0; i < physicalSosig.physicalSosig.Links.Count; ++i)
                    {
                        GameManager.trackedObjectByInteractive.Remove(physicalSosig.physicalSosig.Links[i].O);
                        GameManager.trackedObjectByDamageable.Remove(physicalSosig.physicalSosig.Links[i]);
                    }
                }
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
                    TrackedObjectData[] arrToUse = ThreadManager.host ? Server.objects : Client.objects;

                    if (arrToUse[inventory[i]] != null && arrToUse[inventory[i]].controller != GameManager.ID)
                    {
                        TrackedItemData trackedItem = arrToUse[inventory[i]] as TrackedItemData;
                        trackedItem.TakeControlRecursive();
                        if (trackedItem.physicalItem != null)
                        {
                            Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
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
                if (inventory != null)
                {
                    TakeInventoryControl();
                }

                if (physicalSosig != null && physicalSosig.physicalSosig != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(physicalSosig.physicalSosig.E);
                    }
                    if (physicalSosig.physicalSosig.CoreRB != null)
                    {
                        physicalSosig.physicalSosig.CoreRB.isKinematic = false;
                    }
                }
            }
            else if (controller == GameManager.ID) // Lose control
            {
                if (physicalSosig != null && physicalSosig.physicalSosig != null)
                {
                    physicalSosig.EnsureUncontrolled();

                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.DeRegisterAIEntity(physicalSosig.physicalSosig.E);
                    }
                    if (physicalSosig.physicalSosig.CoreRB != null)
                    {
                        physicalSosig.physicalSosig.CoreRB.isKinematic = true;
                    }
                }
            }
        }
    }
}
