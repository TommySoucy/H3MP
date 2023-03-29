using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using FistVR;

namespace H3MP
{
    internal class H3MP_Server
    {
        public static ushort port;
        public static ushort maxClientCount;
        public static Dictionary<int, H3MP_ServerClient> clients = new Dictionary<int, H3MP_ServerClient>();
        public delegate void PacketHandler(int clientID, H3MP_Packet packet);
        public static PacketHandler[] packetHandlers;
        public static H3MP_TrackedItemData[] items; // All tracked items, regardless of whos control they are under
        public static List<int> availableItemIndices;
        public static H3MP_TrackedSosigData[] sosigs; // All tracked Sosigs, regardless of whos control they are under
        public static List<int> availableSosigIndices;
        public static H3MP_TrackedAutoMeaterData[] autoMeaters; // All tracked AutoMeaters, regardless of whos control they are under
        public static List<int> availableAutoMeaterIndices;
        public static H3MP_TrackedEncryptionData[] encryptions; // All tracked TNH_EncryptionTarget, regardless of whos control they are under
        public static List<int> availableEncryptionIndices;

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
            if (!H3MP_GameManager.nonSynchronizedScenes.ContainsKey(H3MP_GameManager.scene))
            {
                H3MP_GameManager.SyncTrackedItems(true, true);
                H3MP_GameManager.SyncTrackedSosigs(true, true);
                H3MP_GameManager.SyncTrackedAutoMeaters(true, true);
                H3MP_GameManager.SyncTrackedEncryptions(true, true);
            }
        }

        public static void Close()
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            Mod.LogInfo("Closing server.", false);

            H3MP_ServerSend.ServerClosed();

            H3MP_GameManager.TakeAllPhysicalControl(true);

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

            H3MP_GameManager.Reset();
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

                using(H3MP_Packet packet = new H3MP_Packet(data))
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

