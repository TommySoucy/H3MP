using FistVR;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace H3MP.Networking
{
    public class ServerSend
    {
        private static void ConvertToCustomID(Packet packet)
        {
            byte[] convertedID = BitConverter.GetBytes(BitConverter.ToInt32(new byte[] { packet.buffer[0], packet.buffer[1], packet.buffer[2], packet.buffer[3] }, 0) * -1 - 2);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = convertedID[i];
            }
        }

        public static void SendTCPData(int toClient, Packet packet, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPData: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            Server.clients[toClient].tcp.SendData(packet);
        }

        public static void SendTCPData(List<int> toClients, Packet packet, int exclude = -1, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
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
                    Server.clients[toClients[i]].tcp.SendData(packet);
                }
            }
        }
        
        public static void SendUDPData(List<int> toClients, Packet packet, int exclude = -1, bool custom = false, object key = null)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }

            if (key == null)
            {
                key = new object();
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPData multiple: " + BitConverter.ToInt32(packet.ToArray(), 0)+" with length "+packet.buffer.Count);
            }
#endif
            packet.WriteLength();
            for (int i = 0; i < toClients.Count; ++i)
            {
                if (exclude == -1 || toClients[i] != exclude)
                {
                    //Server.clients[toClients[i]].udp.SendData(packet);
                    lock (Server.clients[toClients[i]].queuedPackets)
                    {
                        Server.clients[toClients[i]].queuedPackets[key] = packet.ToArray();
                    }
                }
            }
        }

        public static void SendAllBatchedUDPData()
        {
            foreach (ServerClient client in Server.clients.Values)
            {
                SendBatchedPackets(client);
            }
        }
        
        public static void SendBatchedPackets(ServerClient client)
        {
            List<byte[]> packetsToSend = new List<byte[]>();
            lock (client.queuedPackets)
            {
                packetsToSend.AddRange(client.queuedPackets.Values);
                client.queuedPackets.Clear();
            }

            const int mtu = 1300;
            
            Packet batchedPacket = new Packet((int) ServerPackets.batchedPacket);
            
            foreach (byte[] packetData in packetsToSend)
            {

                // If the data of this packet would put us over the MTU, send what we have now
                int curLength = batchedPacket.Length();
                if (curLength > 0 && curLength + packetData.Length > mtu)
                {
                    batchedPacket.WriteLength();
                    client.udp.SendData(batchedPacket);
                    batchedPacket.Dispose();
                    batchedPacket = new Packet((int)ServerPackets.batchedPacket);
                }
                
                // Then add the data to the batch
                batchedPacket.Write(packetData);
            }

            // Send the remaining data
            if (batchedPacket.Length() > 0)
            {
                batchedPacket.WriteLength();
                client.udp.SendData(batchedPacket);
            }
            batchedPacket.Dispose();
        }
        
        public static void SendTCPDataToAll(Packet packet, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for(int i = 1; i<= Server.maxClientCount; ++i)
            {
                Server.clients[i].tcp.SendData(packet);
            }
        }

//         public static void SendUDPDataToAll(Packet packet, bool custom = false)
//         {
//             if (custom)
//             {
//                 ConvertToCustomID(packet);
//             }
// #if DEBUG
//             if (Input.GetKey(KeyCode.PageDown))
//             {
//                 Mod.LogInfo("SendUDPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
//             }
// #endif
//             packet.WriteLength();
//             for(int i = 1; i<= Server.maxClientCount; ++i)
//             {
//                 Server.clients[i].udp.SendData(packet);
//             }
//         }

        public static void SendUDPDataToClients(Packet packet, List<int> clientIDs, int excluding = -1, bool custom = false, object key = null)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }

            if (key == null)
            {
                key = new object();
            }
            
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPDataToClients: " + BitConverter.ToInt32(packet.ToArray(), 0)+", size "+packet.buffer.Count);
            }
#endif
            packet.WriteLength();
            foreach(int clientID in clientIDs)
            {
                if (excluding == -1 || clientID != excluding)
                {
                    //Server.clients[clientID].udp.SendData(packet);
                    lock (Server.clients[clientID].queuedPackets)
                    {
                        Server.clients[clientID].queuedPackets[key] = packet.ToArray();
                    }
                }
            }
        }

        public static void SendTCPDataToClients(Packet packet, List<int> clientIDs, int excluding = -1, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPDataToClients: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            foreach(int clientID in clientIDs)
            {
                if (excluding == -1 || clientID != excluding)
                {
                    Server.clients[clientID].tcp.SendData(packet);
                }
            }
        }

        public static void SendTCPDataToAll(int exceptClient, Packet packet, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            for(int i = 1; i<= Server.maxClientCount; ++i)
            {
                if (i != exceptClient)
                {
                    Server.clients[i].tcp.SendData(packet);
                }
            }
        }

