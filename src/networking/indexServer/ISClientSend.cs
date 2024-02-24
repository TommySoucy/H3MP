using BepInEx;
using BepInEx.Bootstrap;
using System.Collections.Generic;

namespace H3MP.Networking
{
    public class ISClientSend
    {
        public enum Packets
        {
            welcomeReceived = 0,
            ping = 1,
            requestHostEntries = 2,
            list = 3,
            unlist = 4,
            disconnect = 5,
            join = 6,
            playerCount = 7,
            confirmConnection = 8,
            admin = 9,
            reserved0 = 10,
            reserved1 = 11,
            reserved2 = 12,
            requestModlist = 13,
        }

        public static void SendTCPData(Packet packet, bool custom = false)
        {
            packet.WriteLength();
            ISClient.SendData(packet);
        }

        public static void WelcomeReceived()
        {
            using (Packet packet = new Packet((int)Packets.welcomeReceived))
            {
                packet.Write(Mod.pluginVersion);

                SendTCPData(packet);
            }
        }

        public static void Ping(long time)
        {
            using (Packet packet = new Packet((int)Packets.ping))
            {
                packet.Write(time);
                SendTCPData(packet);
            }
        }

        public static void RequestHostEntries()
        {
            using (Packet packet = new Packet((int)Packets.requestHostEntries))
            {
                SendTCPData(packet);
            }
        }

        public static void List(string name, int limit, string password, ushort port, int modlistEnforcement)
        {
            using (Packet packet = new Packet((int)Packets.list))
            {
                packet.Write(name);
                packet.Write(limit);
                if(password != null && !password.Equals(""))
                {
                    packet.Write(true);
                    packet.Write(Mod.GetSHA256Hash(password));
                }
                else
                {
                    packet.Write(false);
                }
                packet.Write(port);
                packet.Write((byte)modlistEnforcement);
                if (modlistEnforcement != 2)
                {
                    packet.Write(Chainloader.PluginInfos.Count);
                    foreach (KeyValuePair<string, PluginInfo> otherPlugin in Chainloader.PluginInfos)
                    {
                        packet.Write(otherPlugin.Key);
                    }
                }
                SendTCPData(packet);
            }
        }

        public static void Unlist()
        {
            using (Packet packet = new Packet((int)Packets.unlist))
            {
                SendTCPData(packet);
            }
        }

        public static void Disconnect()
        {
            using (Packet packet = new Packet((int)Packets.disconnect))
            {
                SendTCPData(packet);
            }
        }

        public static void Join(int ID, string passwordHash)
        {
            using (Packet packet = new Packet((int)Packets.join))
            {
                packet.Write(ID);
                packet.Write(passwordHash);
                SendTCPData(packet);
            }
        }

        public static void RequestModlist(int ID, string passwordHash)
        {
            using (Packet packet = new Packet((int)Packets.requestModlist))
            {
                packet.Write(ID);
                packet.Write(passwordHash);
                SendTCPData(packet);
            }
        }

        public static void PlayerCount(int count)
        {
            using (Packet packet = new Packet((int)Packets.playerCount))
            {
                packet.Write(count);
                SendTCPData(packet);
            }
        }

        public static void ConfirmConnection(bool valid, int forClient)
        {
            using (Packet packet = new Packet((int)Packets.confirmConnection))
            {
                packet.Write(valid);
                packet.Write(forClient);
                SendTCPData(packet);
            }
        }

        public static void Admin(int key, string p)
        {
            using (Packet packet = new Packet((int)Packets.admin))
            {
                packet.Write(key);
                packet.Write(Mod.GetSHA256Hash(p));
                SendTCPData(packet);
            }
        }
    }
}
