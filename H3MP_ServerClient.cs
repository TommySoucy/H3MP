using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using FistVR;
using System.Collections.Generic;

namespace H3MP
{
    internal class H3MP_ServerClient
    {
        public static int dataBufferSize = 4096;

        public int ID;
        public H3MP_Player player;
        public TCP tcp;
        public UDP udp;
        public bool connected;
        public long ping;

        public H3MP_ServerClient(int ID)
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
            private H3MP_Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int ID)
            {
                this.ID = ID;
            }

            public void Connect(TcpClient socket)
            {
                openTime = Convert.ToInt64((DateTime.Now.ToUniversalTime() - H3MP_ThreadManager.epoch).TotalMilliseconds);

                this.socket = socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new H3MP_Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                H3MP_ServerSend.Welcome(ID, "Welcome to the server", H3MP_GameManager.colorByIFF, H3MP_GameManager.nameplateMode, H3MP_GameManager.radarMode, H3MP_GameManager.radarColor, H3MP_GameManager.maxHealthByInstanceByScene);
            }

            public void SendData(H3MP_Packet packet)
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
                    if (byteLength == 0 && H3MP_Server.clients[ID].connected)
                    {
                        H3MP_Server.clients[ID].Disconnect(0);
                        return;
                    }

                    data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    if (H3MP_Server.clients[ID].connected)
                    {
                        if (data != null)
                        {
                            for (int i = 0; i < byteLength; ++i)
                            {
                                Mod.LogWarning("data[" + i + "] = " + data[i]);
                            }
                        }
                        H3MP_Server.clients[ID].Disconnect(1, ex);
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
                    H3MP_ThreadManager.ExecuteOnMainThread(() =>
                    {
                        if (H3MP_Server.tcpListener != null)
                        {
                            using (H3MP_Packet packet = new H3MP_Packet(packetBytes))
                            {
                                int packetID = packet.ReadInt();
#if DEBUG
                                if (Input.GetKey(KeyCode.PageDown))
                                {
                                    Mod.LogInfo("\tHandling TCP packet: " + packetID);
                                }
#endif
                                H3MP_Server.packetHandlers[packetID](ID, packet);
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

            public void SendData(H3MP_Packet packet)
            {
                H3MP_Server.SendUDPData(endPoint, packet);
            }

            public void HandleData(H3MP_Packet packetData)
            {
                int packetLength = packetData.ReadInt();
                byte[] packetBytes = packetData.ReadBytes(packetLength);

                H3MP_ThreadManager.ExecuteOnMainThread(() =>
                {
                    if (H3MP_Server.tcpListener != null)
                    {
                        using (H3MP_Packet packet = new H3MP_Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
#if DEBUG
                            if (Input.GetKey(KeyCode.PageDown))
                            {
                                Mod.LogInfo("\tHandling UDP packet: " + packetID);
                            }
#endif
                            H3MP_Server.packetHandlers[packetID](ID, packet);
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
            player = new H3MP_Player(ID, playerName, Vector3.zero, IFF, colorIndex);
            player.scene = scene;
            player.instance = instance;

            // Spawn this client's player in all connected client but itself
            foreach(H3MP_ServerClient client in H3MP_Server.clients.Values)
            {
                if(client.player != null)
                {
                    if(client.ID != ID)
                    {
                        H3MP_ServerSend.SpawnPlayer(ID, client.player, scene, instance, IFF, colorIndex);
                    }
                }
            }

            // Also spawn player for host
            H3MP_GameManager.singleton.SpawnPlayer(player.ID, player.username, scene, instance, player.position, player.rotation, IFF, colorIndex);

            // Spawn all clients' players in this client
            bool inControl = true;
            foreach (H3MP_ServerClient client in H3MP_Server.clients.Values)
            {
                if(client.player != null && client.ID != ID)
                {
                    H3MP_ServerSend.SpawnPlayer(client.ID, player, client.player.scene, client.player.instance, IFF, colorIndex, true);
                    inControl &= !scene.Equals(client.player.scene);
                }
            }

            // Also spawn host player in this client
            H3MP_ServerSend.SpawnPlayer(ID, 0, Mod.config["Username"].ToString(), H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene, H3MP_GameManager.instance, GM.CurrentPlayerBody.transform.position, GM.CurrentPlayerBody.transform.rotation, GM.CurrentPlayerBody == null ? -3 : GM.CurrentPlayerBody.GetPlayerIFF(), H3MP_GameManager.colorIndex, true);
            inControl &= !scene.Equals(H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene);

            Mod.LogInfo("Player " + ID + " join server in scene " + scene, false);
            if (!H3MP_GameManager.nonSynchronizedScenes.ContainsKey(scene))
            {
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                {
                    List<int> waitingFromClients = new List<int>();

                    // There are other players in the client's scene/instance, request up to date objects before sending
                    for (int i = 0; i < otherPlayers.Count; ++i)
                    {
                        if (otherPlayers[i] != ID)
                        {
                            if (H3MP_Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                            {
                                H3MP_Server.clientsWaitingUpDate[otherPlayers[i]].Add(ID);
                            }
                            else
                            {
                                H3MP_Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { ID });
                            }
                            H3MP_ServerSend.RequestUpToDateObjects(otherPlayers[i], false, ID);
                            waitingFromClients.Add(otherPlayers[i]);
                        }
                    }

                    if (waitingFromClients.Count > 0)
                    {
                        if (H3MP_Server.loadingClientsWaitingFrom.ContainsKey(ID))
                        {
                            H3MP_Server.loadingClientsWaitingFrom[ID] = waitingFromClients;
                        }
                        else
                        {
                            H3MP_Server.loadingClientsWaitingFrom.Add(ID, waitingFromClients);
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
                H3MP_ServerSend.ConnectSync(ID, inControl);
            }

            // Also send TNH instances
            H3MP_ServerSend.InitTNHInstances(ID);

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
                H3MP_ServerSend.InitConnectionData(ID, data);
            }
        }

        public void SendRelevantTrackedObjects(int fromClient = -1)
        {
            Mod.LogInfo("Sending relevant object to " + ID + " from " + fromClient+" in "+ player.scene+"/"+ player.instance);
            // Items
            if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> itemInstances) &&
                itemInstances.TryGetValue(player.instance, out List<int> items))
            {
                for(int i=0; i < items.Count; ++i)
                {
                    H3MP_TrackedItemData trackedItemData = H3MP_Server.items[items[i]];
                    if(trackedItemData != null && (fromClient == -1 || trackedItemData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedItemData.controller == 0)
                        {
                            // Check if should send
                            if(H3MP_GameManager.sceneLoading && trackedItemData.scene.Equals(H3MP_GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedItemData.Update();
                        }
                        H3MP_ServerSend.TrackedItemSpecific(trackedItemData, ID);
                    }
                }
            }

            // Sosigs
            if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> sosigInstances) &&
                sosigInstances.TryGetValue(player.instance, out List<int> sosigs))
            {
                Mod.LogInfo("\tHas sosig");
                for (int i=0; i < sosigs.Count; ++i)
                {
                    H3MP_TrackedSosigData trackedSosigData = H3MP_Server.sosigs[sosigs[i]];
                    if(trackedSosigData != null && (fromClient == -1 || trackedSosigData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedSosigData.controller == 0)
                        {
                            // Check if should send
                            if (H3MP_GameManager.sceneLoading && trackedSosigData.scene.Equals(H3MP_GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedSosigData.Update();
                        }
                        H3MP_ServerSend.TrackedSosigSpecific(trackedSosigData, ID);
                    }
                }
            }

            // AutoMeaters
            if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> autoMeaterInstances) &&
                autoMeaterInstances.TryGetValue(player.instance, out List<int> autoMeaters))
            {
                for(int i=0; i < autoMeaters.Count; ++i)
                {
                    H3MP_TrackedAutoMeaterData trackedAutoMeaterData = H3MP_Server.autoMeaters[autoMeaters[i]];
                    if(trackedAutoMeaterData != null && (fromClient == -1 || trackedAutoMeaterData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedAutoMeaterData.controller == 0)
                        {
                            // Check if should send
                            if (H3MP_GameManager.sceneLoading && trackedAutoMeaterData.scene.Equals(H3MP_GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedAutoMeaterData.Update();
                        }
                        H3MP_ServerSend.TrackedAutoMeaterSpecific(trackedAutoMeaterData, ID);
                    }
                }
            }

            // Encryptions
            if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> encryptionInstances) &&
                encryptionInstances.TryGetValue(player.instance, out List<int> encryptions))
            {
                for(int i=0; i < encryptions.Count; ++i)
                {
                    H3MP_TrackedEncryptionData trackedEncryptionData = H3MP_Server.encryptions[encryptions[i]];
                    if(trackedEncryptionData != null && (fromClient == -1 || trackedEncryptionData.controller == fromClient))
                    {
                        // If this is ours
                        if(trackedEncryptionData.controller == 0)
                        {
                            // Check if should send
                            if (H3MP_GameManager.sceneLoading && trackedEncryptionData.scene.Equals(H3MP_GameManager.scene))
                            {
                                // We don't want to send an object that is ours from the scene we are currently loading away from
                                // So just continue to next object
                                continue;
                            }

                            // If sending, make sure it init otherwise we might be missing data
                            trackedEncryptionData.Update();
                        }
                        H3MP_ServerSend.TrackedEncryptionSpecific(trackedEncryptionData, ID);
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
            H3MP_GameManager.DistributeAllControl(ID);
            SpecificDisconnect();
            H3MP_ServerSend.ClientDisconnect(ID);

            player = null;
        }

        // MOD: This will be called after disconnection to reset specific fields
        //      For example, here we deal with current TNH data
        //      If your mod has some H3MP dependent data that you want to get rid of when you disconnect from a server, do it here
        private void SpecificDisconnect()
        {
            if (H3MP_GameManager.TNHInstances.TryGetValue(player.instance, out H3MP_TNHInstance TNHInstance) && TNHInstance.currentlyPlaying.Contains(ID)) // TNH_Manager was set to null and we are currently playing
            {
                TNHInstance.RemoveCurrentlyPlaying(true, ID, true);
            }
        }
    }
}
