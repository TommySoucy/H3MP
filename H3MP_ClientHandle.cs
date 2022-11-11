using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

namespace H3MP
{
    internal class H3MP_ClientHandle
    {
        public static void Welcome(H3MP_Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();

            Debug.Log($"Message from server: {msg}");

            H3MP_Client.singleton.ID = ID;
            H3MP_ClientSend.WelcomeReceived();

            H3MP_Client.singleton.udp.Connect(((IPEndPoint)H3MP_Client.singleton.tcp.socket.Client.LocalEndPoint).Port);
        }

        public static void SpawnPlayer(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();

            H3MP_GameManager.singleton.SpawnPlayer(ID, username, scene, instance, position, rotation);
        }

        public static void ConnectSync(H3MP_Packet packet)
        {
            bool inControl = packet.ReadBool();

            // Just connected, sync if current scene is syncable
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(SceneManager.GetActiveScene().name))
            {
                H3MP_GameManager.SyncTrackedSosigs(true, inControl);
                H3MP_GameManager.SyncTrackedItems(true, inControl);
            }
        }

        public static void PlayerState(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            Vector3 headPos = packet.ReadVector3();
            Quaternion headRot = packet.ReadQuaternion();
            Vector3 torsoPos = packet.ReadVector3();
            Quaternion torsoRot = packet.ReadQuaternion();
            Vector3 leftHandPos = packet.ReadVector3();
            Quaternion leftHandRot = packet.ReadQuaternion();
            Vector3 rightHandPos = packet.ReadVector3();
            Quaternion rightHandRot = packet.ReadQuaternion();
            float health = packet.ReadFloat();
            int maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if (additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            H3MP_GameManager.UpdatePlayerState(ID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                               leftHandPos, leftHandRot,
                                               rightHandPos, rightHandRot,
                                               health, maxHealth, additionalData);
        }

        public static void PlayerScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(ID, scene);
        }

        public static void PlayerInstance(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerInstance(ID, instance);
        }

