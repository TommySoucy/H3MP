using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using static Valve.VR.SteamVR_TrackedObject;

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
                packet.Write(H3MP_GameManager.instance);
                packet.Write(GM.CurrentPlayerBody.GetPlayerIFF());

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

        public static void PlayerIFF(int iff)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerIFF))
            {
                packet.Write(iff);

                SendTCPData(packet);
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

        public static void PlayerInstance(int instance)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playerInstance))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void AddTNHInstance(int hostID, bool letPeopleJoin,
                                          int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                          int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                          int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, int levelIndex)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addTNHInstance))
            {
                packet.Write(hostID);
                packet.Write(letPeopleJoin);
                packet.Write(progressionTypeSetting);
                packet.Write(healthModeSetting);
                packet.Write(equipmentModeSetting);
                packet.Write(targetModeSetting);
                packet.Write(AIDifficultyModifier);
                packet.Write(radarModeModifier);
                packet.Write(itemSpawnerMode);
                packet.Write(backpackMode);
                packet.Write(healthMult);
                packet.Write(sosiggunShakeReloading);
                packet.Write(TNHSeed);
                packet.Write(levelIndex);

                SendTCPData(packet);
            }
        }

        public static void AddInstance()
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addInstance))
            {
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
            while (index < H3MP_GameManager.items.Count)
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

                        if(trackedItem.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        if (trackedItem.Update())
                        {
                            trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedItem, true, false);

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

                            packet.Write(trackedItem, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked items in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    if (count == 0)
                    {
                        break;
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

        public static void TrackedSosigs()
        {
            int index = 0;
            while (index < H3MP_GameManager.sosigs.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedSosigs))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.sosigs.Count; ++i)
                    {
                        H3MP_TrackedSosigData trackedSosig = H3MP_GameManager.sosigs[i];

                        if (trackedSosig.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        if (trackedSosig.Update())
                        {
                            trackedSosig.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                            packet.Write(trackedSosig, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedSosig.insuranceCounter > 0)
                        {
                            --trackedSosig.insuranceCounter;

                            packet.Write(trackedSosig, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    if (count == 0)
                    {
                        break;
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

        public static void TrackedAutoMeaters()
        {
            int index = 0;
            while (index < H3MP_GameManager.autoMeaters.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedAutoMeaters))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.autoMeaters.Count; ++i)
                    {
                        H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_GameManager.autoMeaters[i];

                        if (trackedAutoMeater.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        if (trackedAutoMeater.Update())
                        {
                            trackedAutoMeater.insuranceCounter = H3MP_TrackedAutoMeaterData.insuranceCount;

                            packet.Write(trackedAutoMeater, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked automeaters in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedAutoMeater.insuranceCounter > 0)
                        {
                            --trackedAutoMeater.insuranceCounter;

                            packet.Write(trackedAutoMeater, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked automeaters in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    if (count == 0)
                    {
                        break;
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

        public static void TrackedEncryptions()
        {
            int index = 0;
            while (index < H3MP_GameManager.encryptions.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedEncryptions))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.encryptions.Count; ++i)
                    {
                        H3MP_TrackedEncryptionData trackedEncryption = H3MP_GameManager.encryptions[i];

                        if (trackedEncryption.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        if (trackedEncryption.Update())
                        {
                            trackedEncryption.insuranceCounter = H3MP_TrackedEncryptionData.insuranceCount;

                            packet.Write(trackedEncryption, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }
                        else if(trackedEncryption.insuranceCounter > 0)
                        {
                            --trackedEncryption.insuranceCounter;

                            packet.Write(trackedEncryption, true, false);

                            ++count;

                            // Limit buffer size to MTU, will send next set of tracked Encryptions in separate packet
                            if (packet.buffer.Count >= 1300)
                            {
                                break;
                            }
                        }

                        ++index;
                    }

                    if (count == 0)
                    {
                        break;
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

        public static void TrackedItem(H3MP_TrackedItemData trackedItem)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedItem))
            {
                packet.Write(trackedItem, false, true);

                SendTCPData(packet);
            }
        }

        public static void TrackedSosig(H3MP_TrackedSosigData trackedSosig)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedSosig))
            {
                packet.Write(trackedSosig, false, true);

                SendTCPData(packet);
            }
        }

        public static void TrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedAutoMeater))
            {
                packet.Write(trackedAutoMeater, false, true);

                SendTCPData(packet);
            }
        }

        public static void TrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption)
        {
            using(H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.trackedEncryption))
            {
                packet.Write(trackedEncryption, false, true);

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

        public static void GiveSosigControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveSosigControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void GiveAutoMeaterControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveAutoMeaterControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void GiveEncryptionControl(int trackedID, int newController)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.giveEncryptionControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);

                SendTCPData(packet);
            }
        }

        public static void DestroyItem(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyItem))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void DestroySosig(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroySosig))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void DestroyAutoMeater(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyAutoMeater))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void DestroyEncryption(int trackedID, bool removeFromList = true)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.destroyEncryption))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

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

        public static void WeaponFire(int trackedID, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.weaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void FlintlockWeaponBurnOffOuter(int trackedID, FlintlockBarrel.LoadedElementType[] loadedElementTypes, float[] loadedElementPositions,
                                                       int powderAmount, bool ramRod, float num2, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.flintlockWeaponBurnOffOuter))
            {
                packet.Write(trackedID);
                packet.Write((byte)loadedElementTypes.Length);
                for(int i=0; i< loadedElementTypes.Length; ++i)
                {
                    packet.Write((byte)loadedElementTypes[i]);
                    packet.Write(loadedElementPositions[i]);
                }
                packet.Write(powderAmount);
                packet.Write(ramRod);
                packet.Write(num2);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void FlintlockWeaponFire(int trackedID, FlintlockBarrel.LoadedElementType[] loadedElementTypes, float[] loadedElementPositions,
                                               int[] loadedElementPowderAmounts, bool ramRod, float num5, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.flintlockWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write((byte)loadedElementTypes.Length);
                for(int i=0; i< loadedElementTypes.Length; ++i)
                {
                    packet.Write((byte)loadedElementTypes[i]);
                    packet.Write(loadedElementPositions[i]);
                    packet.Write(loadedElementPowderAmounts[i]);
                }
                packet.Write(ramRod);
                packet.Write(num5);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void BreakActionWeaponFire(int trackedID, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.breakActionWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)barrelIndex);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void DerringerFire(int trackedID, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.derringerFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)barrelIndex);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void LeverActionFirearmFire(int trackedID, FireArmRoundClass roundClass, bool hammer1, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.leverActionFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write(hammer1);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void RevolvingShotgunFire(int trackedID, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.revolvingShotgunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void RevolverFire(int trackedID, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.revolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void SingleActionRevolverFire(int trackedID, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.singleActionRevolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void GrappleGunFire(int trackedID, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.grappleGunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)curChamber);
                if(positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for(int i=0; i<positions.Count;++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void HCBReleaseSled(int trackedID, float cookedAmount, Vector3 position, Vector3 direction)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.HCBReleaseSled))
            {
                packet.Write(trackedID);
                packet.Write(cookedAmount);
                packet.Write(position);
                packet.Write(direction);

                SendTCPData(packet);
            }
        }

        public static void StingerLauncherFire(int trackedID, Vector3 targetPos, Vector3 position, Vector3 direction)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.stingerLauncherFire))
            {
                packet.Write(trackedID);
                packet.Write(targetPos);
                packet.Write(position);
                packet.Write(direction);

                SendTCPData(packet);
            }
        }

        public static void SosigWeaponFire(int trackedID, float recoilMult, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write(recoilMult);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void LAPD2019Fire(int trackedID, int chamberIndex, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.LAPD2019Fire))
            {
                packet.Write(trackedID);
                packet.Write(chamberIndex);
                packet.Write((short)roundClass);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void LAPD2019LoadBattery(int trackedID, int batteryTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.LAPD2019LoadBattery))
            {
                packet.Write(trackedID);
                packet.Write(batteryTrackedID);

                SendTCPData(packet);
            }
        }

        public static void LAPD2019ExtractBattery(int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.LAPD2019ExtractBattery))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void MinigunFire(int trackedID, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.minigunFire))
            {
                packet.Write(trackedID);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)1);
                    packet.Write(positions[0]);
                    packet.Write(directions[0]);
                }

                SendTCPData(packet);
            }
        }

        public static void AttachableFirearmFire(int trackedID, FireArmRoundClass roundClass, bool firedFromInterface, List<Vector3> positions, List<Vector3> directions)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.attachableFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write(firedFromInterface);
                if (positions == null || positions.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)positions.Count);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        packet.Write(positions[i]);
                        packet.Write(directions[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void SosigWeaponShatter(int trackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigWeaponShatter))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterFirearmFireShot(int trackedID, Vector3 angles)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterFireShot))
            {
                packet.Write(trackedID);
                packet.Write(angles);

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

        public static void UberShatterableShatter(int trackedID, Vector3 point, Vector3 dir, float intensity)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.uberShatterableShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);

                SendTCPData(packet);
            }
        }

        public static void SosigPickUpItem(H3MP_TrackedSosig trackedSosig, int itemTrackedID, bool primaryHand)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigPickupItem))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(itemTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigPlaceItemIn(int sosigTrackedID, int slotIndex, int itemTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigPlaceItemIn))
            {
                packet.Write(sosigTrackedID);
                packet.Write(itemTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigDropSlot(int sosigTrackedID, int slotIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDropSlot))
            {
                packet.Write(sosigTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigHandDrop(int sosigTrackedID, bool primaryHand)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigHandDrop))
            {
                packet.Write(sosigTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigConfigure(int sosigTrackedID, SosigConfigTemplate config)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigConfigure))
            {
                packet.Write(sosigTrackedID);
                packet.Write(config);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDeRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkDeRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetIFF(int trackedSosigID, int IFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigSetOriginalIFF(int trackedSosigID, int IFF)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetOriginalIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDamage(int trackedSosigID, int linkIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.sosigLinkDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void SosigWearableDamage(int trackedSosigID, int linkIndex, int wearableIndex, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigWearableDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)wearableIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigWearableDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.sosigWearableDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void SosigDamageData(H3MP_TrackedSosig trackedSosig)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDamageData))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(trackedSosig.physicalSosigScript.IsStunned);
                packet.Write(trackedSosig.physicalSosigScript.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosigScript.BodyState);
                packet.Write((bool)Mod.Sosig_m_isOnOffMeshLinkField.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosigScript.Agent.enabled);
                List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(trackedSosig.physicalSosigScript);
                packet.Write((byte)joints.Count);
                for(int i=0; i < joints.Count; ++i)
                {
                    if (joints[i] != null)
                    {
                        packet.Write(joints[i].lowTwistLimit.limit);
                        packet.Write(joints[i].highTwistLimit.limit);
                        packet.Write(joints[i].swing1Limit.limit);
                        packet.Write(joints[i].swing2Limit.limit);
                    }
                    else
                    {
                        packet.Write(0);
                        packet.Write(0);
                        packet.Write(0);
                        packet.Write(0);
                    }
                }
                packet.Write((bool)Mod.Sosig_m_isCountingDownToStagger.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_staggerAmountToApply.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((bool)Mod.Sosig_m_recoveringFromBallisticState.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticLerp.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_tickDownToWrithe.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((float)Mod.Sosig_m_recoveryFromBallisticTick.GetValue(trackedSosig.physicalSosigScript));
                packet.Write((byte)trackedSosig.physicalSosigScript.GetDiedFromIFF());
                packet.Write((byte)trackedSosig.physicalSosigScript.GetDiedFromClass());
                packet.Write(trackedSosig.physicalSosigScript.IsBlinded);
                packet.Write((float)Mod.Sosig_m_blindTime.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.IsFrozen);
                packet.Write((float)Mod.Sosig_m_debuffTime_Freeze.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.GetDiedFromHeadShot());
                packet.Write((float)Mod.Sosig_m_timeSinceLastDamage.GetValue(trackedSosig.physicalSosigScript));
                packet.Write(trackedSosig.physicalSosigScript.IsConfused);
                packet.Write(trackedSosig.physicalSosigScript.m_confusedTime);
                packet.Write((float)Mod.Sosig_m_storedShudder.GetValue(trackedSosig.physicalSosigScript));

                SendTCPData(packet);
            }
        }

        public static void EncryptionDamageData(H3MP_TrackedEncryption trackedEncryption)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionDamageData))
            {
                packet.Write(trackedEncryption.data.trackedID);
                packet.Write((int)Mod.TNH_EncryptionTarget_m_numHitsLeft.GetValue(trackedEncryption.physicalEncryptionScript));

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamageData(int trackedID, AutoMeaterHitZone hitZone)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterHitZoneDamageData))
            {
                packet.Write(trackedID);
                packet.Write((byte)hitZone.Type);

                packet.Write(hitZone.ArmorThreshold);
                packet.Write(hitZone.LifeUntilFailure);
                packet.Write((bool)Mod.AutoMeaterHitZone_m_isDestroyed.GetValue(hitZone));

                SendTCPData(packet);
            }
        }

        public static void SosigLinkExplodes(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkExplodes))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);

                SendTCPData(packet);
            }
        }

        public static void SosigDies(int sosigTrackedID, Damage.DamageClass damClass, Sosig.SosigDeathType deathType)
        {
            Mod.LogInfo("Client sending sosig " + sosigTrackedID + " dies");
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigDies))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)damClass);
                packet.Write((byte)deathType);

                SendTCPData(packet);
            }
        }

        public static void SosigClear(int sosigTrackedID)
        {
            Mod.LogInfo("Client sending sosig " + sosigTrackedID + " clear");
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigClear))
            {
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetBodyState(int sosigTrackedID, Sosig.SosigBodyState s)
        {
            Mod.LogInfo("Client sending sosig " + sosigTrackedID + " body state " + s);
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetBodyState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)s);

                SendTCPData(packet);
            }
        }

        public static void PlaySosigFootStepSound(int sosigTrackedID, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.playSosigFootStepSound))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)audioType);
                packet.Write(position);
                packet.Write(vol);
                packet.Write(pitch);
                packet.Write(delay);

                SendTCPData(packet);
            }
        }

        public static void SosigSpeakState(int sosigTrackedID, Sosig.SosigOrder currentOrder)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSpeakState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPData(packet);
            }
        }

        public static void SosigSetCurrentOrder(int sosigTrackedID, Sosig.SosigOrder currentOrder)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigSetCurrentOrder))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPData(packet);
            }
        }

        public static void SosigVaporize(int sosigTrackedID, int iff)
        {
            Mod.LogInfo("Client sending sosig " + sosigTrackedID + " vaporize");
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigVaporize))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)iff);

                SendTCPData(packet);
            }
        }

        public static void SosigRequestHitDecal(int sosigTrackedID, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, int linkIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigRequestHitDecal))
            {
                packet.Write(sosigTrackedID);
                packet.Write(point);
                packet.Write(normal);
                packet.Write(edgeNormal);
                packet.Write(scale);
                packet.Write((byte)linkIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkBreak(int sosigTrackedID, int linkIndex, bool isStart, Damage.DamageClass damClass)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkBreak))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write(isStart);
                packet.Write((byte)damClass);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkSever(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass, bool isPullApart)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigLinkSever))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);
                packet.Write(isPullApart);

                SendTCPData(packet);
            }
        }

        public static void UpToDateObjects(bool instantiateOnReceive, int forClient)
        {
            int index = 0;
            while (index < H3MP_GameManager.items.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateItemRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);
                    packet.Write(instantiateOnReceive);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.items.Count; ++i)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_GameManager.items[i];

                        if(trackedItem.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        trackedItem.insuranceCounter = H3MP_TrackedItemData.insuranceCount;

                        trackedItem.Update(true);
                        packet.Write(trackedItem, false, true);

                        ++count;

                        ++index;

                        // Limit buffer size to MTU, will send next set of tracked items in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
            index = 0;
            while (index < H3MP_GameManager.sosigs.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateSosigRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);
                    packet.Write(instantiateOnReceive);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.sosigs.Count; ++i)
                    {
                        H3MP_TrackedSosigData trackedSosig = H3MP_GameManager.sosigs[i];

                        if (trackedSosig.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        trackedSosig.insuranceCounter = H3MP_TrackedSosigData.insuranceCount;

                        trackedSosig.Update(true);
                        packet.Write(trackedSosig, false, true);

                        ++count;

                        ++index;

                        // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
            index = 0;
            while (index < H3MP_GameManager.autoMeaters.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateAutoMeatersRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);
                    packet.Write(instantiateOnReceive);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.autoMeaters.Count; ++i)
                    {
                        H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_GameManager.autoMeaters[i];

                        if (trackedAutoMeater.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        trackedAutoMeater.insuranceCounter = H3MP_TrackedAutoMeaterData.insuranceCount;

                        trackedAutoMeater.Update(true);
                        packet.Write(trackedAutoMeater, false, true);

                        ++count;

                        ++index;

                        // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }
            index = 0;
            while (index < H3MP_GameManager.encryptions.Count)
            {
                using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.updateEncryptionsRequest))
                {
                    // Write place holder int at start to hold the count once we know it
                    int countPos = packet.buffer.Count;
                    packet.Write((short)0);
                    packet.Write(instantiateOnReceive);

                    short count = 0;
                    for (int i = index; i < H3MP_GameManager.encryptions.Count; ++i)
                    {
                        H3MP_TrackedEncryptionData trackedEncryption = H3MP_GameManager.encryptions[i];

                        if (trackedEncryption.trackedID == -1)
                        {
                            ++index;
                            continue;
                        }

                        trackedEncryption.insuranceCounter = H3MP_TrackedEncryptionData.insuranceCount;

                        trackedEncryption.Update(true);
                        packet.Write(trackedEncryption, false, true);

                        ++count;

                        ++index;

                        // Limit buffer size to MTU, will send next set of tracked sosigs in separate packet
                        if (packet.buffer.Count >= 1300)
                        {
                            break;
                        }
                    }

                    // Write the count to packet
                    byte[] countArr = BitConverter.GetBytes(count);
                    for (int i = countPos, j = 0; i < countPos + 2; ++i, ++j)
                    {
                        packet.buffer[i] = countArr[j];
                    }

                    SendTCPData(packet);
                }
            }

            DoneSendingUpdaToDateObjects(forClient);
        }

        public static void DoneLoadingScene()
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.DoneLoadingScene))
            {
                SendTCPData(packet);
            }
        }

        public static void DoneSendingUpdaToDateObjects(int forClient)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.DoneSendingUpdaToDateObjects))
            {
                packet.Write(forClient);

                SendTCPData(packet);
            }
        }

        public static void AddTNHCurrentlyPlaying(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.addTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.removeTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHProgression(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHProgression))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHEquipment(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHEquipment))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHHealthMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHTargetMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHTargetMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHAIDifficulty(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHAIDifficulty))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHRadarMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHRadarMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHItemSpawnerMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHItemSpawnerMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHBackpackMode(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHBackpackMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMult(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHHealthMult))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSosigGunReload(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHSosigGunReload))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSeed(int i, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHSeed))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHLevelIndex(int levelIndex, int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHLevelIndex))
            {
                packet.Write(levelIndex);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHController(int instance, int ID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.setTNHController))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHData(int instance, TNH_Manager manager)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHData))
            {
                packet.Write(instance);
                packet.Write(manager);

                SendTCPData(packet);
            }
        }

        public static void TNHData(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.TNHData);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void TNHPlayerDied(int instance, int ID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHPlayerDied))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHAddTokens(int instance, int amount)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHAddTokens))
            {
                packet.Write(instance);
                packet.Write(amount);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetState(int trackedID, byte state)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterSetState))
            {
                packet.Write(trackedID);
                packet.Write(state);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetBladesActive(int trackedID, bool active)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterSetBladesActive))
            {
                packet.Write(trackedID);
                packet.Write(active);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.autoMeaterDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void AutoMeaterFirearmFireAtWill(int trackedID, int firearmIndex, bool fireAtWill, float dist)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterFirearmFireAtWill))
            {
                packet.Write(trackedID);
                packet.Write(firearmIndex);
                packet.Write(fireAtWill);
                packet.Write(dist);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(int trackedID, AutoMeater.AMHitZoneType type, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.autoMeaterHitZoneDamage))
            {
                packet.Write(trackedID);
                packet.Write((byte)type);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.autoMeaterHitZoneDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void EncryptionDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void EncryptionDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.encryptionDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void TNHSosigKill(int instance, int sosigTrackedID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSosigKill))
            {
                packet.Write(instance);
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointSystemNode(int instance, int charIndex, int progressionIndex, int progressionEndlessIndex, int levelIndex, int holdPointIndex)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointSystemNode))
            {
                packet.Write(instance);
                packet.Write(charIndex);
                packet.Write(progressionIndex);
                packet.Write(progressionEndlessIndex);
                packet.Write(levelIndex);
                packet.Write(holdPointIndex);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldBeginChallenge(int instance, bool controller)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldBeginChallenge))
            {
                packet.Write(instance);
                packet.Write(controller);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointRaiseBarriers(int instance, List<int> barrierIndices, List<int> barrierPrefabIndices)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointRaiseBarriers))
            {
                packet.Write(instance);
                if(barrierIndices == null || barrierIndices.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(barrierIndices.Count);
                    for (int i = 0; i < barrierIndices.Count; i++)
                    {
                        packet.Write(barrierIndices[i]);
                    }
                    for (int i = 0; i < barrierIndices.Count; i++)
                    {
                        packet.Write(barrierPrefabIndices[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void ShatterableCrateDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.shatterableCrateDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void ShatterableCrateDamage(H3MP_Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.shatterableCrateDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void ShatterableCrateDestroy(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.shatterableCrateDestroy))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void TNHSetLevel(int instance, int level)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSetLevel))
            {
                packet.Write(instance);
                packet.Write(level);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhaseTake(int instance, List<int> activeSupplyPointIndices, List<TNH_SupplyPoint.SupplyPanelType> types)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSetPhaseTake))
            {
                packet.Write(instance);
                packet.Write(activeSupplyPointIndices.Count);
                foreach(int index in activeSupplyPointIndices)
                {
                    packet.Write(index);
                }
                foreach (TNH_SupplyPoint.SupplyPanelType type in types)
                {
                    packet.Write((byte)type);
                }

                SendTCPData(packet);
            }
        }

        public static void TNHHoldCompletePhase(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldCompletePhase))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointFailOut(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointFailOut))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointBeginPhase(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointBeginPhase))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointCompleteHold(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointCompleteHold))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhaseComplete(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSetPhaseComplete))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhase(int instance, TNH_Phase p)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHSetPhase))
            {
                packet.Write(instance);
                packet.Write((short)p);

                SendTCPData(packet);
            }
        }

        public static void EncryptionRespawnSubTarg(int trackedID, int index)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionRespawnSubTarg))
            {
                packet.Write(trackedID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void EncryptionSpawnGrowth(int trackedID, int index, Vector3 point)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionSpawnGrowth))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);

                SendTCPData(packet);
            }
        }

        public static void EncryptionRecursiveInit(int trackedID, List<int> indices)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionRecursiveInit))
            {
                packet.Write(trackedID);
                if(indices == null || indices.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(indices.Count);
                    foreach(int index in indices)
                    {
                        packet.Write(index);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void EncryptionResetGrowth(int trackedID, int index, Vector3 point)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionResetGrowth))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);

                SendTCPData(packet);
            }
        }

        public static void EncryptionDisableSubtarg(int trackedID, int index)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionDisableSubtarg))
            {
                packet.Write(trackedID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void EncryptionSubDamage(int trackedID, int index, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.encryptionSubDamage))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigWeaponDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigWeaponDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void RemoteMissileDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.remoteMissileDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void StingerMissileDamage(int trackedID, Damage d)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.stingerMissileDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int instance, List<Vector3> data, float tickDownToID)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldPointBeginAnalyzing))
            {
                packet.Write(instance);
                packet.Write(tickDownToID);
                if(data == null || data.Count == 0)
                {
                    packet.Write((byte)0);
                }
                else
                {
                    packet.Write((byte)data.Count);
                    foreach(Vector3 dataEntry in data)
                    {
                        packet.Write(dataEntry);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void TNHHoldIdentifyEncryption(int instance)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.TNHHoldIdentifyEncryption))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SosigPriorityIFFChart(int trackedID, int chart)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.sosigPriorityIFFChart))
            {
                packet.Write(trackedID);
                packet.Write(chart);

                SendTCPData(packet);
            }
        }

        public static void RemoteMissileDetonate(int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.remoteMissileDetonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void StingerMissileExplode(int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.stingerMissileExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void PinnedGrenadeExplode(int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.pinnedGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void FVRGrenadeExplode(int trackedID, Vector3 pos)
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.FVRGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void ClientDisconnect()
        {
            using (H3MP_Packet packet = new H3MP_Packet((int)ClientPackets.clientDisconnect))
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                packet.Write(Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalMilliseconds));

                SendTCPData(packet);
            }
        }
    }
}
