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

        public static void PlayerState(H3MP_Player player)
        {
            PlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                        player.leftHandPos, player.leftHandRot, player.leftHandTrackedID,
                        player.leftHandPos, player.leftHandRot, player.leftHandTrackedID);

            // Also update for host
            H3MP_GameManager.players[player.ID].UpdateState(player);
        }

        public static void PlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot, int leftHandTrackedID,
                                       Vector3 rightHandPos, Quaternion rightHandRot, int rightHandTrackedID)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerState))
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
                packet.Write(leftHandTrackedID);
                packet.Write(rightHandPos);
                packet.Write(rightHandRot);
                packet.Write(rightHandTrackedID);

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
            while (index < H3MP_Server.items.Length - 1) // TODO: Optimize, keep track of highest used index in global items list so we can stop there right away
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItems))
                {
                    // Write place holder int at start to hold the count once we know it
                    packet.Write(0);

                    int count = 0;
                    for (int i = index; i < H3MP_Server.items.Length; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_Server.items[i];
                        if (trackedItem != null)
                        {
                            if(trackedItem.controller == 0)
                            {
                                if (trackedItem.Update())
                                {
                                    packet.Write(trackedItem);

                                    index = i;
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
                                packet.Write(trackedItem);

                                index = i;
                                ++count;

                                // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                if (packet.buffer.Count >= 1300)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = 0; i < 4; ++i)
                    {
                        packet.buffer[i] = countArr[i];
                    }

                    SendUDPDataToAll(packet);
                }
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, string scene)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);

                SendTCPDataToAll(packet);
            }
        }

        public static void GiveControl(int trackedID, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);

                SendTCPDataToAll(clientID, packet);
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
    }
}
