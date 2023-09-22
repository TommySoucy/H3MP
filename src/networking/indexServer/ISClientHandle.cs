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

            if (ServerListController.instance != null && ServerListController.instance.state == ServerListController.State.MainWaiting)
            {
                ServerListController.instance.SetMainPage(null);
            }
        }

        public static void Ping(Packet packet)
        {
            GameManager.ping = Convert.ToInt64((DateTime.Now.ToUniversalTime() - ISThreadManager.epoch).TotalMilliseconds) - packet.ReadLong();
        }

        public static void HostEntries(Packet packet)
        {
            List<ISEntry> entries = new List<ISEntry>();
            int count = packet.ReadInt();
            for(int i =0; i < count; ++i)
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
                    if(ServerListController.instance.state == ServerListController.State.ClientWaiting)
                    {
                        ServerListController.instance.gotEndPoint = true;
                        int byteCount = packet.ReadInt();
                        IPAddress address = new IPAddress(packet.ReadBytes(byteCount));
                        IPEndPoint endPoint = new IPEndPoint(address, packet.ReadInt());

                        ServerListController.instance.SetClientPage(true);
                        Client.punchThrough = true;
                        Mod.OnConnectClicked(endPoint);
                        ThreadManager.pingTimer = ThreadManager.pingTime;
                    }
                    else if (Mod.managerObject != null && ThreadManager.host)
                    {
                        TODO: // Initiate punchthrough attempt
                    }
                }
                else
                {
                    ServerListController.instance.gotEndPoint = false;
                    ServerListController.instance.joiningEntry = -1;
                    ServerListController.instance.SetClientPage(true);
                }
            }
            else
            {
                bool gotEndPoint = packet.ReadBool();
                if (gotEndPoint)
                {
                    if (Mod.managerObject != null && ThreadManager.host)
                    {
                        TODO: // Initiate punchthrough attempt
                    }
                }
            }

        }
    }
}
