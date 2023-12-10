using FistVR;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using UnityEngine;

namespace H3MP.Networking
{
    public class Client : MonoBehaviour
    {
        private static Client _singleton;
        public static Client singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Mod.LogInfo($"{nameof(Client)} instance already exists, destroying duplicate!", false);
                    Destroy(value);
                }
            }
        }

        public static int dataBufferSize = 4096;

        public string IP;
        public ushort port;
        public int ID = -1;
        public TCP tcp;
        public UDP udp;

        private bool isConnected = false;
        public static bool isFullyConnected = false;
        public bool gotWelcome = false;
        public bool gotConnectSync = false;
        public int pingAttemptCounter = 0;
        public delegate void PacketHandler(Packet packet);
        public static PacketHandler[] packetHandlers;
        public static Dictionary<string, int> synchronizedScenes;
        public static TrackedObjectData[] objects; // All tracked objects, regardless of whos control they are under

        public static uint localObjectCounter = 0;
        public static Dictionary<uint, TrackedObjectData> waitingLocalObjects = new Dictionary<uint, TrackedObjectData>();
        public static bool punchThrough;
        public static bool punchThroughWaiting;
        public static IAsyncResult connectResult;
        public static int punchThroughAttemptCounter;

        public int tickRate = 20;
        public Timer tickTimer = new Timer();
        
        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnDisconnect event
        /// </summary>
        public delegate void OnDisconnectDelegate();

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we (Client) disconnect from a Server
        /// </summary>
        public static event OnDisconnectDelegate OnDisconnect;

        private void Awake()
        {
            singleton = this;
        }

        private void Start()
        {
            if (tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }
        }

        public void ConnectToServer()
        {
            if(tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }

            InitializeClientData();

            isConnected = true;
            tcp.Connect();
        }

        public void SetTickRate(int tickRate)
        {
            this.tickRate = tickRate;
            tickTimer.Elapsed += Tick;
            tickTimer.Interval = 1000f / tickRate;
            tickTimer.AutoReset = true;
            tickTimer.Start();
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            ClientSend.SendBatchedPackets();
        }
        
        public class TCP
        {
            public TcpClient socket;

            public NetworkStream stream;
            public Packet receivedData;
            public byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];
                Mod.LogInfo("Attempting connection to " + singleton.IP + ":" + singleton.port, false);
                connectResult = socket.BeginConnect(singleton.IP, singleton.port, ConnectCallback, socket);
                if (punchThrough)
                {
                    punchThroughAttemptCounter = 0;
                    punchThroughWaiting = true;
                }
            }

            public void ConnectCallback(IAsyncResult result)
            {
                punchThroughWaiting = false;
                Mod.LogInfo("Got connect callback");
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            public void SendData(Packet packet, bool overrideWelcome = false)
            {
                if (Client.singleton.gotWelcome || overrideWelcome)
                {
                    try
                    {
                        if (socket != null)
                        {
                            stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Mod.LogInfo($"Error sending data to server via TCP: {ex}", false);
                    }
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                // We received bytes through stream
                try
                {
                    // Read how many, if none, disconnect
                    int byteLength = stream.EndRead(result);
                    if (byteLength == 0)
                    {
                        singleton.Disconnect(true, 1);
                        return;
                    }

                    // If we received some data prepare a data array and fill it up with the data
                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    int handleCode = HandleData(data);
                    if (handleCode > 0)
                    {
                        receivedData.Reset(handleCode == 2);
                    }
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception)
                {
                    Disconnect(2);
                }
            }

            private int HandleData(byte[] data)
            {
                int packetLength = 0;
                bool readLength = false;

                receivedData.SetBytes(data);

                // Handle receiving empty packet, we return true so that receivedData packet gets reset
                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    readLength = true;
                    if (packetLength <= 0)
                    {
                        return 2;
                    }
                }

                // If we have enough data to read packet length and we got here, it means we have at least some data for a packet
                // This might not be the entire packet, so we check if the length written to packet is less than the data received
                // If so, it means we have enough data to build a packet we can process
                while(packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    // TODO: // When player state handler dont forget int ID = packet.ReadInt();
                    readLength = false;
                    // So here we take all the data for that new packet
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        if (singleton.isConnected)
                        {
                            // Build a packet from it
                            using (Packet packet = new Packet(packetBytes))
                            {
                                // Read its ID, and process it
                                int packetID = packet.ReadInt();

                                try
                                {
                                    if (singleton.ID >= 0 || (ServerPackets)packetID == ServerPackets.welcome)
                                    {
                                        if (packetID < 0)
                                        {
                                            if (packetID == -1)
                                            {
                                                Mod.GenericCustomPacketReceivedInvoke(0, packet.ReadString(), packet);
                                            }
                                            else // packetID <= -2
                                            {
                                                int index = packetID * -1 - 2;
                                                if (Mod.customPacketHandlers.Length > index && Mod.customPacketHandlers[index] != null)
                                                {
#if DEBUG
                                                    if (Input.GetKey(KeyCode.PageDown))
                                                    {
                                                        Mod.LogInfo("\tHandling custom TCP packet: " + packetID);
                                                    }
#endif
                                                    Mod.customPacketHandlers[index](0, packet);
                                                }
#if DEBUG
                                                else
                                                {
                                                    Mod.LogWarning("\tClient received invalid custom TCP packet ID: " + packetID);
                                                }
#endif
                                            }
                                        }
                                        else
                                        {
#if DEBUG
                                            if (Input.GetKey(KeyCode.PageDown))
                                            {
                                                Mod.LogInfo("\tHandling TCP packet: " + packetID + " ("+(ServerPackets)packetID+"), length: " + packet.buffer.Count);
                                            }
#endif
                                            packetHandlers[packetID](packet);
                                        }
                                    }
                                }
                                catch(IndexOutOfRangeException ex)
                                {
                                    Mod.LogError("Client TCP received packet with ID: "+packetID+ " as ServerPackets: " + ((ServerPackets)packetID).ToString()+" with packetLength: "+ packetLength + ":\n"+ ex.StackTrace);
                                }
                            }
                        }
                    });

                    packetLength = 0;

                    // Check again if we have empty packet, if we do, return true to reset the receivedData
                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        readLength = true;
                        if (packetLength <= 0)
                        {
                            return 2;
                        }
                    }
                }

                // If there is no data left to read, means we processed the last byte of data we had as part of the last packet we handled
                // so we can reset the receivedData completely
                if (packetLength == 0 && receivedData.UnreadLength() == 0)
                {
                    return 2;
                }

                // If we get here, it is because we have data left to read
                // If we read the length of the packet we didnt end up handling because we are still missing data
                // we want to undo reading 4 bytes, so return 1
                // Otherwise we return 0 indicating we don't want to reset anything, we just want to keep building up data
                return readLength ? 1 : 0;
            }

            private void Disconnect(int code)
            {
                singleton.Disconnect(false, code);

                stream = null;
                receiveBuffer = null;
                receivedData = null;
                socket = null;
            }
        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(singleton.IP), singleton.port);
            }

            public void Connect(int localPort)
            {
                socket = new UdpClient(localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using(Packet packet = new Packet())
                {
                    SendData(packet);
                }
            }

            public void SendData(Packet packet)
            {
                try
                {
                    packet.InsertInt(singleton.ID);
                    if(socket != null)
                    {
                        socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Mod.LogInfo($"Error sending UDP data {ex}", false);
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                if (Client.singleton.isConnected)
                {
                    try
                    {
                        byte[] data = socket.EndReceive(result, ref endPoint);
                        socket.BeginReceive(ReceiveCallback, null);

                        if(data.Length < 4)
                        {
                            singleton.Disconnect(true, 1);
                            return;
                        }

                        HandleData(data);
                    }
                    catch(Exception)
                    {
                        Disconnect(2);
                    }
                }
            }

            public void HandleData(byte[] data)
            {
                using(Packet prePacket = new Packet(data))
                {
                    int packetLength = prePacket.ReadInt();
                    data = prePacket.ReadBytes(packetLength);
                }

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    if (singleton.isConnected)
                    {
                        using (Packet packet = new Packet(data))
                        {
                            int packetID = packet.ReadInt();

                            if (packetID < 0)
                            {
                                if (packetID == -1)
                                {
                                    Mod.GenericCustomPacketReceivedInvoke(0, packet.ReadString(), packet);
                                }
                                else // packetID <= -2
                                {
                                    int index = packetID * -1 - 2;
                                    if (Mod.customPacketHandlers.Length > index && Mod.customPacketHandlers[index] != null)
                                    {
#if DEBUG
                                        if (Input.GetKey(KeyCode.PageDown))
                                        {
                                            Mod.LogInfo("\tHandling custom UDP packet: " + packetID);
                                        }
#endif
                                        Mod.customPacketHandlers[index](0, packet);
                                    }
#if DEBUG
                                    else
                                    {
                                        Mod.LogWarning("\tClient received invalid custom UDP packet ID: " + packetID);
                                    }
#endif
                                }
                            }
                            else
                            {
#if DEBUG
                                if (Input.GetKey(KeyCode.PageDown))
                                {
                                    Mod.LogInfo("\tHandling UDP packet: " + packetID + " (" + (ServerPackets)packetID + "), length: " + packet.buffer.Count);
                                }
#endif
                                packetHandlers[packetID](packet);
                            }
                        }
                    }
                });
            }

            private void Disconnect(int code)
            {
                singleton.Disconnect(false, code);

                endPoint = null;
                socket = null;
            }
        }

        private void InitializeClientData()
        {
            packetHandlers = new PacketHandler[]
            {
                ClientHandle.Welcome,
                ClientHandle.SpawnPlayer,
                ClientHandle.PlayerState,
                ClientHandle.PlayerScene,
                null,
                ClientHandle.ShatterableCrateSetHoldingHealth,
                ClientHandle.GiveObjectControl,
                ClientHandle.ObjectParent,
                ClientHandle.ConnectSync,
                ClientHandle.WeaponFire,
                ClientHandle.PlayerDamage,
                ClientHandle.SosigPickUpItem,
                ClientHandle.SosigPlaceItemIn,
                ClientHandle.SosigDropSlot,
                ClientHandle.SosigHandDrop,
                ClientHandle.SosigConfigure,
                ClientHandle.SosigLinkRegisterWearable,
                ClientHandle.SosigLinkDeRegisterWearable,
                ClientHandle.SosigSetIFF,
                ClientHandle.SosigSetOriginalIFF,
                ClientHandle.SosigLinkDamage,
                ClientHandle.SosigDamageData,
                ClientHandle.SosigWearableDamage,
                ClientHandle.SosigLinkExplodes,
                ClientHandle.SosigDies,
                ClientHandle.SosigClear,
                ClientHandle.SosigSetBodyState,
                ClientHandle.PlaySosigFootStepSound,
                ClientHandle.SosigSpeakState,
                ClientHandle.SosigSetCurrentOrder,
                ClientHandle.SosigVaporize,
                ClientHandle.SosigRequestHitDecal,
                ClientHandle.SosigLinkBreak,
                ClientHandle.SosigLinkSever,
                ClientHandle.RequestUpToDateObjects,
                ClientHandle.PlayerInstance,
                ClientHandle.AddTNHInstance,
                ClientHandle.AddTNHCurrentlyPlaying,
                ClientHandle.RemoveTNHCurrentlyPlaying,
                ClientHandle.SetTNHProgression,
                ClientHandle.SetTNHEquipment,
                ClientHandle.SetTNHHealthMode,
                ClientHandle.SetTNHTargetMode,
                ClientHandle.SetTNHAIDifficulty,
                ClientHandle.SetTNHRadarMode,
                ClientHandle.SetTNHItemSpawnerMode,
                ClientHandle.SetTNHBackpackMode,
                ClientHandle.SetTNHHealthMult,
                ClientHandle.SetTNHSosigGunReload,
                ClientHandle.SetTNHSeed,
                ClientHandle.SetTNHLevelID,
                ClientHandle.AddInstance,
                ClientHandle.SetTNHController,
                ClientHandle.SpectatorHost,
                ClientHandle.TNHPlayerDied,
                ClientHandle.TNHAddTokens,
                ClientHandle.TNHSetLevel,
                ClientHandle.AutoMeaterSetState,
                ClientHandle.AutoMeaterSetBladesActive,
                ClientHandle.AutoMeaterDamage,
                ClientHandle.AutoMeaterFirearmFireShot,
                ClientHandle.AutoMeaterFirearmFireAtWill,
                ClientHandle.AutoMeaterHitZoneDamage,
                ClientHandle.AutoMeaterHitZoneDamageData,
                ClientHandle.TNHSosigKill,
                ClientHandle.TNHHoldPointSystemNode,
                ClientHandle.TNHHoldBeginChallenge,
                ClientHandle.TNHSetPhaseTake,
                ClientHandle.TNHHoldCompletePhase,
                ClientHandle.TNHHoldPointFailOut,
                ClientHandle.TNHSetPhaseComplete,
                ClientHandle.TNHSetPhase,
                ClientHandle.EncryptionDamage,
                ClientHandle.EncryptionDamageData,
                ClientHandle.EncryptionRespawnSubTarg,
                ClientHandle.EncryptionSpawnGrowth,
                ClientHandle.EncryptionInit,
                ClientHandle.EncryptionResetGrowth,
                ClientHandle.EncryptionDisableSubtarg,
                ClientHandle.EncryptionSubDamage,
                ClientHandle.ShatterableCrateDamage,
                ClientHandle.ShatterableCrateDestroy,
                ClientHandle.InitTNHInstances,
                ClientHandle.SosigWeaponFire,
                ClientHandle.SosigWeaponShatter,
                ClientHandle.SosigWeaponDamage,
                ClientHandle.LAPD2019Fire,
                ClientHandle.LAPD2019LoadBattery,
                ClientHandle.LAPD2019ExtractBattery,
                null,
                ClientHandle.AttachableFirearmFire,
                ClientHandle.BreakActionWeaponFire,
                ClientHandle.PlayerIFF,
                ClientHandle.UberShatterableShatter,
                ClientHandle.TNHHoldPointBeginAnalyzing,
                ClientHandle.TNHHoldPointRaiseBarriers,
                ClientHandle.TNHHoldIdentifyEncryption,
                ClientHandle.TNHHoldPointBeginPhase,
                ClientHandle.TNHHoldPointCompleteHold,
                ClientHandle.SosigPriorityIFFChart,
                ClientHandle.LeverActionFirearmFire,
                ClientHandle.RevolvingShotgunFire,
                ClientHandle.DerringerFire,
                ClientHandle.FlintlockWeaponBurnOffOuter,
                ClientHandle.FlintlockWeaponFire,
                ClientHandle.GrappleGunFire,
                ClientHandle.HCBReleaseSled,
                ClientHandle.RemoteMissileDetonate,
                ClientHandle.RemoteMissileDamage,
                ClientHandle.RevolverFire,
                ClientHandle.SingleActionRevolverFire,
                ClientHandle.StingerLauncherFire,
                ClientHandle.StingerMissileDamage,
                ClientHandle.StingerMissileExplode,
                ClientHandle.PinnedGrenadeExplode,
                ClientHandle.FVRGrenadeExplode,
                ClientHandle.ClientDisconnect,
                ClientHandle.ServerClosed,
                null, // UNUSED
                ClientHandle.BangSnapSplode,
                ClientHandle.C4Detonate,
                ClientHandle.ClaymoreMineDetonate,
                ClientHandle.SLAMDetonate,
                ClientHandle.Ping,
                ClientHandle.TNHSetPhaseHold,
                ClientHandle.ShatterableCrateSetHoldingToken,
                ClientHandle.ResetTNH,
                ClientHandle.ReviveTNHPlayer,
                ClientHandle.PlayerColor,
                ClientHandle.ColorByIFF,
                ClientHandle.NameplateMode,
                ClientHandle.RadarMode,
                ClientHandle.RadarColor,
                ClientHandle.TNHInitializer,
                ClientHandle.MaxHealth,
                ClientHandle.FuseIgnite,
                ClientHandle.FuseBoom,
                ClientHandle.MolotovShatter,
                ClientHandle.MolotovDamage,
                ClientHandle.PinnedGrenadePullPin,
                ClientHandle.MagazineAddRound,
                ClientHandle.ClipAddRound,
                ClientHandle.SpeedloaderChamberLoad,
                ClientHandle.RemoteGunChamber,
                ClientHandle.ChamberRound,
                ClientHandle.MagazineLoad,
                ClientHandle.MagazineLoadAttachable,
                ClientHandle.ClipLoad,
                ClientHandle.RevolverCylinderLoad,
                ClientHandle.RevolvingShotgunLoad,
                ClientHandle.GrappleGunLoad,
                ClientHandle.CarlGustafLatchSate,
                ClientHandle.CarlGustafShellSlideSate,
                ClientHandle.TNHHostStartHold,
                ClientHandle.IntegratedFirearmFire,
                ClientHandle.GrappleAttached,
                ClientHandle.TrackedObject,
                ClientHandle.TrackedObjects,
                ClientHandle.ObjectUpdate,
                ClientHandle.DestroyObject,
                ClientHandle.RegisterCustomPacketType,
                ClientHandle.BreakableGlassDamage,
                ClientHandle.WindowShatterSound,
                ClientHandle.SpectatorHostAssignment,
                ClientHandle.GiveUpSpectatorHost,
                ClientHandle.SpectatorHostOrderTNHHost,
                ClientHandle.TNHSpectatorHostReady,
                ClientHandle.SpectatorHostStartTNH,
                ClientHandle.UnassignSpectatorHost,
                ClientHandle.ReactiveSteelTargetDamage,
                ClientHandle.MTUTest,
                ClientHandle.IDConfirm,
                ClientHandle.EnforcePlayerModels,
                ClientHandle.ObjectScene,
                ClientHandle.ObjectInstance,
                ClientHandle.UpdateEncryptionDisplay,
                ClientHandle.EncryptionRespawnSubTargGeo,
                ClientHandle.RoundDamage,
                ClientHandle.RoundSplode,
                ClientHandle.ConnectionComplete,
                ClientHandle.SightFlipperState,
                ClientHandle.SightRaiserState,
                ClientHandle.GatlingGunFire,
                null, // punch through
                ClientHandle.GasCuboidGout,
                ClientHandle.GasCuboidDamage,
                ClientHandle.GasCuboidHandleDamage,
                ClientHandle.GasCuboidDamageHandle,
                ClientHandle.GasCuboidExplode,
                ClientHandle.GasCuboidShatter,
                ClientHandle.FloaterDamage,
                ClientHandle.FloaterCoreDamage,
                ClientHandle.FloaterBeginExploding,
                ClientHandle.FloaterExplode,
                ClientHandle.IrisShatter,
                ClientHandle.IrisSetState,
                ClientHandle.BrutBlockSystemStart,
                ClientHandle.FloaterBeginDefusing,
                ClientHandle.BatchedPackets,
                ClientHandle.NodeInit,
                ClientHandle.NodeFire,
                ClientHandle.HazeDamage,
                ClientHandle.EncryptionFireGun,
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

            objects = new TrackedObjectData[100];

            localObjectCounter = 0;
            waitingLocalObjects.Clear();

            Mod.LogInfo("Initialized client", false);
        }

        public static void AddTrackedObject(TrackedObjectData trackedObject)
        {
            Mod.LogInfo("Client adding new tracked object with waiting local index: " + trackedObject.localWaitingIndex+", tracked ID: "+trackedObject.trackedID+", and type ID: "+trackedObject.typeIdentifier, false);
            TrackedObjectData actualTrackedObject = null;
            // If this is a scene init object the server rejected
            if (trackedObject.trackedID == -2)
            {
                Mod.LogInfo("\tRejected", false);
                actualTrackedObject = waitingLocalObjects[trackedObject.localWaitingIndex];
                waitingLocalObjects.Remove(trackedObject.localWaitingIndex);
                if (actualTrackedObject.physical != null)
                {
                    // Get rid of the object
                    actualTrackedObject.physical.skipDestroyProcessing = true;
                    actualTrackedObject.trackedID = -2;
                    actualTrackedObject.physical.sendDestroy = false;
                    Destroy(actualTrackedObject.physical.gameObject);
                }
                return;
            }

            // Adjust items size to acommodate if necessary
            if (objects.Length <= trackedObject.trackedID)
            {
                IncreaseObjectsSize(trackedObject.trackedID);
            }

            if (trackedObject.controller == GameManager.ID) // We control this object
            {
                Mod.LogInfo("\tController", false);
                // Check if we were waiting for this object's data
                bool notLocalWaiting = true;
                if (waitingLocalObjects.TryGetValue(trackedObject.localWaitingIndex, out actualTrackedObject) && trackedObject.initTracker == GameManager.ID)
                {
                    // Get our object
                    waitingLocalObjects.Remove(trackedObject.localWaitingIndex);

                    // Set its new tracked ID
                    actualTrackedObject.trackedID = trackedObject.trackedID;

                    // Add the object to client global list
                    objects[actualTrackedObject.trackedID] = actualTrackedObject;
                    Mod.LogInfo("\t\tSet in objects array, was local waiting", false);

                    // Add to object tracking list
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

                    actualTrackedObject.OnTrackedIDReceived(trackedObject);

                    notLocalWaiting = false;
                }

                if (notLocalWaiting) // We were not waiting for this data, process by trackedID instead
                {
                    // Check if already have object data
                    TrackedObjectData actual = objects[trackedObject.trackedID];
                    if (actual == null)
                    {
                        // Dont have object data yet, set it
                        objects[trackedObject.trackedID] = trackedObject;
                        Mod.LogInfo("\t\tSet in objects array, was not local waiting, didnt have data yet", false);
                        actual = trackedObject;

                        // Add to object tracking list
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

                        // Add local list
                        trackedObject.localTrackedID = GameManager.objects.Count;
                        GameManager.objects.Add(trackedObject);
                    }
                    else
                    {
                        Mod.LogInfo("\t\tAlready set in objects array", false);
                    }

                    if (actual.physical == null && !actual.awaitingInstantiation &&
                        !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                    {
                        actual.awaitingInstantiation = true;
                        AnvilManager.Run(actual.Instantiate());
                    }
                }
            }
            else // We are not controller
            {
                Mod.LogInfo("\tNot controller", false);
                // We might already have the object
                if (objects[trackedObject.trackedID] != null)
                {
                    Mod.LogInfo("\t\tAlready set in objects array", false);
                    // We could have received this data again from relevant objects, if so, need to instantiate
                    actualTrackedObject = objects[trackedObject.trackedID];
                    if (actualTrackedObject.physical == null && !actualTrackedObject.awaitingInstantiation &&
                        actualTrackedObject.scene.Equals(GameManager.scene) &&
                        actualTrackedObject.instance == GameManager.instance)
                    {
                        actualTrackedObject.awaitingInstantiation = true;
                        AnvilManager.Run(actualTrackedObject.Instantiate());
                    }
                    return;
                }

                trackedObject.localTrackedID = -1;

                // Add the object to client global list
                objects[trackedObject.trackedID] = trackedObject;
                Mod.LogInfo("\t\tSet in objects array, didnt have data yet", false);

                // Add to object tracking list
                if (GameManager.objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if(relevantInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
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
                    TrackedObjectData parentData = objects[trackedObject.parent];

                    if (parentData.children == null)
                    {
                        parentData.children = new List<TrackedObjectData>();
                    }

                    trackedObject.childIndex = parentData.children.Count;
                    parentData.children.Add(trackedObject);
                }

                // Instantiate object if it is identiafiable and in the current scene/instance
                if (!trackedObject.awaitingInstantiation &&
                    trackedObject.IsIdentifiable() &&
                    trackedObject.scene.Equals(GameManager.scene) &&
                    trackedObject.instance == GameManager.instance)
                {
                    trackedObject.awaitingInstantiation = true;
                    AnvilManager.Run(trackedObject.Instantiate());
                }
            }
        }

        private static void IncreaseObjectsSize(int minimum)
        {
            int minCapacity = objects.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            TrackedObjectData[] tempObjects = objects;
            objects = new TrackedObjectData[minCapacity];
            for (int i = 0; i < tempObjects.Length; ++i)
            {
                objects[i] = tempObjects[i];
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect(true, -1);
        }

        public void Disconnect(bool sendToServer, int code)
        {
            if (isConnected)
            {
                isConnected = false;

                bool reconnect = false;
                GameManager.reconnectionInstance = -1;
                switch (code)
                {
                    case -1:
                        Mod.LogWarning("Disconnecting from server due to application quit.");
                        break;
                    case 0:
                        Mod.LogWarning("Disconnecting from server.");
                        break;
                    case 1:
                        Mod.LogWarning("Connection to server lost, end of stream. Attempting to reconnect...");
                        GameManager.reconnectionInstance = GameManager.instance;
                        reconnect = true;
                        break;
                    case 2:
                        Mod.LogWarning("Connection to server lost, TCP forced. Attempting to reconnect...");
                        GameManager.reconnectionInstance = GameManager.instance;
                        reconnect = true;
                        break;
                    case 3:
                        Mod.LogWarning("Connection to server lost, UDP forced. Attempting to reconnect...");
                        GameManager.reconnectionInstance = GameManager.instance;
                        reconnect = true;
                        break;
                    case 4:
                        Mod.LogWarning("Connection to server failed, timed out.");
                        break;
                }

                if (sendToServer) 
                {
                    // Give control of everything we control
                    // HANDLED BY SERVER
                    //GameManager.GiveUpAllControl();

                    ClientSend.ClientDisconnect();
                }

                // On our side take physical control of everything
                GameManager.TakeAllPhysicalControl(true);

                if (tcp.socket != null)
                {
                    tcp.socket.Close();
                }
                tcp.socket = null;
                if (tcp.stream != null)
                {
                    tcp.stream.Close();
                }
                tcp.stream = null;
                tcp.receiveBuffer = null;
                tcp.receivedData = null;
                if(udp.socket != null)
                {
                    udp.socket.Close();
                }
                udp.socket = null;

                ID = -1;
                GameManager.Reset();
                SpecificDisconnect();
                if (OnDisconnect != null)
                {
                    OnDisconnect();
                }
                Mod.Reset();

                isFullyConnected = false;

                if (reconnect)
                {
                    Mod.OnConnectClicked(null);
                }

                tickTimer.Stop();
                tickTimer.Elapsed -= Tick;
            }
        }

        private void SpecificDisconnect()
        {
            if (Mod.currentlyPlayingTNH) // TNH_Manager was set to null and we are currently playing
            {
                Mod.currentlyPlayingTNH = false;
                Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, GameManager.ID);
            }
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
            {
                GM.CurrentPlayerBody.EnableHands();
            }
            Mod.currentTNHInstancePlayers = null;
            Mod.joinTNHInstances = null;
            if(Mod.TNHMenu != null)
            {
                Destroy(Mod.TNHMenu);
                Mod.TNHMenu = null;
            }
        }
    }
}
