﻿using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using FistVR;
using System.Collections.Generic;
using H3MP.Patches;
using H3MP.Tracking;

namespace H3MP.Networking
{
    internal class ServerClient
    {
        public static int dataBufferSize = 4096;

        public int ID;
        public Player player;
        public TCP tcp;
        public UDP udp;
        public bool connected;
        public long ping;

        public ServerClient(int ID)
        {
            this.ID = ID;
            tcp = new TCP(ID);
            udp = new UDP(ID);
        }

        public class TCP
        {
            public TcpClient socket;

            public long openTime;

            private readonly int ID;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int ID)
            {
                this.ID = ID;
            }

            public void Connect(TcpClient socket)
            {
                openTime = Convert.ToInt64((DateTime.Now.ToUniversalTime() - ThreadManager.epoch).TotalMilliseconds);

                this.socket = socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(ID, "Welcome to the server", GameManager.colorByIFF, GameManager.nameplateMode, GameManager.radarMode, GameManager.radarColor, GameManager.maxHealthByInstanceByScene);
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if(socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Mod.LogError($"Error sending data to player {ID} via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                int byteLength = 0;
                byte[] data = null;
                try
                {
                    byteLength = stream.EndRead(result);
                    if (byteLength == 0 && Server.clients[ID].connected)
                    {
                        Server.clients[ID].Disconnect(0);
                        return;
                    }

                    data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    if (Server.clients[ID].connected)
                    {
                        if (data != null)
                        {
                            for (int i = 0; i < byteLength; ++i)
                            {
                                Mod.LogWarning("data[" + i + "] = " + data[i]);
                            }
                        }
                        Server.clients[ID].Disconnect(1, ex);
                    }
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        if (Server.tcpListener != null)
                        {
                            using (Packet packet = new Packet(packetBytes))
                            {
                                int packetID = packet.ReadInt();
#if DEBUG
                                if (Input.GetKey(KeyCode.PageDown))
                                {
                                    Mod.LogInfo("\tHandling TCP packet: " + packetID);
                                }
#endif
                                Server.packetHandlers[packetID](ID, packet);
                            }
                        }
                    });

                    packetLength = 0;

                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receiveBuffer = null;
                receivedData = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int ID;

            public UDP(int ID)
            {
                this.ID = ID;
            }

            public void Connect(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(endPoint, packet);
            }

            public void HandleData(Packet packetData)
            {
                int packetLength = packetData.ReadInt();
                byte[] packetBytes = packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    if (Server.tcpListener != null)
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
#if DEBUG
                            if (Input.GetKey(KeyCode.PageDown))
                            {
                                Mod.LogInfo("\tHandling UDP packet: " + packetID);
                            }
#endif
                            Server.packetHandlers[packetID](ID, packet);
                        }
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }

        public void SendIntoGame(string playerName, string scene, int instance, int IFF, int colorIndex)
        {
            player = new Player(ID, playerName, Vector3.zero, IFF, colorIndex);
            player.scene = scene;
            player.instance = instance;

            // Spawn this client's player in all connected client but itself
            foreach(ServerClient client in Server.clients.Values)
            {
                if(client.player != null)
                {
                    if(client.ID != ID)
                    {
                        ServerSend.SpawnPlayer(ID, client.player, scene, instance, IFF, colorIndex);
                    }
                }
            }

            // Also spawn player for host
            GameManager.singleton.SpawnPlayer(player.ID, player.username, scene, instance, player.position, player.rotation, IFF, colorIndex);

            // Spawn all clients' players in this client
            bool inControl = true;
            foreach (ServerClient client in Server.clients.Values)
            {
                if(client.player != null && client.ID != ID)
                {
                    ServerSend.SpawnPlayer(client.ID, player, client.player.scene, client.player.instance, IFF, colorIndex, true);
                    inControl &= !scene.Equals(client.player.scene);
                }
            }

            // Also spawn host player in this client
            ServerSend.SpawnPlayer(ID, 0, Mod.config["Username"].ToString(), GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene, GameManager.instance, GM.CurrentPlayerBody.transform.position, GM.CurrentPlayerBody.transform.rotation, GM.CurrentPlayerBody == null ? -3 : GM.CurrentPlayerBody.GetPlayerIFF(), GameManager.colorIndex, true);
            inControl &= !scene.Equals(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene);

            Mod.LogInfo("Player " + ID + " join server in scene " + scene, false);
            if (!GameManager.nonSynchronizedScenes.ContainsKey(scene))
            {
                if (GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                {
                    List<int> waitingFromClients = new List<int>();

                    // There are other players in the client's scene/instance, request up to date objects before sending
                    for (int i = 0; i < otherPlayers.Count; ++i)
                    {
                        if (otherPlayers[i] != ID)
                        {
                            if (Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                            {
                                Server.clientsWaitingUpDate[otherPlayers[i]].Add(ID);
                            }
                            else
                            {
                                Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { ID });
                            }
                            ServerSend.RequestUpToDateObjects(otherPlayers[i], false, ID);
                            waitingFromClients.Add(otherPlayers[i]);
                        }
                    }

                    if (waitingFromClients.Count > 0)
                    {
                        if (Server.loadingClientsWaitingFrom.ContainsKey(ID))
                        {
                            Server.loadingClientsWaitingFrom[ID] = waitingFromClients;
                        }
                        else
                        {
                            Server.loadingClientsWaitingFrom.Add(ID, waitingFromClients);
                        }
                    }
                    else
                    {
                        Mod.LogInfo("Client " + ID + " just got sent into game, no other player in scene/instance, sending relevant tracked objects");
                        SendRelevantTrackedObjects();
                    }
                }
                else // No other player in the client's scene/instance 
                {
                    Mod.LogInfo("Client " + ID + " just got sent into game, no other player in scene/instance, sending relevant tracked objects");
                    SendRelevantTrackedObjects();
                }

                // Tell the client to sync its items
                ServerSend.ConnectSync(ID, inControl);
            }

            // Also send TNH instances
            ServerSend.InitTNHInstances(ID);

            // Send custom connection data
            byte[] initData = null;
            SendInitConnectionData(ID, initData);
        }

        // MOD: This will get called when the server sends all the data a newly connected client connect needs
        //      A mod that wants to send its own initial data to process can prefix this to modify data before it gets sent
        private void SendInitConnectionData(int ID, byte[] data)
        {
            if (data != null)
            {
                ServerSend.InitConnectionData(ID, data);
            }
        }

        public void SendRelevantTrackedObjects(int fromClient = -1)
        {
            Mod.LogInfo("Sending relevant object to " + ID + " from " + fromClient+" in "+ player.scene+"/"+ player.instance);
            // Items
            if (GameManager.itemsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> itemInstances) &&
                itemInstances.TryGetValue(player.instance, out List<int> items))
            {
                for(int i=0; i < items.Count; ++i)
                {
                    TrackedItemData trackedItemData = Server.items[items[i]];
                    if(trackedItemData != null && (fromClient == -1 || trackedItemData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedItemData.controller == 0)
                        {
                            // Check if should send
                            if(GameManager.sceneLoading && trackedItemData.scene.Equals(GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedItemData.Update();
                        }
                        ServerSend.TrackedItemSpecific(trackedItemData, ID);
                    }
                }
            }

            // Sosigs
            if (GameManager.sosigsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> sosigInstances) &&
                sosigInstances.TryGetValue(player.instance, out List<int> sosigs))
            {
                Mod.LogInfo("\tHas sosig");
                for (int i=0; i < sosigs.Count; ++i)
                {
                    TrackedSosigData trackedSosigData = Server.sosigs[sosigs[i]];
                    if(trackedSosigData != null && (fromClient == -1 || trackedSosigData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedSosigData.controller == 0)
                        {
                            // Check if should send
                            if (GameManager.sceneLoading && trackedSosigData.scene.Equals(GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedSosigData.Update();
                        }
                        ServerSend.TrackedSosigSpecific(trackedSosigData, ID);
                    }
                }
            }

            // AutoMeaters
            if (GameManager.autoMeatersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> autoMeaterInstances) &&
                autoMeaterInstances.TryGetValue(player.instance, out List<int> autoMeaters))
            {
                for(int i=0; i < autoMeaters.Count; ++i)
                {
                    TrackedAutoMeaterData trackedAutoMeaterData = Server.autoMeaters[autoMeaters[i]];
                    if(trackedAutoMeaterData != null && (fromClient == -1 || trackedAutoMeaterData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedAutoMeaterData.controller == 0)
                        {
                            // Check if should send
                            if (GameManager.sceneLoading && trackedAutoMeaterData.scene.Equals(GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedAutoMeaterData.Update();
                        }
                        ServerSend.TrackedAutoMeaterSpecific(trackedAutoMeaterData, ID);
                    }
                }
            }

            // Encryptions
            if (GameManager.encryptionsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> encryptionInstances) &&
                encryptionInstances.TryGetValue(player.instance, out List<int> encryptions))
            {
                for(int i=0; i < encryptions.Count; ++i)
                {
                    TrackedEncryptionData trackedEncryptionData = Server.encryptions[encryptions[i]];
                    if(trackedEncryptionData != null && (fromClient == -1 || trackedEncryptionData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedEncryptionData.controller == 0)
                        {
                            // Check if should send
                            if (GameManager.sceneLoading && trackedEncryptionData.scene.Equals(GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedEncryptionData.Update();
                        }
                        ServerSend.TrackedEncryptionSpecific(trackedEncryptionData, ID);
                    }
                }
            }
        }

        public void Disconnect(int code, Exception ex = null)
        {
            connected = false;

            switch (code)
            {
                case 0:
                    Mod.LogInfo("Client "+ID+" : " + tcp.socket.Client.RemoteEndPoint + " disconnected, end of stream.", false);
                    break;
                case 1:
                    Mod.LogInfo("Client "+ID+" : " + tcp.socket.Client.RemoteEndPoint + " forcibly disconnected.", false);
                    Mod.LogWarning("Exception: " + ex.Message + "\n" + ex);
                    break;
                case 2:
                    Mod.LogInfo("Client "+ID+" : " + tcp.socket.Client.RemoteEndPoint + " disconnected.", false);
                    break;
            }

            tcp.Disconnect();
            udp.Disconnect();

            Mod.RemovePlayerFromLists(ID);
            GameManager.DistributeAllControl(ID);
            SpecificDisconnect();
            ServerSend.ClientDisconnect(ID);

            player = null;
        }

        // MOD: This will be called after disconnection to reset specific fields
        //      For example, here we deal with current TNH data
        //      If your mod has some H3MP dependent data that you want to get rid of when you disconnect from a server, do it here
        private void SpecificDisconnect()
        {
            if (GameManager.TNHInstances.TryGetValue(player.instance, out TNHInstance TNHInstance) && TNHInstance.currentlyPlaying.Contains(ID)) // TNH_Manager was set to null and we are currently playing
            {
                TNHInstance.RemoveCurrentlyPlaying(true, ID, true);
            }
        }
    }
}