//         public static void SendUDPDataToAll(int exceptClient, Packet packet, bool custom = false)
//         {
//             if (custom)
//             {
//                 ConvertToCustomID(packet);
//             }
// #if DEBUG
//             if (Input.GetKey(KeyCode.PageDown))
//             {
//                 Mod.LogInfo("SendUDPDataToAll: " + BitConverter.ToInt32(packet.ToArray(), 0));
//             }
// #endif
//             packet.WriteLength();
//             for(int i = 1; i<= Server.maxClientCount; ++i)
//             {
//                 if (i != exceptClient)
//                 {
//                     Server.clients[i].udp.SendData(packet);
//                 }
//             }
//         }

        public static void Welcome(int toClient, string msg, bool colorByIFF, int nameplateMode, int radarMode, bool radarColor, Dictionary<string, Dictionary<int, KeyValuePair<float, int>>> maxHealthEntries)
        {
            using(Packet packet = new Packet((int)ServerPackets.welcome))
            {
                packet.Write(msg);
                packet.Write(toClient);
                packet.Write((byte)(int)Mod.config["TickRate"]);
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
                packet.Write(Mod.registeredCustomPacketIDs.Count);
                foreach(KeyValuePair<string, int> entry in Mod.registeredCustomPacketIDs)
                {
                    packet.Write(entry.Key);
                    packet.Write(entry.Value);
                }
                packet.Write(GameManager.spectatorHosts.Count);
                for(int i = 0;i < GameManager.spectatorHosts.Count; ++i)
                {
                    packet.Write(GameManager.spectatorHosts[i]);
                }

                SendTCPData(toClient, packet);
            }
        }

        public static void Ping(int toClient, long time)
        {
            using (Packet packet = new Packet((int)ServerPackets.ping))
            {
                packet.Write(time);
                SendTCPData(toClient, packet);
            }
        }

        public static void SpawnPlayer(int clientID, Player player, string scene, int instance, int IFF, int colorIndex, bool join = false)
        {
            SpawnPlayer(clientID, player.ID, player.username, scene, instance, IFF, colorIndex, join);
        }

        public static void SpawnPlayer(int clientID, int ID, string username, string scene, int instance, int IFF, int colorIndex, bool join = false)
        {
            Mod.LogInfo("Server sending SpawnPlayer order to "+clientID+": ID: "+ID+", username: "+username+", scene: "+scene+", instance: "+instance, false);
            using (Packet packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                packet.Write(ID);
                packet.Write(username);
                packet.Write(scene);
                packet.Write(instance);
                packet.Write(IFF);
                packet.Write(colorIndex);
                packet.Write(join);

                SendTCPData(clientID, packet);
            }
        }

        public static void ConnectSync(int clientID, bool inControl)
        {
            using (Packet packet = new Packet((int)ServerPackets.connectSync))
            {
                packet.Write(inControl);

                SendTCPData(clientID, packet);
            }
        }

        public static void PlayerState(Player player, string scene, int instance)
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
            if (GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(instance, out List<int> otherPlayers) &&
                otherPlayers.Count > 0 && (otherPlayers.Count > 1 || otherPlayers[0] != ID))
            {
                using (Packet packet = new Packet((int)ServerPackets.playerState))
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
                    byte[] additionalData = GameManager.playerStateAddtionalDataSize == -1 ? null : new byte[GameManager.playerStateAddtionalDataSize];
                    if (additionalData != null && additionalData.Length > 0)
                    {
                        GameManager.playerStateAddtionalDataSize = additionalData.Length;
                        packet.Write((short)additionalData.Length);
                        packet.Write(additionalData);
                    }
                    else
                    {
                        GameManager.playerStateAddtionalDataSize = 0;
                        packet.Write((short)0);
                    }

                    SendUDPData(otherPlayers, packet, ID, false, GM.CurrentPlayerBody);
                }
            }
        }

        public static void PlayerState(Packet packet, Player player)
        {
            if (GameManager.playersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(player.instance, out List<int> otherPlayers) && (otherPlayers.Count > 1 || otherPlayers[0] != player.ID))
            {
                // Make sure the packet ID is correct
                // Note: Must do this if relaying packets, because the packet ID is not necessarily the same in ClientPackets and ServerPackets
                //       so when a client receives the relayed packet, it will expect the ServerPackets ID but if we didn't replace it
                //       like we do here, it will still be the ClientPackets ID which might be different
                byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.playerState);
                for (int i = 0; i < 4; ++i)
                {
                    packet.buffer[i] = IDbytes[i];
                }

                // TODO: Optimization: Verify which is best, including the ID in the packet (Using bandwidth, time writing), or inserting it here (Time inserting) 
                // Write the player's ID because it was sent from a client and was not included in the packet
                packet.buffer.InsertRange(4, BitConverter.GetBytes(player.ID)); // 4 because we want to insert after the packet ID

                SendUDPData(otherPlayers, packet, player.ID);
            }
        }

        public static void PlayerIFF(int clientID, int IFF)
        {
            using (Packet packet = new Packet((int)ServerPackets.playerIFF))
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
            using (Packet packet = new Packet((int)ServerPackets.playerScene))
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

        // TODO: Optimization: Limit this to a max number of objects per frame, this will most likely come in useful in meatov when we have over a hundred items to update per frame
        //                     if physics are weird and objects need updates because it rotated 0.00001 deg since last update or something
        public static void TrackedObjects()
        {
            if (GameManager.playersPresent.Count > 0)
            {
                for (int i = 0; i < GameManager.objects.Count; ++i)
                {
                    TrackedObjectData trackedObject = GameManager.objects[i];

                    if (trackedObject.trackedID == -1)
                    {
                        continue;
                    }

                    if (trackedObject.Update())
                    {
                        trackedObject.latestUpdateSent = false;

                        using (Packet packet = new Packet((int)ServerPackets.trackedObjects))
                        {
                            // Write packet
                            trackedObject.WriteToPacket(packet, true, false);

                            if (packet.buffer.Count >= 1500)
                            {
                                Mod.LogWarning("Update packet size for " + trackedObject.trackedID + " of type: " + trackedObject.typeIdentifier + " is above 1500 bytes");
                            }

                            SendUDPDataToClients(packet, GameManager.playersPresent, key:trackedObject);
                        }
                    }
                    else if (!trackedObject.latestUpdateSent)
                    {
                        trackedObject.latestUpdateSent = true;

                        // Send latest update on its own
                        ObjectUpdate(trackedObject);
                    }
                }
            }
        }

        public static void TrackedObjects(Packet packet, List<int> players, int sender)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.trackedObjects);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendUDPDataToClients(packet, players, sender);
        }

        public static void ObjectUpdate(TrackedObjectData trackedObject)
        {
            using (Packet packet = new Packet((int)ServerPackets.objectUpdate))
            {
                trackedObject.WriteToPacket(packet, true, false);

                if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> playerInstances) &&
                    playerInstances.TryGetValue(trackedObject.instance, out List<int> players) && players.Count > 0)
                {
                    SendTCPDataToClients(packet, players, trackedObject.controller);
                }
            }
        }

        public static void ObjectUpdate(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.objectUpdate);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            if (GameManager.playersByInstanceByScene.TryGetValue(Server.clients[clientID].player.scene, out Dictionary<int, List<int>> playerInstances) &&
                    playerInstances.TryGetValue(Server.clients[clientID].player.instance, out List<int> players) && players.Count > 0)
            {
                SendTCPDataToClients(packet, players, clientID);
            }
        }

        public static void TrackedObject(TrackedObjectData trackedObject, List<int> toClients)
        {
            using (Packet packet = new Packet((int)ServerPackets.trackedObject))
            {
                trackedObject.WriteToPacket(packet, false, true);

                SendTCPData(toClients, packet);
            }
        }

        public static void TrackedObjectSpecific(TrackedObjectData trackedObject, int toClientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.trackedObject))
            {
                trackedObject.WriteToPacket(packet, false, true);

                SendTCPData(toClientID, packet);
            }
        }

        public static void GiveObjectControl(int trackedID, int clientID, List<int> debounce)
        {
            using (Packet packet = new Packet((int)ServerPackets.giveObjectControl))
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

        public static void DestroyObject(int trackedID, bool removeFromList = true, int clientID = -1)
        {
            using (Packet packet = new Packet((int)ServerPackets.destroyObject))
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

        public static void ObjectParent(int trackedID, int newParentID, int clientID = -1)
        {
            using (Packet packet = new Packet((int)ServerPackets.objectParent))
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
            using (Packet packet = new Packet((int)ServerPackets.weaponFire))
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

        public static void WeaponFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.flintlockWeaponBurnOffOuter))
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

        public static void FlintlockWeaponBurnOffOuter(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.flintlockWeaponFire))
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

        public static void FlintlockWeaponFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.breakActionWeaponFire))
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

        public static void BreakActionWeaponFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.derringerFire))
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

        public static void DerringerFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.leverActionFirearmFire))
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

        public static void LeverActionFirearmFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.revolvingShotgunFire))
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

        public static void RevolvingShotgunFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.revolverFire))
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

        public static void RevolverFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.singleActionRevolverFire))
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

        public static void SingleActionRevolverFire(int clientID, Packet packet)
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

        public static void StingerLauncherFire(int clientID, int trackedID, Vector3 targetPos, Vector3 position, Quaternion rotation)
        {
            using (Packet packet = new Packet((int)ServerPackets.stingerLauncherFire))
            {
                packet.Write(trackedID);
                packet.Write(targetPos);
                packet.Write(position);
                packet.Write(rotation);

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

        public static void StingerLauncherFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.grappleGunFire))
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

        public static void GrappleGunFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.HCBReleaseSled))
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

        public static void HCBReleaseSled(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.sosigWeaponFire))
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

        public static void SosigWeaponFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.LAPD2019Fire))
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

        public static void LAPD2019Fire(int clientID, Packet packet)
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

        public static void MinigunFire(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.attachableFirearmFire))
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

        public static void AttachableFirearmFire(int clientID, Packet packet)
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

        public static void IntegratedFirearmFire(int clientID, int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ServerPackets.integratedFirearmFire))
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

        public static void IntegratedFirearmFire(int clientID, Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.integratedFirearmFire);
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
            using (Packet packet = new Packet((int)ServerPackets.LAPD2019LoadBattery))
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
            using (Packet packet = new Packet((int)ServerPackets.LAPD2019ExtractBattery))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigWeaponShatter))
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
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterFireShot))
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

        public static void PlayerDamage(int clientID, float damageMult, bool head, Damage damage)
        {
            using (Packet packet = new Packet((int)ServerPackets.playerDamage))
            {
                packet.Write(damageMult);
                packet.Write(head);
                packet.Write(damage);

                SendTCPData(clientID, packet);
            }
        }

        public static void UberShatterableShatter(int clientID, Packet packet)
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

        public static void UberShatterableShatter(int trackedID, Vector3 point, Vector3 dir, float intensity, byte[] data, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.uberShatterableShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);
                if (data == null)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(data.Length);
                    packet.Write(data);
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

        public static void SosigPickUpItem(int trackedSosigID, int itemTrackedID, bool primaryHand, int fromclientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigPickUpItem))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigPlaceItemIn))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigDropSlot))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigHandDrop))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigConfigure))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkRegisterWearable))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkDeRegisterWearable))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigSetIFF))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigSetOriginalIFF))
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

        public static void SosigLinkDamage(TrackedSosigData trackedSosig, int linkIndex, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkDamage))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)linkIndex);
                packet.Write(d);

                SendTCPData(trackedSosig.controller, packet);
            }
        }

        public static void AutoMeaterDamage(TrackedAutoMeaterData trackedAutoMeater, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterDamage))
            {
                packet.Write(trackedAutoMeater.trackedID);
                packet.Write(d);

                SendTCPData(trackedAutoMeater.controller, packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(TrackedAutoMeaterData trackedAutoMeater, byte type, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterHitZoneDamage))
            {
                packet.Write(trackedAutoMeater.trackedID);
                packet.Write(type);
                packet.Write(d);

                SendTCPData(trackedAutoMeater.controller, packet);
            }
        }

        public static void EncryptionDamage(TrackedEncryptionData trackedEncryption, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.encryptionDamage))
            {
                packet.Write(trackedEncryption.trackedID);
                packet.Write(d);

                SendTCPData(trackedEncryption.controller, packet);
            }
        }

        public static void EncryptionSubDamage(TrackedEncryptionData trackedEncryption, int index, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.encryptionSubDamage))
            {
                packet.Write(trackedEncryption.trackedID);
                packet.Write(index);
                packet.Write(d);

                SendTCPData(trackedEncryption.controller, packet);
            }
        }

        public static void SosigWeaponDamage(TrackedItemData trackedItem, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigWeaponDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void RemoteMissileDamage(TrackedItemData trackedItem, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.remoteMissileDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void RemoteMissileDamage(TrackedItemData trackedItem, Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.remoteMissileDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(trackedItem.controller, packet);
        }

        public static void StingerMissileDamage(TrackedItemData trackedItem, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.stingerMissileDamage))
            {
                packet.Write(trackedItem.trackedID);
                packet.Write(d);

                SendTCPData(trackedItem.controller, packet);
            }
        }

        public static void StingerMissileDamage(TrackedItemData trackedItem, Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.stingerMissileDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(trackedItem.controller, packet);
        }

        public static void SosigWearableDamage(TrackedSosigData trackedSosig, int linkIndex, int wearableIndex, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigWearableDamage))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)wearableIndex);
                packet.Write(d);

                SendTCPData(trackedSosig.controller, packet);
            }
        }

        public static void SosigDamageData(TrackedSosig trackedSosig)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigDamageData))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(trackedSosig.physicalSosig.IsStunned);
                packet.Write(trackedSosig.physicalSosig.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosig.BodyState);
                packet.Write(trackedSosig.physicalSosig.m_isOnOffMeshLink);
                packet.Write(trackedSosig.physicalSosig.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosig.Agent.enabled);
                List<CharacterJoint> joints = trackedSosig.physicalSosig.m_joints;
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
                packet.Write(trackedSosig.physicalSosig.m_isCountingDownToStagger);
                packet.Write(trackedSosig.physicalSosig.m_staggerAmountToApply);
                packet.Write(trackedSosig.physicalSosig.m_recoveringFromBallisticState);
                packet.Write(trackedSosig.physicalSosig.m_recoveryFromBallisticLerp);
                packet.Write(trackedSosig.physicalSosig.m_tickDownToWrithe);
                packet.Write(trackedSosig.physicalSosig.m_recoveryFromBallisticTick);
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromIFF());
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromClass());
                packet.Write(trackedSosig.physicalSosig.IsBlinded);
                packet.Write(trackedSosig.physicalSosig.m_blindTime);
                packet.Write(trackedSosig.physicalSosig.IsFrozen);
                packet.Write(trackedSosig.physicalSosig.m_debuffTime_Freeze);
                packet.Write(trackedSosig.physicalSosig.GetDiedFromHeadShot());
                packet.Write(trackedSosig.physicalSosig.m_timeSinceLastDamage);
                packet.Write(trackedSosig.physicalSosig.IsConfused);
                packet.Write(trackedSosig.physicalSosig.m_confusedTime);
                packet.Write(trackedSosig.physicalSosig.m_storedShudder);

                SosigLinkDamageData(packet);
            }
        }

        public static void SosigLinkDamageData(Packet packet)
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

        public static void EncryptionDamageData(TrackedEncryption trackedEncryption)
        {
            using (Packet packet = new Packet((int)ServerPackets.encryptionDamageData))
            {
                packet.Write(trackedEncryption.data.trackedID);
                packet.Write(trackedEncryption.physicalEncryption.m_numHitsLeft);

                EncryptionDamageData(packet);
            }
        }

        public static void EncryptionDamageData(Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterHitZoneDamageData))
            {
                packet.Write(trackedID);
                packet.Write((byte)hitZone.Type);

                packet.Write(hitZone.ArmorThreshold);
                packet.Write(hitZone.LifeUntilFailure);
                packet.Write(hitZone.m_isDestroyed);

                AutoMeaterHitZoneDamageData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamageData(Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkExplodes))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);

                SosigLinkExplodes(packet, fromClientID);
            }
        }

        public static void SosigLinkExplodes(Packet packet, int fromClientID)
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
            using (Packet packet = new Packet((int)ServerPackets.sosigDies))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)damClass);
                packet.Write((byte)deathType);

                SosigDies(packet, fromClientID);
            }
        }

        public static void SosigDies(Packet packet, int fromClientID)
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
            using (Packet packet = new Packet((int)ServerPackets.playSosigFootStepSound))
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

        public static void PlaySosigFootStepSound(Packet packet, int fromClientID = 0)
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
            using (Packet packet = new Packet((int)ServerPackets.sosigRequestHitDecal))
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

        public static void SosigRequestHitDecal(Packet packet, int fromClientID = 0)
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
            using (Packet packet = new Packet((int)ServerPackets.sosigClear))
            {
                packet.Write(sosigTrackedID);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSetBodyState(int sosigTrackedID, Sosig.SosigBodyState s, int fromClientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigSetBodyState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)s);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSpeakState(int sosigTrackedID, Sosig.SosigOrder currentOrder, int fromClientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigSpeakState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigSetCurrentOrder(TrackedSosigData trackedSosig, Sosig.SosigOrder currentOrder, int fromClientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigSetCurrentOrder))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigVaporize))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)iff);

                SendTCPDataToAll(fromClientID, packet);
            }
        }

        public static void SosigLinkBreak(int sosigTrackedID, int linkIndex, bool isStart, byte damClass, int fromClientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkBreak))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigLinkSever))
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
            using (Packet packet = new Packet((int)ServerPackets.updateRequest))
            {
                packet.Write(instantiateOnReceive);
                packet.Write(forClient);

                SendTCPData(clientID, packet);
            }
        }

        public static void PlayerInstance(int clientID, int instance)
        {
            using (Packet packet = new Packet((int)ServerPackets.playerInstance))
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

        public static void AddTNHInstance(TNHInstance instance)
        {
            using (Packet packet = new Packet((int)ServerPackets.addTNHInstance))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void AddInstance(int instance)
        {
            using (Packet packet = new Packet((int)ServerPackets.addInstance))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void AddTNHCurrentlyPlaying(int instance, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.addTNHCurrentlyPlaying))
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
            using (Packet packet = new Packet((int)ServerPackets.removeTNHCurrentlyPlaying))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHProgression))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHEquipment))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHHealthMode))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHTargetMode))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHAIDifficulty))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHRadarMode))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHItemSpawnerMode))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHBackpackMode))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHHealthMult))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHSosigGunReload))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHSeed))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHLevelID))
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
            using (Packet packet = new Packet((int)ServerPackets.setTNHController))
            {
                Mod.LogInfo("Server sending TNH controller " + newController + " for instance " + instance);
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
            using (Packet packet = new Packet((int)ServerPackets.TNHPlayerDied))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHAddTokens))
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
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterSetState))
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
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterSetBladesActive))
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
            using (Packet packet = new Packet((int)ServerPackets.autoMeaterFirearmFireAtWill))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSosigKill))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointSystemNode))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldBeginChallenge))
            {
                Mod.LogInfo("TNHHoldBeginChallenge server send", false);
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
                    Mod.LogInfo("\tSpecifically to controller: "+clientID, false);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void ShatterableCrateSetHoldingHealth(int trackedID, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.shatterableCrateSetHoldingHealth))
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
            using (Packet packet = new Packet((int)ServerPackets.shatterableCrateSetHoldingToken))
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
            using (Packet packet = new Packet((int)ServerPackets.shatterableCrateDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPDataToAll(packet);
            }
        }

        public static void ShatterableCrateDestroy(int trackedID, Damage d, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.shatterableCrateDestroy))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSetLevel))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSetPhaseTake))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSetPhaseHold))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldCompletePhase))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointFailOut))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointBeginPhase))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointCompleteHold))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSetPhaseComplete))
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
            using (Packet packet = new Packet((int)ServerPackets.TNHSetPhase))
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
            using (Packet packet = new Packet((int)ServerPackets.encryptionRespawnSubTarg))
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

        public static void EncryptionRespawnSubTargGeo(int instance, int index, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.encryptionRespawnSubTargGeo))
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
            using (Packet packet = new Packet((int)ServerPackets.encryptionSpawnGrowth))
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

        public static void EncryptionInit(int clientID, int trackedID, List<int> indices, List<Vector3> points, Vector3 initialPos, int numHitsLeft)
        {
            using (Packet packet = new Packet((int)ServerPackets.encryptionInit))
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
                    }
                }
                if (points == null || points.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(points.Count);
                    for (int i = 0; i < points.Count; ++i)
                    {
                        packet.Write(points[i]);
                    }
                }
                packet.Write(initialPos);
                packet.Write(numHitsLeft);

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
            using (Packet packet = new Packet((int)ServerPackets.encryptionResetGrowth))
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
            using (Packet packet = new Packet((int)ServerPackets.encryptionDisableSubtarg))
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
            if (GameManager.TNHInstances == null || GameManager.TNHInstances.Count == 0)
            {
                return;
            }

            using (Packet packet = new Packet((int)ServerPackets.initTNHInstances))
            {
                packet.Write(GameManager.TNHInstances.Count);
                foreach(KeyValuePair<int, TNHInstance> TNHInstanceEntry in GameManager.TNHInstances)
                {
                    packet.Write(TNHInstanceEntry.Value, true);
                }

                SendTCPData(toClientID, packet);
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int clientID, int instance, List<Vector3> data, float tickDownToID)
        {
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointBeginAnalyzing))
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

        public static void TNHHoldPointBeginAnalyzing(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldPointRaiseBarriers))
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

        public static void TNHHoldPointRaiseBarriers(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.TNHHoldIdentifyEncryption))
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
            using (Packet packet = new Packet((int)ServerPackets.sosigPriorityIFFChart))
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
            using (Packet packet = new Packet((int)ServerPackets.remoteMissileDetonate))
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

        public static void RemoteMissileDetonate(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.stingerMissileExplode))
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

        public static void StingerMissileExplode(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.pinnedGrenadeExplode))
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

        public static void PinnedGrenadeExplode(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.pinnedGrenadePullPin))
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
            using (Packet packet = new Packet((int)ServerPackets.FVRGrenadeExplode))
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

        public static void FVRGrenadeExplode(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.bangSnapSplode))
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

        public static void BangSnapSplode(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.C4Detonate))
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

        public static void C4Detonate(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.claymoreMineDetonate))
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

        public static void ClaymoreMineDetonate(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.SLAMDetonate))
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

        public static void SLAMDetonate(int clientID, Packet packet)
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
            using (Packet packet = new Packet((int)ServerPackets.clientDisconnect))
            {
                packet.Write(clientID);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ServerClosed()
        {
            using (Packet packet = new Packet((int)ServerPackets.serverClosed))
            {
                SendTCPDataToAll(packet);
            }
        }

        public static void SpectatorHost(int clientID, bool spectatorHost)
        {
            using (Packet packet = new Packet((int)ServerPackets.spectatorHost))
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
            using (Packet packet = new Packet((int)ServerPackets.resetTNH))
            {
                packet.Write(instance);

                SendTCPDataToAll(packet);
            }
        }

        public static void ReviveTNHPlayer(int ID, int instance, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.reviveTNHPlayer))
            {
                packet.Write(ID);
                packet.Write(instance);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void PlayerColor(int ID, int index, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.playerColor))
            {
                packet.Write(ID);
                packet.Write(index);

                SendTCPDataToAll(clientID, packet);
            }
        }

        public static void ColorByIFF(bool colorByIFF)
        {
            using (Packet packet = new Packet((int)ServerPackets.colorByIFF))
            {
                packet.Write(colorByIFF);

                SendTCPDataToAll(packet);
            }
        }

        public static void NameplateMode(int nameplateMode)
        {
            using (Packet packet = new Packet((int)ServerPackets.nameplateMode))
            {
                packet.Write(nameplateMode);

                SendTCPDataToAll(packet);
            }
        }

        public static void RadarMode(int radarMode)
        {
            using (Packet packet = new Packet((int)ServerPackets.radarMode))
            {
                packet.Write(radarMode);

                SendTCPDataToAll(packet);
            }
        }

        public static void RadarColor(bool radarColor)
        {
            using (Packet packet = new Packet((int)ServerPackets.radarColor))
            {
                packet.Write(radarColor);

                SendTCPDataToAll(packet);
            }
        }

        public static void TNHInitializer(int instance, int initializer, bool only = false)
        {
            using (Packet packet = new Packet((int)ServerPackets.TNHInitializer))
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
            using (Packet packet = new Packet((int)ServerPackets.maxHealth))
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
            using (Packet packet = new Packet((int)ServerPackets.fuseIgnite))
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
            using (Packet packet = new Packet((int)ServerPackets.fuseBoom))
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
            using (Packet packet = new Packet((int)ServerPackets.molotovShatter))
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

        public static void MolotovDamage(TrackedItemData itemData, Damage damage)
        {
            using (Packet packet = new Packet((int)ServerPackets.molotovDamage))
            {
                packet.Write(itemData.trackedID);
                packet.Write(damage);

                SendTCPData(itemData.controller, packet);
            }
        }

        public static void MagazineAddRound(int trackedID, FireArmRoundClass roundClass, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.magazineAddRound))
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
            using (Packet packet = new Packet((int)ServerPackets.clipAddRound))
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
            using (Packet packet = new Packet((int)ServerPackets.speedloaderChamberLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.remoteGunChamber))
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
            using (Packet packet = new Packet((int)ServerPackets.chamberRound))
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
            using (Packet packet = new Packet((int)ServerPackets.magazineLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.magazineLoadAttachable))
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
            using (Packet packet = new Packet((int)ServerPackets.clipLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.revolverCylinderLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.revolvingShotgunLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.grappleGunLoad))
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
            using (Packet packet = new Packet((int)ServerPackets.carlGustafLatchSate))
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
            using (Packet packet = new Packet((int)ServerPackets.carlGustafShellSlideSate))
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

        public static void TNHHostStartHold(int instance, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.TNHHostStartHold))
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

        public static void GrappleAttached(int trackedID, byte[] data, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.grappleAttached))
            {
                packet.Write(trackedID);
                packet.Write((short)data.Length);
                packet.Write(data);

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

        public static void RegisterCustomPacketType(string handlerID, int index)
        {
            using (Packet packet = new Packet((int)ServerPackets.registerCustomPacketType))
            {
                packet.Write(handlerID);
                packet.Write(index);

                SendTCPDataToAll(packet);
            }
        }

        public static void BreakableGlassDamage(TrackedBreakableGlassData trackedBreakableGlassData, Damage d)
        {
            using (Packet packet = new Packet((int)ServerPackets.breakableGlassDamage))
            {
                packet.Write(trackedBreakableGlassData.trackedID);
                packet.Write(d);

                SendTCPData(trackedBreakableGlassData.controller, packet);
            }
        }

        public static void BreakableGlassDamage(Packet packet, int controller)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.breakableGlassDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(controller, packet);
        }

        public static void WindowShatterSound(int trackedID, int mode, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.windowShatterSound))
            {
                packet.Write(trackedID);
                packet.Write((byte)mode);

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

        public static void SpectatorHostAssignment(int host, int clientID, bool reassignment = false)
        {
            using (Packet packet = new Packet((int)ServerPackets.spectatorHostAssignment))
            {
                packet.Write(host);
                packet.Write(clientID);
                packet.Write(reassignment);

                SendTCPDataToClients(packet, new List<int>() { host, clientID }, 0);
            }
        }

        public static void GiveUpSpectatorHost(int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.giveUpSpectatorHost))
            {
                SendTCPData(clientID, packet);
            }
        }

        public static void SpectatorHostOrderTNHHost(int host, bool spectateOnDeath)
        {
            using (Packet packet = new Packet((int)ServerPackets.spectatorHostOrderTNHHost))
            {
                packet.Write(spectateOnDeath);

                SendTCPData(host, packet);
            }
        }

        public static void TNHSpectatorHostReady(int controller, int instance)
        {
            using (Packet packet = new Packet((int)ServerPackets.TNHSpectatorHostReady))
            {
                packet.Write(instance);

                SendTCPData(controller, packet);
            }
        }

        public static void SpectatorHostStartTNH(int host)
        {
            using (Packet packet = new Packet((int)ServerPackets.spectatorHostStartTNH))
            {
                SendTCPData(host, packet);
            }
        }

        public static void UnassignSpectatorHost(int host)
        {
            using (Packet packet = new Packet((int)ServerPackets.unassignSpectatorHost))
            {
                SendTCPData(host, packet);
            }
        }

        public static void ReactiveSteelTargetDamage(int trackedID, int targetIndex, float damKinetic, float damBlunt, Vector3 point, Vector3 dir, bool usesHoles, Vector3 pos, Quaternion rot, float scale)
        {
            using (Packet packet = new Packet((int)ServerPackets.reactiveSteelTargetDamage))
            {
                packet.Write(trackedID);
                packet.Write(targetIndex);
                packet.Write(damKinetic);
                packet.Write(damBlunt);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(usesHoles);
                if (usesHoles)
                {
                    packet.Write(pos);
                    packet.Write(rot);
                    packet.Write(scale);
                }

                SendTCPDataToAll(packet);
            }
        }

        public static void ReactiveSteelTargetDamage(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.reactiveSteelTargetDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPDataToAll(clientID, packet);
        }

        public static void IDConfirm(KeyValuePair<int, List<int>> entry)
        {
            using (Packet packet = new Packet((int)ServerPackets.IDConfirm))
            {
                packet.Write(entry.Key);

                SendTCPDataToClients(packet, entry.Value);
            }
        }

        public static void PlayerPrefabID(int clientID, string ID, int secondClientID)
        {
            TODO: // Make this into the player model enforcement packet
            using (Packet packet = new Packet((int)ServerPackets.enforcePlayerModels))
            {
                packet.Write(clientID);
                packet.Write(ID);

                if(secondClientID == 0)
                {
                    SendTCPDataToAll(packet);
                }
                else
                {
                    SendTCPDataToAll(secondClientID, packet);
                }
            }
        }

        public static void ObjectScene(TrackedObjectData trackedObjectData, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.objectScene))
            {
                trackedObjectData.WriteToPacket(packet, false, true);
                
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

        public static void ObjectInstance(TrackedObjectData trackedObjectData, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.objectInstance))
            {
                trackedObjectData.WriteToPacket(packet, false, true);

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

        public static void UpdateEncryptionDisplay(int trackedID, int numHitsLeft, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.updateEncryptionDisplay))
            {
                packet.Write(trackedID);
                packet.Write(numHitsLeft);

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

        public static void RoundDamage(int trackedID, Damage damage, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.roundDamage))
            {
                packet.Write(trackedID);
                packet.Write(damage);

                SendTCPData(clientID, packet);
            }
        }

        public static void RoundSplode(int trackedID, float velMultiplier, bool isRandomDir, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.roundSplode))
            {
                packet.Write(trackedID);
                packet.Write(velMultiplier);
                packet.Write(isRandomDir);

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

        public static void ConnectionComplete(int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.connectionComplete))
            {
                SendTCPData(clientID, packet);
            }
        }

        public static void SightFlipperState(int trackedID, int index, bool large, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sightFlipperState))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(large);

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

        public static void SightRaiserState(int trackedID, int index, AR15HandleSightRaiser.SightHeights height, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.sightRaiserState))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write((byte)height);

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

        public static void GatlingGunFire(int trackedID, Vector3 pos, Quaternion rot, Vector3 dir, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.gatlingGunFire))
            {
                packet.Write(trackedID);
                packet.Write(pos);
                packet.Write(rot);
                packet.Write(dir);

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

        public static void GasCuboidGout(int trackedID, Vector3 pos, Vector3 norm, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidGout))
            {
                packet.Write(trackedID);
                packet.Write(pos);
                packet.Write(norm);

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

        public static void GasCuboidDamage(int trackedID, Damage d, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(clientID, packet);
            }
        }

        public static void GasCuboidDamage(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.gasCuboidDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(clientID, packet);
        }

        public static void GasCuboidHandleDamage(int trackedID, Damage d, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidHandleDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(clientID, packet);
            }
        }

        public static void GasCuboidHandleDamage(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.gasCuboidHandleDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(clientID, packet);
        }

        public static void GasCuboidDamageHandle(int trackedID, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidDamageHandle))
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

        public static void GasCuboidExplode(int trackedID, Vector3 point, Vector3 dir, bool big, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidExplode))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(big);

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

        public static void GasCuboidShatter(int trackedID, Vector3 point, Vector3 dir, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.gasCuboidShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);

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

        public static void FloaterDamage(int trackedID, Damage d, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.floaterDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(clientID, packet);
            }
        }

        public static void FloaterDamage(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.floaterDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(clientID, packet);
        }

        public static void FloaterCoreDamage(int trackedID, Damage d, int clientID)
        {
            using (Packet packet = new Packet((int)ServerPackets.floaterCoreDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(clientID, packet);
            }
        }

        public static void FloaterCoreDamage(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.floaterCoreDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(clientID, packet);
        }

        public static void FloaterBeginExploding(int trackedID, bool fromController, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.floaterBeginExploding))
            {
                packet.Write(trackedID);
                packet.Write(fromController);

                if (fromController)
                {
                    SendTCPDataToAll(clientID, packet);
                }
                else
                {
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void FloaterBeginDefusing(int trackedID, bool fromController, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.floaterBeginDefusing))
            {
                packet.Write(trackedID);
                packet.Write(fromController);

                if (fromController)
                {
                    SendTCPDataToAll(clientID, packet);
                }
                else
                {
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void FloaterExplode(int trackedID, bool defusing, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.floaterExplode))
            {
                packet.Write(trackedID);
                packet.Write(defusing);

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

        public static void IrisShatter(int trackedID, byte index, Vector3 point, Vector3 dir, float intensity, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.irisShatter))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);

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

        public static void IrisShatter(Packet packet, int clientID)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ServerPackets.irisShatter);
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

        public static void IrisSetState(int trackedID, Construct_Iris.IrisState state, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.irisSetState))
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

        public static void BrutBlockSystemStart(int trackedID, bool next, int clientID = 0)
        {
            using (Packet packet = new Packet((int)ServerPackets.brutBlockSystemStart))
            {
                packet.Write(trackedID);
                packet.Write(next);

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
