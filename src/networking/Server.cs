using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using FistVR;
using H3MP.Patches;
using H3MP.Tracking;

namespace H3MP.Networking
{
    internal class Server
    {
        public static ushort port;
        public static ushort maxClientCount;
        public static Dictionary<int, ServerClient> clients = new Dictionary<int, ServerClient>();
        public delegate void PacketHandler(int clientID, Packet packet);
        public static PacketHandler[] packetHandlers;
        public static TrackedObjectData[] objects; // All tracked objects, regardless of whos control they are under
        public static List<int> availableObjectIndices;

        public static List<int> availableSpectatorHosts;

        public static Dictionary<int, List<int>> clientsWaitingUpDate = new Dictionary<int, List<int>>(); // Clients we requested up to date objects from, for which clients
        public static Dictionary<int, List<int>> loadingClientsWaitingFrom = new Dictionary<int, List<int>>(); // Clients currently loading, waiting for up to date objects from which clients

        public static TcpListener tcpListener;
        public static UdpClient udpListener;

        public static void Start(ushort _maxClientCount, ushort _port)
        {
            maxClientCount = _maxClientCount;
            port = _port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Mod.LogInfo($"Server started, listening on port: {port}", false);

            // Just connected, sync if current scene is syncable
            if (!GameManager.nonSynchronizedScenes.ContainsKey(GameManager.scene))
            {
                GameManager.SyncTrackedItems(true, true);
                GameManager.SyncTrackedSosigs(true, true);
                GameManager.SyncTrackedAutoMeaters(true, true);
                GameManager.SyncTrackedEncryptions(true, true);
            }
        }

        public static void Close()
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            Mod.LogInfo("Closing server.", false);

            ServerSend.ServerClosed();

            GameManager.TakeAllPhysicalControl(true);

            clients.Clear();
            items = null;
            availableItemIndices = null;
            sosigs = null;
            availableSosigIndices = null;
            autoMeaters = null;
            availableAutoMeaterIndices = null;
            encryptions = null;
            availableEncryptionIndices = null;

            tcpListener.Stop();
            tcpListener = null;
            udpListener.Close();
            udpListener = null;

            GameManager.Reset();
            Mod.Reset();
            SpecificClose();
        }

