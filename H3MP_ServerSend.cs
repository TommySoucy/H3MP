using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        public static void TrackedItemSpecific(H3MP_TrackedItemData trackedItem, string scene, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
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
    }
}
