using RootMotion.FinalIK;

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

        public static void List(string name, int limit, string password, ushort port)
        {
            using (Packet packet = new Packet((int)Packets.list))
            {
                packet.Write(name);
                packet.Write(limit);
                if(password != null && !password.Equals(""))
                {
                    packet.Write(false);
                }
                else
                {
                    packet.Write(true);
                    packet.Write(password.GetDeterministicHashCode());
                }
                packet.Write(port);
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

        public static void Join(int ID, int passwordHash)
        {
            using (Packet packet = new Packet((int)Packets.join))
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
    }
}
