﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;

namespace H3MP
{
    internal class H3MP_ClientSend
    {
        private static void SendTCPData(H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Client.singleton.tcp.SendData(packet);
        }

        private static void SendUDPData(H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Client.singleton.udp.SendData(packet);
        }

        public static void WelcomeReceived()
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.welcomeReceived))
            {
                packet.Write(H3MP_Client.singleton.ID);
                packet.Write(Mod.config["Username"].ToString());
                packet.Write(SceneManager.GetActiveScene().name);
                packet.Write(H3MP_GameManager.instance);

                SendTCPData(packet);
            }
        }

        public static void PlayerState(Vector3 playerPos, Quaternion playerRot, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot,
                                       Vector3 rightHandPos, Quaternion rightHandRot,
                                       float health, int maxHealth)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerState))
            {
                packet.Write(playerPos);
                packet.Write(playerRot);
                packet.Write(headPos);
                packet.Write(headRot);
                packet.Write(torsoPos);
                packet.Write(torsoRot);
                packet.Write(leftHandPos);
                packet.Write(leftHandRot);
                packet.Write(rightHandPos);
                packet.Write(rightHandRot);
                packet.Write(health);
                packet.Write(maxHealth);
                byte[] additionalData = H3MP_GameManager.playerStateAddtionalDataSize == -1 ? null : new byte[H3MP_GameManager.playerStateAddtionalDataSize];
                H3MP_GameManager.WriteAdditionalPlayerState(additionalData);
                if(additionalData != null && additionalData.Length > 0)
                {
                    H3MP_GameManager.playerStateAddtionalDataSize = additionalData.Length;
                    packet.Write((short)additionalData.Length);
                    packet.Write(additionalData);
                }
                else
                {
                    H3MP_GameManager.playerStateAddtionalDataSize = 0;
                    packet.Write((short)0);
                }

                SendUDPData(packet);
            }
        }

        public static void PlayerScene(string sceneName)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerScene))
            {
                packet.Write(sceneName);

                SendTCPData(packet);
            }
        }

        public static void PlayerInstance(int instance)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerInstance))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void AddTNHInstance(int hostID, bool letPeopleJoin,
                                          int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                          int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                          int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, int levelIndex)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addTNHInstance))
            {
                packet.Write(hostID);
                packet.Write(letPeopleJoin);
                packet.Write(progressionTypeSetting);
                packet.Write(healthModeSetting);
                packet.Write(equipmentModeSetting);
                packet.Write(targetModeSetting);
                packet.Write(AIDifficultyModifier);
                packet.Write(radarModeModifier);
                packet.Write(itemSpawnerMode);
                packet.Write(backpackMode);
                packet.Write(healthMult);
                packet.Write(sosiggunShakeReloading);
                packet.Write(TNHSeed);
                packet.Write(levelIndex);

                SendTCPData(packet);
            }
        }

        public static void AddInstance()
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addInstance))
            {
                SendTCPData(packet);
            }
        }

        // MOD: This is what a mod that adds a scene it wants to sync would call to sync players and items inside it
        public static void AddSyncScene(string sceneName)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addSyncScene))
            {
                packet.Write(sceneName);

                SendTCPData(packet);
            }
        }

        public static void TrackedItems()
        {
            int index = 0;
            while (index < H3MP_GameManager.items.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedItems))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.items.Count; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_GameManager.items[i];
                        if (trackedItem.Update())
                        {
                            trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedItem);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedItem.insuranceCounter > 0)
                        {
                            --trackedItem.insuranceCounter;

                            packet.Write(trackedItem);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPData(packet);
                }
            }
        }

        public static void TrackedSosigs()
        {
            int index = 0;
            while (index < H3MP_GameManager.sosigs.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedSosigs))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.sosigs.Count; ++i)
                    {
                        H3MP_TrackedSosigData trackedSosig = H3MP_GameManager.sosigs[i];
                        if (trackedSosig.Update())
                        {
                            trackedSosig.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedSosig);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedSosig.insuranceCounter > 0)
                        {
                            --trackedSosig.insuranceCounter;

                            packet.Write(trackedSosig);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPData(packet);
                }
            }
        }

        public static void TrackedAutoMeaters()
        {
            int index = 0;
            while (index < H3MP_GameManager.autoMeaters.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedAutoMeaters))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.autoMeaters.Count; ++i)
                    {
                        H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_GameManager.autoMeaters[i];
                        if (trackedAutoMeater.Update())
                        {
                            trackedAutoMeater.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedAutoMeater);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedAutoMeater.insuranceCounter > 0)
                        {
                            --trackedAutoMeater.insuranceCounter;

                            packet.Write(trackedAutoMeater);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPData(packet);
                }
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, string scene)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);

                SendTCPData(packet);
            }
        }

        public static void TrackedSosig(H3MP_TrackedSosigData trackedSosig, string scene)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedSosig))
            {
                packet.Write(trackedSosig, true);
                packet.Write(scene);

                SendTCPData(packet);
            }
        }

        public static void TrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, string scene)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, true);
                packet.Write(scene);

                SendTCPData(packet);
            }
        }

        public static void GiveControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void GiveSosigControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveSosigControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void GiveAutoMeaterControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveAutoMeaterControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void DestroyItem(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyItem))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void DestroySosig(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroySosig))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void DestroyAutoMeater(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyAutoMeater))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void ItemParent(int trackedID, int newParentID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.itemParent))
            {
                packet.Write(trackedID);
                packet.Write(newParentID);

                SendTCPData(packet);
            }
        }

        public static void WeaponFire(int trackedID)
        {
            // TODO: It may be necessary to also pass the round class 
            //       This is because when a weapon that is controlled by this client in fired in aanother client, it will use up the projectile in its chamber (if it has one)
            //       The problem is that if it isn't up to date before it gets fired again, the chamber is now empty but we are still calling fire
            //       This will either cause error or just not fire the weapon. If we pass the round class, we can ensure that our chamber contains the correct
            //       class, if anything, before firing
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.weaponFire))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterFirearmFireShot(int trackedID, Vector3 angles)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterFireShot))
            {
                packet.Write(trackedID);
                packet.Write(angles);

                SendTCPData(packet);
            }
        }

        public static void PlayerDamage(int clientID, byte part, Damage damage)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerDamage))
            {
                packet.Write(clientID);
                packet.Write(part);
                packet.Write(damage);

                SendTCPData(packet);
            }
        }

        public static void SosigPickUpItem(H3MP_TrackedSosig trackedSosig, int itemTrackedID, bool primaryHand)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigPickupItem))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(itemTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigPlaceItemIn(int sosigTrackedID, int slotIndex, int itemTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigPlaceItemIn))
            {
                packet.Write(sosigTrackedID);
                packet.Write(itemTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigDropSlot(int sosigTrackedID, int slotIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDropSlot))
            {
                packet.Write(sosigTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigHandDrop(int sosigTrackedID, bool primaryHand)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigHandDrop))
            {
                packet.Write(sosigTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigConfigure(int sosigTrackedID, SosigConfigTemplate config)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigConfigure))
            {
                packet.Write(sosigTrackedID);
                packet.Write(config);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDeRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkDeRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetIFF(int trackedSosigID, int IFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigSetOriginalIFF(int trackedSosigID, int IFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetOriginalIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDamage(int trackedSosigID, int linkIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigWearableDamage(int trackedSosigID, int linkIndex, int wearableIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigWearableDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)wearableIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigDamageData(H3MP_TrackedSosig trackedSosig)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDamageData))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(trackedSosig.physicalSosigScript.IsStunned);
                packet.Write(trackedSosig.physicalSosigScript.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosigScript.BodyState);
                packet.Write((bool)Mod.Sosig_m_isOnOffMeshLinkField.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosigScript.Agent.enabled);
                List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(trackedSosig.physicalSosigScript);
                packet.Write((byte)joints.Count);
                for(int i=0; i < joints.Count; ++i)
                {
                    packet.Write(joints[i].lowTwistLimit.limit);
                    packet.Write(joints[i].highTwistLimit.limit);
                    packet.Write(joints[i].swing1Limit.limit);
                    packet.Write(joints[i].swing2Limit.limit);
                }
                packet.Write((bool)Mod.Sosig_m_isCountingDownToStagger.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_staggerAmountToApply.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((bool)Mod.Sosig_m_recoveringFromBallisticState.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticLerp.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_tickDownToWrithe.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticTick.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((byte)trackedSosig.physicalSosigScript.GetDiedFromIFF());
                packet.Write((byte)trackedSosig.physicalSosigScript.GetDiedFromClass());
                packet.Write(trackedSosig.physicalSosigScript.IsBlinded);
                packet.Write((float)Mod.Sosig_m_blindTime.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.IsFrozen);
                packet.Write((float)Mod.Sosig_m_debuffTime_Freeze.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.GetDiedFromHeadShot());
                packet.Write((float)Mod.Sosig_m_timeSinceLastDamage.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.IsConfused);
                packet.Write(trackedSosig.physicalSosigScript.m_confusedTime);
                packet.Write((float)Mod.Sosig_m_storedShudder.GetValue(trackedSosig.physicalSosigScript));

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamageData(int trackedID, AutoMeaterHitZone hitZone)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterHitZoneDamageData))
            {
                packet.Write(trackedID);
                packet.Write((byte)hitZone.Type);

                packet.Write(hitZone.ArmorThreshold);
                packet.Write(hitZone.LifeUntilFailure);
                packet.Write((bool)Mod.AutoMeaterHitZone_m_isDestroyed.GetValue(hitZone));

                SendTCPData(packet);
            }
        }

        public static void SosigLinkExplodes(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkExplodes))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);

                SendTCPData(packet);
            }
        }

        public static void SosigDies(int sosigTrackedID, Damage.DamageClass damClass, Sosig.SosigDeathType deathType)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDies))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)damClass);
                packet.Write((byte)deathType);

                SendTCPData(packet);
            }
        }

        public static void SosigClear(int sosigTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigClear))
            {
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetBodyState(int sosigTrackedID, Sosig.SosigBodyState s)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetBodyState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)s);

                SendTCPData(packet);
            }
        }

        public static void PlaySosigFootStepSound(int sosigTrackedID, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playSosigFootStepSound))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)audioType);
                packet.Write(position);
                packet.Write(vol);
                packet.Write(pitch);
                packet.Write(delay);

                SendTCPData(packet);
            }
        }

        public static void SosigSpeakState(int sosigTrackedID, Sosig.SosigOrder currentOrder)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSpeakState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPData(packet);
            }
        }

        public static void SosigSetCurrentOrder(int sosigTrackedID, Sosig.SosigOrder currentOrder)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetCurrentOrder))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPData(packet);
            }
        }

        public static void SosigVaporize(int sosigTrackedID, int iff)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigVaporize))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)iff);

                SendTCPData(packet);
            }
        }

        public static void SosigRequestHitDecal(int sosigTrackedID, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, int linkIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigRequestHitDecal))
            {
                packet.Write(sosigTrackedID);
                packet.Write(point);
                packet.Write(normal);
                packet.Write(edgeNormal);
                packet.Write(scale);
                packet.Write((byte)linkIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkBreak(int sosigTrackedID, int linkIndex, bool isStart, Damage.DamageClass damClass)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkBreak))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write(isStart);
                packet.Write((byte)damClass);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkSever(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass, bool isPullApart)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkSever))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);
                packet.Write(isPullApart);

                SendTCPData(packet);
            }
        }

        public static void UpToDateObjects()
        {
            Debug.Log(H3MP_Client.singleton.ID.ToString()+ " sending up to date objects to server");
            int index = 0;
            while (index < H3MP_GameManager.items.Count)
            {
                Debug.Log("\tItem Packet");
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateItemRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.items.Count; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_GameManager.items[i];
                        trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                        Debug.Log("\t\tTracked item at: "+trackedItem.trackedID);
                        trackedItem.Update(true);
                        packet.Write(trackedItem, true);

                        ++count;

                        // Limit buffer size to MTU, will send next set of tracked items in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
            index = 0;
            while (index < H3MP_GameManager.sosigs.Count)
            {
                Debug.Log("\tSosig Packet");
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateSosigRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.sosigs.Count; ++i)
                    {
                        H3MP_TrackedSosigData trackedSosig = H3MP_GameManager.sosigs[i];
                        trackedSosig.insuranceCounter = H3MP_TrackedSosigData.insuranceCount;

                        Debug.Log("\t\tTracked sosig at: " + trackedSosig.trackedID);
                        trackedSosig.Update(true);
                        packet.Write(trackedSosig, true);

                        ++count;

                        // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
            index = 0;
            while (index < H3MP_GameManager.autoMeaters.Count)
            {
                Debug.Log("\tAutoMeater Packet");
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateAutoMeatersRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.autoMeaters.Count; ++i)
                    {
                        H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_GameManager.autoMeaters[i];
                        trackedAutoMeater.insuranceCounter = H3MP_TrackedAutoMeaterData.insuranceCount;

                        Debug.Log("\t\tTracked AutoMeater at: " + trackedAutoMeater.trackedID);
                        trackedAutoMeater.Update(true);
                        packet.Write(trackedAutoMeater, true);

                        ++count;

                        // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }

                        ++index;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
        }

        public static void AddTNHCurrentlyPlaying(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.removeTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHProgression(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHProgression))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHEquipment(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHEquipment))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHHealthMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHTargetMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHTargetMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHAIDifficulty(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHAIDifficulty))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHRadarMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHRadarMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHItemSpawnerMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHItemSpawnerMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHBackpackMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHBackpackMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMult(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHHealthMult))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSosigGunReload(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHSosigGunReload))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSeed(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHSeed))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHLevelIndex(int levelIndex, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHLevelIndex))
            {
                packet.Write(levelIndex);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHController(int instance, int ID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHController))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHData(int controller, TNH_Manager manager)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHData))
            {
                packet.Write(controller);
                packet.Write(manager);

                SendTCPData(packet);
            }
        }

        public static void TNHPlayerDied(int instance, int ID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHPlayerDied))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHAddTokens(int instance, int amount)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHAddTokens))
            {
                packet.Write(instance);
                packet.Write(amount);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetState(int trackedID, byte state)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterSetState))
            {
                packet.Write(trackedID);
                packet.Write(state);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetBladesActive(int trackedID, bool active)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterSetBladesActive))
            {
                packet.Write(trackedID);
                packet.Write(active);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterFirearmFireAtWill(int trackedID, int firearmIndex, bool fireAtWill, float dist)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterFirearmFireAtWill))
            {
                packet.Write(trackedID);
                packet.Write(firearmIndex);
                packet.Write(fireAtWill);
                packet.Write(dist);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(int trackedID, AutoMeater.AMHitZoneType type, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterHitZoneDamage))
            {
                packet.Write(trackedID);
                packet.Write((byte)type);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void TNHSosigKill(int instance, int sosigTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSosigKill))
            {
                packet.Write(instance);
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointSystemNode(int instance, int charIndex, int progressionIndex, int progressionEndlessIndex, int levelIndex, int holdPointIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointSystemNode))
            {
                packet.Write(instance);
                packet.Write(charIndex);
                packet.Write(progressionIndex);
                packet.Write(progressionEndlessIndex);
                packet.Write(levelIndex);
                packet.Write(holdPointIndex);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldBeginChallenge(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldBeginChallenge))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }
    }
}
