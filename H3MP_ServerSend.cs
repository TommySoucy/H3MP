using FistVR;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_ServerSend
    {
        private static void SendTCPData(int toClient, H3MP_Packet packet)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPData: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            H3MP_Server.clients[toClient].tcp.SendData(packet);
        }

        private static void SendTCPData(List<int> toClients, H3MP_Packet packet, int exclude = -1)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPData multiple: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for (int i = 0; i < toClients.Count; ++i)
            {
                if (exclude == -1 || toClients[i] != exclude)
                {
                    H3MP_Server.clients[toClients[i]].tcp.SendData(packet);
                }
            }
        }

        private static void SendUDPData(List<int> toClients, H3MP_Packet packet, int exclude = -1)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPData multiple: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for (int i = 0; i < toClients.Count; ++i)
            {
                if (exclude == -1 || toClients[i] != exclude)
                {
                    H3MP_Server.clients[toClients[i]].udp.SendData(packet);
                }
            }
        }

        private static void SendTCPDataToAll(H3MP_Packet packet)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                H3MP_Server.clients[i].tcp.SendData(packet);
            }
        }

        private static void SendUDPDataToAll(H3MP_Packet packet)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                H3MP_Server.clients[i].udp.SendData(packet);
            }
        }

        private static void SendUDPDataToClients(H3MP_Packet packet, List<int> clientIDs)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPDataToClients: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            foreach(int clientID in clientIDs)
            {
                H3MP_Server.clients[clientID].udp.SendData(packet);
            }
        }

        private static void SendTCPDataToAll(int exceptClient, H3MP_Packet packet)
        {
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
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
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for(int i = 1; i<= H3MP_Server.maxClientCount; ++i)
            {
                if (i != exceptClient)
                {
                    H3MP_Server.clients[i].udp.SendData(packet);
                }
            }
        }

        public static void Welcome(int toClient, string msg, bool colorByIFF, int nameplateMode, int radarMode, bool radarColor, Dictionary<string, Dictionary<int, KeyValuePair<float, int>>> maxHealthEntries)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.welcome))
            {
                packet.Write(msg);
                packet.Write(toClient);
                packet.Write(colorByIFF);
                packet.Write(nameplateMode);
                packet.Write(radarMode);
                packet.Write(radarColor);
                packet.Write(maxHealthEntries.Count);
                foreach(KeyValuePair<string, Dictionary<int, KeyValuePair<float, int>>> sceneEntry in maxHealthEntries)
                {
                    packet.Write(sceneEntry.Key);
                    packet.Write(sceneEntry.Value.Count);
                    foreach(KeyValuePair<int, KeyValuePair<float, int>> instanceEntry in sceneEntry.Value)
                    {
                        packet.Write(instanceEntry.Key);
                        packet.Write(instanceEntry.Value.Key);
                        packet.Write(instanceEntry.Value.Value);
                    }
                }

                SendTCPData(toClient, packet);
            }
        }

        public static void Ping(int toClient, long time)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.ping))
            {
                packet.Write(time);
                SendTCPData(toClient, packet);
            }
        }

        public static void SpawnPlayer(int clientID, H3MP_Player player, string scene, int instance, int IFF, int colorIndex, bool join = false)
        {
            SpawnPlayer(clientID, player.ID, player.username, scene, instance, player.position, player.rotation, IFF, colorIndex, join);
        }

        public static void SpawnPlayer(int clientID, int ID, string username, string scene, int instance, Vector3 position, Quaternion rotation, int IFF, int colorIndex, bool join = false)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(ID);
                packet.Write(username);
                packet.Write(scene);
                packet.Write(instance);
                packet.Write(position);
                packet.Write(rotation);
                packet.Write(IFF);
                packet.Write(colorIndex);
                packet.Write(join);

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

        public static void PlayerState(H3MP_Player player, string scene, int instance)
        {
            PlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                        player.leftHandPos, player.leftHandRot,
                        player.leftHandPos, player.leftHandRot,
                        player.health, player.maxHealth, scene, instance);
        }

        public static void PlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot,
                                       Vector3 rightHandPos, Quaternion rightHandRot,
                                       float health, int maxHealth, string scene, int instance)
        {
            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(instance, out List<int> otherPlayers) &&
                otherPlayers.Count > 0 && (otherPlayers.Count > 1 || otherPlayers[0] != ID))
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

                    SendUDPData(otherPlayers, packet, ID);
                }
            }
        }

        public static void PlayerIFF(int clientID, int IFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerIFF))
            {
                packet.Write(clientID);
                packet.Write(IFF);

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

        public static void AddNonSyncScene(int ID, string sceneName)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.addNonSyncScene))
            {
                packet.Write(ID);
                packet.Write(sceneName);

                SendTCPDataToAll(ID, packet);
            }
        }

        public static void TrackedItems()
        {
            foreach(KeyValuePair<string, Dictionary<int, List<int>>> outer in H3MP_GameManager.itemsByInstanceByScene)
            {
                foreach(KeyValuePair<int, List<int>> inner in outer.Value)
                {
                    if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(outer.Key, out Dictionary<int, List<int>> playerInstances) &&
                        playerInstances.TryGetValue(inner.Key, out List<int> players) && players.Count > 0)
                    {
                        int index = 0;
                        while (index < inner.Value.Count)
                        {
                            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItems))
                            {
                                // Write place holder int at start to hold the count once we know it
                                int countPos = packet.buffer.Count;
                                packet.Write((short)0);

                                short count = 0;
                                for (int i = index; i < inner.Value.Count; ++i, ++index)
                                {
                                    H3MP_TrackedItemData trackedItem = H3MP_Server.items[inner.Value[i]];
                                    if (trackedItem != null)
                                    {
                                        if (trackedItem.controller == 0)
                                        {
                                            if (trackedItem.Update())
                                            {
                                                trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                                                packet.Write(trackedItem, true, false);

                                                ++count;

                                                // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                                if (packet.buffer.Count >= 1300)
                                                {
                                                    break;
                                                }
                                            }
                                            else if (trackedItem.insuranceCounter > 0)
                                            {
                                                --trackedItem.insuranceCounter;

                                                packet.Write(trackedItem, true, false);

                                                ++count;

                                                // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                                if (packet.buffer.Count >= 1300)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        else if (trackedItem.NeedsUpdate())
                                        {
                                            trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                                            packet.Write(trackedItem, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                        else if (trackedItem.insuranceCounter > 0)
                                        {
                                            --trackedItem.insuranceCounter;

                                            packet.Write(trackedItem, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (count == 0)
                                {
                                    break;
                                }

                                // Write the count to packet
                                byte[] countArr = BitConverter.GetBytes(count);
                                for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                                {
                                    packet.buffer[i] = countArr[j];
                                }

                                SendUDPDataToClients(packet, players);
                            }
                        }
                    }
                }
            }
        }

        public static void TrackedSosigs()
        {
            foreach (KeyValuePair<string, Dictionary<int, List<int>>> outer in H3MP_GameManager.sosigsByInstanceByScene)
            {
                foreach (KeyValuePair<int, List<int>> inner in outer.Value)
                {
                    if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(outer.Key, out Dictionary<int, List<int>> playerInstances) &&
                        playerInstances.TryGetValue(inner.Key, out List<int> players) && players.Count > 0)
                    {
                        int index = 0;
                        while (index < inner.Value.Count)
                        {
                            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosigs))
                            {
                                // Write place holder int at start to hold the count once we know it
                                int countPos = packet.buffer.Count;
                                packet.Write((short)0);

                                short count = 0;
                                for (int i = index; i < inner.Value.Count; ++i, ++index)
                                {
                                    H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[inner.Value[i]];
                                    if (trackedSosig != null)
                                    {
                                        if (trackedSosig.controller == 0)
                                        {
                                            if (trackedSosig.Update())
                                            {
                                                trackedSosig.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                                                packet.Write(trackedSosig, true, false);

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

                                                packet.Write(trackedSosig, true, false);

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

                                            packet.Write(trackedSosig, false, false);

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

                                            packet.Write(trackedSosig, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (count == 0)
                                {
                                    break;
                                }

                                // Write the count to packet
                                byte[] countArr = BitConverter.GetBytes(count);
                                for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                                {
                                    packet.buffer[i] = countArr[j];
                                }

                                SendUDPDataToClients(packet, players);
                            }
                        }
                    }
                }
            }
        }

        public static void TrackedAutoMeaters()
        {
            foreach (KeyValuePair<string, Dictionary<int, List<int>>> outer in H3MP_GameManager.autoMeatersByInstanceByScene)
            {
                foreach (KeyValuePair<int, List<int>> inner in outer.Value)
                {
                    if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(outer.Key, out Dictionary<int, List<int>> playerInstances) &&
                        playerInstances.TryGetValue(inner.Key, out List<int> players) && players.Count > 0)
                    {
                        int index = 0;
                        while (index < inner.Value.Count)
                        {
                            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeaters))
                            {
                                // Write place holder int at start to hold the count once we know it
                                int countPos = packet.buffer.Count;
                                packet.Write((short)0);

                                short count = 0;
                                for (int i = index; i < inner.Value.Count; ++i, ++index)
                                {
                                    H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[inner.Value[i]];
                                    if (trackedAutoMeater != null)
                                    {
                                        if (trackedAutoMeater.controller == 0)
                                        {
                                            if (trackedAutoMeater.Update())
                                            {
                                                trackedAutoMeater.insuranceCounter = H3MP_TrackedAutoMeaterData.insuranceCount;

                                                packet.Write(trackedAutoMeater, true, false);

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

                                                packet.Write(trackedAutoMeater, true, false);

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
                                            trackedAutoMeater.insuranceCounter = H3MP_TrackedAutoMeaterData.insuranceCount;

                                            packet.Write(trackedAutoMeater, false, false);

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

                                            packet.Write(trackedAutoMeater, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (count == 0)
                                {
                                    break;
                                }

                                // Write the count to packet
                                byte[] countArr = BitConverter.GetBytes(count);
                                for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                                {
                                    packet.buffer[i] = countArr[j];
                                }

                                SendUDPDataToClients(packet, players);
                            }
                        }
                    }
                }
            }
        }

        public static void TrackedEncryptions()
        {
            foreach (KeyValuePair<string, Dictionary<int, List<int>>> outer in H3MP_GameManager.encryptionsByInstanceByScene)
            {
                foreach (KeyValuePair<int, List<int>> inner in outer.Value)
                {
                    if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(outer.Key, out Dictionary<int, List<int>> playerInstances) &&
                        playerInstances.TryGetValue(inner.Key, out List<int> players) && players.Count > 0)
                    {
                        int index = 0;
                        while (index < inner.Value.Count)
                        {
                            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedEncryptions))
                            {
                                // Write place holder int at start to hold the count once we know it
                                int countPos = packet.buffer.Count;
                                packet.Write((short)0);

                                short count = 0;
                                for (int i = index; i < inner.Value.Count; ++i, ++index)
                                {
                                    H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[inner.Value[i]];
                                    if (trackedEncryption != null)
                                    {
                                        if (trackedEncryption.controller == 0)
                                        {
                                            if (trackedEncryption.Update())
                                            {
                                                trackedEncryption.insuranceCounter = H3MP_TrackedEncryptionData.insuranceCount;

                                                packet.Write(trackedEncryption, true, false);

                                                ++count;

                                                // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                                                if (packet.buffer.Count >= 1300)
                                                {
                                                    break;
                                                }
                                            }
                                            else if (trackedEncryption.insuranceCounter > 0)
                                            {
                                                --trackedEncryption.insuranceCounter;

                                                packet.Write(trackedEncryption, true, false);

                                                ++count;

                                                // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                                                if (packet.buffer.Count >= 1300)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        else if (trackedEncryption.NeedsUpdate())
                                        {
                                            trackedEncryption.insuranceCounter = H3MP_TrackedEncryptionData.insuranceCount;

                                            packet.Write(trackedEncryption, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                        else if (trackedEncryption.insuranceCounter > 0)
                                        {
                                            --trackedEncryption.insuranceCounter;

                                            packet.Write(trackedEncryption, false, false);

                                            ++count;

                                            // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                                            if (packet.buffer.Count >= 1300)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (count == 0)
                                {
                                    break;
                                }

                                // Write the count to packet
                                byte[] countArr = BitConverter.GetBytes(count);
                                for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                                {
                                    packet.buffer[i] = countArr[j];
                                }

                                SendUDPDataToClients(packet, players);
                            }
                        }
                    }
                }
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, List<int> toClients)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, false, true);

                SendTCPData(toClients, packet);
            }
        }

        public static void TrackedSosig(H3MP_TrackedSosigData trackedSosig, List<int> toClients)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, false, true);

                SendTCPData(toClients, packet);
            }
        }

        public static void TrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, List<int> toClients)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, false, true);

                SendTCPData(toClients, packet);
            }
        }

        public static void TrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption, List<int> toClients)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedEncryption))
            {
                packet.Write(trackedEncryption, false, true);

                SendTCPData(toClients, packet);
            }
        }

        public static void TrackedItemSpecific(H3MP_TrackedItemData trackedItem, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedItem))
            {
                packet.Write(trackedItem, false, true);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedSosigSpecific(H3MP_TrackedSosigData trackedSosig, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedSosig))
            {
                packet.Write(trackedSosig, false, true);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedAutoMeaterSpecific(H3MP_TrackedAutoMeaterData trackedAutoMeater, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, false, true);

                SendTCPData(toClientID, packet);
            }
        }

        public static void TrackedEncryptionSpecific(H3MP_TrackedEncryptionData trackedEncryption, int toClientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.trackedEncryption))
            {
                packet.Write(trackedEncryption, false, true);

                SendTCPData(toClientID, packet);
            }
        }

        public static void GiveControl(int trackedID, int clientID, List<int> debounce)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);
                if (debounce == null || debounce.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for (int i = 0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPDataToAll(packet);
            }
        }

        public static void GiveSosigControl(int trackedID, int clientID, List<int> debounce)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveSosigControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);
                if (debounce == null || debounce.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for (int i = 0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPDataToAll(packet);
            }
        }

        public static void GiveAutoMeaterControl(int trackedID, int clientID, List<int> debounce)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveAutoMeaterControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);
                if (debounce == null || debounce.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for (int i = 0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPDataToAll(packet);
            }
        }

        public static void GiveEncryptionControl(int trackedID, int clientID, List<int> debounce)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.giveEncryptionControl))
            {
                packet.Write(trackedID);
                packet.Write(clientID);
                if (debounce == null || debounce.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for (int i = 0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPDataToAll(packet);
            }
        }

        public static void DestroyItem(int trackedID, bool removeFromList = true, int clientID = -1)
        {
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
                packet.Write(removeFromList);

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

        public static void DestroyAutoMeater(int trackedID, bool removeFromList = true, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroyAutoMeater))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

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

        public static void DestroyEncryption(int trackedID, bool removeFromList = true, int clientID = -1)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.destroyEncryption))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

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

        public static void WeaponFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions, int chamberIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.weaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }
                packet.Write(chamberIndex);

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

        public static void WeaponFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.weaponFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void FlintlockWeaponBurnOffOuter(int clientID, int trackedID, FlintlockBarrel.LoadedElementType[] loadedElementTypes, float[] loadedElementPositions,
                                                       int powderAmount, bool ramRod, float num2, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.flintlockWeaponBurnOffOuter))
            {
                packet.Write(trackedID);
                packet.Write((byte)loadedElementTypes.Length);
                for (int i = 0; i < loadedElementTypes.Length; ++i)
                {
                    packet.Write((byte)loadedElementTypes[i]);
                    packet.Write(loadedElementPositions[i]);
                }
                packet.Write(powderAmount);
                packet.Write(ramRod);
                packet.Write(num2);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void FlintlockWeaponBurnOffOuter(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.flintlockWeaponBurnOffOuter);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void FlintlockWeaponFire(int clientID, int trackedID, FlintlockBarrel.LoadedElementType[] loadedElementTypes, float[] loadedElementPositions,
                                                       int[] loadedElementPowderAmounts, bool ramRod, float num5, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.flintlockWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write((byte)loadedElementTypes.Length);
                for (int i = 0; i < loadedElementTypes.Length; ++i)
                {
                    packet.Write((byte)loadedElementTypes[i]);
                    packet.Write(loadedElementPositions[i]);
                    packet.Write(loadedElementPowderAmounts[i]);
                }
                packet.Write(ramRod);
                packet.Write(num5);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void FlintlockWeaponFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.flintlockWeaponFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void BreakActionWeaponFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.breakActionWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)barrelIndex);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void BreakActionWeaponFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.breakActionWeaponFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void DerringerFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.derringerFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)barrelIndex);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void DerringerFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.derringerFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void LeverActionFirearmFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, bool hammer1, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.leverActionFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write(hammer1);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void LeverActionFirearmFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.leverActionFirearmFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void RevolvingShotgunFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.revolvingShotgunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void RevolvingShotgunFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.revolvingShotgunFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void RevolverFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.revolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void RevolverFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.revolverFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void SingleActionRevolverFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.singleActionRevolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void SingleActionRevolverFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.singleActionRevolverFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void StingerLauncherFire(int clientID, int trackedID, Vector3 targetPos, Vector3 position, Vector3 direction)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.stingerLauncherFire))
            {
                packet.Write(trackedID);
                packet.Write(targetPos);
                packet.Write(position);
                packet.Write(direction);

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

        public static void StingerLauncherFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.stingerLauncherFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void GrappleGunFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.grappleGunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void GrappleGunFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.grappleGunFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void HCBReleaseSled(int clientID, int trackedID, float cookedAmount, Vector3 position, Vector3 direction)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.HCBReleaseSled))
            {
                packet.Write(trackedID);
                packet.Write(cookedAmount);
                packet.Write(position);
                packet.Write(direction);

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

        public static void HCBReleaseSled(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.HCBReleaseSled);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void SosigWeaponFire(int clientID, int trackedID, float recoilMult, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write(recoilMult);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void SosigWeaponFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.sosigWeaponFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void LAPD2019Fire(int clientID, int trackedID, int chamberIndex, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.LAPD2019Fire))
            {
                packet.Write(trackedID);
                packet.Write(chamberIndex);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void LAPD2019Fire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.LAPD2019Fire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void MinigunFire(int clientID, int trackedID, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.minigunFire))
            {
                packet.Write(trackedID);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)1);
                    packet.Write(positions[0]);
                    packet.Write(directions[0]);
                }

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

        public static void MinigunFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.minigunFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void AttachableFirearmFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, bool firedFromInterface, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.attachableFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
                packet.Write((short)roundClass);
                packet.Write(firedFromInterface);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

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

        public static void AttachableFirearmFire(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.attachableFirearmFire);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void LAPD2019LoadBattery(int clientID, int trackedID, int batteryTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.LAPD2019LoadBattery))
            {
                packet.Write(trackedID);
                packet.Write(batteryTrackedID);

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

        public static void LAPD2019ExtractBattery(int clientID, int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.LAPD2019ExtractBattery))
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

        public static void SosigWeaponShatter(int clientID, int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigWeaponShatter))
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

        public static void UberShatterableShatter(int clientID, H3MP_Packet packet)
        {
            // Make sure the packet is set to ServerPackets.uberShatterableShatter
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.uberShatterableShatter);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if(clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void UberShatterableShatter(int trackedID, Vector3 point, Vector3 dir, float intensity)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.uberShatterableShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);

                SendTCPDataToAll(packet);
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

        public static void EncryptionDamage(H3MP_TrackedEncryptionData trackedEncryption, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionDamage))
            {
                packet.Write(trackedEncryption.trackedID);
                packet.Write(d);

                SendTCPData(trackedEncryption.controller, packet);
            }
        }

        public static void EncryptionSubDamage(H3MP_TrackedEncryptionData trackedEncryption, int index, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionSubDamage))
            {
                packet.Write(trackedEncryption.trackedID);
                packet.Write(index);
                packet.Write(d);

                SendTCPData(trackedEncryption.controller, packet);
            }
        }

        public static void SosigWeaponDamage(H3MP_TrackedItemData trackedItem, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigWeaponDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void RemoteMissileDamage(H3MP_TrackedItemData trackedItem, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.remoteMissileDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void RemoteMissileDamage(H3MP_TrackedItemData trackedItem, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.remoteMissileDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(trackedItem.controller, packet);
        }

        public static void StingerMissileDamage(H3MP_TrackedItemData trackedItem, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.stingerMissileDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void StingerMissileDamage(H3MP_TrackedItemData trackedItem, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.stingerMissileDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(trackedItem.controller, packet);
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
                    // Joint could be null is the link has been destroyed, in which case a link destruction 
                    // will have been ent anyway, meaning this data is meaningless so we can send 0 instead
                    if (joints[i] == null)
                    {
                        packet.Write(0);
                        packet.Write(0);
                        packet.Write(0);
                        packet.Write(0);
                    }
                    else
                    {
                        packet.Write(joints[i].lowTwistLimit.limit);
                        packet.Write(joints[i].highTwistLimit.limit);
                        packet.Write(joints[i].swing1Limit.limit);
                        packet.Write(joints[i].swing2Limit.limit);
                    }
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
            for(int i= 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(packet);
        }

        public static void EncryptionDamageData(H3MP_TrackedEncryption trackedEncryption)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionDamageData))
            {
                packet.Write(trackedEncryption.data.trackedID);
                packet.Write((int)Mod.TNH_EncryptionTarget_m_numHitsLeft.GetValue(trackedEncryption.physicalEncryptionScript));

                EncryptionDamageData(packet);
            }
        }

        public static void EncryptionDamageData(H3MP_Packet packet)
        {
            // Make sure the packet is set to ServerPackets.encryptionDamageData
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.encryptionDamageData);
            for(int i= 0; i < 4; ++i)
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
            for(int i= 0; i < 4; ++i)
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
            for(int i= 0; i < 4; ++i)
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

            for(int i= 0; i < 4; ++i)
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
            for(int i= 0; i < 4; ++i)
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

        public static void SosigSetCurrentOrder(H3MP_TrackedSosigData trackedSosig, Sosig.SosigOrder currentOrder, int fromClientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigSetCurrentOrder))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)currentOrder);
                switch (trackedSosig.currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        packet.Write(trackedSosig.guardPoint);
                        packet.Write(trackedSosig.guardDir);
                        packet.Write(trackedSosig.hardGuard);
                        break;
                    case Sosig.SosigOrder.Skirmish:
                        packet.Write(trackedSosig.skirmishPoint);
                        packet.Write(trackedSosig.pathToPoint);
                        packet.Write(trackedSosig.assaultPoint);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.Investigate:
                        packet.Write(trackedSosig.guardPoint);
                        packet.Write(trackedSosig.hardGuard);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                    case Sosig.SosigOrder.Wander:
                        packet.Write(trackedSosig.wanderPoint);
                        break;
                    case Sosig.SosigOrder.Assault:
                        packet.Write(trackedSosig.assaultPoint);
                        packet.Write((byte)trackedSosig.assaultSpeed);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.Idle:
                        packet.Write(trackedSosig.idleToPoint);
                        packet.Write(trackedSosig.idleDominantDir);
                        break;
                    case Sosig.SosigOrder.PathTo:
                        packet.Write(trackedSosig.pathToPoint);
                        packet.Write(trackedSosig.pathToLookDir);
                        break;
                }

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

        public static void RequestUpToDateObjects(int clientID, bool instantiateOnReceive, int forClient)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.updateRequest))
            {
                packet.Write(instantiateOnReceive);
                packet.Write(forClient);

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

        public static void SetTNHLevelID(string levelID, int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.setTNHLevelID))
            {
                packet.Write(levelID);
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

        public static void TNHHoldPointSystemNode(int instance, int levelIndex, int holdPointIndex, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointSystemNode))
            {
                packet.Write(instance);
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

        public static void TNHHoldBeginChallenge(int instance, bool controller, bool toAll, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldBeginChallenge))
            {
                packet.Write(instance);
                packet.Write(controller);

                if (toAll)
                {
                    if (clientID == 0)
                    {
                        SendTCPDataToAll(packet);
                    }
                    else
                    {
                        SendTCPDataToAll(clientID, packet);
                    }
                }
                else
                {
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void ShatterableCrateSetHoldingHealth(int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.shatterableCrateSetHoldingHealth))
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

        public static void ShatterableCrateSetHoldingToken(int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.shatterableCrateSetHoldingToken))
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

        public static void ShatterableCrateDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.shatterableCrateDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPDataToAll(packet);
            }
        }

        public static void ShatterableCrateDestroy(int trackedID, Damage d, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.shatterableCrateDestroy))
            {
                packet.Write(trackedID);
                packet.Write(d);

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

        public static void TNHSetLevel(int instance, int level, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSetLevel))
            {
                packet.Write(instance);
                packet.Write(level);

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

        public static void TNHSetPhaseTake(int instance, int holdIndex, List<int> activeSupplyPointIndices, bool init, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSetPhaseTake))
            {
                packet.Write(instance);
                packet.Write(holdIndex);
                packet.Write(activeSupplyPointIndices.Count);
                foreach (int index in activeSupplyPointIndices)
                {
                    packet.Write(index);
                }
                packet.Write(init);

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

        public static void TNHSetPhaseHold(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSetPhaseHold))
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

        public static void TNHHoldCompletePhase(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldCompletePhase))
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

        public static void TNHHoldPointFailOut(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointFailOut))
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

        public static void TNHHoldPointBeginPhase(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointBeginPhase))
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

        public static void TNHHoldPointCompleteHold(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointCompleteHold))
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

        public static void TNHSetPhaseComplete(int instance, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSetPhaseComplete))
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

        public static void TNHSetPhase(int instance, short p, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHSetPhase))
            {
                packet.Write(instance);
                packet.Write(p);

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

        public static void EncryptionRespawnSubTarg(int instance, int index, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionRespawnSubTarg))
            {
                packet.Write(instance);
                packet.Write(index);

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

        public static void EncryptionSpawnGrowth(int instance, int index, Vector3 point, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionSpawnGrowth))
            {
                packet.Write(instance);
                packet.Write(index);
                packet.Write(point);

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

        public static void EncryptionInit(int clientID, int trackedID, List<int> indices, List<Vector3> points)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionInit))
            {
                packet.Write(trackedID);
                if (indices == null || indices.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(indices.Count);
                    for (int i = 0; i < indices.Count; ++i)
                    {
                        packet.Write(indices[i]);
                        packet.Write(points[i]);
                    }
                }

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

        public static void EncryptionResetGrowth(int instance, int index, Vector3 point, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionResetGrowth))
            {
                packet.Write(instance);
                packet.Write(index);
                packet.Write(point);

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

        public static void EncryptionDisableSubtarg(int instance, int index, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.encryptionDisableSubtarg))
            {
                packet.Write(instance);
                packet.Write(index);

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

        public static void InitTNHInstances(int toClientID)
        {
            if (H3MP_GameManager.TNHInstances == null || H3MP_GameManager.TNHInstances.Count == 0)
            {
                return;
            }

            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.initTNHInstances))
            {
                packet.Write(H3MP_GameManager.TNHInstances.Count);
                foreach(KeyValuePair<int, H3MP_TNHInstance> TNHInstanceEntry in H3MP_GameManager.TNHInstances)
                {
                    packet.Write(TNHInstanceEntry.Value, true);
                }

                SendTCPData(toClientID, packet);
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int clientID, int instance, List<Vector3> data, float tickDownToID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointBeginAnalyzing))
            {
                packet.Write(instance);
                packet.Write(tickDownToID);
                if (data == null || data.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)data.Count);
                    foreach (Vector3 dataEntry in data)
                    {
                        packet.Write(dataEntry);
                    }
                }

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

        public static void TNHHoldPointBeginAnalyzing(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.TNHHoldPointBeginAnalyzing);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void TNHHoldPointRaiseBarriers(int clientID, int instance, List<int> barrierIndices, List<int> barrierPrefabIndices)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldPointRaiseBarriers))
            {
                packet.Write(instance);
                if (barrierIndices == null || barrierIndices.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(barrierIndices.Count);
                    for (int i = 0; i < barrierIndices.Count; i++)
                    {
                        packet.Write(barrierIndices[i]);
                    }
                    for (int i = 0; i < barrierIndices.Count; i++)
                    {
                        packet.Write(barrierPrefabIndices[i]);
                    }
                }

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

        public static void TNHHoldPointRaiseBarriers(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.TNHHoldPointRaiseBarriers);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void TNHHoldIdentifyEncryption(int clientID, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHHoldIdentifyEncryption))
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

        public static void SosigPriorityIFFChart(int clientID, int trackedID, int chart)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.sosigPriorityIFFChart))
            {
                packet.Write(trackedID);
                packet.Write(chart);

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

        public static void RemoteMissileDetonate(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.remoteMissileDetonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void RemoteMissileDetonate(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.remoteMissileDetonate);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void StingerMissileExplode(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.stingerMissileExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void StingerMissileExplode(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.stingerMissileExplode);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void PinnedGrenadeExplode(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.pinnedGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void PinnedGrenadeExplode(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.pinnedGrenadeExplode);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void PinnedGrenadePullPin(int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.pinnedGrenadePullPin))
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

        public static void FVRGrenadeExplode(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.FVRGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void FVRGrenadeExplode(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.FVRGrenadeExplode);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void BangSnapSplode(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.bangSnapSplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void BangSnapSplode(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.bangSnapSplode);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void C4Detonate(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.C4Detonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void C4Detonate(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.C4Detonate);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ClaymoreMineDetonate(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.claymoreMineDetonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void ClaymoreMineDetonate(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.claymoreMineDetonate);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void SLAMDetonate(int clientID, int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.SLAMDetonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

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

        public static void SLAMDetonate(int clientID, H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.SLAMDetonate);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (clientID == 0)
            {
                SendTCPDataToAll(packet);
            }
            else
            {
                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ClientDisconnect(int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.clientDisconnect))
            {
                packet.Write(clientID);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ServerClosed()
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.serverClosed))
            {
                SendTCPDataToAll(packet);
            }
        }

        public static void InitConnectionData(int toClient, byte[] data)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.initConnectionData))
            {
                packet.Write(data.Length);
                packet.Write(data);

                SendTCPData(toClient, packet);
            }
        }

        public static void SpectatorHost(int clientID, bool spectatorHost)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.spectatorHost))
            {
                packet.Write(clientID);
                packet.Write(spectatorHost);

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

        public static void ResetTNH(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.resetTNH))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void ReviveTNHPlayer(int ID, int instance, int clientID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.reviveTNHPlayer))
            {
                packet.Write(ID);
                packet.Write(instance);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void PlayerColor(int ID, int index, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.playerColor))
            {
                packet.Write(ID);
                packet.Write(index);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ColorByIFF(bool colorByIFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.colorByIFF))
            {
                packet.Write(colorByIFF);

                SendTCPDataToAll(packet);
            }
        }

        public static void NameplateMode(int nameplateMode)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.nameplateMode))
            {
                packet.Write(nameplateMode);

                SendTCPDataToAll(packet);
            }
        }

        public static void RadarMode(int radarMode)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.radarMode))
            {
                packet.Write(radarMode);

                SendTCPDataToAll(packet);
            }
        }

        public static void RadarColor(bool radarColor)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.radarColor))
            {
                packet.Write(radarColor);

                SendTCPDataToAll(packet);
            }
        }

        public static void TNHInitializer(int instance, int initializer, bool only = false)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.TNHInitializer))
            {
                packet.Write(instance);
                packet.Write(initializer);

                if (only)
                {
                    SendTCPData(initializer, packet);
                }
                else
                {
                    SendTCPDataToAll(initializer, packet);
                }
            }
        }

        public static void MaxHealth(string scene, int instance, int index, float original, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.maxHealth))
            {
                packet.Write(scene);
                packet.Write(instance);
                packet.Write(index);
                packet.Write(original);

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

        public static void FuseIgnite(int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.fuseIgnite))
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

        public static void FuseBoom(int trackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.fuseBoom))
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

        public static void MolotovShatter(int trackedID, bool ignited, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.molotovShatter))
            {
                packet.Write(trackedID);
                packet.Write(ignited);

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

        public static void MolotovDamage(H3MP_TrackedItemData itemData, Damage damage)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.molotovDamage))
            {
                packet.Write(itemData.trackedID);
                packet.Write(damage);

                SendTCPData(itemData.controller, packet);
            }
        }

        public static void MagazineAddRound(int trackedID, FireArmRoundClass roundClass, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.magazineAddRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);

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

        public static void ClipAddRound(int trackedID, FireArmRoundClass roundClass, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.clipAddRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);

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

        public static void SpeedloaderChamberLoad(int trackedID, FireArmRoundClass roundClass, int chamberIndex, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.speedloaderChamberLoad))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)chamberIndex);

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

        public static void RemoteGunChamber(int trackedID, FireArmRoundClass roundClass, FireArmRoundType roundType, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.remoteGunChamber))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((short)roundType);

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

        public static void ChamberRound(int trackedID, FireArmRoundClass roundClass, int chamberIndex, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.chamberRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)chamberIndex);

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

        public static void MagazineLoad(int trackedID, int FATrackedID, int slot = -1, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.magazineLoad))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);
                packet.Write((short)slot);

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

        public static void MagazineLoadAttachable(int trackedID, int FATrackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.magazineLoadAttachable))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);

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

        public static void ClipLoad(int trackedID, int FATrackedID, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.clipLoad))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);

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

        public static void RevolverCylinderLoad(int trackedID, Speedloader speedLoader, List<short> classes = null, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.revolverCylinderLoad))
            {
                packet.Write(trackedID);
                if (speedLoader == null)
                {
                    packet.Write((byte)classes.Count);
                    for (int i = 0; i < classes.Count; ++i)
                    {
                        packet.Write(classes[i]);
                    }
                }
                else
                {
                    packet.Write((byte)speedLoader.Chambers.Count);
                    for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                    {
                        if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                        {
                            packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                        }
                        else
                        {
                            packet.Write((short)-1);
                        }
                    }
                }

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

        public static void RevolvingShotgunLoad(int trackedID, Speedloader speedLoader, List<short> classes = null, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.revolvingShotgunLoad))
            {
                packet.Write(trackedID);
                if (speedLoader == null)
                {
                    packet.Write((byte)classes.Count);
                    for (int i = 0; i < classes.Count; ++i)
                    {
                        packet.Write(classes[i]);
                    }
                }
                else
                {
                    packet.Write((byte)speedLoader.Chambers.Count);
                    for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                    {
                        if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                        {
                            packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                        }
                        else
                        {
                            packet.Write((short)-1);
                        }
                    }
                }

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

        public static void GrappleGunLoad(int trackedID, Speedloader speedLoader, List<short> classes = null, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.grappleGunLoad))
            {
                packet.Write(trackedID);
                if (speedLoader == null)
                {
                    packet.Write((byte)classes.Count);
                    for (int i = 0; i < classes.Count; ++i)
                    {
                        packet.Write(classes[i]);
                    }
                }
                else
                {
                    packet.Write((byte)speedLoader.Chambers.Count);
                    for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                    {
                        if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                        {
                            packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                        }
                        else
                        {
                            packet.Write((short)-1);
                        }
                    }
                }

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

        public static void CarlGustafLatchSate(int trackedID, CarlGustafLatch.CGLatchType type, CarlGustafLatch.CGLatchState state, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.carlGustafLatchSate))
            {
                packet.Write(trackedID);
                packet.Write((byte)type);
                packet.Write((byte)state);

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

        public static void CarlGustafShellSlideSate(int trackedID, CarlGustafShellInsertEject.ChamberSlideState state, int clientID = 0)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ServerPackets.carlGustafShellSlideSate))
            {
                packet.Write(trackedID);
                packet.Write((byte)state);

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
    }
}
