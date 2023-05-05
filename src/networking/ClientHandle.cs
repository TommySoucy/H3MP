using FistVR;
using H3MP.Patches;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Networking
{
    internal class ClientHandle
    {
        public static void Welcome(Packet packet)
        {
            string msg = packet.ReadString();
            int ID = packet.ReadInt();
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

            WristMenuSection.UpdateMaxHealth(GameManager.scene, GameManager.instance, -2, -1);

            Mod.LogInfo($"Message from server: {msg}", false);

            Client.singleton.gotWelcome = true;
            Client.singleton.ID = ID;
            GameManager.ID = ID;
            ClientSend.WelcomeReceived();

            Client.singleton.udp.Connect(((IPEndPoint)Client.singleton.tcp.socket.Client.LocalEndPoint).Port);
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
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            int IFF = packet.ReadInt();
            int colorIndex = packet.ReadInt();
            bool join = packet.ReadBool();

            GameManager.singleton.SpawnPlayer(ID, username, scene, instance, position, rotation, IFF, colorIndex, join);
        }

        public static void ConnectSync(Packet packet)
        {
            bool inControl = packet.ReadBool();

            // Just connected, sync if current scene is syncable
            if (!GameManager.nonSynchronizedScenes.ContainsKey(GameManager.scene))
            {
                GameManager.SyncTrackedSosigs(true, inControl);
                GameManager.SyncTrackedAutoMeaters(true, inControl);
                GameManager.SyncTrackedItems(true, inControl);
                GameManager.SyncTrackedEncryptions(true, inControl);
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
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                TrackedObjectData.Update(packet);
            }
        }

        public static void ObjectUpdate(Packet packet)
        {
            TrackedObjectData.Update(packet);
        }

        public static void TrackedItems(Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void ItemUpdate(Packet packet)
        {
            GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
        }

        public static void TrackedSosigs(Packet packet)
        {
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TrackedAutoMeaters(Packet packet)
        {
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                GameManager.UpdateTrackedAutoMeater(packet.ReadTrackedAutoMeater());
            }
        }

        public static void TrackedEncryptions(Packet packet)
        {
            // Reconstruct passed TrackedEncryptions from packet
            int count = packet.ReadShort();
            for (int i = 0; i < count; ++i)
            {
                GameManager.UpdateTrackedEncryption(packet.ReadTrackedEncryption());
            }
        }

        public static void TrackedObject(Packet packet)
        {
            Client.AddTrackedObject((TrackedObjectData)Activator.CreateInstance(Mod.trackedObjectTypes[packet.ReadString()], packet));
        }

        public static void TrackedItem(Packet packet)
        {
            Client.AddTrackedItem(packet.ReadTrackedItem(true));
        }

        public static void TrackedSosig(Packet packet)
        {
            Client.AddTrackedSosig(packet.ReadTrackedSosig(true));
        }

        public static void TrackedAutoMeater(Packet packet)
        {
            Client.AddTrackedAutoMeater(packet.ReadTrackedAutoMeater(true));
        }

        public static void TrackedEncryption(Packet packet)
        {
            Client.AddTrackedEncryption(packet.ReadTrackedEncryption(true));
        }

        public static void AddNonSyncScene(Packet packet)
        {
            int ID = packet.ReadInt();
            string scene = packet.ReadString();

            GameManager.nonSynchronizedScenes.Add(scene, ID);
        }

        public static void GiveControl(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            TrackedItemData trackedItem = Client.items[trackedID];
            Mod.LogInfo("Client received order to set control of item " + trackedID + " to " + controllerID);

            if (trackedItem != null)
            {
                Mod.LogInfo("\tGot item data for "+trackedItem.itemID);
                bool destroyed = false;
                if (trackedItem.controller == Client.singleton.ID && controllerID != Client.singleton.ID)
                {
                    Mod.LogInfo("\t\tGiving up control");
                    FVRPhysicalObject physObj = trackedItem.physicalItem.GetComponent<FVRPhysicalObject>();

                    GameManager.EnsureUncontrolled(physObj);

                    Mod.SetKinematicRecursive(physObj.transform, true);

                    trackedItem.RemoveFromLocal();
                }
                else if (trackedItem.controller != Client.singleton.ID && controllerID == Client.singleton.ID)
                {
                    Mod.LogInfo("\t\tTaking control");
                    trackedItem.localTrackedID = GameManager.items.Count;
                    GameManager.items.Add(trackedItem);
                    if (trackedItem.physicalItem == null)
                    {
                        // If it is null and we receive this after having finishing loading, we only want to instantiate if it is in our current scene/instance
                        if (!GameManager.sceneLoading)
                        {
                            if (trackedItem.scene.Equals(GameManager.scene) && trackedItem.instance == GameManager.instance)
                            {
                                if (!trackedItem.awaitingInstantiation)
                                {
                                    trackedItem.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedItem.Instantiate());
                                }
                            }
                            else
                            {
                                // Scene not loading but object is not in our scene/instance, try bouncing or destroy
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedItem.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedItem.controller, true, true, newPlayerList, trackedItem.scene, trackedItem.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroyItem(trackedID);
                                        trackedItem.RemoveFromLocal();
                                        Client.items[trackedID] = null;
                                        if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                        {
                                            itemList.Remove(trackedItem.trackedID);
                                        }
                                        trackedItem.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedItem.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyItem(trackedID);
                                    trackedItem.RemoveFromLocal();
                                    Client.items[trackedID] = null;
                                    if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                    {
                                        itemList.Remove(trackedItem.trackedID);
                                    }
                                    trackedItem.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else
                        {
                            // Only bounce control or destroy if we are not on our way towards the object's scene/instance
                            if (!trackedItem.scene.Equals(LoadLevelBeginPatch.loadingLevel) || trackedItem.instance != GameManager.instance)
                            {
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedItem.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedItem.controller, true, true, newPlayerList, trackedItem.scene, trackedItem.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroyItem(trackedID);
                                        trackedItem.RemoveFromLocal();
                                        Client.items[trackedID] = null;
                                        if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                        {
                                            itemList.Remove(trackedItem.trackedID);
                                        }
                                        trackedItem.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedItem.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyItem(trackedID);
                                    trackedItem.RemoveFromLocal();
                                    Client.items[trackedID] = null;
                                    if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                    {
                                        itemList.Remove(trackedItem.trackedID);
                                    }
                                    trackedItem.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                            // else, Loading on our way to the object's scene/instance, will instantiate when we arrive
                        }
                    }
                    else
                    {
                        Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
                    }
                }

                if (!destroyed)
                {
                    trackedItem.SetController(controllerID);
                }
            }
        }

        public static void GiveSosigControl(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }
            Mod.LogInfo("GiveSosigControl handle with trackedID: "+trackedID+ ", controllerID: "+controllerID);

            TrackedSosigData trackedSosig = Client.sosigs[trackedID];

            if (trackedSosig != null)
            {
                Mod.LogInfo("\tWe have data");
                bool destroyed = false;
                if (trackedSosig.controller == Client.singleton.ID && controllerID != Client.singleton.ID)
                {
                    Mod.LogInfo("\t\tWe must give up control");
                    trackedSosig.RemoveFromLocal();

                    if (trackedSosig.physicalObject != null)
                    {
                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.DeRegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                        }
                        trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
                    }
                }
                else if (trackedSosig.controller != Client.singleton.ID && controllerID == Client.singleton.ID)
                {
                    Mod.LogInfo("\t\tWe must take control");
                    trackedSosig.localTrackedID = GameManager.sosigs.Count;
                    GameManager.sosigs.Add(trackedSosig);

                    if (trackedSosig.physicalObject == null)
                    {
                        Mod.LogInfo("\t\t\tWe do not have phys yet");
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!GameManager.sceneLoading)
                        {
                            Mod.LogInfo("\t\t\t\tScene not loading");
                            if (trackedSosig.scene.Equals(GameManager.scene) && trackedSosig.instance == GameManager.instance)
                            {
                                Mod.LogInfo("\t\t\t\t\tIs in our scene/instance, instantiating");
                                if (!trackedSosig.awaitingInstantiation)
                                {
                                    trackedSosig.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedSosig.Instantiate());
                                }
                            }
                            else
                            {
                                Mod.LogInfo("\t\t\t\t\tIs not in our scene/instance, bouncing control or destroying");
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedSosig.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedSosig.controller, true, true, newPlayerList, trackedSosig.scene, trackedSosig.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroySosig(trackedID);
                                        trackedSosig.RemoveFromLocal();
                                        Client.sosigs[trackedID] = null;
                                        if (GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                                        {
                                            sosigList.Remove(trackedSosig.trackedID);
                                        }
                                        trackedSosig.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedSosig.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveSosigControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroySosig(trackedID);
                                    trackedSosig.RemoveFromLocal();
                                    Client.sosigs[trackedID] = null;
                                    if (GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                                    {
                                        sosigList.Remove(trackedSosig.trackedID);
                                    }
                                    trackedSosig.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else
                        {
                            Mod.LogInfo("\t\t\t\tScene loading");
                            // Only bounce control or destroy if we are not on our way towards the object's scene/instance
                            if (!trackedSosig.scene.Equals(LoadLevelBeginPatch.loadingLevel) || trackedSosig.instance != GameManager.instance)
                            {
                                Mod.LogInfo("\t\t\t\t\tNot in our destination scene/instance");
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedSosig.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedSosig.controller, true, true, newPlayerList, trackedSosig.scene, trackedSosig.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroySosig(trackedID);
                                        trackedSosig.RemoveFromLocal();
                                        Client.sosigs[trackedID] = null;
                                        if (GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                                        {
                                            sosigList.Remove(trackedSosig.trackedID);
                                        }
                                        trackedSosig.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedSosig.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveSosigControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroySosig(trackedID);
                                    trackedSosig.RemoveFromLocal();
                                    Client.sosigs[trackedID] = null;
                                    if (GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                                    {
                                        sosigList.Remove(trackedSosig.trackedID);
                                    }
                                    trackedSosig.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                            // else, Loading on our way to the object's scene/instance, will instantiate when we arrive
                        }
                    }
                    else
                    {
                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                        }
                        trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                    }
                }

                if (!destroyed)
                {
                    trackedSosig.controller = controllerID;

                    if(controllerID == GameManager.ID)
                    {
                        trackedSosig.TakeInventoryControl();
                    }
                }
            }
        }

        public static void GiveAutoMeaterControl(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[trackedID];

            if (trackedAutoMeater != null)
            {
                bool destroyed = false;
                if (trackedAutoMeater.controller == Client.singleton.ID && controllerID != Client.singleton.ID)
                {
                    trackedAutoMeater.RemoveFromLocal();

                    if (trackedAutoMeater.physicalObject != null)
                    {

                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.DeRegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                        }
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
                    }
                }
                else if (trackedAutoMeater.controller != Client.singleton.ID && controllerID == Client.singleton.ID)
                {
                    trackedAutoMeater.localTrackedID = GameManager.autoMeaters.Count;
                    GameManager.autoMeaters.Add(trackedAutoMeater);

                    if (trackedAutoMeater.physicalObject == null)
                    {
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!GameManager.sceneLoading)
                        {
                            if (trackedAutoMeater.scene.Equals(GameManager.scene) && trackedAutoMeater.instance == GameManager.instance)
                            {
                                if (!trackedAutoMeater.awaitingInstantiation)
                                {
                                    trackedAutoMeater.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                                }
                            }
                            else
                            {
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedAutoMeater.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedAutoMeater.controller, true, true, newPlayerList, trackedAutoMeater.scene, trackedAutoMeater.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroySosig(trackedID);
                                        trackedAutoMeater.RemoveFromLocal();
                                        Client.autoMeaters[trackedID] = null;
                                        if (GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                                        {
                                            autoMeaterList.Remove(trackedAutoMeater.trackedID);
                                        }
                                        trackedAutoMeater.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedAutoMeater.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveAutoMeaterControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyAutoMeater(trackedID);
                                    trackedAutoMeater.RemoveFromLocal();
                                    Client.autoMeaters[trackedID] = null;
                                    if (GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                                    {
                                        autoMeaterList.Remove(trackedAutoMeater.trackedID);
                                    }
                                    trackedAutoMeater.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else
                        {
                            // Only bounce control or destroy if we are not on our way towards the object's scene/instance
                            if (!trackedAutoMeater.scene.Equals(LoadLevelBeginPatch.loadingLevel) || trackedAutoMeater.instance != GameManager.instance)
                            {
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedAutoMeater.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedAutoMeater.controller, true, true, newPlayerList, trackedAutoMeater.scene, trackedAutoMeater.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroySosig(trackedID);
                                        trackedAutoMeater.RemoveFromLocal();
                                        Client.autoMeaters[trackedID] = null;
                                        if (GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                                        {
                                            autoMeaterList.Remove(trackedAutoMeater.trackedID);
                                        }
                                        trackedAutoMeater.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedAutoMeater.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveAutoMeaterControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyAutoMeater(trackedID);
                                    trackedAutoMeater.RemoveFromLocal();
                                    Client.autoMeaters[trackedID] = null;
                                    if (GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                                    {
                                        autoMeaterList.Remove(trackedAutoMeater.trackedID);
                                    }
                                    trackedAutoMeater.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                            // else, Loading on our way to the object's scene/instance, will instantiate when we arrive
                        }
                    }
                    else
                    {
                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.RegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                        }
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                    }
                }

                if (!destroyed)
                {
                    trackedAutoMeater.controller = controllerID;
                }
            }
        }

        public static void GiveEncryptionControl(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int controllerID = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            TrackedEncryptionData trackedEncryption = Client.encryptions[trackedID];

            if (trackedEncryption != null)
            {
                bool destroyed = false;
                if (trackedEncryption.controller == Client.singleton.ID && controllerID != Client.singleton.ID)
                {
                    trackedEncryption.RemoveFromLocal();
                }
                else if (trackedEncryption.controller != Client.singleton.ID && controllerID == Client.singleton.ID)
                {
                    trackedEncryption.localTrackedID = GameManager.encryptions.Count;
                    GameManager.encryptions.Add(trackedEncryption);

                    if (trackedEncryption.physicalObject == null)
                    {
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!GameManager.sceneLoading)
                        {
                            if (trackedEncryption.scene.Equals(GameManager.scene) && trackedEncryption.instance == GameManager.instance)
                            {
                                if (!trackedEncryption.awaitingInstantiation)
                                {
                                    trackedEncryption.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedEncryption.Instantiate());
                                }
                            }
                            else
                            {
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedEncryption.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedEncryption.controller, true, true, newPlayerList, trackedEncryption.scene, trackedEncryption.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroyEncryption(trackedID);
                                        trackedEncryption.RemoveFromLocal();
                                        Client.encryptions[trackedID] = null;
                                        if (GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                        {
                                            encryptionList.Remove(trackedEncryption.trackedID);
                                        }
                                        trackedEncryption.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedEncryption.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveEncryptionControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyEncryption(trackedID);
                                    trackedEncryption.RemoveFromLocal();
                                    Client.encryptions[trackedID] = null;
                                    if (GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                    {
                                        encryptionList.Remove(trackedEncryption.trackedID);
                                    }
                                    trackedEncryption.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else
                        {
                            // Only bounce control or destroy if we are not on our way towards the object's scene/instance
                            if (!trackedEncryption.scene.Equals(LoadLevelBeginPatch.loadingLevel) || trackedEncryption.instance != GameManager.instance)
                            {
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedEncryption.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    controllerID = Mod.GetBestPotentialObjectHost(trackedEncryption.controller, true, true, newPlayerList, trackedEncryption.scene, trackedEncryption.instance);
                                    if (controllerID == -1)
                                    {
                                        ClientSend.DestroyEncryption(trackedID);
                                        trackedEncryption.RemoveFromLocal();
                                        Client.encryptions[trackedID] = null;
                                        if (GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                        {
                                            encryptionList.Remove(trackedEncryption.trackedID);
                                        }
                                        trackedEncryption.awaitingInstantiation = false;
                                        destroyed = true;
                                    }
                                    else
                                    {
                                        trackedEncryption.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        ClientSend.GiveEncryptionControl(trackedID, controllerID, debounce);
                                    }
                                }
                                else
                                {
                                    ClientSend.DestroyEncryption(trackedID);
                                    trackedEncryption.RemoveFromLocal();
                                    Client.encryptions[trackedID] = null;
                                    if (GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                    {
                                        encryptionList.Remove(trackedEncryption.trackedID);
                                    }
                                    trackedEncryption.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                            // else, Loading on our way to the object's scene/instance, will instantiate when we arrive
                        }
                    }
                }

                if (!destroyed)
                {
                    trackedEncryption.controller = controllerID;
                }
            }
        }

        public static void DestroyItem(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            TrackedItemData trackedItem = Client.items[trackedID];

            if (trackedItem != null)
            {
                trackedItem.awaitingInstantiation = false;

                bool destroyed = false;
                if (trackedItem.physicalItem != null)
                {
                    trackedItem.removeFromListOnDestroy = removeFromList;
                    trackedItem.physicalItem.sendDestroy = false;
                    trackedItem.physicalItem.dontGiveControl = true;
                    GameObject.Destroy(trackedItem.physicalItem.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedItem.controller == Client.singleton.ID)
                {
                    trackedItem.RemoveFromLocal();
                }

                if (!destroyed && removeFromList)
                {
                    Client.items[trackedID] = null;
                    GameManager.itemsByInstanceByScene[trackedItem.scene][trackedItem.instance].Remove(trackedID);
                }
            }
        }

        public static void DestroySosig(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            Mod.LogInfo("Received order to destroy Sosig: " + trackedID);
            TrackedSosigData trackedSosig = Client.sosigs[trackedID];

            if (trackedSosig != null)
            {
                trackedSosig.awaitingInstantiation = false;
                bool destroyed = false;
                if (trackedSosig.physicalObject != null)
                {
                    trackedSosig.removeFromListOnDestroy = removeFromList;
                    trackedSosig.physicalObject.sendDestroy = false;
                    foreach (SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
                    {
                        if (link != null)
                        {
                            GameObject.Destroy(link.gameObject);
                        }
                    }
                    trackedSosig.physicalObject.dontGiveControl = true;
                    GameObject.Destroy(trackedSosig.physicalObject.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedSosig.controller == Client.singleton.ID)
                {
                    trackedSosig.RemoveFromLocal();
                }

                if (!destroyed && removeFromList)
                {
                    Client.sosigs[trackedID] = null;
                    GameManager.sosigsByInstanceByScene[trackedSosig.scene][trackedSosig.instance].Remove(trackedID);
                }
            }
        }

        public static void DestroyAutoMeater(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[trackedID];

            if (trackedAutoMeater != null)
            {
                trackedAutoMeater.awaitingInstantiation = false;
                bool destroyed = false;
                if (trackedAutoMeater.physicalObject != null)
                {
                    trackedAutoMeater.removeFromListOnDestroy = removeFromList;
                    trackedAutoMeater.physicalObject.sendDestroy = false;
                    trackedAutoMeater.physicalObject.dontGiveControl = true;
                    GameObject.Destroy(trackedAutoMeater.physicalObject.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedAutoMeater.controller == Client.singleton.ID)
                {
                    trackedAutoMeater.RemoveFromLocal();
                }

                if (!destroyed && removeFromList)
                {
                    Client.autoMeaters[trackedID] = null;
                    GameManager.autoMeatersByInstanceByScene[trackedAutoMeater.scene][trackedAutoMeater.instance].Remove(trackedID);
                }
            }
        }

        public static void DestroyEncryption(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();

            TrackedEncryptionData trackedEncryption = Client.encryptions[trackedID];

            if (trackedEncryption != null)
            {
                trackedEncryption.awaitingInstantiation = false;
                bool destroyed = false;
                if (trackedEncryption.physicalObject != null)
                {
                    trackedEncryption.removeFromListOnDestroy = removeFromList;
                    trackedEncryption.physicalObject.sendDestroy = false;
                    trackedEncryption.physicalObject.dontGiveControl = true;
                    GameObject.Destroy(trackedEncryption.physicalObject.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedEncryption.controller == Client.singleton.ID)
                {
                    trackedEncryption.RemoveFromLocal();
                }

                if (!destroyed && removeFromList)
                {
                    Client.encryptions[trackedID] = null;
                    GameManager.encryptionsByInstanceByScene[trackedEncryption.scene][trackedEncryption.instance].Remove(trackedID);
                }
            }
        }

        public static void ItemParent(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            if (Client.items[trackedID] != null)
            {
                Client.items[trackedID].SetParent(newParentID);
            }
        }

        public static void WeaponFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                Client.items[trackedID].physicalItem.setFirearmUpdateOverride(roundType, roundClass, chamberIndex);
                ++ProjectileFirePatch.skipBlast;
                Client.items[trackedID].physicalItem.fireFunc(chamberIndex);
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void FlintlockWeaponBurnOffOuter(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = Client.items[trackedID].physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = Client.items[trackedID].physicalItem.physicalObject as FlintlockWeapon;
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
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = Client.items[trackedID].physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = Client.items[trackedID].physicalItem.physicalObject as FlintlockWeapon;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                BreakActionWeapon asBAW = Client.items[trackedID].physicalItem.physicalObject as BreakActionWeapon;
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
                    Mod.LogError("Received order to fire break action weapon at " + trackedID + " but the item is not a BreakActionWeapon. It is actually: "+(Client.items[trackedID].physicalItem.physicalObject.GetType()));
                }
            }
        }

        public static void DerringerFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                Derringer asDerringer = Client.items[trackedID].physicalItem.physicalObject as Derringer;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                RevolvingShotgun asRS = Client.items[trackedID].physicalItem.physicalObject as RevolvingShotgun;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                Revolver asRevolver = Client.items[trackedID].physicalItem.physicalObject as Revolver;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                SingleActionRevolver asRevolver = Client.items[trackedID].physicalItem.physicalObject as SingleActionRevolver;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                FireStingerLauncherPatch.targetPos = packet.ReadVector3();
                FireStingerLauncherPatch.position = packet.ReadVector3();
                FireStingerLauncherPatch.direction = packet.ReadVector3();
                FireStingerLauncherPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireStingerLauncherPatch.skip;
                StingerLauncher asStingerLauncher = Client.items[trackedID].physicalItem.physicalObject as StingerLauncher;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                GrappleGun asGG = Client.items[trackedID].physicalItem.physicalObject as GrappleGun;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                float cookedAmount = packet.ReadFloat();
                FireHCBPatch.position = packet.ReadVector3();
                FireHCBPatch.direction = packet.ReadVector3();
                FireHCBPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireHCBPatch.releaseSledSkip;
                HCB asHCB = Client.items[trackedID].physicalItem.physicalObject as HCB;
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
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                LeverActionFirearm asLAF = Client.items[trackedID].physicalItem.dataObject as LeverActionFirearm;
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                SosigWeaponPlayerInterface asInterface = Client.items[trackedID].physicalItem.dataObject as SosigWeaponPlayerInterface;
                if (asInterface.W.m_shotsLeft <= 0)
                {
                    asInterface.W.m_shotsLeft = 1;
                }
                asInterface.W.MechaState = SosigWeapon.SosigWeaponMechaState.ReadyToFire;
                Client.items[trackedID].physicalItem.sosigWeaponfireFunc(recoilMult);
            }
        }

        public static void LAPD2019Fire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                LAPD2019 asLAPD2019 = Client.items[trackedID].physicalItem.physicalObject as LAPD2019;
                asLAPD2019.CurChamber = chamberIndex;
                FireArmRoundType prevRoundType = asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].SetRound(roundClass, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.position, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = prevRoundType;
                ++ProjectileFirePatch.skipBlast;
                ((LAPD2019)Client.items[trackedID].physicalItem.physicalObject).Fire();
                --ProjectileFirePatch.skipBlast;
            }
        }
        
        public static void MinigunFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                FireMinigunPatch.positions = new List<Vector3>();
                FireMinigunPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireMinigunPatch.positions.Add(packet.ReadVector3());
                    FireMinigunPatch.directions.Add(packet.ReadVector3());
                }
                FireMinigunPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                ((LAPD2019)Client.items[trackedID].physicalItem.physicalObject).Fire();
            }
        }
        
        public static void AttachableFirearmFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                Client.items[trackedID].physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                Client.items[trackedID].physicalItem.attachableFirearmFireFunc(firedFromInterface);
                --ProjectileFirePatch.skipBlast;
            }
        }
        
        public static void IntegratedFirearmFire(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
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
                Client.items[trackedID].physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                Client.items[trackedID].physicalItem.attachableFirearmFireFunc(false);
                --ProjectileFirePatch.skipBlast;
            }
        }

        public static void LAPD2019LoadBattery(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null && Client.items[batteryTrackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)Client.items[trackedID].physicalItem.physicalObject).LoadBattery((LAPD2019Battery)Client.items[batteryTrackedID].physicalItem.physicalObject);
                --LAPD2019ActionPatch.loadBatterySkip;
            }
        }

        public static void LAPD2019ExtractBattery(Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)Client.items[trackedID].physicalItem.physicalObject).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }
        }

        public static void SosigWeaponShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                ++SosigWeaponShatterPatch.skip;
                (Client.items[trackedID].physicalItem.physicalObject as SosigWeaponPlayerInterface).W.Shatter();
                --SosigWeaponShatterPatch.skip;
            }
        }

        public static void AutoMeaterFirearmFireShot(Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            if (Client.items[trackedID] != null && Client.autoMeaters[trackedID].physicalObject != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                Client.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0].FireShot();
                --AutoMeaterFirearmFireShotPatch.skip;
            }
        }

        public static void PlayerDamage(Packet packet)
        {
            PlayerHitbox.Part part = (PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            Mod.LogInfo("Client received player damage for itself",false);
            GameManager.ProcessPlayerDamage(part, damage);
        }

        public static void UberShatterableShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                ++UberShatterableShatterPatch.skip;
                Client.items[trackedID].physicalItem.GetComponent<UberShatterable>().Shatter(packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat());
                --UberShatterableShatterPatch.skip;
            }
        }

        public static void SosigPickUpItem(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if(trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = itemTrackedID;

                if (trackedSosig.physicalObject != null)
                {
                    if (Client.items[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at "+itemTrackedID+" is missing item data!");
                    }
                    else if (Client.items[itemTrackedID].physicalItem == null)
                    {
                        Client.items[itemTrackedID].toPutInSosigInventory = new int[] { sosigTrackedID, primaryHand ? 0 : 1 };
                    }
                    else
                    {
                        ++SosigPickUpPatch.skip;
                        if (primaryHand)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                        }
                        else
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if(trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = itemTrackedID;

                if (trackedSosig.physicalObject != null)
                {
                    if (Client.items[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (Client.items[itemTrackedID].physicalItem == null)
                    {
                        Client.items[itemTrackedID].toPutInSosigInventory = new int[] { sosigTrackedID, slotIndex + 2 };
                    }
                    else
                    {
                        ++SosigPlaceObjectInPatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].PlaceObjectIn(Client.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                        --SosigPlaceObjectInPatch.skip;
                    }
                }
            }
        }

        public static void SosigDropSlot(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if(trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = -1;

                if (trackedSosig.physicalObject != null)
                {
                    ++SosigSlotDetachPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].DetachHeldObject();
                    --SosigSlotDetachPatch.skip;
                }
            }
        }

        public static void SosigHandDrop(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if(trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = -1;

                if (trackedSosig.physicalObject != null)
                {
                    ++SosigHandDropPatch.skip;
                    if (primaryHand)
                    {
                        trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.DropHeldObject();
                    }
                    else
                    {
                        trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.DropHeldObject();
                    }
                    --SosigHandDropPatch.skip;
                }
            }
        }

        public static void SosigConfigure(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;

                if (trackedSosig.physicalObject != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalObject.physicalSosigScript.Configure(config);
                }
            }
        }

        public static void SosigLinkRegisterWearable(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if(trackedSosig.physicalObject != null)
                    {
                        foreach(SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
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

                if (trackedSosig.physicalObject != null)
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables != null)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                        {
                            if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                            {
                                trackedSosig.wearables[linkIndex].RemoveAt(i);
                                if (trackedSosig.physicalObject != null)
                                {
                                    ++SosigLinkActionPatch.skipDeRegisterWearable;
                                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].DeRegisterWearable(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].m_wearables[i]);
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetIFF(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigSetOriginalIFF(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetOriginalIFFTeam(IFF);
                    --SosigIFFPatch.skip;
                }
            }
        }

        public static void SosigLinkDamage(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller == Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null &&
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex] != null &&
                        !trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].IsExploded)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].Damage(damage);
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

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalObject != null)
                    {
                        ++AutoMeaterDamagePatch.skip;
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Damage(damage);
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

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.physicalObject != null)
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
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedEncryptionData trackedEncryption = Client.encryptions[autoMeaterTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == Client.singleton.ID)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.Damage(damage);
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

            TrackedEncryptionData trackedEncryption = Client.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == GameManager.ID)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
            }
        }

        public static void SosigWeaponDamage(Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData trackedItem = Client.items[sosigWeaponTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++SosigWeaponDamagePatch.skip;
                        (trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W.Damage(damage);
                        --SosigWeaponDamagePatch.skip;
                    }
                }
            }
        }

        public static void RemoteMissileDamage(Packet packet)
        {
            int RMLTrackedID = packet.ReadInt();

            TrackedItemData trackedItem = Client.items[RMLTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        RemoteMissile remoteMissile = (Client.items[RMLTrackedID].physicalItem.physicalObject as RemoteMissileLauncher).m_missile;
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

            TrackedItemData trackedItem = Client.items[SLTrackedID];
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                byte linkIndex = packet.ReadByte();
                byte wearableIndex = packet.ReadByte();
                Damage damage = packet.ReadDamage();

                if (trackedSosig.controller == Client.singleton.ID)
                {
                    if (trackedSosig.physicalObject != null &&
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex] != null &&
                        !trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].IsExploded)
                    {
                        ++SosigWearableDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].m_wearables[wearableIndex].Damage(damage);
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigSetBodyStateSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SetBodyState(bodyState);
                    --SosigActionPatch.sosigSetBodyStateSkip;
                }
            }
        }

        public static void SosigDamageData(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.controller != Client.singleton.ID && trackedSosig.physicalObject != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalObject.physicalSosigScript;
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

            TrackedEncryptionData trackedEncryption = Client.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller != Client.singleton.ID && trackedEncryption.physicalObject != null)
                {
                    trackedEncryption.physicalObject.physicalEncryptionScript.m_numHitsLeft = packet.ReadInt();
                }
            }
        }

        public static void AutoMeaterHitZoneDamageData(Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.controller != Client.singleton.ID && trackedAutoMeater.physicalObject != null)
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

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }
        }

        public static void SosigDies(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }
        }

        public static void SosigClear(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosigScript.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
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

            if (Client.sosigs[sosigTrackedID] != null && Client.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = Client.sosigs[sosigTrackedID].physicalObject.physicalSosigScript.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }
        }

        public static void SosigSpeakState(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander);
                        break;
                    case Sosig.SosigOrder.Investigate:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnInvestigate);
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnSearchingForGuns);
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnTakingCover);
                        break;
                    case Sosig.SosigOrder.Wander:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander);
                        break;
                    case Sosig.SosigOrder.Assault:
                        trackedSosig.physicalObject.physicalSosigScript.Speak_State(trackedSosig.physicalObject.physicalSosigScript.Speech.OnAssault);
                        break;
                }
            }
        }

        public static void SosigSetCurrentOrder(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();
            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.currentOrder = currentOrder;
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        trackedSosig.guardPoint = packet.ReadVector3();
                        trackedSosig.guardDir = packet.ReadVector3();
                        trackedSosig.hardGuard = packet.ReadBool();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.CommandGuardPoint(trackedSosig.guardPoint, trackedSosig.hardGuard);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.m_guardDominantDirection = trackedSosig.guardDir;
                        }
                        break;
                    case Sosig.SosigOrder.Skirmish:
                        trackedSosig.skirmishPoint = packet.ReadVector3();
                        trackedSosig.pathToPoint = packet.ReadVector3();
                        trackedSosig.assaultPoint = packet.ReadVector3();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.m_skirmishPoint = trackedSosig.skirmishPoint;
                            trackedSosig.physicalObject.physicalSosigScript.m_pathToPoint = trackedSosig.pathToPoint;
                            trackedSosig.physicalObject.physicalSosigScript.m_assaultPoint = trackedSosig.assaultPoint;
                            trackedSosig.physicalObject.physicalSosigScript.m_faceTowards = trackedSosig.faceTowards;
                        }
                        break;
                    case Sosig.SosigOrder.Investigate:
                        trackedSosig.guardPoint = packet.ReadVector3();
                        trackedSosig.hardGuard = packet.ReadBool();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.UpdateGuardPoint(trackedSosig.guardPoint);
                            trackedSosig.physicalObject.physicalSosigScript.m_hardGuard = trackedSosig.hardGuard;
                            trackedSosig.physicalObject.physicalSosigScript.m_faceTowards = trackedSosig.faceTowards;
                        }
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                    case Sosig.SosigOrder.Wander:
                        trackedSosig.wanderPoint = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.m_wanderPoint = trackedSosig.wanderPoint;
                        }
                        break;
                    case Sosig.SosigOrder.Assault:
                        trackedSosig.assaultPoint = packet.ReadVector3();
                        trackedSosig.assaultSpeed = (Sosig.SosigMoveSpeed)packet.ReadByte();
                        trackedSosig.faceTowards = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.CommandAssaultPoint(trackedSosig.assaultPoint);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.m_faceTowards = trackedSosig.faceTowards;
                            trackedSosig.physicalObject.physicalSosigScript.SetAssaultSpeed(trackedSosig.assaultSpeed);
                        }
                        break;
                    case Sosig.SosigOrder.Idle:
                        trackedSosig.idleToPoint = packet.ReadVector3();
                        trackedSosig.idleDominantDir = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.CommandIdle(trackedSosig.idleToPoint, trackedSosig.idleDominantDir);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                        }
                        break;
                    case Sosig.SosigOrder.PathTo:
                        trackedSosig.pathToPoint = packet.ReadVector3();
                        trackedSosig.pathToLookDir = packet.ReadVector3();
                        if (trackedSosig.physicalObject != null)
                        {
                            ++SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                            --SosigActionPatch.sosigSetCurrentOrderSkip;
                            trackedSosig.physicalObject.physicalSosigScript.m_pathToPoint = trackedSosig.pathToPoint;
                            trackedSosig.physicalObject.physicalSosigScript.m_pathToLookDir = trackedSosig.pathToLookDir;
                        }
                        break;
                    default:
                        trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                        break;
                }
            }
        }

        public static void SosigVaporize(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosigScript.Vaporize(trackedSosig.physicalObject.physicalSosigScript.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }
        }

        public static void SosigLinkBreak(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }
        }

        public static void SosigLinkSever(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].SeverJoint((Damage.DamageClass)damClass, isPullApart);
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }
        }

        public static void SosigRequestHitDecal(Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Client.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                if (trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex] != null)
                {
                    ++SosigActionPatch.sosigRequestHitDecalSkip;
                    trackedSosig.physicalObject.physicalSosigScript.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]);
                    --SosigActionPatch.sosigRequestHitDecalSkip;
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
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i);
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

            // The instance may not exist anymore if we were the last one in there and we left between
            // server sending us order to set TNH controller and receiving it
            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance i))
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

            if (Client.autoMeaters[trackedID] != null && Client.autoMeaters[trackedID].physicalObject != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                Client.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript.SetState((AutoMeater.AutoMeaterState)state);
                --AutoMeaterSetStatePatch.skip;
            }
        }

        public static void AutoMeaterSetBladesActive(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                if (active)
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].Reactivate();
                    }
                }
                else
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].ShutDown();
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

            Mod.LogInfo("Received auto meater firea ta will order, trackedID: " + trackedID + ", firearmIndex: " + firearmIndex+", automeaters length: "+ Client.autoMeaters, false);
            TrackedAutoMeaterData trackedAutoMeater = Client.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                Mod.LogInfo("\tFirearms count: "+ trackedAutoMeater.physicalObject.physicalAutoMeaterScript.FireControl.Firearms.Count, false);
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }
        }

        public static void TNHSosigKill(Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                TrackedSosigData trackedSosig = Client.sosigs[trackedID];
                if (trackedSosig != null && trackedSosig.physicalObject != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.currentTNHInstance.manager.OnSosigKill(trackedSosig.physicalObject.physicalSosigScript);
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
                    if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                    {
                        // Begin hold on our side
                        ++TNH_HoldPointPatch.beginHoldSendSkip;
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
                    Mod.currentTNHInstance.manager.m_curHoldPoint.BeginHoldChallenge();

                    // TP to point since we are not the one who started the hold
                    if (!actualInstance.dead.Contains(GameManager.ID) || Mod.TNHOnDeathSpectate)
                    {
                        GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.m_curHoldPoint.SpawnPoint_SystemNode.position, true);
                    }
                }
                else if (actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    // Begin hold on our side
                    ++TNH_HoldPointPatch.beginHoldSendSkip;
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

        public static void ShatterableCrateSetHoldingHealth(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null)
            {
                Client.items[trackedID].identifyingData[1] = 1;

                if (Client.items[trackedID].physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                    Client.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                }
            }
        }

        public static void ShatterableCrateSetHoldingToken(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null)
            {
                Client.items[trackedID].identifyingData[2] = 1;

                if (Client.items[trackedID].physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                    Client.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                }
            }
        }

        public static void ShatterableCrateDamage(Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Client.items[trackedID] != null)
            {
                if (Client.items[trackedID].controller == GameManager.ID)
                {
                    if (Client.items[trackedID].physicalItem != null)
                    {
                        ++TNH_ShatterableCrateDamagePatch.skip;
                        Client.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
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

            if (Client.items[trackedID] != null && Client.items[trackedID].physicalItem != null)
            {
                TNH_ShatterableCrate crateScript = Client.items[trackedID].physicalItem.GetComponentInChildren<TNH_ShatterableCrate>();
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

            if (Client.encryptions[trackedID] != null)
            {
                Client.encryptions[trackedID].subTargsActive[index] = true;

                if (Client.encryptions[trackedID].physicalObject != null)
                {
                    Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(true);
                    ++Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
                }
            }
        }

        public static void EncryptionSpawnGrowth(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (Client.encryptions[trackedID] != null)
            {
                Client.encryptions[trackedID].tendrilsActive[index] = true;
                Client.encryptions[trackedID].growthPoints[index] = point;
                Client.encryptions[trackedID].subTargsPos[index] = point;
                Client.encryptions[trackedID].subTargsActive[index] = true;
                Client.encryptions[trackedID].tendrilFloats[index] = 1f;

                if (Client.encryptions[trackedID].physicalObject != null)
                {
                    Vector3 forward = point - Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                    Client.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                    Client.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                    ++EncryptionSpawnGrowthPatch.skip;
                    Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SpawnGrowth(index, point);
                    --EncryptionSpawnGrowthPatch.skip;
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

            if (Client.encryptions[trackedID] != null)
            {
                if (pointCount > 0)
                {
                    for (int i = 0; i < indexCount; ++i)
                    {
                        Client.encryptions[trackedID].tendrilsActive[indices[i]] = true;
                        Client.encryptions[trackedID].growthPoints[indices[i]] = points[i];
                        Client.encryptions[trackedID].subTargsPos[indices[i]] = points[i];
                        Client.encryptions[trackedID].subTargsActive[indices[i]] = true;
                        Client.encryptions[trackedID].tendrilFloats[indices[i]] = 1f;
                    }

                    if (Client.encryptions[trackedID].physicalObject != null)
                    {
                        ++EncryptionSpawnGrowthPatch.skip;
                        for (int i = 0; i < indexCount; ++i)
                        {
                            Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SpawnGrowth(indices[i], points[i]);
                        }
                        --EncryptionSpawnGrowthPatch.skip;
                    }
                }
                else
                {
                    for (int i = 0; i < indexCount; ++i)
                    {
                        Client.encryptions[trackedID].subTargsActive[indices[i]] = true;
                    }

                    if (Client.encryptions[trackedID].physicalObject != null)
                    {
                        ++EncryptionSpawnGrowthPatch.skip;
                        for (int i = 0; i < indexCount; ++i)
                        {
                            Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[indices[i]].SetActive(true);
                        }
                        --EncryptionSpawnGrowthPatch.skip;

                        Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.m_numSubTargsLeft = indexCount;
                    }
                }
            }
        }

        public static void EncryptionResetGrowth(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (Client.encryptions[trackedID] != null)
            {
                Client.encryptions[trackedID].growthPoints[index] = point;
                Client.encryptions[trackedID].tendrilFloats[index] = 0;
                if (Client.encryptions[trackedID].physicalObject != null)
                {
                    Vector3 forward = point - Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                    Client.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                    Client.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                    ++EncryptionResetGrowthPatch.skip;
                    Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.ResetGrowth(index, point);
                    --EncryptionResetGrowthPatch.skip;
                }
            }
        }

        public static void EncryptionDisableSubtarg(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (Client.encryptions[trackedID] != null)
            {
                Client.encryptions[trackedID].subTargsActive[index] = false;

                if (Client.encryptions[trackedID].physicalObject != null)
                {
                    Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(false);
                    --Client.encryptions[trackedID].physicalObject.physicalEncryptionScript.m_numSubTargsLeft;
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
                    instanceButton.Button.onClick.AddListener(() => { Mod.modInstance.OnTNHInstanceClicked(TNHInstance.instance); });

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
            if (Client.sosigs[trackedID] != null)
            {
                // Update local
                Client.sosigs[trackedID].IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                if (Client.sosigs[trackedID].physicalObject != null)
                {
                    Client.sosigs[trackedID].physicalObject.physicalSosigScript.Priority.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                }
            }
        }

        public static void RemoteMissileDetonate(Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    RemoteMissile remoteMissile = (Client.items[trackedID].physicalItem.physicalObject as RemoteMissileLauncher).m_missile;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    StingerMissile missile = Client.items[trackedID].physicalItem.stingerMissile;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    PinnedGrenade grenade = Client.items[trackedID].physicalItem.physicalObject as PinnedGrenade;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    PinnedGrenade grenade = Client.items[trackedID].physicalItem.physicalObject as PinnedGrenade;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    FVRGrenade grenade = Client.items[trackedID].physicalItem.physicalObject as FVRGrenade;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    BangSnap bangSnap = Client.items[trackedID].physicalItem.physicalObject as BangSnap;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    C4 c4 = Client.items[trackedID].physicalItem.physicalObject as C4;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    ClaymoreMine cm = Client.items[trackedID].physicalItem.physicalObject as ClaymoreMine;
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
            if (Client.items[trackedID] != null)
            {
                // Update local
                if (Client.items[trackedID].physicalItem != null)
                {
                    SLAM slam = Client.items[trackedID].physicalItem.physicalObject as SLAM;
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

        // MOD: A mod that sent initial connection data can handle the data on client side here
        //      Such a mod could patch this handle and access the data by doing the commented lines, then process it however they want
        public static void InitConnectionData(Packet packet)
        {
            //int dataLength = packet.ReadInt();
            //byte[] data = packet.ReadBytes(dataLength);
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

            //TODO: Update UI
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
            if (WristMenuSection.colorByIFFText != null)
            {
                WristMenuSection.colorByIFFText.text = "Color by IFF (" + GameManager.colorByIFF + ")";
            }

            if (GameManager.colorByIFF)
            {
                GameManager.colorIndex = GM.CurrentPlayerBody.GetPlayerIFF() % GameManager.colors.Length;
                if (WristMenuSection.colorText != null)
                {
                    WristMenuSection.colorText.text = "Current color: " + GameManager.colorNames[GameManager.colorIndex];
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
                    if (WristMenuSection.nameplateText != null)
                    {
                        WristMenuSection.nameplateText.text = "Nameplates (All)";
                    }
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(playerEntry.Value.visible);
                    }
                    break;
                case 1:
                    if (WristMenuSection.nameplateText != null)
                    {
                        WristMenuSection.nameplateText.text = "Nameplates (Friendly only)";
                    }
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(playerEntry.Value.visible && GM.CurrentPlayerBody.GetPlayerIFF() == playerEntry.Value.IFF);
                    }
                    break;
                case 2:
                    if (WristMenuSection.nameplateText != null)
                    {
                        WristMenuSection.nameplateText.text = "Nameplates (None)";
                    }
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(false);
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
                    if (WristMenuSection.radarModeText != null)
                    {
                        WristMenuSection.radarModeText.text = "Radar mode (All)";
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
                    if (WristMenuSection.radarModeText != null)
                    {
                        WristMenuSection.radarModeText.text = "Radar mode (Friendly only)";
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
                    if (WristMenuSection.radarModeText != null)
                    {
                        WristMenuSection.radarModeText.text = "Radar mode (None)";
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
            if (WristMenuSection.radarColorText != null)
            {
                WristMenuSection.radarColorText.text = "Radar color IFF (" + GameManager.radarColor + ")";
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
                }
            }
        }

        public static void MaxHealth(Packet packet)
        {
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            int index = packet.ReadInt();
            float original = packet.ReadFloat();

            WristMenuSection.UpdateMaxHealth(scene, instance, index, original);
        }

        public static void FuseIgnite(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is FVRFusedThrowable)
            {
                ++FusePatch.igniteSkip;
                (itemData.physicalItem.physicalObject as FVRFusedThrowable).Fuse.Ignite(0);
                --FusePatch.igniteSkip;
            }
        }

        public static void FuseBoom(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is FVRFusedThrowable)
            {
                (itemData.physicalItem.physicalObject as FVRFusedThrowable).Fuse.Boom();
            }
        }

        public static void MolotovShatter(Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool ignited = packet.ReadBool();

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is Molotov)
            {
                Molotov asMolotov = itemData.physicalItem.physicalObject as Molotov;
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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == Client.singleton.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MolotovPatch.damageSkip;
                        (itemData.physicalItem.physicalObject as Molotov).Damage(damage);
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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MagazinePatch.addRoundSkip;
                        (itemData.physicalItem.physicalObject as FVRFireArmMagazine).AddRound(roundClass, true, true);
                        --MagazinePatch.addRoundSkip;
                    }
                }
            }
        }

        public static void ClipAddRound(Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++ClipPatch.addRoundSkip;
                        (itemData.physicalItem.physicalObject as FVRFireArmClip).AddRound(roundClass, true, true);
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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++SpeedloaderChamberPatch.loadSkip;
                        (itemData.physicalItem.physicalObject as Speedloader).Chambers[chamberIndex].Load(roundClass, true);
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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        FVRFireArmRound round = AM.GetRoundSelfPrefab(roundType, roundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        ++RemoteGunPatch.chamberSkip;
                        (itemData.physicalItem.physicalObject as RemoteGun).ChamberCartridge(round);
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

            TrackedItemData itemData = Client.items[trackedID];
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

            TrackedItemData magItemData = Client.items[trackedID];
            TrackedItemData FAItemData = Client.items[FATrackedID];
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        if (slot == -1)
                        {
                            ++MagazinePatch.loadSkip;
                            (magItemData.physicalItem.physicalObject as FVRFireArmMagazine).Load(FAItemData.physicalItem.physicalObject as FVRFireArm);
                            --MagazinePatch.loadSkip;
                        }
                        else
                        {
                            ++MagazinePatch.loadSkip;
                            (magItemData.physicalItem.physicalObject as FVRFireArmMagazine).LoadIntoSecondary(FAItemData.physicalItem.physicalObject as FVRFireArm, slot);
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

            TrackedItemData magItemData = Client.items[trackedID];
            TrackedItemData FAItemData = Client.items[FATrackedID];
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        ++MagazinePatch.loadSkip;
                        (magItemData.physicalItem.physicalObject as FVRFireArmMagazine).Load(FAItemData.physicalItem.dataObject as AttachableFirearm);
                        --MagazinePatch.loadSkip;
                    }
                }
            }
        }

        public static void ClipLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            TrackedItemData clipItemData = Client.items[trackedID];
            TrackedItemData FAItemData = Client.items[FATrackedID];
            if (clipItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == GameManager.ID)
                {
                    if (FAItemData.physicalItem != null && clipItemData.physicalItem != null)
                    {
                        ++ClipPatch.loadSkip;
                        (clipItemData.physicalItem.physicalObject as FVRFireArmClip).Load(FAItemData.physicalItem.physicalObject as FVRFireArm);
                        --ClipPatch.loadSkip;
                    }
                }
            }
        }

        public static void RevolverCylinderLoad(Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Client.items[trackedID];
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
                        Revolver revolver = itemData.physicalItem.physicalObject as Revolver;
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

            TrackedItemData itemData = Client.items[trackedID];
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
                        RevolvingShotgun revShotgun = itemData.physicalItem.physicalObject as RevolvingShotgun;

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

            TrackedItemData itemData = Client.items[trackedID];
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
                        GrappleGun grappleGun = itemData.physicalItem.physicalObject as GrappleGun;

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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                byte type = packet.ReadByte();
                byte state = packet.ReadByte();

                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustafLatch latch = (itemData.physicalItem.physicalObject as CarlGustaf).TailLatch;
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

            TrackedItemData itemData = Client.items[trackedID];
            if (itemData != null)
            {
                byte state = packet.ReadByte();

                if (itemData.controller == GameManager.ID)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustaf asCG = itemData.physicalItem.physicalObject as CarlGustaf;

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
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                }
            }
        }

        public static void GrappleAttached(Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte[] data = packet.ReadBytes(packet.ReadShort());

            if (Client.items[trackedID] != null && Client.items[trackedID].controller != GameManager.ID)
            {
                Client.items[trackedID].additionalData = data;

                if (Client.items[trackedID].physicalItem != null)
                {
                    GrappleThrowable asGrappleThrowable = Client.items[trackedID].physicalItem.physicalObject as GrappleThrowable;
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

                    int count = Client.items[trackedID].additionalData[1];
                    Vector3 currentRopePoint = new Vector3(BitConverter.ToSingle(Client.items[trackedID].additionalData, 2), BitConverter.ToSingle(Client.items[trackedID].additionalData, 6), BitConverter.ToSingle(Client.items[trackedID].additionalData, 10));
                    for (int i = 1; i < count; ++i)
                    {
                        Vector3 newPoint = new Vector3(BitConverter.ToSingle(Client.items[trackedID].additionalData, i * 12 + 2), BitConverter.ToSingle(Client.items[trackedID].additionalData, i * 12 + 6), BitConverter.ToSingle(Client.items[trackedID].additionalData, i * 12 + 10));
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
    }
}