        public static void SendUDPData(IPEndPoint clientEndPoint, H3MP_Packet packet)
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

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem, int clientID)
        {
            // If this is a sceneInit item received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if(trackedItem.trackedID == -1 && trackedItem.controller != 0 && trackedItem.sceneInit && !clients[trackedItem.controller].player.firstInSceneInstance)
            {
                // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                // indicate this to the sending client so they can destroy their item
                trackedItem.trackedID = -2;
                H3MP_ServerSend.TrackedItemSpecific(trackedItem, trackedItem.controller);
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
            if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> relevantInstances))
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
                H3MP_GameManager.itemsByInstanceByScene.Add(trackedItem.scene, newInstances);
            }

            // Add to parent children if has parent and we are not initTracker (It isn't already in the list)
            if (trackedItem.parent != -1 && trackedItem.initTracker != H3MP_GameManager.ID)
            {
                // Note that this should never be null, we should always receive the parent data before receiving the children's
                // TODO: Review: If this is actually true: is the hierarchy maintained when server sends relevant items to a client when the client joins a scene/instance?
                H3MP_TrackedItemData parentData = items[trackedItem.parent];

                if (parentData.children == null)
                {
                    parentData.children = new List<H3MP_TrackedItemData>();
                }

                trackedItem.childIndex = parentData.children.Count;
                parentData.children.Add(trackedItem);
            }

            // Instantiate item if it is in the current scene and not controlled by us
            if (clientID != 0 && H3MP_GameManager.IsItemIdentifiable(trackedItem))
            {
                // Here, we don't want to instantiate if this is a scene we are in the process of loading
                // This is due to the possibility of items only being identifiable in certain contexts like TNH_ShatterableCrates needing a TNH_manager
                if (!trackedItem.awaitingInstantiation && trackedItem.scene.Equals(H3MP_GameManager.scene) && trackedItem.instance == H3MP_GameManager.instance)
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
            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> instances) &&
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
            H3MP_ServerSend.TrackedItem(trackedItem, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedItem.controller != 0)
            {
                trackedItem.localTrackedID = -1;
            }
        }

        public static void AddTrackedSosig(H3MP_TrackedSosigData trackedSosig, int clientID)
        {
            if (trackedSosig.trackedID == -1)
            {
                // If this is a sceneInit sosig received from client that we haven't tracked yet
                // And if the controller is not the first player in scene/instance
                if (trackedSosig.controller != 0 && trackedSosig.sceneInit && !clients[trackedSosig.controller].player.firstInSceneInstance)
                {
                    // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                    // indicate this to the sending client so they can destroy their sosig
                    trackedSosig.trackedID = -2;
                    H3MP_ServerSend.TrackedSosigSpecific(trackedSosig, trackedSosig.controller);
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
                if (H3MP_GameManager.sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> relevantInstances))
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
                    H3MP_GameManager.sosigsByInstanceByScene.Add(trackedSosig.scene, newInstances);
                }

                // Instantiate sosig if it is in the current scene and not controlled by us
                if (clientID != 0)
                {
                    // Don't use loading scene here. See AddTrackedItem why
                    if (!trackedSosig.awaitingInstantiation && trackedSosig.scene.Equals(H3MP_GameManager.scene) && trackedSosig.instance == H3MP_GameManager.instance)
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
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> instances) &&
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
                H3MP_ServerSend.TrackedSosig(trackedSosig, toClients);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedSosig.controller != 0)
                {
                    trackedSosig.localTrackedID = -1;
                }

                // Manage control for TNH
                if (H3MP_GameManager.TNHInstances.TryGetValue(trackedSosig.instance, out H3MP_TNHInstance TNHInstance) &&
                    TNHInstance.controller != trackedSosig.controller && 
                    ((TNHInstance.controller == 0 && trackedSosig.scene.Equals(H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene)) ||
                    (TNHInstance.controller != 0 && trackedSosig.scene.Equals(clients[TNHInstance.controller].player.scene))))
                {
                    // Sosig is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                    if(TNHInstance.controller == 0)
                    {
                        // Us, take control
                        trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                        H3MP_GameManager.sosigs.Add(trackedSosig);
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
                                GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                            }
                            trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                        }

                    }
                    else if(trackedSosig.controller == 0)
                    {
                        // Was us, give up control
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

                    trackedSosig.controller = TNHInstance.controller;
                    H3MP_ServerSend.GiveSosigControl(trackedSosig.trackedID, TNHInstance.controller, null);
                }
            }
            else
            {
                // This is a sosig we already received full data for and assigned a tracked ID to but things may
                // have happened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                sosigs[trackedSosig.trackedID].Update(trackedSosig, true);

                List<int> toClients = new List<int>();
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> instances) &&
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
                H3MP_ServerSend.TrackedSosig(trackedSosig, toClients);
            }
        }

        public static void AddTrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, int clientID)
        {
            // If this is a sceneInit autoMeater received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if (trackedAutoMeater.trackedID == -1 && trackedAutoMeater.controller != 0 && trackedAutoMeater.sceneInit && !clients[trackedAutoMeater.controller].player.firstInSceneInstance)
            {
                // We only want to track this if controller was first in their scene/instance, so in this case set tracked ID to -2 to
                // indicate this to the sending client so they can destroy their autoMeater
                trackedAutoMeater.trackedID = -2;
                H3MP_ServerSend.TrackedAutoMeaterSpecific(trackedAutoMeater, trackedAutoMeater.controller);
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
            if (H3MP_GameManager.autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> relevantInstances))
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
                H3MP_GameManager.autoMeatersByInstanceByScene.Add(trackedAutoMeater.scene, newInstances);
            }

            // Instantiate AutoMeater if it is in the current scene and not controlled by us
            if (clientID != 0)
            {
                // Don't use loading scene here. See AddTrackedItem why
                if (!trackedAutoMeater.awaitingInstantiation && trackedAutoMeater.scene.Equals(H3MP_GameManager.scene) && trackedAutoMeater.instance == H3MP_GameManager.instance)
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
            if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> instances) &&
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
            H3MP_ServerSend.TrackedAutoMeater(trackedAutoMeater, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedAutoMeater.controller != 0)
            {
                trackedAutoMeater.localTrackedID = -1;
            }

            // Manage control for TNH
            if (H3MP_GameManager.TNHInstances.TryGetValue(trackedAutoMeater.instance, out H3MP_TNHInstance TNHInstance) &&
                TNHInstance.controller != trackedAutoMeater.controller &&
                ((TNHInstance.controller == 0 && trackedAutoMeater.scene.Equals(H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene)) ||
                (TNHInstance.controller != 0 && trackedAutoMeater.scene.Equals(clients[TNHInstance.controller].player.scene))))
            {
                // Object is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                if (TNHInstance.controller == 0)
                {
                    // Us, take control
                    trackedAutoMeater.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                    H3MP_GameManager.autoMeaters.Add(trackedAutoMeater);
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
                H3MP_ServerSend.GiveAutoMeaterControl(trackedAutoMeater.trackedID, TNHInstance.controller, null);
            }
        }

        public static void AddTrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption, int clientID)
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
                    H3MP_ServerSend.TrackedEncryptionSpecific(trackedEncryption, trackedEncryption.controller);
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
                if (H3MP_GameManager.encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> relevantInstances))
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
                    H3MP_GameManager.encryptionsByInstanceByScene.Add(trackedEncryption.scene, newInstances);
                }

                // Instantiate Encryption if it is in the current scene and not controlled by us
                if (clientID != 0)
                {
                    // Don't use loading scene here. See AddTrackedItem why
                    if (!trackedEncryption.awaitingInstantiation && trackedEncryption.scene.Equals(H3MP_GameManager.scene) && trackedEncryption.instance == H3MP_GameManager.instance)
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
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> instances) &&
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
                H3MP_ServerSend.TrackedEncryption(trackedEncryption, toClients);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedEncryption.controller != 0)
                {
                    trackedEncryption.localTrackedID = -1;
                }

                // Manage control for TNH
                if (H3MP_GameManager.TNHInstances.TryGetValue(trackedEncryption.instance, out H3MP_TNHInstance TNHInstance) &&
                    TNHInstance.controller != trackedEncryption.controller &&
                    ((TNHInstance.controller == 0 && trackedEncryption.scene.Equals(H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene)) ||
                    (TNHInstance.controller != 0 && trackedEncryption.scene.Equals(clients[TNHInstance.controller].player.scene))))
                {
                    // Object is in a TNH instance with the instance's controller but is not controlled by the controller, give control
                    if (TNHInstance.controller == 0)
                    {
                        // Us, take control
                        trackedEncryption.localTrackedID = H3MP_GameManager.encryptions.Count;
                        H3MP_GameManager.encryptions.Add(trackedEncryption);
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
                    H3MP_ServerSend.GiveEncryptionControl(trackedEncryption.trackedID, TNHInstance.controller, null);
                }
            }
            else
            {
                // This is a encryption we already received full data for and assigned a tracked ID to but things may
                // have happened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                encryptions[trackedEncryption.trackedID].Update(trackedEncryption, true);

                List<int> toClients = new List<int>();
                if (H3MP_GameManager.playersByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> instances) &&
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
                H3MP_ServerSend.TrackedEncryption(trackedEncryption, toClients);
            }
        }

        private static void IncreaseItemsSize()
        {
            H3MP_TrackedItemData[] tempItems = items;
            items = new H3MP_TrackedItemData[tempItems.Length + 100];
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
            H3MP_TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new H3MP_TrackedSosigData[tempSosigs.Length + 100];
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
            H3MP_TrackedAutoMeaterData[] tempAutoMeaters = autoMeaters;
            autoMeaters = new H3MP_TrackedAutoMeaterData[tempAutoMeaters.Length + 100];
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
            H3MP_TrackedEncryptionData[] tempEncryptions = encryptions;
            encryptions = new H3MP_TrackedEncryptionData[tempEncryptions.Length + 100];
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
                clients.Add(i, new H3MP_ServerClient(i));
            }

            packetHandlers = new PacketHandler[]
            {
                null,
                H3MP_ServerHandle.WelcomeReceived,
                H3MP_ServerHandle.PlayerState,
                H3MP_ServerHandle.PlayerScene,
                H3MP_ServerHandle.AddNonSyncScene,
                H3MP_ServerHandle.TrackedItems,
                H3MP_ServerHandle.TrackedItem,
                H3MP_ServerHandle.ShatterableCrateSetHoldingHealth,
                H3MP_ServerHandle.GiveControl,
                H3MP_ServerHandle.DestroyItem,
                H3MP_ServerHandle.ItemParent,
                H3MP_ServerHandle.WeaponFire,
                H3MP_ServerHandle.PlayerDamage,
                H3MP_ServerHandle.TrackedSosig,
                H3MP_ServerHandle.TrackedSosigs,
                H3MP_ServerHandle.GiveSosigControl,
                H3MP_ServerHandle.DestroySosig,
                H3MP_ServerHandle.SosigPickUpItem,
                H3MP_ServerHandle.SosigPlaceItemIn,
                H3MP_ServerHandle.SosigDropSlot,
                H3MP_ServerHandle.SosigHandDrop,
                H3MP_ServerHandle.SosigConfigure,
                H3MP_ServerHandle.SosigLinkRegisterWearable,
                H3MP_ServerHandle.SosigLinkDeRegisterWearable,
                H3MP_ServerHandle.SosigSetIFF,
                H3MP_ServerHandle.SosigSetOriginalIFF,
                H3MP_ServerHandle.SosigLinkDamage,
                H3MP_ServerHandle.SosigDamageData,
                H3MP_ServerHandle.SosigWearableDamage,
                H3MP_ServerHandle.SosigLinkExplodes,
                H3MP_ServerHandle.SosigDies,
                H3MP_ServerHandle.SosigClear,
                H3MP_ServerHandle.SosigSetBodyState,
                H3MP_ServerHandle.PlaySosigFootStepSound,
                H3MP_ServerHandle.SosigSpeakState,
                H3MP_ServerHandle.SosigSetCurrentOrder,
                H3MP_ServerHandle.SosigVaporize,
                H3MP_ServerHandle.SosigRequestHitDecal,
                H3MP_ServerHandle.SosigLinkBreak,
                H3MP_ServerHandle.SosigLinkSever,
                H3MP_ServerHandle.UpToDateItems,
                H3MP_ServerHandle.UpToDateSosigs,
                H3MP_ServerHandle.PlayerInstance,
                H3MP_ServerHandle.AddTNHInstance,
                H3MP_ServerHandle.AddTNHCurrentlyPlaying,
                H3MP_ServerHandle.RemoveTNHCurrentlyPlaying,
                H3MP_ServerHandle.SetTNHProgression,
                H3MP_ServerHandle.SetTNHEquipment,
                H3MP_ServerHandle.SetTNHHealthMode,
                H3MP_ServerHandle.SetTNHTargetMode,
                H3MP_ServerHandle.SetTNHAIDifficulty,
                H3MP_ServerHandle.SetTNHRadarMode,
                H3MP_ServerHandle.SetTNHItemSpawnerMode,
                H3MP_ServerHandle.SetTNHBackpackMode,
                H3MP_ServerHandle.SetTNHHealthMult,
                H3MP_ServerHandle.SetTNHSosigGunReload,
                H3MP_ServerHandle.SetTNHSeed,
                H3MP_ServerHandle.SetTNHLevelID,
                H3MP_ServerHandle.AddInstance,
                H3MP_ServerHandle.SetTNHController,
                H3MP_ServerHandle.SpectatorHost,
                H3MP_ServerHandle.TNHPlayerDied,
                H3MP_ServerHandle.TNHAddTokens,
                H3MP_ServerHandle.TNHSetLevel,
                H3MP_ServerHandle.TrackedAutoMeater,
                H3MP_ServerHandle.TrackedAutoMeaters,
                H3MP_ServerHandle.DestroyAutoMeater,
                H3MP_ServerHandle.GiveAutoMeaterControl,
                H3MP_ServerHandle.UpToDateAutoMeaters,
                H3MP_ServerHandle.AutoMeaterSetState,
                H3MP_ServerHandle.AutoMeaterSetBladesActive,
                H3MP_ServerHandle.AutoMeaterDamage,
                H3MP_ServerHandle.AutoMeaterDamageData,
                H3MP_ServerHandle.AutoMeaterFirearmFireShot,
                H3MP_ServerHandle.AutoMeaterFirearmFireAtWill,
                H3MP_ServerHandle.AutoMeaterHitZoneDamage,
                H3MP_ServerHandle.AutoMeaterHitZoneDamageData,
                H3MP_ServerHandle.TNHSosigKill,
                H3MP_ServerHandle.TNHHoldPointSystemNode,
                H3MP_ServerHandle.TNHHoldBeginChallenge,
                H3MP_ServerHandle.ShatterableCrateDamage,
                H3MP_ServerHandle.TNHSetPhaseTake,
                H3MP_ServerHandle.TNHHoldCompletePhase,
                H3MP_ServerHandle.TNHHoldPointFailOut,
                H3MP_ServerHandle.TNHSetPhaseComplete,
                H3MP_ServerHandle.TNHSetPhase,
                H3MP_ServerHandle.TrackedEncryptions,
                H3MP_ServerHandle.TrackedEncryption,
                H3MP_ServerHandle.GiveEncryptionControl,
                H3MP_ServerHandle.DestroyEncryption,
                H3MP_ServerHandle.EncryptionDamage,
                H3MP_ServerHandle.EncryptionDamageData,
                H3MP_ServerHandle.EncryptionRespawnSubTarg,
                H3MP_ServerHandle.EncryptionSpawnGrowth,
                H3MP_ServerHandle.EncryptionInit,
                H3MP_ServerHandle.EncryptionResetGrowth,
                H3MP_ServerHandle.EncryptionDisableSubtarg,
                H3MP_ServerHandle.EncryptionSubDamage,
                H3MP_ServerHandle.ShatterableCrateDestroy,
                H3MP_ServerHandle.UpToDateEncryptions,
                H3MP_ServerHandle.DoneLoadingScene,
                H3MP_ServerHandle.DoneSendingUpToDateObjects,
                H3MP_ServerHandle.SosigWeaponFire,
                H3MP_ServerHandle.SosigWeaponShatter,
                H3MP_ServerHandle.SosigWeaponDamage,
                H3MP_ServerHandle.LAPD2019Fire,
                H3MP_ServerHandle.LAPD2019LoadBattery,
                H3MP_ServerHandle.LAPD2019ExtractBattery,
                H3MP_ServerHandle.MinigunFire,
                H3MP_ServerHandle.AttachableFirearmFire,
                H3MP_ServerHandle.BreakActionWeaponFire,
                H3MP_ServerHandle.PlayerIFF,
                H3MP_ServerHandle.UberShatterableShatter,
                H3MP_ServerHandle.TNHHoldPointBeginAnalyzing,
                H3MP_ServerHandle.TNHHoldPointRaiseBarriers,
                H3MP_ServerHandle.TNHHoldIdentifyEncryption,
                H3MP_ServerHandle.TNHHoldPointBeginPhase,
                H3MP_ServerHandle.TNHHoldPointCompleteHold,
                H3MP_ServerHandle.SosigPriorityIFFChart,
                H3MP_ServerHandle.LeverActionFirearmFire,
                H3MP_ServerHandle.RevolvingShotgunFire,
                H3MP_ServerHandle.DerringerFire,
                H3MP_ServerHandle.FlintlockWeaponBurnOffOuter,
                H3MP_ServerHandle.FlintlockWeaponFire,
                H3MP_ServerHandle.GrappleGunFire,
                H3MP_ServerHandle.HCBReleaseSled,
                H3MP_ServerHandle.RemoteMissileDetonate,
                H3MP_ServerHandle.RemoteMissileDamage,
                H3MP_ServerHandle.RevolverFire,
                H3MP_ServerHandle.SingleActionRevolverFire,
                H3MP_ServerHandle.StingerLauncherFire,
                H3MP_ServerHandle.StingerMissileDamage,
                H3MP_ServerHandle.StingerMissileExplode,
                H3MP_ServerHandle.PinnedGrenadeExplode,
                H3MP_ServerHandle.FVRGrenadeExplode,
                H3MP_ServerHandle.ClientDisconnect,
                H3MP_ServerHandle.BangSnapSplode,
                H3MP_ServerHandle.C4Detonate,
                H3MP_ServerHandle.ClaymoreMineDetonate,
                H3MP_ServerHandle.SLAMDetonate,
                H3MP_ServerHandle.Ping,
                H3MP_ServerHandle.TNHSetPhaseHold,
                H3MP_ServerHandle.ShatterableCrateSetHoldingToken,
                H3MP_ServerHandle.ResetTNH,
                H3MP_ServerHandle.ReviveTNHPlayer,
                H3MP_ServerHandle.PlayerColor,
                H3MP_ServerHandle.RequestTNHInitialization,
                H3MP_ServerHandle.TNHInitializer,
                H3MP_ServerHandle.FuseIgnite,
                H3MP_ServerHandle.FuseBoom,
                H3MP_ServerHandle.MolotovShatter,
                H3MP_ServerHandle.MolotovDamage,
                H3MP_ServerHandle.PinnedGrenadePullPin,
            };

            items = new H3MP_TrackedItemData[100];
            availableItemIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
                                                     10,11,12,13,14,15,16,17,18,19,
                                                     20,21,22,23,24,25,26,27,28,29,
                                                     30,31,32,33,34,35,36,37,38,39,
                                                     40,41,42,43,44,45,46,47,48,49,
                                                     50,51,52,53,54,55,56,57,58,59,
                                                     60,61,62,63,64,65,66,67,68,69,
                                                     70,71,72,73,74,75,76,77,78,79,
                                                     80,81,82,83,84,85,86,87,88,89,
                                                     90,91,92,93,94,95,96,97,98,99};

            sosigs = new H3MP_TrackedSosigData[100];
            availableSosigIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
                                                     10,11,12,13,14,15,16,17,18,19,
                                                     20,21,22,23,24,25,26,27,28,29,
                                                     30,31,32,33,34,35,36,37,38,39,
                                                     40,41,42,43,44,45,46,47,48,49,
                                                     50,51,52,53,54,55,56,57,58,59,
                                                     60,61,62,63,64,65,66,67,68,69,
                                                     70,71,72,73,74,75,76,77,78,79,
                                                     80,81,82,83,84,85,86,87,88,89,
                                                     90,91,92,93,94,95,96,97,98,99};

            autoMeaters = new H3MP_TrackedAutoMeaterData[100];
            availableAutoMeaterIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
                                                     10,11,12,13,14,15,16,17,18,19,
                                                     20,21,22,23,24,25,26,27,28,29,
                                                     30,31,32,33,34,35,36,37,38,39,
                                                     40,41,42,43,44,45,46,47,48,49,
                                                     50,51,52,53,54,55,56,57,58,59,
                                                     60,61,62,63,64,65,66,67,68,69,
                                                     70,71,72,73,74,75,76,77,78,79,
                                                     80,81,82,83,84,85,86,87,88,89,
                                                     90,91,92,93,94,95,96,97,98,99};

            encryptions = new H3MP_TrackedEncryptionData[100];
            availableEncryptionIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9,
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
