using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using H3MP.Tracking;
using FistVR;
using System.Timers;

namespace H3MP.Networking
{
    public class Server
    {
        public static ushort port;
        public static ushort maxClientCount;
        public static Dictionary<int, ServerClient> clients = new Dictionary<int, ServerClient>();
        public static List<int> connectedClients = new List<int>();
        public delegate void PacketHandler(int clientID, Packet packet);
        public static PacketHandler[] packetHandlers;
        public static TrackedObjectData[] objects; // All tracked objects, regardless of whos control they are under
        public static List<int> availableObjectIndices;
        public static Dictionary<int, List<int>> availableIndexBufferWaitingFor = new Dictionary<int, List<int>>(); // Clients a key index is waiting for confirmation from
        public static Dictionary<int, List<int>> availableIndexBufferClients = new Dictionary<int, List<int>>(); // Indices for which we are waiting for key client confirmation
        public static Dictionary<int, List<int>> IDsToConfirm = new Dictionary<int, List<int>>(); // IDs we still need to request confirmation for to lsit of clients
        public static readonly int IDConfirmLimit = 1;

        public static List<int> availableSpectatorHosts;
        public static Dictionary<int, int> spectatorHostByController = new Dictionary<int, int>();
        public static Dictionary<int, int> spectatorHostControllers = new Dictionary<int, int>();

        public static Dictionary<int, List<int>> clientsWaitingUpDate = new Dictionary<int, List<int>>(); // Clients we requested up to date objects from, for which clients
        public static Dictionary<int, List<int>> loadingClientsWaitingFrom = new Dictionary<int, List<int>>(); // Clients currently loading, waiting for up to date objects from which clients

        public static TcpListener tcpListener;
        public static UdpClient udpListener;

        public static List<ServerClient> PTClients = new List<ServerClient>();

        public static int tickRate = 20;
        public static Timer tickTimer = new Timer();
        
        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnServerClose event
        /// </summary>
        public delegate void OnServerCloseDelegate();

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we (Server) close the Server
        /// </summary>
        public static event OnServerCloseDelegate OnServerClose;

        public static void Start(ushort _maxClientCount, ushort _port, int tickRate)
        {
            Mod.LogInfo("Starting server on port: "+_port, false);

            maxClientCount = _maxClientCount;
            port = _port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Mod.LogInfo("Server started, listening on port: "+port, false);

            // Just connected, sync if current scene is syncable
            if (!GameManager.nonSynchronizedScenes.ContainsKey(GameManager.scene))
            {
                GameManager.SyncTrackedObjects(true, true);
            }

            Server.tickRate = tickRate;
            tickTimer.Elapsed += Tick;
            tickTimer.Interval = 1000f / tickRate;
            tickTimer.AutoReset = true;
            tickTimer.Start();
        }

        private static void Tick(object sender, ElapsedEventArgs e)
        {
            ServerSend.SendAllBatchedUDPData();
        }
        
        public static void Close()
        {
            tickTimer.Elapsed -= Tick;
            tickTimer.Stop();
            
            if(Mod.managerObject == null)
            {
                return;
            }

            Mod.LogInfo("Closing server.", false);

            ServerSend.ServerClosed();

            GameManager.TakeAllPhysicalControl(true);

            clients.Clear();
            objects = null;
            availableObjectIndices = null;
            availableSpectatorHosts.Clear();
            spectatorHostByController.Clear();

            tcpListener.Stop();
            tcpListener = null;
            udpListener.Close();
            udpListener = null;

            GameManager.Reset();
            Mod.Reset();
            SpecificClose();
            if (OnServerClose != null)
            {
                OnServerClose();
            }
        }

