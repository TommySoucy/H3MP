using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP
{
    internal class H3MP_ClientSend
    {
        private static void SendTCPData(H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Client.singleton.tcp.SendData(packet);
        }

        private static void SendUDPData(H3MP_Packet packet)
        {
            packet.WriteLength();
            H3MP_Client.singleton.udp.SendData(packet);
        }

        public static void WelcomeReceived()
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.welcomeReceived))
            {
                packet.Write(H3MP_Client.singleton.ID);
                packet.Write(Mod.config["Username"].ToString());
                packet.Write(SceneManager.GetActiveScene().name);

                SendTCPData(packet);
            }
        }

        public static void PlayerState(Vector3 playerPos, Quaternion playerRot, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot,
                                       Vector3 rightHandPos, Quaternion rightHandRot,
                                       float health, int maxHealth)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerState))
            {
                packet.Write(playerPos);
                packet.Write(playerRot);
                packet.Write(headPos);
                packet.Write(headRot);
                packet.Write(torsoPos);
                packet.Write(torsoRot);
                packet.Write(leftHandPos);
                packet.Write(leftHandRot);
                packet.Write(rightHandPos);
                packet.Write(rightHandRot);
                packet.Write(health);
                packet.Write(maxHealth);
                byte[] additionalData = H3MP_GameManager.playerStateAddtionalDataSize == -1 ? null : new byte[H3MP_GameManager.playerStateAddtionalDataSize];
                H3MP_GameManager.WriteAdditionalPlayerState(additionalData);
                if(additionalData != null && additionalData.Length > 0)
                {
                    H3MP_GameManager.playerStateAddtionalDataSize = additionalData.Length;
                    packet.Write((short)additionalData.Length);
                    packet.Write(additionalData);
                }
                else
                {
                    H3MP_GameManager.playerStateAddtionalDataSize = 0;
                    packet.Write((short)0);
                }

                SendUDPData(packet);
            }
        }

        public static void PlayerScene(string sceneName)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerScene))
            {
                packet.Write(sceneName);

                SendTCPData(packet);
            }
        }

        // MOD: This is what a mod that adds a scene it wants to sync would call to sync players and items inside it
        public static void AddSyncScene(string sceneName)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addSyncScene))
            {
                packet.Write(sceneName);

                SendTCPData(packet);
            }
        }

        public static void TrackedItems()
        {
            int index = 0;
            while (index < H3MP_GameManager.items.Count - 1)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedItems))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.items.Count; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_GameManager.items[i];
                        if (trackedItem.Update())
                        {
                            trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedItem);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedItem.insuranceCounter > 0)
                        {
                            --trackedItem.insuranceCounter;

                            packet.Write(trackedItem);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        index = i;
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendUDPData(packet);
                }
            }
        }

        public static void TrackedItem(H3MP_TrackedItemData trackedItem, string scene)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedItem))
            {
                packet.Write(trackedItem, true);
                packet.Write(scene);

                SendTCPData(packet);
            }
        }

        public static void GiveControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void DestroyItem(int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyItem))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void ItemParent(int trackedID, int newParentID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.itemParent))
            {
                packet.Write(trackedID);
                packet.Write(newParentID);

                SendTCPData(packet);
            }
        }

        public static void WeaponFire(int trackedID)
        {
            // TODO: It may be necessary to also pass the round class 
            //       This is because when a weapon that is controlled by this client in fired in aanother client, it will use up the projectile in its chamber (if it has one)
            //       The problem is that if it isn't up to date before it gets fired again, the chamber is now empty but we are still calling fire
            //       This will either cause error or just not fire the weapon. If we pass the round class, we can ensure that our chamber contains the correct
            //       class, if anything, before firing
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.weaponFire))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void PlayerDamage(int clientID, byte part, Damage damage)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerDamage))
            {
                packet.Write(clientID);
                packet.Write(part);
                packet.Write(damage);

                SendTCPData(packet);
            }
        }
    }
}
