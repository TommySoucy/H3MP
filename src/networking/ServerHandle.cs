using FistVR;
using H3MP.Patches;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Networking
{
    public class ServerHandle
    {
        public static void WelcomeReceived(int clientID, Packet packet)
        {
            int clientIDCheck = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();
            int IFF = packet.ReadInt();
            int colorIndex = packet.ReadInt();

            Mod.LogInfo($"{Server.clients[clientID].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {clientID}", false);

            if (clientID != clientIDCheck)
            {
                Mod.LogInfo($"Player \"{username}\" (ID:{clientID}) has assumed wrong client ID ({clientIDCheck})", false);
            }

            // Spawn player to clients 
            Server.clients[clientID].SendIntoGame(username, scene, instance, IFF, colorIndex);

            Server.connectedClients.Add(clientID);

            // Tell the client connection is complete
            ServerSend.ConnectionComplete(clientID);

            if (ISClient.isConnected && ISClient.listed)
            {
                ISClientSend.PlayerCount(GameManager.players.Count);
            }
        }

        public static void Ping(int clientID, Packet packet)
        {
            long time = packet.ReadLong();
            Server.clients[clientID].ping = Convert.ToInt64((DateTime.Now.ToUniversalTime() - ThreadManager.epoch).TotalMilliseconds) - time;
            ServerSend.Ping(clientID, time);
        }

        public static void PlayerState(int clientID, Packet packet)
        {
            if(Server.clients[clientID] == null || Server.clients[clientID].player == null)
            {
                // This case is possible if we received player disconnection but we still receive late UDP player state packets
                return;
            }

            Player player = Server.clients[clientID].player;

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

            GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                          player.leftHandPos, player.leftHandRot,
                                          player.rightHandPos, player.rightHandRot,
                                          player.health, player.maxHealth, additionalData);

            // Relay
            ServerSend.PlayerState(packet, player);
        }

        public static void PlayerIFF(int clientID, Packet packet)
        {
            int IFF = packet.ReadInt();
            if (Server.clients.ContainsKey(clientID))
            {
                Server.clients[clientID].player.IFF = IFF;
            }
            if (GameManager.players.ContainsKey(clientID))
            {
                GameManager.players[clientID].SetIFF(IFF);
            }

            ServerSend.PlayerIFF(clientID, IFF);
        }

        public static void PlayerScene(int clientID, Packet packet)
        {
            Player player = Server.clients[clientID].player;

            string scene = packet.ReadString();

            GameManager.UpdatePlayerScene(player.ID, scene);

            // Send to all other clients
            ServerSend.PlayerScene(player.ID, scene);

            // Request most up to date items from relevant clients, so we can send them to this client 
            if (!GameManager.nonSynchronizedScenes.ContainsKey(scene))
            {
                if (GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                {
                    List<int> waitingFromClients = new List<int>();

                    // There are other players in the client's scene/instance, request up to date objects before sending
                    for (int i = 0; i < otherPlayers.Count; ++i)
                    {
                        if (otherPlayers[i] != clientID)
                        {
                            if (Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                            {
                                Server.clientsWaitingUpDate[otherPlayers[i]].Add(clientID);
                            }
                            else
                            {
                                Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { clientID });
                            }
                            ServerSend.RequestUpToDateObjects(otherPlayers[i], false, clientID);
                            waitingFromClients.Add(otherPlayers[i]);
                        }
                    }

                    if (waitingFromClients.Count > 0)
                    {
                        if (Server.loadingClientsWaitingFrom.ContainsKey(clientID))
                        {
                            Server.loadingClientsWaitingFrom[clientID] = waitingFromClients;
                        }
                        else
                        {
                            Server.loadingClientsWaitingFrom.Add(clientID, waitingFromClients);
                        }
                    }
                    else
                    {
                        Server.clients[clientID].SendRelevantTrackedObjects();
                    }
                }
                else // No other player in the client's scene/instance 
                {
                    Server.clients[clientID].SendRelevantTrackedObjects();
                }
            }
        }

        public static void PlayerInstance(int clientID, Packet packet)
        {
            Player player = Server.clients[clientID].player;

            int instance = packet.ReadInt();
            bool wasLoading = packet.ReadBool();

            GameManager.UpdatePlayerInstance(player.ID, instance);

            // Send to all other clients
            ServerSend.PlayerInstance(player.ID, instance);

            // We don't want to request nor send up to date objects to a client that was in the process of loading into a new
            // scene when it changed instance. We will instead send up to date objects when they arrive at their new scene
            if (!wasLoading) 
            {
                // Request most up to date items from relevant clients so we can send them to the client when it is ready to receive them
                if (!GameManager.nonSynchronizedScenes.ContainsKey(player.scene))
                {
                    if (GameManager.playersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> instances) &&
                        instances.TryGetValue(player.instance, out List<int> otherPlayers) && otherPlayers.Count > 1)
                    {
                        List<int> waitingFromClients = new List<int>();

                        // There are other players in the client's scene/instance, request up to date objects before sending
                        for (int i = 0; i < otherPlayers.Count; ++i)
                        {
                            if (otherPlayers[i] != clientID)
                            {
                                if (Server.clientsWaitingUpDate.ContainsKey(otherPlayers[i]))
                                {
                                    Server.clientsWaitingUpDate[otherPlayers[i]].Add(clientID);
                                }
                                else
                                {
                                    Server.clientsWaitingUpDate.Add(otherPlayers[i], new List<int> { clientID });
                                }
                                ServerSend.RequestUpToDateObjects(otherPlayers[i], false, clientID);
                                waitingFromClients.Add(otherPlayers[i]);
                            }
                        }

                        if (waitingFromClients.Count > 0)
                        {
                            if (Server.loadingClientsWaitingFrom.ContainsKey(clientID))
                            {
                                Server.loadingClientsWaitingFrom[clientID] = waitingFromClients;
                            }
                            else
                            {
                                Server.loadingClientsWaitingFrom.Add(clientID, waitingFromClients);
                            }
                        }
                        else
                        {
                            Server.clients[clientID].SendRelevantTrackedObjects();
                        }
                    }
                    else // No other player in the client's scene/instance 
                    {
                        Server.clients[clientID].SendRelevantTrackedObjects();
                    }
                }
            }
        }

        public static void AddTNHInstance(int clientID, Packet packet)
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
            ServerSend.AddTNHInstance(GameManager.AddNewTNHInstance(hostID, letPeopleJoin,
                                                                              progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                                                              targetModeSetting, AIDifficultyModifier, radarModeModifier, itemSpawnerMode, backpackMode,
                                                                              healthMult, sosiggunShakeReloading, TNHSeed, levelID));
        }

        public static void AddInstance(int clientID, Packet packet)
        {
            // Send to all clients
            ServerSend.AddInstance(GameManager.AddNewInstance());
        }

        public static void TrackedObject(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            if (Mod.trackedObjectTypesByName.TryGetValue(typeID, out Type trackedObjectType))
            {
                Server.AddTrackedObject((TrackedObjectData)Activator.CreateInstance(Mod.trackedObjectTypesByName[typeID], packet, typeID, trackedID), clientID);
            }
        }

        public static void TrackedObjects(int clientID, Packet packet)
        {
            //int count = packet.ReadShort();
            //for(int i=0; i < count; ++i)
            //{
                TrackedObjectData.Update(packet, true);
            //}

            // Relay to other clients in same scene/instance as clientID right away
            // TODO: Optimization: Is this better than just sending it to everyone and client just ignoring if don't have data?
            if (GameManager.playersByInstanceByScene.TryGetValue(Server.clients[clientID].player.scene, out Dictionary<int, List<int>> instances) && 
                instances.TryGetValue(Server.clients[clientID].player.instance, out List<int> players))
            {
                ServerSend.TrackedObjects(packet, players, clientID);
            }
        }

        public static void ObjectUpdate(int clientID, Packet packet)
        {
            TrackedObjectData.Update(packet, false);

            // Send to all other clients
            ServerSend.ObjectUpdate(packet, clientID);
        }

        public static void GiveObjectControl(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();
            int debounceCount = packet.ReadInt();
            List<int> debounce = new List<int>();
            for (int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }
            Mod.LogInfo("Server received GiveObjectControl for " + trackedID + " to controller: " + newController, false);

            // Update locally
            TrackedObjectData trackedObject = Server.objects[trackedID];

            bool destroyed = false;

            if (trackedObject == null)
            {
                Mod.LogError("Server received order to set object " + trackedID + " controller to " + newController + " but object is missing from objects array!");
                ServerSend.DestroyObject(trackedID);
            }
            else
            {
                if (trackedObject.controller != 0 && newController == 0)
                {
                    Mod.LogInfo("\tNew controller", false);
                    trackedObject.localTrackedID = GameManager.objects.Count;
                    GameManager.objects.Add(trackedObject);
                    // Physical object could be null if we are given control while we are loading, the giving client will think we are in their scene/instance
                    if (trackedObject.physical == null)
                    {
                        Mod.LogInfo("\t\tNo phys", false);
                        // If its is null and we receive this after having finished loading, we only want to instantiate if it is in our current scene/instance
                        // Otherwise we send destroy order for the object
                        if (!GameManager.sceneLoading)
                        {
                            Mod.LogInfo("\t\t\tNot loading", false);
                            if (trackedObject.scene.Equals(GameManager.scene) && trackedObject.instance == GameManager.instance)
                            {
                                Mod.LogInfo("\t\t\t\tSame scene instance, instantiating", false);
                                if (!trackedObject.awaitingInstantiation)
                                {
                                    trackedObject.awaitingInstantiation = true;
                                    AnvilManager.Run(trackedObject.Instantiate());
                                }
                            }
                            else
                            {
                                Mod.LogInfo("\t\t\t\tDifferent scene instance, can't take control", false);
                                if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> playerInstances) &&
                                    playerInstances.TryGetValue(trackedObject.instance, out List<int> playerList))
                                {
                                    List<int> newPlayerList = new List<int>(playerList);
                                    for (int i = 0; i < debounce.Count; ++i)
                                    {
                                        newPlayerList.Remove(debounce[i]);
                                    }
                                    newController = Mod.GetBestPotentialObjectHost(trackedObject.controller, true, true, newPlayerList, trackedObject.scene, trackedObject.instance);
                                    if (newController == -1)
                                    {
                                        Mod.LogInfo("\t\t\t\t\tNo one to bounce to, destroying", false);
                                        ServerSend.DestroyObject(trackedID);
                                        trackedObject.RemoveFromLocal();
                                        Server.objects[trackedID] = null;

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
                                        Mod.LogInfo("\t\t\t\t\tBouncing to " + newController, false);
                                        trackedObject.RemoveFromLocal();
                                        debounce.Add(GameManager.ID);
                                        // Don't resend give control here right away, we will send at the end
                                    }
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\t\t\tNo one to bounce to, destroying", false);
                                    ServerSend.DestroyObject(trackedID);
                                    trackedObject.RemoveFromLocal();
                                    Server.objects[trackedID] = null;
                                    if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                        currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                    {
                                        objectList.Remove(trackedObject.trackedID);
                                    }
                                    trackedObject.awaitingInstantiation = false;
                                    destroyed = true;
                                }

                                if(newController == -1)
                                {
                                    if (Server.connectedClients.Count > 0)
                                    {
                                        if (Server.availableIndexBufferWaitingFor.TryGetValue(trackedObject.trackedID, out List<int> waitingForPlayers))
                                        {
                                            for (int j = 0; j < Server.connectedClients.Count; ++j)
                                            {
                                                if (!waitingForPlayers.Contains(Server.connectedClients[j]))
                                                {
                                                    waitingForPlayers.Add(Server.connectedClients[j]);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Server.availableIndexBufferWaitingFor.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                        }
                                        for (int j = 0; j < Server.connectedClients.Count; ++j)
                                        {
                                            if (Server.availableIndexBufferClients.TryGetValue(Server.connectedClients[j], out List<int> existingIndices))
                                            {
                                                // Already waiting for this client's confirmation for some index, just add it to existing list
                                                existingIndices.Add(trackedObject.trackedID);
                                            }
                                            else // Not yet waiting for this client's confirmation for an index, add entry to dict
                                            {
                                                Server.availableIndexBufferClients.Add(Server.connectedClients[j], new List<int>() { trackedObject.trackedID });
                                            }
                                        }

                                        // Add to dict of IDs to request
                                        if (Server.IDsToConfirm.TryGetValue(trackedObject.trackedID, out List<int> clientList))
                                        {
                                            for (int j = 0; j < Server.connectedClients.Count; ++j)
                                            {
                                                if (!clientList.Contains(Server.connectedClients[j]))
                                                {
                                                    clientList.Add(Server.connectedClients[j]);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Server.IDsToConfirm.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                        }

                                        Mod.LogInfo("Added " + trackedObject.trackedID + " to ID buffer");
                                    }
                                    else // No one to request ID availability from, can just readd directly
                                    {
                                        Server.availableObjectIndices.Add(trackedObject.trackedID);
                                    }
                                }
                            }
                        }
                        else // Loading or not our scene/instance
                        {
                            Mod.LogInfo("\t\t\tLoading or not our scene/instance", false);
                            if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> playerInstances) &&
                                playerInstances.TryGetValue(trackedObject.instance, out List<int> playerList))
                            {
                                List<int> newPlayerList = new List<int>(playerList);
                                for (int i = 0; i < debounce.Count; ++i)
                                {
                                    newPlayerList.Remove(debounce[i]);
                                }
                                newController = Mod.GetBestPotentialObjectHost(trackedObject.controller, true, true, newPlayerList, trackedObject.scene, trackedObject.instance);
                                if (newController == -1)
                                {
                                    Mod.LogInfo("\t\t\t\tNo one to bounce to, destroying", false);
                                    ServerSend.DestroyObject(trackedID);
                                    trackedObject.RemoveFromLocal();
                                    Server.objects[trackedID] = null;

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
                                    Mod.LogInfo("\t\t\t\tBouncing to " + newController, false);
                                    trackedObject.RemoveFromLocal();
                                    debounce.Add(GameManager.ID);
                                    // Don't resend give control here right away, we will send at the end
                                }
                            }
                            else
                            {
                                Mod.LogInfo("\t\t\t\tNo one to bounce to, destroying", false);
                                ServerSend.DestroyObject(trackedID);
                                trackedObject.RemoveFromLocal();
                                Server.objects[trackedID] = null;
                                if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                    currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                {
                                    objectList.Remove(trackedObject.trackedID);
                                }
                                trackedObject.awaitingInstantiation = false;
                                destroyed = true;
                            }

                            if (newController == -1)
                            {
                                if (Server.connectedClients.Count > 0)
                                {
                                    if (Server.availableIndexBufferWaitingFor.TryGetValue(trackedObject.trackedID, out List<int> waitingForPlayers))
                                    {
                                        for (int j = 0; j < Server.connectedClients.Count; ++j)
                                        {
                                            if (!waitingForPlayers.Contains(Server.connectedClients[j]))
                                            {
                                                waitingForPlayers.Add(Server.connectedClients[j]);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Server.availableIndexBufferWaitingFor.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                    }
                                    for (int j = 0; j < Server.connectedClients.Count; ++j)
                                    {
                                        if (Server.availableIndexBufferClients.TryGetValue(Server.connectedClients[j], out List<int> existingIndices))
                                        {
                                            // Already waiting for this client's confirmation for some index, just add it to existing list
                                            existingIndices.Add(trackedObject.trackedID);
                                        }
                                        else // Not yet waiting for this client's confirmation for an index, add entry to dict
                                        {
                                            Server.availableIndexBufferClients.Add(Server.connectedClients[j], new List<int>() { trackedObject.trackedID });
                                        }
                                    }

                                    // Add to dict of IDs to request
                                    if (Server.IDsToConfirm.TryGetValue(trackedObject.trackedID, out List<int> clientList))
                                    {
                                        for (int j = 0; j < Server.connectedClients.Count; ++j)
                                        {
                                            if (!clientList.Contains(Server.connectedClients[j]))
                                            {
                                                clientList.Add(Server.connectedClients[j]);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Server.IDsToConfirm.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                    }

                                    Mod.LogInfo("Added " + trackedObject.trackedID + " to ID buffer");
                                }
                                else // No one to request ID availability from, can just readd directly
                                {
                                    Server.availableObjectIndices.Add(trackedObject.trackedID);
                                }
                            }
                        }
                    }
                }
                else if (trackedObject.controller == 0 && newController != 0)
                {
                    Mod.LogInfo("\tNew controller", false);
                    trackedObject.RemoveFromLocal();
                }

                if (!destroyed)
                {
                    trackedObject.SetController(newController);

                    // Send to all other clients
                    ServerSend.GiveObjectControl(trackedID, newController, debounce);
                }
            }
        }

        public static void DestroyObject(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            TrackedObjectData trackedObject = Server.objects[trackedID];

            Mod.LogInfo("Server received object destruction for tracked ID: " + trackedID + ", remove from lists: " + removeFromList, false);
            if (trackedObject != null)
            {
                Mod.LogInfo("\tGot object", false);
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

            ServerSend.DestroyObject(trackedID, removeFromList, clientID);
        }

        public static void ObjectParent(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            Server.objects[trackedID].SetParent(newParentID);

            // Send to all other clients
            ServerSend.ObjectParent(trackedID, newParentID, clientID);
        }

        public static void WeaponFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Server.objects[trackedID] == null)
            {
                Mod.LogError("Server received order to fire weapon " + trackedID + " but item is missing from items array!");
            }
            else
            {
                // Update locally
                if (Server.objects[trackedID].physical != null)
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
                    (Server.objects[trackedID] as TrackedItemData).physicalItem.setFirearmUpdateOverride((FireArmRoundType)roundType, (FireArmRoundClass)roundClass, chamberIndex);
                    ++ProjectileFirePatch.skipBlast;
                    (Server.objects[trackedID] as TrackedItemData).physicalItem.fireFunc(chamberIndex);
                    --ProjectileFirePatch.skipBlast;
                }
            }

            // Send to other clients
            ServerSend.WeaponFire(clientID, packet);
        }

        public static void FlintlockWeaponBurnOffOuter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                // Override
                FlintlockBarrel asBarrel = (Server.objects[trackedID] as TrackedItemData).physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = (Server.objects[trackedID] as TrackedItemData).physicalItem.physicalItem as FlintlockWeapon;
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
                    asFlintlockWeapon.RamRod.m_curBarrel = asBarrel;
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
            ServerSend.FlintlockWeaponBurnOffOuter(clientID, packet);
        }

        public static void FlintlockWeaponFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                // Override
                FlintlockBarrel asBarrel = (Server.objects[trackedID] as TrackedItemData).physicalItem.dataObject as FlintlockBarrel;
                FlintlockWeapon asFlintlockWeapon = (Server.objects[trackedID] as TrackedItemData).physicalItem.physicalItem as FlintlockWeapon;
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
                    asFlintlockWeapon.RamRod.m_curBarrel = asBarrel;
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
                asBarrel.Fire();
                --FireFlintlockWeaponPatch.fireSkip;
            }

            // Send to other clients
            ServerSend.FlintlockWeaponFire(clientID, packet);
        }

        public static void BreakActionWeaponFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                BreakActionWeapon asBAW = (Server.objects[trackedID] as TrackedItemData).physicalItem.physicalItem as BreakActionWeapon;
                FireArmRoundType prevRoundType = asBAW.Barrels[barrelIndex].Chamber.RoundType;
                asBAW.Barrels[barrelIndex].Chamber.RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asBAW.Barrels[barrelIndex].Chamber.SetRound(roundClass, asBAW.Barrels[barrelIndex].Chamber.transform.position, asBAW.Barrels[barrelIndex].Chamber.transform.rotation);
                --ChamberPatch.chamberSkip;
                asBAW.Barrels[barrelIndex].Chamber.RoundType = prevRoundType;
                // NOTE: Only barrel index is used in the Fire method, other arguments are presumably reserved for later,
                // TODO: Future: will need to add support later if implemented
                ++ProjectileFirePatch.skipBlast;
                asBAW.Fire(barrelIndex, false, 0);
                --ProjectileFirePatch.skipBlast;
            }

            // Send to other clients
            ServerSend.BreakActionWeaponFire(clientID, packet);
        }

        public static void DerringerFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                Derringer asDerringer = Server.objects[trackedID].physical.physical as Derringer;
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

            // Send to other clients
            ServerSend.DerringerFire(clientID, packet);
        }

        public static void RevolvingShotgunFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                RevolvingShotgun asRS = Server.objects[trackedID].physical.physical as RevolvingShotgun;
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

            // Send to other clients
            ServerSend.RevolvingShotgunFire(clientID, packet);
        }

        public static void RevolverFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                Revolver asRevolver = Server.objects[trackedID].physical.physical as Revolver;
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
                ++ProjectileFirePatch.skipBlast;
                asRevolver.Fire();
                --ProjectileFirePatch.skipBlast;
            }

            // Send to other clients
            ServerSend.RevolverFire(clientID, packet);
        }

        public static void SingleActionRevolverFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                SingleActionRevolver asRevolver = Server.objects[trackedID].physical.physical as SingleActionRevolver;
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

            // Send to other clients
            ServerSend.SingleActionRevolverFire(clientID, packet);
        }

        public static void GrappleGunFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                GrappleGun asGG = Server.objects[trackedID].physical.physical as GrappleGun;
                asGG.m_curChamber = curChamber;
                FireArmRoundType prevRoundType = asGG.Chambers[curChamber].RoundType;
                asGG.Chambers[curChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].SetRound(roundClass, asGG.Chambers[curChamber].transform.position, asGG.Chambers[curChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asGG.Chambers[curChamber].RoundType = prevRoundType;
                asGG.Fire();
            }

            // Send to other clients
            ServerSend.GrappleGunFire(clientID, packet);
        }

        public static void HCBReleaseSled(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                float cookedAmount = packet.ReadFloat();
                FireHCBPatch.position = packet.ReadVector3();
                FireHCBPatch.direction = packet.ReadVector3();
                FireHCBPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireHCBPatch.releaseSledSkip;
                HCB asHCB = Server.objects[trackedID].physical.physical as HCB;
                if(cookedAmount == -1)
                {
                    ++ChamberPatch.chamberSkip;
                    asHCB.Chamber.SetRound(null);
                    --ChamberPatch.chamberSkip;
                }
                else
                {
                    asHCB.m_cookedAmount = cookedAmount;
                    if (!asHCB.Chamber.IsFull)
                    {
                        ++ChamberPatch.chamberSkip;
                        asHCB.Chamber.SetRound(FireArmRoundClass.FMJ, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                }
                asHCB.ReleaseSled();
                --FireHCBPatch.releaseSledSkip;
            }

            // Send to other clients
            ServerSend.HCBReleaseSled(clientID, packet);
        }

        public static void StingerLauncherFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                FireStingerLauncherPatch.targetPos = packet.ReadVector3();
                FireStingerLauncherPatch.position = packet.ReadVector3();
                FireStingerLauncherPatch.rotation = packet.ReadQuaternion();
                FireStingerLauncherPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++FireStingerLauncherPatch.skip;
                StingerLauncher asStingerLauncher = Server.objects[trackedID].physical.physical as StingerLauncher;
                asStingerLauncher.m_hasMissile = true;
                ++ProjectileFirePatch.skipBlast;
                asStingerLauncher.Fire();
                --ProjectileFirePatch.skipBlast;
                --FireStingerLauncherPatch.skip;
            }

            // Send to other clients
            ServerSend.StingerLauncherFire(clientID, packet);
        }

        public static void LeverActionFirearmFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                LeverActionFirearm asLAF = (Server.objects[trackedID].physical as TrackedItem).dataObject as LeverActionFirearm;
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

            // Send to other clients
            ServerSend.LeverActionFirearmFire(clientID, packet);
        }

        public static void SosigWeaponFire(int clientID, Packet packet)
        {
#if DEBUG
            if(packet == null)
            {
                Mod.LogError("SosigWeaponFire packet null");
                return;
            }
#endif

            int trackedID = packet.ReadInt();
            float recoilMult = packet.ReadFloat();

#if DEBUG
            if (Server.objects[trackedID] == null)
            {
                Mod.LogError("SosigWeaponFire object at "+trackedID+" null");
                return;
            }
#endif

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
#if DEBUG
                if (Server.objects[trackedID].physical as TrackedItem == null)
                {
                    Mod.LogError("SosigWeaponFire physical not trackedItem");
                    return;
                }
                if ((Server.objects[trackedID].physical as TrackedItem).dataObject as SosigWeaponPlayerInterface == null)
                {
                    Mod.LogError("SosigWeaponFire physical item not SosigWeaponPlayerInterface");
                    return;
                }
                if ((Server.objects[trackedID].physical as TrackedItem).dataObject as SosigWeaponPlayerInterface == null)
                {
                    Mod.LogError("SosigWeaponFire physical item not SosigWeaponPlayerInterface");
                    return;
                }
                if (((Server.objects[trackedID].physical as TrackedItem).dataObject as SosigWeaponPlayerInterface).W == null)
                {
                    Mod.LogError("SosigWeaponFire SosigWeaponPlayerInterface has no weapon");
                    return;
                }
#endif

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
                SosigWeaponPlayerInterface asInterface = (Server.objects[trackedID].physical as TrackedItem).dataObject as SosigWeaponPlayerInterface;
                if(asInterface.W.m_shotsLeft <= 0)
                {
                    asInterface.W.m_shotsLeft = 1;
                }
                asInterface.W.MechaState = SosigWeapon.SosigWeaponMechaState.ReadyToFire;
                (Server.objects[trackedID].physical as TrackedItem).sosigWeaponfireFunc(recoilMult);
            }

            // Send to other clients
            ServerSend.SosigWeaponFire(clientID, packet);
        }

        public static void AttachableFirearmFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                (Server.objects[trackedID].physical as TrackedItem).attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                (Server.objects[trackedID].physical as TrackedItem).attachableFirearmFireFunc(firedFromInterface);
                --ProjectileFirePatch.skipBlast;
            }

            // Send to other clients
            ServerSend.AttachableFirearmFire(clientID, packet);
        }

        public static void IntegratedFirearmFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                (Server.objects[trackedID].physical as TrackedItem).attachableFirearmChamberRoundFunc(roundType, roundClass);
                ++ProjectileFirePatch.skipBlast;
                (Server.objects[trackedID].physical as TrackedItem).attachableFirearmFireFunc(false);
                --ProjectileFirePatch.skipBlast;
            }

            // Send to other clients
            ServerSend.IntegratedFirearmFire(clientID, packet);
        }

        public static void LAPD2019Fire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
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
                LAPD2019 asLAPD2019 = Server.objects[trackedID].physical.physical as LAPD2019;
                asLAPD2019.CurChamber = chamberIndex;
                FireArmRoundType prevRoundType = asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = roundType;
                ++ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].SetRound(roundClass, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.position, asLAPD2019.Chambers[asLAPD2019.CurChamber].transform.rotation);
                --ChamberPatch.chamberSkip;
                asLAPD2019.Chambers[asLAPD2019.CurChamber].RoundType = prevRoundType;
                ++ProjectileFirePatch.skipBlast;
                asLAPD2019.Fire();
                --ProjectileFirePatch.skipBlast;
            }

            // Send to other clients
            ServerSend.LAPD2019Fire(clientID, packet);
        }

        public static void LAPD2019LoadBattery(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null && Server.objects[batteryTrackedID].physical != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)Server.objects[trackedID].physical.physical).LoadBattery((LAPD2019Battery)Server.objects[batteryTrackedID].physical.physical);
                --LAPD2019ActionPatch.loadBatterySkip;
            }

            // Send to other clients
            ServerSend.LAPD2019LoadBattery(clientID, trackedID, batteryTrackedID);
        }

        public static void LAPD2019ExtractBattery(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)Server.objects[trackedID].physical.physical).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }

            // Send to other clients
            ServerSend.LAPD2019ExtractBattery(clientID, trackedID);
        }

        public static void SosigWeaponShatter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                ++SosigWeaponShatterPatch.skip;
                (Server.objects[trackedID].physical.physical as SosigWeaponPlayerInterface).W.Shatter();
                --SosigWeaponShatterPatch.skip;
            }

            // Send to other clients
            ServerSend.SosigWeaponShatter(clientID, trackedID);
        }

        public static void AutoMeaterFirearmFireShot(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            if (Server.objects[trackedID].physical != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                (Server.objects[trackedID].physical as TrackedAutoMeater).physicalAutoMeater.FireControl.Firearms[0].FireShot();
                --AutoMeaterFirearmFireShotPatch.skip;
            }

            // Send to other clients
            ServerSend.AutoMeaterFirearmFireShot(clientID, trackedID, angles);
        }

        public static void PlayerDamage(int clientID, Packet packet)
        {
            int ID = packet.ReadInt();
            float damageMult = packet.ReadFloat();
            bool head = packet.ReadBool();
            Damage damage = packet.ReadDamage();

            if (ID == 0)
            {
                GameManager.ProcessPlayerDamage(damageMult, head, damage);
            }
            else
            {
                ServerSend.PlayerDamage(ID, damageMult, head, damage);
            }
        }

        public static void UberShatterableShatter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedObjectData trackedObject = Server.objects[trackedID];
            if (trackedObject != null)
            {
                if(trackedObject.physical != null)
                {
                    trackedObject.physical.HandleShatter(null, packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat(), true, clientID, packet.ReadBytes(packet.ReadInt()));
                }
                //trackedItem.additionalData = new byte[30];
                //trackedItem.additionalData[0] = 1;
                //trackedItem.additionalData[1] = 1;
                //for (int i = 2, j = 0; i < 30; ++i, ++j)
                //{
                //    trackedItem.additionalData[i] = packet.readableBuffer[packet.readPos + j];
                //}

                //if (trackedItem.physical != null)
                //{
                //    ++UberShatterableShatterPatch.skip;
                //    (trackedItem.physicalItem.dataObject as UberShatterable).Shatter(packet.ReadVector3(), packet.ReadVector3(), packet.ReadFloat());
                //    --UberShatterableShatterPatch.skip;
                //}
                ServerSend.UberShatterableShatter(clientID, packet);
            }
        }

        public static void SosigPickUpItem(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.inventory[primaryHand ? 0 : 1] = itemTrackedID;

                if (trackedSosig.physicalSosig != null)
                {
                    if (Server.objects[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (Server.objects[itemTrackedID].physical == null)
                    {
                        (Server.objects[itemTrackedID] as TrackedItemData).toPutInSosigInventory = new int[] { sosigTrackedID, primaryHand ? 0 : 1 };
                    }
                    else
                    {
                        ++SosigPickUpPatch.skip;
                        if (primaryHand)
                        {
                            trackedSosig.physicalSosig.physicalSosig.Hand_Primary.PickUp(Server.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        }
                        else
                        {
                            trackedSosig.physicalSosig.physicalSosig.Hand_Secondary.PickUp(Server.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        }
                        --SosigPickUpPatch.skip;
                    }
                }
            }

            ServerSend.SosigPickUpItem(sosigTrackedID, itemTrackedID, primaryHand, clientID);
        }

        public static void SosigPlaceItemIn(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = itemTrackedID;

                if (trackedSosig.physicalSosig != null)
                {
                    if (Server.objects[itemTrackedID] == null)
                    {
                        Mod.LogError("SosigPickUpItem: item at " + itemTrackedID + " is missing item data!");
                    }
                    else if (Server.objects[itemTrackedID].physical == null)
                    {
                        (Server.objects[itemTrackedID] as TrackedItemData).toPutInSosigInventory = new int[] { sosigTrackedID, slotIndex + 2 };
                    }
                    else
                    {
                        ++SosigPlaceObjectInPatch.skip;
                        trackedSosig.physicalSosig.physicalSosig.Inventory.Slots[slotIndex].PlaceObjectIn(Server.objects[itemTrackedID].physical.GetComponent<SosigWeapon>());
                        --SosigPlaceObjectInPatch.skip;
                    }
                }
            }

            ServerSend.SosigPlaceItemIn(sosigTrackedID, slotIndex, itemTrackedID, clientID);
        }

        public static void SosigDropSlot(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.inventory[slotIndex + 2] = -1;

                if (trackedSosig.physicalSosig != null)
                {
                    ++SosigSlotDetachPatch.skip;
                    trackedSosig.physicalSosig.physicalSosig.Inventory.Slots[slotIndex].DetachHeldObject();
                    --SosigSlotDetachPatch.skip;
                }
            }

            ServerSend.SosigDropSlot(sosigTrackedID, slotIndex, clientID);
        }

        public static void SosigHandDrop(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
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

            ServerSend.SosigHandDrop(sosigTrackedID, primaryHand, clientID);
        }

        public static void SosigConfigure(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            SosigConfigTemplate config = packet.ReadSosigConfig();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                trackedSosig.configTemplate = config;

                if (trackedSosig.physicalSosig != null)
                {
                    SosigConfigurePatch.skipConfigure = true;
                    trackedSosig.physicalSosig.physicalSosig.Configure(config);
                }
            }

            ServerSend.SosigConfigure(sosigTrackedID, config, clientID);
        }

        public static void SosigLinkRegisterWearable(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if (trackedSosig.physicalSosig != null)
                    {
                        foreach (SosigLink link in trackedSosig.physicalSosig.physicalSosig.Links)
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

                if (trackedSosig.physicalSosig != null)
                {
                    AnvilManager.Run(trackedSosig.EquipWearable(linkIndex, wearableID, true));
                }
            }

            ServerSend.SosigLinkRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigLinkDeRegisterWearable(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            if (sosigTrackedID != -1)
            {
                TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
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

            ServerSend.SosigLinkDeRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigSetIFF(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
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

            ServerSend.SosigSetIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigSetOriginalIFF(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
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

            ServerSend.SosigSetOriginalIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigLinkDamage(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
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
                else
                {
                    ServerSend.SosigLinkDamage(trackedSosig, linkIndex, damage);
                }
            }
        }

        public static void AutoMeaterDamage(int clientID, Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedAutoMeaterData trackedAutoMeater = Server.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
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
                    ServerSend.AutoMeaterDamage(trackedAutoMeater, damage);
                }
            }
        }

        public static void AutoMeaterHitZoneDamage(int clientID, Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            byte type = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedAutoMeaterData trackedAutoMeater = Server.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
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
                    ServerSend.AutoMeaterHitZoneDamage(trackedAutoMeater, type, damage);
                }
            }
        }

        public static void EncryptionDamage(int clientID, Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedEncryptionData trackedEncryption = Server.objects[encryptionTrackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller == 0)
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
                    ServerSend.EncryptionDamage(trackedEncryption, damage);
                }
            }
        }

        public static void AutoMeaterDamageData(int clientID, Packet packet)
        {
            // TODO: Future: if ever there is data we need to pass back from a auto meater damage call
        }

        public static void SosigWearableDamage(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
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
                    ServerSend.SosigWearableDamage(trackedSosig, linkIndex, wearableIndex, damage);
                }
            }
        }

        public static void SosigDamageData(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.controller != 0 && trackedSosig.physicalSosig != null)
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

            packet.readPos = 0;
            ServerSend.SosigLinkDamageData(packet);
        }

        public static void EncryptionDamageData(int clientID, Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Server.objects[encryptionTrackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller != 0 && trackedEncryption.physicalEncryption != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.m_numHitsLeft = packet.ReadInt();
                }
            }

            packet.readPos = 0;
            ServerSend.EncryptionDamageData(packet);
        }

        public static void AutoMeaterHitZoneDamageData(int clientID, Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            TrackedAutoMeaterData trackedAutoMeater = Server.objects[autoMeaterTrackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller != 0 && trackedAutoMeater.physicalAutoMeater != null)
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
            ServerSend.AutoMeaterHitZoneDamageData(packet);
        }

        public static void SosigLinkExplodes(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalSosig != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }

            ServerSend.SosigLinkExplodes(packet, clientID);
        }

        public static void SosigDies(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalSosig != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigPatch.sosigDiesSkip;
                    trackedSosig.physicalSosig.physicalSosig.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigPatch.sosigDiesSkip;
                }
            }

            ServerSend.SosigDies(packet, clientID);
        }

        public static void SosigClear(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalSosig != null)
                {
                    ++SosigPatch.sosigClearSkip;
                    trackedSosig.physicalSosig.physicalSosig.ClearSosig();
                    --SosigPatch.sosigClearSkip;
                }
            }

            ServerSend.SosigClear(sosigTrackedID, clientID);
        }

        public static void SosigSetBodyState(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalSosig != null)
                {
                    ++SosigPatch.sosigSetBodyStateSkip;
                    trackedSosig.physicalSosig.physicalSosig.SetBodyState(bodyState);
                    --SosigPatch.sosigSetBodyStateSkip;
                }
            }

            ServerSend.SosigSetBodyState(sosigTrackedID, bodyState, clientID);
        }

        public static void PlaySosigFootStepSound(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (Server.objects[sosigTrackedID] != null && Server.objects[sosigTrackedID].physical != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = (Server.objects[sosigTrackedID].physical as TrackedSosig).physicalSosig.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }

            ServerSend.PlaySosigFootStepSound(packet, clientID);
        }

        public static void SosigSpeakState(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
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

            ServerSend.SosigSpeakState(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigSetCurrentOrder(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
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

                ServerSend.SosigSetCurrentOrder(trackedSosig, currentOrder, clientID);
            }
        }

        public static void SosigVaporize(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigPatch.sosigVaporizeSkip;
                trackedSosig.physicalSosig.physicalSosig.Vaporize(trackedSosig.physicalSosig.physicalSosig.DamageFX_Vaporize, iff);
                --SosigPatch.sosigVaporizeSkip;
            }

            ServerSend.SosigVaporize(sosigTrackedID, iff, clientID);
        }

        public static void SosigRequestHitDecal(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                ++SosigPatch.sosigRequestHitDecalSkip;
                trackedSosig.physicalSosig.physicalSosig.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalSosig.physicalSosig.Links[linkIndex]);
                --SosigPatch.sosigRequestHitDecalSkip;
            }

            ServerSend.SosigRequestHitDecal(packet, clientID);
        }

        public static void SosigLinkBreak(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }

            ServerSend.SosigLinkBreak(sosigTrackedID, linkIndex, isStart, damClass, clientID);
        }

        public static void SosigLinkSever(int clientID, Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            TrackedSosigData trackedSosig = Server.objects[sosigTrackedID] as TrackedSosigData;
            if (trackedSosig != null && trackedSosig.physicalSosig != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                trackedSosig.physicalSosig.physicalSosig.Links[linkIndex].SeverJoint((Damage.DamageClass)damClass, isPullApart);
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }

            ServerSend.SosigLinkSever(sosigTrackedID, linkIndex, damClass, isPullApart, clientID);
        }
        
        public static void UpToDateObjects(int clientID, Packet packet)
        {
            // Reconstruct passed trackedObjects from packet
            bool instantiate = packet.ReadBool();
            int trackedID = packet.ReadInt();
            TrackedObjectData actualTrackedObject = Server.objects[trackedID];

            if(actualTrackedObject == null)
            {
                Mod.LogWarning("Server received up to date object at "+trackedID+" but we do not have data.");
            }
            else
            {
                actualTrackedObject.UpdateFromPacket(packet, true);

                // Although we only request up to date objects from our scene/instance, it might have changed since we made the request
                // So here we check it again
                if (instantiate && actualTrackedObject.physical == null && !actualTrackedObject.awaitingInstantiation &&
                    actualTrackedObject.scene.Equals(GameManager.scene) && actualTrackedObject.instance == GameManager.instance &&
                    actualTrackedObject.IsIdentifiable())
                {
                    actualTrackedObject.awaitingInstantiation = true;
                    AnvilManager.Run(actualTrackedObject.Instantiate());
                }
            }
        }

        public static void DoneLoadingScene(int clientID, Packet packet)
        {
            if(Server.loadingClientsWaitingFrom.TryGetValue(clientID, out List<int> otherClients))
            {
                // We were waiting for this client to finish loading to send relevant items
                // Check if we are still waiting on up to date items for this client
                bool stillWaiting = false;
                foreach(int otherCLientID in otherClients)
                {
                    if(Server.clientsWaitingUpDate.TryGetValue(otherCLientID, out List<int> clientIDs))
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
                    Server.clients[clientID].SendRelevantTrackedObjects();
                }

                Server.loadingClientsWaitingFrom.Remove(clientID);
            }
        }

        public static void DoneSendingUpToDateObjects(int clientID, Packet packet)
        {
            int forClient = packet.ReadInt();

            // If clients were waiting for this client to finish sending up to date objects
            if (Server.clientsWaitingUpDate.TryGetValue(clientID, out List<int> waitingClients))
            {
                if (forClient == 0)
                {
                    waitingClients.Remove(forClient);
                    if (waitingClients.Count == 0)
                    {
                        Server.clientsWaitingUpDate.Remove(clientID);
                    }

                    if (Server.loadingClientsWaitingFrom.TryGetValue(forClient, out List<int> waitingFrom))
                    {
                        waitingFrom.Remove(clientID);
                        if (waitingFrom.Count == 0)
                        {
                            // When requesting up to date objects, the server will instantiate directly upon receiving them, unlike a client, below
                            // In the server's case we then want to instantiate all missing items directly
                            Server.loadingClientsWaitingFrom.Remove(forClient);// Items

                            // This is necessary for the case in which server sent request before some items it was returning a tracked ID for for the relevant client
                            if (GameManager.objectsByInstanceByScene.TryGetValue(GameManager.scene, out Dictionary<int, List<int>> objectInstances) &&
                                objectInstances.TryGetValue(GameManager.instance, out List<int> objects))
                            {
                                for (int i = 0; i < objects.Count; ++i)
                                {
                                    TrackedObjectData trackedObjectData = Server.objects[objects[i]];
                                    if (trackedObjectData != null && trackedObjectData.physical == null && !trackedObjectData.awaitingInstantiation)
                                    {
                                        trackedObjectData.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedObjectData.Instantiate());
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If the relevant client is no longer loading or wasn't to begin with
                    Server.clients[forClient].SendRelevantTrackedObjects(clientID);

                    waitingClients.Remove(forClient);
                    if (waitingClients.Count == 0)
                    {
                        Server.clientsWaitingUpDate.Remove(clientID);
                    }

                    if (Server.loadingClientsWaitingFrom.TryGetValue(forClient, out List<int> waitingFrom))
                    {
                        waitingFrom.Remove(clientID);
                        if (waitingFrom.Count == 0)
                        {
                            // This means this client is no longer waiting for any more up to date objects, send them the insurance relevant objects
                            Server.loadingClientsWaitingFrom.Remove(forClient);
                            Server.clients[forClient].SendRelevantTrackedObjects();
                        }
                    }
                }
            }
        }

        public static void AddTNHCurrentlyPlaying(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            Mod.LogInfo("ServerHandle: Received AddTNHCurrentlyPlaying for instance: "+instance+" to add "+clientID, false);
            if (GameManager.TNHInstances == null || !GameManager.TNHInstances.ContainsKey(instance))
            {
                Mod.LogError("ServerHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                GameManager.TNHInstances[instance].AddCurrentlyPlaying(true, clientID, true);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance currentInstance))
            {
                currentInstance.RemoveCurrentlyPlaying(true, clientID, true);
            }
        }

        public static void SetTNHProgression(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].progressionTypeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.progressionSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Progression(i);
                GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)i;
                --TNH_UIManagerPatch.progressionSkip;
            }

            ServerSend.SetTNHProgression(i, instance, clientID);
        }

        public static void SetTNHEquipment(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].equipmentModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.equipmentSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_EquipmentMode(i);
                GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)i;
                --TNH_UIManagerPatch.equipmentSkip;
            }

            ServerSend.SetTNHEquipment(i, instance, clientID);
        }

        public static void SetTNHHealthMode(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].healthModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthModeSkip;
                Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMode(i);
                GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)i;
                --TNH_UIManagerPatch.healthModeSkip;
            }

            ServerSend.SetTNHHealthMode(i, instance, clientID);
        }

        public static void SetTNHTargetMode(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].targetModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.targetSkip;
                Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_TargetMode(i);
                GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)i;
                --TNH_UIManagerPatch.targetSkip;
            }

            ServerSend.SetTNHTargetMode(i, instance, clientID);
        }

        public static void SetTNHAIDifficulty(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].AIDifficultyModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.AIDifficultySkip;
                Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIDifficulty(i);
                GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)i;
                --TNH_UIManagerPatch.AIDifficultySkip;
            }

            ServerSend.SetTNHAIDifficulty(i, instance, clientID);
        }

        public static void SetTNHRadarMode(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].radarModeModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.radarSkip;
                Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIRadarMode(i);
                GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)i;
                --TNH_UIManagerPatch.radarSkip;
            }

            ServerSend.SetTNHRadarMode(i, instance, clientID);
        }

        public static void SetTNHItemSpawnerMode(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].itemSpawnerMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.itemSpawnerSkip;
                Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_ItemSpawner(i);
                GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)i;
                --TNH_UIManagerPatch.itemSpawnerSkip;
            }

            ServerSend.SetTNHItemSpawnerMode(i, instance, clientID);
        }

        public static void SetTNHBackpackMode(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].backpackMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.backpackSkip;
                Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Backpack(i);
                GM.TNHOptions.BackpackMode = (TNH_BackpackMode)i;
                --TNH_UIManagerPatch.backpackSkip;
            }

            ServerSend.SetTNHBackpackMode(i, instance, clientID);
        }

        public static void SetTNHHealthMult(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].healthMult = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthMultSkip;
                Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMult(i);
                GM.TNHOptions.HealthMult = (TNH_HealthMult)i;
                --TNH_UIManagerPatch.healthMultSkip;
            }

            ServerSend.SetTNHHealthMult(i, instance, clientID);
        }

        public static void SetTNHSosigGunReload(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].sosiggunShakeReloading = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.sosigGunReloadSkip;
                Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_SosiggunShakeReloading(i);
                GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)i;
                --TNH_UIManagerPatch.sosigGunReloadSkip;
            }

            ServerSend.SetTNHSosigGunReload(i, instance, clientID);
        }

        public static void SetTNHSeed(int clientID, Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            GameManager.TNHInstances[instance].TNHSeed = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.seedSkip;
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i + 1);
                Mod.currentTNHUIManager.SetOBS_RunSeed(i);
                GM.TNHOptions.TNHSeed = i;
                --TNH_UIManagerPatch.seedSkip;
            }

            ServerSend.SetTNHSeed(i, instance, clientID);
        }

        public static void SetTNHLevelID(int clientID, Packet packet)
        {
            string levelID = packet.ReadString();
            int instance = packet.ReadInt();
            
            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
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
                        Mod.LogError("Missing TNH level: " + levelID + "! Make sure you have it installed.");
                    }
                }
            }

            ServerSend.SetTNHLevelID(levelID, instance, clientID);
        }

        public static void SetTNHController(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int newController = packet.ReadInt();

            Mod.LogInfo("Server received order to set TNH instance: " + instance + " controller to: " + newController, false);
            GameManager.TNHInstances[instance].controller = newController;

            ServerSend.SetTNHController(instance, newController, clientID);
        }

        public static void TNHPlayerDied(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int ID = packet.ReadInt();

            // Process dead
            TNHInstance TNHinstance = GameManager.TNHInstances[instance];
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

            ServerSend.TNHPlayerDied(instance, ID, clientID);
        }

        public static void TNHAddTokens(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int amount = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance currentInstance))
            {
                currentInstance.tokenCount += amount;

                // Implies we are in-game in this instance 
                if(currentInstance.manager != null && !currentInstance.dead.Contains(GameManager.ID))
                {
                    ++TNH_ManagerPatch.addTokensSkip;
                    currentInstance.manager.AddTokens(amount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
            }

            ServerSend.TNHAddTokens(instance, amount, clientID);
        }

        public static void AutoMeaterSetState(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte state = packet.ReadByte();

            if (Server.objects[trackedID] != null && Server.objects[trackedID].physical != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                (Server.objects[trackedID].physical as TrackedAutoMeater).physicalAutoMeater.SetState((AutoMeater.AutoMeaterState)state);
                --AutoMeaterSetStatePatch.skip;
            }

            ServerSend.AutoMeaterSetState(trackedID, state, clientID);
        }

        public static void AutoMeaterSetBladesActive(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            TrackedAutoMeaterData trackedAutoMeater = Server.objects[trackedID] as TrackedAutoMeaterData;
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

            ServerSend.AutoMeaterSetBladesActive(trackedID, active, clientID);
        }

        public static void AutoMeaterFirearmFireAtWill(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int firearmIndex = packet.ReadInt();
            bool fireAtWill = packet.ReadBool();
            float dist = packet.ReadFloat();

            TrackedAutoMeaterData trackedAutoMeater = Server.objects[trackedID] as TrackedAutoMeaterData;
            if (trackedAutoMeater != null && trackedAutoMeater.physicalAutoMeater != null)
            {
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalAutoMeater.physicalAutoMeater.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }

            ServerSend.AutoMeaterFirearmFireAtWill(trackedID, firearmIndex, fireAtWill, dist, clientID);
        }

        public static void TNHSosigKill(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                TrackedSosigData trackedSosig = Server.objects[trackedID] as TrackedSosigData;
                if (trackedSosig != null && trackedSosig.physicalSosig != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.currentTNHInstance.manager.OnSosigKill(trackedSosig.physicalSosig.physicalSosig);
                    --TNH_ManagerPatch.sosigKillSkip;
                }
            }

            ServerSend.TNHSosigKill(instance, trackedID, clientID);
        }

        public static void TNHHoldPointSystemNode(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int levelIndex = packet.ReadInt();
            int holdPointIndex = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdPointIndex;
                actualInstance.level = levelIndex;

                if(actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.SetLevel(levelIndex);
                }
            }

            ServerSend.TNHHoldPointSystemNode(instance, levelIndex, holdPointIndex, clientID);
        }

        public static void TNHHoldBeginChallenge(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            bool fromController = packet.ReadBool();
            Mod.LogInfo("TNHHoldBeginChallenge server handle from "+clientID+" which is controller?: "+fromController+", for instance: "+instance, false);
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

                // Pass it on
                Mod.LogInfo("\tRelaying to all", false);
                ServerSend.TNHHoldBeginChallenge(instance, true, true, clientID);
            }
            else if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                if(actualInstance.controller == GameManager.ID)
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

                    // Relay will be done by BeginHoldChallenge patch
                }
                else // We are not controller
                {
                    // Relay to controller
                    Mod.LogInfo("\tRelaying to controller: "+ actualInstance.controller, false);
                    ServerSend.TNHHoldBeginChallenge(instance, false, false, actualInstance.controller);
                }
            }
        }

        public static void TNHHoldPointRaiseBarriers(int clientID, Packet packet)
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
                        TNH_DestructibleBarrierPoint point = TNHInstance.manager.m_curHoldPoint.BarrierPoints[TNHInstance.raisedBarriers[i]];
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

            ServerSend.TNHHoldPointRaiseBarriers(clientID, packet);
        }

        public static void ShatterableCrateSetHoldingHealth(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Server.objects[trackedID] != null)
            {
                (Server.objects[trackedID] as TrackedItemData).additionalData[3] = 1;

                if (Server.objects[trackedID].physical != null)
                {
                    ++TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                    Server.objects[trackedID].physical.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingHealthPatch.skip;
                }
            }

            ServerSend.ShatterableCrateSetHoldingHealth(trackedID, clientID);
        }

        public static void ShatterableCrateSetHoldingToken(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Server.objects[trackedID] != null)
            {
                (Server.objects[trackedID] as TrackedItemData).additionalData[4] = 1;

                if (Server.objects[trackedID].physical != null)
                {
                    ++TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                    Server.objects[trackedID].physical.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(GM.TNH_Manager);
                    --TNH_ShatterableCrateSetHoldingTokenPatch.skip;
                }
            }

            ServerSend.ShatterableCrateSetHoldingToken(trackedID, clientID);
        }

        public static void ShatterableCrateDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (Server.objects[trackedID] != null)
            {
                if (Server.objects[trackedID].controller == GameManager.ID)
                {
                    if (Server.objects[trackedID].physical != null)
                    {
                        ++TNH_ShatterableCrateDamagePatch.skip;
                        Server.objects[trackedID].physical.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
                        --TNH_ShatterableCrateDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.ShatterableCrateDamage(trackedID, packet.ReadDamage());
                }
            }
        }

        public static void ShatterableCrateDestroy(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage d = packet.ReadDamage();

            if (Server.objects[trackedID] != null && Server.objects[trackedID].physical != null)
            {
                TNH_ShatterableCrate crateScript = Server.objects[trackedID].physical.GetComponentInChildren<TNH_ShatterableCrate>();
                if (crateScript == null)
                {
                    Mod.LogError("Received order to destroy shatterable crate for which we have physObj but it has not crate script!");
                }
                else
                {
                    ++TNH_ShatterableCrateDestroyPatch.skip;
                    crateScript.Destroy(d);
                    --TNH_ShatterableCrateDestroyPatch.skip;
                }
            }

            ServerSend.ShatterableCrateDestroy(trackedID, d, clientID);
        }

        public static void TNHSetLevel(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            int level = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.level = level;

                if(actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.level = level;
                    actualInstance.manager.m_level = level;
                    actualInstance.manager.SetLevel(level);
                }
            }

            ServerSend.TNHSetLevel(instance, level, clientID);
        }

        public static void TNHSetPhaseTake(int clientID, Packet packet)
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

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdIndex;
                actualInstance.phase = TNH_Phase.Take;
                actualInstance.activeSupplyPointIndices = activeIndices;

                if(!init && actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.SetPhase_Take();
                }
            }

            ServerSend.TNHSetPhaseTake(instance, holdIndex, activeIndices, init, clientID);
        }

        public static void TNHSetPhaseHold(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.phase = TNH_Phase.Hold;

                if(actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.SetPhase_Hold();
                }
            }

            ServerSend.TNHSetPhaseHold(instance, clientID);
        }

        public static void TNHHoldCompletePhase(int clientID, Packet packet)
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

            ServerSend.TNHHoldCompletePhase(instance, clientID);
        }

        public static void TNHHoldPointFailOut(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.FailOut();
                }
            }

            ServerSend.TNHHoldPointFailOut(instance, clientID);
        }

        public static void TNHHoldPointBeginPhase(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = true;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.BeginPhase();
                }
            }

            ServerSend.TNHHoldPointBeginPhase(instance, clientID);
        }

        public static void TNHHoldPointCompleteHold(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                TNHInstance.holdOngoing = false;
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    Mod.currentTNHInstance.manager.m_curHoldPoint.CompleteHold();
                }
            }

            ServerSend.TNHHoldPointCompleteHold(instance, clientID);
        }

        public static void TNHSetPhaseComplete(int clientID, Packet packet)
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

            ServerSend.TNHSetPhaseComplete(instance, clientID);
        }

        public static void TNHSetPhase(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            short p = packet.ReadShort();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                // Update data
                actualInstance.phase = (TNH_Phase)p;

                // Update state
                if(actualInstance.manager != null && actualInstance.manager.m_hasInit)
                {
                    actualInstance.manager.Phase = (TNH_Phase)p;
                }
            }

            ServerSend.TNHSetPhase(instance, p, clientID);
        }

        public static void EncryptionRespawnSubTarg(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physical != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].SetActive(true);
                    ++trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft;
                }
            }

            ServerSend.EncryptionRespawnSubTarg(trackedID, index, clientID);
        }

        public static void EncryptionRespawnSubTargGeo(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargGeosActive[index] = true;
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physical != null)
                {
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].SetActive(true);
                    trackedEncryption.physicalEncryption.physicalEncryption.SubTargGeo[index].gameObject.SetActive(true);
                    ++trackedEncryption.physicalEncryption.physicalEncryption.m_numSubTargsLeft;
                }
            }

            ServerSend.EncryptionRespawnSubTarg(trackedID, index, clientID);
        }

        public static void EncryptionSpawnGrowth(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = true;

                if (trackedEncryption.physical != null)
                {
                    Vector3 forward = point - trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.position;

                    ++EncryptionSpawnGrowthPatch.skip;
                    trackedEncryption.physicalEncryption.physicalEncryption.SpawnGrowth(index, point);
                    --EncryptionSpawnGrowthPatch.skip;

                    trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.localScale = new Vector3(0.2f, 0.2f, forward.magnitude * trackedEncryption.physicalEncryption.physicalEncryption.TendrilFloats[index]);
                }
            }

            ServerSend.EncryptionSpawnGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionInit(int clientID, Packet packet)
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

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (pointCount > 0)
                {
                    for (int i = 0; i < indexCount; ++i)
                    {
                        trackedEncryption.subTargsActive[indices[i]] = true;
                        trackedEncryption.subTargPos[indices[i]] = points[i];
                    }

                    if (trackedEncryption.physical != null)
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

                    if (trackedEncryption.physical != null)
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

            ServerSend.EncryptionInit(clientID, trackedID, indices, points, initialPos, numHitsLeft);
        }

        public static void EncryptionResetGrowth(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (Server.objects[trackedID].physical != null)
                {
                    Vector3 forward = point - trackedEncryption.physicalEncryption.physicalEncryption.Tendrils[index].transform.position;

                    ++EncryptionResetGrowthPatch.skip;
                    trackedEncryption.physicalEncryption.physicalEncryption.ResetGrowth(index, point);
                    --EncryptionResetGrowthPatch.skip;
                }
            }

            ServerSend.EncryptionResetGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionDisableSubtarg(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                trackedEncryption.subTargsActive[index] = false;
                if (trackedEncryption.subTargGeosActive != null && trackedEncryption.subTargGeosActive.Length > index)
                {
                    trackedEncryption.subTargGeosActive[index] = false;
                }

                if (trackedEncryption.physical != null)
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

            ServerSend.EncryptionDisableSubtarg(trackedID, index, clientID);
        }

        public static void EncryptionSubDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedEncryptionData trackedEncryption = Server.objects[trackedID] as TrackedEncryptionData;
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == 0)
                {
                    if (trackedEncryption.physicalEncryption != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalEncryption.physicalEncryption.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.EncryptionSubDamage(trackedEncryption, index, damage);
                }
            }
        }

        public static void SosigWeaponDamage(int clientID, Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData trackedItem = Server.objects[sosigWeaponTrackedID] as TrackedItemData;
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
                else
                {
                    ServerSend.SosigWeaponDamage(trackedItem, damage);
                }
            }
        }

        public static void RemoteMissileDamage(int clientID, Packet packet)
        {
            int RMLTrackedID = packet.ReadInt();

            TrackedItemData trackedItem = Server.objects[RMLTrackedID] as TrackedItemData;
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
                else
                {
                    ServerSend.RemoteMissileDamage(trackedItem, packet);
                }
            }
        }

        public static void StingerMissileDamage(int clientID, Packet packet)
        {
            int SLTrackedID = packet.ReadInt();

            TrackedItemData trackedItem = Server.objects[SLTrackedID] as TrackedItemData;
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
                else
                {
                    ServerSend.StingerMissileDamage(trackedItem, packet);
                }
            }
        }

        public static void TNHHoldPointBeginAnalyzing(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
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
                if(TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    // Note that since we received this order, we are not the controller of the instance
                    // Consequently, the warpins will not be spawned as in a normal call to BeginAnalyzing
                    // We have to spawn them ourselves with the given data
                    GM.TNH_Manager.m_curHoldPoint.BeginAnalyzing();

                    for (int i = 0; i < dataCount; i += 2)
                    {
                        GM.TNH_Manager.m_curHoldPoint.m_warpInTargets.Add(UnityEngine.Object.Instantiate<GameObject>(GM.TNH_Manager.m_curHoldPoint.M.Prefab_TargetWarpingIn, TNHInstance.warpInData[i], Quaternion.Euler(TNHInstance.warpInData[i+1])));
                    }
                }
            }

            ServerSend.TNHHoldPointBeginAnalyzing(clientID, packet);
        }

        public static void TNHHoldIdentifyEncryption(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            if(GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                // Set instance data
                TNHInstance.holdState = TNH_HoldPoint.HoldState.Hacking;
                TNHInstance.tickDownToFailure = 120f;

                // If this is our TNH game, actually begin analyzing
                if (TNHInstance.manager != null && TNHInstance.manager.m_hasInit)
                {
                    TNH_HoldPoint curHoldPoint = GM.TNH_Manager.m_curHoldPoint;

                    curHoldPoint.IdentifyEncryption();
                }
            }

            ServerSend.TNHHoldIdentifyEncryption(clientID, instance);
        }

        public static void SosigPriorityIFFChart(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int chart = packet.ReadInt();

            TrackedSosigData trackedSosig = Server.objects[trackedID] as TrackedSosigData;
            if (trackedSosig != null)
            {
                // Update local
                trackedSosig.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                if (trackedSosig.physicalSosig != null)
                {
                    trackedSosig.physicalSosig.physicalSosig.Priority.IFFChart = SosigTargetPrioritySystemPatch.IntToBoolArr(chart);
                }
            }

            ServerSend.SosigPriorityIFFChart(clientID, trackedID, chart);
        }

        public static void RemoteMissileDetonate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local;
                if (Server.objects[trackedID].physical != null)
                {
                    RemoteMissile remoteMissile = (Server.objects[trackedID].physical.physical as RemoteMissileLauncher).m_missile;
                    if(remoteMissile != null)
                    {
                        RemoteMissileDetonatePatch.overriden = true;
                        remoteMissile.transform.position = packet.ReadVector3();
                        remoteMissile.Detonante();
                    }
                }
            }

            ServerSend.RemoteMissileDetonate(clientID, packet);
        }

        public static void StingerMissileExplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local;
                if (Server.objects[trackedID].physical != null)
                {
                    StingerMissile missile = (Server.objects[trackedID].physical as TrackedItem).stingerMissile;
                    if(missile != null)
                    {
                        StingerMissileExplodePatch.overriden = true;
                        missile.transform.position = packet.ReadVector3();
                        missile.Explode();
                    }
                }
            }

            ServerSend.StingerMissileExplode(clientID, packet);
        }

        public static void PinnedGrenadeExplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (trackedID == -1 || trackedID >= Server.objects.Length)
            {
                Mod.LogError("Server received order to explode pinned grenade with tracked ID: "+trackedID+" but items array is not large enough to hold this ID!");
            }
            else
            {
                if (Server.objects[trackedID] != null)
                {
                    // Update local;
                    if (Server.objects[trackedID].physical != null)
                    {
                        PinnedGrenade grenade = Server.objects[trackedID].physical.physical as PinnedGrenade;
                        if (grenade != null)
                        {
                            PinnedGrenadePatch.ExplodePinnedGrenade(grenade, packet.ReadVector3());
                        }
                    }
                }
            }

            ServerSend.PinnedGrenadeExplode(clientID, packet);
        }

        public static void PinnedGrenadePullPin(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (trackedID == -1 || trackedID >= Server.objects.Length)
            {
                Mod.LogError("Server received order to pull pin on pinned grenade with tracked ID: "+trackedID+" but items array is not large enough to hold this ID!");
            }
            else
            {
                if (Server.objects[trackedID] != null)
                {
                    if (Server.objects[trackedID].physical != null)
                    {
                        TrackedItemData trackedItem = Server.objects[trackedID] as TrackedItemData;
                        PinnedGrenade grenade = trackedItem.physical.physical as PinnedGrenade;
                        if (grenade != null)
                        {
                            for(int i=0; i< grenade.m_rings.Count; ++i)
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

                            // If we control and is in spawn lock, want to duplicate and switch in QBS
                            if (trackedItem.controller == GameManager.ID &&
                                trackedItem.physicalItem.physicalItem.m_isSpawnLock) // Implies is in QBS
                            {
                                // Keep ref to QBS
                                FVRQuickBeltSlot slot = trackedItem.physicalItem.physicalItem.QuickbeltSlot;

                                // Detach original with now pulled pin from QBS
                                trackedItem.physicalItem.physicalItem.ClearQuickbeltState();

                                // Spawn replacement
                                GameObject replacement = trackedItem.physicalItem.physicalItem.DuplicateFromSpawnLock(null);
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

            ServerSend.PinnedGrenadePullPin(trackedID, clientID);
        }

        public static void FVRGrenadeExplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local;
                if (Server.objects[trackedID].physical != null)
                {
                    FVRGrenade grenade = Server.objects[trackedID].physical.physical as FVRGrenade;
                    if(grenade != null)
                    {
                        FVRGrenadePatch.ExplodeGrenade(grenade, packet.ReadVector3());
                    }
                }
            }

            ServerSend.FVRGrenadeExplode(clientID, packet);
        }

        public static void ClientDisconnect(int clientID, Packet packet)
        {
            if (Server.clients[clientID].connected && // If a client is connected at that index, AND
                Server.clients[clientID].tcp.openTime < packet.ReadLong()) // If it is not a new client we have connected since the client has disconnected
            {
                Server.clients[clientID].Disconnect(2);
            }
        }

        public static void BangSnapSplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local
                if (Server.objects[trackedID].physical != null)
                {
                    BangSnap bangSnap = Server.objects[trackedID].physical.physical as BangSnap;
                    if (bangSnap != null)
                    {
                        bangSnap.transform.position = packet.ReadVector3();
                        ++BangSnapPatch.skip;
                        bangSnap.Splode();
                        --BangSnapPatch.skip;
                    }
                }
            }

            ServerSend.BangSnapSplode(clientID, packet);
        }

        public static void C4Detonate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local
                if (Server.objects[trackedID].physical != null)
                {
                    C4 c4 = Server.objects[trackedID].physical.physical as C4;
                    if (c4 != null)
                    {
                        c4.transform.position = packet.ReadVector3();
                        ++C4DetonatePatch.skip;
                        c4.Detonate();
                        --C4DetonatePatch.skip;
                    }
                }
            }

            ServerSend.C4Detonate(clientID, packet);
        }

        public static void ClaymoreMineDetonate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local
                if (Server.objects[trackedID].physical != null)
                {
                    ClaymoreMine cm = Server.objects[trackedID].physical.physical as ClaymoreMine;
                    if (cm != null)
                    {
                        cm.transform.position = packet.ReadVector3();
                        ++ClaymoreMineDetonatePatch.skip;
                        cm.Detonate();
                        --ClaymoreMineDetonatePatch.skip;
                    }
                }
            }

            ServerSend.ClaymoreMineDetonate(clientID, packet);
        }

        public static void SLAMDetonate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            if (Server.objects[trackedID] != null)
            {
                // Update local
                if (Server.objects[trackedID].physical != null)
                {
                    SLAM slam = Server.objects[trackedID].physical.physical as SLAM;
                    if (slam != null)
                    {
                        slam.transform.position = packet.ReadVector3();
                        ++SLAMDetonatePatch.skip;
                        slam.Detonate();
                        --SLAMDetonatePatch.skip;
                    }
                }
            }

            ServerSend.SLAMDetonate(clientID, packet);
        }

        public static void SpectatorHost(int clientID, Packet packet)
        {
            bool spectatorHost = packet.ReadBool();

            if (spectatorHost) 
            {
                if (!GameManager.spectatorHosts.Contains(clientID))
                {
                    GameManager.spectatorHosts.Add(clientID);
                    Server.availableSpectatorHosts.Add(clientID);
                }
            }
            else
            {
                GameManager.spectatorHosts.Remove(clientID);
                Server.availableSpectatorHosts.Remove(clientID);

                if(Server.spectatorHostControllers.TryGetValue(clientID, out int controller))
                {
                    Server.spectatorHostControllers.Remove(clientID);
                    Server.spectatorHostByController.Remove(controller);

                    ServerSend.GiveUpSpectatorHost(controller);
                }
            }

            GameManager.UpdatePlayerHidden(GameManager.players[clientID]);

            GameManager.OnSpectatorHostsChangedInvoke();

            ServerSend.SpectatorHost(clientID, spectatorHost);
        }

        public static void ResetTNH(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance) && actualInstance.controller == clientID)
            {
                actualInstance.Reset();

                if(actualInstance.manager != null)
                {
                    actualInstance.ResetManager();
                }

                ServerSend.ResetTNH(instance);
            }
        }

        public static void ReviveTNHPlayer(int clientID, Packet packet)
        {
            int ID = packet.ReadInt();
            int instance = packet.ReadInt();

            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance actualInstance))
            {
                actualInstance.RevivePlayer(ID, true);

                ServerSend.ReviveTNHPlayer(ID, instance, clientID);
            }
        }

        public static void PlayerColor(int clientID, Packet packet)
        {
            int ID = packet.ReadInt();
            int index = packet.ReadInt();

            GameManager.SetPlayerColor(ID, index, true, clientID);
        }

        public static void RequestTNHInitialization(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();
            Mod.LogInfo("TNH Initializion requested by " + clientID + " for " + instance);
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                if (TNHInstance.initializer == -1)
                {
                    Mod.LogInfo("\tNo initializer yet, granting init");
                    TNHInstance.initializer = clientID;
                    TNHInstance.initializationRequested = true; // We are waiting for init from this player

                    ServerSend.TNHInitializer(instance, clientID, true);
                }
                // else, already have an initializer, ignore
            }
        }

        public static void TNHInitializer(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            Mod.LogInfo("Received TNH Initializion by " + clientID + " for " + instance);
            if (GameManager.TNHInstances.TryGetValue(instance, out TNHInstance TNHInstance))
            {
                if(TNHInstance.initializer == clientID)
                {
                    Mod.LogInfo("\tThey were initializer, relaying");
                    TNHInstance.initializationRequested = false;
                    ServerSend.TNHInitializer(instance, clientID);

                    if(Mod.currentTNHInstance == TNHInstance && Mod.waitingForTNHGameStart)
                    {
                        Mod.currentTNHSceneLoader.LoadMG();
                    }
                }
                else
                {
                    Mod.LogError("Server received signal that "+clientID+" init TNH "+instance+" but they aren't the initializer!");
                }
            }
        }

        public static void FuseIgnite(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData as TrackedItemData;
            if(itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalItem is FVRFusedThrowable)
            {
                ++FusePatch.igniteSkip;
                (itemData.physicalItem.physicalItem as FVRFusedThrowable).Fuse.Ignite(0);
                --FusePatch.igniteSkip;
            }

            ServerSend.FuseIgnite(trackedID, clientID);
        }

        public static void FuseBoom(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
            if(itemData != null && itemData.physicalItem != null && itemData.physicalItem.physicalItem is FVRFusedThrowable)
            {
                (itemData.physicalItem.physicalItem as FVRFusedThrowable).Fuse.Boom();
            }

            ServerSend.FuseBoom(trackedID, clientID);
        }

        public static void MolotovShatter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool ignited = packet.ReadBool();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
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

            ServerSend.MolotovShatter(trackedID, ignited, clientID);
        }

        public static void MolotovDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
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
                    ServerSend.MolotovDamage(itemData, damage);
                }
            }
        }

        public static void MagazineAddRound(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++MagazinePatch.addRoundSkip;
                        (itemData.physicalItem.physicalItem as FVRFireArmMagazine).AddRound(roundClass, true, true);
                        --MagazinePatch.addRoundSkip;
                    }
                }
                else
                {
                    ServerSend.MagazineAddRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void ClipAddRound(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++ClipPatch.addRoundSkip;
                        (itemData.physicalItem.physicalItem as FVRFireArmClip).AddRound(roundClass, true, true);
                        --ClipPatch.addRoundSkip;
                    }
                }
                else
                {
                    ServerSend.ClipAddRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void SpeedloaderChamberLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        ++SpeedloaderChamberPatch.loadSkip;
                        (itemData.physicalItem.physicalItem as Speedloader).Chambers[chamberIndex].Load(roundClass, true);
                        --SpeedloaderChamberPatch.loadSkip;
                    }
                }
                else
                {
                    ServerSend.SpeedloaderChamberLoad(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void RemoteGunChamber(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            FireArmRoundType roundType = (FireArmRoundType)packet.ReadShort();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData; 
            if (itemData != null)
            {
                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        FVRFireArmRound round = AM.GetRoundSelfPrefab(roundType, roundClass).GetGameObject().GetComponent<FVRFireArmRound>();
                        ++RemoteGunPatch.chamberSkip;
                        (itemData.physicalItem.physicalItem as RemoteGun).ChamberCartridge(round);
                        --RemoteGunPatch.chamberSkip;
                    }
                }
                else
                {
                    ServerSend.RemoteGunChamber(itemData.trackedID, roundClass, roundType, clientID);
                }
            }
        }

        public static void ChamberRound(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            FireArmRoundClass roundClass = (FireArmRoundClass)packet.ReadShort();
            int chamberIndex = packet.ReadByte();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
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
                    ServerSend.ChamberRound(itemData.trackedID, roundClass, clientID);
                }
            }
        }

        public static void MagazineLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();
            short slot = packet.ReadShort();

            TrackedItemData magItemData = Server.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Server.objects[FATrackedID] as TrackedItemData;
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
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
                else
                {
                    ServerSend.MagazineLoad(trackedID, FATrackedID, slot, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load mag " + trackedID + " into firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void MagazineLoadAttachable(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            TrackedItemData magItemData = Server.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Server.objects[FATrackedID] as TrackedItemData;
            if (magItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
                {
                    if (FAItemData.physicalItem != null && magItemData.physicalItem != null)
                    {
                        ++MagazinePatch.loadSkip;
                        (magItemData.physicalItem.physicalItem as FVRFireArmMagazine).Load(FAItemData.physicalItem.dataObject as AttachableFirearm);
                        --MagazinePatch.loadSkip;
                    }
                }
                else
                {
                    ServerSend.MagazineLoadAttachable(trackedID, FATrackedID, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load mag " + trackedID + " into attachable firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void ClipLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int FATrackedID = packet.ReadInt();

            TrackedItemData clipItemData = Server.objects[trackedID] as TrackedItemData;
            TrackedItemData FAItemData = Server.objects[FATrackedID] as TrackedItemData;
            if (clipItemData != null && FAItemData != null)
            {
                if (FAItemData.controller == 0)
                {
                    if (FAItemData.physicalItem != null && clipItemData.physicalItem != null)
                    {
                        ++ClipPatch.loadSkip;
                        (clipItemData.physicalItem.physicalItem as FVRFireArmClip).Load(FAItemData.physicalItem.physicalItem as FVRFireArm);
                        --ClipPatch.loadSkip;
                    }
                }
                else
                {
                    ServerSend.ClipLoad(trackedID, FATrackedID, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load clip " + trackedID + " into firearm " + FATrackedID + " but we are missing item data!");
            }
        }

        public static void RevolverCylinderLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
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
                        Revolver revolver = itemData.physicalItem.physicalItem as Revolver;
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
                    ServerSend.RevolverCylinderLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolver cylinder " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void RevolvingShotgunLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
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
                else
                {
                    ServerSend.RevolvingShotgunLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolving shotgun " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void GrappleGunLoad(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
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
                else
                {
                    ServerSend.GrappleGunLoad(trackedID, null, classes, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to load revolving shotgun " + trackedID + " with speedloader but we are missing item data!");
            }
        }

        public static void CarlGustafLatchSate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                byte type = packet.ReadByte();
                byte state = packet.ReadByte();

                if (itemData.controller == 0)
                {
                    if (itemData.physicalItem != null)
                    {
                        CarlGustafLatch latch = (itemData.physicalItem.physicalItem as CarlGustaf).TailLatch;
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
                else
                {
                    ServerSend.CarlGustafLatchSate(trackedID, (CarlGustafLatch.CGLatchType)type, (CarlGustafLatch.CGLatchState)state, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to set latch state of CG " + trackedID + " but we are missing item data!");
            }
        }

        public static void CarlGustafShellSlideSate(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData itemData = Server.objects[trackedID] as TrackedItemData;
            if (itemData != null)
            {
                byte state = packet.ReadByte();

                if (itemData.controller == 0)
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
                else
                {
                    ServerSend.CarlGustafShellSlideSate(trackedID, (CarlGustafShellInsertEject.ChamberSlideState)state, clientID);
                }
            }
            else
            {
                Mod.LogError("Server got order to set shell slide state of CG " + trackedID + " but we are missing item data!");
            }
        }

        public static void TNHHostStartHold(int clientID, Packet packet)
        {
            int instance = packet.ReadInt();

            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.controller == GameManager.ID)
            {
                if (Mod.currentTNHInstance.manager != null && !Mod.currentTNHInstance.holdOngoing)
                {
                    GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode.position, true);
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasActivated = true;
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasInitiatedHold = true;
                    Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                }
            }
            else
            {
                ServerSend.TNHHostStartHold(Mod.currentTNHInstance.instance, clientID);
            }
        }

        public static void GrappleAttached(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte[] data = packet.ReadBytes(packet.ReadShort());

            TrackedItemData trackedItem = Server.objects[trackedID] as TrackedItemData;
            if (trackedItem != null && trackedItem.controller != GameManager.ID)
            {
                trackedItem.additionalData = data;

                if (trackedItem.physical != null)
                {
                    GrappleThrowable asGrappleThrowable = trackedItem.physical.physical as GrappleThrowable;
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

                ServerSend.GrappleAttached(trackedID, data, clientID);
            }
        }

        public static void RegisterCustomPacketType(int clientID, Packet packet)
        {
            Server.RegisterCustomPacketType(packet.ReadString(), clientID);
        }

        public static void BreakableGlassDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedBreakableGlassData trackedBreakableGlass = Server.objects[trackedID] as TrackedBreakableGlassData;
            if (trackedBreakableGlass != null)
            {
                if (trackedBreakableGlass.controller == 0)
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
                    ServerSend.BreakableGlassDamage(packet, trackedBreakableGlass.controller);
                }
            }
        }

        public static void WindowShatterSound(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int mode = packet.ReadByte();

            TrackedBreakableGlassData trackedBreakableGlass = Server.objects[trackedID] as TrackedBreakableGlassData;
            if (trackedBreakableGlass != null)
            {
                if(trackedBreakableGlass.physicalBreakableGlass != null)
                {
                    trackedBreakableGlass.physicalBreakableGlass.PlayerShatterAudio(mode);
                }

                ServerSend.WindowShatterSound(trackedID, mode, clientID);
            }
        }

        public static void RequestSpectatorHost(int clientID, Packet packet)
        {
            int host = -1;
            if (Server.availableSpectatorHosts.Count > 0)
            {
                host = Server.availableSpectatorHosts[Server.availableSpectatorHosts.Count - 1];
                Server.spectatorHostByController.Add(clientID, host);
                Server.spectatorHostControllers.Add(host, clientID);
                Server.availableSpectatorHosts.RemoveAt(Server.availableSpectatorHosts.Count - 1);

                if(host == 0)
                {
                    GameManager.spectatorHostControlledBy = clientID;
                }
            }

            ServerSend.SpectatorHostAssignment(host, clientID);
        }

        public static void UnassignSpectatorHost(int clientID, Packet packet)
        {
            if(Server.spectatorHostByController.TryGetValue(clientID, out int host))
            {
                Server.spectatorHostByController.Remove(clientID);
                Server.spectatorHostControllers.Remove(host);

                if (GameManager.spectatorHosts.Contains(host))
                {
                    Server.availableSpectatorHosts.Add(host);
                }

                if(host == GameManager.ID)
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
                else
                {
                    ServerSend.UnassignSpectatorHost(host);
                }
            }
        }

        public static void SpectatorHostOrderTNHHost(int clientID, Packet packet)
        {
            if (Server.spectatorHostByController.TryGetValue(clientID, out int host))
            {
                if (host == GameManager.ID)
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

                            ServerSend.TNHSpectatorHostReady(clientID, GameManager.instance);
                            Mod.spectatorHostWaitingForTNHSetup = false;
                        }
                        else if(GameManager.scene.Equals("MainMenu3"))
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
                else
                {
                    ServerSend.SpectatorHostOrderTNHHost(host, packet.ReadBool());
                }
            }
            else
            {
                ServerSend.GiveUpSpectatorHost(clientID);
            }
        }

        public static void TNHSpectatorHostReady(int clientID, Packet packet)
        {
            if (Server.spectatorHostControllers.TryGetValue(clientID, out int controller))
            {
                int instance = packet.ReadInt();

                if (controller == GameManager.ID)
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
                else
                {
                    ServerSend.TNHSpectatorHostReady(controller, instance);
                }
            }
        }

        public static void SpectatorHostStartTNH(int clientID, Packet packet)
        {
            int host = packet.ReadInt();

            if(Server.spectatorHostControllers.TryGetValue(host, out int controller))
            {
                if(host == GameManager.ID)
                {
                    if (GameManager.spectatorHost && !GameManager.sceneLoading && GameManager.scene.Equals("TakeAndHold_Lobby_2") &&
                        Mod.currentTNHInstance != null && Mod.currentTNHInstance.playerIDs.Count > 0 &&
                        Mod.currentTNHInstance.playerIDs[0] == GameManager.ID && Mod.currentTNHSceneLoader != null)
                    {
                        Mod.currentTNHSceneLoader.LoadMG();
                    }
                }
                else
                {
                    ServerSend.SpectatorHostStartTNH(host);
                }
            }
        }

        public static void ReassignSpectatorHost(int clientID, Packet packet)
        {
            List<int> debounce = new List<int>();
            int debounceCount = packet.ReadInt();
            for(int i = 0; i < debounceCount; ++i)
            {
                debounce.Add(packet.ReadInt());
            }

            GameManager.ReassignSpectatorHost(clientID, debounce);
        }

        public static void ReactiveSteelTargetDamage(int clientID, Packet packet)
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

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physicalItem != null)
                {
                    ReactiveSteelTarget rst = trackedItemData.physicalItem.getSecondary(index) as ReactiveSteelTarget;

                    if(rst != null)
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

                ServerSend.ReactiveSteelTargetDamage(packet, clientID);
            }
        }

        public static void MTUTest(int clientID, Packet packet)
        {
            Mod.LogWarning("Received MTU test packet from client");
        }

        public static void IDConfirm(int clientID, Packet packet)
        {
            int ID = packet.ReadInt();

            Mod.LogInfo("Server received IDConfirm for " + ID);

            if(Server.availableIndexBufferClients.TryGetValue(clientID, out List<int> IDs))
            {
                IDs.Remove(ID);

                if(IDs.Count == 0)
                {
                    Server.availableIndexBufferClients.Remove(clientID);
                }
            }

            if (Server.availableIndexBufferWaitingFor.TryGetValue(ID, out List<int> clients))
            {
                clients.Remove(clientID);

                if (clients.Count == 0)
                {
                    Server.availableIndexBufferWaitingFor.Remove(ID);
                    Server.availableObjectIndices.Add(ID);
                }
            }
        }

        public static void ObjectScene(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            TrackedObjectData trackedObjectData = (TrackedObjectData)Activator.CreateInstance(Mod.trackedObjectTypesByName[typeID], packet, typeID, trackedID);

            if (Server.objects[trackedID] != null)
            {
                Server.objects[trackedID].SetScene(trackedObjectData.scene, false);

                ServerSend.ObjectScene(trackedObjectData, clientID);
            }
            //else // Don't have object data yet, note that this is server though, meaning this should never be the case
        }

        public static void ObjectInstance(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            string typeID = packet.ReadString();
            TrackedObjectData trackedObjectData = (TrackedObjectData)Activator.CreateInstance(Mod.trackedObjectTypesByName[typeID], packet, typeID, trackedID);

            if (Server.objects[trackedID] != null)
            {
                Server.objects[trackedID].SetInstance(trackedObjectData.instance, false);

                ServerSend.ObjectInstance(trackedObjectData, clientID);
            }
            //else // Don't have object data yet, note that this is server though, meaning this should never be the case
        }

        public static void UpdateEncryptionDisplay(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int numHitsLeft = packet.ReadInt();

            TrackedEncryptionData trackedEncryptionData = Server.objects[trackedID] as TrackedEncryptionData;
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

                ServerSend.UpdateEncryptionDisplay(trackedID, numHitsLeft, clientID);
            }
        }

        public static void RoundDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if(trackedItemData.controller == GameManager.ID)
                {
                    if(trackedItemData.physical != null)
                    {
                        ++RoundPatch.splodeInDamage;
                        (trackedItemData.physicalItem.physicalItem as FVRFireArmRound).Damage(damage);
                        --RoundPatch.splodeInDamage;
                    }
                }
                else
                {
                    ServerSend.RoundDamage(trackedID, damage, trackedItemData.controller);
                }
            }
        }

        public static void RoundSplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                float velMultiplier = packet.ReadFloat();
                bool isRandomDir = packet.ReadBool();

                if (trackedItemData.physical != null)
                {
                    ++RoundPatch.splodeSkip;
                    (trackedItemData.physicalItem.physicalItem as FVRFireArmRound).Splode(velMultiplier, isRandomDir, false);
                    --RoundPatch.splodeSkip;
                }

                ServerSend.RoundSplode(trackedID, velMultiplier, isRandomDir, clientID);
            }
        }

        public static void SightFlipperState(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            bool large = packet.ReadBool();

            TrackedObjectData trackedObjectData = Server.objects[trackedID];
            if (trackedObjectData != null)
            {
                if (trackedObjectData.physical)
                {
                    AR15HandleSightFlipper[] flippers = trackedObjectData.physical.GetComponentsInChildren<AR15HandleSightFlipper>();
                    if(flippers.Length > index)
                    {
                        flippers[index].m_isLargeAperture = large;
                    }
                }

                ServerSend.SightFlipperState(trackedID, index, large, clientID);
            }
        }

        public static void SightRaiserState(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            AR15HandleSightRaiser.SightHeights height = (AR15HandleSightRaiser.SightHeights)packet.ReadByte();

            TrackedObjectData trackedObjectData = Server.objects[trackedID];
            if (trackedObjectData != null)
            {
                if (trackedObjectData.physical)
                {
                    AR15HandleSightRaiser[] raisers = trackedObjectData.physical.GetComponentsInChildren<AR15HandleSightRaiser>();
                    if(raisers.Length > index)
                    {
                        switch (height)
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

                ServerSend.SightRaiserState(trackedID, index, height, clientID);
            }
        }

        public static void GatlingGunFire(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 pos = packet.ReadVector3();
            Quaternion rot = packet.ReadQuaternion();
            Vector3 dir = packet.ReadVector3();

            TrackedObjectData trackedObjectData = Server.objects[trackedID];
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

                ServerSend.GatlingGunFire(trackedID, pos, rot, dir, clientID);
            }
        }

        public static void GasCuboidGout(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 pos = packet.ReadVector3();
            Vector3 norm = packet.ReadVector3();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
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

                ServerSend.GasCuboidGout(trackedID, pos, norm, clientID);
            }
        }

        public static void GasCuboidDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.controller == GameManager.ID)
                {
                    if(trackedItemData.physical != null)
                    {
                        ++GasCuboidDamagePatch.skip;
                        (trackedItemData.physicalItem.dataObject as Brut_GasCuboid).Damage(packet.ReadDamage());
                        --GasCuboidDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.GasCuboidDamage(packet, trackedItemData.controller);
                }
            }
        }

        public static void GasCuboidHandleDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.controller == GameManager.ID)
                {
                    if (trackedItemData.physical != null)
                    {
                        ++GasCuboidHandleDamagePatch.skip;
                        (trackedItemData.physicalItem.dataObject as Brut_GasCuboid).Handle.GetComponent<Brut_GasCuboidHandle>().Damage(packet.ReadDamage());
                        --GasCuboidHandleDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.GasCuboidHandleDamage(packet, trackedItemData.controller);
                }
            }
        }

        public static void GasCuboidDamageHandle(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                trackedItemData.additionalData[0] = 1;
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    asGC.m_isHandleBrokenOff = true;
                    asGC.Handle.SetActive(false);
                }

                ServerSend.GasCuboidDamageHandle(trackedID, clientID);
            }
        }

        public static void GasCuboidExplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 point = packet.ReadVector3();
            Vector3 dir = packet.ReadVector3();
            bool big = packet.ReadBool();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    ++GasCuboidPatch.explodeSkip;
                    asGC.Explode(point, dir, big);
                    --GasCuboidPatch.explodeSkip;
                }

                ServerSend.GasCuboidExplode(trackedID, point, dir, big, clientID);
            }
        }

        public static void GasCuboidShatter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 point = packet.ReadVector3();
            Vector3 dir = packet.ReadVector3();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                if (trackedItemData.physical != null)
                {
                    Brut_GasCuboid asGC = trackedItemData.physicalItem.dataObject as Brut_GasCuboid;
                    ++GasCuboidPatch.shatterSkip;
                    asGC.Shatter(point, dir);
                    --GasCuboidPatch.shatterSkip;
                }

                ServerSend.GasCuboidShatter(trackedID, point, dir, clientID);
            }
        }

        public static void FloaterDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedFloaterData trackedFloaterData = Server.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (trackedFloaterData.controller == GameManager.ID)
                {
                    if (trackedFloaterData.physical != null)
                    {
                        ++FloaterDamagePatch.skip;
                        trackedFloaterData.physicalFloater.physicalFloater.Damage(packet.ReadDamage());
                        --FloaterDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.FloaterDamage(packet, trackedFloaterData.controller);
                }
            }
        }

        public static void FloaterCoreDamage(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedFloaterData trackedFloaterData = Server.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (trackedFloaterData.controller == GameManager.ID)
                {
                    if (trackedFloaterData.physical != null)
                    {
                        ++FloaterCoreDamagePatch.skip;
                        trackedFloaterData.physicalFloater.GetComponentInChildren<Construct_Floater_Core>().Damage(packet.ReadDamage());
                        --FloaterCoreDamagePatch.skip;
                    }
                }
                else
                {
                    ServerSend.FloaterCoreDamage(packet, trackedFloaterData.controller);
                }
            }
        }

        public static void FloaterBeginExploding(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool fromController = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Server.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (fromController) // From controller, trigger explosion on our side
                {
                    FloaterPatch.beginExplodingOverride = true;
                    trackedFloaterData.physicalFloater.physicalFloater.BeginExploding();
                    FloaterPatch.beginExplodingOverride = false;

                    ServerSend.FloaterBeginExploding(trackedID, true, trackedFloaterData.controller);
                }
                else if(trackedFloaterData.controller == GameManager.ID) // We control, trigger explosion and send order to everyone else
                {
                    trackedFloaterData.physicalFloater.physicalFloater.BeginExploding();
                }
                else // Not from controller and we don't control, relay to controller
                {
                    ServerSend.FloaterBeginExploding(trackedID, false, trackedFloaterData.controller);
                }

            }
        }

        public static void FloaterBeginDefusing(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool fromController = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Server.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (fromController) // From controller, trigger explosion on our side
                {
                    FloaterPatch.beginExplodingOverride = true;
                    trackedFloaterData.physicalFloater.physicalFloater.BeginDefusing();
                    FloaterPatch.beginExplodingOverride = false;

                    ServerSend.FloaterBeginDefusing(trackedID, true, trackedFloaterData.controller);
                }
                else if(trackedFloaterData.controller == GameManager.ID) // We control, trigger explosion and send order to everyone else
                {
                    trackedFloaterData.physicalFloater.physicalFloater.BeginDefusing();
                }
                else // Not from controller and we don't control, relay to controller
                {
                    ServerSend.FloaterBeginDefusing(trackedID, false, trackedFloaterData.controller);
                }

            }
        }

        public static void FloaterExplode(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool defusing = packet.ReadBool();

            TrackedFloaterData trackedFloaterData = Server.objects[trackedID] as TrackedFloaterData;
            if (trackedFloaterData != null)
            {
                if (trackedFloaterData.physicalFloater != null)
                {
                    trackedFloaterData.physicalFloater.physicalFloater.isExplosionDefuse = defusing;

                    ++FloaterPatch.explodeSkip;
                    trackedFloaterData.physicalFloater.physicalFloater.BeginExploding();
                    --FloaterPatch.explodeSkip;
                }

                ServerSend.FloaterExplode(trackedID, defusing, clientID);
            }
        }

        public static void IrisShatter(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedIrisData trackedIrisData = Server.objects[trackedID] as TrackedIrisData;
            if (trackedIrisData != null)
            {
                if (trackedIrisData.physicalIris != null)
                {
                    byte index = packet.ReadByte();
                    Vector3 point = packet.ReadVector3();
                    Vector3 dir = packet.ReadVector3();
                    float intensity = packet.ReadFloat();

                    ++UberShatterableShatterPatch.skip;
                    trackedIrisData.physicalIris.physicalIris.Rings[index].Shatter(point, dir, intensity);
                    --UberShatterableShatterPatch.skip;
                }

                ServerSend.IrisShatter(packet, clientID);
            }
        }

        public static void IrisSetState(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            Construct_Iris.IrisState state = (Construct_Iris.IrisState)packet.ReadByte();

            TrackedIrisData trackedIrisData = Server.objects[trackedID] as TrackedIrisData;
            if (trackedIrisData != null)
            {
                trackedIrisData.state = state;

                if (trackedIrisData.physicalIris != null)
                {
                    ++IrisPatch.stateSkip;
                    trackedIrisData.physicalIris.physicalIris.SetState(state);
                    --IrisPatch.stateSkip;
                }

                ServerSend.IrisSetState(trackedID, state, clientID);
            }
        }

        public static void BrutBlockSystemStart(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool next = packet.ReadBool();

            TrackedBrutBlockSystemData trackedBrutBlockSystemData = Server.objects[trackedID] as TrackedBrutBlockSystemData;
            if (trackedBrutBlockSystemData != null)
            {
                if (trackedBrutBlockSystemData.physicalBrutBlockSystem != null)
                {
                    trackedBrutBlockSystemData.physicalBrutBlockSystem.physicalBrutBlockSystem.isNextBlock0 = next;

                    ++BrutBlockSystemPatch.startSkip;
                    trackedBrutBlockSystemData.physicalBrutBlockSystem.physicalBrutBlockSystem.TryToStartBlock();
                    --BrutBlockSystemPatch.startSkip;
                }

                ServerSend.BrutBlockSystemStart(trackedID, next, clientID);
            }
        }

        public static void BatchedPackets(int clientID, Packet packet)
        {
            while (packet.UnreadLength() > 0)
            {
                int length = packet.ReadInt();
                byte[] data = packet.ReadBytes(length);

                using (Packet childPacket = new Packet(data))
                {
                    int packetId = childPacket.ReadInt();
                    Server.packetHandlers[packetId](clientID, childPacket);
                }
            }
        }
    }
}
