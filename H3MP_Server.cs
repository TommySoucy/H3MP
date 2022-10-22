using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace H3MP
{
    internal class H3MP_Server
    {
        public static ushort port;
        public static ushort maxClientCount;
        public static Dictionary<int, H3MP_ServerClient> clients = new Dictionary<int, H3MP_ServerClient>();
        public delegate void PacketHandler(int clientID, H3MP_Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static H3MP_TrackedItemData[] items; // All tracked items, regardless of whos control they are under
        public static List<int> availableItemIndices;

        public static TcpListener tcpListener;
        public static UdpClient udpListener;

        public static void Start(ushort _maxClientCount, ushort _port)
        {
            maxClientCount = _maxClientCount;
            port = _port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on: {tcpListener.LocalEndpoint}");

            // Just connected, sync if current scene is syncable
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(SceneManager.GetActiveScene().name))
            {
                H3MP_GameManager.SyncTrackedItems();
            }
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            Console.WriteLine("TCP connect callback");
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}");

            for (int i = 1; i <= maxClientCount; ++i)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect, server full");
        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if(data.Length < 4)
                {
                    return;
                }

                using(H3MP_Packet packet = new H3MP_Packet(data))
                {
                    int clientID = packet.ReadInt();

                    if (clientID == 0)
                    {
                        return;
                    }

                    if (clients[clientID].udp.endPoint == null)
                    {
                        clients[clientID].udp.Connect(clientEndPoint);
                        return;
                    }

                    if (clients[clientID].udp.endPoint.ToString() == clientEndPoint.ToString())
                    {
                        clients[clientID].udp.HandleData(packet);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Log($"Error receiving UDP data: {ex}");
            }
        }

        public static void SendUDPData(IPEndPoint clientEndPoint, H3MP_Packet packet)
        {
            try
            {
                if(clientEndPoint != null)
                {
                    udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch(Exception ex)
            {
                Debug.Log($"Error sending UDP data to {clientEndPoint}: {ex}");
            }
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem)
        {
            // Adjust items size to acommodate if necessary
            if(availableItemIndices.Count == 0)
            {
                IncreaseItemsSize();
            }

            // Add it to server global list
            trackedItem.trackedID = availableItemIndices[availableItemIndices.Count - 1];
            availableItemIndices.RemoveAt(availableItemIndices.Count - 1);

            items[trackedItem.trackedID] = trackedItem;

            // Send to all clients, including controller because they need confirmation from server that this item was added and its trackedID
            H3MP_ServerSend.TrackedItem(trackedItem);
        }

        private static void IncreaseItemsSize()
        {
            H3MP_TrackedItemData[] tempItems = items;
            items = new H3MP_TrackedItemData[tempItems.Length + 100];
            for(int i=0; i<tempItems.Length;++i)
            {
                items[i] = tempItems[i];
            }
            for(int i=tempItems.Length; i < items.Length; ++i)
            {
                availableItemIndices.Add(i);
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= maxClientCount; ++i)
            {
                clients.Add(i, new H3MP_ServerClient(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, H3MP_ServerHandle.WelcomeReceived },
                { (int)ClientPackets.playerState, H3MP_ServerHandle.PlayerState },
                { (int)ClientPackets.playerScene, H3MP_ServerHandle.PlayerScene },
                { (int)ClientPackets.addSyncScene, H3MP_ServerHandle.AddSyncScene },
                { (int)ClientPackets.trackedItems, H3MP_ServerHandle.TrackedItems },
                { (int)ClientPackets.takeControl, H3MP_ServerHandle.TakeControl },
                { (int)ClientPackets.giveControl, H3MP_ServerHandle.GiveControl },
                { (int)ClientPackets.destroyItem, H3MP_ServerHandle.DestroyItem },
                { (int)ClientPackets.trackedItem, H3MP_ServerHandle.TrackedItem },
            };

            items = new H3MP_TrackedItemData[100];
            availableItemIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
                                                     10,11,12,13,14,15,16,17,18,19,
                                                     20,21,22,23,24,25,26,27,28,29,
                                                     30,31,32,33,34,35,36,37,38,39,
                                                     40,41,42,43,44,45,46,47,48,49,
                                                     50,51,52,53,54,55,56,57,58,59,
                                                     60,61,62,63,64,65,66,67,68,69,
                                                     70,71,72,73,74,75,76,77,78,79,
                                                     80,81,82,83,84,85,86,87,88,89,
                                                     90,91,92,93,94,95,96,97,98,99};
             
        Debug.Log("Initialized server");
        }
    }
}
