using System;
using System.Collections.Generic;

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
    }
}
