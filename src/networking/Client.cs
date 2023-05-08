using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace H3MP.Networking
{
    internal class Client : MonoBehaviour
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
        public bool gotWelcome = false;
        public int pingAttemptCounter = 0;
        private delegate void PacketHandler(Packet packet);
        private static PacketHandler[] packetHandlers;
        public static Dictionary<string, int> synchronizedScenes;
        public static TrackedObjectData[] objects; // All tracked objects, regardless of whos control they are under

        public static uint localObjectCounter = 0;
        public static Dictionary<uint, TrackedObjectData> waitingLocalObjects = new Dictionary<uint, TrackedObjectData>();

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
                Mod.LogInfo("Making connection to " + singleton.IP + ":" + singleton.port, false);
                socket.BeginConnect(singleton.IP, singleton.port, ConnectCallback, socket);
                Mod.LogInfo("connection begun", false);
            }

            private void ConnectCallback(IAsyncResult result)
            {
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
                try
                {
                    int byteLength = stream.EndRead(result);
                    if (byteLength == 0)
                    {
                        singleton.Disconnect(true, 1);
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception)
                {
                    Disconnect(2);
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if(packetLength <= 0)
                    {
                        return true;
                    }
                }

                while(packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        if (singleton.isConnected)
                        {
                            using (Packet packet = new Packet(packetBytes))
                            {
                                int packetID = packet.ReadInt();
#if DEBUG
                                if (Input.GetKey(KeyCode.PageDown))
                                {
                                    Mod.LogInfo("\tHandling TCP packet: " + packetID);
                                }
#endif
                                try
                                {
                                    if (singleton.ID >= 0 || packetID == 1)
                                    {
                                        packetHandlers[packetID](packet);
                                    }
                                }
                                catch(IndexOutOfRangeException ex)
                                {
                                    Mod.LogError("Client TCP received packet with ID: "+packetID+ " as ServerPackets: " + ((ServerPackets)packetID).ToString()+":\n"+ ex.StackTrace);
                                }
                            }
                        }
                    });

                    packetLength = 0;

                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if(packetLength <= 1)
                {
                    return true;
                }

                return false;
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

            private void HandleData(byte[] data)
            {
                using(Packet packet = new Packet(data))
                {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    if (singleton.isConnected)
                    {
                        using (Packet packet = new Packet(data))
                        {
                            int packetID = packet.ReadInt();
#if DEBUG
                            if (Input.GetKey(KeyCode.PageDown))
                            {
                                Mod.LogInfo("\tHandling UDP packet: " + packetID);
                            }
#endif
                            packetHandlers[packetID](packet);
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
                null,
                ClientHandle.Welcome,
                ClientHandle.SpawnPlayer,
                ClientHandle.PlayerState,
                ClientHandle.PlayerScene,
                ClientHandle.AddNonSyncScene,
                ClientHandle.TrackedItems,
                ClientHandle.TrackedItem,
                ClientHandle.ShatterableCrateSetHoldingHealth,
                ClientHandle.GiveControl,
                ClientHandle.DestroyItem,
                ClientHandle.ItemParent,
                ClientHandle.ConnectSync,
                ClientHandle.WeaponFire,
                ClientHandle.PlayerDamage,
                ClientHandle.TrackedSosig,
                ClientHandle.TrackedSosigs,
                ClientHandle.GiveSosigControl,
                ClientHandle.DestroySosig,
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
                ClientHandle.TrackedAutoMeater,
                ClientHandle.TrackedAutoMeaters,
                ClientHandle.DestroyAutoMeater,
                ClientHandle.GiveAutoMeaterControl,
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
                ClientHandle.TrackedEncryptions,
                ClientHandle.TrackedEncryption,
                ClientHandle.GiveEncryptionControl,
                ClientHandle.DestroyEncryption,
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
                ClientHandle.MinigunFire,
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
                ClientHandle.InitConnectionData,
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
                ClientHandle.ItemUpdate,
                ClientHandle.TNHHostStartHold,
                ClientHandle.IntegratedFirearmFire,
                ClientHandle.GrappleAttached,
                ClientHandle.TrackedObject,
                ClientHandle.TrackedObjects,
                ClientHandle.ObjectUpdate,
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

            objects = new TrackedObjectData[100];

            items = new TrackedItemData[100];

            sosigs = new TrackedSosigData[100];

            autoMeaters = new TrackedAutoMeaterData[100];

            encryptions = new TrackedEncryptionData[100];

            localObjectCounter = 0;
            waitingLocalObjects.Clear();
            localItemCounter = 0;
            waitingLocalItems.Clear();
            localSosigCounter = 0;
            waitingLocalSosigs.Clear();
            localAutoMeaterCounter = 0;
            waitingLocalAutoMeaters.Clear();
            localEncryptionCounter = 0;
            waitingLocalEncryptions.Clear();

            Mod.LogInfo("Initialized client", false);
        }

        public static void AddTrackedObject(TrackedObjectData trackedObject)
        {
            TrackedObjectData actualTrackedObject = null;
            // If this is a scene init object the server rejected
            if (trackedObject.trackedID == -2)
            {
                actualTrackedObject = waitingLocalObjects[trackedObject.localWaitingIndex];
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

                    actualTrackedObject.OnTrackedIDReceived();

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
                // We might already have the object
                if (objects[trackedObject.trackedID] != null)
                {
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

        public static void AddTrackedItem(TrackedItemData trackedItem)
        {
            TrackedItemData actualTrackedItem = null;
            // If this is a scene init object the server rejected
            if (trackedItem.trackedID == -2)
            {
                actualTrackedItem = waitingLocalItems[trackedItem.localWaitingIndex];
                if (actualTrackedItem.physicalItem != null)
                {
                    // Get rid of the item
                    actualTrackedItem.physicalItem.skipDestroyProcessing = true;
                    actualTrackedItem.trackedID = -2;
                    actualTrackedItem.physicalItem.sendDestroy = false;
                    Destroy(actualTrackedItem.physicalItem.gameObject);
                }
                return;
            }

            // Adjust items size to acommodate if necessary
            if (items.Length <= trackedItem.trackedID)
            {
                IncreaseItemsSize(trackedItem.trackedID);
            }

            //Problem: When joining TNH game that has already been init, but we are instance host, we are given TNH control and control of sosigs
            // Once we arrive in the scene, we are sent relevant sosigs, the ones we are already in control of
            // If the sosig has a local waiting index that we are not waiting for, instantiate the sosig if we have its data
            // If the local waiting index matches one of ours, it could be one we have initially tracked or one from snother player that just happened to match on of ours
            //  If one we have initially tracked, get the ID and process as normal
            //  If one from someone else instantiate if we have the data since we are not in control
            if (trackedItem.controller == Client.singleton.ID)
            {
                if (waitingLocalItems.TryGetValue(trackedItem.localWaitingIndex, out actualTrackedItem))
                {
                    // Check if we were init tracker because we might have a different item with the same local waiting index in our side
                    if (trackedItem.initTracker == GameManager.ID)
                    {
                        // Get our item
                        waitingLocalItems.Remove(trackedItem.localWaitingIndex);

                        // Set its new tracked ID
                        actualTrackedItem.trackedID = trackedItem.trackedID;

                        // Add the item to client global list
                        items[actualTrackedItem.trackedID] = actualTrackedItem;

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

                        actualTrackedItem.OnTrackedIDReceived();
                    }
                    else
                    {
                        TrackedItemData actual = items[trackedItem.trackedID];
                        if (actual == null)
                        {
                            items[trackedItem.trackedID] = trackedItem;
                            actual = trackedItem;

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

                            // Add local list
                            trackedItem.localTrackedID = GameManager.items.Count;
                            GameManager.items.Add(trackedItem);
                        }

                        if (actual.physicalItem == null && !actual.awaitingInstantiation &&
                            !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                        {
                            actual.awaitingInstantiation = true;
                            AnvilManager.Run(actual.Instantiate());
                        }
                    }
                }
                else
                {
                    TrackedItemData actual = items[trackedItem.trackedID];
                    if (actual == null)
                    {
                        items[trackedItem.trackedID] = trackedItem;
                        actual = trackedItem;

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

                        // Add local list
                        trackedItem.localTrackedID = GameManager.items.Count;
                        GameManager.items.Add(trackedItem);
                    }

                    if (actual.physicalItem == null && !actual.awaitingInstantiation &&
                        !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                    {
                        actual.awaitingInstantiation = true;
                        AnvilManager.Run(actual.Instantiate());
                    }
                }
            }
            else
            {
                // We might already have the object
                if (items[trackedItem.trackedID] != null)
                {
                    actualTrackedItem = items[trackedItem.trackedID];
                    if (!actualTrackedItem.itemID.Equals(trackedItem.itemID))
                    {
                        if (actualTrackedItem.physicalItem != null)
                        {
                            actualTrackedItem.physicalItem.sendDestroy = false;
                            DestroyImmediate(actualTrackedItem.physicalItem.gameObject);
                        }
                        actualTrackedItem.awaitingInstantiation = false;
                    }
                    else
                    {
                        // If we got sent this when it initialy got tracked, we would still need to instantiate it when we 
                        // receive it from relevant objects
                        if (actualTrackedItem.physicalItem == null && !actualTrackedItem.awaitingInstantiation &&
                            actualTrackedItem.scene.Equals(GameManager.scene) &&
                            actualTrackedItem.instance == GameManager.instance)
                        {
                            actualTrackedItem.awaitingInstantiation = true;
                            AnvilManager.Run(actualTrackedItem.Instantiate());
                        }
                        return;
                    }
                }

                trackedItem.localTrackedID = -1;

                // Add the item to client global list
                items[trackedItem.trackedID] = trackedItem;

                // Add to item tracking list
                if (GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> relevantInstances))
                {
                    if(relevantInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
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
                    TrackedItemData parentData = items[trackedItem.parent];

                    if (parentData.children == null)
                    {
                        parentData.children = new List<TrackedItemData>();
                    }

                    trackedItem.childIndex = parentData.children.Count;
                    parentData.children.Add(trackedItem);
                }

                // Instantiate item if it is identiafiable and in the current scene/instance
                if (!trackedItem.awaitingInstantiation && 
                    GameManager.IsItemIdentifiable(trackedItem) &&
                    trackedItem.scene.Equals(GameManager.scene) &&
                    trackedItem.instance == GameManager.instance)
                {
                    trackedItem.awaitingInstantiation = true;
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }
        }

        public static void AddTrackedSosig(TrackedSosigData trackedSosig)
        {
            Mod.LogInfo("Received full sosig with trackedID: "+trackedSosig.trackedID, false);
            TrackedSosigData actualTrackedSosig = null;
            // If this is a scene init object the server rejected
            if (trackedSosig.trackedID == -2)
            {
                actualTrackedSosig = waitingLocalSosigs[trackedSosig.localWaitingIndex];
                if (actualTrackedSosig.physicalObject != null)
                {
                    // Get rid of the object
                    actualTrackedSosig.physicalObject.skipDestroyProcessing = true;
                    actualTrackedSosig.trackedID = -2;
                    actualTrackedSosig.physicalObject.sendDestroy = false;
                    Destroy(actualTrackedSosig.physicalObject.gameObject);
                }
                return;
            }

            // Adjust sosigs size to acommodate if necessary
            if (sosigs.Length <= trackedSosig.trackedID)
            {
                IncreaseSosigsSize(trackedSosig.trackedID);
            }

            if (trackedSosig.controller == Client.singleton.ID)
            {
                Mod.LogInfo("\tWe are controller, tracked id: "+ trackedSosig.trackedID+", have actual?: "+(sosigs[trackedSosig.trackedID] != null), false);
                if (waitingLocalSosigs.TryGetValue(trackedSosig.localWaitingIndex, out actualTrackedSosig))
                {
                    if (trackedSosig.initTracker == GameManager.ID)
                    {
                        // Get our sosig
                        waitingLocalSosigs.Remove(trackedSosig.localWaitingIndex);

                        // Set its new tracked ID
                        actualTrackedSosig.trackedID = trackedSosig.trackedID;

                        // Add the sosig to client global list
                        sosigs[trackedSosig.trackedID] = actualTrackedSosig;

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

                        // Send queued up orders
                        actualTrackedSosig.OnTrackedIDReceived();

                        // Only send latest data if not destroyed
                        if (sosigs[trackedSosig.trackedID] != null)
                        {
                            sosigs[trackedSosig.trackedID].Update(true);

                            // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                            ClientSend.TrackedSosig(sosigs[trackedSosig.trackedID]);
                        }
                    }
                    else
                    {
                        TrackedSosigData actual = sosigs[trackedSosig.trackedID];
                        if (actual == null)
                        {
                            sosigs[trackedSosig.trackedID] = trackedSosig;
                            actual = trackedSosig;

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

                            trackedSosig.localTrackedID = GameManager.sosigs.Count;
                            GameManager.sosigs.Add(trackedSosig);
                        }

                        if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                            !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                        {
                            actual.awaitingInstantiation = true;
                            AnvilManager.Run(actual.Instantiate());
                        }
                    }
                }
                else
                {
                    TrackedSosigData actual = sosigs[trackedSosig.trackedID];
                    if (actual == null)
                    {
                        sosigs[trackedSosig.trackedID] = trackedSosig;
                        actual = trackedSosig;

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

                        trackedSosig.localTrackedID = GameManager.sosigs.Count;
                        GameManager.sosigs.Add(trackedSosig);
                    }

                    if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                        !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                    {
                        actual.awaitingInstantiation = true;
                        AnvilManager.Run(actual.Instantiate());
                    }
                }
            }
            else
            {
                Mod.LogInfo("\tWe are not controller, tracked id: " + trackedSosig.trackedID + ", have actual?: " + (sosigs[trackedSosig.trackedID] != null), false);
                if (sosigs[trackedSosig.trackedID] == null)
                {
                    trackedSosig.localTrackedID = -1;

                    // Add the sosig to client global list
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

                    // Instantiate sosig if it is in the current scene
                    if (!trackedSosig.awaitingInstantiation &&
                        trackedSosig.scene.Equals(GameManager.scene) &&
                        trackedSosig.instance == GameManager.instance)
                    {
                        trackedSosig.awaitingInstantiation = true;
                        AnvilManager.Run(trackedSosig.Instantiate());
                    }
                }
                else // This is an initial update sosig data
                {
                    TrackedSosigData trackedSosigData = sosigs[trackedSosig.trackedID];

                    trackedSosigData.Update(trackedSosig, true);

                    // Instantiate sosig if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with sosigs we already have the data for
                    if (trackedSosigData.physicalObject == null && !trackedSosigData.awaitingInstantiation)
                    {
                        if (trackedSosig.scene.Equals(GameManager.scene) &&
                            trackedSosig.instance == GameManager.instance)
                        {
                            trackedSosigData.awaitingInstantiation = true;
                            AnvilManager.Run(trackedSosigData.Instantiate());
                        }
                    }
                }
            }
        }

        public static void AddTrackedAutoMeater(TrackedAutoMeaterData trackedAutoMeater)
        {
            TrackedAutoMeaterData actualTrackedAutoMeater = null;
            // If this is a scene init object the server rejected
            if (trackedAutoMeater.trackedID == -2)
            {
                actualTrackedAutoMeater = waitingLocalAutoMeaters[trackedAutoMeater.localWaitingIndex];
                if (actualTrackedAutoMeater.physicalObject != null)
                {
                    // Get rid of the object
                    actualTrackedAutoMeater.physicalObject.skipDestroyProcessing = true;
                    actualTrackedAutoMeater.trackedID = -2;
                    actualTrackedAutoMeater.physicalObject.sendDestroy = false;
                    Destroy(actualTrackedAutoMeater.physicalObject.gameObject);
                }
                return;
            }

            // Adjust AutoMeaters size to acommodate if necessary
            if (autoMeaters.Length <= trackedAutoMeater.trackedID)
            {
                IncreaseAutoMeatersSize(trackedAutoMeater.trackedID);
            }

            if (trackedAutoMeater.controller == Client.singleton.ID)
            {
                if (waitingLocalAutoMeaters.TryGetValue(trackedAutoMeater.localWaitingIndex, out actualTrackedAutoMeater))
                {
                    if (trackedAutoMeater.initTracker == GameManager.ID)
                    {
                        // Get our autoMeater.
                        waitingLocalAutoMeaters.Remove(trackedAutoMeater.localWaitingIndex);

                        // Set its tracked ID
                        actualTrackedAutoMeater.trackedID = trackedAutoMeater.trackedID;

                        // Add the AutoMeater to client global list
                        autoMeaters[trackedAutoMeater.trackedID] = actualTrackedAutoMeater;

                        // Add to autoMeater tracking list
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

                        // Send queued up orders
                        actualTrackedAutoMeater.OnTrackedIDReceived();
                    }
                    else
                    {
                        TrackedAutoMeaterData actual = autoMeaters[trackedAutoMeater.trackedID];
                        if (actual == null)
                        {
                            autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;
                            actual = trackedAutoMeater;

                            // Add to autoMeater tracking list
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

                            trackedAutoMeater.localTrackedID = GameManager.autoMeaters.Count;
                            GameManager.autoMeaters.Add(trackedAutoMeater);
                        }

                        if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                            !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                        {
                            actual.awaitingInstantiation = true;
                            AnvilManager.Run(actual.Instantiate());
                        }
                    }
                }
                else
                {
                    TrackedAutoMeaterData actual = autoMeaters[trackedAutoMeater.trackedID];
                    if (actual == null)
                    {
                        autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;
                        actual = trackedAutoMeater;

                        // Add to autoMeater tracking list
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

                        trackedAutoMeater.localTrackedID = GameManager.autoMeaters.Count;
                        GameManager.autoMeaters.Add(trackedAutoMeater);
                    }

                    if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                        !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                    {
                        actual.awaitingInstantiation = true;
                        AnvilManager.Run(actual.Instantiate());
                    }
                }
            }
            else
            {
                // We might already have the object
                if (autoMeaters[trackedAutoMeater.trackedID] != null)
                {
                    actualTrackedAutoMeater = autoMeaters[trackedAutoMeater.trackedID];
                    // If we got sent this when it initialy got tracked, we would still need to instantiate it when we 
                    // receive it from relevant objects
                    if (actualTrackedAutoMeater.physicalObject == null && !actualTrackedAutoMeater.awaitingInstantiation &&
                        actualTrackedAutoMeater.scene.Equals(GameManager.scene) &&
                        actualTrackedAutoMeater.instance == GameManager.instance)
                    {
                        actualTrackedAutoMeater.awaitingInstantiation = true;
                        AnvilManager.Run(actualTrackedAutoMeater.Instantiate());
                    }
                    return;
                }

                trackedAutoMeater.localTrackedID = -1;

                // Add the AutoMeater to client global list
                autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;

                // Add to autoMeater tracking list
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

                // Instantiate AutoMeater if it is in the current scene
                if (!trackedAutoMeater.awaitingInstantiation &&
                    trackedAutoMeater.scene.Equals(GameManager.scene) &&
                    trackedAutoMeater.instance == GameManager.instance)
                {
                    trackedAutoMeater.awaitingInstantiation = true;
                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                }
            }
        }

        public static void AddTrackedEncryption(TrackedEncryptionData trackedEncryption)
        {
            TrackedEncryptionData actualTrackedEncryption = null;
            // If this is a scene init object the server rejected
            if (trackedEncryption.trackedID == -2)
            {
                actualTrackedEncryption = waitingLocalEncryptions[trackedEncryption.localWaitingIndex];
                if (actualTrackedEncryption.physicalObject != null)
                {
                    // Get rid of the object
                    actualTrackedEncryption.physicalObject.skipDestroyProcessing = true;
                    actualTrackedEncryption.trackedID = -2;
                    actualTrackedEncryption.physicalObject.sendDestroy = false;
                    Destroy(actualTrackedEncryption.physicalObject.gameObject);
                }
                return;
            }

            // Adjust Encryptions size to acommodate if necessary
            if (encryptions.Length <= trackedEncryption.trackedID)
            {
                IncreaseEncryptionsSize(trackedEncryption.trackedID);
            }

            if (trackedEncryption.controller == Client.singleton.ID)
            {
                if (waitingLocalEncryptions.TryGetValue(trackedEncryption.localWaitingIndex, out actualTrackedEncryption))
                {
                    if (trackedEncryption.initTracker == GameManager.ID)
                    {
                        // Get our encryption.
                        waitingLocalEncryptions.Remove(trackedEncryption.localWaitingIndex);

                        // Set tis tracked ID
                        actualTrackedEncryption.trackedID = trackedEncryption.trackedID;

                        // Add the Encryption to client global list
                        encryptions[trackedEncryption.trackedID] = actualTrackedEncryption;

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

                        // Send queued up orders
                        actualTrackedEncryption.OnTrackedIDReceived();

                        // Only send latest data if not destroyed
                        if (encryptions[trackedEncryption.trackedID] != null)
                        {
                            encryptions[trackedEncryption.trackedID].Update(true);

                            // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                            ClientSend.TrackedEncryption(encryptions[trackedEncryption.trackedID]);
                        }
                    }
                    else
                    {
                        TrackedEncryptionData actual = encryptions[trackedEncryption.trackedID];
                        if (actual == null)
                        {
                            encryptions[trackedEncryption.trackedID] = trackedEncryption;
                            actual = trackedEncryption;

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

                            trackedEncryption.localTrackedID = GameManager.encryptions.Count;
                            GameManager.encryptions.Add(trackedEncryption);
                        }

                        if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                            !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                        {
                            actual.awaitingInstantiation = true;
                            AnvilManager.Run(actual.Instantiate());
                        }
                    }
                }
                else
                {
                    TrackedEncryptionData actual = encryptions[trackedEncryption.trackedID];
                    if (actual == null)
                    {
                        encryptions[trackedEncryption.trackedID] = trackedEncryption;
                        actual = trackedEncryption;

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

                        trackedEncryption.localTrackedID = GameManager.encryptions.Count;
                        GameManager.encryptions.Add(trackedEncryption);
                    }

                    if (actual.physicalObject == null && !actual.awaitingInstantiation &&
                        !GameManager.sceneLoading && actual.scene.Equals(GameManager.scene) && actual.instance == GameManager.instance)
                    {
                        actual.awaitingInstantiation = true;
                        AnvilManager.Run(actual.Instantiate());
                    }
                }
            }
            else
            {
                if (encryptions[trackedEncryption.trackedID] == null)
                {
                    trackedEncryption.localTrackedID = -1;

                    // Add the Encryption to client global list
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

                    // Instantiate Encryption if it is in the current scene
                    if (!trackedEncryption.awaitingInstantiation &&
                        trackedEncryption.scene.Equals(GameManager.scene) && 
                        trackedEncryption.instance == GameManager.instance)
                    {
                        trackedEncryption.awaitingInstantiation = true;
                        AnvilManager.Run(trackedEncryption.Instantiate());
                    }
                }
                else // This is an initial update encryption data
                {
                    TrackedEncryptionData trackedEncryptionData = encryptions[trackedEncryption.trackedID];

                    trackedEncryptionData.Update(trackedEncryption, true);

                    // Instantiate Encryption if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with encryptions we already have the data for
                    if (trackedEncryptionData.physicalObject == null)
                    {
                        if (!trackedEncryptionData.awaitingInstantiation && 
                            trackedEncryption.scene.Equals(GameManager.scene) &&
                            trackedEncryption.instance == GameManager.instance)
                        {
                            trackedEncryptionData.awaitingInstantiation = true;
                            AnvilManager.Run(trackedEncryptionData.Instantiate());
                        }
                    }
                }
            }
        }

        private static void IncreaseItemsSize(int minimum)
        {
            int minCapacity = items.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            TrackedItemData[] tempItems = items;
            items = new TrackedItemData[minCapacity];
            for (int i = 0; i < tempItems.Length; ++i)
            {
                items[i] = tempItems[i];
            }
        }

        private static void IncreaseSosigsSize(int minimum)
        {
            int minCapacity = sosigs.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new TrackedSosigData[minCapacity];
            for (int i = 0; i < tempSosigs.Length; ++i)
            {
                sosigs[i] = tempSosigs[i];
            }
        }

        private static void IncreaseAutoMeatersSize(int minimum)
        {
            int minCapacity = autoMeaters.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            TrackedAutoMeaterData[] tempAutoMeaters = autoMeaters;
            autoMeaters = new TrackedAutoMeaterData[minCapacity];
            for (int i = 0; i < tempAutoMeaters.Length; ++i)
            {
                autoMeaters[i] = tempAutoMeaters[i];
            }
        }

        private static void IncreaseEncryptionsSize(int minimum)
        {
            int minCapacity = encryptions.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            TrackedEncryptionData[] tempEncryptions = encryptions;
            encryptions = new TrackedEncryptionData[minCapacity];
            for (int i = 0; i < tempEncryptions.Length; ++i)
            {
                encryptions[i] = tempEncryptions[i];
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

                switch (code)
                {
                    case 0:
                        Mod.LogInfo("Disconnecting from server.", false);
                        break;
                    case 1:
                        Mod.LogInfo("Disconnecting from server, end of stream.", false);
                        break;
                    case 2:
                        Mod.LogInfo("Disconnecting from server, TCP forced.", false);
                        break;
                    case 3:
                        Mod.LogInfo("Disconnecting from server, UDP forced.", false);
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
                Mod.Reset();
            }
        }

        // MOD: This will be called after disconnection to reset specific fields
        //      For example, here we deal with current TNH data
        //      If your mod has some H3MP dependent data that you want to get rid of when you disconnect from a server, do it here
        private void SpecificDisconnect()
        {
            if (Mod.currentlyPlayingTNH) // TNH_Manager was set to null and we are currently playing
            {
                Mod.currentlyPlayingTNH = false;
                Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, GameManager.ID);
            }
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
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
