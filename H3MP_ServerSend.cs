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

        public static void SpawnPlayer(int clientID, H3MP_Player player, string scene, int instance)
        {
            SpawnPlayer(clientID, player.ID, player.username, scene, instance, player.position, player.rotation);
        }

        public static void SpawnPlayer(int clientID, int ID, string username, string scene, int instance, Vector3 position, Quaternion rotation)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(ID);
                packet.Write(username);
                packet.Write(scene);
                packet.Write(instance);
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

        public static void PlayerScene(int clientID, string sceneName)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerScene))
            {
                packet.Write(clientID);
                packet.Write(sceneName);

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
                                else if (trackedSosig.insuranceCounter > 0)
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
                            else if (trackedSosig.NeedsUpdate())
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
                            else if (trackedSosig.insuranceCounter > 0)
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

        public static void TrackedAutoMeaters()
        {
            int index = 0;
            while (index < H3MP_Server.autoMeaters.Length - 1)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeaters))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_Server.autoMeaters.Length; ++i)
                    {
                        H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[i];
                        if (trackedAutoMeater != null)
                        {
                            if (trackedAutoMeater.controller == 0)
                            {
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
                                else if (trackedAutoMeater.insuranceCounter > 0)
                                {
                                    --trackedAutoMeater.insuranceCounter;

                                    packet.Write(trackedAutoMeater);

                                    ++count;

                                    // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                    if (packet.buffer.Count >= 1300)
                                    {
                                        break;
                                    }
                                }
                            }
                            else if (trackedAutoMeater.NeedsUpdate())
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
                            else if (trackedAutoMeater.insuranceCounter > 0)
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

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, string scene, int instance, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);
                packet.Write(instance);

                // We want to send to all, even the one who requested for the item to be tracked because we need to tell them its tracked ID
                SendTCPDataToAll(packet);
            }
        }

        public static void TrackedSosig(H3MP_TrackedSosigData trackedSosig, string scene, int instance, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, true);
                packet.Write(scene);
                packet.Write(instance);

                // We want to send to all, even the one who requested for the sosig to be tracked because we need to tell them its tracked ID
                SendTCPDataToAll(packet);
            }
        }

        public static void TrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, string scene, int instance, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, true);
                packet.Write(scene);
                packet.Write(instance);

                // We want to send to all, even the one who requested for the sosig to be tracked because we need to tell them its tracked ID
                SendTCPDataToAll(packet);
            }
        }

        public static void TrackedItemSpecific(H3MP_TrackedItemData trackedItem, string scene, int instance, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);
                packet.Write(instance);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedSosigSpecific(H3MP_TrackedSosigData trackedSosig, string scene, int instance, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, true);
                packet.Write(scene);
                packet.Write(instance);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedAutoMeaterSpecific(H3MP_TrackedAutoMeaterData trackedAutoMeater, string scene, int instance, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, true);
                packet.Write(scene);
                packet.Write(instance);

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

        public static void GiveAutoMeaterControl(int trackedID, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveAutoMeaterControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);

                SendTCPDataToAll(packet);
            }
        }

        public static void DestroyItem(int trackedID, bool removeFromList = true, int clientID = -1)
        {
            Debug.Log("Server sending a DestroyItem packet for : " + trackedID+"\n"+Environment.StackTrace);
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroyItem))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

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

        public static void DestroySosig(int trackedID, bool removeFromList = true, int clientID = -1)
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

        public static void DestroyAutoMeater(int trackedID, bool removeFromList = true, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroyAutoMeater))
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

        public static void AutoMeaterFirearmFireShot(int clientID, int trackedID, Vector3 angles)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterFireShot))
            {
                packet.Write(trackedID);
                packet.Write(angles);

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

        public static void AutoMeaterDamage(H3MP_TrackedAutoMeaterData trackedAutoMeater, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterDamage))
            {
                packet.Write(trackedAutoMeater.trackedID);
                packet.Write(d);

                SendTCPData(trackedAutoMeater.controller, packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(H3MP_TrackedAutoMeaterData trackedAutoMeater, byte type, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterHitZoneDamage))
            {
                packet.Write(trackedAutoMeater.trackedID);
                packet.Write(type);
                packet.Write(d);

                SendTCPData(trackedAutoMeater.controller, packet);
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
                packet.Write(trackedSosig.physicalSosigScript.IsStunned);
                packet.Write(trackedSosig.physicalSosigScript.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosigScript.BodyState);
                packet.Write((bool)Mod.Sosig_m_isOnOffMeshLinkField.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosigScript.Agent.enabled);
                List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(trackedSosig.physicalSosigScript);
                packet.Write((byte)joints.Count);
                for (int i = 0; i < joints.Count; ++i)
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

        public static void AutoMeaterHitZoneDamageData(int trackedID, AutoMeaterHitZone hitZone)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterHitZoneDamageData))
            {
                packet.Write(trackedID);
                packet.Write((byte)hitZone.Type);

                packet.Write(hitZone.ArmorThreshold);
                packet.Write(hitZone.LifeUntilFailure);
                packet.Write((bool)Mod.AutoMeaterHitZone_m_isDestroyed.GetValue(hitZone));

                AutoMeaterHitZoneDamageData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamageData(H3MP_Packet packet)
        {
            // Make sure the packet is set to ServerPackets.autoMeaterHitZoneDamageData
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.autoMeaterHitZoneDamageData);
            for (int i = 0; i < 4; ++i)
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

        public static void RequestUpToDateObjects(int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.updateRequest))
            {
                SendTCPData(clientID, packet);
            }
        }

        public static void PlayerInstance(int clientID, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerInstance))
            {
                packet.Write(clientID);
                packet.Write(instance);


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

        public static void AddTNHInstance(H3MP_TNHInstance instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.addTNHInstance))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void AddInstance(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.addInstance))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void AddTNHCurrentlyPlaying(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.addTNHCurrentlyPlaying))
            {
                packet.Write(clientID);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.removeTNHCurrentlyPlaying))
            {
                packet.Write(clientID);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHProgression(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHProgression))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHEquipment(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHEquipment))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHHealthMode(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHHealthMode))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHTargetMode(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHTargetMode))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHAIDifficulty(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHAIDifficulty))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHRadarMode(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHRadarMode))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHItemSpawnerMode(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHItemSpawnerMode))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHBackpackMode(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHBackpackMode))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHHealthMult(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHHealthMult))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHSosigGunReload(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHSosigGunReload))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHSeed(int i, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHSeed))
            {
                packet.Write(i);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHLevelIndex(int levelIndex, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHLevelIndex))
            {
                packet.Write(levelIndex);
                packet.Write(instance);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void SetTNHController(int instance, int newController, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHController))
            {
                packet.Write(instance);
                packet.Write(newController);

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

        public static void TNHData(int controller, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.TNHData);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(controller, packet);
        }

        public static void TNHData(int controller, TNH_Manager manager)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHData))
            {
                packet.Write(controller);
                packet.Write(manager);

                SendTCPData(controller, packet);
            }
        }

        public static void TNHPlayerDied(int instance, int ID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHPlayerDied))
            {
                packet.Write(instance);
                packet.Write(ID);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void TNHAddTokens(int instance, int ID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHAddTokens))
            {
                packet.Write(instance);
                packet.Write(ID);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void AutoMeaterSetState(int trackedID, byte state, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterSetState))
            {
                packet.Write(trackedID);
                packet.Write(state);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void AutoMeaterSetBladesActive(int trackedID, bool active, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterSetBladesActive))
            {
                packet.Write(trackedID);
                packet.Write(active);

                if(clientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(clientID, packet);
                }
            }
        }

        public static void AutoMeaterFirearmFireAtWill(int trackedID, int firearmIndex, bool fireAtWill, float dist, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.autoMeaterFirearmFireAtWill))
            {
                packet.Write(trackedID);
                packet.Write(firearmIndex);
                packet.Write(fireAtWill);
                packet.Write(dist);

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

        public static void TNHSosigKill(int instance, int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSosigKill))
            {
                packet.Write(instance);
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

        public static void TNHHoldPointSystemNode(int instance, int charIndex, int progressionIndex, int progressionEndlessIndex, int levelIndex, int holdPointIndex, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointSystemNode))
            {
                packet.Write(instance);
                packet.Write(charIndex);
                packet.Write(progressionIndex);
                packet.Write(progressionEndlessIndex);
                packet.Write(levelIndex);
                packet.Write(holdPointIndex);

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

        public static void TNHHoldBeginChallenge(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldBeginChallenge))
            {
                packet.Write(instance);

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

        public static void ShatterableCrateDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldBeginChallenge))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPDataToAll(H3MP_Server.items[trackedID].controller, packet);
            }
        }
    }
}
