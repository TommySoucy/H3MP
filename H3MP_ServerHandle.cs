using FistVR;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace H3MP
{
    internal class H3MP_ServerHandle
    {
        public static void WelcomeReceived(int clientID, H3MP_Packet packet)
        {
            int clientIDCheck = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            int IFF = packet.ReadInt();
            int colorIndex = packet.ReadInt();

            Mod.LogInfo($"{H3MP_Server.clients[clientID].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {clientID}", false);

            if (clientID != clientIDCheck)
            {
                Mod.LogInfo($"Player \"{username}\" (ID:{clientID}) has assumed wrong client ID ({clientIDCheck})", false);
            }

            // Spawn player to clients 
            H3MP_Server.clients[clientID].SendIntoGame(username, scene, instance, IFF, colorIndex);
        }

        public static void Ping(int clientID, H3MP_Packet packet)
        {
            long time = packet.ReadLong();
            H3MP_Server.clients[clientID].ping = Convert.ToInt64((DateTime.Now.ToUniversalTime() - H3MP_ThreadManager.epoch).TotalMilliseconds) - time;
            H3MP_ServerSend.Ping(clientID, time);
        }

        public static void PlayerState(int clientID, H3MP_Packet packet)
        {
            if(H3MP_Server.clients[clientID] == null || H3MP_Server.clients[clientID].player == null)
            {
                // This case is possible if we received player disconnection but we still receive late UDP player state packets
                return;
            }

            H3MP_Player player = H3MP_Server.clients[clientID].player;

            player.position = packet.ReadVector3();
            player.rotation = packet.ReadQuaternion();
            player.headPos = packet.ReadVector3();
            player.headRot = packet.ReadQuaternion();
            player.torsoPos = packet.ReadVector3();
            player.torsoRot = packet.ReadQuaternion();
            player.leftHandPos = packet.ReadVector3();
            player.leftHandRot = packet.ReadQuaternion();
            player.rightHandPos = packet.ReadVector3();
            player.rightHandRot = packet.ReadQuaternion();
            player.health = packet.ReadFloat();
            player.maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if(additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            H3MP_GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                               player.leftHandPos, player.leftHandRot,
                                               player.rightHandPos, player.rightHandRot,
                                               player.health, player.maxHealth, additionalData);
        }

        public static void PlayerIFF(int clientID, H3MP_Packet packet)
        {
            int IFF = packet.ReadInt();
            if (H3MP_Server.clients.ContainsKey(clientID))
            {
                H3MP_Server.clients[clientID].player.IFF = IFF;
            }
            if (H3MP_GameManager.players.ContainsKey(clientID))
            {
                H3MP_GameManager.players[clientID].SetIFF(IFF);
            }

            H3MP_ServerSend.PlayerIFF(clientID, IFF);
        }

        public static void PlayerScene(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(player.ID, scene);

            // Send to all other clients
            H3MP_ServerSend.PlayerScene(player.ID, scene);

            // Request most up to date items from relevant clients, so we can send them to this client 
            if (!H3MP_GameManager.nonSynchronizedScenes.ContainsKey(scene))
            {
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                {
                    List<int> waitingFromClients = new List<int>();

                    // There are other players in the client's scene/instance, request up to date objects before sending
                    for (int i = 0; i < otherPlayers.Count; ++i)
                    {
                        if (otherPlayers[i] != clientID)
                        {
                            if (H3MP_Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                            {
                                H3MP_Server.clientsWaitingUpDate[otherPlayers[i]].Add(clientID);
                            }
                            else
                            {
                                H3MP_Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { clientID });
                            }
                            H3MP_ServerSend.RequestUpToDateObjects(otherPlayers[i], false, clientID);
                            waitingFromClients.Add(otherPlayers[i]);
                        }
                    }

                    if (waitingFromClients.Count > 0)
                    {
                        if (H3MP_Server.loadingClientsWaitingFrom.ContainsKey(clientID))
                        {
                            H3MP_Server.loadingClientsWaitingFrom[clientID] = waitingFromClients;
                        }
                        else
                        {
                            H3MP_Server.loadingClientsWaitingFrom.Add(clientID, waitingFromClients);
                        }
                    }
                    else
                    {
                        Mod.LogInfo("Client " + clientID + " just changed scene, no other player in scene/instance, sending relevant tracked objects");
                        H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                    }
                }
                else // No other player in the client's scene/instance 
                {
                    Mod.LogInfo("Client " + clientID + " just changed scene, no other player in scene/instance, sending relevant tracked objects");
                    H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                }
            }

            Mod.LogInfo("Synced with player who just joined a scene");
        }

        public static void PlayerInstance(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            int instance = packet.ReadInt();
            bool wasLoading = packet.ReadBool();

            H3MP_GameManager.UpdatePlayerInstance(player.ID, instance);

            // Send to all other clients
            H3MP_ServerSend.PlayerInstance(player.ID, instance);

            // We don't want to request nor send up to date objects to a client that was in the process of loading into a new
            // scene when it changed instance. We will instead send up to date objects when they arrive at their new scene
            if (!wasLoading) 
            {
                // Request most up to date items from relevant clients so we can send them to the client when it is ready to receive them
                if (!H3MP_GameManager.nonSynchronizedScenes.ContainsKey(player.scene))
                {
                    if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> instances) &&
                        instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                    {
                        List<int> waitingFromClients = new List<int>();

                        // There are other players in the client's scene/instance, request up to date objects before sending
                        for (int i = 0; i < otherPlayers.Count; ++i)
                        {
                            if (otherPlayers[i] != clientID)
                            {
                                if (H3MP_Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                                {
                                    H3MP_Server.clientsWaitingUpDate[otherPlayers[i]].Add(clientID);
                                }
                                else
                                {
                                    H3MP_Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { clientID });
                                }
                                H3MP_ServerSend.RequestUpToDateObjects(otherPlayers[i], false, clientID);
                                waitingFromClients.Add(otherPlayers[i]);
                            }
                        }

                        if (waitingFromClients.Count > 0)
                        {
                            if (H3MP_Server.loadingClientsWaitingFrom.ContainsKey(clientID))
                            {
                                H3MP_Server.loadingClientsWaitingFrom[clientID] = waitingFromClients;
                            }
                            else
                            {
                                H3MP_Server.loadingClientsWaitingFrom.Add(clientID, waitingFromClients);
                            }
                        }
                        else
                        {
                            Mod.LogInfo("Client " + clientID + " just changed instance, no other player in scene/instance, sending relevant tracked objects");
                            H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                        }
                    }
                    else // No other player in the client's scene/instance 
                    {
                        Mod.LogInfo("Client " + clientID + " just changed instance, no other player in scene/instance, sending relevant tracked objects");
                        H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                    }
                }
            }
        }

        public static void AddTNHInstance(int clientID, H3MP_Packet packet)
        {
            int hostID = packet.ReadInt();
            bool letPeopleJoin = packet.ReadBool();
            int progressionTypeSetting = packet.ReadInt();
            int healthModeSetting = packet.ReadInt();
            int equipmentModeSetting = packet.ReadInt();
            int targetModeSetting = packet.ReadInt();
            int AIDifficultyModifier = packet.ReadInt();
            int radarModeModifier = packet.ReadInt();
            int itemSpawnerMode = packet.ReadInt();
            int backpackMode = packet.ReadInt();
            int healthMult = packet.ReadInt();
            int sosiggunShakeReloading = packet.ReadInt();
            int TNHSeed = packet.ReadInt();
            string levelID = packet.ReadString();

            // Send to all clients
            H3MP_ServerSend.AddTNHInstance(H3MP_GameManager.AddNewTNHInstance(hostID, letPeopleJoin,
                                                                              progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                                                              targetModeSetting, AIDifficultyModifier, radarModeModifier, itemSpawnerMode, backpackMode,
                                                                              healthMult, sosiggunShakeReloading, TNHSeed, levelID));
        }

        public static void AddInstance(int clientID, H3MP_Packet packet)
        {
            // Send to all clients
            H3MP_ServerSend.AddInstance(H3MP_GameManager.AddNewInstance());
        }

        public static void AddNonSyncScene(int clientID, H3MP_Packet packet)
        {
            string scene = packet.ReadString();

            H3MP_GameManager.nonSynchronizedScenes.Add(scene, clientID);

            // Send to all other clients
            H3MP_ServerSend.AddNonSyncScene(clientID, scene);
        }

        public static void TrackedItems(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void ItemUpdate(int clientID, H3MP_Packet packet)
        {
            H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());

            // Send to all other clients
            H3MP_ServerSend.ItemUpdate(packet, clientID);
        }

        public static void TrackedSosigs(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TrackedAutoMeaters(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedAutoMeater(packet.ReadTrackedAutoMeater());
            }
        }

        public static void TrackedEncryptions(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedEncryptions from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedEncryption(packet.ReadTrackedEncryption());
            }
        }

        public static void GiveControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            // Update locally
            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            bool destroyed = false;

            if (trackedItem == null)
            {
                Mod.LogError("Server received order to set item " + trackedID + " controller to " + newController + " but item is missing from items array!");
                H3MP_ServerSend.DestroyItem(trackedID);
            }
            else
            {
                if (trackedItem.controller != 0 && newController == 0)
                {
                    trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                    H3MP_GameManager.items.Add(trackedItem);
                    // Physical object could be null if we are given control while we are loading, the giving client will think we are in their scene/instance
                    if (trackedItem.physicalItem == null)
                    {
                        // If its is null and we receive this after having finished loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!H3MP_GameManager.sceneLoading)
                        {
                            if (trackedItem.scene.Equals(H3MP_GameManager.scene) && trackedItem.instance == H3MP_GameManager.instance)
                            {
                                if (!trackedItem.awaitingInstantiation)
                                {
                                    trackedItem.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedItem.Instantiate());
                                }
                            }
                            else
                            {
                                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> playerInstances) &&
                                playerInstances.TryGetValue(trackedItem.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    newController = Mod.GetBestPotentialObjectHost(trackedItem.controller, true, true, newPlayerList, trackedItem.scene, trackedItem.instance);
                                    if (newController == -1)
                                    {
                                        H3MP_ServerSend.DestroyItem(trackedID);
                                        trackedItem.RemoveFromLocal();
                                        H3MP_Server.items[trackedID] = null;
                                        H3MP_Server.availableItemIndices.Add(trackedID);
                                        if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                        debounce.Add(H3MP_GameManager.ID);
                                        // Don't resend give control here right away, we will send at the end
                                    }
                                }
                                else
                                {
                                    H3MP_ServerSend.DestroyItem(trackedID);
                                    trackedItem.RemoveFromLocal();
                                    H3MP_Server.items[trackedID] = null;
                                    H3MP_Server.availableItemIndices.Add(trackedID);
                                    if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                    {
                                        itemList.Remove(trackedItem.trackedID);
                                    }
                                    trackedItem.awaitingInstantiation = false;
                                    destroyed = true;
                                }
                            }
                        }
                        else // Loading or not our scene/instance
                        {
                            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> playerInstances) &&
                                playerInstances.TryGetValue(trackedItem.instance, out List<int> playerList))
                            {
                                List<int> newPlayerList = new List<int>(playerList);
                                for (int i = 0; i < debounce.Count; ++i)
                                {
                                    newPlayerList.Remove(debounce[i]);
                                }
                                newController = Mod.GetBestPotentialObjectHost(trackedItem.controller, true, true, newPlayerList, trackedItem.scene, trackedItem.instance);
                                if (newController == -1)
                                {
                                    H3MP_ServerSend.DestroyItem(trackedID);
                                    trackedItem.RemoveFromLocal();
                                    H3MP_Server.items[trackedID] = null;
                                    H3MP_Server.availableItemIndices.Add(trackedID);
                                    if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                    debounce.Add(H3MP_GameManager.ID);
                                    // Don't resend give control here right away, we will send at the end
                                }
                            }
                            else
                            {
                                H3MP_ServerSend.DestroyItem(trackedID);
                                trackedItem.RemoveFromLocal();
                                H3MP_Server.items[trackedID] = null;
                                H3MP_Server.availableItemIndices.Add(trackedID);
                                if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                    currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                {
                                    itemList.Remove(trackedItem.trackedID);
                                }
                                trackedItem.awaitingInstantiation = false;
                                destroyed = true;
                            }
                        }
                    }
                    else if(trackedItem.parent == -1)
                    {
                        Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
                    }
                }
                else if (trackedItem.controller == 0 && newController != 0)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        H3MP_GameManager.EnsureUncontrolled(trackedItem.physicalItem.physicalObject);

                        Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, true);
                    }
                    trackedItem.RemoveFromLocal();
                }

                if (!destroyed)
                {
                    trackedItem.SetController(newController);

                    // Send to all other clients
                    H3MP_ServerSend.GiveControl(trackedID, newController, debounce);
                }
            }
        }

        public static void GiveSosigControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for(int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

            if (trackedSosig == null)
            {
                Mod.LogError("Server received order to set sosig " + trackedID + " controller to " + newController + " but sosig is missing from sosigs array!");
                H3MP_ServerSend.DestroySosig(trackedID);
            }
            else
            {
                bool destroyed = false;
                if (trackedSosig.controller != 0 && newController == 0)
                {
                    trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                    H3MP_GameManager.sosigs.Add(trackedSosig);
                    if (trackedSosig.physicalObject == null)
                    {
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!H3MP_GameManager.sceneLoading)
                        {
                            if (trackedSosig.scene.Equals(H3MP_GameManager.scene) && trackedSosig.instance == H3MP_GameManager.instance)
                            {
                                if (!trackedSosig.awaitingInstantiation)
                                {
                                    trackedSosig.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedSosig.Instantiate());
                                }
                            }
                            else
                            {
                                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> sosigInstances) &&
                                    sosigInstances.TryGetValue(trackedSosig.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    newController = Mod.GetBestPotentialObjectHost(trackedSosig.controller, true, true, newPlayerList, trackedSosig.scene, trackedSosig.instance);
                                    if (newController == -1)
                                    {
                                        H3MP_ServerSend.DestroySosig(trackedID);
                                        trackedSosig.RemoveFromLocal();
                                        H3MP_Server.sosigs[trackedID] = null;
                                        H3MP_Server.availableSosigIndices.Add(trackedID);
                                        if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                        debounce.Add(H3MP_GameManager.ID);
                                        // Don't resend give control here right away, we will send at the end
                                    }
                                }
                                else
                                {
                                    H3MP_ServerSend.DestroySosig(trackedID);
                                    trackedSosig.RemoveFromLocal();
                                    H3MP_Server.sosigs[trackedID] = null;
                                    H3MP_Server.availableSosigIndices.Add(trackedID);
                                    if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> sosigInstances) &&
                                    sosigInstances.TryGetValue(trackedSosig.instance, out List<int> playerList))
                            {
                                List<int> newPlayerList = new List<int>(playerList);
                                for (int i = 0; i < debounce.Count; ++i)
                                {
                                    newPlayerList.Remove(debounce[i]);
                                }
                                newController = Mod.GetBestPotentialObjectHost(trackedSosig.controller, true, true, newPlayerList, trackedSosig.scene, trackedSosig.instance);
                                if (newController == -1)
                                {
                                    H3MP_ServerSend.DestroySosig(trackedID);
                                    trackedSosig.RemoveFromLocal();
                                    H3MP_Server.sosigs[trackedID] = null;
                                    H3MP_Server.availableSosigIndices.Add(trackedID);
                                    if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                    debounce.Add(H3MP_GameManager.ID);
                                    // Don't resend give control here right away, we will send at the end
                                }
                            }
                            else
                            {
                                H3MP_ServerSend.DestroySosig(trackedID);
                                trackedSosig.RemoveFromLocal();
                                H3MP_Server.sosigs[trackedID] = null;
                                H3MP_Server.availableSosigIndices.Add(trackedID);
                                if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                        }
                        trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                    }
                }
                else if (trackedSosig.controller == 0 && newController != 0)
                {
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

                if (!destroyed)
                {
                    trackedSosig.controller = newController;

                    // Send to all other clients
                    H3MP_ServerSend.GiveSosigControl(trackedID, newController, debounce);

                    if (newController == H3MP_GameManager.ID)
                    {
                        trackedSosig.TakeInventoryControl();
                    }
                }
            }
        }

        public static void GiveAutoMeaterControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];

            if (trackedAutoMeater == null)
            {
                Mod.LogError("Server received order to set automeater " + trackedID + " controller to " + newController + " but automeater is missing from automeaters array!");
                H3MP_ServerSend.DestroyAutoMeater(trackedID);
            }
            else
            {
                bool destroyed = false;
                if (trackedAutoMeater.controller != 0 && newController == 0)
                {
                    trackedAutoMeater.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                    H3MP_GameManager.autoMeaters.Add(trackedAutoMeater);
                    if (trackedAutoMeater.physicalObject == null)
                    {
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!H3MP_GameManager.sceneLoading)
                        {
                            if (trackedAutoMeater.scene.Equals(H3MP_GameManager.scene) && trackedAutoMeater.instance == H3MP_GameManager.instance)
                            {
                                if (!trackedAutoMeater.awaitingInstantiation)
                                {
                                    trackedAutoMeater.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                                }
                            }
                            else
                            {
                                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> playerInstances) &&
                                playerInstances.TryGetValue(trackedAutoMeater.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    newController = Mod.GetBestPotentialObjectHost(trackedAutoMeater.controller, true, true, newPlayerList, trackedAutoMeater.scene, trackedAutoMeater.instance);
                                    if (newController == -1)
                                    {
                                        H3MP_ServerSend.DestroyAutoMeater(trackedID);
                                        trackedAutoMeater.RemoveFromLocal();
                                        H3MP_Server.autoMeaters[trackedID] = null;
                                        H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
                                        if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                        debounce.Add(H3MP_GameManager.ID);
                                        // Don't resend give control here right away, we will send at the end
                                    }
                                }
                                else
                                {
                                    H3MP_ServerSend.DestroyAutoMeater(trackedID);
                                    trackedAutoMeater.RemoveFromLocal();
                                    H3MP_Server.autoMeaters[trackedID] = null;
                                    H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
                                    if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> playerInstances) &&
                                playerInstances.TryGetValue(trackedAutoMeater.instance, out List<int> playerList))
                            {
                                List<int> newPlayerList = new List<int>(playerList);
                                for (int i = 0; i < debounce.Count; ++i)
                                {
                                    newPlayerList.Remove(debounce[i]);
                                }
                                newController = Mod.GetBestPotentialObjectHost(trackedAutoMeater.controller, true, true, newPlayerList, trackedAutoMeater.scene, trackedAutoMeater.instance);
                                if (newController == -1)
                                {
                                    H3MP_ServerSend.DestroyAutoMeater(trackedID);
                                    trackedAutoMeater.RemoveFromLocal();
                                    H3MP_Server.autoMeaters[trackedID] = null;
                                    H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
                                    if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                    debounce.Add(H3MP_GameManager.ID);
                                    // Don't resend give control here right away, we will send at the end
                                }
                            }
                            else
                            {
                                H3MP_ServerSend.DestroyAutoMeater(trackedID);
                                trackedAutoMeater.RemoveFromLocal();
                                H3MP_Server.autoMeaters[trackedID] = null;
                                H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
                                if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                        if (GM.CurrentAIManager != null)
                        {
                            GM.CurrentAIManager.RegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                        }
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                    }
                }
                else if (trackedAutoMeater.controller == 0 && newController != 0)
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
                if (!destroyed)
                {
                    trackedAutoMeater.controller = newController;

                    // Send to all other clients
                    H3MP_ServerSend.GiveAutoMeaterControl(trackedID, newController, debounce);
                }
            }
        }

        public static void GiveEncryptionControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];

            if (trackedEncryption == null)
            {
                Mod.LogError("Server received order to set encryption " + trackedID + " controller to " + newController + " but encryption is missing from encryptions array!");
                H3MP_ServerSend.DestroyEncryption(trackedID);
            }
            else
            {

                bool destroyed = false;
                if (trackedEncryption.controller != 0 && newController == 0)
                {
                    trackedEncryption.localTrackedID = H3MP_GameManager.encryptions.Count;
                    H3MP_GameManager.encryptions.Add(trackedEncryption);
                    if (trackedEncryption.physicalObject == null)
                    {
                        // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!H3MP_GameManager.sceneLoading)
                        {
                            if (trackedEncryption.scene.Equals(H3MP_GameManager.scene) && trackedEncryption.instance == H3MP_GameManager.instance)
                            {
                                if (!trackedEncryption.awaitingInstantiation)
                                {
                                    trackedEncryption.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedEncryption.Instantiate());
                                }
                            }
                            else
                            {
                                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedEncryption.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    newController = Mod.GetBestPotentialObjectHost(trackedEncryption.controller, true, true, newPlayerList, trackedEncryption.scene, trackedEncryption.instance);
                                    if (newController == -1)
                                    {
                                        H3MP_ServerSend.DestroyEncryption(trackedID);
                                        trackedEncryption.RemoveFromLocal();
                                        H3MP_Server.encryptions[trackedID] = null;
                                        H3MP_Server.availableEncryptionIndices.Add(trackedID);
                                        if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                        debounce.Add(H3MP_GameManager.ID);
                                        // Don't resend give control here right away, we will send at the end
                                    }
                                }
                                else
                                {
                                    H3MP_ServerSend.DestroyEncryption(trackedID);
                                    trackedEncryption.RemoveFromLocal();
                                    H3MP_Server.encryptions[trackedID] = null;
                                    H3MP_Server.availableEncryptionIndices.Add(trackedID);
                                    if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedEncryption.instance, out List<int> playerList))
                            {
                                List<int> newPlayerList = new List<int>(playerList);
                                for (int i = 0; i < debounce.Count; ++i)
                                {
                                    newPlayerList.Remove(debounce[i]);
                                }
                                newController = Mod.GetBestPotentialObjectHost(trackedEncryption.controller, true, true, newPlayerList, trackedEncryption.scene, trackedEncryption.instance);
                                if (newController == -1)
                                {
                                    H3MP_ServerSend.DestroyEncryption(trackedID);
                                    trackedEncryption.RemoveFromLocal();
                                    H3MP_Server.encryptions[trackedID] = null;
                                    H3MP_Server.availableEncryptionIndices.Add(trackedID);
                                    if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
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
                                    debounce.Add(H3MP_GameManager.ID);
                                    // Don't resend give control here right away, we will send at the end
                                }
                            }
                            else
                            {
                                H3MP_ServerSend.DestroyEncryption(trackedID);
                                trackedEncryption.RemoveFromLocal();
                                H3MP_Server.encryptions[trackedID] = null;
                                H3MP_Server.availableEncryptionIndices.Add(trackedID);
                                if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                    currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                {
                                    encryptionList.Remove(trackedEncryption.trackedID);
                                }
                                trackedEncryption.awaitingInstantiation = false;
                                destroyed = true;
                            }
                        }
                    }
                }
                else if (trackedEncryption.controller == 0 && newController != 0)
                {
                    trackedEncryption.RemoveFromLocal();
                }

                if (!destroyed)
                {
                    trackedEncryption.controller = newController;

                    // Send to all other clients
                    H3MP_ServerSend.GiveEncryptionControl(trackedID, newController, debounce);
                }
            }
        }

        public static void DestroySosig(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

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

                if (!destroyed && trackedSosig.localTrackedID != -1)
                {
                    trackedSosig.RemoveFromLocal();
                }

                // Check if want to ensure this was removed from list, if it wasn't by the destruction, do it here
                if (removeFromList && H3MP_Server.sosigs[trackedID] != null && !destroyed)
                {
                    H3MP_Server.sosigs[trackedID] = null;
                    H3MP_Server.availableSosigIndices.Add(trackedID);
                    H3MP_GameManager.sosigsByInstanceByScene[trackedSosig.scene][trackedSosig.instance].Remove(trackedID);
                }
            }

            H3MP_ServerSend.DestroySosig(trackedID, removeFromList, clientID);
        }

        public static void DestroyAutoMeater(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];

            bool destroyed = false;
            if (trackedAutoMeater != null)
            {
                trackedAutoMeater.awaitingInstantiation = false;

                if (trackedAutoMeater.physicalObject != null)
                {
                    trackedAutoMeater.removeFromListOnDestroy = removeFromList;
                    trackedAutoMeater.physicalObject.sendDestroy = false;
                    trackedAutoMeater.physicalObject.dontGiveControl = true;
                    GameObject.Destroy(trackedAutoMeater.physicalObject.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedAutoMeater.localTrackedID != -1)
                {
                    trackedAutoMeater.RemoveFromLocal();
                }

                // Check if want to ensure this was removed from list, if it wasn't by the destruction, do it here
                if (removeFromList && H3MP_Server.autoMeaters[trackedID] != null && !destroyed)
                {
                    H3MP_Server.autoMeaters[trackedID] = null;
                    H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
                    H3MP_GameManager.autoMeatersByInstanceByScene[trackedAutoMeater.scene][trackedAutoMeater.instance].Remove(trackedID);
                }
            }

            H3MP_ServerSend.DestroyAutoMeater(trackedID, removeFromList, clientID);
        }

        public static void DestroyEncryption(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];

            bool destroyed = false;
            if (trackedEncryption != null)
            {
                trackedEncryption.awaitingInstantiation = false;

                if (trackedEncryption.physicalObject != null)
                {
                    trackedEncryption.removeFromListOnDestroy = removeFromList;
                    trackedEncryption.physicalObject.sendDestroy = false;
                    trackedEncryption.physicalObject.dontGiveControl = true;
                    GameObject.Destroy(trackedEncryption.physicalObject.gameObject);
                    destroyed = true;
                }

                if (!destroyed && trackedEncryption.localTrackedID != -1)
                {
                    trackedEncryption.RemoveFromLocal();
                }

                // Check if want to ensure this was removed from list, if it wasn't by the destruction, do it here
                if (removeFromList && H3MP_Server.encryptions[trackedID] != null && !destroyed)
                {
                    H3MP_Server.encryptions[trackedID] = null;
                    H3MP_Server.availableEncryptionIndices.Add(trackedID);
                    H3MP_GameManager.encryptionsByInstanceByScene[trackedEncryption.scene][trackedEncryption.instance].Remove(trackedID);
                }
            }

            H3MP_ServerSend.DestroyEncryption(trackedID, removeFromList, clientID);
        }

        public static void DestroyItem(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            if (H3MP_Server.items[trackedID] != null)
            {
                H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

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

                if (!destroyed && trackedItem.localTrackedID != -1)
                {
                    trackedItem.RemoveFromLocal();
                }

                // Check if want to ensure this was removed from list, if it wasn't by the destruction, do it here
                if (removeFromList && H3MP_Server.items[trackedID] != null && !destroyed)
                {
                    H3MP_Server.items[trackedID] = null;
                    H3MP_Server.availableItemIndices.Add(trackedID);
                    H3MP_GameManager.itemsByInstanceByScene[trackedItem.scene][trackedItem.instance].Remove(trackedID);
                }
            }

            H3MP_ServerSend.DestroyItem(trackedID, removeFromList, clientID);
        }

        public static void TrackedItem(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedItem(packet.ReadTrackedItem(true), clientID);
        }

        public static void TrackedSosig(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedSosig(packet.ReadTrackedSosig(true), clientID);
        }

        public static void TrackedAutoMeater(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedAutoMeater(packet.ReadTrackedAutoMeater(true), clientID);
        }

        public static void TrackedEncryption(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedEncryption(packet.ReadTrackedEncryption(true), clientID);
        }

        public static void ItemParent(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            Mod.LogInfo("Received order to set item " + H3MP_Server.items[trackedID].itemID + " at " + trackedID+" with waiting index: "+ H3MP_Server.items[trackedID].localWaitingIndex);
            H3MP_Server.items[trackedID].SetParent(newParentID);

            // Send to all other clients
            H3MP_ServerSend.ItemParent(trackedID, newParentID, clientID);
        }

        public static void WeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] == null)
            {
                Mod.LogError("Server received order to fire weapon " + trackedID + " but item is missing from items array!");
            }
            else
            {
                // Update locally
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    int roundType = packet.ReadShort();
                    int roundClass = packet.ReadShort();
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
                    H3MP_Server.items[trackedID].physicalItem.setFirearmUpdateOverride((FireArmRoundType)roundType, (FireArmRoundClass)roundClass, chamberIndex);
                    H3MP_Server.items[trackedID].physicalItem.fireFunc(chamberIndex);
                }
            }

            // Send to other clients
            H3MP_ServerSend.WeaponFire(clientID, packet);
        }

        public static void FlintlockWeaponBurnOffOuter(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = H3MP_Server.items[trackedID].physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = H3MP_Server.items[trackedID].physicalItem.physicalObject as FlintlockWeapon;
                int loadedElementCount = packet.ReadByte();
                asBarrel.LoadedElements = new List<FlintlockBarrel.LoadedElement>();
                for (int i=0; i < loadedElementCount; ++i)
                {
                    FlintlockBarrel.LoadedElement newElement = new FlintlockBarrel.LoadedElement();
                    newElement.Type = (FlintlockBarrel.LoadedElementType)packet.ReadByte();
                    newElement.Position = packet.ReadFloat();
                }
                asBarrel.LoadedElements[asBarrel.LoadedElements.Count - 1].PowderAmount = packet.ReadInt();
                if(packet.ReadBool() && asFlintlockWeapon.RamRod.GetCurBarrel() != asBarrel)
                {
                    Mod.FlintlockPseudoRamRod_m_curBarrel.SetValue(asFlintlockWeapon.RamRod, asBarrel);
                }
                FireFlintlockWeaponPatch.num2 = packet.ReadFloat();
                FireFlintlockWeaponPatch.positions = new List<Vector3>();
                FireFlintlockWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
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

            // Send to other clients
            H3MP_ServerSend.FlintlockWeaponBurnOffOuter(clientID, packet);
        }

        public static void FlintlockWeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                // Override
                FlintlockBarrel asBarrel = H3MP_Server.items[trackedID].physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = H3MP_Server.items[trackedID].physicalItem.physicalObject as FlintlockWeapon;
                int loadedElementCount = packet.ReadByte();
                asBarrel.LoadedElements = new List<FlintlockBarrel.LoadedElement>();
                for (int i=0; i < loadedElementCount; ++i)
                {
                    FlintlockBarrel.LoadedElement newElement = new FlintlockBarrel.LoadedElement();
                    newElement.Type = (FlintlockBarrel.LoadedElementType)packet.ReadByte();
                    newElement.Position = packet.ReadFloat();
                    newElement.PowderAmount = packet.ReadInt();
                }
                if(packet.ReadBool() && asFlintlockWeapon.RamRod.GetCurBarrel() != asBarrel)
                {
                    Mod.FlintlockPseudoRamRod_m_curBarrel.SetValue(asFlintlockWeapon.RamRod, asBarrel);
                }
                FireFlintlockWeaponPatch.num5 = packet.ReadFloat();
                FireFlintlockWeaponPatch.positions = new List<Vector3>();
                FireFlintlockWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FireFlintlockWeaponPatch.positions.Add(packet.ReadVector3());
                    FireFlintlockWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireFlintlockWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireFlintlockWeaponPatch.fireSkip;
                Mod.FlintlockBarrel_Fire.Invoke(asBarrel, null);
                --FireFlintlockWeaponPatch.fireSkip;
            }

            // Send to other clients
            H3MP_ServerSend.FlintlockWeaponFire(clientID, packet);
        }

        public static void BreakActionWeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int barrelIndex = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                BreakActionWeapon asBAW = H3MP_Server.items[trackedID].physicalItem.physicalObject as BreakActionWeapon;
                FireArmRoundType prevRoundType = asBAW.Barrels[barrelIndex].Chamber.RoundType;
                asBAW.Barrels[barrelIndex].Chamber.RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asBAW.Barrels[barrelIndex].Chamber.SetRound(roundClass, asBAW.Barrels[barrelIndex].Chamber.transform.position, asBAW.Barrels[barrelIndex].Chamber.transform.rotation);
                --ChamberPatch.chamberSkip;
                asBAW.Barrels[barrelIndex].Chamber.RoundType = prevRoundType;
                // NOTE: Only barrel index is used in the Fire method, other arguments are presumably reserved for later,
                // TODO: Future: will need to add support later if implemented
                asBAW.Fire(barrelIndex, false, 0);
            }

            // Send to other clients
            H3MP_ServerSend.BreakActionWeaponFire(clientID, packet);
        }

        public static void DerringerFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int barrelIndex = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Derringer asDerringer = H3MP_Server.items[trackedID].physicalItem.physicalObject as Derringer;
                FireArmRoundType prevRoundType = asDerringer.Barrels[barrelIndex].Chamber.RoundType;
                asDerringer.Barrels[barrelIndex].Chamber.RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asDerringer.Barrels[barrelIndex].Chamber.SetRound(roundClass, asDerringer.Barrels[barrelIndex].Chamber.transform.position, asDerringer.Barrels[barrelIndex].Chamber.transform.rotation);
                --ChamberPatch.chamberSkip;
                asDerringer.Barrels[barrelIndex].Chamber.RoundType = prevRoundType;
                Mod.Derringer_m_curBarrel.SetValue(asDerringer, barrelIndex);
                Mod.Derringer_FireBarrel.Invoke(asDerringer, new object[] { barrelIndex });
            }

            // Send to other clients
            H3MP_ServerSend.DerringerFire(clientID, packet);
        }

        public static void RevolvingShotgunFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                RevolvingShotgun asRS = H3MP_Server.items[trackedID].physicalItem.physicalObject as RevolvingShotgun;
                asRS.CurChamber = curChamber;
                FireArmRoundType prevRoundType = asRS.Chambers[curChamber].RoundType;
                asRS.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asRS.Chambers[curChamber].SetRound(roundClass, asRS.Chambers[curChamber].transform.position, asRS.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asRS.Chambers[curChamber].RoundType = prevRoundType;
                Mod.RevolvingShotgun_Fire.Invoke(asRS, null);
            }

            // Send to other clients
            H3MP_ServerSend.RevolvingShotgunFire(clientID, packet);
        }

        public static void RevolverFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Revolver asRevolver = H3MP_Server.items[trackedID].physicalItem.physicalObject as Revolver;
                bool changedOffset = false;
                int oldOffset = 0;
                if(asRevolver.ChamberOffset != 0)
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
                Mod.Revolver_Fire.Invoke(asRevolver, null);
            }

            // Send to other clients
            H3MP_ServerSend.RevolverFire(clientID, packet);
        }

        public static void SingleActionRevolverFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                SingleActionRevolver asRevolver = H3MP_Server.items[trackedID].physicalItem.physicalObject as SingleActionRevolver;
                asRevolver.CurChamber = curChamber;
                FireArmRoundType prevRoundType = asRevolver.Cylinder.Chambers[curChamber].RoundType;
                asRevolver.Cylinder.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asRevolver.Cylinder.Chambers[curChamber].SetRound(roundClass, asRevolver.Cylinder.Chambers[curChamber].transform.position, asRevolver.Cylinder.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asRevolver.Cylinder.Chambers[curChamber].RoundType = prevRoundType;
                Mod.SingleActionRevolver_Fire.Invoke(asRevolver, null);
            }

            // Send to other clients
            H3MP_ServerSend.SingleActionRevolverFire(clientID, packet);
        }

        public static void GrappleGunFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                int curChamber = packet.ReadByte();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                GrappleGun asGG = H3MP_Server.items[trackedID].physicalItem.physicalObject as GrappleGun;
                Mod.GrappleGun_m_curChamber.SetValue(asGG, curChamber);
                FireArmRoundType prevRoundType = asGG.Chambers[curChamber].RoundType;
                asGG.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].SetRound(roundClass, asGG.Chambers[curChamber].transform.position, asGG.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].RoundType = prevRoundType;
                asGG.Fire();
            }

            // Send to other clients
            H3MP_ServerSend.GrappleGunFire(clientID, packet);
        }

        public static void HCBReleaseSled(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                float cookedAmount = packet.ReadFloat();
                FireHCBPatch.position = packet.ReadVector3();
                FireHCBPatch.direction = packet.ReadVector3();
                FireHCBPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireHCBPatch.releaseSledSkip;
                HCB asHCB = H3MP_Server.items[trackedID].physicalItem.physicalObject as HCB;
                Mod.HCB_m_cookedAmount.SetValue(asHCB, cookedAmount);
                if (!asHCB.Chamber.IsFull)
                {
                    ++ChamberPatch.chamberSkip;
                    asHCB.Chamber.SetRound(FireArmRoundClass.FMJ, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                }
                Mod.HCB_ReleaseSled.Invoke(asHCB, null);
                --FireHCBPatch.releaseSledSkip;
            }

            // Send to other clients
            H3MP_ServerSend.HCBReleaseSled(clientID, packet);
        }

        public static void StingerLauncherFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireStingerLauncherPatch.targetPos = packet.ReadVector3();
                FireStingerLauncherPatch.position = packet.ReadVector3();
                FireStingerLauncherPatch.direction = packet.ReadVector3();
                FireStingerLauncherPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireStingerLauncherPatch.skip;
                StingerLauncher asStingerLauncher = H3MP_Server.items[trackedID].physicalItem.physicalObject as StingerLauncher;
                Mod.StingerLauncher_m_hasMissile.SetValue(asStingerLauncher, true);
                asStingerLauncher.Fire();
                --FireStingerLauncherPatch.skip;
            }

            // Send to other clients
            H3MP_ServerSend.StingerLauncherFire(clientID, packet);
        }

        public static void LeverActionFirearmFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();
                FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
                bool hammer1 = packet.ReadBool();
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                LeverActionFirearm asLAF = H3MP_Server.items[trackedID].physicalItem.dataObject as LeverActionFirearm;
                if (hammer1)
                {
                    FireArmRoundType prevRoundType = asLAF.Chamber.RoundType;
                    asLAF.Chamber.RoundType = roundType;
                    ++ChamberPatch.chamberSkip;
                    asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    asLAF.Chamber.RoundType = prevRoundType;
                    Mod.LeverActionFirearm_m_isHammerCocked.SetValue(asLAF, true);
                    Mod.LeverActionFirearm_Fire.Invoke(asLAF, null);
                }
                else
                {
                    bool reCock = false;
                    if (asLAF.IsHammerCocked)
                    {
                        // Temporarily uncock hammer1
                        reCock = true;
                        Mod.LeverActionFirearm_m_isHammerCocked.SetValue(asLAF, false);
                    }
                    bool reChamber = false;
                    FireArmRoundClass reChamberClass = FireArmRoundClass.a20AP;
                    if(asLAF.Chamber.GetRound() != null)
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
                    Mod.LeverActionFirearm_m_isHammerCocked2.SetValue(asLAF, true);
                    Mod.LeverActionFirearm_Fire.Invoke(asLAF, null);
                    if (reCock)
                    {
                        Mod.LeverActionFirearm_m_isHammerCocked.SetValue(asLAF, true);
                    }
                    if (reChamber)
                    {
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber.SetRound(reChamberClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                }
            }

            // Send to other clients
            H3MP_ServerSend.LeverActionFirearmFire(clientID, packet);
        }

        public static void SosigWeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            float recoilMult = packet.ReadFloat();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
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
                SosigWeaponPlayerInterface asInterface = H3MP_Server.items[trackedID].physicalItem.dataObject as SosigWeaponPlayerInterface;
                int shotsLeft = (int)Mod.SosigWeapon_m_shotsLeft.GetValue(asInterface.W);
                if(shotsLeft <= 0)
                {
                    Mod.SosigWeapon_m_shotsLeft.SetValue(asInterface.W, 1);
                }
                asInterface.W.MechaState = SosigWeapon.SosigWeaponMechaState.ReadyToFire;
                H3MP_Server.items[trackedID].physicalItem.sosigWeaponfireFunc(recoilMult);
            }

            // Send to other clients
            H3MP_ServerSend.SosigWeaponFire(clientID, packet);
        }

        public static void MinigunFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            
            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireMinigunPatch.positions = new List<Vector3>();
                FireMinigunPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireMinigunPatch.positions.Add(packet.ReadVector3());
                    FireMinigunPatch.directions.Add(packet.ReadVector3());
                }
                FireSosigWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Minigun asMinigun = (Minigun)H3MP_Server.items[trackedID].physicalItem.physicalObject;
                Mod.Minigun_Fire.Invoke(asMinigun, null);
            }

            // Send to other clients
            H3MP_ServerSend.MinigunFire(clientID, packet);
        }

        public static void AttachableFirearmFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
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
                H3MP_Server.items[trackedID].physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                H3MP_Server.items[trackedID].physicalItem.attachableFirearmFireFunc(firedFromInterface);
            }

            // Send to other clients
            H3MP_ServerSend.AttachableFirearmFire(clientID, packet);
        }

        public static void IntegratedFirearmFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
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
                H3MP_Server.items[trackedID].physicalItem.attachableFirearmChamberRoundFunc(roundType, roundClass);
                H3MP_Server.items[trackedID].physicalItem.attachableFirearmFireFunc(false);
            }

            // Send to other clients
            H3MP_ServerSend.IntegratedFirearmFire(clientID, packet);
        }

        public static void LAPD2019Fire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
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
                LAPD2019 asLAPD2019 = H3MP_Server.items[trackedID].physicalItem.physicalObject as LAPD2019;
                asLAPD2019.CurChamber = chamberIndex;
                FireArmRoundType prevRoundType = asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].SetRound(roundClass, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.position, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = prevRoundType;
                Mod.LAPD2019_Fire.Invoke(asLAPD2019, null);
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019Fire(clientID, packet);
        }

        public static void LAPD2019LoadBattery(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null && H3MP_Server.items[batteryTrackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)H3MP_Server.items[trackedID].physicalItem.physicalObject).LoadBattery((LAPD2019Battery)H3MP_Server.items[batteryTrackedID].physicalItem.physicalObject);
                --LAPD2019ActionPatch.loadBatterySkip;
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019LoadBattery(clientID, trackedID, batteryTrackedID);
        }

        public static void LAPD2019ExtractBattery(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)H3MP_Server.items[trackedID].physicalItem.physicalObject).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019ExtractBattery(clientID, trackedID);
        }

        public static void SosigWeaponShatter(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++SosigWeaponShatterPatch.skip;
                typeof(SosigWeaponPlayerInterface).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance).Invoke((H3MP_Server.items[trackedID].physicalItem.physicalObject as SosigWeaponPlayerInterface).W, null);
                --SosigWeaponShatterPatch.skip;
            }

            // Send to other clients
            H3MP_ServerSend.SosigWeaponShatter(clientID, trackedID);
        }

        public static void AutoMeaterFirearmFireShot(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            if (H3MP_Server.autoMeaters[trackedID].physicalObject != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                Mod.AutoMeaterFirearm_FireShot.Invoke(H3MP_Server.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0], null);
                --AutoMeaterFirearmFireShotPatch.skip;
            }

            // Send to other clients
            H3MP_ServerSend.AutoMeaterFirearmFireShot(clientID, trackedID, angles);
        }

        public static void PlayerDamage(int clientID, H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            H3MP_PlayerHitbox.Part part = (H3MP_PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            if (ID == 0)
            {
                H3MP_GameManager.ProcessPlayerDamage(part, damage);
            }
            else
            {
                H3MP_ServerSend.PlayerDamage(ID, (byte)part, damage);
            }
        }

        public static void UberShatterableShatter(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null && H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++UberShatterableShatterPatch.skip;
                H3MP_Server.items[trackedID].physicalItem.GetComponent<UberShatterable>().Shatter(packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat());
                --UberShatterableShatterPatch.skip;
            }

            H3MP_ServerSend.UberShatterableShatter(clientID, packet);
        }

        public static void SosigPickUpItem(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = itemTrackedID;

                if (trackedSosig.physicalObject != null)
                {
                    if (H3MP_Server.items[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (H3MP_Server.items[itemTrackedID].physicalItem == null)
                    {
                        H3MP_Server.items[itemTrackedID].toPutInSosigInventory = new int[] { sosigTrackedID, primaryHand ? 0 : 1 };
                    }
                    else
                    {
                        ++SosigPickUpPatch.skip;
                        if (primaryHand)
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                        }
                        else
                        {
                            trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                        }
                        --SosigPickUpPatch.skip;
                    }
                }
            }

            H3MP_ServerSend.SosigPickUpItem(sosigTrackedID, itemTrackedID, primaryHand, clientID);
        }

        public static void SosigPlaceItemIn(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = itemTrackedID;

                if (trackedSosig.physicalObject != null)
                {
                    if (H3MP_Server.items[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (H3MP_Server.items[itemTrackedID].physicalItem == null)
                    {
                        H3MP_Server.items[itemTrackedID].toPutInSosigInventory = new int[] { sosigTrackedID, slotIndex + 2 };
                    }
                    else
                    {
                        ++SosigPlaceObjectInPatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].PlaceObjectIn(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                        --SosigPlaceObjectInPatch.skip;
                    }
                }
            }

            H3MP_ServerSend.SosigPlaceItemIn(sosigTrackedID, slotIndex, itemTrackedID, clientID);
        }

        public static void SosigDropSlot(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = -1;

                if (trackedSosig.physicalObject != null)
                {
                    ++SosigSlotDetachPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].DetachHeldObject();
                    --SosigSlotDetachPatch.skip;
                }
            }

            H3MP_ServerSend.SosigDropSlot(sosigTrackedID, slotIndex, clientID);
        }

        public static void SosigHandDrop(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
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

            H3MP_ServerSend.SosigHandDrop(sosigTrackedID, primaryHand, clientID);
        }

        public static void SosigConfigure(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;

                if (trackedSosig.physicalObject != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalObject.physicalSosigScript.Configure(config);
                }
            }

            H3MP_ServerSend.SosigConfigure(sosigTrackedID, config, clientID);
        }

        public static void SosigLinkRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if (trackedSosig.physicalObject != null)
                    {
                        foreach (SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while (trackedSosig.wearables.Count <= linkIndex)
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

            H3MP_ServerSend.SosigLinkRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigLinkDeRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            if (sosigTrackedID != -1)
            {
                H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
                if (trackedSosig != null)
                {
                    if (trackedSosig.wearables != null)
                    {
                        if (trackedSosig.physicalObject != null)
                        {
                            FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
                            for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                            {
                                if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                                {
                                    trackedSosig.wearables[linkIndex].RemoveAt(i);
                                    if (trackedSosig.physicalObject != null)
                                    {
                                        ++SosigLinkActionPatch.skipDeRegisterWearable;
                                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].DeRegisterWearable((wearablesField.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[i]);
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

            H3MP_ServerSend.SosigLinkDeRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigSetIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigSetIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigSetOriginalIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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

            H3MP_ServerSend.SosigSetOriginalIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigLinkDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
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
                else
                {
                    H3MP_ServerSend.SosigLinkDamage(trackedSosig, linkIndex, damage);
                }
            }
        }

        public static void AutoMeaterDamage(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
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
                    H3MP_ServerSend.AutoMeaterDamage(trackedAutoMeater, damage);
                }
            }
        }

        public static void AutoMeaterHitZoneDamage(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            byte type = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
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
                    H3MP_ServerSend.AutoMeaterHitZoneDamage(trackedAutoMeater, type, damage);
                }
            }
        }

        public static void EncryptionDamage(int clientID, H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller == 0)
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
                    H3MP_ServerSend.EncryptionDamage(trackedEncryption, damage);
                }
            }
        }

        public static void AutoMeaterDamageData(int clientID, H3MP_Packet packet)
        {
            // TODO: Future: if ever there is data we need to pass back from a auto meater damage call
        }

        public static void SosigWearableDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
                {
                    if (trackedSosig.physicalObject != null &&
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex] != null &&
                        !trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].IsExploded)
                    {
                        ++SosigWearableDamagePatch.skip;
                        (Mod.SosigLink_m_wearables.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigWearableDamage(trackedSosig, linkIndex, wearableIndex, damage);
                }
            }
        }

        public static void SosigDamageData(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller != 0 && trackedSosig.physicalObject != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalObject.physicalSosigScript;
                    Mod.Sosig_m_isStunned.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_stunTimeLeft = packet.ReadFloat();
                    physicalSosig.BodyState = (Sosig.SosigBodyState)packet.ReadByte();
                    Mod.Sosig_m_isOnOffMeshLinkField.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.Agent.autoTraverseOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.enabled = packet.ReadBool();
                    List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(physicalSosig);
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
                    Mod.Sosig_m_isCountingDownToStagger.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_staggerAmountToApply.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveringFromBallisticState.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_recoveryFromBallisticLerp.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_tickDownToWrithe.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveryFromBallisticTick.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_lastIFFDamageSource.SetValue(physicalSosig, packet.ReadByte());
                    Mod.Sosig_m_diedFromClass.SetValue(physicalSosig, (Damage.DamageClass)packet.ReadByte());
                    Mod.Sosig_m_isBlinded.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_blindTime.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isFrozen.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_debuffTime_Freeze.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_receivedHeadShot.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_timeSinceLastDamage.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isConfused.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_confusedTime = packet.ReadFloat();
                    Mod.Sosig_m_storedShudder.SetValue(physicalSosig, packet.ReadFloat());
                }
            }

            packet.readPos = 0;
            H3MP_ServerSend.SosigLinkDamageData(packet);
        }

        public static void EncryptionDamageData(int clientID, H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller != 0 && trackedEncryption.physicalObject != null)
                {
                    Mod.TNH_EncryptionTarget_m_numHitsLeft.SetValue(trackedEncryption.physicalObject.physicalEncryptionScript, packet.ReadInt());
                }
            }

            packet.readPos = 0;
            H3MP_ServerSend.EncryptionDamageData(packet);
        }

        public static void AutoMeaterHitZoneDamageData(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller != 0 && trackedAutoMeater.physicalObject != null)
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

            packet.readPos = 0;
            H3MP_ServerSend.AutoMeaterHitZoneDamageData(packet);
        }

        public static void SosigLinkExplodes(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }

            H3MP_ServerSend.SosigLinkExplodes(packet, clientID);
        }

        public static void SosigDies(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }

            H3MP_ServerSend.SosigDies(packet, clientID);
        }

        public static void SosigClear(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosigScript.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
                }
            }

            H3MP_ServerSend.SosigClear(sosigTrackedID, clientID);
        }

        public static void SosigSetBodyState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigSetBodyStateSkip;
                    Mod.Sosig_SetBodyState.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { bodyState });
                    --SosigActionPatch.sosigSetBodyStateSkip;
                }
            }

            H3MP_ServerSend.SosigSetBodyState(sosigTrackedID, bodyState, clientID);
        }

        public static void PlaySosigFootStepSound(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (H3MP_Server.sosigs[sosigTrackedID] != null && H3MP_Server.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = H3MP_Server.sosigs[sosigTrackedID].physicalObject.physicalSosigScript.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }

            H3MP_ServerSend.PlaySosigFootStepSound(packet, clientID);
        }

        public static void SosigSpeakState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Investigate:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnInvestigate });
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnSearchingForGuns });
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnTakingCover });
                        break;
                    case Sosig.SosigOrder.Wander:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Assault:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnAssault });
                        break;
                }
            }

            H3MP_ServerSend.SosigSpeakState(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigSetCurrentOrder(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
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
                            Mod.Sosig_m_guardDominantDirection.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.guardDir);
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
                            Mod.Sosig_m_skirmishPoint.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.skirmishPoint);
                            Mod.Sosig_m_pathToPoint.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.pathToPoint);
                            Mod.Sosig_m_assaultPoint.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.assaultPoint);
                            Mod.Sosig_m_faceTowards.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.faceTowards);
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
                            Mod.Sosig_m_hardGuard.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.hardGuard);
                            Mod.Sosig_m_faceTowards.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.faceTowards);
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
                            Mod.Sosig_m_wanderPoint.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.wanderPoint);
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
                            Mod.Sosig_m_faceTowards.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.faceTowards);
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
                            Mod.Sosig_m_pathToPoint.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.pathToPoint);
                            Mod.Sosig_m_pathToLookDir.SetValue(trackedSosig.physicalObject.physicalSosigScript, trackedSosig.pathToLookDir);
                        }
                        break;
                    default:
                        trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                        break;
                }

                H3MP_ServerSend.SosigSetCurrentOrder(trackedSosig, currentOrder, clientID);
            }
        }

        public static void SosigVaporize(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosigScript.Vaporize(trackedSosig.physicalObject.physicalSosigScript.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }

            H3MP_ServerSend.SosigVaporize(sosigTrackedID, iff, clientID);
        }

        public static void SosigRequestHitDecal(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                ++SosigActionPatch.sosigRequestHitDecalSkip;
                trackedSosig.physicalObject.physicalSosigScript.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]);
                --SosigActionPatch.sosigRequestHitDecalSkip;
            }

            H3MP_ServerSend.SosigRequestHitDecal(packet, clientID);
        }

        public static void SosigLinkBreak(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }

            H3MP_ServerSend.SosigLinkBreak(sosigTrackedID, linkIndex, isStart, damClass, clientID);
        }

        public static void SosigLinkSever(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                Mod.SosigLink_SeverJoint.Invoke(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex], new object[] { damClass, isPullApart });
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }

            H3MP_ServerSend.SosigLinkSever(sosigTrackedID, linkIndex, damClass, isPullApart, clientID);
        }
        
        public static void UpToDateItems(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedItemData trackedItem = packet.ReadTrackedItem(true);
                H3MP_TrackedItemData actualTrackedItem = H3MP_Server.items[trackedItem.trackedID];
                H3MP_GameManager.UpdateTrackedItem(trackedItem, true);

                // Although we only request up to date objects from our scene/instance, it might have changed since we made the request
                // So here we check it again
                if (instantiate && actualTrackedItem.physicalItem == null && !actualTrackedItem.awaitingInstantiation &&
                    actualTrackedItem.scene.Equals(H3MP_GameManager.scene) && actualTrackedItem.instance == H3MP_GameManager.instance &&
                    H3MP_GameManager.IsItemIdentifiable(trackedItem))
                {
                    actualTrackedItem.awaitingInstantiation = true;
                    AnvilManager.Run(actualTrackedItem.Instantiate());
                }
            }
        }

        public static void UpToDateSosigs(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedSosigData trackedSosig = packet.ReadTrackedSosig(true);
                H3MP_TrackedSosigData actualTrackedSosig = H3MP_Server.sosigs[trackedSosig.trackedID];
                H3MP_GameManager.UpdateTrackedSosig(trackedSosig, true);
                if (instantiate && actualTrackedSosig.physicalObject == null && !actualTrackedSosig.awaitingInstantiation &&
                    actualTrackedSosig.scene.Equals(H3MP_GameManager.scene) && actualTrackedSosig.instance == H3MP_GameManager.instance)
                {
                    actualTrackedSosig.awaitingInstantiation = true;
                    AnvilManager.Run(actualTrackedSosig.Instantiate());
                }
            }
        }

        public static void UpToDateAutoMeaters(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedAutoMeaterData trackedAutoMeater = packet.ReadTrackedAutoMeater(true);
                H3MP_TrackedAutoMeaterData actualTrackedAutoMeater = H3MP_Server.autoMeaters[trackedAutoMeater.trackedID];
                H3MP_GameManager.UpdateTrackedAutoMeater(trackedAutoMeater, true);
                if (instantiate && actualTrackedAutoMeater.physicalObject == null && !actualTrackedAutoMeater.awaitingInstantiation &&
                    actualTrackedAutoMeater.scene.Equals(H3MP_GameManager.scene) && actualTrackedAutoMeater.instance == H3MP_GameManager.instance)
                {
                    actualTrackedAutoMeater.awaitingInstantiation = true;
                    AnvilManager.Run(actualTrackedAutoMeater.Instantiate());
                }
            }
        }

        public static void UpToDateEncryptions(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedEncryptions from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedEncryptionData trackedEncryption = packet.ReadTrackedEncryption(true);
                H3MP_TrackedEncryptionData actualTrackedEncryption = H3MP_Server.encryptions[trackedEncryption.trackedID];
                H3MP_GameManager.UpdateTrackedEncryption(trackedEncryption, true);
                if (instantiate && actualTrackedEncryption.physicalObject == null && !actualTrackedEncryption.awaitingInstantiation &&
                    actualTrackedEncryption.scene.Equals(H3MP_GameManager.scene) && actualTrackedEncryption.instance == H3MP_GameManager.instance)
                {
                    actualTrackedEncryption.awaitingInstantiation = true;
                    AnvilManager.Run(actualTrackedEncryption.Instantiate());
                }
            }
        }

        public static void DoneLoadingScene(int clientID, H3MP_Packet packet)
        {
            if(H3MP_Server.loadingClientsWaitingFrom.TryGetValue(clientID, out List<int> otherClients))
            {
                // We were waiting for this client to finish loading to send relevant items
                // Check if we are still waiting on up to date items for this client
                bool stillWaiting = false;
                foreach(int otherCLientID in otherClients)
                {
                    if(H3MP_Server.clientsWaitingUpDate.TryGetValue(otherCLientID, out List<int> clientIDs))
                    {
                        if (clientIDs.Contains(clientID))
                        {
                            stillWaiting = true;
                            break;
                        }
                    }
                }

                if (!stillWaiting)
                {
                    H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                }

                H3MP_Server.loadingClientsWaitingFrom.Remove(clientID);
            }
        }

        public static void DoneSendingUpToDateObjects(int clientID, H3MP_Packet packet)
        {
            int forClient = packet.ReadInt();

            // If clients were waiting for this client to finish sending up to date objects
            if (H3MP_Server.clientsWaitingUpDate.TryGetValue(clientID, out List<int> waitingClients))
            {
                if (forClient == 0)
                {
                    waitingClients.Remove(forClient);
                    if (waitingClients.Count == 0)
                    {
                        H3MP_Server.clientsWaitingUpDate.Remove(clientID);
                    }

                    if (H3MP_Server.loadingClientsWaitingFrom.TryGetValue(forClient, out List<int> waitingFrom))
                    {
                        waitingFrom.Remove(clientID);
                        if (waitingFrom.Count == 0)
                        {
                            // When requesting up to date objects, the server will instantiate directly upon receiving them, unlike a client, below
                            // In the server's case we then want to instantiate all missing items directly
                            H3MP_Server.loadingClientsWaitingFrom.Remove(forClient);// Items

                            // This is necessary for the case in which server sent request before some items it was returning a tracked ID for for the relevant client
                            if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(H3MP_GameManager.scene, out Dictionary<int, List<int>> itemInstances) &&
                                itemInstances.TryGetValue(H3MP_GameManager.instance, out List<int> items))
                            {
                                for (int i = 0; i < items.Count; ++i)
                                {
                                    H3MP_TrackedItemData trackedItemData = H3MP_Server.items[items[i]];
                                    if (trackedItemData != null && trackedItemData.physicalItem == null && !trackedItemData.awaitingInstantiation)
                                    {
                                        trackedItemData.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedItemData.Instantiate());
                                    }
                                }
                            }

                            // Sosigs
                            if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(H3MP_GameManager.scene, out Dictionary<int, List<int>> sosigInstances) &&
                                sosigInstances.TryGetValue(H3MP_GameManager.instance, out List<int> sosigs))
                            {
                                for (int i = 0; i < sosigs.Count; ++i)
                                {
                                    H3MP_TrackedSosigData trackedSosigData = H3MP_Server.sosigs[sosigs[i]];
                                    if (trackedSosigData != null && trackedSosigData.physicalObject == null && !trackedSosigData.awaitingInstantiation)
                                    {
                                        trackedSosigData.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedSosigData.Instantiate());
                                    }
                                }
                            }

                            // AutoMeaters
                            if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(H3MP_GameManager.scene, out Dictionary<int, List<int>> autoMeaterInstances) &&
                                autoMeaterInstances.TryGetValue(H3MP_GameManager.instance, out List<int> autoMeaters))
                            {
                                for (int i = 0; i < autoMeaters.Count; ++i)
                                {
                                    H3MP_TrackedAutoMeaterData trackedAutoMeaterData = H3MP_Server.autoMeaters[autoMeaters[i]];
                                    if (trackedAutoMeaterData != null && trackedAutoMeaterData.physicalObject == null && !trackedAutoMeaterData.awaitingInstantiation)
                                    {
                                        trackedAutoMeaterData.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedAutoMeaterData.Instantiate());
                                    }
                                }
                            }

                            // Encryptions
                            if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(H3MP_GameManager.scene, out Dictionary<int, List<int>> encryptionInstances) &&
                                encryptionInstances.TryGetValue(H3MP_GameManager.instance, out List<int> encryptions))
                            {
                                for (int i = 0; i < encryptions.Count; ++i)
                                {
                                    H3MP_TrackedEncryptionData trackedEncryptionData = H3MP_Server.encryptions[encryptions[i]];
                                    if (trackedEncryptionData != null && trackedEncryptionData.physicalObject == null && !trackedEncryptionData.awaitingInstantiation)
                                    {
                                        trackedEncryptionData.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedEncryptionData.Instantiate());
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If the relevant client is no longer loading or wasn't to begin with
                    H3MP_Server.clients[forClient].SendRelevantTrackedObjects(clientID);

                    waitingClients.Remove(forClient);
                    if (waitingClients.Count == 0)
                    {
                        H3MP_Server.clientsWaitingUpDate.Remove(clientID);
                    }

                    if (H3MP_Server.loadingClientsWaitingFrom.TryGetValue(forClient, out List<int> waitingFrom))
                    {
                        waitingFrom.Remove(clientID);
                        if (waitingFrom.Count == 0)
                        {
                            // This means this client is no longer waiting for any more up to date objects, send them the insurance relevant objects
                            H3MP_Server.loadingClientsWaitingFrom.Remove(forClient);
                            H3MP_Server.clients[forClient].SendRelevantTrackedObjects();
                        }
                    }
                }
            }
        }

        public static void AddTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances == null || !H3MP_GameManager.TNHInstances.ContainsKey(instance))
            {
                Mod.LogError("H3MP_ServerHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                H3MP_GameManager.TNHInstances[instance].AddCurrentlyPlaying(true, clientID, true);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.RemoveCurrentlyPlaying(true, clientID, true);
            }
        }

        public static void SetTNHProgression(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].progressionTypeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.progressionSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Progression(i);
                GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)i;
                --TNH_UIManagerPatch.progressionSkip;
            }

            H3MP_ServerSend.SetTNHProgression(i, instance, clientID);
        }

        public static void SetTNHEquipment(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].equipmentModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.equipmentSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_EquipmentMode(i);
                GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)i;
                --TNH_UIManagerPatch.equipmentSkip;
            }

            H3MP_ServerSend.SetTNHEquipment(i, instance, clientID);
        }

        public static void SetTNHHealthMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].healthModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthModeSkip;
                Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMode(i);
                GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)i;
                --TNH_UIManagerPatch.healthModeSkip;
            }

            H3MP_ServerSend.SetTNHHealthMode(i, instance, clientID);
        }

        public static void SetTNHTargetMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].targetModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.targetSkip;
                Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_TargetMode(i);
                GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)i;
                --TNH_UIManagerPatch.targetSkip;
            }

            H3MP_ServerSend.SetTNHTargetMode(i, instance, clientID);
        }

        public static void SetTNHAIDifficulty(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].AIDifficultyModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.AIDifficultySkip;
                Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIDifficulty(i);
                GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)i;
                --TNH_UIManagerPatch.AIDifficultySkip;
            }

            H3MP_ServerSend.SetTNHAIDifficulty(i, instance, clientID);
        }

        public static void SetTNHRadarMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].radarModeModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.radarSkip;
                Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIRadarMode(i);
                GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)i;
                --TNH_UIManagerPatch.radarSkip;
            }

            H3MP_ServerSend.SetTNHRadarMode(i, instance, clientID);
        }

        public static void SetTNHItemSpawnerMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].itemSpawnerMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.itemSpawnerSkip;
                Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_ItemSpawner(i);
                GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)i;
                --TNH_UIManagerPatch.itemSpawnerSkip;
            }

            H3MP_ServerSend.SetTNHItemSpawnerMode(i, instance, clientID);
        }

        public static void SetTNHBackpackMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].backpackMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.backpackSkip;
                Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Backpack(i);
                GM.TNHOptions.BackpackMode = (TNH_BackpackMode)i;
                --TNH_UIManagerPatch.backpackSkip;
            }

            H3MP_ServerSend.SetTNHBackpackMode(i, instance, clientID);
        }

        public static void SetTNHHealthMult(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].healthMult = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthMultSkip;
                Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMult(i);
                GM.TNHOptions.HealthMult = (TNH_HealthMult)i;
                --TNH_UIManagerPatch.healthMultSkip;
            }

            H3MP_ServerSend.SetTNHHealthMult(i, instance, clientID);
        }

        public static void SetTNHSosigGunReload(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].sosiggunShakeReloading = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.sosigGunReloadSkip;
                Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_SosiggunShakeReloading(i);
                GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)i;
                --TNH_UIManagerPatch.sosigGunReloadSkip;
            }

            H3MP_ServerSend.SetTNHSosigGunReload(i, instance, clientID);
        }

        public static void SetTNHSeed(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].TNHSeed = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.seedSkip;
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_RunSeed(i);
                GM.TNHOptions.TNHSeed = i;
                --TNH_UIManagerPatch.seedSkip;
            }

            H3MP_ServerSend.SetTNHSeed(i, instance, clientID);
        }

        public static void SetTNHLevelID(int clientID, H3MP_Packet packet)
        {
            string levelID = packet.ReadString();
            int instance = packet.ReadInt();
            
            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
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
                            Mod.TNH_UIManager_m_currentLevelIndex.SetValue(Mod.currentTNHUIManager, i);
                            Mod.currentTNHUIManager.CurLevelID = levelID;
                            Mod.TNH_UIManager_UpdateLevelSelectDisplayAndLoader.Invoke(Mod.currentTNHUIManager, null);
                            Mod.TNH_UIManager_UpdateTableBasedOnOptions.Invoke(Mod.currentTNHUIManager, null);
                            Mod.TNH_UIManager_PlayButtonSound.Invoke(Mod.currentTNHUIManager, new object[] { 2 });
                            Mod.currentTNHSceneLoader.gameObject.SetActive(true);
                            break;
                        }
                    }
                    if (!found)
                    {
                        Mod.currentTNHSceneLoader.gameObject.SetActive(false);
                        Mod.LogError("Missing TNH level: " + levelID + "! Make sure you have it installed.");
                    }
                }
            }

            H3MP_ServerSend.SetTNHLevelID(levelID, instance, clientID);
        }

        public static void SetTNHController(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int newController = packet.ReadInt();

            H3MP_GameManager.TNHInstances[instance].controller = newController;

            H3MP_ServerSend.SetTNHController(instance, newController, clientID);
        }

        public static void TNHPlayerDied(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int ID = packet.ReadInt();

            // Process dead
            H3MP_TNHInstance TNHinstance = H3MP_GameManager.TNHInstances[instance];
            TNHinstance.dead.Add(ID);
            bool allDead = true;
            for(int i=0; i < TNHinstance.currentlyPlaying.Count; ++i)
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
                foreach(int playerID in TNHinstance.dead)
                {
                    if (H3MP_GameManager.players.TryGetValue(playerID, out H3MP_PlayerManager player))
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
                    ((List<TNH_Manager.SosigPatrolSquad>)Mod.TNH_Manager_m_patrolSquads.GetValue(Mod.currentTNHInstance.manager)).Clear();
                }
                else
                {
                    if (H3MP_GameManager.players.TryGetValue(ID, out H3MP_PlayerManager player))
                    {
                        player.SetVisible(false);

                        if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null && Mod.currentTNHInstance.manager.TAHReticle != null && player.reticleContact != null)
                        {
                            for (int i = 0; i < Mod.currentTNHInstance.manager.TAHReticle.Contacts.Count; ++i)
                            {
                                if (Mod.currentTNHInstance.manager.TAHReticle.Contacts[i] == player.reticleContact)
                                {
                                    ((HashSet<Transform>)Mod.TAH_Reticle_m_trackedTransforms.GetValue(GM.TNH_Manager.TAHReticle)).Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
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

            H3MP_ServerSend.TNHPlayerDied(instance, ID, clientID);
        }

        public static void TNHAddTokens(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int amount = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.tokenCount += amount;

                // Implies we are in-game in this instance 
                if(currentInstance.manager != null && !currentInstance.dead.Contains(H3MP_GameManager.ID))
                {
                    ++TNH_ManagerPatch.addTokensSkip;
                    currentInstance.manager.AddTokens(amount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
            }

            H3MP_ServerSend.TNHAddTokens(instance, amount, clientID);
        }

        public static void AutoMeaterSetState(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte state = packet.ReadByte();

            if (H3MP_Server.autoMeaters[trackedID] != null && H3MP_Server.autoMeaters[trackedID].physicalObject != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                Mod.AutoMeater_SetState.Invoke(H3MP_Server.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript, new object[] { (AutoMeater.AutoMeaterState)state });
                --AutoMeaterSetStatePatch.skip;
            }

            H3MP_ServerSend.AutoMeaterSetState(trackedID, state, clientID);
        }

        public static void AutoMeaterSetBladesActive(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];
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

            H3MP_ServerSend.AutoMeaterSetBladesActive(trackedID, active, clientID);
        }

        public static void AutoMeaterFirearmFireAtWill(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int firearmIndex = packet.ReadInt();
            bool fireAtWill = packet.ReadBool();
            float dist = packet.ReadFloat();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }

            H3MP_ServerSend.AutoMeaterFirearmFireAtWill(trackedID, firearmIndex, fireAtWill, dist, clientID);
        }

        public static void TNHSosigKill(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];
                if (trackedSosig != null && trackedSosig.physicalObject != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.TNH_Manager_OnSosigKill.Invoke(Mod.currentTNHInstance.manager, new object[] {trackedSosig.physicalObject.physicalSosigScript});
                    --TNH_ManagerPatch.sosigKillSkip;
                }
            }

            H3MP_ServerSend.TNHSosigKill(instance, trackedID, clientID);
        }

        public static void TNHHoldPointSystemNode(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int levelIndex = packet.ReadInt();
            int holdPointIndex = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdPointIndex;
                actualInstance.level = levelIndex;

                if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    Mod.TNH_Manager_SetLevel.Invoke(actualInstance.manager, new object[] { levelIndex });
                }
            }

            H3MP_ServerSend.TNHHoldPointSystemNode(instance, levelIndex, holdPointIndex, clientID);
        }

        public static void TNHHoldBeginChallenge(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            bool fromController = packet.ReadBool();
            if (fromController)
            {
                if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
                {
                    if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                    {
                        TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);

                        // Begin hold on our side
                        ++TNH_HoldPointPatch.beginHoldSendSkip;
                        curHoldPoint.BeginHoldChallenge();
                        --TNH_HoldPointPatch.beginHoldSendSkip;

                        // TP to hold point
                        if (!actualInstance.dead.Contains(H3MP_GameManager.ID) || Mod.TNHOnDeathSpectate)
                        {
                            GM.CurrentMovementManager.TeleportToPoint(curHoldPoint.SpawnPoint_SystemNode.position, true);
                        }
                    }
                }

                // Pass it on
                H3MP_ServerSend.TNHHoldBeginChallenge(instance, true, true, clientID);
            }
            else if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                if(actualInstance.controller == H3MP_GameManager.ID)
                {
                    // We received order to begin hold and we are the controller, begin it
                    TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);
                    curHoldPoint.BeginHoldChallenge();

                    // TP to point since we are not the one who started the hold
                    if (!actualInstance.dead.Contains(H3MP_GameManager.ID) || Mod.TNHOnDeathSpectate)
                    {
                        GM.CurrentMovementManager.TeleportToPoint(curHoldPoint.SpawnPoint_SystemNode.position, true);
                    }

                    // Pass it on
                    H3MP_ServerSend.TNHHoldBeginChallenge(instance, true, true, clientID);
                }
                else if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);

                    // Begin hold on our side
                    ++TNH_HoldPointPatch.beginHoldSendSkip;
                    curHoldPoint.BeginHoldChallenge();
                    --TNH_HoldPointPatch.beginHoldSendSkip;

                    // TP to hold point
                    if (!actualInstance.dead.Contains(H3MP_GameManager.ID) || Mod.TNHOnDeathSpectate)
                    {
                        GM.CurrentMovementManager.TeleportToPoint(curHoldPoint.SpawnPoint_SystemNode.position, true);
                    }

                    // Send it to controller
                    H3MP_ServerSend.TNHHoldBeginChallenge(instance, false, false, actualInstance.controller);
                }
                else // We are not in this TNH game
                {
                    // Send it to controller
                    H3MP_ServerSend.TNHHoldBeginChallenge(instance, false, false, actualInstance.controller);
                }
            }
        }

        public static void TNHHoldPointRaiseBarriers(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
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
                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(TNHInstance.manager);

                    // Raise barriers
                    for (int i = 0; i < TNHInstance.raisedBarriers.Count; ++i)
                    {
                        TNH_DestructibleBarrierPoint point = curHoldPoint.BarrierPoints[TNHInstance.raisedBarriers[i]];
                        TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[TNHInstance.raisedBarrierPrefabIndices[i]];
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                        TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                        Mod.TNH_DestructibleBarrierPoint_m_curBarrier.SetValue(point, curBarrier);
                        curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                        curBarrier.SetBarrierPoint(point);
                        Mod.TNH_DestructibleBarrierPoint_SetCoverPointData.Invoke(point, new object[] { TNHInstance.raisedBarrierPrefabIndices[i] });
                    }
                }
            }

            H3MP_ServerSend.TNHHoldPointRaiseBarriers(clientID, packet);
        }

        public static void ShatterableCrateSetHoldingHealth(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] != null)
            {
                H3MP_Server.items[trackedID].identifyingData[1] = 1;

                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                    H3MP_Server.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                }
            }

            H3MP_ServerSend.ShatterableCrateSetHoldingHealth(trackedID, clientID);
        }

        public static void ShatterableCrateSetHoldingToken(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] != null)
            {
                H3MP_Server.items[trackedID].identifyingData[2] = 1;

                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    ++TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                    H3MP_Server.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                }
            }

            H3MP_ServerSend.ShatterableCrateSetHoldingToken(trackedID, clientID);
        }

        public static void ShatterableCrateDamage(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] != null)
            {
                if (H3MP_Server.items[trackedID].controller == H3MP_GameManager.ID)
                {
                    if (H3MP_Server.items[trackedID].physicalItem != null)
                    {
                        ++TNH_ShatterableCrateDamagePatch.skip;
                        H3MP_Server.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
                        --TNH_ShatterableCrateDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.ShatterableCrateDamage(trackedID, packet.ReadDamage());
                }
            }
        }

        public static void ShatterableCrateDestroy(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage d = packet.ReadDamage();

            if (H3MP_Server.items[trackedID] != null && H3MP_Server.items[trackedID].physicalItem != null)
            {
                TNH_ShatterableCrate crateScript = H3MP_Server.items[trackedID].physicalItem.GetComponentInChildren<TNH_ShatterableCrate>();
                if (crateScript == null)
                {
                    Mod.LogError("Received order to destroy shatterable crate for which we have physObj but it has not crate script!");
                }
                else
                {
                    ++TNH_ShatterableCrateDestroyPatch.skip;
                    Mod.TNH_ShatterableCrate_Destroy.Invoke(crateScript, new object[] { d });
                    --TNH_ShatterableCrateDestroyPatch.skip;
                }
            }

            H3MP_ServerSend.ShatterableCrateDestroy(trackedID, d, clientID);
        }

        public static void TNHSetLevel(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int level = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.level = level;

                if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    actualInstance.level = level;
                    Mod.TNH_Manager_m_level.SetValue(actualInstance.manager, level);
                    Mod.TNH_Manager_SetLevel.Invoke(actualInstance.manager, new object[] { level });
                }
            }

            H3MP_ServerSend.TNHSetLevel(instance, level, clientID);
        }

        public static void TNHSetPhaseTake(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int holdIndex = packet.ReadInt();
            int activeSupplyCount = packet.ReadInt();
            List<int> activeIndices = new List<int>();
            for(int i=0; i < activeSupplyCount; ++i)
            {
                activeIndices.Add(packet.ReadInt());
            }
            bool init = packet.ReadBool();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdIndex;
                actualInstance.phase = TNH_Phase.Take;
                actualInstance.activeSupplyPointIndices = activeIndices;

                if(!init && actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    Mod.TNH_Manager_SetPhase_Take.Invoke(Mod.currentTNHInstance.manager, null);
                }
            }

            H3MP_ServerSend.TNHSetPhaseTake(instance, holdIndex, activeIndices, init, clientID);
        }

        public static void TNHSetPhaseHold(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.phase = TNH_Phase.Hold;

                if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    Mod.TNH_Manager_SetPhase_Hold.Invoke(Mod.currentTNHInstance.manager, null);
                }
            }

            H3MP_ServerSend.TNHSetPhaseHold(instance, clientID);
        }

        public static void TNHHoldCompletePhase(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Transition;
                TNHInstance.raisedBarriers = null;
                TNHInstance.raisedBarrierPrefabIndices = null;

                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    Mod.TNH_HoldPoint_CompletePhase.Invoke(Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                }
            }

            H3MP_ServerSend.TNHHoldCompletePhase(instance, clientID);
        }

        public static void TNHHoldPointFailOut(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    Mod.TNH_HoldPoint_FailOut.Invoke((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                }
            }

            H3MP_ServerSend.TNHHoldPointFailOut(instance, clientID);
        }

        public static void TNHHoldPointBeginPhase(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = true;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    Mod.TNH_HoldPoint_BeginPhase.Invoke((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                }
            }

            H3MP_ServerSend.TNHHoldPointBeginPhase(instance, clientID);
        }

        public static void TNHHoldPointCompleteHold(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    Mod.TNH_HoldPoint_CompleteHold.Invoke((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                }
            }

            H3MP_ServerSend.TNHHoldPointCompleteHold(instance, clientID);
        }

        public static void TNHSetPhaseComplete(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.TNH_Manager_SetPhase_Completed.Invoke(Mod.currentTNHInstance.manager, null);
            }

            H3MP_ServerSend.TNHSetPhaseComplete(instance, clientID);
        }

        public static void TNHSetPhase(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            short p = packet.ReadShort();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                // Update data
                actualInstance.phase = (TNH_Phase)p;

                // Update state
                if(actualInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(actualInstance.manager))
                {
                    actualInstance.manager.Phase = (TNH_Phase)p;
                }
            }

            H3MP_ServerSend.TNHSetPhase(instance, p, clientID);
        }

        public static void EncryptionRespawnSubTarg(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Server.encryptions[trackedID] != null)
            {
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = true;

                if (H3MP_Server.encryptions[trackedID].physicalObject != null)
                {
                    H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(true);
                    Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, (int)Mod.TNH_EncryptionTarget_m_numSubTargsLeft.GetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript) + 1);
                }
            }

            H3MP_ServerSend.EncryptionRespawnSubTarg(trackedID, index, clientID);
        }

        public static void EncryptionSpawnGrowth(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Server.encryptions[trackedID] != null)
            {
                H3MP_Server.encryptions[trackedID].tendrilsActive[index] = true;
                H3MP_Server.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Server.encryptions[trackedID].subTargsPos[index] = point;
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = true;
                H3MP_Server.encryptions[trackedID].tendrilFloats[index] = 1f;

                if (H3MP_Server.encryptions[trackedID].physicalObject != null)
                {
                    Vector3 forward = point - H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                    H3MP_Server.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                    H3MP_Server.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                    ++EncryptionSpawnGrowthPatch.skip;
                    Mod.TNH_EncryptionTarget_SpawnGrowth.Invoke(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { index, point });
                    --EncryptionSpawnGrowthPatch.skip;
                }
            }

            H3MP_ServerSend.EncryptionSpawnGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionInit(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int count = packet.ReadInt();
            List<int> indices = new List<int>();
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                indices.Add(packet.ReadInt());
                points.Add(packet.ReadVector3());
            }

            if (H3MP_Server.encryptions[trackedID] != null)
            {
                for (int i = 0; i < count; ++i)
                {
                    H3MP_Server.encryptions[trackedID].tendrilsActive[indices[i]] = true;
                    H3MP_Server.encryptions[trackedID].growthPoints[indices[i]] = points[i];
                    H3MP_Server.encryptions[trackedID].subTargsPos[indices[i]] = points[i];
                    H3MP_Server.encryptions[trackedID].subTargsActive[indices[i]] = true;
                    H3MP_Server.encryptions[trackedID].tendrilFloats[indices[i]] = 1f;
                }

                if (H3MP_Server.encryptions[trackedID].physicalObject != null)
                {
                    ++EncryptionSpawnGrowthPatch.skip;
                    for (int i = 0; i < count; ++i)
                    {
                        Mod.TNH_EncryptionTarget_SpawnGrowth.Invoke(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { indices[i], points[i] });
                    }
                    --EncryptionSpawnGrowthPatch.skip;
                }
            }

            H3MP_ServerSend.EncryptionInit(clientID, trackedID, indices, points);
        }

        public static void EncryptionResetGrowth(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Server.encryptions[trackedID] != null)
            {
                H3MP_Server.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Server.encryptions[trackedID].tendrilFloats[index] = 0;
                if (H3MP_Server.encryptions[trackedID].physicalObject != null)
                {
                    Vector3 forward = point - H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                    H3MP_Server.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                    H3MP_Server.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                    ++EncryptionResetGrowthPatch.skip;
                    Mod.TNH_EncryptionTarget_ResetGrowth.Invoke(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { index, point });
                    --EncryptionResetGrowthPatch.skip;
                }
            }

            H3MP_ServerSend.EncryptionResetGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionDisableSubtarg(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Server.encryptions[trackedID] != null)
            {
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = false;

                if (H3MP_Server.encryptions[trackedID].physicalObject != null)
                {
                    H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(false);
                    Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, (int)Mod.TNH_EncryptionTarget_m_numSubTargsLeft.GetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript) - 1);
                }
            }

            H3MP_ServerSend.EncryptionDisableSubtarg(trackedID, index, clientID);
        }

        public static void EncryptionSubDamage(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == 0)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.EncryptionSubDamage(trackedEncryption, index, damage);
                }
            }
        }

        public static void SosigWeaponDamage(int clientID, H3MP_Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[sosigWeaponTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == H3MP_GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++SosigWeaponDamagePatch.skip;
                        (trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W.Damage(damage);
                        --SosigWeaponDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigWeaponDamage(trackedItem, damage);
                }
            }
        }

        public static void RemoteMissileDamage(int clientID, H3MP_Packet packet)
        {
            int RMLTrackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[RMLTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == H3MP_GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        object remoteMissile = Mod.RemoteMissileLauncher_m_missile.GetValue(H3MP_Server.items[RMLTrackedID].physicalItem.physicalObject as RemoteMissileLauncher);
                        if (remoteMissile != null)
                        {
                            ++RemoteMissileDamagePatch.skip;
                            (remoteMissile as RemoteMissile).Damage(packet.ReadDamage());
                            --RemoteMissileDamagePatch.skip;
                        }
                    }
                }
                else
                {
                    H3MP_ServerSend.RemoteMissileDamage(trackedItem, packet);
                }
            }
        }

        public static void StingerMissileDamage(int clientID, H3MP_Packet packet)
        {
            int SLTrackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[SLTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == H3MP_GameManager.ID)
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
                else
                {
                    H3MP_ServerSend.StingerMissileDamage(trackedItem, packet);
                }
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                // Set instance data
                TNHInstance.tickDownToID = packet.ReadFloat();
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Analyzing;
                TNHInstance.warpInData = new List<Vector3>();
                byte dataCount = packet.ReadByte();
                for(int i=0; i < dataCount; ++i)
                {
                    TNHInstance.warpInData.Add(packet.ReadVector3());
                }

                // If this is our TNH game, actually begin analyzing
                if(TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(GM.TNH_Manager);

                    // Note that since we received this order, we are not the controller of the instance
                    // Consequently, the warpins will not be spawned as in a normal call to BeginAnalyzing
                    // We have to spawn them ourselves with the given data
                    Mod.TNH_HoldPoint_BeginAnalyzing.Invoke(curHoldPoint, null);

                    for (int i = 0; i < dataCount; i += 2)
                    {
                        List<GameObject> warpInTargets = (List<GameObject>)Mod.TNH_HoldPoint_m_warpInTargets.GetValue(curHoldPoint);
                        warpInTargets.Add(UnityEngine.Object.Instantiate<GameObject>(curHoldPoint.M.Prefab_TargetWarpingIn, TNHInstance.warpInData[i], Quaternion.Euler(TNHInstance.warpInData[i+1])));
                    }
                }
            }

            H3MP_ServerSend.TNHHoldPointBeginAnalyzing(clientID, packet);
        }

        public static void TNHHoldIdentifyEncryption(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                // Set instance data
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Hacking;
                TNHInstance.tickDownToFailure = 120f;

                // If this is our TNH game, actually begin analyzing
                if (TNHInstance.manager != null && (bool)Mod.TNH_Manager_m_hasInit.GetValue(TNHInstance.manager))
                {
                    TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(GM.TNH_Manager);

                    Mod.TNH_HoldPoint_IdentifyEncryption.Invoke(curHoldPoint, null);
                }
            }

            H3MP_ServerSend.TNHHoldIdentifyEncryption(clientID, instance);
        }

        public static void SosigPriorityIFFChart(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int chart = packet.ReadInt();
            if (H3MP_Server.sosigs[trackedID] != null)
            {
                // Update local
                H3MP_Server.sosigs[trackedID].IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                if (H3MP_Server.sosigs[trackedID].physicalObject != null)
                {
                    H3MP_Server.sosigs[trackedID].physicalObject.physicalSosigScript.Priority.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                }
            }

            H3MP_ServerSend.SosigPriorityIFFChart(clientID, trackedID, chart);
        }

        public static void RemoteMissileDetonate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local;
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    object remoteMissile = Mod.RemoteMissileLauncher_m_missile.GetValue(H3MP_Server.items[trackedID].physicalItem.physicalObject as RemoteMissileLauncher);
                    if(remoteMissile != null)
                    {
                        RemoteMissile actualMissile = remoteMissile as RemoteMissile;
                        RemoteMissileDetonatePatch.overriden = true;
                        actualMissile.transform.position = packet.ReadVector3();
                        actualMissile.Detonante();
                    }
                }
            }

            H3MP_ServerSend.RemoteMissileDetonate(clientID, packet);
        }

        public static void StingerMissileExplode(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local;
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    StingerMissile missile = H3MP_Server.items[trackedID].physicalItem.stingerMissile;
                    if(missile != null)
                    {
                        StingerMissileExplodePatch.overriden = true;
                        missile.transform.position = packet.ReadVector3();
                        Mod.StingerMissile_Explode.Invoke(missile, null);
                    }
                }
            }

            H3MP_ServerSend.StingerMissileExplode(clientID, packet);
        }

        public static void PinnedGrenadeExplode(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (trackedID == -1 || trackedID >= H3MP_Server.items.Length)
            {
                Mod.LogError("Server received order to explode pinned grenade with tracked ID: "+trackedID+" but items array is not large enough to hold this ID!");
            }
            else
            {
                if (H3MP_Server.items[trackedID] != null)
                {
                    // Update local;
                    if (H3MP_Server.items[trackedID].physicalItem != null)
                    {
                        PinnedGrenade grenade = H3MP_Server.items[trackedID].physicalItem.physicalObject as PinnedGrenade;
                        if (grenade != null)
                        {
                            PinnedGrenadePatch.ExplodePinnedGrenade(grenade, packet.ReadVector3());
                        }
                    }
                }
            }

            H3MP_ServerSend.PinnedGrenadeExplode(clientID, packet);
        }

        public static void PinnedGrenadePullPin(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (trackedID == -1 || trackedID >= H3MP_Server.items.Length)
            {
                Mod.LogError("Server received order to pull pin on pinned grenade with tracked ID: "+trackedID+" but items array is not large enough to hold this ID!");
            }
            else
            {
                if (H3MP_Server.items[trackedID] != null)
                {
                    if (H3MP_Server.items[trackedID].physicalItem != null)
                    {
                        PinnedGrenade grenade = H3MP_Server.items[trackedID].physicalItem.physicalObject as PinnedGrenade;
                        if (grenade != null)
                        {
                            List<PinnedGrenadeRing> rings = (List<PinnedGrenadeRing>)Mod.PinnedGrenade_m_rings.GetValue(grenade);
                            for(int i=0; i< rings.Count; ++i)
                            {
                                if (!rings[i].HasPinDetached())
                                {
                                    Mod.PinnedGrenadeRing_m_hasPinDetached.SetValue(rings[i], true);
                                    rings[i].Pin.RootRigidbody = rings[i].Pin.gameObject.AddComponent<Rigidbody>();
                                    rings[i].Pin.RootRigidbody.mass = 0.02f;
                                    rings[i].ForceBreakInteraction();
                                    rings[i].transform.SetParent(rings[i].Pin.transform);
                                    rings[i].Pin.enabled = true;
                                    SM.PlayCoreSound(FVRPooledAudioType.GenericClose, rings[i].G.AudEvent_Pinpull, rings[i].transform.position);
                                    rings[i].GetComponent<Collider>().enabled = false;
                                    rings[i].enabled = false;
                                }
                            }

                            // If we control and is in spawn lock, want to duplicate and switch in QBS
                            if (H3MP_Server.items[trackedID].controller == H3MP_GameManager.ID &&
                                H3MP_Server.items[trackedID].physicalItem.physicalObject.m_isSpawnLock) // Implies is in QBS
                            {
                                // Keep ref to QBS
                                FVRQuickBeltSlot slot = H3MP_Server.items[trackedID].physicalItem.physicalObject.QuickbeltSlot;

                                // Detach original with now pulled pin from QBS
                                H3MP_Server.items[trackedID].physicalItem.physicalObject.ClearQuickbeltState();

                                // Spawn replacement
                                GameObject replacement = H3MP_Server.items[trackedID].physicalItem.physicalObject.DuplicateFromSpawnLock(null);
                                FVRPhysicalObject phys = replacement.GetComponent<FVRPhysicalObject>();

                                // Set replacement to the QBS
                                phys.SetQuickBeltSlot(slot);

                                // Set spawnlock
                                phys.ToggleQuickbeltState();
                            }
                        }
                    }
                }
            }

            H3MP_ServerSend.PinnedGrenadePullPin(trackedID, clientID);
        }

        public static void FVRGrenadeExplode(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local;
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    FVRGrenade grenade = H3MP_Server.items[trackedID].physicalItem.physicalObject as FVRGrenade;
                    if(grenade != null)
                    {
                        FVRGrenadePatch.ExplodeGrenade(grenade, packet.ReadVector3());
                    }
                }
            }

            H3MP_ServerSend.FVRGrenadeExplode(clientID, packet);
        }

        public static void ClientDisconnect(int clientID, H3MP_Packet packet)
        {
            if (H3MP_Server.clients[clientID].connected && // If a client is connected at that index, AND
                H3MP_Server.clients[clientID].tcp.openTime < packet.ReadLong()) // If it is not a new client we have connected since the client has disconnected
            {
                H3MP_Server.clients[clientID].Disconnect(2);
            }
        }

        public static void BangSnapSplode(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    BangSnap bangSnap = H3MP_Server.items[trackedID].physicalItem.physicalObject as BangSnap;
                    if (bangSnap != null)
                    {
                        bangSnap.transform.position = packet.ReadVector3();
                        ++BangSnapPatch.skip;
                        Mod.BangSnap_Splode.Invoke(bangSnap, null);
                        --BangSnapPatch.skip;
                    }
                }
            }

            H3MP_ServerSend.BangSnapSplode(clientID, packet);
        }

        public static void C4Detonate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    C4 c4 = H3MP_Server.items[trackedID].physicalItem.physicalObject as C4;
                    if (c4 != null)
                    {
                        c4.transform.position = packet.ReadVector3();
                        ++C4DetonatePatch.skip;
                        c4.Detonate();
                        --C4DetonatePatch.skip;
                    }
                }
            }

            H3MP_ServerSend.C4Detonate(clientID, packet);
        }

        public static void ClaymoreMineDetonate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    ClaymoreMine cm = H3MP_Server.items[trackedID].physicalItem.physicalObject as ClaymoreMine;
                    if (cm != null)
                    {
                        cm.transform.position = packet.ReadVector3();
                        ++ClaymoreMineDetonatePatch.skip;
                        Mod.ClaymoreMine_Detonate.Invoke(cm, null);
                        --ClaymoreMineDetonatePatch.skip;
                    }
                }
            }

            H3MP_ServerSend.ClaymoreMineDetonate(clientID, packet);
        }

        public static void SLAMDetonate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (H3MP_Server.items[trackedID] != null)
            {
                // Update local
                if (H3MP_Server.items[trackedID].physicalItem != null)
                {
                    SLAM slam = H3MP_Server.items[trackedID].physicalItem.physicalObject as SLAM;
                    if (slam != null)
                    {
                        slam.transform.position = packet.ReadVector3();
                        ++SLAMDetonatePatch.skip;
                        slam.Detonate();
                        --SLAMDetonatePatch.skip;
                    }
                }
            }

            H3MP_ServerSend.SLAMDetonate(clientID, packet);
        }

        public static void SpectatorHost(int clientID, H3MP_Packet packet)
        {
            bool spectatorHost = packet.ReadBool();

            if (spectatorHost) 
            {
                if (!H3MP_GameManager.spectatorHosts.Contains(clientID))
                {
                    H3MP_GameManager.spectatorHosts.Add(clientID);
                    H3MP_Server.availableSpectatorHosts.Add(clientID);
                }
            }
            else
            {
                H3MP_GameManager.spectatorHosts.Remove(clientID);
                H3MP_Server.availableSpectatorHosts.Remove(clientID);
            }

            H3MP_GameManager.UpdatePlayerHidden(H3MP_GameManager.players[clientID]);

            //TODO: Update UI

            H3MP_ServerSend.SpectatorHost(clientID, spectatorHost);
        }

        public static void ResetTNH(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance) && actualInstance.controller == clientID)
            {
                actualInstance.Reset();

                if(actualInstance.manager != null)
                {
                    actualInstance.ResetManager();
                }

                H3MP_ServerSend.ResetTNH(instance);
            }
        }

        public static void ReviveTNHPlayer(int clientID, H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.RevivePlayer(ID, true);

                H3MP_ServerSend.ReviveTNHPlayer(ID, instance, clientID);
            }
        }

        public static void PlayerColor(int clientID, H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            int index = packet.ReadInt();

            H3MP_GameManager.SetPlayerColor(ID, index, true, clientID);
        }

        public static void RequestTNHInitialization(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            Mod.LogInfo("TNH Initializion requested by " + clientID + " for " + instance);
            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                if (TNHInstance.initializer == -1)
                {
                    Mod.LogInfo("\tNo initializer yet, granting init");
                    TNHInstance.initializer = clientID;
                    TNHInstance.initializationRequested = true; // We are waiting for init from this player

                    H3MP_ServerSend.TNHInitializer(instance, clientID, true);
                }
                // else, already have an initializer, ignore
            }
        }

        public static void TNHInitializer(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            Mod.LogInfo("Received TNH Initializion by " + clientID + " for " + instance);
            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance TNHInstance))
            {
                if(TNHInstance.initializer == clientID)
                {
                    Mod.LogInfo("\tThey were initializer, relaying");
                    TNHInstance.initializationRequested = false;
                    H3MP_ServerSend.TNHInitializer(instance, clientID);
                }
                else
                {
                    Mod.LogError("Server received signal that "+clientID+" init TNH "+instance+" but they aren't the initializer!");
                }
            }
        }

        public static void FuseIgnite(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if(itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is FVRFusedThrowable)
            {
                ++FusePatch.igniteSkip;
                (itemData.physicalItem.physicalObject as FVRFusedThrowable).Fuse.Ignite(0);
                --FusePatch.igniteSkip;
            }

            H3MP_ServerSend.FuseIgnite(trackedID, clientID);
        }

        public static void FuseBoom(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if(itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is FVRFusedThrowable)
            {
                (itemData.physicalItem.physicalObject as FVRFusedThrowable).Fuse.Boom();
            }

            H3MP_ServerSend.FuseBoom(trackedID, clientID);
        }

        public static void MolotovShatter(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool ignited = packet.ReadBool();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if (itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalObject is Molotov)
            {
                Molotov asMolotov = itemData.physicalItem.physicalObject as Molotov;
                if (ignited && !asMolotov.Igniteable.IsOnFire())
                {
                    asMolotov.RemoteIgnite();
                }
                ++MolotovPatch.shatterSkip;
                Mod.Molotov_Shatter.Invoke(asMolotov, null);
                --MolotovPatch.shatterSkip;
            }

            H3MP_ServerSend.MolotovShatter(trackedID, ignited, clientID);
        }

        public static void MolotovDamage(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID]; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
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
                    H3MP_ServerSend.MolotovDamage(itemData, damage);
                }
            }
        }

        public static void MagazineAddRound(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID]; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MagazinePatch.addRoundSkip;
                        (itemData.physicalItem.physicalObject as FVRFireArmMagazine).AddRound(roundClass, true, true);
                        --MagazinePatch.addRoundSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.MagazineAddRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void ClipAddRound(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID]; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++ClipPatch.addRoundSkip;
                        (itemData.physicalItem.physicalObject as FVRFireArmClip).AddRound(roundClass, true, true);
                        --ClipPatch.addRoundSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.ClipAddRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void SpeedloaderChamberLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID]; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++SpeedloaderChamberPatch.loadSkip;
                        (itemData.physicalItem.physicalObject as Speedloader).Chambers[chamberIndex].Load(roundClass, true);
                        --SpeedloaderChamberPatch.loadSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SpeedloaderChamberLoad(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void RemoteGunChamber(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID]; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        FVRFireArmRound round = AM.GetRoundSelfPrefab(roundType, roundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        ++RemoteGunPatch.chamberSkip;
                        (itemData.physicalItem.physicalObject as RemoteGun).ChamberCartridge(round);
                        --RemoteGunPatch.chamberSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.RemoteGunChamber(itemData.trackedID, roundClass, roundType, clientID);
                }
            }
        }

        public static void ChamberRound(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null && itemData.physicalItem.chamberRound != null)
                    {
                        itemData.physicalItem.chamberRound(roundClass, (FireArmRoundType)(-1), chamberIndex);
                    }
                }
                else
                {
                    H3MP_ServerSend.ChamberRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void MagazineLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();
            short slot = packet.ReadShort();

            H3MP_TrackedItemData magItemData = H3MP_Server.items[trackedID];
            H3MP_TrackedItemData FAItemData = H3MP_Server.items[FATrackedID];
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
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
                else
                {
                    H3MP_ServerSend.MagazineLoad(trackedID, FATrackedID, slot, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load mag " + trackedID + " into firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void MagazineLoadAttachable(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            H3MP_TrackedItemData magItemData = H3MP_Server.items[trackedID];
            H3MP_TrackedItemData FAItemData = H3MP_Server.items[FATrackedID];
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        ++MagazinePatch.loadSkip;
                        (magItemData.physicalItem.physicalObject as FVRFireArmMagazine).Load(FAItemData.physicalItem.dataObject as AttachableFirearm);
                        --MagazinePatch.loadSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.MagazineLoadAttachable(trackedID, FATrackedID, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load mag " + trackedID + " into attachable firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void ClipLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            H3MP_TrackedItemData clipItemData = H3MP_Server.items[trackedID];
            H3MP_TrackedItemData FAItemData = H3MP_Server.items[FATrackedID];
            if (clipItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
                {
                    if (FAItemData.physicalItem != null && clipItemData.physicalItem != null)
                    {
                        ++ClipPatch.loadSkip;
                        (clipItemData.physicalItem.physicalObject as FVRFireArmClip).Load(FAItemData.physicalItem.physicalObject as FVRFireArm);
                        --ClipPatch.loadSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.ClipLoad(trackedID, FATrackedID, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load clip " + trackedID + " into firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void RevolverCylinderLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
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
                        Revolver revolver = itemData.physicalItem.physicalObject as Revolver;
                        ++ChamberPatch.chamberSkip;
                        for(int i=0; i < revolver.Chambers.Length; ++i)
                        {
                            if (classes[i] != -1)
                            {
                                revolver.Chambers[i].SetRound((FireArmRoundClass)classes[i], revolver.Chambers[i].transform.position, revolver.Chambers[i].transform.rotation);
                            }
                        }
                        --ChamberPatch.chamberSkip;
                    }
                }
                else
                {
                    H3MP_ServerSend.RevolverCylinderLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolver cylinder " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void RevolvingShotgunLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
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
                else
                {
                    H3MP_ServerSend.RevolvingShotgunLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolving shotgun " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void GrappleGunLoad(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
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
                        Mod.GrappleGun_m_curChamber.SetValue(grappleGun, 0);
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
                else
                {
                    H3MP_ServerSend.GrappleGunLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolving shotgun " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void CarlGustafLatchSate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if (itemData != null)
            {
                byte type = packet.ReadByte();
                byte state = packet.ReadByte();

                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustafLatch latch = (itemData.physicalItem.physicalObject as CarlGustaf).TailLatch;
                        if(type == 1)
                        {
                            latch = latch.RestrictingLatch;
                        }

                        ++CarlGustafLatchPatch.skip;
                        if (state == 0) // Closed
                        {
                            if (latch.LState != CarlGustafLatch.CGLatchState.Closed)
                            {
                                float val = latch.IsMinOpen ? latch.RotMax : latch.RotMin;
                                Mod.CarlGustafLatch_m_curRot.SetValue(latch, val);
                                Mod.CarlGustafLatch_m_tarRot.SetValue(latch, val);
                                latch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                                latch.LState = CarlGustafLatch.CGLatchState.Closed;
                            }
                        }
                        else if (latch.LState != CarlGustafLatch.CGLatchState.Open)
                        {
                            float val = latch.IsMinOpen ? latch.RotMin : latch.RotMax;
                            Mod.CarlGustafLatch_m_curRot.SetValue(latch, val);
                            Mod.CarlGustafLatch_m_tarRot.SetValue(latch, val);
                            latch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                            latch.LState = CarlGustafLatch.CGLatchState.Open;
                        }
                        --CarlGustafLatchPatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.CarlGustafLatchSate(trackedID, (CarlGustafLatch.CGLatchType)type, (CarlGustafLatch.CGLatchState)state, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to set latch state of CG " + trackedID + " but we are missing item data!");
            }
        }

        public static void CarlGustafShellSlideSate(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData itemData = H3MP_Server.items[trackedID];
            if (itemData != null)
            {
                byte state = packet.ReadByte();

                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustaf asCG = itemData.physicalItem.physicalObject as CarlGustaf;

                        ++CarlGustafShellInsertEjectPatch.skip;
                        if (state == 0) // In
                        {
                            if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.In)
                            {
                                Mod.CarlGustafShellInsertEject_m_curZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                                Mod.CarlGustafShellInsertEject_m_tarZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                                asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                                asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.In;
                            }
                        }
                        else if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Out)
                        {
                            Mod.CarlGustafShellInsertEject_m_curZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                            Mod.CarlGustafShellInsertEject_m_tarZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                            asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                            asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Out;
                        }
                        --CarlGustafShellInsertEjectPatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.CarlGustafShellSlideSate(trackedID, (CarlGustafShellInsertEject.ChamberSlideState)state, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to set shell slide state of CG " + trackedID + " but we are missing item data!");
            }
        }

        public static void TNHHostStartHold(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                if (Mod.currentTNHInstance.manager != null && !Mod.currentTNHInstance.holdOngoing)
                {
                    GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode.position, true);
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                }
            }
            else
            {
                H3MP_ServerSend.TNHHostStartHold(Mod.currentTNHInstance.instance, clientID);
            }
        }
    }
}
