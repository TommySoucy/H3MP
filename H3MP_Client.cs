using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_Client : MonoBehaviour
    {
        private static H3MP_Client _singleton;
        public static H3MP_Client singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(H3MP_Client)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public static int dataBufferSize = 4096;

        public string IP;
        public ushort port;
        public int ID;
        public TCP tcp;
        public UDP udp;

        private bool isConnected = false;
        private delegate void PacketHandler(H3MP_Packet packet);
        private static Dictionary<int, PacketHandler> packetHandlers;
        public static Dictionary<string, int> synchronizedScenes;
        public static H3MP_TrackedItemData[] items; // All tracked items, regardless of whos control they are under
        public static List<int> availableItemIndices;

        private void Awake()
        {
            singleton = this;
        }

        private void Start()
        {
            if (tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }
        }

        public void ConnectToServer()
        {
            if(tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }

            InitializeClientData();

            isConnected = true;
            tcp.Connect();
        }

        public class TCP
        {
            public TcpClient socket;

            public NetworkStream stream;
            private H3MP_Packet receivedData;
            public byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];
                Debug.Log("Making connection to " + singleton.IP + ":" + singleton.port);
                socket.BeginConnect(singleton.IP, singleton.port, ConnectCallback, socket);
                Debug.Log("connection begun");
            }

            private void ConnectCallback(IAsyncResult result)
            {
                Debug.Log("Connect callback");
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new H3MP_Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
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
                    Debug.Log($"Error sending data to server via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if (byteLength == 0)
                    {
                        singleton.Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error receiving TCP data {ex}");
                    Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if(packetLength <= 0)
                    {
                        return true;
                    }
                }

                while(packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    H3MP_ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using(H3MP_Packet packet = new H3MP_Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
                            packetHandlers[packetID](packet);
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

                if(packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            private void Disconnect()
            {
                singleton.Disconnect();

                stream = null;
                receiveBuffer = null;
                receivedData = null;
                socket = null;
            }
        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(singleton.IP), singleton.port);
            }

            public void Connect(int localPort)
            {
                socket = new UdpClient(localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using(H3MP_Packet packet = new H3MP_Packet())
                {
                    SendData(packet);
                }
            }

            public void SendData(H3MP_Packet packet)
            {
                try
                {
                    packet.InsertInt(singleton.ID);
                    if(socket != null)
                    {
                        socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Debug.Log($"Error sending UDP data {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    byte[] data = socket.EndReceive(result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if(data.Length < 4)
                    {
                        singleton.Disconnect();
                        return;
                    }

                    HandleData(data);
                }
                catch(Exception ex)
                {
                    Debug.Log($"Error receiving UDP data {ex}");
                    Disconnect();
                }
            }

            private void HandleData(byte[] data)
            {
                using(H3MP_Packet packet = new H3MP_Packet(data))
                {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                H3MP_ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(H3MP_Packet packet = new H3MP_Packet(data))
                    {
                        int packetID = packet.ReadInt();
                        packetHandlers[packetID](packet);
                    }
                });
            }

            private void Disconnect()
            {
                singleton.Disconnect();

                endPoint = null;
                socket = null;
            }
        }

        private void InitializeClientData()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ServerPackets.welcome, H3MP_ClientHandle.Welcome },
                { (int)ServerPackets.spawnPlayer, H3MP_ClientHandle.SpawnPlayer },
                { (int)ServerPackets.playerState, H3MP_ClientHandle.PlayerState },
                { (int)ServerPackets.playerScene, H3MP_ClientHandle.PlayerScene },
                { (int)ServerPackets.addSyncScene, H3MP_ClientHandle.AddSyncScene },
                { (int)ServerPackets.takeControl, H3MP_ClientHandle.TakeControl },
                { (int)ServerPackets.giveControl, H3MP_ClientHandle.GiveControl },
                { (int)ServerPackets.trackedItems, H3MP_ClientHandle.TrackedItems },
                { (int)ServerPackets.destroyItem, H3MP_ClientHandle.DestroyItem },
                { (int)ServerPackets.trackedItem, H3MP_ClientHandle.TrackedItem },
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

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

            Debug.Log("Initialized client");
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem)
        {
            // Adjust items size to acommodate if necessary
            if (items.Length <= trackedItem.trackedID)
            {
                IncreaseItemsSize(trackedItem.trackedID);
            }

            // Add the item to client global list
            items[trackedItem.trackedID] = trackedItem;
        }

        private static void IncreaseItemsSize(int minimum)
        {
            int minCapacity = items.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedItemData[] tempItems = items;
            items = new H3MP_TrackedItemData[minCapacity];
            for (int i = 0; i < tempItems.Length; ++i)
            {
                items[i] = tempItems[i];
            }
            for (int i = tempItems.Length; i < items.Length; ++i)
            {
                availableItemIndices.Add(i);
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                tcp.socket.Close();
                udp.socket.Close();

                Debug.Log("Disconnected from server.");
            }
        }
    }
}
