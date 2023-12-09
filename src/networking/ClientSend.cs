using FistVR;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Networking
{
    public class ClientSend
    {
        // TODO: Optimization: Use array instead of dictionry, packet ID would be the position in the array
        //                     and used instead of a key to find our packet. A simple Queue<int> would be used to 
        //                     keep track of exactly which IDs we need to batch
        public static Dictionary<object, byte[]> queuedPackets = new Dictionary<object, byte[]>();
        
        private static void ConvertToCustomID(Packet packet)
        {
            byte[] convertedID = BitConverter.GetBytes(BitConverter.ToInt32(new byte[] { packet.buffer[0], packet.buffer[1], packet.buffer[2], packet.buffer[3] }, 0) * -1 - 2);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = convertedID[i];
            }
        }

        public static void SendTCPData(Packet packet, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendTCPData: " + BitConverter.ToInt32(packet.ToArray(), 0));
            }
#endif
            packet.WriteLength();
            Client.singleton.tcp.SendData(packet);
        }

        public static void SendUDPData(Packet packet, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("SendUDPData: " + BitConverter.ToInt32(packet.ToArray(), 0)+" legnth "+packet.buffer.Count);
            }
#endif
            packet.WriteLength();
            Client.singleton.udp.SendData(packet);
        }

        public static void SendUDPDataBatched(Packet packet, object key = null, bool custom = false)
        {
            if (custom)
            {
                ConvertToCustomID(packet);
            }

            if (key == null)
            {
                key = new object();
            }
            packet.WriteLength();

            lock (queuedPackets)
            {
                queuedPackets[key] = packet.ToArray();
            }
        }

        public static void SendBatchedPackets()
        {
            List<byte[]> packetsToSend = new List<byte[]>();
            lock (queuedPackets)
            {
                packetsToSend.AddRange(queuedPackets.Values);
                queuedPackets.Clear();
            }

            const int mtu = 1300;
            
            Packet batchedPacket = new Packet((int) ClientPackets.batchedPacket);
            
            foreach (byte[] packetData in packetsToSend)
            {

                // If the data of this packet would put us over the MTU, send what we have now
                int curLength = batchedPacket.Length();
                if (curLength > 0 && curLength + packetData.Length > mtu)
                {
                    batchedPacket.WriteLength();
                    Client.singleton.udp.SendData(batchedPacket);
                    batchedPacket.Dispose();
                    batchedPacket = new Packet((int)ClientPackets.batchedPacket);
                }
                
                // Then add the data to the batch
                batchedPacket.Write(packetData);
            }

            // Send the remaining data
            if (batchedPacket.Length() > 0)
            {
                batchedPacket.WriteLength();
                Client.singleton.udp.SendData(batchedPacket);
            }
            batchedPacket.Dispose();
        }
        
        public static void WelcomeReceived()
        {
            using(Packet packet = new Packet((int)ClientPackets.welcomeReceived))
            {
                packet.Write(Client.singleton.ID);
                packet.Write(Mod.config["Username"].ToString());
                packet.Write(GameManager.scene);
                packet.Write(GameManager.instance);
                packet.Write(GM.CurrentPlayerBody.GetPlayerIFF());
                packet.Write(GameManager.colorIndex);

                SendTCPData(packet);
            }
        }

        public static void Ping(long time)
        {
            using (Packet packet = new Packet((int)ClientPackets.ping))
            {
                packet.Write(time);
                SendTCPData(packet);
            }
        }

        public static void PlayerState(Vector3 playerPos, Quaternion playerRot, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                       Vector3 leftHandPos, Quaternion leftHandRot,
                                       Vector3 rightHandPos, Quaternion rightHandRot,
                                       float health, int maxHealth)
        {
            using(Packet packet = new Packet((int)ClientPackets.playerState))
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
                byte[] additionalData = GameManager.playerStateAddtionalDataSize == -1 ? null : new byte[GameManager.playerStateAddtionalDataSize];
                if(additionalData != null && additionalData.Length > 0)
                {
                    GameManager.playerStateAddtionalDataSize = additionalData.Length;
                    packet.Write((short)additionalData.Length);
                    packet.Write(additionalData);
                }
                else
                {
                    GameManager.playerStateAddtionalDataSize = 0;
                    packet.Write((short)0);
                }

                SendUDPDataBatched(packet, GM.CurrentPlayerBody);
            }
        }

        public static void PlayerIFF(int iff)
        {
            using(Packet packet = new Packet((int)ClientPackets.playerIFF))
            {
                packet.Write(iff);

                SendTCPData(packet);
            }
        }

        public static void PlayerScene(string sceneName)
        {
            using(Packet packet = new Packet((int)ClientPackets.playerScene))
            {
                packet.Write(sceneName);

                SendTCPData(packet);
            }
        }

        public static void PlayerInstance(int instance, bool sceneLoading)
        {
            using(Packet packet = new Packet((int)ClientPackets.playerInstance))
            {
                packet.Write(instance);
                packet.Write(sceneLoading);

                SendTCPData(packet);
            }
        }

        public static void AddTNHInstance(int hostID, bool letPeopleJoin,
                                          int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                          int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                          int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, string levelID)
        {
            using(Packet packet = new Packet((int)ClientPackets.addTNHInstance))
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
                packet.Write(levelID);

                SendTCPData(packet);
            }
        }

        public static void AddInstance()
        {
            using(Packet packet = new Packet((int)ClientPackets.addInstance))
            {
                SendTCPData(packet);
            }
        }

        public static void TrackedObjects()
        {
            for(int i=0; i<GameManager.objects.Count;++i)
            {
                TrackedObjectData trackedObject = GameManager.objects[i];

                if (trackedObject.trackedID == -1)
                {
                    continue;
                }

                if (trackedObject.Update())
                {
                    trackedObject.latestUpdateSent = false;

                    using (Packet packet = new Packet((int)ClientPackets.trackedObjects))
                    {
                        // Write packet
                        trackedObject.WriteToPacket(packet, true, false);
                        
                        if (packet.buffer.Count >= 1500)
                        {
                            Mod.LogWarning("Update packet size for "+trackedObject.trackedID+" of type: "+trackedObject.typeIdentifier+" is above 1500 bytes");
                        }

                        SendUDPDataBatched(packet, trackedObject);
                    }
                }
                else if (!trackedObject.latestUpdateSent)
                {
                    trackedObject.latestUpdateSent = true;

                    // Send latest update on its own
                    ObjectUpdate(trackedObject);
                }
            }
        }

        public static void ObjectUpdate(TrackedObjectData objectData)
        {
            using (Packet packet = new Packet((int)ClientPackets.objectUpdate))
            {
                objectData.WriteToPacket(packet, true, false);

                SendTCPData(packet);
            }
        }

        public static void TrackedObject(TrackedObjectData trackedObject)
        {
            using(Packet packet = new Packet((int)ClientPackets.trackedObject))
            {
                trackedObject.WriteToPacket(packet, false, true);

                SendTCPData(packet);
            }
        }

        public static void GiveObjectControl(int trackedID, int newController, List<int> debounce)
        {
            using (Packet packet = new Packet((int)ClientPackets.giveObjectControl))
            {
                packet.Write(trackedID);
                packet.Write(newController);
                if(debounce == null || debounce.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for(int i=0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void DestroyObject(int trackedID, bool removeFromList = true)
        {
            using (Packet packet = new Packet((int)ClientPackets.destroyObject))
            {
                packet.Write(trackedID);
                packet.Write(removeFromList);

                SendTCPData(packet);
            }
        }

        public static void ObjectParent(int trackedID, int newParentID)
        {
            using (Packet packet = new Packet((int)ClientPackets.objectParent))
            {
                packet.Write(trackedID);
                packet.Write(newParentID);

                SendTCPData(packet);
            }
        }

        public static void WeaponFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions, int chamberIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.weaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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
                packet.Write(chamberIndex);

                SendTCPData(packet);
            }
        }

        public static void FlintlockWeaponBurnOffOuter(int trackedID, FlintlockBarrel.LoadedElementType[] loadedElementTypes, float[] loadedElementPositions,
                                                       int powderAmount, bool ramRod, float num2, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.flintlockWeaponBurnOffOuter))
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
            using (Packet packet = new Packet((int)ClientPackets.flintlockWeaponFire))
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

        public static void BreakActionWeaponFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.breakActionWeaponFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void DerringerFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int barrelIndex, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.derringerFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void LeverActionFirearmFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, bool hammer1, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.leverActionFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void RevolvingShotgunFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.revolvingShotgunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void RevolverFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.revolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void SingleActionRevolverFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.singleActionRevolverFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void GrappleGunFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, int curChamber, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.grappleGunFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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
            using (Packet packet = new Packet((int)ClientPackets.HCBReleaseSled))
            {
                packet.Write(trackedID);
                packet.Write(cookedAmount);
                packet.Write(position);
                packet.Write(direction);

                SendTCPData(packet);
            }
        }

        public static void StingerLauncherFire(int trackedID, Vector3 targetPos, Vector3 position, Quaternion rotation)
        {
            using (Packet packet = new Packet((int)ClientPackets.stingerLauncherFire))
            {
                packet.Write(trackedID);
                packet.Write(targetPos);
                packet.Write(position);
                packet.Write(rotation);

                SendTCPData(packet);
            }
        }

        public static void SosigWeaponFire(int trackedID, float recoilMult, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigWeaponFire))
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

        public static void LAPD2019Fire(int trackedID, int chamberIndex, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.LAPD2019Fire))
            {
                packet.Write(trackedID);
                packet.Write(chamberIndex);
                packet.Write((short)roundType);
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
            using (Packet packet = new Packet((int)ClientPackets.LAPD2019LoadBattery))
            {
                packet.Write(trackedID);
                packet.Write(batteryTrackedID);

                SendTCPData(packet);
            }
        }

        public static void LAPD2019ExtractBattery(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.LAPD2019ExtractBattery))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void AttachableFirearmFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, bool firedFromInterface, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.attachableFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void IntegratedFirearmFire(int trackedID, FireArmRoundType roundType, FireArmRoundClass roundClass, List<Vector3> positions, List<Vector3> directions)
        {
            using (Packet packet = new Packet((int)ClientPackets.integratedFirearmFire))
            {
                packet.Write(trackedID);
                packet.Write((short)roundType);
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

        public static void SosigWeaponShatter(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigWeaponShatter))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterFirearmFireShot(int trackedID, Vector3 angles)
        {
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterFireShot))
            {
                packet.Write(trackedID);
                packet.Write(angles);

                SendTCPData(packet);
            }
        }

        public static void PlayerDamage(int clientID, float damageMult, bool head, Damage damage)
        {
            using (Packet packet = new Packet((int)ClientPackets.playerDamage))
            {
                packet.Write(clientID);
                packet.Write(damageMult);
                packet.Write(head);
                packet.Write(damage);

                SendTCPData(packet);
            }
        }

        public static void UberShatterableShatter(int trackedID, Vector3 point, Vector3 dir, float intensity, byte[] data)
        {
            using (Packet packet = new Packet((int)ClientPackets.uberShatterableShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);
                if(data == null)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(data.Length);
                    packet.Write(data);
                }

                SendTCPData(packet);
            }
        }

        public static void SosigPickUpItem(TrackedSosig trackedSosig, int itemTrackedID, bool primaryHand)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigPickupItem))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(itemTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigPlaceItemIn(int sosigTrackedID, int slotIndex, int itemTrackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigPlaceItemIn))
            {
                packet.Write(sosigTrackedID);
                packet.Write(itemTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigDropSlot(int sosigTrackedID, int slotIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigDropSlot))
            {
                packet.Write(sosigTrackedID);
                packet.Write(slotIndex);

                SendTCPData(packet);
            }
        }

        public static void SosigHandDrop(int sosigTrackedID, bool primaryHand)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigHandDrop))
            {
                packet.Write(sosigTrackedID);
                packet.Write(primaryHand);

                SendTCPData(packet);
            }
        }

        public static void SosigConfigure(int sosigTrackedID, SosigConfigTemplate config)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigConfigure))
            {
                packet.Write(sosigTrackedID);
                packet.Write(config);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDeRegisterWearable(int trackedSosigID, int linkIndex, string itemID)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkDeRegisterWearable))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(itemID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetIFF(int trackedSosigID, int IFF)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigSetIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigSetOriginalIFF(int trackedSosigID, int IFF)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigSetOriginalIFF))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)IFF);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDamage(int trackedSosigID, int linkIndex, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkDamage(Packet packet)
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
            using (Packet packet = new Packet((int)ClientPackets.sosigWearableDamage))
            {
                packet.Write(trackedSosigID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)wearableIndex);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigWearableDamage(Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.sosigWearableDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void SosigDamageData(TrackedSosig trackedSosig)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigDamageData))
            {
                packet.Write(trackedSosig.data.trackedID);
                packet.Write(trackedSosig.physicalSosig.IsStunned);
                packet.Write(trackedSosig.physicalSosig.m_stunTimeLeft);
                packet.Write((byte)trackedSosig.physicalSosig.BodyState);
                packet.Write(trackedSosig.physicalSosig.m_isOnOffMeshLink);
                packet.Write(trackedSosig.physicalSosig.Agent.autoTraverseOffMeshLink);
                packet.Write(trackedSosig.physicalSosig.Agent.enabled);
                List<CharacterJoint> joints = trackedSosig.physicalSosig.m_joints;
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
                packet.Write(trackedSosig.physicalSosig.m_isCountingDownToStagger);
                packet.Write(trackedSosig.physicalSosig.m_staggerAmountToApply);
                packet.Write(trackedSosig.physicalSosig.m_recoveringFromBallisticState);
                packet.Write(trackedSosig.physicalSosig.m_recoveryFromBallisticLerp);
                packet.Write(trackedSosig.physicalSosig.m_tickDownToWrithe);
                packet.Write(trackedSosig.physicalSosig.m_recoveryFromBallisticTick);
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromIFF());
                packet.Write((byte)trackedSosig.physicalSosig.GetDiedFromClass());
                packet.Write(trackedSosig.physicalSosig.IsBlinded);
                packet.Write(trackedSosig.physicalSosig.m_blindTime);
                packet.Write(trackedSosig.physicalSosig.IsFrozen);
                packet.Write(trackedSosig.physicalSosig.m_debuffTime_Freeze);
                packet.Write(trackedSosig.physicalSosig.GetDiedFromHeadShot());
                packet.Write(trackedSosig.physicalSosig.m_timeSinceLastDamage);
                packet.Write(trackedSosig.physicalSosig.IsConfused);
                packet.Write(trackedSosig.physicalSosig.m_confusedTime);
                packet.Write(trackedSosig.physicalSosig.m_storedShudder);

                SendTCPData(packet);
            }
        }

        public static void EncryptionDamageData(TrackedEncryption trackedEncryption)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionDamageData))
            {
                packet.Write(trackedEncryption.data.trackedID);
                packet.Write(trackedEncryption.physicalEncryption.m_numHitsLeft);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamageData(int trackedID, AutoMeaterHitZone hitZone)
        {
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterHitZoneDamageData))
            {
                packet.Write(trackedID);
                packet.Write((byte)hitZone.Type);

                packet.Write(hitZone.ArmorThreshold);
                packet.Write(hitZone.LifeUntilFailure);
                packet.Write(hitZone.m_isDestroyed);

                SendTCPData(packet);
            }
        }

        public static void SosigLinkExplodes(int sosigTrackedID, int linkIndex, Damage.DamageClass damClass)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkExplodes))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)linkIndex);
                packet.Write((byte)damClass);

                SendTCPData(packet);
            }
        }

        public static void SosigDies(int sosigTrackedID, Damage.DamageClass damClass, Sosig.SosigDeathType deathType)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigDies))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)damClass);
                packet.Write((byte)deathType);

                SendTCPData(packet);
            }
        }

        public static void SosigClear(int sosigTrackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigClear))
            {
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void SosigSetBodyState(int sosigTrackedID, Sosig.SosigBodyState s)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigSetBodyState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)s);

                SendTCPData(packet);
            }
        }

        public static void PlaySosigFootStepSound(int sosigTrackedID, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay)
        {
            using (Packet packet = new Packet((int)ClientPackets.playSosigFootStepSound))
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
            using (Packet packet = new Packet((int)ClientPackets.sosigSpeakState))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)currentOrder);

                SendTCPData(packet);
            }
        }

        public static void SosigSetCurrentOrder(TrackedSosigData trackedSosig, Sosig.SosigOrder currentOrder)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigSetCurrentOrder))
            {
                packet.Write(trackedSosig.trackedID);
                packet.Write((byte)currentOrder);
                switch (trackedSosig.currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        packet.Write(trackedSosig.guardPoint);
                        packet.Write(trackedSosig.guardDir);
                        packet.Write(trackedSosig.hardGuard);
                        break;
                    case Sosig.SosigOrder.Skirmish:
                        packet.Write(trackedSosig.skirmishPoint);
                        packet.Write(trackedSosig.pathToPoint);
                        packet.Write(trackedSosig.assaultPoint);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.Investigate:
                        packet.Write(trackedSosig.guardPoint);
                        packet.Write(trackedSosig.hardGuard);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                    case Sosig.SosigOrder.Wander:
                        packet.Write(trackedSosig.wanderPoint);
                        break;
                    case Sosig.SosigOrder.Assault:
                        packet.Write(trackedSosig.assaultPoint);
                        packet.Write((byte)trackedSosig.assaultSpeed);
                        packet.Write(trackedSosig.faceTowards);
                        break;
                    case Sosig.SosigOrder.Idle:
                        packet.Write(trackedSosig.idleToPoint);
                        packet.Write(trackedSosig.idleDominantDir);
                        break;
                    case Sosig.SosigOrder.PathTo:
                        packet.Write(trackedSosig.pathToPoint);
                        packet.Write(trackedSosig.pathToLookDir);
                        break;
                }

                SendTCPData(packet);
            }
        }

        public static void SosigVaporize(int sosigTrackedID, int iff)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigVaporize))
            {
                packet.Write(sosigTrackedID);
                packet.Write((byte)iff);

                SendTCPData(packet);
            }
        }

        public static void SosigRequestHitDecal(int sosigTrackedID, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, int linkIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigRequestHitDecal))
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
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkBreak))
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
            using (Packet packet = new Packet((int)ClientPackets.sosigLinkSever))
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
            for(int i=0; i < GameManager.objects.Count; ++i)
            {
                using (Packet packet = new Packet((int)ClientPackets.updateObjectRequest))
                {
                    TrackedObjectData trackedObject = GameManager.objects[i];

                    if (trackedObject.trackedID == -1)
                    {
                        continue;
                    }

                    packet.Write(instantiateOnReceive);

                    trackedObject.latestUpdateSent = false;

                    trackedObject.Update(true);

                    trackedObject.WriteToPacket(packet, false, true);

                    SendTCPData(packet);
                }
            }

            DoneSendingUpdaToDateObjects(forClient);
        }

        public static void DoneLoadingScene()
        {
            using (Packet packet = new Packet((int)ClientPackets.DoneLoadingScene))
            {
                SendTCPData(packet);
            }
        }

        public static void DoneSendingUpdaToDateObjects(int forClient)
        {
            using (Packet packet = new Packet((int)ClientPackets.DoneSendingUpdaToDateObjects))
            {
                packet.Write(forClient);

                SendTCPData(packet);
            }
        }

        public static void AddTNHCurrentlyPlaying(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.addTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.removeTNHCurrentlyPlaying))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHProgression(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHProgression))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHEquipment(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHEquipment))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMode(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHHealthMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHTargetMode(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHTargetMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHAIDifficulty(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHAIDifficulty))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHRadarMode(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHRadarMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHItemSpawnerMode(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHItemSpawnerMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHBackpackMode(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHBackpackMode))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHHealthMult(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHHealthMult))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSosigGunReload(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHSosigGunReload))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHSeed(int i, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHSeed))
            {
                packet.Write(i);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHLevelID(string levelID, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHLevelID))
            {
                packet.Write(levelID);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SetTNHController(int instance, int ID)
        {
            using (Packet packet = new Packet((int)ClientPackets.setTNHController))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHPlayerDied(int instance, int ID)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHPlayerDied))
            {
                packet.Write(instance);
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void TNHAddTokens(int instance, int amount)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHAddTokens))
            {
                packet.Write(instance);
                packet.Write(amount);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetState(int trackedID, byte state)
        {
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterSetState))
            {
                packet.Write(trackedID);
                packet.Write(state);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterSetBladesActive(int trackedID, bool active)
        {
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterSetBladesActive))
            {
                packet.Write(trackedID);
                packet.Write(active);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterDamage(Packet packet)
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
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterFirearmFireAtWill))
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
            using (Packet packet = new Packet((int)ClientPackets.autoMeaterHitZoneDamage))
            {
                packet.Write(trackedID);
                packet.Write((byte)type);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(Packet packet)
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
            using (Packet packet = new Packet((int)ClientPackets.encryptionDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void EncryptionDamage(Packet packet)
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
            using (Packet packet = new Packet((int)ClientPackets.TNHSosigKill))
            {
                packet.Write(instance);
                packet.Write(sosigTrackedID);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointSystemNode(int instance, int levelIndex, int holdPointIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointSystemNode))
            {
                packet.Write(instance);
                packet.Write(levelIndex);
                packet.Write(holdPointIndex);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldBeginChallenge(int instance, bool controller)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldBeginChallenge))
            {
                Mod.LogInfo("TNHHoldBeginChallenge client send", false);
                packet.Write(instance);
                packet.Write(controller);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointRaiseBarriers(int instance, List<int> barrierIndices, List<int> barrierPrefabIndices)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointRaiseBarriers))
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

        public static void ShatterableCrateSetHoldingHealth(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.shatterableCrateSetHoldingHealth))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void ShatterableCrateSetHoldingToken(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.shatterableCrateSetHoldingToken))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void ShatterableCrateDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.shatterableCrateDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void ShatterableCrateDamage(Packet packet)
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
            using (Packet packet = new Packet((int)ClientPackets.shatterableCrateDestroy))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void TNHSetLevel(int instance, int level)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSetLevel))
            {
                packet.Write(instance);
                packet.Write(level);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhaseTake(int instance, int holdIndex, List<int> activeSupplyPointIndices, bool init)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSetPhaseTake))
            {
                packet.Write(instance);
                packet.Write(holdIndex);
                packet.Write(activeSupplyPointIndices.Count);
                foreach(int index in activeSupplyPointIndices)
                {
                    packet.Write(index);
                }
                packet.Write(init);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhaseHold(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSetPhaseHold))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldCompletePhase(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldCompletePhase))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointFailOut(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointFailOut))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointBeginPhase(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointBeginPhase))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointCompleteHold(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointCompleteHold))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhaseComplete(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSetPhaseComplete))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHSetPhase(int instance, TNH_Phase p)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSetPhase))
            {
                packet.Write(instance);
                packet.Write((short)p);

                SendTCPData(packet);
            }
        }

        public static void EncryptionRespawnSubTarg(int trackedID, int index)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionRespawnSubTarg))
            {
                packet.Write(trackedID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void EncryptionRespawnSubTargGeo(int trackedID, int index)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionRespawnSubTargGeo))
            {
                packet.Write(trackedID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void EncryptionSpawnGrowth(int trackedID, int index, Vector3 point)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionSpawnGrowth))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);

                SendTCPData(packet);
            }
        }

        public static void EncryptionInit(int trackedID, List<int> indices, List<Vector3> points, Vector3 initialPos, int numHitsLeft)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionInit))
            {
                packet.Write(trackedID);
                if(indices == null || indices.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(indices.Count);
                    for(int i=0; i < indices.Count; ++i)
                    {
                        packet.Write(indices[i]);
                    }
                }
                if(points == null || points.Count == 0)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(points.Count);
                    for(int i=0; i < points.Count; ++i)
                    {
                        packet.Write(points[i]);
                    }
                }
                packet.Write(initialPos);
                packet.Write(numHitsLeft);

                SendTCPData(packet);
            }
        }

        public static void EncryptionResetGrowth(int trackedID, int index, Vector3 point)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionResetGrowth))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);

                SendTCPData(packet);
            }
        }

        public static void EncryptionDisableSubtarg(int trackedID, int index)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionDisableSubtarg))
            {
                packet.Write(trackedID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void EncryptionSubDamage(int trackedID, int index, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.encryptionSubDamage))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void SosigWeaponDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigWeaponDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void RemoteMissileDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.remoteMissileDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void StingerMissileDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.stingerMissileDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int instance, List<Vector3> data, float tickDownToID)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldPointBeginAnalyzing))
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
            using (Packet packet = new Packet((int)ClientPackets.TNHHoldIdentifyEncryption))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SosigPriorityIFFChart(int trackedID, int chart)
        {
            using (Packet packet = new Packet((int)ClientPackets.sosigPriorityIFFChart))
            {
                packet.Write(trackedID);
                packet.Write(chart);

                SendTCPData(packet);
            }
        }

        public static void RemoteMissileDetonate(int trackedID, Vector3 pos)
        {
            using (Packet packet = new Packet((int)ClientPackets.remoteMissileDetonate))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void StingerMissileExplode(int trackedID, Vector3 pos)
        {
            using (Packet packet = new Packet((int)ClientPackets.stingerMissileExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void PinnedGrenadeExplode(int trackedID, Vector3 pos)
        {
            using (Packet packet = new Packet((int)ClientPackets.pinnedGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void PinnedGrenadePullPin(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.pinnedGrenadePullPin))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void FVRGrenadeExplode(int trackedID, Vector3 pos)
        {
            using (Packet packet = new Packet((int)ClientPackets.FVRGrenadeExplode))
            {
                packet.Write(trackedID);
                packet.Write(pos);

                SendTCPData(packet);
            }
        }

        public static void ClientDisconnect()
        {
            using (Packet packet = new Packet((int)ClientPackets.clientDisconnect))
            {
                packet.Write(Convert.ToInt64((DateTime.Now.ToUniversalTime() - ThreadManager.epoch).TotalMilliseconds));

                SendTCPData(packet);
            }
        }

        public static void BangSnapSplode(int trackedID, Vector3 position)
        {
            using (Packet packet = new Packet((int)ClientPackets.bangSnapSplode))
            {
                packet.Write(trackedID);
                packet.Write(position);

                SendTCPData(packet);
            }
        }

        public static void C4Detonate(int trackedID, Vector3 position)
        {
            using (Packet packet = new Packet((int)ClientPackets.C4Detonate))
            {
                packet.Write(trackedID);
                packet.Write(position);

                SendTCPData(packet);
            }
        }

        public static void ClaymoreMineDetonate(int trackedID, Vector3 position)
        {
            using (Packet packet = new Packet((int)ClientPackets.claymoreMineDetonate))
            {
                packet.Write(trackedID);
                packet.Write(position);

                SendTCPData(packet);
            }
        }

        public static void SLAMDetonate(int trackedID, Vector3 position)
        {
            using (Packet packet = new Packet((int)ClientPackets.SLAMDetonate))
            {
                packet.Write(trackedID);
                packet.Write(position);

                SendTCPData(packet);
            }
        }

        public static void SpectatorHost(bool spectatorHost)
        {
            using (Packet packet = new Packet((int)ClientPackets.spectatorHost))
            {
                packet.Write(spectatorHost);

                SendTCPData(packet);
            }
        }

        public static void ResetTNH(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.resetTNH))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void ReviveTNHPlayer(int ID, int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.reviveTNHPlayer))
            {
                packet.Write(ID);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void PlayerColor(int ID, int index)
        {
            using (Packet packet = new Packet((int)ClientPackets.playerColor))
            {
                packet.Write(ID);
                packet.Write(index);

                SendTCPData(packet);
            }
        }

        public static void RequestTNHInitialization(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.requestTNHInit))
            {
                Mod.LogInfo("Sending request for perm to init TNH " + instance, false);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void TNHInitializer(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHInit))
            {
                Mod.LogInfo("Sending TNH "+instance+" init signal", false);
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void FuseIgnite(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.fuseIgnite))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void FuseBoom(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.fuseBoom))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void MolotovShatter(int trackedID, bool ignited)
        {
            using (Packet packet = new Packet((int)ClientPackets.molotovShatter))
            {
                packet.Write(trackedID);
                packet.Write(ignited);

                SendTCPData(packet);
            }
        }

        public static void MolotovDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.molotovDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void MagazineAddRound(int trackedID, FireArmRoundClass roundClass)
        {
            using (Packet packet = new Packet((int)ClientPackets.magazineAddRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);

                SendTCPData(packet);
            }
        }

        public static void ClipAddRound(int trackedID, FireArmRoundClass roundClass)
        {
            using (Packet packet = new Packet((int)ClientPackets.clipAddRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);

                SendTCPData(packet);
            }
        }

        public static void RemoteGunChamber(int trackedID, FireArmRoundClass roundClass, FireArmRoundType roundType)
        {
            using (Packet packet = new Packet((int)ClientPackets.remoteGunChamber))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((short)roundType);

                SendTCPData(packet);
            }
        }

        public static void SpeedloaderChamberLoad(int trackedID, FireArmRoundClass roundClass, int chamberIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.speedloaderChamberLoad))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)chamberIndex);

                SendTCPData(packet);
            }
        }

        public static void ChamberRound(int trackedID, FireArmRoundClass roundClass, int chamberIndex)
        {
            using (Packet packet = new Packet((int)ClientPackets.chamberRound))
            {
                packet.Write(trackedID);
                packet.Write((short)roundClass);
                packet.Write((byte)chamberIndex);

                SendTCPData(packet);
            }
        }

        public static void MagazineLoad(int trackedID, int FATrackedID, int slot = -1)
        {
            using (Packet packet = new Packet((int)ClientPackets.magazineLoad))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);
                packet.Write((short)slot);

                SendTCPData(packet);
            }
        }

        public static void MagazineLoadAttachable(int trackedID, int FATrackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.magazineLoadAttachable))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);

                SendTCPData(packet);
            }
        }

        public static void ClipLoad(int trackedID, int FATrackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.clipLoad))
            {
                packet.Write(trackedID);
                packet.Write(FATrackedID);

                SendTCPData(packet);
            }
        }

        public static void RevolverCylinderLoad(int trackedID, Speedloader speedLoader)
        {
            using (Packet packet = new Packet((int)ClientPackets.revolverCylinderLoad))
            {
                packet.Write(trackedID);
                packet.Write((byte)speedLoader.Chambers.Count);
                for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                {
                    if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                    {
                        packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                    }
                    else
                    {
                        packet.Write((short)-1);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void RevolvingShotgunLoad(int trackedID, Speedloader speedLoader)
        {
            using (Packet packet = new Packet((int)ClientPackets.revolvingShotgunLoad))
            {
                packet.Write(trackedID);
                packet.Write((byte)speedLoader.Chambers.Count);
                for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                {
                    if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                    {
                        packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                    }
                    else
                    {
                        packet.Write((short)-1);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void GrappleGunLoad(int trackedID, Speedloader speedLoader)
        {
            using (Packet packet = new Packet((int)ClientPackets.grappleGunLoad))
            {
                packet.Write(trackedID);
                packet.Write((byte)speedLoader.Chambers.Count);
                for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                {
                    if (speedLoader.Chambers[i].IsLoaded && !speedLoader.Chambers[i].IsSpent)
                    {
                        packet.Write((short)speedLoader.Chambers[i].LoadedClass);
                    }
                    else
                    {
                        packet.Write((short)-1);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void CarlGustafLatchSate(int trackedID, CarlGustafLatch.CGLatchType type, CarlGustafLatch.CGLatchState state)
        {
            using (Packet packet = new Packet((int)ClientPackets.carlGustafLatchSate))
            {
                packet.Write(trackedID);
                packet.Write((byte)type);
                packet.Write((byte)state);

                SendTCPData(packet);
            }
        }

        public static void CarlGustafShellSlideSate(int trackedID, CarlGustafShellInsertEject.ChamberSlideState state)
        {
            using (Packet packet = new Packet((int)ClientPackets.carlGustafShellSlideSate))
            {
                packet.Write(trackedID);
                packet.Write((byte)state);

                SendTCPData(packet);
            }
        }

        public static void TNHHostStartHold(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHHostStartHold))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void GrappleAttached(int trackedID, byte[] data)
        {
            using (Packet packet = new Packet((int)ClientPackets.grappleAttached))
            {
                packet.Write(trackedID);
                packet.Write((short)data.Length);
                packet.Write(data);

                SendTCPData(packet);
            }
        }

        /// <summary>
        /// Requests the registration of a new packet ID to the Server
        /// </summary>
        /// <param name="ID">The identifier for the new packet ID</param>
        public static void RegisterCustomPacketType(string ID)
        {
            using (Packet packet = new Packet((int)ClientPackets.registerCustomPacketType))
            {
                packet.Write(ID);

                SendTCPData(packet);
            }
        }

        public static void BreakableGlassDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.breakableGlassDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void BreakableGlassDamage(Packet packet)
        {
            byte[] IDbytes = BitConverter.GetBytes((int)ClientPackets.breakableGlassDamage);
            for (int i = 0; i < 4; ++i)
            {
                packet.buffer[i] = IDbytes[i];
            }
            packet.readPos = 0;

            SendTCPData(packet);
        }

        public static void WindowShatterSound(int trackedID, int mode)
        {
            using (Packet packet = new Packet((int)ClientPackets.windowShatterSound))
            {
                packet.Write(trackedID);
                packet.Write((byte)mode);

                SendTCPData(packet);
            }
        }

        /// <summary>
        /// CUSTOMIZATION
        /// Used to request a Spectator Host from the server
        /// </summary>
        public static void RequestSpectatorHost()
        {
            using (Packet packet = new Packet((int)ClientPackets.requestSpectatorHost))
            {
                SendTCPData(packet);
            }
        }

        /// <summary>
        /// CUSTOMIZATION
        /// Used to give up the control of a spectator host
        /// </summary>
        public static void UnassignSpectatorHost()
        {
            using (Packet packet = new Packet((int)ClientPackets.unassignSpectatorHost))
            {
                SendTCPData(packet);
            }
        }

        public static void SpectatorHostOrderTNHHost(bool onDeathSpectate)
        {
            using (Packet packet = new Packet((int)ClientPackets.spectatorHostOrderTNHHost))
            {
                packet.Write(onDeathSpectate);

                SendTCPData(packet);
            }
        }

        public static void TNHSpectatorHostReady(int instance)
        {
            using (Packet packet = new Packet((int)ClientPackets.TNHSpectatorHostReady))
            {
                packet.Write(instance);

                SendTCPData(packet);
            }
        }

        public static void SpectatorHostStartTNH(int host)
        {
            using (Packet packet = new Packet((int)ClientPackets.spectatorHostStartTNH))
            {
                packet.Write(host);

                SendTCPData(packet);
            }
        }

        public static void ReassignSpectatorHost(List<int> debounce)
        {
            using (Packet packet = new Packet((int)ClientPackets.reassignSpectatorHost))
            {
                if(debounce == null)
                {
                    packet.Write(0);
                }
                else
                {
                    packet.Write(debounce.Count);
                    for (int i = 0; i < debounce.Count; ++i)
                    {
                        packet.Write(debounce[i]);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void ReactiveSteelTargetDamage(int trackedID, int targetIndex, float damKinetic, float damBlunt, Vector3 point, Vector3 dir, bool usesHoles, Vector3 pos, Quaternion rot, float scale)
        {
            using (Packet packet = new Packet((int)ClientPackets.reactiveSteelTargetDamage))
            {
                packet.Write(trackedID);
                packet.Write(targetIndex);
                packet.Write(damKinetic);
                packet.Write(damBlunt);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(usesHoles);
                if (usesHoles)
                {
                    packet.Write(pos);
                    packet.Write(rot);
                    packet.Write(scale);
                }

                SendTCPData(packet);
            }
        }

        public static void IDConfirm(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.IDConfirm))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void ObjectScene(TrackedObjectData trackedObjectData)
        {
            using (Packet packet = new Packet((int)ClientPackets.objectScene))
            {
                trackedObjectData.WriteToPacket(packet, false, true);

                SendTCPData(packet);
            }
        }

        public static void ObjectInstance(TrackedObjectData trackedObjectData)
        {
            using (Packet packet = new Packet((int)ClientPackets.objectInstance))
            {
                trackedObjectData.WriteToPacket(packet, false, true);

                SendTCPData(packet);
            }
        }

        public static void UpdateEncryptionDisplay(int trackedID, int numHitsLeft)
        {
            using (Packet packet = new Packet((int)ClientPackets.updateEncryptionDisplay))
            {
                packet.Write(trackedID);
                packet.Write(numHitsLeft);

                SendTCPData(packet);
            }
        }

        public static void RoundDamage(int trackedID, Damage damage)
        {
            using (Packet packet = new Packet((int)ClientPackets.roundDamage))
            {
                packet.Write(trackedID);
                packet.Write(damage);

                SendTCPData(packet);
            }
        }

        public static void RoundSplode(int trackedID, float velMultiplier, bool isRandomDir)
        {
            using (Packet packet = new Packet((int)ClientPackets.roundSplode))
            {
                packet.Write(trackedID);
                packet.Write(velMultiplier);
                packet.Write(isRandomDir);

                SendTCPData(packet);
            }
        }

        public static void SightFlipperState(int trackedID, int index, bool large)
        {
            using (Packet packet = new Packet((int)ClientPackets.sightFlipperState))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(large);

                SendTCPData(packet);
            }
        }

        public static void SightRaiserState(int trackedID, int index, AR15HandleSightRaiser.SightHeights height)
        {
            using (Packet packet = new Packet((int)ClientPackets.sightRaiserState))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write((byte)height);

                SendTCPData(packet);
            }
        }

        public static void GatlingGunFire(int trackedID, Vector3 pos, Quaternion rot, Vector3 dir)
        {
            using (Packet packet = new Packet((int)ClientPackets.gatlingGunFire))
            {
                packet.Write(trackedID);
                packet.Write(pos);
                packet.Write(rot);
                packet.Write(dir);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidGout(int trackedID, Vector3 pos, Vector3 norm)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidGout))
            {
                packet.Write(trackedID);
                packet.Write(pos);
                packet.Write(norm);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidHandleDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidHandleDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidDamageHandle(int trackedID)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidDamageHandle))
            {
                packet.Write(trackedID);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidExplode(int trackedID, Vector3 point, Vector3 dir, bool big)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidExplode))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(big);

                SendTCPData(packet);
            }
        }

        public static void GasCuboidShatter(int trackedID, Vector3 point, Vector3 dir)
        {
            using (Packet packet = new Packet((int)ClientPackets.gasCuboidShatter))
            {
                packet.Write(trackedID);
                packet.Write(point);
                packet.Write(dir);

                SendTCPData(packet);
            }
        }

        public static void FloaterDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.floaterDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void FloaterCoreDamage(int trackedID, Damage d)
        {
            using (Packet packet = new Packet((int)ClientPackets.floaterCoreDamage))
            {
                packet.Write(trackedID);
                packet.Write(d);

                SendTCPData(packet);
            }
        }

        public static void FloaterBeginExploding(int trackedID, bool control)
        {
            using (Packet packet = new Packet((int)ClientPackets.floaterBeginExploding))
            {
                packet.Write(trackedID);
                packet.Write(control);

                SendTCPData(packet);
            }
        }

        public static void FloaterBeginDefusing(int trackedID, bool control)
        {
            using (Packet packet = new Packet((int)ClientPackets.floaterBeginDefusing))
            {
                packet.Write(trackedID);
                packet.Write(control);

                SendTCPData(packet);
            }
        }

        public static void FloaterExplode(int trackedID, bool defusing)
        {
            using (Packet packet = new Packet((int)ClientPackets.floaterExplode))
            {
                packet.Write(trackedID);
                packet.Write(defusing);

                SendTCPData(packet);
            }
        }

        public static void IrisShatter(int trackedID, byte index, Vector3 point, Vector3 dir, float intensity)
        {
            using (Packet packet = new Packet((int)ClientPackets.irisShatter))
            {
                packet.Write(trackedID);
                packet.Write(index);
                packet.Write(point);
                packet.Write(dir);
                packet.Write(intensity);

                SendTCPData(packet);
            }
        }

        public static void IrisSetState(int trackedID, Construct_Iris.IrisState s)
        {
            using (Packet packet = new Packet((int)ClientPackets.irisSetState))
            {
                packet.Write(trackedID);
                packet.Write((byte)s);

                SendTCPData(packet);
            }
        }

        public static void BrutBlockSystemStart(int trackedID, bool next)
        {
            using (Packet packet = new Packet((int)ClientPackets.brutBlockSystemStart))
            {
                packet.Write(trackedID);
                packet.Write(next);

                SendTCPData(packet);
            }
        }
    }
}