        private static void SpecificClose()
        {
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
            {
                GM.CurrentPlayerBody.EnableHands();
            }
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
                if (clients[i].tcp.socket == null && !clients[i].attemptingPunchThrough)
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
            Mod.LogInfo("Server adding new tracked object with waiting local index: "+trackedObject.localWaitingIndex+" from client: "+clientID + " with type ID: " + trackedObject.typeIdentifier+" in "+trackedObject.scene+"/"+trackedObject.instance, false);
            // If this is a sceneInit object received from client that we haven't tracked yet
            // And if the controller is not the first player in scene/instance
            if (trackedObject.trackedID == -1 && trackedObject.controller != 0 && trackedObject.sceneInit && !clients[trackedObject.controller].player.firstInSceneInstance)
            {
                Mod.LogInfo("\tRejecting", false);
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

            Mod.LogInfo("\tAssigned tracked ID: "+ trackedObject.trackedID, false);

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

            // Instantiate object if it is in the current scene and not controlled by us
            if (clientID != 0 && trackedObject.IsIdentifiable())
            {
                // Here, we don't want to instantiate if this is a scene we are in the process of loading
                // This is due to the possibility of objects only being identifiable in certain contexts like TNH_ShatterableCrates needing a TNH_manager
                if (trackedObject.physical == null && !trackedObject.awaitingInstantiation && 
                    !GameManager.sceneLoading && trackedObject.scene.Equals(GameManager.scene) && trackedObject.instance == GameManager.instance)
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

            Mod.LogInfo("\tSending to players:", false);
            for (int i = 0; i < toClients.Count; ++i) 
            {
                Mod.LogInfo("\t\t"+ toClients[i], false);
            }
            ServerSend.TrackedObject(trackedObject, toClients);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedObject.controller != 0)
            {
                trackedObject.localTrackedID = -1;
            }
        }

        private static void IncreaseObjectsSize()
        {
            TrackedObjectData[] tempObjects = objects;
            objects = new TrackedObjectData[tempObjects.Length + 100];
            for (int i = 0; i < tempObjects.Length; ++i)
            {
                objects[i] = tempObjects[i];
            }
            for (int i = tempObjects.Length; i < objects.Length; ++i) 
            {
                availableObjectIndices.Add(i);
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
                ServerHandle.GrappleAttached,
                ServerHandle.WelcomeReceived,
                ServerHandle.PlayerState,
                ServerHandle.PlayerScene,
                null,
                ServerHandle.DestroyObject,
                ServerHandle.ObjectUpdate,
                ServerHandle.ShatterableCrateSetHoldingHealth,
                ServerHandle.GiveObjectControl,
                ServerHandle.TrackedObjects,
                ServerHandle.ObjectParent,
                ServerHandle.WeaponFire,
                ServerHandle.PlayerDamage,
                ServerHandle.RemoteGunChamber,
                ServerHandle.ChamberRound,
                ServerHandle.IntegratedFirearmFire,
                ServerHandle.TrackedObject,
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
                ServerHandle.UpToDateObjects,
                ServerHandle.SpeedloaderChamberLoad,
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
                ServerHandle.RevolvingShotgunLoad,
                ServerHandle.GrappleGunLoad,
                ServerHandle.CarlGustafLatchSate,
                ServerHandle.CarlGustafShellSlideSate,
                ServerHandle.TNHHostStartHold,
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
                ServerHandle.MagazineLoad,
                ServerHandle.MagazineLoadAttachable,
                ServerHandle.ClipLoad,
                ServerHandle.RevolverCylinderLoad,
                ServerHandle.EncryptionDamage,
                ServerHandle.EncryptionDamageData,
                ServerHandle.EncryptionRespawnSubTarg,
                ServerHandle.EncryptionSpawnGrowth,
                ServerHandle.EncryptionInit,
                ServerHandle.EncryptionResetGrowth,
                ServerHandle.EncryptionDisableSubtarg,
                ServerHandle.EncryptionSubDamage,
                ServerHandle.ShatterableCrateDestroy,
                ServerHandle.RegisterCustomPacketType,
                ServerHandle.DoneLoadingScene,
                ServerHandle.DoneSendingUpToDateObjects,
                ServerHandle.SosigWeaponFire,
                ServerHandle.SosigWeaponShatter,
                ServerHandle.SosigWeaponDamage,
                ServerHandle.LAPD2019Fire,
                ServerHandle.LAPD2019LoadBattery,
                ServerHandle.LAPD2019ExtractBattery,
                null,
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
                ServerHandle.BreakableGlassDamage,
                ServerHandle.WindowShatterSound,
                ServerHandle.RequestSpectatorHost,
                ServerHandle.UnassignSpectatorHost,
                ServerHandle.SpectatorHostOrderTNHHost,
                ServerHandle.TNHSpectatorHostReady,
                ServerHandle.SpectatorHostStartTNH,
                ServerHandle.ReassignSpectatorHost,
                ServerHandle.ReactiveSteelTargetDamage,
                ServerHandle.MTUTest,
                ServerHandle.IDConfirm,
                ServerHandle.ObjectScene,
                ServerHandle.ObjectInstance,
                ServerHandle.UpdateEncryptionDisplay,
                ServerHandle.EncryptionRespawnSubTargGeo,
                ServerHandle.RoundDamage,
                ServerHandle.RoundSplode,
                ServerHandle.SightFlipperState,
                ServerHandle.SightRaiserState,
                ServerHandle.GatlingGunFire,
                ServerHandle.GasCuboidGout,
                ServerHandle.GasCuboidDamage,
                ServerHandle.GasCuboidHandleDamage,
                ServerHandle.GasCuboidDamageHandle,
                ServerHandle.GasCuboidExplode,
                ServerHandle.GasCuboidShatter,
                ServerHandle.FloaterDamage,
                ServerHandle.FloaterCoreDamage,
                ServerHandle.FloaterBeginExploding,
                ServerHandle.FloaterExplode,
                ServerHandle.IrisShatter,
                ServerHandle.IrisSetState,
                ServerHandle.BrutBlockSystemStart,
                ServerHandle.FloaterBeginDefusing,
                ServerHandle.BatchedPackets
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

        public static int RegisterCustomPacketType(string handlerID, int clientID = 0)
        {
            int index = -1;

            if (Mod.registeredCustomPacketIDs.TryGetValue(handlerID, out index))
            {
                Mod.LogWarning("Client " + clientID + " requested for " + handlerID + " custom packet handler to be registered but this ID already exists.");
            }
            else // We don't yet have this handlerID, add it
            {
                // Get next available handler ID
                if(Mod.availableCustomPacketIndices.Count > 0)
                {
                    index = Mod.availableCustomPacketIndices[Mod.availableCustomPacketIndices.Count - 1];
                    Mod.availableCustomPacketIndices.RemoveAt(Mod.availableCustomPacketIndices.Count - 1);
                }

                // If couldn't find one, need to add more space to handlers array
                if (index == -1)
                {
                    index = Mod.customPacketHandlers.Length;
                    Mod.CustomPacketHandler[] temp = Mod.customPacketHandlers;
                    Mod.customPacketHandlers = new Mod.CustomPacketHandler[index + 10];
                    for (int i = 0; i < temp.Length; ++i)
                    {
                        Mod.customPacketHandlers[i] = temp[i];
                    }
                    for (int i = index + 1; i < Mod.customPacketHandlers.Length; ++i) 
                    {
                        Mod.availableCustomPacketIndices.Add(i);
                    }
                }

                // Store for potential later use
                Mod.registeredCustomPacketIDs.Add(handlerID, index);

                // Send event so a mod can add their handler at the index
                Mod.CustomPacketHandlerReceivedInvoke(handlerID, index);
            }

            // Send back/relay to others
            ServerSend.RegisterCustomPacketType(handlerID, index);

            return index;
        }
    }
}
