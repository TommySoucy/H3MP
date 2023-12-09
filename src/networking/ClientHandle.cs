using FistVR;
using H3MP.Patches;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Networking
{
    public class ClientHandle
    {
        public static void Welcome(Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();
            Client.singleton.SetTickRate(packet.ReadByte());
            GameManager.colorByIFF = packet.ReadBool();
            GameManager.nameplateMode = packet.ReadInt();
            GameManager.radarMode = packet.ReadInt();
            GameManager.radarColor = packet.ReadBool();
            int maxHealthCount = packet.ReadInt();
            for(int i=0; i < maxHealthCount; ++i)
            {
                string currentScene = packet.ReadString();
                int instanceCount = packet.ReadInt();

                Dictionary<int, KeyValuePair<float, int>> newDict = new Dictionary<int, KeyValuePair<float, int>>();
                GameManager.maxHealthByInstanceByScene.Add(currentScene, newDict);

                for(int j=0; j < instanceCount; ++j)
                {
                    int currentInstance = packet.ReadInt();
                    float original = packet.ReadFloat();
                    int index = packet.ReadInt();

                    newDict.Add(currentInstance, new KeyValuePair<float, int>(original, index));
                }
            }
            int registeredCustomPacketIDCount = packet.ReadInt();
            for(int i=0; i < registeredCustomPacketIDCount; ++i)
            {
                string cutomPacketHandlerID = packet.ReadString();
                int cutomPacketIndex = packet.ReadInt();
                Mod.registeredCustomPacketIDs.Add(cutomPacketHandlerID, cutomPacketIndex);
                Mod.CustomPacketHandlerReceivedInvoke(cutomPacketHandlerID, cutomPacketIndex);
            }

            H3MPWristMenuSection.UpdateMaxHealth(GameManager.scene, GameManager.instance, -2, -1);

            Mod.LogInfo($"Message from server: {msg}", false);

            Client.singleton.gotWelcome = true;
            Client.singleton.ID = ID;
            GameManager.ID = ID;
            ClientSend.WelcomeReceived();

            try
            {
                Client.singleton.udp.Connect(((IPEndPoint)Client.singleton.tcp.socket.Client.LocalEndPoint).Port);
            }
            catch (SocketException)
            {
                Mod.LogError("SocketException caught trying to make UDP connection to server, disconnecting. A game restart should be enough to fix this.");
                Client.singleton.Disconnect(true, 0);
                return;
            }

            if (GameManager.reconnectionInstance != -1)
            {
                GameManager.SetInstance(GameManager.reconnectionInstance);
                GameManager.reconnectionInstance = -1;
            }

            // Ensure the player has a body
            Mod.ForceDefaultPlayerBody();
        }

        public static void Ping(Packet packet)
        {
            GameManager.ping = Convert.ToInt64((DateTime.Now.ToUniversalTime() - ThreadManager.epoch).TotalMilliseconds) - packet.ReadLong();
        }

        public static void SpawnPlayer(Packet packet)
        {
            int ID = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            int IFF = packet.ReadInt();
            int colorIndex = packet.ReadInt();
            bool join = packet.ReadBool();

            Mod.LogInfo("ClientHandle SpawnPlayer, ID: " + ID + ", username: " + username, false);

            GameManager.singleton.SpawnPlayer(ID, username, scene, instance, IFF, colorIndex, join);
        }

        public static void ConnectSync(Packet packet)
        {
            Client.singleton.gotConnectSync = true;
            bool inControl = packet.ReadBool();

            // Just connected, sync if current scene is syncable
            if (!GameManager.nonSynchronizedScenes.ContainsKey(GameManager.scene))
            {
                GameManager.SyncTrackedObjects(true, inControl);
            }
        }

        public static void PlayerState(Packet packet)
        {
            int ID = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            Vector3 headPos = packet.ReadVector3();
            Quaternion headRot = packet.ReadQuaternion();
            Vector3 torsoPos = packet.ReadVector3();
            Quaternion torsoRot = packet.ReadQuaternion();
            Vector3 leftHandPos = packet.ReadVector3();
            Quaternion leftHandRot = packet.ReadQuaternion();
            Vector3 rightHandPos = packet.ReadVector3();
            Quaternion rightHandRot = packet.ReadQuaternion();
            float health = packet.ReadFloat();
            int maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if (additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            GameManager.UpdatePlayerState(ID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                          leftHandPos, leftHandRot,
                                          rightHandPos, rightHandRot,
                                          health, maxHealth, additionalData);
        }

        public static void PlayerIFF(Packet packet)
        {
            int clientID = packet.ReadInt();
            int IFF = packet.ReadInt();
            if (GameManager.players.ContainsKey(clientID))
            {
                GameManager.players[clientID].SetIFF(IFF);
            }
        }

        public static void PlayerScene(Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            GameManager.UpdatePlayerScene(ID, scene);
        }

        public static void PlayerInstance(Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.UpdatePlayerInstance(ID, instance);
        }

        public static void TrackedObjects(Packet packet)
        {
            //int count = packet.ReadShort();
            //for (int i = 0; i < count; ++i)
            //{
                TrackedObjectData.Update(packet, true);
            //}
        }

        public static void ObjectUpdate(Packet packet)
        {
            TrackedObjectData.Update(packet, false);
        }

        public static void TrackedObject(Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            Mod.LogInfo("Client received tracked object " + trackedID + " with type: " + typeID, false);
            if (Mod.trackedObjectTypesByName.TryGetValue(typeID, out Type trackedObjectType))
            {
                Mod.LogInfo("\tGot type, adding", false);
                Client.AddTrackedObject((TrackedObjectData)Activator.CreateInstance(trackedObjectType, packet, typeID, trackedID));
            }
        }

        public static void GiveObjectControl(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }
            Mod.LogInfo("Client received GiveObjectControl for "+trackedID+" to controller: "+controllerID, false);

            TrackedObjectData trackedObject = Client.objects[trackedID];

            if (trackedObject != null)
            {
                Mod.LogInfo("\tGot object", false);
                bool destroyed = false;
                if (trackedObject.controller == GameManager.ID && controllerID != GameManager.ID)
                {
                    Mod.LogInfo("\t\tWas controller, removing from local", false);
                    trackedObject.RemoveFromLocal();
                }
                else if (trackedObject.controller != GameManager.ID && controllerID == GameManager.ID)
                {
                    Mod.LogInfo("\t\tNew controller", false);
                    trackedObject.localTrackedID = GameManager.objects.Count;
                    GameManager.objects.Add(trackedObject);
                    if (trackedObject.physical == null)
                    {
                        Mod.LogInfo("\t\t\tNo phys", false);
                        // If it is null and we receive this after having finishing loading, we only want to instantiate if it is in our current scene/instance
                        if (!GameManager.sceneLoading)
                        {
                            Mod.LogInfo("\t\t\t\tNot loading", false);
                            if (trackedObject.scene.Equals(GameManager.scene) && trackedObject.instance == GameManager.instance)
                            {
                                Mod.LogInfo("\t\t\t\t\tSame scene instance, instantiating", false);
                                if (!trackedObject.awaitingInstantiation)
                                {
                                    trackedObject.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedObject.Instantiate());
                                }
                            }
                            else
                            {
                                Mod.LogInfo("\t\t\t\t\tDifferent scene instance, can't take control", false);
                                // Scene not loading but object is not in our scene/instance, try bouncing or destroy
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedObject.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedObject.controller, true, true, newPlayerList, trackedObject.scene, trackedObject.instance);
                                    if (controllerID == -1)
                                    {
                                        Mod.LogInfo("\t\t\t\t\t\tNo one to bounce to, destroying", false);
                                        ClientSend.DestroyObject(trackedID);
                                        trackedObject.RemoveFromLocal();
                                        Client.objects[trackedID] = null;
                                        if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                        {
                                            objectList.Remove(trackedObject.trackedID);
                                        }
                                        trackedObject.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        Mod.LogInfo("\t\t\t\t\t\tBouncing to "+ controllerID, false);
                                        trackedObject.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveObjectControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\t\t\t\tNo one to bounce to, destroying", false);
                                    ClientSend.DestroyObject(trackedID);
                                    trackedObject.RemoveFromLocal();
                                    Client.objects[trackedID] = null;
                                    if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                    {
                                        objectList.Remove(trackedObject.trackedID);
                                    }
                                    trackedObject.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else
                        {
                            Mod.LogInfo("\t\t\t\tLoading", false);
                            // Only bounce control or destroy if we are not on our way towards the object's scene/instance
                            if (!trackedObject.scene.Equals(LoadLevelBeginPatch.loadingLevel) || trackedObject.instance != GameManager.instance)
                            {
                                Mod.LogInfo("\t\t\t\t\tDestination not same scene instance", false);
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedObject.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedObject.controller, true, true, newPlayerList, trackedObject.scene, trackedObject.instance);
                                    if (controllerID == -1)
                                    {
                                        Mod.LogInfo("\t\t\t\t\t\tNo one to bounce to, destroying", false);
                                        ClientSend.DestroyObject(trackedID);
                                        trackedObject.RemoveFromLocal();
                                        Client.objects[trackedID] = null;
                                        if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                        {
                                            objectList.Remove(trackedObject.trackedID);
                                        }
                                        trackedObject.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        Mod.LogInfo("\t\t\t\t\t\tBouncing to " + controllerID, false);
                                        trackedObject.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveObjectControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\t\t\t\tNo one to bounce to, destroying", false);
                                    ClientSend.DestroyObject(trackedID);
                                    trackedObject.RemoveFromLocal();
                                    Client.objects[trackedID] = null;
                                    if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                    {
                                        objectList.Remove(trackedObject.trackedID);
                                    }
                                    trackedObject.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                            // else, Loading on our way to the object's scene/instance, will instantiate when we arrive
                        }
                    }
                }

                if (!destroyed)
                {
                    trackedObject.SetController(controllerID);
                }
            }
        }

        public static void DestroyObject(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            TrackedObjectData trackedObject = Client.objects[trackedID];
            Mod.LogInfo("Client received object destruction for tracked ID: " + trackedID+", remove from lists: "+removeFromList, false);

            if (trackedObject != null)
            {
                Mod.LogInfo("\tGot object",false);
                trackedObject.awaitingInstantiation = false;

                bool destroyed = false;
                if (trackedObject.physical != null)
                {
                    Mod.LogInfo("\t\tGot phys, destroying", false);
                    trackedObject.removeFromListOnDestroy = removeFromList;
                    trackedObject.physical.sendDestroy = false;
                    trackedObject.physical.dontGiveControl = true;
                    TrackedObject[] childrenTrackedObjects = trackedObject.physical.GetComponentsInChildren<TrackedObject>();
                    for (int i = 0; i < childrenTrackedObjects.Length; ++i)
                    {
                        if (childrenTrackedObjects[i] != null)
                        {
                            childrenTrackedObjects[i].sendDestroy = false;
                            childrenTrackedObjects[i].data.removeFromListOnDestroy = removeFromList;
                            childrenTrackedObjects[i].dontGiveControl = true;
                        }
                    }

                    trackedObject.physical.SecondaryDestroy();

                    GameObject.Destroy(trackedObject.physical.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedObject.localTrackedID != -1)
                {
                    Mod.LogInfo("\t\tNo phys, is local, removing from local", false);
                    trackedObject.RemoveFromLocal();
                }

                // Check if want to ensure this was removed from list, if it wasn't by the destruction, do it here
                if (removeFromList && !destroyed)
                {
                    Mod.LogInfo("\t\tNo phys, removing from lists", false);
                    trackedObject.RemoveFromLists();
                }
            }
        }

        public static void ObjectParent(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            if (Client.objects[trackedID] != null)
            {
                Client.objects[trackedID].SetParent(newParentID);
            }
        }

        public static void WeaponFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;
                int chamberIndex = packet.ReadInt();

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                trackedItem.physicalItem.setFirearmUpdateOverride(roundType, roundClass, chamberIndex);
                ++ProjectileFirePatch.skipBlast;
                trackedItem.physicalItem.fireFunc(chamberIndex);
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void FlintlockWeaponBurnOffOuter(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = trackedItem.physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = trackedItem.physicalItem.physicalItem as FlintlockWeapon;
                int loadedElementCount = packet.ReadByte();
                asBarrel.LoadedElements = new List<FlintlockBarrel.LoadedElement>();
                for (int i = 0; i < loadedElementCount; ++i)
                {
                    FlintlockBarrel.LoadedElement newElement = new FlintlockBarrel.LoadedElement();
                    newElement.Type = (FlintlockBarrel.LoadedElementType)packet.ReadByte();
                    newElement.Position = packet.ReadFloat();
                }
                asBarrel.LoadedElements[asBarrel.LoadedElements.Count - 1].PowderAmount = packet.ReadInt();
                if (packet.ReadBool() && asFlintlockWeapon.RamRod.GetCurBarrel() != asBarrel)
                {
                    asFlintlockWeapon.RamRod.m_curBarrel = asBarrel;
                }
                FireFlintlockWeaponPatch.num2 = packet.ReadFloat();
                FireFlintlockWeaponPatch.positions = new List<Vector3>();
                FireFlintlockWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireFlintlockWeaponPatch.positions.Add(packet.ReadVector3());
                    FireFlintlockWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireFlintlockWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireFlintlockWeaponPatch.burnSkip;
                asBarrel.BurnOffOuter();
                --FireFlintlockWeaponPatch.burnSkip;
            }
        }

        public static void FlintlockWeaponFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = trackedItem.physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = trackedItem.physicalItem.physicalItem as FlintlockWeapon;
                int loadedElementCount = packet.ReadByte();
                asBarrel.LoadedElements = new List<FlintlockBarrel.LoadedElement>();
                for (int i = 0; i < loadedElementCount; ++i)
                {
                    FlintlockBarrel.LoadedElement newElement = new FlintlockBarrel.LoadedElement();
                    newElement.Type = (FlintlockBarrel.LoadedElementType)packet.ReadByte();
                    newElement.Position = packet.ReadFloat();
                    newElement.PowderAmount = packet.ReadInt();
                }
                if (packet.ReadBool() && asFlintlockWeapon.RamRod.GetCurBarrel() != asBarrel)
                {
                    asFlintlockWeapon.RamRod.m_curBarrel = asBarrel;
                }
                FireFlintlockWeaponPatch.num5 = packet.ReadFloat();
                FireFlintlockWeaponPatch.positions = new List<Vector3>();
                FireFlintlockWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireFlintlockWeaponPatch.positions.Add(packet.ReadVector3());
                    FireFlintlockWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireFlintlockWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireFlintlockWeaponPatch.fireSkip;
                asBarrel.Fire();
                --FireFlintlockWeaponPatch.fireSkip;
            }
        }

        public static void BreakActionWeaponFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int barrelIndex = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                BreakActionWeapon asBAW = trackedItem.physicalItem.physicalItem as BreakActionWeapon;
                if (asBAW != null)
                {
                    FireArmRoundType prevRoundType = asBAW.Barrels[barrelIndex].Chamber.RoundType;
                    asBAW.Barrels[barrelIndex].Chamber.RoundType = roundType;
                    ++ChamberPatch.chamberSkip;
                    asBAW.Barrels[barrelIndex].Chamber.SetRound(roundClass, asBAW.Barrels[barrelIndex].Chamber.transform.position, asBAW.Barrels[barrelIndex].Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    asBAW.Barrels[barrelIndex].Chamber.RoundType = prevRoundType;
                    ++ProjectileFirePatch.skipBlast;
                    asBAW.Fire(barrelIndex, false, 0);
                    --ProjectileFirePatch.skipBlast;
                }
                else
                {
                    Mod.LogError("Received order to fire break action weapon at " + trackedID + " but the item is not a BreakActionWeapon. It is actually: "+(trackedItem.physicalItem.physicalItem.GetType()));
                }
            }
        }

        public static void DerringerFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int barrelIndex = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Derringer asDerringer = trackedItem.physicalItem.physicalItem as Derringer;
                FireArmRoundType prevRoundType = asDerringer.Barrels[barrelIndex].Chamber.RoundType;
                asDerringer.Barrels[barrelIndex].Chamber.RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asDerringer.Barrels[barrelIndex].Chamber.SetRound(roundClass, asDerringer.Barrels[barrelIndex].Chamber.transform.position, asDerringer.Barrels[barrelIndex].Chamber.transform.rotation);
                --ChamberPatch.chamberSkip;
                asDerringer.Barrels[barrelIndex].Chamber.RoundType = prevRoundType;
                asDerringer.m_curBarrel = barrelIndex;
                ++ProjectileFirePatch.skipBlast;
                asDerringer.FireBarrel(barrelIndex);
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void RevolvingShotgunFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                RevolvingShotgun asRS = trackedItem.physicalItem.physicalItem as RevolvingShotgun;
                asRS.CurChamber = curChamber;
                FireArmRoundType prevRoundType = asRS.Chambers[curChamber].RoundType;
                asRS.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asRS.Chambers[curChamber].SetRound(roundClass, asRS.Chambers[curChamber].transform.position, asRS.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asRS.Chambers[curChamber].RoundType = prevRoundType;
                ++ProjectileFirePatch.skipBlast;
                asRS.Fire();
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void RevolverFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Revolver asRevolver = trackedItem.physicalItem.physicalItem as Revolver;
                bool changedOffset = false;
                int oldOffset = 0;
                if (asRevolver.ChamberOffset != 0)
                {
                    changedOffset = true;
                    oldOffset = asRevolver.ChamberOffset;
                    asRevolver.ChamberOffset = 0;
                }
                asRevolver.CurChamber = curChamber;
                FireArmRoundType prevRoundType = asRevolver.Chambers[curChamber].RoundType;
                asRevolver.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asRevolver.Chambers[curChamber].SetRound(roundClass, asRevolver.Chambers[curChamber].transform.position, asRevolver.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asRevolver.Chambers[curChamber].RoundType = prevRoundType;
                if (changedOffset)
                {
                    asRevolver.ChamberOffset = oldOffset;
                }
                ++ProjectileFirePatch.skipBlast;
                asRevolver.Fire();
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void SingleActionRevolverFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                SingleActionRevolver asRevolver = trackedItem.physicalItem.physicalItem as SingleActionRevolver;
                asRevolver.CurChamber = curChamber;
                FireArmRoundType prevRoundType = asRevolver.Cylinder.Chambers[curChamber].RoundType;
                asRevolver.Cylinder.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asRevolver.Cylinder.Chambers[curChamber].SetRound(roundClass, asRevolver.Cylinder.Chambers[curChamber].transform.position, asRevolver.Cylinder.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asRevolver.Cylinder.Chambers[curChamber].RoundType = prevRoundType;
                ++ProjectileFirePatch.skipBlast;
                asRevolver.Fire();
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void StingerLauncherFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireStingerLauncherPatch.targetPos = packet.ReadVector3();
                FireStingerLauncherPatch.position = packet.ReadVector3();
                FireStingerLauncherPatch.rotation = packet.ReadQuaternion();
                FireStingerLauncherPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireStingerLauncherPatch.skip;
                StingerLauncher asStingerLauncher = trackedItem.physicalItem.physicalItem as StingerLauncher;
                asStingerLauncher.m_hasMissile = true;
                ++ProjectileFirePatch.skipBlast;
                asStingerLauncher.Fire();
                --ProjectileFirePatch.skipBlast;
                --FireStingerLauncherPatch.skip;
            }
        }

        public static void GrappleGunFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                GrappleGun asGG = trackedItem.physicalItem.physicalItem as GrappleGun;
                asGG.m_curChamber = curChamber;
                FireArmRoundType prevRoundType = asGG.Chambers[curChamber].RoundType;
                asGG.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].SetRound(roundClass, asGG.Chambers[curChamber].transform.position, asGG.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].RoundType = prevRoundType;
                asGG.Fire();
            }
        }

        public static void HCBReleaseSled(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                float cookedAmount = packet.ReadFloat();
                FireHCBPatch.position = packet.ReadVector3();
                FireHCBPatch.direction = packet.ReadVector3();
                FireHCBPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireHCBPatch.releaseSledSkip;
                HCB asHCB = trackedItem.physicalItem.physicalItem as HCB;
                asHCB.m_cookedAmount = cookedAmount;
                if (!asHCB.Chamber.IsFull)
                {
                    ++ChamberPatch.chamberSkip;
                    asHCB.Chamber.SetRound(FireArmRoundClass.FMJ, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                }
                asHCB.ReleaseSled();
                --FireHCBPatch.releaseSledSkip;
            }
        }

        public static void LeverActionFirearmFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                bool hammer1 = packet.ReadBool();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                LeverActionFirearm asLAF = trackedItem.physicalItem.dataObject as LeverActionFirearm;
                if (hammer1)
                {
                    FireArmRoundType prevRoundType = asLAF.Chamber.RoundType;
                    asLAF.Chamber.RoundType = roundType;
                    ++ChamberPatch.chamberSkip;
                    asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    asLAF.Chamber.RoundType = prevRoundType;
                    asLAF.m_isHammerCocked = true;
                    ++ProjectileFirePatch.skipBlast;
                    asLAF.Fire();
                    --ProjectileFirePatch.skipBlast;
                }
                else
                {
                    bool reCock = false;
                    if (asLAF.IsHammerCocked)
                    {
                        // Temporarily uncock hammer1
                        reCock = true;
                        asLAF.m_isHammerCocked = false;
                    }
                    bool reChamber = false;
                    FireArmRoundClass reChamberClass = FireArmRoundClass.a20AP;
                    if (asLAF.Chamber.GetRound() != null)
                    {
                        // Temporarily unchamber round
                        reChamber = true;
                        reChamberClass = asLAF.Chamber.GetRound().RoundClass;
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber.SetRound(null);
                        --ChamberPatch.chamberSkip;
                    }
                    FireArmRoundType prevRoundType = asLAF.Chamber.RoundType;
                    asLAF.Chamber2.RoundType = roundType;
                    ++ChamberPatch.chamberSkip;
                    asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    asLAF.Chamber2.RoundType = prevRoundType;
                    asLAF.m_isHammerCocked2 = true;
                    ++ProjectileFirePatch.skipBlast;
                    asLAF.Fire();
                    --ProjectileFirePatch.skipBlast;
                    if (reCock)
                    {
                        asLAF.m_isHammerCocked = true;
                    }
                    if (reChamber)
                    {
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber.SetRound(reChamberClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                }
            }
        }

        public static void SosigWeaponFire(Packet packet)
        {
            int trackedID = packet.ReadInt();
            float recoilMult = packet.ReadFloat();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireSosigWeaponPatch.positions = new List<Vector3>();
                FireSosigWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireSosigWeaponPatch.positions.Add(packet.ReadVector3());
                    FireSosigWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireSosigWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                SosigWeaponPlayerInterface asInterface = trackedItem.physicalItem.dataObject as SosigWeaponPlayerInterface;
                if (asInterface.W.m_shotsLeft <= 0)
                {
                    asInterface.W.m_shotsLeft = 1;
                }
                asInterface.W.MechaState = SosigWeapon.SosigWeaponMechaState.ReadyToFire;
                trackedItem.physicalItem.sosigWeaponfireFunc(recoilMult);
            }
        }

        public static void LAPD2019Fire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                int chamberIndex = packet.ReadInt();
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                FireLAPD2019Patch.positions = new List<Vector3>();
                FireLAPD2019Patch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireLAPD2019Patch.positions.Add(packet.ReadVector3());
                    FireLAPD2019Patch.directions.Add(packet.ReadVector3());
                }
                FireLAPD2019Patch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                LAPD2019 asLAPD2019 = trackedItem.physicalItem.physicalItem as LAPD2019;
                asLAPD2019.CurChamber = chamberIndex;
                FireArmRoundType prevRoundType = asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].SetRound(roundClass, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.position, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = prevRoundType;
                ++ProjectileFirePatch.skipBlast;
                ((LAPD2019)trackedItem.physicalItem.physicalItem).Fire();
                --ProjectileFirePatch.skipBlast;
            }
        }
        
        public static void AttachableFirearmFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                bool firedFromInterface = packet.ReadBool();
                FireAttachableFirearmPatch.positions = new List<Vector3>();
                FireAttachableFirearmPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireAttachableFirearmPatch.positions.Add(packet.ReadVector3());
                    FireAttachableFirearmPatch.directions.Add(packet.ReadVector3());
                }
                FireAttachableFirearmPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                trackedItem.physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                trackedItem.physicalItem.attachableFirearmFireFunc(firedFromInterface);
                --ProjectileFirePatch.skipBlast;
            }
        }
        
        public static void IntegratedFirearmFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                FireAttachableFirearmPatch.positions = new List<Vector3>();
                FireAttachableFirearmPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireAttachableFirearmPatch.positions.Add(packet.ReadVector3());
                    FireAttachableFirearmPatch.directions.Add(packet.ReadVector3());
                }
                FireAttachableFirearmPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                trackedItem.physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                trackedItem.physicalItem.attachableFirearmFireFunc(false);
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void LAPD2019LoadBattery(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null && Client.objects[batteryTrackedID].physical != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)trackedItem.physicalItem.physicalItem).LoadBattery((LAPD2019Battery)Client.objects[batteryTrackedID].physical.physical);
                --LAPD2019ActionPatch.loadBatterySkip;
            }
        }

        public static void LAPD2019ExtractBattery(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)trackedItem.physicalItem.physicalItem).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }
        }

        public static void SosigWeaponShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                ++SosigWeaponShatterPatch.skip;
                (trackedItem.physicalItem.physicalItem as SosigWeaponPlayerInterface).W.Shatter();
                --SosigWeaponShatterPatch.skip;
            }
        }

        public static void AutoMeaterFirearmFireShot(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            TrackedAutoMeaterData trackedAutoMeater = Client.objects[trackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null && trackedAutoMeater.physicalAutoMeater != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.FireControl.Firearms[0].FireShot();
                --AutoMeaterFirearmFireShotPatch.skip;
            }
        }

        public static void PlayerDamage(Packet packet)
        {
            float damageMultiplier = packet.ReadFloat();
            bool head = packet.ReadBool();
            Damage damage = packet.ReadDamage();

            Mod.LogInfo("Client received player damage for itself",false);
            GameManager.ProcessPlayerDamage(damageMultiplier, head, damage);
        }

        public static void UberShatterableShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();
            TrackedObjectData trackedObject = Client.objects[trackedID];
            if (trackedObject != null)
            {
                if (trackedObject.physical != null)
                {
                    trackedObject.physical.HandleShatter(null, packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat(), true, 0, packet.ReadBytes(packet.ReadInt()));
                }

                //trackedItem.additionalData = new byte[30];
                //trackedItem.additionalData[0] = 1;
                //trackedItem.additionalData[1] = 1;
                //for (int i = 2, j = 0; i < 30; ++i, ++j) 
                //{
                //    trackedItem.additionalData[i] = packet.readableBuffer[packet.readPos + j];
                //}

                //if (trackedItem.physicalItem != null)
                //{
                //    ++UberShatterableShatterPatch.skip;
                //    (trackedItem.physicalItem.dataObject as UberShatterable).Shatter(packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat());
                //    --UberShatterableShatterPatch.skip;
                //}
            }
        }

        public static void SosigPickUpItem(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if(trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = itemTrackedID;

                if (trackedSosig.physicalSosig != null)
                {
                    if (Client.objects[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at "+itemTrackedID+" is missing item data!");
                    }
                    else if (Client.objects[itemTrackedID].physical == null)
                    {
                        (Client.objects[itemTrackedID] as TrackedItemData).toPutInSosigInventory = new int[] { sosigTrackedID, primaryHand ? 0 : 1 };
                    }
                    else
                    {
                        ++SosigPickUpPatch.skip;
                        if (primaryHand)
                        {
                            trackedSosig.physicalSosig.physicalSosig.Hand_Primary.PickUp(Client.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        }
                        else
                        {
                            trackedSosig.physicalSosig.physicalSosig.Hand_Secondary.PickUp(Client.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        }
                        --SosigPickUpPatch.skip;
                    }
                }
            }
        }

        public static void SosigPlaceItemIn(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if(trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = itemTrackedID;

                if (trackedSosig.physicalSosig != null)
                {
                    if (Client.objects[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (Client.objects[itemTrackedID].physical == null)
                    {
                        (Client.objects[itemTrackedID] as TrackedItemData).toPutInSosigInventory = new int[] { sosigTrackedID, slotIndex + 2 };
                    }
                    else
                    {
                        ++SosigPlaceObjectInPatch.skip;
                        trackedSosig.physicalSosig.physicalSosig.Inventory.Slots[slotIndex].PlaceObjectIn(Client.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        --SosigPlaceObjectInPatch.skip;
                    }
                }
            }
        }

        public static void SosigDropSlot(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if(trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = -1;

                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigSlotDetachPatch.skip;
                    trackedSosig.physicalSosig.physicalSosig.Inventory.Slots[slotIndex].DetachHeldObject();
                    --SosigSlotDetachPatch.skip;
                }
            }
        }

        public static void SosigHandDrop(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if(trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = -1;

                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigHandDropPatch.skip;
                    if (primaryHand)
                    {
                        trackedSosig.physicalSosig.physicalSosig.Hand_Primary.DropHeldObject();
                    }
                    else
                    {
                        trackedSosig.physicalSosig.physicalSosig.Hand_Secondary.DropHeldObject();
                    }
                    --SosigHandDropPatch.skip;
                }
            }
        }

        public static void SosigConfigure(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;

                if (trackedSosig.physicalSosig != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalSosig.physicalSosig.Configure(config);
                }
            }
        }

        public static void SosigLinkRegisterWearable(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if(trackedSosig.physicalSosig != null)
                    {
                        foreach(SosigLink link in trackedSosig.physicalSosig.physicalSosig.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while(trackedSosig.wearables.Count <= linkIndex)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                }
                trackedSosig.wearables[linkIndex].Add(wearableID);

                if (trackedSosig.physicalSosig != null)
                {
                    AnvilManager.Run(trackedSosig.EquipWearable(linkIndex, wearableID, true));
                }
            }
        }

        public static void SosigLinkDeRegisterWearable(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables != null)
                {
                    if (trackedSosig.physicalSosig != null)
                    {
                        for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                        {
                            if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                            {
                                trackedSosig.wearables[linkIndex].RemoveAt(i);
                                if (trackedSosig.physicalSosig != null)
                                {
                                    ++SosigLinkActionPatch.skipDeRegisterWearable;
                                    trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].DeRegisterWearable(trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].m_wearables[i]);
                                    --SosigLinkActionPatch.skipDeRegisterWearable;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        trackedSosig.wearables[linkIndex].Remove(wearableID);
                    }
                }
            }
        }

        public static void SosigSetIFF(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalSosig.physicalSosig.SetIFF(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigSetOriginalIFF(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalSosig.physicalSosig.SetOriginalIFFTeam(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigLinkDamage(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == Client.singleton.ID)
                {
                    if (trackedSosig.physicalSosig != null &&
                        trackedSosig.physicalSosig.physicalSosig.Links[linkIndex] != null &&
                        !trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].IsExploded)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].Damage(damage);
                        --SosigLinkDamagePatch.skip;
                    }
                }
                else // We are not controller anymore (if we received this packet, it means we were to the server when they sent it to us)
                {
                    // Bounce
                    ClientSend.SosigLinkDamage(packet);
                }
            }
            else // Not sure if this case is possible, but if happens, let server consume it instead of us just in case
            {
                // Bounce
                ClientSend.SosigLinkDamage(packet);
            }
        }

        public static void AutoMeaterDamage(Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalAutoMeater != null)
                    {
                        ++AutoMeaterDamagePatch.skip;
                        trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.Damage(damage);
                        --AutoMeaterDamagePatch.skip;
                    }
                }
                else
                {
                    ClientSend.AutoMeaterDamage(packet);
                }
            }
            else
            {
                ClientSend.AutoMeaterDamage(packet);
            }
        }

        public static void AutoMeaterHitZoneDamage(Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            byte type = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalAutoMeater != null)
                    {
                        ++AutoMeaterHitZoneDamagePatch.skip;
                        trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)type].Damage(damage);
                        --AutoMeaterHitZoneDamagePatch.skip;
                    }
                }
                else
                {
                    ClientSend.AutoMeaterHitZoneDamage(packet);
                }
            }
            else
            {
                ClientSend.AutoMeaterHitZoneDamage(packet);
            }
        }

        public static void EncryptionDamage(Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedEncryptionData trackedEncryption = Client.objects[encryptionTrackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == Client.singleton.ID)
                {
                    if (trackedEncryption.physicalEncryption != null)
                    {
                        ++EncryptionDamagePatch.skip;
                        trackedEncryption.physicalEncryption.physicalEncryption.Damage(damage);
                        --EncryptionDamagePatch.skip;
                    }
                }
                else
                {
                    ClientSend.EncryptionDamage(packet);
                }
            }
            else
            {
                ClientSend.EncryptionDamage(packet);
            }
        }

        public static void EncryptionSubDamage(Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedEncryptionData trackedEncryption = Client.objects[encryptionTrackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == GameManager.ID)
                {
                    if (trackedEncryption.physicalEncryption != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigWeaponDamage(Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData trackedItem = Client.objects[sosigWeaponTrackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++SosigWeaponDamagePatch.skip;
                        (trackedItem.physicalItem.physicalItem as SosigWeaponPlayerInterface).W.Damage(damage);
                        --SosigWeaponDamagePatch.skip;
                    }
                }
            }
        }

        public static void RemoteMissileDamage(Packet packet)
        {
            int RMLTrackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[RMLTrackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        RemoteMissile remoteMissile = (trackedItem.physicalItem.physicalItem as RemoteMissileLauncher).m_missile;
                        if (remoteMissile != null)
                        {
                            ++RemoteMissileDamagePatch.skip;
                            remoteMissile.Damage(packet.ReadDamage());
                            --RemoteMissileDamagePatch.skip;
                        }
                    }
                }
            }
        }

        public static void StingerMissileDamage(Packet packet)
        {
            int SLTrackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[SLTrackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        StingerMissile missile = trackedItem.physicalItem.stingerMissile;
                        if (missile != null)
                        {
                            ++StingerMissileDamagePatch.skip;
                            missile.Damage(packet.ReadDamage());
                            --StingerMissileDamagePatch.skip;
                        }
                    }
                }
            }
        }

        public static void SosigWearableDamage(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                byte linkIndex = packet.ReadByte();
                byte wearableIndex = packet.ReadByte();
                Damage damage = packet.ReadDamage();

                if (trackedSosig.controller == Client.singleton.ID)
                {
                    if (trackedSosig.physicalSosig != null &&
                        trackedSosig.physicalSosig.physicalSosig.Links[linkIndex] != null &&
                        !trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].IsExploded)
                    {
                        ++SosigWearableDamagePatch.skip;
                        trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].m_wearables[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
                else
                {
                    ClientSend.SosigWearableDamage(packet);
                }
            }
            else
            {
                // TODO: Review: Maybe apply this bounce mechanic to other damage packets, not just sosigs?
                ClientSend.SosigWearableDamage(packet);
            }
        }

        public static void SosigSetBodyState(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigPatch.sosigSetBodyStateSkip;
                    trackedSosig.physicalSosig.physicalSosig.SetBodyState(bodyState);
                    --SosigPatch.sosigSetBodyStateSkip;
                }
            }
        }

        public static void SosigDamageData(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.controller != Client.singleton.ID && trackedSosig.physicalSosig != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalSosig.physicalSosig;
                    physicalSosig.m_isStunned = packet.ReadBool();
                    physicalSosig.m_stunTimeLeft = packet.ReadFloat();
                    physicalSosig.BodyState = (Sosig.SosigBodyState)packet.ReadByte();
                    physicalSosig.m_isOnOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.autoTraverseOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.enabled = packet.ReadBool();
                    List<CharacterJoint> joints = physicalSosig.m_joints;
                    byte jointCount = packet.ReadByte();
                    for (int i = 0; i < joints.Count; ++i)
                    {
                        if (joints[i] != null)
                        {
                            SoftJointLimit softJointLimit = joints[i].lowTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].lowTwistLimit = softJointLimit;
                            softJointLimit = joints[i].highTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].highTwistLimit = softJointLimit;
                            softJointLimit = joints[i].swing1Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing1Limit = softJointLimit;
                            softJointLimit = joints[i].swing2Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing2Limit = softJointLimit;
                        }
                    }
                    physicalSosig.m_isCountingDownToStagger = packet.ReadBool();
                    physicalSosig.m_staggerAmountToApply = packet.ReadFloat();
                    physicalSosig.m_recoveringFromBallisticState = packet.ReadBool();
                    physicalSosig.m_recoveryFromBallisticLerp = packet.ReadFloat();
                    physicalSosig.m_tickDownToWrithe = packet.ReadFloat();
                    physicalSosig.m_recoveryFromBallisticTick = packet.ReadFloat();
                    physicalSosig.m_lastIFFDamageSource = packet.ReadByte();
                    physicalSosig.m_diedFromClass = (Damage.DamageClass)packet.ReadByte();
                    physicalSosig.m_isBlinded = packet.ReadBool();
                    physicalSosig.m_blindTime = packet.ReadFloat();
                    physicalSosig.m_isFrozen = packet.ReadBool();
                    physicalSosig.m_debuffTime_Freeze = packet.ReadFloat();
                    physicalSosig.m_receivedHeadShot = packet.ReadBool();
                    physicalSosig.m_timeSinceLastDamage = packet.ReadFloat();
                    physicalSosig.m_isConfused = packet.ReadBool();
                    physicalSosig.m_confusedTime = packet.ReadFloat();
                    physicalSosig.m_storedShudder = packet.ReadFloat();
                }
            }
        }

        public static void EncryptionDamageData(Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Client.objects[encryptionTrackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller != Client.singleton.ID && trackedEncryption.physicalEncryption != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.m_numHitsLeft = packet.ReadInt();
                }
            }
        }

        public static void AutoMeaterHitZoneDamageData(Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller != Client.singleton.ID && trackedAutoMeater.physicalAutoMeater != null)
                {
                    AutoMeaterHitZone hitZone = trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)packet.ReadByte()];
                    hitZone.ArmorThreshold = packet.ReadFloat();
                    hitZone.LifeUntilFailure = packet.ReadFloat();
                    if (packet.ReadBool()) // Destroyed
                    {
                        hitZone.BlowUp();
                    }
                }
            }
        }

        public static void SosigLinkExplodes(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalSosig != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }
        }

        public static void SosigDies(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalSosig != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigPatch.sosigDiesSkip;
                    trackedSosig.physicalSosig.physicalSosig.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigPatch.sosigDiesSkip;
                }
            }
        }

        public static void SosigClear(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigPatch.sosigClearSkip;
                    trackedSosig.physicalSosig.physicalSosig.ClearSosig();
                    --SosigPatch.sosigClearSkip;
                }
            }
        }

        public static void PlaySosigFootStepSound(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = trackedSosig.physicalSosig.physicalSosig.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }
        }

        public static void SosigSpeakState(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnWander);
                        break;
                    case Sosig.SosigOrder.Investigate:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnInvestigate);
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnSearchingForGuns);
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnTakingCover);
                        break;
                    case Sosig.SosigOrder.Wander:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnWander);
                        break;
                    case Sosig.SosigOrder.Assault:
                        trackedSosig.physicalSosig.physicalSosig.Speak_State(trackedSosig.physicalSosig.physicalSosig.Speech.OnAssault);
                        break;
                }
            }
        }

        public static void SosigSetCurrentOrder(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();
            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.currentOrder = currentOrder;
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        trackedSosig.guardPoint = packet.ReadVector3();
                        trackedSosig.guardDir = packet.ReadVector3();
                        trackedSosig.hardGuard = packet.ReadBool();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.CommandGuardPoint(trackedSosig.guardPoint, trackedSosig.hardGuard);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.m_guardDominantDirection = trackedSosig.guardDir;
                        }
                        break;
                    case Sosig.SosigOrder.Skirmish:
                        trackedSosig.skirmishPoint = packet.ReadVector3();
                        trackedSosig.pathToPoint = packet.ReadVector3();
                        trackedSosig.assaultPoint = packet.ReadVector3();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.m_skirmishPoint = trackedSosig.skirmishPoint;
                            trackedSosig.physicalSosig.physicalSosig.m_pathToPoint = trackedSosig.pathToPoint;
                            trackedSosig.physicalSosig.physicalSosig.m_assaultPoint = trackedSosig.assaultPoint;
                            trackedSosig.physicalSosig.physicalSosig.m_faceTowards = trackedSosig.faceTowards;
                        }
                        break;
                    case Sosig.SosigOrder.Investigate:
                        trackedSosig.guardPoint = packet.ReadVector3();
                        trackedSosig.hardGuard = packet.ReadBool();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.UpdateGuardPoint(trackedSosig.guardPoint);
                            trackedSosig.physicalSosig.physicalSosig.m_hardGuard = trackedSosig.hardGuard;
                            trackedSosig.physicalSosig.physicalSosig.m_faceTowards = trackedSosig.faceTowards;
                        }
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                    case Sosig.SosigOrder.Wander:
                        trackedSosig.wanderPoint = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.m_wanderPoint = trackedSosig.wanderPoint;
                        }
                        break;
                    case Sosig.SosigOrder.Assault:
                        trackedSosig.assaultPoint = packet.ReadVector3();
                        trackedSosig.assaultSpeed = (Sosig.SosigMoveSpeed)packet.ReadByte();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.CommandAssaultPoint(trackedSosig.assaultPoint);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.m_faceTowards = trackedSosig.faceTowards;
                            trackedSosig.physicalSosig.physicalSosig.SetAssaultSpeed(trackedSosig.assaultSpeed);
                        }
                        break;
                    case Sosig.SosigOrder.Idle:
                        trackedSosig.idleToPoint = packet.ReadVector3();
                        trackedSosig.idleDominantDir = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.CommandIdle(trackedSosig.idleToPoint, trackedSosig.idleDominantDir);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                        }
                        break;
                    case Sosig.SosigOrder.PathTo:
                        trackedSosig.pathToPoint = packet.ReadVector3();
                        trackedSosig.pathToLookDir = packet.ReadVector3();
                        if (trackedSosig.physicalSosig != null)
                        {
                            ++SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                            --SosigPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalSosig.physicalSosig.m_pathToPoint = trackedSosig.pathToPoint;
                            trackedSosig.physicalSosig.physicalSosig.m_pathToLookDir = trackedSosig.pathToLookDir;
                        }
                        break;
                    default:
                        ++SosigPatch.sosigSetCurrentOrderSkip;
                        trackedSosig.physicalSosig.physicalSosig.SetCurrentOrder(currentOrder);
                        --SosigPatch.sosigSetCurrentOrderSkip;
                        break;
                }
            }
        }

        public static void SosigVaporize(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigPatch.sosigVaporizeSkip;
                trackedSosig.physicalSosig.physicalSosig.Vaporize(trackedSosig.physicalSosig.physicalSosig.DamageFX_Vaporize, iff);
                --SosigPatch.sosigVaporizeSkip;
            }
        }

        public static void SosigLinkBreak(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }
        }

        public static void SosigLinkSever(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].SeverJoint((Damage.DamageClass)damClass, isPullApart);
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }
        }

        public static void SosigRequestHitDecal(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                if (trackedSosig.physicalSosig.physicalSosig.Links[linkIndex] != null)
                {
                    ++SosigPatch.sosigRequestHitDecalSkip;
                    trackedSosig.physicalSosig.physicalSosig.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalSosig.physicalSosig.Links[linkIndex]);
                    --SosigPatch.sosigRequestHitDecalSkip;
                }
            }
        }

        public static void RequestUpToDateObjects(Packet packet)
        {
            // Only send requested up to date objects if not currently loading,
            // because even if we send the most up to date now they will be destroyed by the loading
            // This request was only made to us because the server thought our scene/instance to be our destination one but we are not there yet
            bool instantiateOnReceive = packet.ReadBool();
            int forClient = packet.ReadInt();
            if (!GameManager.sceneLoading)
            {
                ClientSend.UpToDateObjects(instantiateOnReceive, forClient);
            }
            else
            {
                ClientSend.DoneSendingUpdaToDateObjects(forClient);
            }
        }

        public static void AddTNHInstance(Packet packet)
        {
            GameManager.AddTNHInstance(packet.ReadTNHInstance());
        }

        public static void AddInstance(Packet packet)
        {
            GameManager.AddInstance(packet.ReadInt());
        }

        public static void AddTNHCurrentlyPlaying(Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            Mod.LogInfo("ClientHandle: Received AddTNHCurrentlyPlaying for instance: " + instance + " to add " + ID, false);
            if (GameManager.TNHInstances == null || !GameManager.TNHInstances.ContainsKey(instance))
            {
                Mod.LogError("ClientHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                GameManager.TNHInstances[instance].AddCurrentlyPlaying(false, ID);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance currentInstance))
            {
                currentInstance.RemoveCurrentlyPlaying(false, ID);
            }
        }

        public static void SetTNHProgression(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].progressionTypeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.progressionSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Progression(i);
                GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)i;
                --TNH_UIManagerPatch.progressionSkip;
            }
        }

        public static void SetTNHEquipment(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].equipmentModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.equipmentSkip;
                Mod.currentTNHUIManager.OBS_EquipmentMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_EquipmentMode(i);
                GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)i;
                --TNH_UIManagerPatch.equipmentSkip;
            }
        }

        public static void SetTNHHealthMode(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].healthModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthModeSkip;
                Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMode(i);
                GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)i;
                --TNH_UIManagerPatch.healthModeSkip;
            }
        }

        public static void SetTNHTargetMode(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].targetModeSetting = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.targetSkip;
                Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_TargetMode(i);
                GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)i;
                --TNH_UIManagerPatch.targetSkip;
            }
        }

        public static void SetTNHAIDifficulty(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].AIDifficultyModifier = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.AIDifficultySkip;
                Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIDifficulty(i);
                GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)i;
                --TNH_UIManagerPatch.AIDifficultySkip;
            }
        }

        public static void SetTNHRadarMode(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].radarModeModifier = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.radarSkip;
                Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIRadarMode(i);
                GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)i;
                --TNH_UIManagerPatch.radarSkip;
            }
        }

        public static void SetTNHItemSpawnerMode(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].itemSpawnerMode = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.itemSpawnerSkip;
                Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_ItemSpawner(i);
                GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)i;
                --TNH_UIManagerPatch.itemSpawnerSkip;
            }
        }

        public static void SetTNHBackpackMode(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].backpackMode = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.backpackSkip;
                Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Backpack(i);
                GM.TNHOptions.BackpackMode = (TNH_BackpackMode)i;
                --TNH_UIManagerPatch.backpackSkip;
            }
        }

        public static void SetTNHHealthMult(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].healthMult = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthMultSkip;
                Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMult(i);
                GM.TNHOptions.HealthMult = (TNH_HealthMult)i;
                --TNH_UIManagerPatch.healthMultSkip;
            }
        }

        public static void SetTNHSosigGunReload(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].sosiggunShakeReloading = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.sosigGunReloadSkip;
                Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_SosiggunShakeReloading(i);
                GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)i;
                --TNH_UIManagerPatch.sosigGunReloadSkip;
            }
        }

        public static void SetTNHSeed(Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();

            GameManager.TNHInstances[instance].TNHSeed = i;

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.seedSkip;
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i + 1);
                Mod.currentTNHUIManager.SetOBS_RunSeed(i);
                GM.TNHOptions.TNHSeed = i;
                --TNH_UIManagerPatch.seedSkip;
            }
        }

        public static void SetTNHLevelID(Packet packet)
        {
            string levelID = packet.ReadString();
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.levelID = levelID;

                if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
                {
                    // Find level
                    bool found = false;
                    for (int i = 0; i < Mod.currentTNHUIManager.Levels.Count; ++i)
                    {
                        if (Mod.currentTNHUIManager.Levels[i].LevelID.Equals(levelID))
                        {
                            found = true;
                            Mod.currentTNHUIManager.m_currentLevelIndex = i;
                            Mod.currentTNHUIManager.CurLevelID = levelID;
                            Mod.currentTNHUIManager.UpdateLevelSelectDisplayAndLoader();
                            Mod.currentTNHUIManager.UpdateTableBasedOnOptions();
                            Mod.currentTNHUIManager.PlayButtonSound(2);
                            Mod.currentTNHSceneLoader.gameObject.SetActive(true);
                            break;
                        }
                    }
                    if (!found)
                    {
                        Mod.currentTNHSceneLoader.gameObject.SetActive(false);
                        Mod.LogError("Missing TNH level: " + levelID + ", you will not be able to play in this instance if the game is started!");
                    }
                }
            }
        }

        public static void SetTNHController(Packet packet)
        {
            int instance = packet.ReadInt();
            int newController = packet.ReadInt();

            Mod.LogInfo("Client received order to set TNH instance: " + instance + " controller to: " + newController, false);
            // The instance may not exist anymore if we were the last one in there and we left between
            // server sending us order to set TNH controller and receiving it
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance i))
            {
                i.controller = newController;

                if(i.manager != null)
                {
                    // We are in control, the instance is not init yet but the TNH_Manager attempted to
                    if (newController == GameManager.ID && i.phase == TNH_Phase.StartUp && Mod.currentTNHInstance.manager.m_hasInit)
                    {
                        // Init
                        i.manager.SetPhase(TNH_Phase.Take);
                    }
                }
            }
        }

        public static void TNHPlayerDied(Packet packet)
        {
            int instance = packet.ReadInt();
            int ID = packet.ReadInt();

            // Process dead
            TNHInstance TNHinstance = GameManager.TNHInstances[instance];
            TNHinstance.dead.Add(ID);
            bool allDead = true;
            for (int i = 0; i < TNHinstance.currentlyPlaying.Count; ++i)
            {
                if (!TNHinstance.dead.Contains(TNHinstance.currentlyPlaying[i]))
                {
                    allDead = false;
                    break;
                }
            }
            if (allDead)
            {
                // Set visibility of all of the previously dead players
                foreach (int playerID in TNHinstance.dead)
                {
                    if (GameManager.players.TryGetValue(playerID, out PlayerManager player))
                    {
                        player.SetVisible(true);
                    }
                }

                TNHinstance.Reset();
            }

            // Set player visibility if still necessary
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentlyPlayingTNH)
            {
                if (allDead)
                {
                    Mod.currentTNHInstance.manager.m_patrolSquads.Clear();
                }
                else
                {
                    if (GameManager.players.TryGetValue(ID, out PlayerManager player))
                    {
                        player.SetVisible(false);

                        if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null && Mod.currentTNHInstance.manager.TAHReticle != null && player.reticleContact != null)
                        {
                            for (int i = 0; i < Mod.currentTNHInstance.manager.TAHReticle.Contacts.Count; ++i)
                            {
                                if (Mod.currentTNHInstance.manager.TAHReticle.Contacts[i] == player.reticleContact)
                                {
                                    GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                    UnityEngine.Object.Destroy(Mod.currentTNHInstance.manager.TAHReticle.Contacts[i].gameObject);
                                    Mod.currentTNHInstance.manager.TAHReticle.Contacts.RemoveAt(i);
                                    player.reticleContact = null;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void TNHAddTokens(Packet packet)
        {
            int instance = packet.ReadInt();
            int amount = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance currentInstance))
            {
                currentInstance.tokenCount += amount;

                // Implies we are in-game in this instance 
                if (currentInstance.manager != null && !currentInstance.dead.Contains(GameManager.ID))
                {
                    ++TNH_ManagerPatch.addTokensSkip;
                    currentInstance.manager.AddTokens(amount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
            }
        }

        public static void AutoMeaterSetState(Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte state = packet.ReadByte();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[trackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null && trackedAutoMeater.physicalAutoMeater != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.SetState((AutoMeater.AutoMeaterState)state);
                --AutoMeaterSetStatePatch.skip;
            }
        }

        public static void AutoMeaterSetBladesActive(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[trackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null && trackedAutoMeater.physicalAutoMeater != null)
            {
                if (active)
                {
                    for (int i = 0; i < trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.Blades[i].Reactivate();
                    }
                }
                else
                {
                    for (int i = 0; i < trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.Blades[i].ShutDown();
                    }
                }
            }
        }

        public static void AutoMeaterFirearmFireAtWill(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int firearmIndex = packet.ReadInt();
            bool fireAtWill = packet.ReadBool();
            float dist = packet.ReadFloat();

            TrackedAutoMeaterData trackedAutoMeater = Client.objects[trackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null && trackedAutoMeater.physicalAutoMeater != null)
            {
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }
        }

        public static void TNHSosigKill(Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                TrackedSosigData trackedSosig = Client.objects[trackedID] as TrackedSosigData;
                if (trackedSosig != null && trackedSosig.physicalSosig != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.currentTNHInstance.manager.OnSosigKill(trackedSosig.physicalSosig.physicalSosig);
                    --TNH_ManagerPatch.sosigKillSkip;
                }
            }
        }

        public static void TNHHoldPointSystemNode(Packet packet)
        {
            int instance = packet.ReadInt();
            int levelIndex = packet.ReadInt();
            int holdPointIndex = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdPointIndex;
                actualInstance.level = levelIndex;

                if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.SetLevel(levelIndex);
                }
            }
        }

        public static void TNHHoldBeginChallenge(Packet packet)
        {
            int instance = packet.ReadInt();
            bool fromController = packet.ReadBool();
            Mod.LogInfo("TNHHoldBeginChallenge client handle", false);
            if (fromController)
            {
                if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
                {
                    actualInstance.holdOngoing = true;
                    Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                    if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                    {
                        // Begin hold on our side
                        ++TNH_HoldPointPatch.beginHoldSendSkip;
                        Mod.currentTNHInstance.manager.m_curHoldPoint.m_systemNode.m_hasActivated = true;
                        Mod.currentTNHInstance.manager.m_curHoldPoint.m_systemNode.m_hasInitiatedHold = true;
                        Mod.currentTNHInstance.manager.m_curHoldPoint.BeginHoldChallenge();
                        --TNH_HoldPointPatch.beginHoldSendSkip;

                        // TP to hold point
                        if (!actualInstance.dead.Contains(GameManager.ID) || Mod.TNHOnDeathSpectate)
                        {
                            GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.m_curHoldPoint.SpawnPoint_SystemNode.position, true);
                        }
                    }
                }
            }
            else if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                if (actualInstance.controller == GameManager.ID)
                {
                    // We received order to begin hold and we are the controller, begin it
                    Mod.currentTNHInstance.manager.m_curHoldPoint.m_systemNode.m_hasActivated = true;
                    Mod.currentTNHInstance.manager.m_curHoldPoint.m_systemNode.m_hasInitiatedHold = true;
                    Mod.currentTNHInstance.manager.m_curHoldPoint.BeginHoldChallenge();

                    // TP to point since we are not the one who started the hold
                    if (!actualInstance.dead.Contains(GameManager.ID) || Mod.TNHOnDeathSpectate)
                    {
                        GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.m_curHoldPoint.SpawnPoint_SystemNode.position, true);
                    }
                }
            }
        }

        public static void ShatterableCrateSetHoldingHealth(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                trackedItem.additionalData[3] = 1;

                if (trackedItem.physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                    trackedItem.physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                }
            }
        }

        public static void ShatterableCrateSetHoldingToken(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                trackedItem.additionalData[4] = 1;

                if (trackedItem.physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                    trackedItem.physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                }
            }
        }

        public static void ShatterableCrateDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++TNH_ShatterableCrateDamagePatch.skip;
                        trackedItem.physicalItem.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
                        --TNH_ShatterableCrateDamagePatch.skip;
                    }
                }
                else
                {
                    ClientSend.ShatterableCrateDamage(packet);
                }
            }
        }

        public static void ShatterableCrateDestroy(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.physicalItem != null)
            {
                TNH_ShatterableCrate crateScript = trackedItem.physicalItem.GetComponentInChildren<TNH_ShatterableCrate>();
                if (crateScript == null)
                {
                    Mod.LogError("Received order to destroy shatterable crate for which we have physObj but it has not crate script!");
                }
                else
                {
                    ++TNH_ShatterableCrateDestroyPatch.skip;
                    crateScript.Destroy(packet.ReadDamage());
                    --TNH_ShatterableCrateDestroyPatch.skip;
                }
            }
        }

        public static void TNHSetLevel(Packet packet)
        {
            int instance = packet.ReadInt();
            int level = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.level = level;

                if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.level = level;
                    actualInstance.manager.m_level = level;
                    actualInstance.manager.SetLevel(level);
                }
            }
        }

        public static void TNHSetPhaseTake(Packet packet)
        {
            int instance = packet.ReadInt();
            int holdIndex = packet.ReadInt();
            int activeSupplyCount = packet.ReadInt();
            List<int> activeIndices = new List<int>();
            for (int i = 0; i < activeSupplyCount; ++i)
            {
                activeIndices.Add(packet.ReadInt());
            }
            bool init = packet.ReadBool();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdIndex;
                actualInstance.phase = TNH_Phase.Take;
                actualInstance.activeSupplyPointIndices = activeIndices;

                if (!init && actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.SetPhase_Take();
                }
            }
        }

        public static void TNHSetPhaseHold(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.phase = TNH_Phase.Hold;

                if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.SetPhase_Hold();
                }
            }
        }

        public static void TNHHoldCompletePhase(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Transition;
                TNHInstance.raisedBarriers = null;
                TNHInstance.raisedBarrierPrefabIndices = null;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.CompletePhase();
                }
            }
        }

        public static void TNHSetPhaseComplete(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                // Update state if necessary
                if (actualInstance.manager != null)
                {
                    Mod.currentTNHInstance.manager.SetPhase_Completed();
                }

                // Update data
                actualInstance.Reset();
            }
        }

        public static void TNHSetPhase(Packet packet)
        {
            int instance = packet.ReadInt();
            short p = packet.ReadShort();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                // Update data
                actualInstance.phase = (TNH_Phase)p;

                // Update state
                if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.Phase = (TNH_Phase)p;
                }
            }
        }

        public static void EncryptionRespawnSubTarg(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physicalEncryption != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].SetActive(true);
                    ++trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft;
                }
            }
        }

        public static void EncryptionRespawnSubTargGeo(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargGeosActive[index] = true;
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physicalEncryption != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargGeo[index].gameObject.SetActive(true);
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].SetActive(true);
                    ++trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft;
                }
            }
        }

        public static void EncryptionSpawnGrowth(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physicalEncryption != null)
                {
                    Vector3 forward = point - trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.position;

                    ++EncryptionSpawnGrowthPatch.skip;
                    trackedEncryption.physicalEncryption.physicalEncryption.SpawnGrowth(index, point);
                    --EncryptionSpawnGrowthPatch.skip;

                    trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.localScale = new Vector3(0.2f, 0.2f, forward.magnitude * trackedEncryption.physicalEncryption.physicalEncryption.TendrilFloats[index]);
                }
            }
        }

        public static void EncryptionInit(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int indexCount = packet.ReadInt();
            List<int> indices = new List<int>();
            for (int i = 0; i < indexCount; i++)
            {
                indices.Add(packet.ReadInt());
            }
            int pointCount = packet.ReadInt();
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < pointCount; i++)
            {
                points.Add(packet.ReadVector3());
            }
            Vector3 initialPos = packet.ReadVector3();
            int numHitsLeft = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (pointCount > 0)
                {
                    for (int i = 0; i < indexCount; ++i)
                    {
                        trackedEncryption.subTargsActive[indices[i]] = true;
                        trackedEncryption.subTargPos[indices[i]] = points[i];
                    }

                    if (trackedEncryption.physicalEncryption != null)
                    {
                        ++EncryptionSpawnGrowthPatch.skip;
                        for (int i = 0; i < indexCount; ++i)
                        {
                            Vector3 forward = points[i] - trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[indices[i]].transform.position;
                            trackedEncryption.physicalEncryption.physicalEncryption.SpawnGrowth(indices[i], points[i]);
                            trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[indices[i]].transform.localScale = new Vector3(0.2f, 0.2f, forward.magnitude * trackedEncryption.physicalEncryption.physicalEncryption.TendrilFloats[indices[i]]);
                        }
                        --EncryptionSpawnGrowthPatch.skip;
                    }
                }
                else
                {
                    for (int i = 0; i < indexCount; ++i)
                    {
                        trackedEncryption.subTargsActive[indices[i]] = true;
                    }

                    if (trackedEncryption.physicalEncryption != null)
                    {
                        ++EncryptionSpawnGrowthPatch.skip;
                        for (int i = 0; i < indexCount; ++i)
                        {
                            trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[indices[i]].SetActive(true);
                        }
                        --EncryptionSpawnGrowthPatch.skip;

                        trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft = indexCount;
                    }
                }

                trackedEncryption.initialPos = initialPos;
                if (trackedEncryption.physical != null && trackedEncryption.physicalEncryption.physicalEncryption.UseReturnToSpawnForce)
                {
                    if (trackedEncryption.physicalEncryption.physicalEncryption.m_returnToSpawnLine != null)
                    {
                        GameObject.Destroy(trackedEncryption.physicalEncryption.physicalEncryption.m_returnToSpawnLine.gameObject);
                    }
                    trackedEncryption.physicalEncryption.physicalEncryption.initialPos = initialPos;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(trackedEncryption.physicalEncryption.physicalEncryption.ReturnToSpawnLineGO, trackedEncryption.physicalEncryption.transform.position, Quaternion.identity);
                    trackedEncryption.physicalEncryption.physicalEncryption.m_returnToSpawnLine = gameObject.transform;
                    trackedEncryption.physicalEncryption.physicalEncryption.UpdateLine();
                }

                trackedEncryption.numHitsLeft = numHitsLeft;
                if (trackedEncryption.physical != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.m_numHitsLeft = numHitsLeft;

                    if (trackedEncryption.physicalEncryption.physicalEncryption.UsesMultipleDisplay
                        && trackedEncryption.physicalEncryption.physicalEncryption.DisplayList.Count > numHitsLeft
                        && trackedEncryption.physicalEncryption.physicalEncryption.DisplayList[numHitsLeft] != null)
                    {
                        ++EncryptionPatch.updateDisplaySkip;
                        trackedEncryption.physicalEncryption.physicalEncryption.UpdateDisplay();
                        --EncryptionPatch.updateDisplaySkip;
                    }
                }
            }
        }

        public static void EncryptionResetGrowth(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (trackedEncryption.physicalEncryption != null)
                {
                    Vector3 forward = point - trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.position;

                    ++EncryptionResetGrowthPatch.skip;
                    trackedEncryption.physicalEncryption.physicalEncryption.ResetGrowth(index, point);
                    --EncryptionResetGrowthPatch.skip;
                }
            }
        }

        public static void EncryptionDisableSubtarg(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = false;
                if(trackedEncryption.subTargGeosActive != null && trackedEncryption.subTargGeosActive.Length > index)
                {
                    trackedEncryption.subTargGeosActive[index] = false;
                }

                if (trackedEncryption.physicalEncryption != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].SetActive(false);
                    if (trackedEncryption.physicalEncryption.physicalEncryption.UsesRegeneratingSubtargs)
                    {
                        trackedEncryption.physicalEncryption.physicalEncryption.SubTargGeo[index].gameObject.SetActive(false);
                    }
                    if (trackedEncryption.physicalEncryption.physicalEncryption.Tendrils != null 
                        && trackedEncryption.physicalEncryption.physicalEncryption.Tendrils.Count > index)
                    {
                        trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].SetActive(false);
                    }
                    --trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft;
                }
            }
        }

        public static void InitTNHInstances(Packet packet)
        {
            int count = packet.ReadInt();

            for(int i=0; i < count; ++i)
            {
                TNHInstance TNHInstance = packet.ReadTNHInstance(true);

                GameManager.TNHInstances.Add(TNHInstance.instance, TNHInstance);

                if((TNHInstance.letPeopleJoin || TNHInstance.currentlyPlaying.Count == 0) && Mod.TNHInstanceList != null && !Mod.joinTNHInstances.ContainsKey(TNHInstance.instance))
                {
                    GameObject newInstance = GameObject.Instantiate<GameObject>(Mod.TNHInstancePrefab, Mod.TNHInstanceList.transform);
                    newInstance.transform.GetChild(0).GetComponent<Text>().text = "Instance " + TNHInstance.instance;
                    newInstance.SetActive(true);

                    FVRPointableButton instanceButton = newInstance.AddComponent<FVRPointableButton>();
                    instanceButton.SetButton();
                    instanceButton.MaxPointingRange = 5;
                    instanceButton.Button.onClick.AddListener(() => { Mod.OnTNHInstanceClicked(TNHInstance.instance); });

                    Mod.joinTNHInstances.Add(TNHInstance.instance, newInstance);
                }
            }
        }

        public static void TNHHoldPointBeginAnalyzing(Packet packet)
        {
            int instance = packet.ReadInt();
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                // Set instance data
                TNHInstance.tickDownToID = packet.ReadFloat();
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Analyzing;
                TNHInstance.warpInData = new List<Vector3>();
                byte dataCount = packet.ReadByte();
                for (int i = 0; i < dataCount; ++i)
                {
                    TNHInstance.warpInData.Add(packet.ReadVector3());
                }

                // If this is our TNH game, actually begin analyzing
                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    // Note that since we received this order, we are not the controller of the instance
                    // Consequently, the warpins will not be spawned as in a normal call to BeginAnalyzing
                    // We have to spawn them ourselves with the given data
                    GM.TNH_Manager.m_curHoldPoint.BeginAnalyzing();

                    for (int i = 0; i < dataCount; i += 2)
                    {
                        GM.TNH_Manager.m_curHoldPoint.m_warpInTargets.Add(UnityEngine.Object.Instantiate<GameObject>(GM.TNH_Manager.m_curHoldPoint.M.Prefab_TargetWarpingIn, TNHInstance.warpInData[i], Quaternion.Euler(TNHInstance.warpInData[i + 1])));
                    }
                }
            }
        }

        public static void TNHHoldPointRaiseBarriers(Packet packet)
        {
            int instance = packet.ReadInt();
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                // Set instance data
                int barrierCount = packet.ReadInt();
                TNHInstance.raisedBarriers = new List<int>();
                TNHInstance.raisedBarrierPrefabIndices = new List<int>();
                for (int i = 0; i < barrierCount; ++i)
                {
                    TNHInstance.raisedBarriers.Add(packet.ReadInt());
                }
                for (int i = 0; i < barrierCount; ++i)
                {
                    TNHInstance.raisedBarrierPrefabIndices.Add(packet.ReadInt());
                }

                // If this is our TNH game, actually raise barriers
                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    // Raise barriers
                    for (int i = 0; i < TNHInstance.raisedBarriers.Count; ++i)
                    {
                        TNH_DestructibleBarrierPoint point = GM.TNH_Manager.m_curHoldPoint.BarrierPoints[TNHInstance.raisedBarriers[i]];
                        TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[TNHInstance.raisedBarrierPrefabIndices[i]];
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                        TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                        point.m_curBarrier = curBarrier;
                        curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                        curBarrier.SetBarrierPoint(point);
                        point.SetCoverPointData(TNHInstance.raisedBarrierPrefabIndices[i]);
                    }
                }
            }
        }

        public static void TNHHoldIdentifyEncryption(Packet packet)
        {
            int instance = packet.ReadInt();
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                // Set instance data
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Hacking;
                TNHInstance.tickDownToFailure = 120f;

                // If this is our TNH game, actually raise barriers
                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.IdentifyEncryption();
                }
            }
        }

        public static void TNHHoldPointFailOut(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.FailOut();
                }
            }
        }

        public static void TNHHoldPointBeginPhase(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = true;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.BeginPhase();
                }
            }
        }

        public static void TNHHoldPointCompleteHold(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.CompleteHold();
                }
            }
        }

        public static void SosigPriorityIFFChart(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int chart = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.objects[trackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                // Update local
                trackedSosig.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                if (trackedSosig.physicalSosig != null)
                {
                    trackedSosig.physicalSosig.physicalSosig.Priority.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                }
            }
        }

        public static void RemoteMissileDetonate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    RemoteMissile remoteMissile = (trackedItem.physicalItem.physicalItem as RemoteMissileLauncher).m_missile;
                    if (remoteMissile != null)
                    {
                        RemoteMissileDetonatePatch.overriden = true;
                        remoteMissile.transform.position = packet.ReadVector3();
                        remoteMissile.Detonante();
                    }
                }
            }
        }

        public static void StingerMissileExplode(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    StingerMissile missile = trackedItem.physicalItem.stingerMissile;
                    if (missile != null)
                    {
                        StingerMissileExplodePatch.overriden = true;
                        missile.transform.position = packet.ReadVector3();
                        missile.Explode();
                    }
                }
            }
        }

        public static void PinnedGrenadeExplode(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    PinnedGrenade grenade = trackedItem.physicalItem.physicalItem as PinnedGrenade;
                    if (grenade != null)
                    {
                        PinnedGrenadePatch.ExplodePinnedGrenade(grenade, packet.ReadVector3());
                    }
                }
            }
        }

        public static void PinnedGrenadePullPin(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    PinnedGrenade grenade = trackedItem.physicalItem.physicalItem as PinnedGrenade;
                    if (grenade != null)
                    {
                        for (int i = 0; i < grenade.m_rings.Count; ++i)
                        {
                            if (!grenade.m_rings[i].HasPinDetached())
                            {
                                grenade.m_rings[i].m_hasPinDetached = true;
                                grenade.m_rings[i].Pin.RootRigidbody = grenade.m_rings[i].Pin.gameObject.AddComponent<Rigidbody>();
                                grenade.m_rings[i].Pin.RootRigidbody.mass = 0.02f;
                                grenade.m_rings[i].ForceBreakInteraction();
                                grenade.m_rings[i].transform.SetParent(grenade.m_rings[i].Pin.transform);
                                grenade.m_rings[i].Pin.enabled = true;
                                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, grenade.m_rings[i].G.AudEvent_Pinpull, grenade.m_rings[i].transform.position);
                                grenade.m_rings[i].GetComponent<Collider>().enabled = false;
                                grenade.m_rings[i].enabled = false;
                            }
                        }
                    }
                }
            }
        }

        public static void FVRGrenadeExplode(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    FVRGrenade grenade = trackedItem.physicalItem.physicalItem as FVRGrenade;
                    if (grenade != null)
                    {
                        FVRGrenadePatch.ExplodeGrenade(grenade, packet.ReadVector3());
                    }
                }
            }
        }

        public static void BangSnapSplode(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    BangSnap bangSnap = trackedItem.physicalItem.physicalItem as BangSnap;
                    if (bangSnap != null)
                    {
                        bangSnap.transform.position = packet.ReadVector3();
                        ++BangSnapPatch.skip;
                        bangSnap.Splode();
                        --BangSnapPatch.skip;
                    }
                }
            }
        }

        public static void C4Detonate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    C4 c4 = trackedItem.physicalItem.physicalItem as C4;
                    if (c4 != null)
                    {
                        c4.transform.position = packet.ReadVector3();
                        ++C4DetonatePatch.skip;
                        c4.Detonate();
                        --C4DetonatePatch.skip;
                    }
                }
            }
        }

        public static void ClaymoreMineDetonate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    ClaymoreMine cm = trackedItem.physicalItem.physicalItem as ClaymoreMine;
                    if (cm != null)
                    {
                        cm.transform.position = packet.ReadVector3();
                        ++ClaymoreMineDetonatePatch.skip;
                        cm.Detonate();
                        --ClaymoreMineDetonatePatch.skip;
                    }
                }
            }
        }

        public static void SLAMDetonate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null)
            {
                // Update local
                if (trackedItem.physicalItem != null)
                {
                    SLAM slam = trackedItem.physicalItem.physicalItem as SLAM;
                    if (slam != null)
                    {
                        slam.transform.position = packet.ReadVector3();
                        ++SLAMDetonatePatch.skip;
                        slam.Detonate();
                        --SLAMDetonatePatch.skip;
                    }
                }
            }
        }

        public static void ClientDisconnect(Packet packet)
        {
            int ID = packet.ReadInt();

            Mod.RemovePlayerFromLists(ID);
        }

        public static void ServerClosed(Packet packet)
        {
            Client.singleton.Disconnect(false, 0);
        }

        public static void SpectatorHost(Packet packet)
        {
            int clientID = packet.ReadInt();
            bool spectatorHost = packet.ReadBool();

            if (spectatorHost)
            {
                if (!GameManager.spectatorHosts.Contains(clientID))
                {
                    GameManager.spectatorHosts.Add(clientID);
                }
            }
            else
            {
                GameManager.spectatorHosts.Remove(clientID);
            }

            GameManager.UpdatePlayerHidden(GameManager.players[clientID]);

            GameManager.OnSpectatorHostsChangedInvoke();
        }

        public static void ResetTNH(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.Reset();

                if (actualInstance.manager != null)
                {
                    actualInstance.ResetManager();
                }
            }
        }

        public static void ReviveTNHPlayer(Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.RevivePlayer(ID, true);
            }
        }

        public static void PlayerColor(Packet packet)
        {
            int ID = packet.ReadInt();
            int index = packet.ReadInt();

            GameManager.SetPlayerColor(ID, index, true, 0);
        }

        public static void ColorByIFF(Packet packet)
        {
            GameManager.colorByIFF = packet.ReadBool();
            if (H3MPWristMenuSection.colorByIFFText != null)
            {
                H3MPWristMenuSection.colorByIFFText.text = "Color by IFF (" + GameManager.colorByIFF + ")";
            }

            if (GameManager.colorByIFF)
            {
                GameManager.colorIndex = GM.CurrentPlayerBody.GetPlayerIFF() % GameManager.colors.Length;
                if (BodyWristMenuSection.colorText != null)
                {
                    BodyWristMenuSection.colorText.text = "Current color: " + GameManager.colorNames[GameManager.colorIndex];
                }

                foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                {
                    playerEntry.Value.SetColor(playerEntry.Value.IFF);
                }
            }
            else
            {
                foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                {
                    playerEntry.Value.SetColor(playerEntry.Value.colorIndex);
                }
            }
        }

        public static void NameplateMode(Packet packet)
        {
            GameManager.nameplateMode = packet.ReadInt();

            switch (GameManager.nameplateMode)
            {
                case 0:
                    if (H3MPWristMenuSection.nameplateText != null)
                    {
                        H3MPWristMenuSection.nameplateText.text = "Nameplates (All)";
                    }
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        if (playerEntry.Value.playerBody != null)
                        {
                            playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(true);
                        }
                    }
                    break;
                case 1:
                    if (H3MPWristMenuSection.nameplateText != null)
                    {
                        H3MPWristMenuSection.nameplateText.text = "Nameplates (Friendly only)";
                    }
                    if (GM.CurrentPlayerBody != null)
                    {
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.playerBody != null)
                            {
                                playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(GM.CurrentPlayerBody.GetPlayerIFF() == playerEntry.Value.IFF);
                            }
                        }
                    }
                    break;
                case 2:
                    if (H3MPWristMenuSection.nameplateText != null)
                    {
                        H3MPWristMenuSection.nameplateText.text = "Nameplates (None)";
                    }
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        if (playerEntry.Value.playerBody != null)
                        {
                            playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(false);
                        }
                    }
                    break;
            }
        }

        public static void RadarMode(Packet packet)
        {
            GameManager.radarMode = packet.ReadInt();

            switch (GameManager.radarMode)
            {
                case 0:
                    if (H3MPWristMenuSection.radarModeText != null)
                    {
                        H3MPWristMenuSection.radarModeText.text = "Radar mode (All)";
                    }
                    if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                    {
                        // Add all currently playing players to radar
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.visible && playerEntry.Value.reticleContact == null && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key))
                            {
                                playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                            }
                        }
                    }
                    break;
                case 1:
                    if (H3MPWristMenuSection.radarModeText != null)
                    {
                        H3MPWristMenuSection.radarModeText.text = "Radar mode (Friendly only)";
                    }
                    if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                    {
                        // Add all currently playing friendly players to radar, remove if not friendly
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.visible && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) &&
                                playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() &&
                                playerEntry.Value.reticleContact == null)
                            {
                                playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                            }
                            else if ((!playerEntry.Value.visible || !Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) || playerEntry.Value.IFF != GM.CurrentPlayerBody.GetPlayerIFF())
                                     && playerEntry.Value.reticleContact != null)
                            {
                                for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                {
                                    if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                    {
                                        GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                        UnityEngine.Object.Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                                        GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                                        playerEntry.Value.reticleContact = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 2:
                    if (H3MPWristMenuSection.radarModeText != null)
                    {
                        H3MPWristMenuSection.radarModeText.text = "Radar mode (None)";
                    }
                    if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                    {
                        // Remvoe all player contacts
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.reticleContact != null)
                            {
                                for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                {
                                    if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                    {
                                        GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                        UnityEngine.Object.Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                                        GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                                        playerEntry.Value.reticleContact = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public static void RadarColor(Packet packet)
        {
            GameManager.radarColor = packet.ReadBool();
            if (H3MPWristMenuSection.radarColorText != null)
            {
                H3MPWristMenuSection.radarColorText.text = "Radar color IFF (" + GameManager.radarColor + ")";
            }

            // Set color of any active player contacts
            if (GameManager.radarColor)
            {
                foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                {
                    if (playerEntry.Value.reticleContact == null)
                    {
                        playerEntry.Value.reticleContact.R_Arrow.material.color = playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
                        playerEntry.Value.reticleContact.R_Icon.material.color = playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                {
                    if (playerEntry.Value.reticleContact == null)
                    {
                        playerEntry.Value.reticleContact.R_Arrow.material.color = GameManager.colors[playerEntry.Value.colorIndex];
                        playerEntry.Value.reticleContact.R_Icon.material.color = GameManager.colors[playerEntry.Value.colorIndex];
                    }
                }
            }
        }

        public static void TNHInitializer(Packet packet)
        {
            int instance = packet.ReadInt();
            int initializer = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.initializer = initializer;

                if(initializer != GameManager.ID)
                {
                    TNHInstance.initializationRequested = false;

                    if (Mod.currentTNHInstance == TNHInstance && Mod.waitingForTNHGameStart)
                    {
                        Mod.currentTNHSceneLoader.LoadMG();
                    }
                }
            }
        }

        public static void MaxHealth(Packet packet)
        {
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            int index = packet.ReadInt();
            float original = packet.ReadFloat();

            H3MPWristMenuSection.UpdateMaxHealth(scene, instance, index, original);
        }

        public static void FuseIgnite(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalItem is FVRFusedThrowable)
            {
                ++FusePatch.igniteSkip;
                (itemData.physicalItem.physicalItem as FVRFusedThrowable).Fuse.Ignite(0);
                --FusePatch.igniteSkip;
            }
        }

        public static void FuseBoom(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalItem is FVRFusedThrowable)
            {
                (itemData.physicalItem.physicalItem as FVRFusedThrowable).Fuse.Boom();
            }
        }

        public static void MolotovShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool ignited = packet.ReadBool();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalItem is Molotov)
            {
                Molotov asMolotov = itemData.physicalItem.physicalItem as Molotov;
                if (ignited && !asMolotov.Igniteable.IsOnFire())
                {
                    asMolotov.RemoteIgnite();
                }
                ++MolotovPatch.shatterSkip;
                asMolotov.Shatter();
                --MolotovPatch.shatterSkip;
            }
        }

        public static void MolotovDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == Client.singleton.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MolotovPatch.damageSkip;
                        (itemData.physicalItem.physicalItem as Molotov).Damage(damage);
                        --MolotovPatch.damageSkip;
                    }
                }
                else
                {
                    ClientSend.MolotovDamage(trackedID, damage);
                }
            }
            else
            {
                ClientSend.MolotovDamage(trackedID, damage);
            }
        }

        public static void MagazineAddRound(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MagazinePatch.addRoundSkip;
                        (itemData.physicalItem.physicalItem as FVRFireArmMagazine).AddRound(roundClass, true, true);
                        --MagazinePatch.addRoundSkip;
                    }
                }
            }
        }

        public static void ClipAddRound(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++ClipPatch.addRoundSkip;
                        (itemData.physicalItem.physicalItem as FVRFireArmClip).AddRound(roundClass, true, true);
                        --ClipPatch.addRoundSkip;
                    }
                }
            }
        }

        public static void SpeedloaderChamberLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++SpeedloaderChamberPatch.loadSkip;
                        (itemData.physicalItem.physicalItem as Speedloader).Chambers[chamberIndex].Load(roundClass, true);
                        --SpeedloaderChamberPatch.loadSkip;
                    }
                }
            }
        }

        public static void RemoteGunChamber(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        FVRFireArmRound round = AM.GetRoundSelfPrefab(roundType, roundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        ++RemoteGunPatch.chamberSkip;
                        (itemData.physicalItem.physicalItem as RemoteGun).ChamberCartridge(round);
                        --RemoteGunPatch.chamberSkip;
                    }
                }
            }
        }

        public static void ChamberRound(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null && itemData.physicalItem.chamberRound != null)
                    {
                        itemData.physicalItem.chamberRound(roundClass, (FireArmRoundType)(-1), chamberIndex);
                    }
                }
            }
        }

        public static void MagazineLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();
            short slot = packet.ReadShort();

            TrackedItemData magItemData = Client.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Client.objects[FATrackedID] as TrackedItemData;
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        if (slot == -1)
                        {
                            ++MagazinePatch.loadSkip;
                            (magItemData.physicalItem.physicalItem as FVRFireArmMagazine).Load(FAItemData.physicalItem.physicalItem as FVRFireArm);
                            --MagazinePatch.loadSkip;
                        }
                        else
                        {
                            ++MagazinePatch.loadSkip;
                            (magItemData.physicalItem.physicalItem as FVRFireArmMagazine).LoadIntoSecondary(FAItemData.physicalItem.physicalItem as FVRFireArm, slot);
                            --MagazinePatch.loadSkip;
                        }
                    }
                }
            }
        }

        public static void MagazineLoadAttachable(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            TrackedItemData magItemData = Client.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Client.objects[FATrackedID] as TrackedItemData;
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        ++MagazinePatch.loadSkip;
                        (magItemData.physicalItem.physicalItem as FVRFireArmMagazine).Load(FAItemData.physicalItem.dataObject as AttachableFirearm);
                        --MagazinePatch.loadSkip;
                    }
                }
            }
        }

        public static void ClipLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            TrackedItemData clipItemData = Client.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Client.objects[FATrackedID] as TrackedItemData;
            if (clipItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && clipItemData.physicalItem != null)
                    {
                        ++ClipPatch.loadSkip;
                        (clipItemData.physicalItem.physicalItem as FVRFireArmClip).Load(FAItemData.physicalItem.physicalItem as FVRFireArm);
                        --ClipPatch.loadSkip;
                    }
                }
            }
        }

        public static void RevolverCylinderLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                int chamberCount = packet.ReadByte();
                List<short> classes = new List<short>();
                for (int i = 0; i < chamberCount; ++i)
                {
                    classes.Add(packet.ReadShort());
                }

                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        Revolver revolver = itemData.physicalItem.physicalItem as Revolver;
                        ++ChamberPatch.chamberSkip;
                        for (int i = 0; i < revolver.Chambers.Length; ++i)
                        {
                            if (classes[i] != -1)
                            {
                                revolver.Chambers[i].SetRound((FireArmRoundClass)classes[i], revolver.Chambers[i].transform.position, revolver.Chambers[i].transform.rotation);
                            }
                        }
                        --ChamberPatch.chamberSkip;
                    }
                }
            }
        }

        public static void RevolvingShotgunLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                int chamberCount = packet.ReadByte();
                List<short> classes = new List<short>();
                for (int i = 0; i < chamberCount; ++i)
                {
                    classes.Add(packet.ReadShort());
                }

                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        RevolvingShotgun revShotgun = itemData.physicalItem.physicalItem as RevolvingShotgun;

                        if (revShotgun.CylinderLoaded)
                        {
                            return;
                        }
                        revShotgun.CylinderLoaded = true;
                        revShotgun.ProxyCylinder.gameObject.SetActive(true);
                        revShotgun.PlayAudioEvent(FirearmAudioEventType.MagazineIn, 1f);
                        revShotgun.CurChamber = 0;
                        revShotgun.ProxyCylinder.localRotation = revShotgun.GetLocalRotationFromCylinder(0);
                        ++ChamberPatch.chamberSkip;
                        for (int i = 0; i < revShotgun.Chambers.Length; i++)
                        {
                            if (classes[i] == -1)
                            {
                                revShotgun.Chambers[i].Unload();
                            }
                            else
                            {
                                revShotgun.Chambers[i].Autochamber((FireArmRoundClass)classes[i]);
                            }
                            revShotgun.Chambers[i].UpdateProxyDisplay();
                        }
                        --ChamberPatch.chamberSkip;
                    }
                }
            }
        }

        public static void GrappleGunLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                int chamberCount = packet.ReadByte();
                List<short> classes = new List<short>();
                for (int i = 0; i < chamberCount; ++i)
                {
                    classes.Add(packet.ReadShort());
                }

                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        GrappleGun grappleGun = itemData.physicalItem.physicalItem as GrappleGun;

                        if (grappleGun.IsMagLoaded)
                        {
                            return;
                        }
                        grappleGun.IsMagLoaded = true;
                        grappleGun.ProxyMag.gameObject.SetActive(true);
                        grappleGun.PlayAudioEvent(FirearmAudioEventType.MagazineIn, 1f);
                        grappleGun.m_curChamber = 0;
                        ++ChamberPatch.chamberSkip;
                        for (int i = 0; i < grappleGun.Chambers.Length; i++)
                        {
                            if (classes[i] == -1)
                            {
                                grappleGun.Chambers[i].Unload();
                            }
                            else
                            {
                                grappleGun.Chambers[i].Autochamber((FireArmRoundClass)classes[i]);
                            }
                            grappleGun.Chambers[i].UpdateProxyDisplay();
                        }
                        --ChamberPatch.chamberSkip;
                    }
                }
            }
        }

        public static void CarlGustafLatchSate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                byte type = packet.ReadByte();
                byte state = packet.ReadByte();

                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustafLatch latch = (itemData.physicalItem.physicalItem as CarlGustaf).TailLatch;
                        if (type == 1)
                        {
                            latch = latch.RestrictingLatch;
                        }

                        ++CarlGustafLatchPatch.skip;
                        if (state == 0) // Closed
                        {
                            if (latch.LState != CarlGustafLatch.CGLatchState.Closed)
                            {
                                float val = latch.IsMinOpen ? latch.RotMax : latch.RotMin;
                                latch.m_curRot = val;
                                latch.m_tarRot = val;
                                latch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                                latch.LState = CarlGustafLatch.CGLatchState.Closed;
                            }
                        }
                        else if (latch.LState != CarlGustafLatch.CGLatchState.Open)
                        {
                            float val = latch.IsMinOpen ? latch.RotMin : latch.RotMax;
                            latch.m_curRot = val;
                            latch.m_tarRot = val;
                            latch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                            latch.LState = CarlGustafLatch.CGLatchState.Open;
                        }
                        --CarlGustafLatchPatch.skip;
                    }
                }
            }
        }

        public static void CarlGustafShellSlideSate(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                byte state = packet.ReadByte();

                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustaf asCG = itemData.physicalItem.physicalItem as CarlGustaf;

                        ++CarlGustafShellInsertEjectPatch.skip;
                        if (state == 0) // In
                        {
                            if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.In)
                            {
                                asCG.ShellInsertEject.m_curZ = asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z;
                                asCG.ShellInsertEject.m_tarZ = asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z;
                                asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                                asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.In;
                            }
                        }
                        else if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Out)
                        {
                            asCG.ShellInsertEject.m_curZ = asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z;
                            asCG.ShellInsertEject.m_tarZ = asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z;
                            asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                            asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Out;
                        }
                        --CarlGustafShellInsertEjectPatch.skip;
                    }
                }
            }
        }

        public static void TNHHostStartHold(Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.controller == GameManager.ID)
            {
                if (Mod.currentTNHInstance.manager != null && !Mod.currentTNHInstance.holdOngoing)
                {
                    GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode.position, true);
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasActivated = true;
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasInitiatedHold = true;
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                }
            }
        }

        public static void GrappleAttached(Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte[] data = packet.ReadBytes(packet.ReadShort());

            TrackedItemData trackedItem = Client.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.controller != GameManager.ID)
            {
                trackedItem.additionalData = data;

                if (trackedItem.physicalItem != null)
                {
                    GrappleThrowable asGrappleThrowable = trackedItem.physicalItem.physicalItem as GrappleThrowable;
                    asGrappleThrowable.RootRigidbody.isKinematic = true;
                    asGrappleThrowable.m_isRopeFree = true;
                    asGrappleThrowable.BundledRope.SetActive(false);
                    asGrappleThrowable.m_hasBeenThrown = true;
                    asGrappleThrowable.m_hasLanded = true;
                    if (asGrappleThrowable.m_ropeLengths.Count > 0)
                    {
                        for (int i = asGrappleThrowable.m_ropeLengths.Count - 1; i >= 0; i--)
                        {
                            UnityEngine.Object.Destroy(asGrappleThrowable.m_ropeLengths[i]);
                        }
                        asGrappleThrowable.m_ropeLengths.Clear();
                    }
                    asGrappleThrowable.finalRopePoints.Clear();
                    asGrappleThrowable.FakeRopeLength.SetActive(false);

                    int count = trackedItem.additionalData[1];
                    Vector3 currentRopePoint = new Vector3(BitConverter.ToSingle(trackedItem.additionalData, 2), BitConverter.ToSingle(trackedItem.additionalData, 6), BitConverter.ToSingle(trackedItem.additionalData, 10));
                    for (int i = 1; i < count; ++i)
                    {
                        Vector3 newPoint = new Vector3(BitConverter.ToSingle(trackedItem.additionalData, i * 12 + 2), BitConverter.ToSingle(trackedItem.additionalData, i * 12 + 6), BitConverter.ToSingle(trackedItem.additionalData, i * 12 + 10));
                        Vector3 vector = newPoint - currentRopePoint;

                        GameObject gameObject = UnityEngine.Object.Instantiate(asGrappleThrowable.RopeLengthPrefab, newPoint, Quaternion.LookRotation(-vector, Vector3.up));
                        gameObject.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
                        FVRHandGrabPoint fvrhandGrabPoint = null;
                        if (asGrappleThrowable.m_ropeLengths.Count > 0)
                        {
                            fvrhandGrabPoint = asGrappleThrowable.m_ropeLengths[asGrappleThrowable.m_ropeLengths.Count - 1].GetComponent<FVRHandGrabPoint>();
                        }
                        FVRHandGrabPoint component = gameObject.GetComponent<FVRHandGrabPoint>();
                        asGrappleThrowable.m_ropeLengths.Add(gameObject);
                        if (fvrhandGrabPoint != null && component != null)
                        {
                            fvrhandGrabPoint.ConnectedGrabPoint_Base = component;
                            component.ConnectedGrabPoint_End = fvrhandGrabPoint;
                        }
                        asGrappleThrowable.finalRopePoints.Add(newPoint);
                        currentRopePoint = newPoint;
                    }
                }
            }
        }

        public static void RegisterCustomPacketType(Packet packet)
        {
            string handlerID = packet.ReadString();
            int index = packet.ReadInt();

            if (Mod.registeredCustomPacketIDs.TryGetValue(handlerID, out int actualIndex))
            {
                Mod.LogError("Server sent for " + handlerID + " custom packet handler to be registered at "+ index + " but this ID already exists at "+ actualIndex + ".");
            }
            else // We don't yet have this handlerID, add it
            {
                // Check if index fits, if not make array large enough
                if (index >= Mod.customPacketHandlers.Length)
                { 
                    int newLength = index - index % 10 + 10;
                    Mod.CustomPacketHandler[] temp = Mod.customPacketHandlers;
                    Mod.customPacketHandlers = new Mod.CustomPacketHandler[newLength];
                    for (int i = 0; i < temp.Length; ++i)
                    {
                        Mod.customPacketHandlers[i] = temp[i];
                    }
                }

                // Store for potential later use
                Mod.registeredCustomPacketIDs.Add(handlerID, index);

                // Send event so a mod can add their handler at the index
                Mod.CustomPacketHandlerReceivedInvoke(handlerID, index);
            }
        }

        public static void BreakableGlassDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedBreakableGlassData trackedBreakableGlass = Client.objects[trackedID] as TrackedBreakableGlassData;
            if (trackedBreakableGlass != null)
            {
                if (trackedBreakableGlass.controller == GameManager.ID)
                {
                    if (trackedBreakableGlass.damager != null)
                    {
                        ++BreakableGlassDamagerPatch.damageSkip;
                        trackedBreakableGlass.damager.Damage(damage);
                        --BreakableGlassDamagerPatch.damageSkip;
                    }
                }
                else
                {
                    ClientSend.BreakableGlassDamage(packet);
                }
            }
        }

        public static void WindowShatterSound(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int mode = packet.ReadByte();

            TrackedBreakableGlassData trackedBreakableGlass = Client.objects[trackedID] as TrackedBreakableGlassData;
            if (trackedBreakableGlass != null && trackedBreakableGlass.physicalBreakableGlass != null)
            {
                trackedBreakableGlass.physicalBreakableGlass.PlayerShatterAudio(mode);
            }
        }

        public static void SpectatorHostAssignment(Packet packet)
        {
            int host = packet.ReadInt();
            int controller = packet.ReadInt();
            bool reassignment = packet.ReadBool();

            if (host == GameManager.ID)
            {
                if (GameManager.spectatorHost)
                {
                    GameManager.spectatorHostControlledBy = controller;
                }
            }
            else
            {
                Mod.OnSpectatorHostReceivedInvoke(host, reassignment);
            }
        }

        public static void GiveUpSpectatorHost(Packet packet)
        {
            Mod.OnSpectatorHostGiveUpInvoke();
        }

        public static void SpectatorHostOrderTNHHost(Packet packet)
        {
            if (GameManager.spectatorHost && GameManager.spectatorHostControlledBy != -1)
            {
                if (GameManager.sceneLoading)
                {
                    Mod.spectatorHostWaitingForTNHSetup = true;
                    Mod.TNHRequestHostOnDeathSpectate = packet.ReadBool();
                }
                else
                {
                    if (GameManager.scene.Equals("TakeAndHold_Lobby_2"))
                    {
                        Mod.OnTNHHostClicked();
                        Mod.TNHOnDeathSpectate = packet.ReadBool();
                        Mod.OnTNHHostConfirmClicked();

                        Mod.spectatorHostWaitingForTNHInstance = true;
                        Mod.spectatorHostWaitingForTNHSetup = false;
                    }
                    else if (GameManager.scene.Equals("MainMenu3"))
                    {
                        SteamVR_LoadLevel.Begin("TakeAndHold_Lobby_2", false, 0.5f, 0f, 0f, 0f, 1f);
                        Mod.spectatorHostWaitingForTNHSetup = true;
                    }
                    else
                    {
                        SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                        Mod.spectatorHostWaitingForTNHSetup = true;
                    }
                }
            }
        }

        public static void TNHSpectatorHostReady(Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.controlledSpectatorHost != -1 && GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                if (Mod.waitingForTNHHost)
                {
                    if (Mod.TNHMenu != null)
                    {
                        Mod.TNHHostedInstance = instance;
                        Mod.TNHMenuPages[6].SetActive(false);
                        Mod.TNHMenuPages[2].SetActive(true);
                        Mod.TNHStatusText.text = "Setting up as Client";
                        Mod.TNHStatusText.color = Color.blue;
                    }
                    else
                    {
                        Mod.waitingForTNHHost = false;
                    }
                }
            }
        }

        public static void SpectatorHostStartTNH(Packet packet)
        {
            if (GameManager.spectatorHost && !GameManager.sceneLoading && GameManager.scene.Equals("TakeAndHold_Lobby_2") &&
                Mod.currentTNHInstance != null && Mod.currentTNHInstance.playerIDs.Count > 0 &&
                Mod.currentTNHInstance.playerIDs[0] == GameManager.ID && Mod.currentTNHSceneLoader != null)
            {
                Mod.currentTNHSceneLoader.LoadMG();
            }
        }

        public static void UnassignSpectatorHost(Packet packet)
        {
            if (GameManager.spectatorHost)
            {
                GameManager.spectatorHostControlledBy = -1;

                if (!GameManager.sceneLoading)
                {
                    if (!GameManager.scene.Equals("MainMenu3"))
                    {
                        SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                    }
                }
                else
                {
                    GameManager.resetSpectatorHost = true;
                }
            }
        }

        public static void ReactiveSteelTargetDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            float damKinetic = packet.ReadInt();
            float damBlunt = packet.ReadInt();
            Vector3 point = packet.ReadVector3();
            Vector3 dir = packet.ReadVector3();
            bool usesHoles = packet.ReadBool();
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            float scale = 0;
            if (usesHoles)
            {
                pos = packet.ReadVector3();
                rot = packet.ReadQuaternion();
                scale = packet.ReadFloat();
            }

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physicalItem != null)
                {
                    ReactiveSteelTarget rst = trackedItemData.physicalItem.getSecondary(index) as ReactiveSteelTarget;

                    if (rst != null)
                    {
                        if (rst.BulletHolePrefabs.Length > 0 && usesHoles)
                        {
                            if (rst.m_currentHoles.Count > rst.MaxHoles)
                            {
                                rst.holeindex++;
                                if (rst.holeindex > rst.MaxHoles - 1)
                                {
                                    rst.holeindex = 0;
                                }
                                rst.m_currentHoles[rst.holeindex].transform.position = pos;
                                rst.m_currentHoles[rst.holeindex].transform.rotation = rot;
                                rst.m_currentHoles[rst.holeindex].transform.localScale = new Vector3(scale, scale, scale);
                            }
                            else
                            {
                                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(rst.BulletHolePrefabs[UnityEngine.Random.Range(0, rst.BulletHolePrefabs.Length)], pos, rot);
                                gameObject.transform.SetParent(rst.transform);
                                gameObject.transform.localScale = new Vector3(scale, scale, scale);
                                rst.m_currentHoles.Add(gameObject);
                            }
                        }
                        if (trackedItemData.controller == GameManager.ID && rst.m_hasRB && rst.AddForceJuice)
                        {
                            float d = Mathf.Clamp(damBlunt * 0.01f, 0f, rst.MaxForceImpulse);
                            Debug.DrawLine(point, point + dir * d, Color.red, 40f);
                            rst.rb.AddForceAtPosition(dir * d, point, ForceMode.Impulse);
                        }
                        rst.PlayHitSound(Mathf.Clamp(damKinetic * 0.0025f, 0.05f, 1f));
                    }
                }
            }
        }

        public static void MTUTest(Packet packet)
        {
            Mod.LogWarning("Received MTU test packet from server");
        }

        public static void IDConfirm(Packet packet)
        {
            int IDToConfirm = packet.ReadInt();

            Mod.LogInfo("Client received IDConfirm for "+IDToConfirm);

            TrackedObjectData trackedObjectData = Client.objects[IDToConfirm];
            if (trackedObjectData != null)
            {
                trackedObjectData.awaitingInstantiation = false;
                
                if (trackedObjectData.physical != null)
                {
                    trackedObjectData.removeFromListOnDestroy = true;
                    trackedObjectData.physical.sendDestroy = false;
                    trackedObjectData.physical.dontGiveControl = true;
                    TrackedObject[] childrenTrackedObjects = trackedObjectData.physical.GetComponentsInChildren<TrackedObject>();
                    for (int i = 0; i < childrenTrackedObjects.Length; ++i)
                    {
                        if (childrenTrackedObjects[i] != null)
                        {
                            childrenTrackedObjects[i].sendDestroy = false;
                            childrenTrackedObjects[i].data.removeFromListOnDestroy = true;
                            childrenTrackedObjects[i].dontGiveControl = true;
                        }
                    }

                    trackedObjectData.physical.SecondaryDestroy();

                    GameObject.Destroy(trackedObjectData.physical.gameObject);
                }
                else
                {
                    if (trackedObjectData.localTrackedID != -1)
                    {
                        trackedObjectData.RemoveFromLocal();
                    }

                    trackedObjectData.RemoveFromLists();
                }
            }

            ClientSend.IDConfirm(IDToConfirm);
        }

        public static void EnforcePlayerModels(Packet packet)
        {
            // TODO
        }

        public static void ObjectScene(Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            if (Mod.trackedObjectTypesByName.TryGetValue(typeID, out Type trackedObjectType))
            {
                TrackedObjectData trackedObjectData = (TrackedObjectData)Activator.CreateInstance(trackedObjectType, packet, typeID, trackedID);

                if (Client.objects[trackedID] != null)
                {
                    Client.objects[trackedID].SetScene(trackedObjectData.scene, false);
                }
                else // Don't have object data yet
                {
                    Client.AddTrackedObject(trackedObjectData);
                }
            }
        }

        public static void ObjectInstance(Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            if (Mod.trackedObjectTypesByName.TryGetValue(typeID, out Type trackedObjectType))
            {
                TrackedObjectData trackedObjectData = (TrackedObjectData)Activator.CreateInstance(trackedObjectType, packet, typeID, trackedID);

                if (Client.objects[trackedID] != null)
                {
                    Client.objects[trackedID].SetInstance(trackedObjectData.instance, false);
                }
                else // Don't have object data yet
                {
                    Client.AddTrackedObject(trackedObjectData);
                }
            }
        }

        public static void UpdateEncryptionDisplay(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int numHitsLeft = packet.ReadInt();

            TrackedEncryptionData trackedEncryptionData = Client.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryptionData != null)
            {
                trackedEncryptionData.numHitsLeft = numHitsLeft;

                if (trackedEncryptionData.physicalEncryption != null)
                {
                    trackedEncryptionData.physicalEncryption.physicalEncryption.m_numHitsLeft = numHitsLeft;
                    if (trackedEncryptionData.physicalEncryption.physicalEncryption.UsesMultipleDisplay
                        && trackedEncryptionData.physicalEncryption.physicalEncryption.DisplayList.Count > numHitsLeft
                        && trackedEncryptionData.physicalEncryption.physicalEncryption.DisplayList[numHitsLeft] != null)
                    {
                        ++EncryptionPatch.updateDisplaySkip;
                        trackedEncryptionData.physicalEncryption.physicalEncryption.UpdateDisplay();
                        --EncryptionPatch.updateDisplaySkip;
                    }
                }
            }
        }

        public static void RoundDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.controller == GameManager.ID)
                {
                    if (trackedItemData.physical != null)
                    {
                        ++RoundPatch.splodeInDamage;
                        (trackedItemData.physicalItem.physicalItem as FVRFireArmRound).Damage(damage);
                        --RoundPatch.splodeInDamage;
                    }
                }
                else
                {
                    ClientSend.RoundDamage(trackedID, damage);
                }
            }
        }

        public static void RoundSplode(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physical != null)
                {
                    float velMultiplier = packet.ReadFloat();
                    bool isRandomDir = packet.ReadBool();

                    ++RoundPatch.splodeSkip;
                    (trackedItemData.physicalItem.physicalItem as FVRFireArmRound).Splode(velMultiplier, isRandomDir, false);
                    --RoundPatch.splodeSkip;
                }
            }
        }

        public static void ConnectionComplete(Packet packet)
        {
            Client.isFullyConnected = true;

            Mod.OnConnectionInvoke();
        }

        public static void SightFlipperState(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedObjectData trackedObjectData = Client.objects[trackedID];
            if (trackedObjectData != null)
            {
                if (trackedObjectData.physical)
                {
                    int index = packet.ReadInt();
                    AR15HandleSightFlipper[] flippers = trackedObjectData.physical.GetComponentsInChildren<AR15HandleSightFlipper>();
                    if (flippers.Length > index)
                    {
                        flippers[index].m_isLargeAperture = packet.ReadBool();
                    }
                }
            }
        }

        public static void SightRaiserState(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedObjectData trackedObjectData = Client.objects[trackedID];
            if (trackedObjectData != null)
            {
                if (trackedObjectData.physical)
                {
                    int index = packet.ReadInt();
                    AR15HandleSightRaiser[] raisers = trackedObjectData.physical.GetComponentsInChildren<AR15HandleSightRaiser>();
                    if (raisers.Length > index)
                    {
                        switch ((AR15HandleSightRaiser.SightHeights)packet.ReadByte())
                        {
                            case AR15HandleSightRaiser.SightHeights.Low:
                                raisers[index].height = AR15HandleSightRaiser.SightHeights.Low;
                                raisers[index].m_sightHeight = 0.25f;
                                break;
                            case AR15HandleSightRaiser.SightHeights.Mid:
                                raisers[index].height = AR15HandleSightRaiser.SightHeights.Mid;
                                raisers[index].m_sightHeight = 0.5f;
                                break;
                            case AR15HandleSightRaiser.SightHeights.High:
                                raisers[index].height = AR15HandleSightRaiser.SightHeights.High;
                                raisers[index].m_sightHeight = 0.75f;
                                break;
                            case AR15HandleSightRaiser.SightHeights.Highest:
                                raisers[index].height = AR15HandleSightRaiser.SightHeights.Highest;
                                raisers[index].m_sightHeight = 1f;
                                break;
                            case AR15HandleSightRaiser.SightHeights.Lowest:
                                raisers[index].height = AR15HandleSightRaiser.SightHeights.Lowest;
                                raisers[index].m_sightHeight = 0f;
                                break;
                        }
                    }
                }
            }
        }

        public static void GatlingGunFire(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 pos = packet.ReadVector3();
            Quaternion rot = packet.ReadQuaternion();
            Vector3 dir = packet.ReadVector3();

            TrackedObjectData trackedObjectData = Client.objects[trackedID];
            if (trackedObjectData != null)
            {
                if (trackedObjectData.physical)
                {
                    wwGatlingGun instance = (trackedObjectData as TrackedGatlingGunData).physicalGatlingGun.physicalGatlingGun;
                    wwGatlingGun.MuzzleFireType muzzleFireType = instance.MuzzleFX[instance.AmmoType];
                    for (int i = 0; i < muzzleFireType.MuzzleFires.Length; i++)
                    {
                        muzzleFireType.MuzzleFires[i].Emit(muzzleFireType.MuzzleFireAmounts[i]);
                    }
                    instance.m_pool_shot.PlayClip(instance.AudioClipSet.Shots_Main, instance.MuzzlePos.position, null);
                    instance.m_pool_mechanics.PlayClip(instance.AudioClipSet.HammerHit, instance.MuzzlePos.position, null);
                    instance.m_pool_tail.PlayClipPitchOverride(SM.GetTailSet(instance.TailClass, GM.CurrentPlayerBody.GetCurrentSoundEnvironment()), instance.MuzzlePos.position, instance.AudioClipSet.TailPitchMod_Main, null);
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(muzzleFireType.ProjectilePrefab, pos, rot);
                    gameObject.GetComponent<BallisticProjectile>().Fire(dir, null);
                }
            }
        }

        public static void GasCuboidGout(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 pos = packet.ReadVector3();
            Vector3 norm = packet.ReadVector3();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null && trackedItemData.additionalData[0] < 255)
            {
                byte[] temp = trackedItemData.additionalData;
                trackedItemData.additionalData = new byte[temp.Length + 24];
                for (int i = 0; i < temp.Length; ++i)
                {
                    trackedItemData.additionalData[i] = temp[i];
                }
                ++trackedItemData.additionalData[1];

                if (trackedItemData.physical)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    asGC.hasGeneratedGoutYet = false;
                    ++GasCuboidPatch.generateGoutSkip;
                    asGC.GenerateGout(pos, norm);
                    --GasCuboidPatch.generateGoutSkip;
                }
            }
        }

        public static void GasCuboidDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.controller == GameManager.ID && trackedItemData.physical != null)
                {
                    ++GasCuboidDamagePatch.skip;
                    (trackedItemData.physicalItem.dataObject as Brut_GasCuboid).Damage(packet.ReadDamage());
                    --GasCuboidDamagePatch.skip;
                }
            }
        }

        public static void GasCuboidHandleDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null && trackedItemData.controller == GameManager.ID)
            {
                if (trackedItemData.physical != null)
                {
                    ++GasCuboidHandleDamagePatch.skip;
                    (trackedItemData.physicalItem.dataObject as Brut_GasCuboid).Handle.GetComponent<Brut_GasCuboidHandle>().Damage(packet.ReadDamage());
                    --GasCuboidHandleDamagePatch.skip;
                }
            }
        }

        public static void GasCuboidDamageHandle(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                trackedItemData.additionalData[0] = 1;
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    asGC.m_isHandleBrokenOff = true;
                    asGC.Handle.SetActive(false);
                }
            }
        }

        public static void GasCuboidExplode(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 point = packet.ReadVector3();
            Vector3 dir = packet.ReadVector3();
            bool big = packet.ReadBool();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    ++GasCuboidPatch.explodeSkip;
                    asGC.Explode(point, dir, big);
                    --GasCuboidPatch.explodeSkip;
                }
            }
        }

        public static void GasCuboidShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 point = packet.ReadVector3();
            Vector3 dir = packet.ReadVector3();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    ++GasCuboidPatch.shatterSkip;
                    asGC.Shatter(point, dir);
                    --GasCuboidPatch.shatterSkip;
                }
            }
        }

        public static void FloaterDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedFloaterData trackedFloaterData = Client.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null && trackedFloaterData.controller == GameManager.ID && trackedFloaterData.physical != null)
            {
                ++FloaterDamagePatch.skip;
                trackedFloaterData.physicalFloater.physicalFloater.Damage(packet.ReadDamage());
                --FloaterDamagePatch.skip;
            }
        }

        public static void FloaterCoreDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedFloaterData trackedFloaterData = Client.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null && trackedFloaterData.controller == GameManager.ID && trackedFloaterData.physical != null)
            {
                ++FloaterCoreDamagePatch.skip;
                trackedFloaterData.physicalFloater.GetComponentInChildren<Construct_Floater_Core>().Damage(packet.ReadDamage());
                --FloaterCoreDamagePatch.skip;
            }
        }

        public static void FloaterBeginExploding(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool fromController = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Client.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (fromController) // From controller, trigger explosion on our side
                {
                    FloaterPatch.beginExplodingOverride = true;
                    trackedFloaterData.physicalFloater.physicalFloater.BeginExploding();
                    FloaterPatch.beginExplodingOverride = false;
                }
                else if (trackedFloaterData.controller == GameManager.ID) // We control, trigger explosion and send order to everyone else
                {
                    trackedFloaterData.physicalFloater.physicalFloater.BeginExploding();
                }
                // else // Not from controller and we don't control, this should not happen
            }
        }

        public static void FloaterBeginDefusing(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool fromController = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Client.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (fromController) // From controller, trigger explosion on our side
                {
                    FloaterPatch.beginExplodingOverride = true;
                    trackedFloaterData.physicalFloater.physicalFloater.BeginDefusing();
                    FloaterPatch.beginExplodingOverride = false;
                }
                else if (trackedFloaterData.controller == GameManager.ID) // We control, trigger explosion and send order to everyone else
                {
                    trackedFloaterData.physicalFloater.physicalFloater.BeginDefusing();
                }
                // else // Not from controller and we don't control, this should not happen
            }
        }

        public static void FloaterExplode(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool defusing = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Client.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null && trackedFloaterData.physicalFloater != null)
            {
                trackedFloaterData.physicalFloater.physicalFloater.isExplosionDefuse = defusing;

                ++FloaterPatch.explodeSkip;
                trackedFloaterData.physicalFloater.physicalFloater.Explode();
                --FloaterPatch.explodeSkip;
            }
        }

        public static void IrisShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedIrisData trackedIrisData = Client.objects[trackedID] as TrackedIrisData;
            if (trackedIrisData != null && trackedIrisData.physicalIris != null)
            {
                byte index = packet.ReadByte();
                Vector3 point = packet.ReadVector3();
                Vector3 dir = packet.ReadVector3();
                float intensity = packet.ReadFloat();

                ++UberShatterableShatterPatch.skip;
                trackedIrisData.physicalIris.physicalIris.Rings[index].Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;
            }
        }

        public static void IrisSetState(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Construct_Iris.IrisState state = (Construct_Iris.IrisState)packet.ReadByte();

            TrackedIrisData trackedIrisData = Client.objects[trackedID] as TrackedIrisData;
            if (trackedIrisData != null)
            {
                trackedIrisData.state = state;

                if(trackedIrisData.physicalIris != null)
                {
                    ++IrisPatch.stateSkip;
                    trackedIrisData.physicalIris.physicalIris.SetState(state);
                    --IrisPatch.stateSkip;
                }
            }
        }

        public static void BrutBlockSystemStart(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool next = packet.ReadBool();

            TrackedBrutBlockSystemData trackedBrutBlockSystemData = Client.objects[trackedID] as TrackedBrutBlockSystemData;
            if (trackedBrutBlockSystemData != null)
            {
                if (trackedBrutBlockSystemData.physicalBrutBlockSystem != null)
                {
                    trackedBrutBlockSystemData.physicalBrutBlockSystem.physicalBrutBlockSystem.isNextBlock0 = next;

                    ++BrutBlockSystemPatch.startSkip;
                    trackedBrutBlockSystemData.physicalBrutBlockSystem.physicalBrutBlockSystem.TryToStartBlock();
                    --BrutBlockSystemPatch.startSkip;
                } 
            }
        }

        public static void BatchedPackets(Packet packet)
        {
            while (packet.UnreadLength() > 0)
            {
                int length = packet.ReadInt();
                byte[] data = packet.ReadBytes(length);

                using (Packet childPacket = new Packet(data))
                {
                    int packetId = childPacket.ReadInt();
                    Client.packetHandlers[packetId](childPacket);
                }
            }
        }
    }
}
