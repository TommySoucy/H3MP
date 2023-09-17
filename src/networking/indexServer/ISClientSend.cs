namespace H3MP.Networking
{
    public class ISClientSend
    {
        public enum Packets
        {
            welcomeReceived = 0,
            ping = 1,
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
    }
}