        // MOD: This will be called after disconnection to reset specific fields
        //      For example, here we deal with current TNH data
        //      If your mod has some H3MP dependent data that you want to get rid of when you close a server, do it here
        private static void SpecificClose()
        {
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
            Mod.currentlyPlayingTNH = false;
            Mod.currentTNHInstancePlayers = null;
            Mod.joinTNHInstances = null;
            if (Mod.TNHMenu != null)
            {
                GameObject.Destroy(Mod.TNHMenu);
                Mod.TNHMenu = null;
            }
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Mod.LogInfo($"Incoming connection from {client.Client.RemoteEndPoint}", false);

            for (int i = 1; i <= maxClientCount; ++i)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    clients[i].connected = true;
                    return;
                }
            }

            Mod.LogWarning($"{client.Client.RemoteEndPoint} failed to connect, server full");
        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
            if(tcpListener == null)
            {
                return;
            }

            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if(data.Length < 4)
                {
                    return;
                }

                using(Packet packet = new Packet(data))
                {
                    int clientID = packet.ReadInt();

                    if (clientID == 0)
                    {
                        return;
                    }

                    if (clients[clientID].udp.endPoint == null)
                    {
                        clients[clientID].udp.Connect(clientEndPoint);
                        return;
                    }

                    if (clients[clientID].udp.endPoint.ToString() == clientEndPoint.ToString())
                    {
                        clients[clientID].udp.HandleData(packet);
                    }
                }
            }
            catch(Exception ex)
            {
                Mod.LogInfo($"Error receiving UDP data: {ex}", false);
            }
        }

        public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if(clientEndPoint != null)
                {
                    udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch(Exception ex)
            {
                Mod.LogInfo($"Error sending UDP data to {clientEndPoint}: {ex}", false);
            }
        }

        public static void AddTrackedObject(TrackedObjectData trackedObject, int clientID)
        {
            // If this is a sceneInit object received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if(trackedObject.trackedID == -1 && trackedObject.controller != 0 && trackedObject.sceneInit && !clients[trackedObject.controller].player.firstInSceneInstance)
            {
                // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                // indicate this to the sending client so they can destroy their item
                trackedObject.trackedID = -2;
                ServerSend.TrackedObjectSpecific(trackedObject, trackedObject.controller);
                return;
            }

            // Adjust objects size to acommodate if necessary
            if (availableObjectIndices.Count == 0)
            {
                IncreaseObjectsSize();
            }

            // Add it to server global list
            trackedObject.trackedID = availableObjectIndices[availableObjectIndices.Count - 1];
            availableObjectIndices.RemoveAt(availableObjectIndices.Count - 1);

            objects[trackedObject.trackedID] = trackedObject;

            // Add to item tracking list
            if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> relevantInstances))
            {
                if (relevantInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                {
                    objectList.Add(trackedObject.trackedID);
                }
                else
                {
                    relevantInstances.Add(trackedObject.instance, new List<int>() { trackedObject.trackedID });
                }
            }
            else
            {
                Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                newInstances.Add(trackedObject.instance, new List<int>() { trackedObject.trackedID });
                GameManager.objectsByInstanceByScene.Add(trackedObject.scene, newInstances);
            }

            // Add to parent children if has parent and we are not initTracker (It isn't already in the list)
            if (trackedObject.parent != -1 && trackedObject.initTracker != GameManager.ID)
            {
                // Note that this should never be null, we should always receive the parent data before receiving the children's
                // TODO: Review: If this is actually true: is the hierarchy maintained when server sends relevant objects to a client when the client joins a scene/instance?
                TrackedObjectData parentData = objects[trackedObject.parent];

                if (parentData.children == null)
                {
                    parentData.children = new List<TrackedObjectData>();
                }

                trackedObject.childIndex = parentData.children.Count;
                parentData.children.Add(trackedObject);
            }

            // Instantiate item if it is in the current scene and not controlled by us
            if (clientID != 0 && trackedObject.IsIdentifiable())
            {
                // Here, we don't want to instantiate if this is a scene we are in the process of loading
                // This is due to the possibility of objects only being identifiable in certain contexts like TNH_ShatterableCrates needing a TNH_manager
                if (!trackedObject.awaitingInstantiation && trackedObject.scene.Equals(GameManager.scene) && trackedObject.instance == GameManager.instance)
                {
                    trackedObject.awaitingInstantiation = true;
                    AnvilManager.Run(trackedObject.Instantiate());
                }
            }

            // Send to all clients in same scene/instance, including controller because they need confirmation from server that this object was added and its trackedID
            List<int> toClients = new List<int>();
            if (clientID != 0)
            {
                // We explicitly include clientID in the list because the client might have changed scene/instance since, but they still need to get the data
                toClients.Add(clientID);
            }
            if (GameManager.playersByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(trackedObject.instance, out List<int> players))
            {
                for(int i=0; i < players.Count; ++i)
                {
                    if (players[i] != clientID)
                    {
                        toClients.Add(players[i]);
                    }
                }
            }
            ServerSend.TrackedObject(trackedObject, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedObject.controller != 0)
            {
                trackedObject.localTrackedID = -1;
            }
        }

        public static void AddTrackedItem(TrackedItemData trackedItem, int clientID)
        {
            // If this is a sceneInit item received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if(trackedItem.trackedID == -1 && trackedItem.controller != 0 && trackedItem.sceneInit && !clients[trackedItem.controller].player.firstInSceneInstance)
            {
                // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                // indicate this to the sending client so they can destroy their item
                trackedItem.trackedID = -2;
                ServerSend.TrackedItemSpecific(trackedItem, trackedItem.controller);
                return;
            }

            // Adjust items size to acommodate if necessary
            if (availableItemIndices.Count == 0)
            {
                IncreaseItemsSize();
            }

            // Add it to server global list
            trackedItem.trackedID = availableItemIndices[availableItemIndices.Count - 1];
            availableItemIndices.RemoveAt(availableItemIndices.Count - 1);

            items[trackedItem.trackedID] = trackedItem;

            // Add to item tracking list
            if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> relevantInstances))
            {
                if (relevantInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                {
                    itemList.Add(trackedItem.trackedID);
                }
                else
                {
                    relevantInstances.Add(trackedItem.instance, new List<int>() { trackedItem.trackedID });
                }
            }
            else
            {
                Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                newInstances.Add(trackedItem.instance, new List<int>() { trackedItem.trackedID });
                GameManager.itemsByInstanceByScene.Add(trackedItem.scene, newInstances);
            }

            // Add to parent children if has parent and we are not initTracker (It isn't already in the list)
            if (trackedItem.parent != -1 && trackedItem.initTracker != GameManager.ID)
            {
                // Note that this should never be null, we should always receive the parent data before receiving the children's
                // TODO: Review: If this is actually true: is the hierarchy maintained when server sends relevant items to a client when the client joins a scene/instance?
                TrackedItemData parentData = items[trackedItem.parent];

                if (parentData.children == null)
                {
                    parentData.children = new List<TrackedItemData>();
                }

                trackedItem.childIndex = parentData.children.Count;
                parentData.children.Add(trackedItem);
            }

            // Instantiate item if it is in the current scene and not controlled by us
            if (clientID != 0 && GameManager.IsItemIdentifiable(trackedItem))
            {
                // Here, we don't want to instantiate if this is a scene we are in the process of loading
                // This is due to the possibility of items only being identifiable in certain contexts like TNH_ShatterableCrates needing a TNH_manager
                if (!trackedItem.awaitingInstantiation && trackedItem.scene.Equals(GameManager.scene) && trackedItem.instance == GameManager.instance)
                {
                    trackedItem.awaitingInstantiation = true;
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }

            // Send to all clients in same scene/instance, including controller because they need confirmation from server that this object was added and its trackedID
            List<int> toClients = new List<int>();
            if (clientID != 0)
            {
                // We explicitly include clientID in the list because the client might have changed scene/instance since, but they still need to get the data
                toClients.Add(clientID);
            }
            if (GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(trackedItem.instance, out List<int> players))
            {
                for(int i=0; i < players.Count; ++i)
                {
                    if (players[i] != clientID)
                    {
                        toClients.Add(players[i]);
                    }
                }
            }
            ServerSend.TrackedItem(trackedItem, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedItem.controller != 0)
            {
                trackedItem.localTrackedID = -1;
            }
        }

        public static void AddTrackedSosig(TrackedSosigData trackedSosig, int clientID)
        {
            Mod.LogInfo("Received full sosig with trackedID: " + trackedSosig.trackedID+" from client "+clientID, false);
            if (trackedSosig.trackedID == -1)
            {
                Mod.LogInfo("\tNot tracked yet, tracking", false);
                // If this is a sceneInit sosig received from client that we haven't tracked yet
                // And if the controller is not the first player in scene/instance
                if (trackedSosig.controller != 0 && trackedSosig.sceneInit && !clients[trackedSosig.controller].player.firstInSceneInstance)
                {
                    // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                    // indicate this to the sending client so they can destroy their sosig
                    trackedSosig.trackedID = -2;
                    ServerSend.TrackedSosigSpecific(trackedSosig, trackedSosig.controller);
                    return;
                }

                // Adjust sosigs size to acommodate if necessary
                if (availableSosigIndices.Count == 0)
                {
                    IncreaseSosigsSize();
                }

                // Add it to server global list
                trackedSosig.trackedID = availableSosigIndices[availableSosigIndices.Count - 1];
                availableSosigIndices.RemoveAt(availableSosigIndices.Count - 1);

                sosigs[trackedSosig.trackedID] = trackedSosig;

                // Add to sosig tracking list
                if (GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                    {
                        sosigList.Add(trackedSosig.trackedID);
                    }
                    else
                    {
                        relevantInstances.Add(trackedSosig.instance, new List<int>() { trackedSosig.trackedID });
                    }
                }
                else
                {
                    Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                    newInstances.Add(trackedSosig.instance, new List<int>() { trackedSosig.trackedID });
                    GameManager.sosigsByInstanceByScene.Add(trackedSosig.scene, newInstances);
                }

                // Instantiate sosig if it is in the current scene and not controlled by us
                if (clientID != 0)
                {
                    // Don't use loading scene here. See AddTrackedItem why
                    if (!trackedSosig.awaitingInstantiation && trackedSosig.scene.Equals(GameManager.scene) && trackedSosig.instance == GameManager.instance)
                    {
                        trackedSosig.awaitingInstantiation = true;
                        AnvilManager.Run(trackedSosig.Instantiate());
                    }
                }

                // Send to all clients in same scene/instance, including controller because they need confirmation from server that this object was added and its trackedID
                List<int> toClients = new List<int>();
                if (clientID != 0)
                {
                    // We explicitly include clientID in the list because the client might have changed scene/instance since, but they still need to get the data
                    toClients.Add(clientID);
                }
                if (GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(trackedSosig.instance, out List<int> players))
                {
                    for (int i = 0; i < players.Count; ++i)
                    {
                        if (players[i] != clientID)
                        {
                            toClients.Add(players[i]);
                        }
                    }
                }
                ServerSend.TrackedSosig(trackedSosig, toClients);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedSosig.controller != 0)
                {
                    trackedSosig.localTrackedID = -1;
                }

                // Manage control for TNH
                if (GameManager.TNHInstances.TryGetValue(trackedSosig.instance, out TNHInstance TNHInstance) &&
                    TNHInstance.controller != trackedSosig.controller &&
                    ((TNHInstance.controller == 0 && trackedSosig.scene.Equals(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene)) ||
                    (TNHInstance.controller != 0 && trackedSosig.scene.Equals(clients[TNHInstance.controller].player.scene))))
                {
                    // Sosig is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                    if (TNHInstance.controller == 0)
                    {
                        // Us, take control
                        trackedSosig.localTrackedID = GameManager.sosigs.Count;
                        GameManager.sosigs.Add(trackedSosig);
                        if (trackedSosig.physicalObject == null)
                        {
                            if (!trackedSosig.awaitingInstantiation)
                            {
                                trackedSosig.awaitingInstantiation = true;
                                AnvilManager.Run(trackedSosig.Instantiate());
                            }
                        }
                        else
                        {
                            if (GM.CurrentAIManager != null)
                            {
                                GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosig.E);
                            }
                            trackedSosig.physicalObject.physicalSosig.CoreRB.isKinematic = false;
                        }

                    }
                    else if (trackedSosig.controller == 0)
                    {
                        // Was us, give up control
                        trackedSosig.RemoveFromLocal();
                        if (trackedSosig.physicalObject != null)
                        {
                            if (GM.CurrentAIManager != null)
                            {
                                GM.CurrentAIManager.DeRegisterAIEntity(trackedSosig.physicalObject.physicalSosig.E);
                            }
                            trackedSosig.physicalObject.physicalSosig.CoreRB.isKinematic = true;
                        }
                    }

                    trackedSosig.controller = TNHInstance.controller;
                    ServerSend.GiveSosigControl(trackedSosig.trackedID, TNHInstance.controller, null);
                }
            }
            else
            {
                Mod.LogInfo("\tInit update", false);
                // This is a sosig we already received full data for and assigned a tracked ID to but things may
                // have happened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                sosigs[trackedSosig.trackedID].Update(trackedSosig, true);

                List<int> toClients = new List<int>();
                if (GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(trackedSosig.instance, out List<int> players))
                {
                    for (int i = 0; i < players.Count; ++i)
                    {
                        if (players[i] != clientID)
                        {
                            toClients.Add(players[i]);
                        }
                    }
                }
                ServerSend.TrackedSosig(trackedSosig, toClients);
            }
        }

        public static void AddTrackedAutoMeater(TrackedAutoMeaterData trackedAutoMeater, int clientID)
        {
            // If this is a sceneInit autoMeater received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if (trackedAutoMeater.trackedID == -1 && trackedAutoMeater.controller != 0 && trackedAutoMeater.sceneInit && !clients[trackedAutoMeater.controller].player.firstInSceneInstance)
            {
                // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                // indicate this to the sending client so they can destroy their autoMeater
                trackedAutoMeater.trackedID = -2;
                ServerSend.TrackedAutoMeaterSpecific(trackedAutoMeater, trackedAutoMeater.controller);
                return;
            }

            // Adjust AutoMeaters size to acommodate if necessary
            if (availableAutoMeaterIndices.Count == 0)
            {
                IncreaseAutoMeatersSize();
            }

            // Add it to server global list
            trackedAutoMeater.trackedID = availableAutoMeaterIndices[availableAutoMeaterIndices.Count - 1];
            availableAutoMeaterIndices.RemoveAt(availableAutoMeaterIndices.Count - 1);

            autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;

            // Add to sosig tracking list
            if (GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> relevantInstances))
            {
                if (relevantInstances.TryGetValue(trackedAutoMeater.instance, out List<int> sosigList))
                {
                    sosigList.Add(trackedAutoMeater.trackedID);
                }
                else
                {
                    relevantInstances.Add(trackedAutoMeater.instance, new List<int>() { trackedAutoMeater.trackedID });
                }
            }
            else
            {
                Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                newInstances.Add(trackedAutoMeater.instance, new List<int>() { trackedAutoMeater.trackedID });
                GameManager.autoMeatersByInstanceByScene.Add(trackedAutoMeater.scene, newInstances);
            }

            // Instantiate AutoMeater if it is in the current scene and not controlled by us
            if (clientID != 0)
            {
                // Don't use loading scene here. See AddTrackedItem why
                if (!trackedAutoMeater.awaitingInstantiation && trackedAutoMeater.scene.Equals(GameManager.scene) && trackedAutoMeater.instance == GameManager.instance)
                {
                    trackedAutoMeater.awaitingInstantiation = true;
                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                }
            }

            // Send to all clients in same scene/instance, including controller because they need confirmation from server that this object was added and its trackedID
            List<int> toClients = new List<int>();
            if (clientID != 0)
            {
                // We explicitly include clientID in the list because the client might have changed scene/instance since, but they still need to get the data
                toClients.Add(clientID);
            }
            if (GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> instances) &&
                instances.TryGetValue(trackedAutoMeater.instance, out List<int> players))
            {
                for (int i = 0; i < players.Count; ++i)
                {
                    if (players[i] != clientID)
                    {
                        toClients.Add(players[i]);
                    }
                }
            }
            ServerSend.TrackedAutoMeater(trackedAutoMeater, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedAutoMeater.controller != 0)
            {
                trackedAutoMeater.localTrackedID = -1;
            }

            // Manage control for TNH
            if (GameManager.TNHInstances.TryGetValue(trackedAutoMeater.instance, out TNHInstance TNHInstance) &&
                TNHInstance.controller != trackedAutoMeater.controller &&
                ((TNHInstance.controller == 0 && trackedAutoMeater.scene.Equals(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene)) ||
                (TNHInstance.controller != 0 && trackedAutoMeater.scene.Equals(clients[TNHInstance.controller].player.scene))))
            {
                // Object is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                if (TNHInstance.controller == 0)
                {
                    // Us, take control
                    trackedAutoMeater.localTrackedID = GameManager.autoMeaters.Count;
                    GameManager.autoMeaters.Add(trackedAutoMeater);
                    if (trackedAutoMeater.physicalObject == null)
                    {
                        if (!trackedAutoMeater.awaitingInstantiation)
                        {
                            trackedAutoMeater.awaitingInstantiation = true;
                            AnvilManager.Run(trackedAutoMeater.Instantiate());
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
                else if (trackedAutoMeater.controller == 0)
                {
                    // Was us, give up control
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

                trackedAutoMeater.controller = TNHInstance.controller;
                ServerSend.GiveAutoMeaterControl(trackedAutoMeater.trackedID, TNHInstance.controller, null);
            }
        }

        public static void AddTrackedEncryption(TrackedEncryptionData trackedEncryption, int clientID)
        {
            if (trackedEncryption.trackedID == -1)
            {
                // If this is a sceneInit Encryption received from client that we haven't tracked yet
                // And if the controller is not the first player in scene/instance
                if (trackedEncryption.controller != 0 && trackedEncryption.sceneInit && !clients[trackedEncryption.controller].player.firstInSceneInstance)
                {
                    // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                    // indicate this to the sending client so they can destroy their Encryption
                    trackedEncryption.trackedID = -2;
                    ServerSend.TrackedEncryptionSpecific(trackedEncryption, trackedEncryption.controller);
                    return;
                }

                // Adjust Encryptions size to acommodate if necessary
                if (availableEncryptionIndices.Count == 0)
                {
                    IncreaseEncryptionsSize();
                }

                // Add it to server global list
                trackedEncryption.trackedID = availableEncryptionIndices[availableEncryptionIndices.Count - 1];
                availableEncryptionIndices.RemoveAt(availableEncryptionIndices.Count - 1);

                encryptions[trackedEncryption.trackedID] = trackedEncryption;

                // Add to encryption tracking list
                if (GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if (relevantInstances.TryGetValue(trackedEncryption.instance, out List<int> sosigList))
                    {
                        sosigList.Add(trackedEncryption.trackedID);
                    }
                    else
                    {
                        relevantInstances.Add(trackedEncryption.instance, new List<int>() { trackedEncryption.trackedID });
                    }
                }
                else
                {
                    Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                    newInstances.Add(trackedEncryption.instance, new List<int>() { trackedEncryption.trackedID });
                    GameManager.encryptionsByInstanceByScene.Add(trackedEncryption.scene, newInstances);
                }

                // Instantiate Encryption if it is in the current scene and not controlled by us
                if (clientID != 0)
                {
                    // Don't use loading scene here. See AddTrackedItem why
                    if (!trackedEncryption.awaitingInstantiation && trackedEncryption.scene.Equals(GameManager.scene) && trackedEncryption.instance == GameManager.instance)
                    {
                        trackedEncryption.awaitingInstantiation = true;
                        AnvilManager.Run(trackedEncryption.Instantiate());
                    }
                }

                // Send to all clients in same scene/instance, including controller because they need confirmation from server that this object was added and its trackedID
                List<int> toClients = new List<int>();
                if (clientID != 0)
                {
                    // We explicitly include clientID in the list because the client might have changed scene/instance since, but they still need to get the data
                    toClients.Add(clientID);
                }
                if (GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(trackedEncryption.instance, out List<int> players))
                {
                    for (int i = 0; i < players.Count; ++i)
                    {
                        if (players[i] != clientID)
                        {
                            toClients.Add(players[i]);
                        }
                    }
                }
                ServerSend.TrackedEncryption(trackedEncryption, toClients);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedEncryption.controller != 0)
                {
                    trackedEncryption.localTrackedID = -1;
                }

                // Manage control for TNH
                if (GameManager.TNHInstances.TryGetValue(trackedEncryption.instance, out TNHInstance TNHInstance) &&
                    TNHInstance.controller != trackedEncryption.controller &&
                    ((TNHInstance.controller == 0 && trackedEncryption.scene.Equals(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene)) ||
                    (TNHInstance.controller != 0 && trackedEncryption.scene.Equals(clients[TNHInstance.controller].player.scene))))
                {
                    // Object is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                    if (TNHInstance.controller == 0)
                    {
                        // Us, take control
                        trackedEncryption.localTrackedID = GameManager.encryptions.Count;
                        GameManager.encryptions.Add(trackedEncryption);
                        if (trackedEncryption.physicalObject == null)
                        {
                            if (!trackedEncryption.awaitingInstantiation)
                            {
                                trackedEncryption.awaitingInstantiation = true;
                                AnvilManager.Run(trackedEncryption.Instantiate());
                            }
                        }
                    }
                    else if (trackedEncryption.controller == 0)
                    {
                        // Was us, give up control
                        trackedEncryption.RemoveFromLocal();
                    }

                    trackedEncryption.controller = TNHInstance.controller;
                    ServerSend.GiveEncryptionControl(trackedEncryption.trackedID, TNHInstance.controller, null);
                }
            }
            else
            {
                // This is a encryption we already received full data for and assigned a tracked ID to but things may
                // have happened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                encryptions[trackedEncryption.trackedID].Update(trackedEncryption, true);

                List<int> toClients = new List<int>();
                if (GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(trackedEncryption.instance, out List<int> players))
                {
                    for (int i = 0; i < players.Count; ++i)
                    {
                        if (players[i] != clientID)
                        {
                            toClients.Add(players[i]);
                        }
                    }
                }
                ServerSend.TrackedEncryption(trackedEncryption, toClients);
            }
        }

        private static void IncreaseObjectsSize()
        {
            TrackedObjectData[] tempObjects = objects;
            objects = new TrackedObjectData[tempObjects.Length + 100];
            for(int i=0; i< tempObjects.Length;++i)
            {
                objects[i] = tempObjects[i];
            }
            for (int i = tempObjects.Length; i < objects.Length; ++i) 
            {
                availableObjectIndices.Add(i);
            }
        }

        private static void IncreaseItemsSize()
        {
            TrackedItemData[] tempItems = items;
            items = new TrackedItemData[tempItems.Length + 100];
            for(int i=0; i<tempItems.Length;++i)
            {
                items[i] = tempItems[i];
            }
            for(int i=tempItems.Length; i < items.Length; ++i)
            {
                availableItemIndices.Add(i);
            }
        }

        private static void IncreaseSosigsSize()
        {
            TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new TrackedSosigData[tempSosigs.Length + 100];
            for(int i=0; i< tempSosigs.Length;++i)
            {
                sosigs[i] = tempSosigs[i];
            }
            for(int i= tempSosigs.Length; i < sosigs.Length; ++i)
            {
                availableSosigIndices.Add(i);
            }
        }

        private static void IncreaseAutoMeatersSize()
        {
            TrackedAutoMeaterData[] tempAutoMeaters = autoMeaters;
            autoMeaters = new TrackedAutoMeaterData[tempAutoMeaters.Length + 100];
            for(int i=0; i< tempAutoMeaters.Length;++i)
            {
                autoMeaters[i] = tempAutoMeaters[i];
            }
            for(int i= tempAutoMeaters.Length; i < autoMeaters.Length; ++i)
            {
                availableAutoMeaterIndices.Add(i);
            }
        }

        private static void IncreaseEncryptionsSize()
        {
            TrackedEncryptionData[] tempEncryptions = encryptions;
            encryptions = new TrackedEncryptionData[tempEncryptions.Length + 100];
            for(int i=0; i< tempEncryptions.Length;++i)
            {
                encryptions[i] = tempEncryptions[i];
            }
            for(int i= tempEncryptions.Length; i < encryptions.Length; ++i)
            {
                availableEncryptionIndices.Add(i);
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= maxClientCount; ++i)
            {
                clients.Add(i, new ServerClient(i));
            }

            packetHandlers = new PacketHandler[]
            {
                null,
                ServerHandle.WelcomeReceived,
                ServerHandle.PlayerState,
                ServerHandle.PlayerScene,
                ServerHandle.AddNonSyncScene,
                ServerHandle.TrackedItems,
                ServerHandle.TrackedItem,
                ServerHandle.ShatterableCrateSetHoldingHealth,
                ServerHandle.GiveObjectControl,
                ServerHandle.DestroyItem,
                ServerHandle.ItemParent,
                ServerHandle.WeaponFire,
                ServerHandle.PlayerDamage,
                ServerHandle.TrackedSosig,
                ServerHandle.TrackedSosigs,
                ServerHandle.GiveSosigControl,
                ServerHandle.DestroySosig,
                ServerHandle.SosigPickUpItem,
                ServerHandle.SosigPlaceItemIn,
                ServerHandle.SosigDropSlot,
                ServerHandle.SosigHandDrop,
                ServerHandle.SosigConfigure,
                ServerHandle.SosigLinkRegisterWearable,
                ServerHandle.SosigLinkDeRegisterWearable,
                ServerHandle.SosigSetIFF,
                ServerHandle.SosigSetOriginalIFF,
                ServerHandle.SosigLinkDamage,
                ServerHandle.SosigDamageData,
                ServerHandle.SosigWearableDamage,
                ServerHandle.SosigLinkExplodes,
                ServerHandle.SosigDies,
                ServerHandle.SosigClear,
                ServerHandle.SosigSetBodyState,
                ServerHandle.PlaySosigFootStepSound,
                ServerHandle.SosigSpeakState,
                ServerHandle.SosigSetCurrentOrder,
                ServerHandle.SosigVaporize,
                ServerHandle.SosigRequestHitDecal,
                ServerHandle.SosigLinkBreak,
                ServerHandle.SosigLinkSever,
                ServerHandle.UpToDateItems,
                ServerHandle.UpToDateSosigs,
                ServerHandle.PlayerInstance,
                ServerHandle.AddTNHInstance,
                ServerHandle.AddTNHCurrentlyPlaying,
                ServerHandle.RemoveTNHCurrentlyPlaying,
                ServerHandle.SetTNHProgression,
                ServerHandle.SetTNHEquipment,
                ServerHandle.SetTNHHealthMode,
                ServerHandle.SetTNHTargetMode,
                ServerHandle.SetTNHAIDifficulty,
                ServerHandle.SetTNHRadarMode,
                ServerHandle.SetTNHItemSpawnerMode,
                ServerHandle.SetTNHBackpackMode,
                ServerHandle.SetTNHHealthMult,
                ServerHandle.SetTNHSosigGunReload,
                ServerHandle.SetTNHSeed,
                ServerHandle.SetTNHLevelID,
                ServerHandle.AddInstance,
                ServerHandle.SetTNHController,
                ServerHandle.SpectatorHost,
                ServerHandle.TNHPlayerDied,
                ServerHandle.TNHAddTokens,
                ServerHandle.TNHSetLevel,
                ServerHandle.TrackedAutoMeater,
                ServerHandle.TrackedAutoMeaters,
                ServerHandle.DestroyAutoMeater,
                ServerHandle.GiveAutoMeaterControl,
                ServerHandle.UpToDateAutoMeaters,
                ServerHandle.AutoMeaterSetState,
                ServerHandle.AutoMeaterSetBladesActive,
                ServerHandle.AutoMeaterDamage,
                ServerHandle.AutoMeaterDamageData,
                ServerHandle.AutoMeaterFirearmFireShot,
                ServerHandle.AutoMeaterFirearmFireAtWill,
                ServerHandle.AutoMeaterHitZoneDamage,
                ServerHandle.AutoMeaterHitZoneDamageData,
                ServerHandle.TNHSosigKill,
                ServerHandle.TNHHoldPointSystemNode,
                ServerHandle.TNHHoldBeginChallenge,
                ServerHandle.ShatterableCrateDamage,
                ServerHandle.TNHSetPhaseTake,
                ServerHandle.TNHHoldCompletePhase,
                ServerHandle.TNHHoldPointFailOut,
                ServerHandle.TNHSetPhaseComplete,
                ServerHandle.TNHSetPhase,
                ServerHandle.TrackedEncryptions,
                ServerHandle.TrackedEncryption,
                ServerHandle.GiveEncryptionControl,
                ServerHandle.DestroyEncryption,
                ServerHandle.EncryptionDamage,
                ServerHandle.EncryptionDamageData,
                ServerHandle.EncryptionRespawnSubTarg,
                ServerHandle.EncryptionSpawnGrowth,
                ServerHandle.EncryptionInit,
                ServerHandle.EncryptionResetGrowth,
                ServerHandle.EncryptionDisableSubtarg,
                ServerHandle.EncryptionSubDamage,
                ServerHandle.ShatterableCrateDestroy,
                ServerHandle.UpToDateEncryptions,
                ServerHandle.DoneLoadingScene,
                ServerHandle.DoneSendingUpToDateObjects,
                ServerHandle.SosigWeaponFire,
                ServerHandle.SosigWeaponShatter,
                ServerHandle.SosigWeaponDamage,
                ServerHandle.LAPD2019Fire,
                ServerHandle.LAPD2019LoadBattery,
                ServerHandle.LAPD2019ExtractBattery,
                ServerHandle.MinigunFire,
                ServerHandle.AttachableFirearmFire,
                ServerHandle.BreakActionWeaponFire,
                ServerHandle.PlayerIFF,
                ServerHandle.UberShatterableShatter,
                ServerHandle.TNHHoldPointBeginAnalyzing,
                ServerHandle.TNHHoldPointRaiseBarriers,
                ServerHandle.TNHHoldIdentifyEncryption,
                ServerHandle.TNHHoldPointBeginPhase,
                ServerHandle.TNHHoldPointCompleteHold,
                ServerHandle.SosigPriorityIFFChart,
                ServerHandle.LeverActionFirearmFire,
                ServerHandle.RevolvingShotgunFire,
                ServerHandle.DerringerFire,
                ServerHandle.FlintlockWeaponBurnOffOuter,
                ServerHandle.FlintlockWeaponFire,
                ServerHandle.GrappleGunFire,
                ServerHandle.HCBReleaseSled,
                ServerHandle.RemoteMissileDetonate,
                ServerHandle.RemoteMissileDamage,
                ServerHandle.RevolverFire,
                ServerHandle.SingleActionRevolverFire,
                ServerHandle.StingerLauncherFire,
                ServerHandle.StingerMissileDamage,
                ServerHandle.StingerMissileExplode,
                ServerHandle.PinnedGrenadeExplode,
                ServerHandle.FVRGrenadeExplode,
                ServerHandle.ClientDisconnect,
                ServerHandle.BangSnapSplode,
                ServerHandle.C4Detonate,
                ServerHandle.ClaymoreMineDetonate,
                ServerHandle.SLAMDetonate,
                ServerHandle.Ping,
                ServerHandle.TNHSetPhaseHold,
                ServerHandle.ShatterableCrateSetHoldingToken,
                ServerHandle.ResetTNH,
                ServerHandle.ReviveTNHPlayer,
                ServerHandle.PlayerColor,
                ServerHandle.RequestTNHInitialization,
                ServerHandle.TNHInitializer,
                ServerHandle.FuseIgnite,
                ServerHandle.FuseBoom,
                ServerHandle.MolotovShatter,
                ServerHandle.MolotovDamage,
                ServerHandle.PinnedGrenadePullPin,
                ServerHandle.MagazineAddRound,
                ServerHandle.ClipAddRound,
                ServerHandle.SpeedloaderChamberLoad,
                ServerHandle.RemoteGunChamber,
                ServerHandle.ChamberRound,
                ServerHandle.MagazineLoad,
                ServerHandle.MagazineLoadAttachable,
                ServerHandle.ClipLoad,
                ServerHandle.RevolverCylinderLoad,
                ServerHandle.RevolvingShotgunLoad,
                ServerHandle.GrappleGunLoad,
                ServerHandle.CarlGustafLatchSate,
                ServerHandle.CarlGustafShellSlideSate,
                ServerHandle.ItemUpdate,
                ServerHandle.TNHHostStartHold,
                ServerHandle.IntegratedFirearmFire,
                ServerHandle.GrappleAttached,
                ServerHandle.SosigUpdate,
                ServerHandle.AutoMeaterUpdate,
                ServerHandle.EncryptionUpdate,
                ServerHandle.TrackedObject,
                ServerHandle.TrackedObjects,
                ServerHandle.ObjectUpdate,
            };

            objects = new TrackedObjectData[100];
            availableObjectIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
                                                       10,11,12,13,14,15,16,17,18,19,
                                                       20,21,22,23,24,25,26,27,28,29,
                                                       30,31,32,33,34,35,36,37,38,39,
                                                       40,41,42,43,44,45,46,47,48,49,
                                                       50,51,52,53,54,55,56,57,58,59,
                                                       60,61,62,63,64,65,66,67,68,69,
                                                       70,71,72,73,74,75,76,77,78,79,
                                                       80,81,82,83,84,85,86,87,88,89,
                                                       90,91,92,93,94,95,96,97,98,99};

            availableSpectatorHosts = new List<int>();

            clientsWaitingUpDate.Clear();
            loadingClientsWaitingFrom.Clear();

            Mod.LogInfo("Initialized server", false);
        }
    }
}
