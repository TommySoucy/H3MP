using H3MP.Scripts;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace H3MP.Networking
{
    public class ISClientHandle
    {
        public static void Welcome(Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();
            ServerListController.minimumVersion = packet.ReadString();

            Mod.LogInfo("Message from server: "+msg, false);

            if (Mod.pluginVersion.Equals(ServerListController.minimumVersion))
            {
                ISClient.gotWelcome = true;
                ISClient.ID = ID;
                ISClientSend.WelcomeReceived();

                ISClientSend.RequestHostEntries();
            }
            else
            {
                string[] versionSplit = ServerListController.minimumVersion.Split('.');
                int minimumMajor = int.Parse(versionSplit[0]);
                int minimumMinor = int.Parse(versionSplit[1]);
                int minimumPatch = int.Parse(versionSplit[2]);
                versionSplit = Mod.pluginVersion.Split('.');
                int major = int.Parse(versionSplit[0]);
                int minor = int.Parse(versionSplit[1]);
                int patch = int.Parse(versionSplit[2]);

                if(major > minimumMajor || minor > minimumMinor || patch > minimumPatch)
                {
                    ISClient.gotWelcome = true;
                    ISClient.ID = ID;
                    ISClientSend.WelcomeReceived();

                    ISClientSend.RequestHostEntries();
                }
                else
                {
                    ServerListController.failedConnectionReason = "H3MP version required to use server list: "+ ServerListController.minimumVersion+"+. You need to update. If version not available yet, it will be released soon.";
                    ISClient.Disconnect(true, 5);
                }
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
            for (int i = 0; i < count; ++i) 
            {
                ISEntry newEntry = new ISEntry();
                newEntry.ID = packet.ReadInt();
                newEntry.name = packet.ReadString();
                newEntry.playerCount = packet.ReadInt();
                newEntry.limit = packet.ReadInt();
                newEntry.locked = packet.ReadBool();
                newEntry.modlistEnforcement = packet.ReadByte();
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

        public static void Modlist(Packet packet)
        {
            int entryID = packet.ReadInt();
            bool gotModlist = packet.ReadBool();
            int modCount = packet.ReadInt();
            List<string> modlist = new List<string>();
            for(int i = 0; i < modCount; ++i)
            {
                modlist.Add(packet.ReadString());
            }

            if (ServerListController.modlists.TryGetValue(entryID, out List<string> modlists))
            {
                Mod.LogError("Received modlist for entry " + entryID + " but we already had it? Replacing.");
                ServerListController.modlists[entryID] = modlist;
            }
            else
            {
                ServerListController.modlists.Add(entryID, modlist);
            }

            if (ServerListController.instance != null)
            {
                if (gotModlist)
                {
                    if (ServerListController.instance.state == ServerListController.State.ClientWaiting)
                    {
                        ServerListController.instance.modlist = modlist;
                        ServerListController.instance.SetClientPage(true);
                    }
                }
                else
                {
                    ServerListController.instance.gotEndPoint = false;
                    ServerListController.instance.joiningEntry = -1;
                    if(ServerListController.instance.state == ServerListController.State.ClientWaiting)
                    {
                        ServerListController.instance.SetClientPage(true);
                        ServerListController.instance.clientInfoText.color = Color.red;
                        ServerListController.instance.clientInfoText.text = "Error joining server: Couldn't get modlist";
                    }
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
                        Mod.LogInfo("\n-----------\nEntry: " + packet.ReadInt()+"\nClient: "+packet.ReadInt()+"\nEndPoint: "+packet.ReadString()+"\nName: "+packet.ReadString()+"\nPlayer count: "+packet.ReadInt()+"/"+packet.ReadInt()+"\nHas Password: "+packet.ReadBool());
                    }
                    break;
            }
        }

        public static void Unlisted(Packet packet)
        {
            if(ISClient.listed)
            {
                ISClient.listed = false;
                if(ServerListController.instance != null && ServerListController.instance.state == ServerListController.State.Hosting)
                {
                    ServerListController.instance.hostingListButtonText.color = Color.yellow;
                    ServerListController.instance.hostingListButtonText.text = "Private";
                    ServerListController.instance.hostingListButton.interactable = true;
                }
            }
        }
    }
}