        public static void TrackedItems(H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void TrackedSosigs(H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TrackedItem(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedItem(packet.ReadTrackedItem(true), packet.ReadString(), packet.ReadInt());
        }

        public static void TrackedSosig(H3MP_Packet packet)
        {
            H3MP_Client.AddTrackedSosig(packet.ReadTrackedSosig(true), packet.ReadString(), packet.ReadInt());
        }

        public static void AddSyncScene(H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            H3MP_GameManager.synchronizedScenes.Add(scene, ID);
        }

        public static void GiveControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if (trackedItem.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                FVRPhysicalObject physObj = trackedItem.physicalObject.GetComponent<FVRPhysicalObject>();

                H3MP_GameManager.EnsureUncontrolled(physObj);

                physObj.StoreAndDestroyRigidbody();
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            else if(trackedItem.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedItem.controller = controllerID;
                trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(trackedItem);
            }
            trackedItem.controller = controllerID;
        }

        public static void GiveSosigControl(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[trackedID];

            if (trackedSosig.controller == H3MP_Client.singleton.ID && controllerID != H3MP_Client.singleton.ID)
            {
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                trackedSosig.localTrackedID = -1;

                if (trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(trackedSosig.physicalObject.physicalSosig.E);
                    trackedSosig.physicalObject.physicalSosig.CoreRB.isKinematic = true;
                }
            }
            else if(trackedSosig.controller != H3MP_Client.singleton.ID && controllerID == H3MP_Client.singleton.ID)
            {
                trackedSosig.controller = controllerID;
                trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                H3MP_GameManager.sosigs.Add(trackedSosig);

                if (trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosig.E);
                    trackedSosig.physicalObject.physicalSosig.CoreRB.isKinematic = false;
                }
            }
            trackedSosig.controller = controllerID;
        }

        public static void DestroyItem(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[trackedID];

            if (trackedItem != null)
            {
                if (trackedItem.physicalObject == null)
                {
                    if (trackedItem.controller == H3MP_Client.singleton.ID)
                    {
                        H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                        H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                        H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                    }
                }
                else
                {
                    trackedItem.physicalObject.sendDestroy = false;
                    GameObject.Destroy(trackedItem.physicalObject.gameObject);
                }
                H3MP_Client.items[trackedID] = null;
            }
        }

        public static void DestroySosig(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[trackedID];

            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject == null)
                {
                    if (trackedSosig.controller == H3MP_Client.singleton.ID)
                    {
                        H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                        H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                        H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                    }
                }
                else
                {
                    H3MP_GameManager.trackedSosigBySosig.Remove(trackedSosig.physicalObject.physicalSosig);
                    trackedSosig.physicalObject.sendDestroy = false;
                    GameObject.Destroy(trackedSosig.physicalObject.gameObject);
                }
                H3MP_Client.items[trackedID] = null;
            }
        }

        public static void ItemParent(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            H3MP_Client.items[trackedID].SetParent(newParentID);
        }

        public static void WeaponFire(H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Client.items[trackedID].physicalObject != null)
            {
                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Client.items[trackedID].physicalObject.fireFunc();
            }
        }

        public static void PlayerDamage(H3MP_Packet packet)
        {
            H3MP_PlayerHitbox.Part part = (H3MP_PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_GameManager.ProcessPlayerDamage(part, damage);
        }

        public static void SosigPickUpItem(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosig.Hand_Primary.PickUp(H3MP_Client.items[itemTrackedID].physicalObject.GetComponent<SosigWeapon>());
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosig.Hand_Secondary.PickUp(H3MP_Client.items[itemTrackedID].physicalObject.GetComponent<SosigWeapon>());
                }
            }
        }

        public static void SosigPlaceItemIn(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosig.Inventory.Slots[slotIndex].PlaceObjectIn(H3MP_Client.items[itemTrackedID].physicalObject.GetComponent<SosigWeapon>());
            }
        }

        public static void SosigDropSlot(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosig.Inventory.Slots[slotIndex].DetachHeldObject();
            }
        }

        public static void SosigHandDrop(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if(trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosig.Hand_Primary.DropHeldObject();
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosig.Hand_Secondary.DropHeldObject();
                }
            }
        }

        public static void SosigConfigure(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;
                if (trackedSosig.physicalObject != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalObject.physicalSosig.Configure(config);
                }
            }
        }

        public static void SosigLinkRegisterWearable(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if(trackedSosig.physicalObject != null)
                    {
                        foreach(SosigLink link in trackedSosig.physicalObject.physicalSosig.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while(trackedSosig.wearables.Count <= linkIndex)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                }
                trackedSosig.wearables[linkIndex].Add(wearableID);

                if (trackedSosig.physicalObject != null)
                {
                    AnvilManager.Run(trackedSosig.EquipWearable(linkIndex, wearableID, true));
                }
            }
            else // We could receive a register wearable packet before the sosig is receied
            {
                // NOTE: Not sure if this is actually possible, but ill leave this here to handle the case anyway

                // Keep the wearables in a dictionary that we will query once we instantiate the sosig
                if (H3MP_GameManager.waitingWearables.ContainsKey(sosigTrackedID))
                {
                    if (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                    {
                        while (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                        {
                            H3MP_GameManager.waitingWearables[sosigTrackedID].Add(new List<string>());
                        }
                    }
                    H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Add(wearableID);
                }
                else
                {
                    H3MP_GameManager.waitingWearables.Add(sosigTrackedID, new List<List<string>>());
                    while (H3MP_GameManager.waitingWearables[sosigTrackedID].Count <= linkIndex)
                    {
                        H3MP_GameManager.waitingWearables[sosigTrackedID].Add(new List<string>());
                    }
                    H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Add(wearableID);
                }
            }
        }

        public static void SosigLinkDeRegisterWearable(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables != null)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                        {
                            if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                            {
                                trackedSosig.wearables[linkIndex].RemoveAt(i);
                                if (trackedSosig.physicalObject != null)
                                {
                                    ++SosigLinkActionPatch.skipDeRegisterWearable;
                                    trackedSosig.physicalObject.physicalSosig.Links[linkIndex].DeRegisterWearable((wearablesField.GetValue(trackedSosig.physicalObject.physicalSosig.Links[linkIndex]) as List<SosigWearable>)[i]);
                                    --SosigLinkActionPatch.skipDeRegisterWearable;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        trackedSosig.wearables[linkIndex].Remove(wearableID);
                    }
                }
            }
            else if(H3MP_GameManager.waitingWearables.ContainsKey(sosigTrackedID)
                    && H3MP_GameManager.waitingWearables[sosigTrackedID].Count > linkIndex)
            {
                H3MP_GameManager.waitingWearables[sosigTrackedID][linkIndex].Remove(wearableID);
            }
        }

        public static void SosigSetIFF(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosig.SetIFF(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigSetOriginalIFF(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosig.SetOriginalIFFTeam(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigLinkDamage(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosig.Links[linkIndex].Damage(damage);
                        --SosigLinkDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigWearableDamage(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigWearableDamagePatch.skip;
                        (Mod.SosigLink_m_wearables.GetValue(trackedSosig.physicalObject.physicalSosig.Links[linkIndex]) as List<SosigWearable>)[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigDamageData(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller != H3MP_Client.singleton.ID && trackedSosig.physicalObject != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalObject.physicalSosig;
                    Mod.Sosig_m_isStunned.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_stunTimeLeft = packet.ReadFloat();
                    physicalSosig.BodyState = (Sosig.SosigBodyState)packet.ReadByte();
                    Mod.Sosig_m_isOnOffMeshLinkField.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.Agent.autoTraverseOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.enabled = packet.ReadBool();
                    List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(physicalSosig);
                    byte jointCount = packet.ReadByte();
                    for (int i = 0; i < jointCount; ++i)
                    {
                        if (joints[i] != null)
                        {
                            SoftJointLimit softJointLimit = joints[i].lowTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].lowTwistLimit = softJointLimit;
                            softJointLimit = joints[i].highTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].highTwistLimit = softJointLimit;
                            softJointLimit = joints[i].swing1Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing1Limit = softJointLimit;
                            softJointLimit = joints[i].swing2Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing2Limit = softJointLimit;
                        }
                    }
                    Mod.Sosig_m_isCountingDownToStagger.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_staggerAmountToApply.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveringFromBallisticState.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_recoveryFromBallisticLerp.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_tickDownToWrithe.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveryFromBallisticTick.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_lastIFFDamageSource.SetValue(physicalSosig, packet.ReadByte());
                    Mod.Sosig_m_diedFromClass.SetValue(physicalSosig, (Damage.DamageClass)packet.ReadByte());
                    Mod.Sosig_m_isBlinded.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_blindTime.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isFrozen.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_debuffTime_Freeze.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_receivedHeadShot.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_timeSinceLastDamage.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isConfused.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_confusedTime = packet.ReadFloat();
                    Mod.Sosig_m_storedShudder.SetValue(physicalSosig, packet.ReadFloat());
                }
            }
        }

        public static void SosigLinkExplodes(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosig.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }
        }

        public static void SosigDies(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosig.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }
        }

        public static void SosigClear(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosig.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
                }
            }
        }

        public static void PlaySosigFootStepSound(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (H3MP_Client.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = H3MP_Client.sosigs[sosigTrackedID].physicalObject.physicalSosig.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }
        }

        public static void SosigSpeakState(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Investigate:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnInvestigate });
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnSearchingForGuns });
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnTakingCover });
                        break;
                    case Sosig.SosigOrder.Wander:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Assault:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosig, new object[] { trackedSosig.physicalObject.physicalSosig.Speech.OnAssault });
                        break;
                }
            }
        }

        public static void SosigSetCurrentOrder(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigSetCurrentOrderSkip;
                trackedSosig.physicalObject.physicalSosig.SetCurrentOrder(currentOrder);
                --SosigActionPatch.sosigSetCurrentOrderSkip;
            }
        }

        public static void SosigVaporize(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosig.Vaporize(trackedSosig.physicalObject.physicalSosig.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }
        }

        public static void SosigLinkBreak(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosig.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }
        }

        public static void SosigLinkSever(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                Mod.Sosig_SeverJoint.Invoke(trackedSosig.physicalObject.physicalSosig.Links[linkIndex], new object[] { damClass, isPullApart });
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }
        }

        public static void SosigRequestHitDecal(H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                ++SosigActionPatch.sosigRequestHitDecalSkip;
                trackedSosig.physicalObject.physicalSosig.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalObject.physicalSosig.Links[linkIndex]);
                --SosigActionPatch.sosigRequestHitDecalSkip;
            }
        }

        public static void RequestUpToDateObjects(H3MP_Packet packet)
        {
            H3MP_ClientSend.UpToDateObjects();
        }
    }
}
