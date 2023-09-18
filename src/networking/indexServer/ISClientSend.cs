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

        public static void List(string name, int limit, string password)
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
                    packet.Write(password.GetHashCode());
                }
                SendTCPData(packet);
            }
        }
    }
}
