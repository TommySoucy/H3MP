﻿using System.Net.Sockets;
using System;
using System.Net;
using UnityEngine;

namespace H3MP.Networking
{
    public class ISClient
    {
        public enum Packets
        {
            welcome = 0,
            ping = 1,
        }

        public static GameObject managerObject;

        public static int dataBufferSize = 4096;

        public static string IP = "h3mp.tommysoucy.vip";
        public static ushort port = 7861;
        public static int ID = -1;

        public static bool isConnected = false;
        public static bool gotWelcome = false;
        public static int pingAttemptCounter = 0;
        private delegate void PacketHandler(Packet packet);
        private static PacketHandler[] packetHandlers;

        public static TcpClient socket;

        public static NetworkStream stream;
        public static Packet receivedData;
        public static byte[] receiveBuffer;

        public static bool wantListed;
        public static bool listed;

        public static void Connect()
        {

            if (managerObject == null)
            {
                managerObject = new GameObject("ISManagerObject");

                ISThreadManager threadManager = managerObject.AddComponent<ISThreadManager>();

                GameObject.DontDestroyOnLoad(managerObject);
            }

            InitializeClientData();

            isConnected = true;
            gotWelcome = false;
            wantListed = false;
            listed = false;

            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            string actualIP = IP;
            if (Mod.config["ISOverride"] != null)
            {
                actualIP = Dns.GetHostAddresses(Mod.config["ISOverride"].ToString())[0].ToString();
                Mod.LogWarning("ISOverride: " + Mod.config["ISOverride"].ToString()+" resolved to "+ actualIP);
            }
            else
            {
                actualIP = Dns.GetHostAddresses(IP)[0].ToString();
            }
            receiveBuffer = new byte[dataBufferSize];
            Mod.LogInfo("Making connection to IS: " + actualIP + ":" + port, false);
            socket.BeginConnect(actualIP, port, ConnectCallback, socket);
        }

        private static void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public static void SendData(Packet packet, bool overrideWelcome = false)
        {
            if (Client.singleton.gotWelcome || overrideWelcome)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Mod.LogInfo("Error sending data to IS: "+ex.Message, false);
                }
            }
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            // We received bytes through stream
            try
            {
                // Read how many, if none, disconnect
                int byteLength = stream.EndRead(result);
                if (byteLength == 0)
                {
                    ISClient.Disconnect(true, 1);
                    return;
                }

                // If we received some data prepare a data array and fill it up with the data
                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                int handleCode = HandleData(data);
                if (handleCode > 0)
                {
                    receivedData.Reset(handleCode == 2);
                }
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception)
            {
                Disconnect(2);
            }
        }

        private static int HandleData(byte[] data)
        {
            int packetLength = 0;
            bool readLength = false;

            receivedData.SetBytes(data);

            // Handle receiving empty packet, we return true so that receivedData packet gets reset
            if (receivedData.UnreadLength() >= 4)
            {
                packetLength = receivedData.ReadInt();
                readLength = true;
                if (packetLength <= 0)
                {
                    return 2;
                }
            }

            // If we have enough data to read packet length and we got here, it means we have at least some data for a packet
            // This might not be the entire packet, so we check if the length written to packet is less than the data received
            // If so, it means we have enough data to build a packet we can process
            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                readLength = false;
                // So here we take all the data for that new packet
                byte[] packetBytes = receivedData.ReadBytes(packetLength);
                ISThreadManager.ExecuteOnMainThread(() =>
                {
                    if (isConnected)
                    {
                        // Build a packet from it
                        using (Packet packet = new Packet(packetBytes))
                        {
                            // Read its ID, and process it
                            int packetID = packet.ReadInt();

                            packetHandlers[packetID](packet);
                        }
                    }
                });

                packetLength = 0;

                // Check again if we have empty packet, if we do, return true to reset the receivedData
                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    readLength = true;
                    if (packetLength <= 0)
                    {
                        return 2;
                    }
                }
            }

            // If there is no data left to read, means we processed the last byte of data we had as part of the last packet we handled
            // so we can reset the receivedData completely
            if (packetLength == 0 && receivedData.UnreadLength() == 0)
            {
                return 2;
            }

            // If we get here, it is because we have data left to read
            // If we read the length of the packet we didnt end up handling because we are still missing data
            // we want to undo reading 4 bytes, so return 1
            // Otherwise we return 0 indicating we don't want to reset anything, we just want to keep building up data
            return readLength ? 1 : 0;
        }

        private static void Disconnect(int code)
        {
            ISClient.Disconnect(false, code);

            stream = null;
            receiveBuffer = null;
            receivedData = null;
            socket = null;
        }

        private static void InitializeClientData()
        {
            packetHandlers = new PacketHandler[]
            {
                ISClientHandle.Welcome,
                ISClientHandle.Ping,
            };

            Mod.LogInfo("Initialized IS client", false);
        }

        public static void Disconnect(bool sendToServer, int code)
        {
            if (isConnected)
            {
                isConnected = false;

                bool reconnect = false;
                GameManager.reconnectionInstance = -1;
                switch (code)
                {
                    case -1:
                        Mod.LogWarning("Disconnecting from IS due to application quit.");
                        break;
                    case 0:
                        Mod.LogWarning("Disconnecting from IS.");
                        break;
                    case 1:
                        Mod.LogWarning("Connection to IS lost, end of stream. Attempting to reconnect...");
                        GameManager.reconnectionInstance = GameManager.instance;
                        reconnect = true;
                        break;
                    case 2:
                        Mod.LogWarning("Connection to IS lost, TCP forced. Attempting to reconnect...");
                        GameManager.reconnectionInstance = GameManager.instance;
                        reconnect = true;
                        break;
                    case 4:
                        Mod.LogWarning("Connection to IS failed, timed out.");
                        break;
                }

                if (sendToServer)
                {
                    ClientSend.ClientDisconnect();
                }

                // On our side take physical control of everything
                GameManager.TakeAllPhysicalControl(true);

                if (socket != null)
                {
                    socket.Close();
                }
                socket = null;
                if (stream != null)
                {
                    stream.Close();
                }
                stream = null;
                receiveBuffer = null;
                receivedData = null;
                GameObject.Destroy(managerObject);

                ID = -1;

                if (reconnect)
                {
                    // TODO: // Attempt reconnection to IS
                }
            }
        }
    }
}
