using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;

namespace H3MP
{
    internal class H3MP_ServerSend
    {
        private static void SendTCPData(int toClient, H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Server.clients[toClient].tcp.SendData(packet);
        }

        private static void SendUDPData(int toClient, H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Server.clients[toClient].udp.SendData(packet);
        }

        private static void SendTCPDataToAll(H3MP_Packet packet)
        {
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                H3MP_Server.clients[i].tcp.SendData(packet);
            }
        }

        private static void SendUDPDataToAll(H3MP_Packet packet)
        {
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                H3MP_Server.clients[i].udp.SendData(packet);
            }
        }

        private static void SendTCPDataToAll(int exceptClient, H3MP_Packet packet)
        {
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                if (i != exceptClient)
                {
                    H3MP_Server.clients[i].tcp.SendData(packet);
                }
            }
        }

        private static void SendUDPDataToAll(int exceptClient, H3MP_Packet packet)
        {
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                if (i != exceptClient)
                {
                    H3MP_Server.clients[i].udp.SendData(packet);
                }
            }
        }

        public static void Welcome(int toClient, string msg)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.welcome))
            {
                packet.Write(msg);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }

        public static void SpawnPlayer(int clientID, H3MP_Player player, string scene)
        {
            SpawnPlayer(clientID, player.ID, player.username, scene, player.position, player.rotation);
        }

        public static void SpawnPlayer(int clientID, int ID, string username, string scene, Vector3 position, Quaternion rotation)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(ID);
                packet.Write(username);
                packet.Write(scene);
                packet.Write(position);
                packet.Write(rotation);

                SendTCPData(clientID, packet);
            }
        }

        public static void ConnectSync(int clientID, bool inControl)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.connectSync))
            {
                packet.Write(inControl);

                SendTCPData(clientID, packet);
            }
        }

        public static void PlayerState(H3MP_Player player)
        {
            PlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                        player.leftHandPos, player.leftHandRot,
                        player.leftHandPos, player.leftHandRot,
                        player.health, player.maxHealth);
        }

        public static void PlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot,
                                       Vector3 rightHandPos, Quaternion rightHandRot,
                                       float health, int maxHealth)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerState))
            {
                packet.Write(ID);
                packet.Write(position);
                packet.Write(rotation);
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
                if (additionalData != null && additionalData.Length > 0)
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

                SendUDPDataToAll(ID, packet);
            }
        }

        public static void PlayerScene(int ID, string sceneName)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerScene))
            {
                packet.Write(ID);
                packet.Write(sceneName);

                SendTCPDataToAll(ID, packet);
            }
        }

        public static void AddSyncScene(int ID, string sceneName)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.addSyncScene))
            {
                packet.Write(ID);
                packet.Write(sceneName);

                SendTCPDataToAll(ID, packet);
            }
        }

        public static void TrackedItems()
        {
            int index = 0;
            while (index < H3MP_Server.items.Length - 1) // TODO: To optimize, we should also keep track of all item IDs taht are in use so we can iterate only them and do the samei n client send
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItems))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_Server.items.Length; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_Server.items[i];
                        if (trackedItem != null)
                        {
                            if (trackedItem.controller == 0)
                            {
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
                            }
                            else if(trackedItem.NeedsUpdate())
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
                        }

                        index = i;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPDataToAll(packet);
                }
            }
        }

        public static void TrackedSosigs()
        {
            int index = 0;
            while (index < H3MP_Server.sosigs.Length - 1)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosigs))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_Server.sosigs.Length; ++i)
                    {
                        H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[i];
                        if (trackedSosig != null)
                        {
                            if (trackedSosig.controller == 0)
                            {
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

                                    // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                    if (packet.buffer.Count >= 1300)
                                    {
                                        break;
                                    }
                                }
                            }
                            else if(trackedSosig.NeedsUpdate())
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
                        }

                        index = i;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPDataToAll(packet);
                }
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, string scene, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);

                // We want to send to all, even the one who requested for the item to be tracked because we need to tell them its tracked ID
                SendTCPDataToAll(packet);
            }
        }

        public static void TrackedSosig(H3MP_TrackedSosigData trackedSosig, string scene, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, true);
                packet.Write(scene);

                // We want to send to all, even the one who requested for the sosig to be tracked because we need to tell them its tracked ID
                SendTCPDataToAll(packet);
            }
        }

        public static void TrackedItemSpecific(H3MP_TrackedItemData trackedItem, string scene, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedSosigSpecific(H3MP_TrackedSosigData trackedSosig, string scene, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, true);
                packet.Write(scene);

                SendTCPData(toClientID, packet);
            }
        }

        public static void GiveControl(int trackedID, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);

                SendTCPDataToAll(packet);
            }
        }

        public static void GiveSosigControl(int trackedID, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveSosigControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);

                SendTCPDataToAll(packet);
            }
        }

        public static void DestroyItem(int trackedID, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroyItem))
            {
                packet.Write(trackedID);

                if(clientID == -1)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void DestroySosig(int trackedID, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroySosig))
            {
                packet.Write(trackedID);

                if(clientID == -1)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void ItemParent(int trackedID, int newParentID, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.itemParent))
            {
                packet.Write(trackedID);
                packet.Write(newParentID);

                if (clientID == -1)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void WeaponFire(int clientID, int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.weaponFire))
            {
                packet.Write(trackedID);

                if (clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void PlayerDamage(int clientID, byte part, Damage damage)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerDamage))
            {
                packet.Write(part);
                packet.Write(damage);

                SendTCPData(clientID, packet);
            }
        }

        public static void SosigPickUpItem(int trackedSosigID, int itemTrackedID, bool primaryHand, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigPickUpItem))
            {
                packet.Write(trackedSosigID);
                packet.Write(itemTrackedID);
                packet.Write(primaryHand);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigPlaceItemIn(int trackedSosigID, int slotIndex, int itemTrackedID, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigPlaceItemIn))
            {
                packet.Write(trackedSosigID);
                packet.Write(itemTrackedID);
                packet.Write(slotIndex);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigDropSlot(int trackedSosigID, int slotIndex, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigDropSlot))
            {
                packet.Write(trackedSosigID);
                packet.Write(slotIndex);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigHandDrop(int trackedSosigID, bool primaryHand, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigHandDrop))
            {
                packet.Write(trackedSosigID);
                packet.Write(primaryHand);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigConfigure(int trackedSosigID, SosigConfigTemplate config, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigConfigure))
            {
                packet.Write(trackedSosigID);
                packet.Write(config);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigLinkRegisterWearable(int trackedSosigID, int linkIndex, string itemID, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigLinkDeRegisterWearable(int trackedSosigID, int linkIndex, string itemID, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkDeRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigSetIFF(int trackedSosigID, int IFF, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSetIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigSetOriginalIFF(int trackedSosigID, int IFF, int fromclientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSetOriginalIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                if (fromclientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(fromclientID, packet);
                }
            }
        }

        public static void SosigLinkDamage(H3MP_TrackedSosigData trackedSosig, int linkIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkDamage))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)linkIndex);
                packet.Write(d);

                SendTCPData(trackedSosig.controller, packet);
            }
        }

        public static void SosigWearableDamage(H3MP_TrackedSosigData trackedSosig, int linkIndex, int wearableIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigWearableDamage))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)wearableIndex);
                packet.Write(d);

                SendTCPData(trackedSosig.controller, packet);
            }
        }

        public static void SosigDamageData(H3MP_TrackedSosig trackedSosig)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigDamageData))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(trackedSosig.physicalSosig.IsStunned);
                packet.Write(trackedSosig.physicalSosig.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosig.BodyState);
                packet.Write((bool)Mod.Sosig_m_isOnOffMeshLinkField.GetValue(trackedSosig.physicalSosig));
                packet.Write(trackedSosig.physicalSosig.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosig.Agent.enabled);
                List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(trackedSosig.physicalSosig);
                packet.Write((byte)joints.Count);
                for (int i = 0; i < joints.Count; ++i)
                {
                    packet.Write(joints[i].lowTwistLimit.limit);
                    packet.Write(joints[i].highTwistLimit.limit);
                    packet.Write(joints[i].swing1Limit.limit);
                    packet.Write(joints[i].swing2Limit.limit);
                }
                packet.Write((bool)Mod.Sosig_m_isCountingDownToStagger.GetValue(trackedSosig.physicalSosig));
                packet.Write((float)Mod.Sosig_m_staggerAmountToApply.GetValue(trackedSosig.physicalSosig));
                packet.Write((bool)Mod.Sosig_m_recoveringFromBallisticState.GetValue(trackedSosig.physicalSosig));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticLerp.GetValue(trackedSosig.physicalSosig));
                packet.Write((float)Mod.Sosig_m_tickDownToWrithe.GetValue(trackedSosig.physicalSosig));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticTick.GetValue(trackedSosig.physicalSosig));
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromIFF());
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromClass());
                packet.Write(trackedSosig.physicalSosig.IsBlinded);
                packet.Write((float)Mod.Sosig_m_blindTime.GetValue(trackedSosig.physicalSosig));
                packet.Write(trackedSosig.physicalSosig.IsFrozen);
                packet.Write((float)Mod.Sosig_m_debuffTime_Freeze.GetValue(trackedSosig.physicalSosig));
                packet.Write(trackedSosig.physicalSosig.GetDiedFromHeadShot());
                packet.Write((float)Mod.Sosig_m_timeSinceLastDamage.GetValue(trackedSosig.physicalSosig));
                packet.Write(trackedSosig.physicalSosig.IsConfused);
                packet.Write(trackedSosig.physicalSosig.m_confusedTime);
                packet.Write((float)Mod.Sosig_m_storedShudder.GetValue(trackedSosig.physicalSosig));

                SosigLinkDamageData(packet);
            }
        }

        public static void SosigLinkDamageData(H3MP_Packet packet)
        {
            // Make sure the packet is set to ServerPackets.sosigLinkDamageData
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.sosigDamageData);
            for(int i=0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(packet);
        }

        public static void SosigLinkExplodes(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkExplodes))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);

                SosigLinkExplodes(packet, fromClientID);
            }
        }

        public static void SosigLinkExplodes(H3MP_Packet packet, int fromClientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.sosigLinkExplodes);
            for(int i=0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(fromClientID, packet);
        }

        public static void SosigDies(int sosigTrackedID, Damage.DamageClass damClass, Sosig.SosigDeathType deathType, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigDies))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)damClass);
                packet.Write((byte)deathType);

                SosigDies(packet, fromClientID);
            }
        }

        public static void SosigDies(H3MP_Packet packet, int fromClientID)
        {
            // Make sure the packet is set to ServerPackets.sosigDies
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.sosigDies);
            for(int i=0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(fromClientID, packet);
        }

        public static void PlaySosigFootStepSound(int sosigTrackedID, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playSosigFootStepSound))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)audioType);
                packet.Write(position);
                packet.Write(vol);
                packet.Write(pitch);
                packet.Write(delay);

                PlaySosigFootStepSound(packet, fromClientID);
            }
        }

        public static void PlaySosigFootStepSound(H3MP_Packet packet, int fromClientID = 0)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.playSosigFootStepSound);
            for(int i=0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(fromClientID, packet);
        }

        public static void SosigRequestHitDecal(int sosigTrackedID, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, int linkIndex, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigRequestHitDecal))
            {
                packet.Write(sosigTrackedID);
                packet.Write(point);
                packet.Write(normal);
                packet.Write(edgeNormal);
                packet.Write(scale);
                packet.Write(linkIndex);

                SosigRequestHitDecal(packet, fromClientID);
            }
        }

        public static void SosigRequestHitDecal(H3MP_Packet packet, int fromClientID = 0)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.sosigRequestHitDecal);
            for(int i=0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(fromClientID, packet);
        }

        public static void SosigClear(int sosigTrackedID, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigClear))
            {
                packet.Write(sosigTrackedID);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSetBodyState(int sosigTrackedID, Sosig.SosigBodyState s, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSetBodyState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)s);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSpeakState(int sosigTrackedID, Sosig.SosigOrder currentOrder, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSpeakState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSetCurrentOrder(int sosigTrackedID, Sosig.SosigOrder currentOrder, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSetCurrentOrder))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigVaporize(int sosigTrackedID, int iff, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigVaporize))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)iff);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigLinkBreak(int sosigTrackedID, int linkIndex, bool isStart, byte damClass, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkBreak))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write(isStart);
                packet.Write(damClass);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigLinkSever(int sosigTrackedID, int linkIndex, byte damClass, bool isPullApart, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigLinkSever))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write(damClass);
                packet.Write(isPullApart);

                SendTCPDataToAll(fromClientID, packet);
            }
        }
    }
}
