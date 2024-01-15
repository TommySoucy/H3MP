using H3MP.Scripts;
using System;
using System.Collections.Generic;
using System.Net;

namespace H3MP.Networking
{
    public class ISClientHandle
    {
        public static void Welcome(Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();

            Mod.LogInfo("Message from server: "+msg, false);

            ISClient.gotWelcome = true;
            ISClient.ID = ID;
            ISClientSend.WelcomeReceived();

            ISClientSend.RequestHostEntries();
        }

        public static void Ping(Packet packet)
        {
            GameManager.ping = Convert.ToInt64((DateTime.Now.ToUniversalTime() - ISThreadManager.epoch).TotalMilliseconds) - packet.ReadLong();
        }

        public static void HostEntries(Packet packet)
        {
            List<ISEntry> entries = new List<ISEntry>();
            int count = packet.ReadInt();
            for (int i = 0; i < count; ++i) 
            {
                ISEntry newEntry = new ISEntry();
                newEntry.ID = packet.ReadInt();
                newEntry.name = packet.ReadString();
                newEntry.playerCount = packet.ReadInt();
                newEntry.limit = packet.ReadInt();
                newEntry.locked = packet.ReadBool();
                entries.Add(newEntry);
            }
            ISClient.OnReceiveHostEntriesInvoke(entries);
        }

        public static void Listed(Packet packet)
        {
            int listedID = packet.ReadInt();

            ISClient.OnListedInvoke(listedID);
        }

        public static void Connect(Packet packet)
        {
            if(ServerListController.instance != null)
            {
                bool gotEndPoint = packet.ReadBool();
                if (gotEndPoint)
                {
                    ServerListController.instance.gotEndPoint = true;
                    int byteCount = packet.ReadInt();
                    IPAddress address = new IPAddress(packet.ReadBytes(byteCount));
                    IPEndPoint endPoint = new IPEndPoint(address, packet.ReadInt());

                    if (ServerListController.instance.state == ServerListController.State.ClientWaiting)
                    {
                        ServerListController.instance.SetClientPage(true);
                        Mod.OnConnectClicked(endPoint);
                        ThreadManager.pingTimer = ThreadManager.pingTime;
                    }
                }
                else
                {
                    ServerListController.instance.gotEndPoint = false;
                    ServerListController.instance.joiningEntry = -1;
                    ServerListController.instance.SetClientPage(true);
                }
            }
        }

        public static void ConfirmConnection(Packet packet)
        {
            int forClient = packet.ReadInt();

            ISClientSend.ConfirmConnection(Mod.managerObject != null && ThreadManager.host && ISClient.isConnected && ISClient.listed && GameManager.players.Count < Server.maxClientCount, forClient);
        }

        public static void Admin(Packet packet)
        {
            int key = packet.ReadInt();

            switch (key)
            {
                case 0: // Client data
                    Mod.LogInfo("Received IS admin 0: Clients: "+packet.ReadInt());
                    break;
                case 1: // Host entry data
                    int hostEntryCount = packet.ReadInt();
                    Mod.LogInfo("Received IS admin 1: Host entries: " + hostEntryCount);
                    for(int i=0; i< hostEntryCount; ++i)
                    {
                        Mod.LogInfo("-----------\nEntry: " + packet.ReadInt()+"\nClient: "+packet.ReadInt()+"\nEndPoint: "+packet.ReadString()+"\nName: "+packet.ReadString()+"\nPlayer count: "+packet.ReadInt()+"/"+packet.ReadInt());
                    }
                    break;
            }
        }
    }
}
