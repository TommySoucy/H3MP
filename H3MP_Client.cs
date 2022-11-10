using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public static H3MP_TrackedSosigData[] sosigs; // All tracked items, regardless of whos control they are under

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
                { (int)ServerPackets.giveControl, H3MP_ClientHandle.GiveControl },
                { (int)ServerPackets.trackedItems, H3MP_ClientHandle.TrackedItems },
                { (int)ServerPackets.destroyItem, H3MP_ClientHandle.DestroyItem },
                { (int)ServerPackets.trackedItem, H3MP_ClientHandle.TrackedItem },
                { (int)ServerPackets.itemParent, H3MP_ClientHandle.ItemParent },
                { (int)ServerPackets.connectSync, H3MP_ClientHandle.ConnectSync },
                { (int)ServerPackets.weaponFire, H3MP_ClientHandle.WeaponFire },
                { (int)ServerPackets.playerDamage, H3MP_ClientHandle.PlayerDamage },
                { (int)ServerPackets.trackedSosig, H3MP_ClientHandle.TrackedSosig },
                { (int)ServerPackets.trackedSosigs, H3MP_ClientHandle.TrackedSosigs },
                { (int)ServerPackets.giveSosigControl, H3MP_ClientHandle.GiveSosigControl },
                { (int)ServerPackets.destroySosig, H3MP_ClientHandle.DestroySosig },
                { (int)ServerPackets.sosigPickUpItem, H3MP_ClientHandle.SosigPickUpItem },
                { (int)ServerPackets.sosigPlaceItemIn, H3MP_ClientHandle.SosigPlaceItemIn },
                { (int)ServerPackets.sosigDropSlot, H3MP_ClientHandle.SosigDropSlot },
                { (int)ServerPackets.sosigHandDrop, H3MP_ClientHandle.SosigHandDrop },
                { (int)ServerPackets.sosigConfigure, H3MP_ClientHandle.SosigConfigure },
                { (int)ServerPackets.sosigLinkRegisterWearable, H3MP_ClientHandle.SosigLinkRegisterWearable },
                { (int)ServerPackets.sosigLinkDeRegisterWearable, H3MP_ClientHandle.SosigLinkDeRegisterWearable },
                { (int)ServerPackets.sosigSetIFF, H3MP_ClientHandle.SosigSetIFF },
                { (int)ServerPackets.sosigSetOriginalIFF, H3MP_ClientHandle.SosigSetOriginalIFF },
                { (int)ServerPackets.sosigLinkDamage, H3MP_ClientHandle.SosigLinkDamage },
                { (int)ServerPackets.sosigDamageData, H3MP_ClientHandle.SosigDamageData },
                { (int)ServerPackets.sosigWearableDamage, H3MP_ClientHandle.SosigWearableDamage },
                { (int)ServerPackets.sosigLinkExplodes, H3MP_ClientHandle.SosigLinkExplodes },
                { (int)ServerPackets.sosigDies, H3MP_ClientHandle.SosigDies },
                { (int)ServerPackets.sosigClear, H3MP_ClientHandle.SosigClear },
                { (int)ServerPackets.playSosigFootStepSound, H3MP_ClientHandle.PlaySosigFootStepSound },
                { (int)ServerPackets.sosigSpeakState, H3MP_ClientHandle.SosigSpeakState },
                { (int)ServerPackets.sosigSetCurrentOrder, H3MP_ClientHandle.SosigSetCurrentOrder },
                { (int)ServerPackets.sosigVaporize, H3MP_ClientHandle.SosigVaporize },
                { (int)ServerPackets.sosigLinkBreak, H3MP_ClientHandle.SosigLinkBreak },
                { (int)ServerPackets.sosigLinkSever, H3MP_ClientHandle.SosigLinkSever },
                { (int)ServerPackets.sosigRequestHitDecal, H3MP_ClientHandle.SosigRequestHitDecal },
                { (int)ServerPackets.updateRequest, H3MP_ClientHandle.RequestUpToDateObjects },
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

            items = new H3MP_TrackedItemData[100];

            sosigs = new H3MP_TrackedSosigData[100];

            Debug.Log("Initialized client");
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem, string scene)
        {
            // Adjust items size to acommodate if necessary
            if (items.Length <= trackedItem.trackedID)
            {
                IncreaseItemsSize(trackedItem.trackedID);
            }

            if (trackedItem.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the item it is because we are the one who send the item to the server
                // We just need to update the tracked ID of the item
                H3MP_GameManager.items[trackedItem.localTrackedID].trackedID = trackedItem.trackedID;

                // Add the item to client global list
                items[trackedItem.trackedID] = H3MP_GameManager.items[trackedItem.localTrackedID];
            }
            else
            {
                trackedItem.localTrackedID = -1;

                // Add the item to client global list
                items[trackedItem.trackedID] = trackedItem;

                // Instantiate item if it is in the current scene
                if (scene.Equals(SceneManager.GetActiveScene().name))
                {
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }
        }

        public static void AddTrackedSosig(H3MP_TrackedSosigData trackedSosig, string scene)
        {
            Debug.Log("Received order to add a sosig");
            // Adjust sosigs size to acommodate if necessary
            if (sosigs.Length <= trackedSosig.trackedID)
            {
                IncreaseSosigsSize(trackedSosig.trackedID);
            }

            if (trackedSosig.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the sosig it is because we are the one who sent the sosig to the server
                // We just need to update the tracked ID of the sosig
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].trackedID = trackedSosig.trackedID;

                // Add the sosig to client global list
                sosigs[trackedSosig.trackedID] = H3MP_GameManager.sosigs[trackedSosig.localTrackedID];
            }
            else
            {
                trackedSosig.localTrackedID = -1;

                // Add the sosig to client global list
                sosigs[trackedSosig.trackedID] = trackedSosig;

                // Instantiate sosig if it is in the current scene
                if (scene.Equals(SceneManager.GetActiveScene().name))
                {
                    AnvilManager.Run(trackedSosig.Instantiate());
                }
            }
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
        }

        private static void IncreaseSosigsSize(int minimum)
        {
            int minCapacity = sosigs.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new H3MP_TrackedSosigData[minCapacity];
            for (int i = 0; i < tempSosigs.Length; ++i)
            {
                sosigs[i] = tempSosigs[i];
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
