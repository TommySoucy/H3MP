using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
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
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItems))
            {
                packet.Write(H3MP_GameManager.items.Count);
                foreach (KeyValuePair<int, H3MP_TrackedItemData> trackedItem in H3MP_GameManager.items)
                {
                    if (trackedItem.Value.controller == 0)
                    {
                        trackedItem.Value.position = trackedItem.Value.physicalObject.transform.position;
                        trackedItem.Value.rotation = trackedItem.Value.physicalObject.transform.rotation;
                    }
                    packet.Write(trackedItem.Value);
                }

                SendUDPDataToAll(packet);
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem);

                SendTCPDataToAll(packet);
            }
        }

        public static void TakeControl(H3MP_TrackedItemData trackedItem)
        {
            trackedItem.controller = 0;
            H3MP_GameManager.items.Add(trackedItem.trackedID, trackedItem);

            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.takeControl))
            {
                packet.Write(trackedItem.trackedID);

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
