using System;

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
    }
}
