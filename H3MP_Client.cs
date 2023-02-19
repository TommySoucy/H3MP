using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace H3MP
{
    internal class H3MP_Client : MonoBehaviour
    {
        private static H3MP_Client _singleton;
        public static H3MP_Client singleton
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
                    Mod.LogInfo($"{nameof(H3MP_Client)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public static int dataBufferSize = 4096;

        public string IP;
        public ushort port;
        public int ID;
        public TCP tcp;
        public UDP udp;

        private bool isConnected = false;
        private delegate void PacketHandler(H3MP_Packet packet);
        private static PacketHandler[] packetHandlers;
        public static Dictionary<string, int> synchronizedScenes;
        public static H3MP_TrackedItemData[] items; // All tracked items, regardless of whos control they are under
        public static H3MP_TrackedSosigData[] sosigs; // All tracked Sosigs, regardless of whos control they are under
        public static H3MP_TrackedAutoMeaterData[] autoMeaters; // All tracked AutoMeaters, regardless of whos control they are under
        public static H3MP_TrackedEncryptionData[] encryptions; // All tracked TNH_EncryptionTarget, regardless of whos control they are under

        public static uint localItemCounter = 0;
        public static Dictionary<uint, H3MP_TrackedItemData> waitingLocalItems = new Dictionary<uint, H3MP_TrackedItemData>();
        public static uint localSosigCounter = 0;
        public static Dictionary<uint, H3MP_TrackedSosigData> waitingLocalSosigs = new Dictionary<uint, H3MP_TrackedSosigData>();
        public static uint localAutoMeaterCounter = 0;
        public static Dictionary<uint, H3MP_TrackedAutoMeaterData> waitingLocalAutoMeaters = new Dictionary<uint, H3MP_TrackedAutoMeaterData>();
        public static uint localEncryptionCounter = 0;
        public static Dictionary<uint, H3MP_TrackedEncryptionData> waitingLocalEncryptions = new Dictionary<uint, H3MP_TrackedEncryptionData>();

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
            private H3MP_Packet receivedData;
            public byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];
                Mod.LogInfo("Making connection to " + singleton.IP + ":" + singleton.port);
                socket.BeginConnect(singleton.IP, singleton.port, ConnectCallback, socket);
                Mod.LogInfo("connection begun");
            }

            private void ConnectCallback(IAsyncResult result)
            {
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new H3MP_Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            public void SendData(H3MP_Packet packet)
            {
                try
                {
                    if(socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Mod.LogInfo($"Error sending data to server via TCP: {ex}");
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
                    H3MP_ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using(H3MP_Packet packet = new H3MP_Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
                            packetHandlers[packetID](packet);
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

                using(H3MP_Packet packet = new H3MP_Packet())
                {
                    SendData(packet);
                }
            }

            public void SendData(H3MP_Packet packet)
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
                    Mod.LogInfo($"Error sending UDP data {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
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

            private void HandleData(byte[] data)
            {
                using(H3MP_Packet packet = new H3MP_Packet(data))
                {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                H3MP_ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(H3MP_Packet packet = new H3MP_Packet(data))
                    {
                        int packetID = packet.ReadInt();
                        packetHandlers[packetID](packet);
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
                H3MP_ClientHandle.Welcome,
                H3MP_ClientHandle.SpawnPlayer,
                H3MP_ClientHandle.PlayerState,
                H3MP_ClientHandle.PlayerScene,
                H3MP_ClientHandle.AddSyncScene,
                H3MP_ClientHandle.TrackedItems,
                H3MP_ClientHandle.TrackedItem,
                null, // Unused ServerPackets.takeControl
                H3MP_ClientHandle.GiveControl,
                H3MP_ClientHandle.DestroyItem,
                H3MP_ClientHandle.ItemParent,
                H3MP_ClientHandle.ConnectSync,
                H3MP_ClientHandle.WeaponFire,
                H3MP_ClientHandle.PlayerDamage,
                H3MP_ClientHandle.TrackedSosig,
                H3MP_ClientHandle.TrackedSosigs,
                H3MP_ClientHandle.GiveSosigControl,
                H3MP_ClientHandle.DestroySosig,
                H3MP_ClientHandle.SosigPickUpItem,
                H3MP_ClientHandle.SosigPlaceItemIn,
                H3MP_ClientHandle.SosigDropSlot,
                H3MP_ClientHandle.SosigHandDrop,
                H3MP_ClientHandle.SosigConfigure,
                H3MP_ClientHandle.SosigLinkRegisterWearable,
                H3MP_ClientHandle.SosigLinkDeRegisterWearable,
                H3MP_ClientHandle.SosigSetIFF,
                H3MP_ClientHandle.SosigSetOriginalIFF,
                H3MP_ClientHandle.SosigLinkDamage,
                H3MP_ClientHandle.SosigDamageData,
                H3MP_ClientHandle.SosigWearableDamage,
                H3MP_ClientHandle.SosigLinkExplodes,
                H3MP_ClientHandle.SosigDies,
                H3MP_ClientHandle.SosigClear,
                H3MP_ClientHandle.SosigSetBodyState,
                H3MP_ClientHandle.PlaySosigFootStepSound,
                H3MP_ClientHandle.SosigSpeakState,
                H3MP_ClientHandle.SosigSetCurrentOrder,
                H3MP_ClientHandle.SosigVaporize,
                H3MP_ClientHandle.SosigRequestHitDecal,
                H3MP_ClientHandle.SosigLinkBreak,
                H3MP_ClientHandle.SosigLinkSever,
                H3MP_ClientHandle.RequestUpToDateObjects,
                H3MP_ClientHandle.PlayerInstance,
                H3MP_ClientHandle.AddTNHInstance,
                H3MP_ClientHandle.AddTNHCurrentlyPlaying,
                H3MP_ClientHandle.RemoveTNHCurrentlyPlaying,
                H3MP_ClientHandle.SetTNHProgression,
                H3MP_ClientHandle.SetTNHEquipment,
                H3MP_ClientHandle.SetTNHHealthMode,
                H3MP_ClientHandle.SetTNHTargetMode,
                H3MP_ClientHandle.SetTNHAIDifficulty,
                H3MP_ClientHandle.SetTNHRadarMode,
                H3MP_ClientHandle.SetTNHItemSpawnerMode,
                H3MP_ClientHandle.SetTNHBackpackMode,
                H3MP_ClientHandle.SetTNHHealthMult,
                H3MP_ClientHandle.SetTNHSosigGunReload,
                H3MP_ClientHandle.SetTNHSeed,
                H3MP_ClientHandle.SetTNHLevelIndex,
                H3MP_ClientHandle.AddInstance,
                H3MP_ClientHandle.SetTNHController,
                H3MP_ClientHandle.SpectatorHost,
                H3MP_ClientHandle.TNHPlayerDied,
                H3MP_ClientHandle.TNHAddTokens,
                H3MP_ClientHandle.TNHSetLevel,
                H3MP_ClientHandle.TrackedAutoMeater,
                H3MP_ClientHandle.TrackedAutoMeaters,
                H3MP_ClientHandle.DestroyAutoMeater,
                H3MP_ClientHandle.GiveAutoMeaterControl,
                H3MP_ClientHandle.AutoMeaterSetState,
                H3MP_ClientHandle.AutoMeaterSetBladesActive,
                H3MP_ClientHandle.AutoMeaterDamage,
                H3MP_ClientHandle.AutoMeaterFirearmFireShot,
                H3MP_ClientHandle.AutoMeaterFirearmFireAtWill,
                H3MP_ClientHandle.AutoMeaterHitZoneDamage,
                H3MP_ClientHandle.AutoMeaterHitZoneDamageData,
                H3MP_ClientHandle.TNHSosigKill,
                H3MP_ClientHandle.TNHHoldPointSystemNode,
                H3MP_ClientHandle.TNHHoldBeginChallenge,
                H3MP_ClientHandle.TNHSetPhaseTake,
                H3MP_ClientHandle.TNHHoldCompletePhase,
                H3MP_ClientHandle.TNHHoldPointFailOut,
                H3MP_ClientHandle.TNHSetPhaseComplete,
                H3MP_ClientHandle.TNHSetPhase,
                H3MP_ClientHandle.TrackedEncryptions,
                H3MP_ClientHandle.TrackedEncryption,
                H3MP_ClientHandle.GiveEncryptionControl,
                H3MP_ClientHandle.DestroyEncryption,
                H3MP_ClientHandle.EncryptionDamage,
                H3MP_ClientHandle.EncryptionDamageData,
                H3MP_ClientHandle.EncryptionRespawnSubTarg,
                H3MP_ClientHandle.EncryptionSpawnGrowth,
                H3MP_ClientHandle.EncryptionInit,
                H3MP_ClientHandle.EncryptionResetGrowth,
                H3MP_ClientHandle.EncryptionDisableSubtarg,
                H3MP_ClientHandle.EncryptionSubDamage,
                H3MP_ClientHandle.ShatterableCrateDamage,
                H3MP_ClientHandle.ShatterableCrateDestroy,
                H3MP_ClientHandle.InitTNHInstances,
                H3MP_ClientHandle.SosigWeaponFire,
                H3MP_ClientHandle.SosigWeaponShatter,
                H3MP_ClientHandle.SosigWeaponDamage,
                H3MP_ClientHandle.LAPD2019Fire,
                H3MP_ClientHandle.LAPD2019LoadBattery,
                H3MP_ClientHandle.LAPD2019ExtractBattery,
                H3MP_ClientHandle.MinigunFire,
                H3MP_ClientHandle.AttachableFirearmFire,
                H3MP_ClientHandle.BreakActionWeaponFire,
                H3MP_ClientHandle.PlayerIFF,
                H3MP_ClientHandle.UberShatterableShatter,
                H3MP_ClientHandle.TNHHoldPointBeginAnalyzing,
                H3MP_ClientHandle.TNHHoldPointRaiseBarriers,
                H3MP_ClientHandle.TNHHoldIdentifyEncryption,
                H3MP_ClientHandle.TNHHoldPointBeginPhase,
                H3MP_ClientHandle.TNHHoldPointCompleteHold,
                H3MP_ClientHandle.SosigPriorityIFFChart,
                H3MP_ClientHandle.LeverActionFirearmFire,
                H3MP_ClientHandle.RevolvingShotgunFire,
                H3MP_ClientHandle.DerringerFire,
                H3MP_ClientHandle.FlintlockWeaponBurnOffOuter,
                H3MP_ClientHandle.FlintlockWeaponFire,
                H3MP_ClientHandle.GrappleGunFire,
                H3MP_ClientHandle.HCBReleaseSled,
                H3MP_ClientHandle.RemoteMissileDetonate,
                H3MP_ClientHandle.RemoteMissileDamage,
                H3MP_ClientHandle.RevolverFire,
                H3MP_ClientHandle.SingleActionRevolverFire,
                H3MP_ClientHandle.StingerLauncherFire,
                H3MP_ClientHandle.StingerMissileDamage,
                H3MP_ClientHandle.StingerMissileExplode,
                H3MP_ClientHandle.PinnedGrenadeExplode,
                H3MP_ClientHandle.FVRGrenadeExplode,
                H3MP_ClientHandle.ClientDisconnect,
                H3MP_ClientHandle.ServerClosed,
                H3MP_ClientHandle.InitConnectionData,
                H3MP_ClientHandle.BangSnapSplode,
                H3MP_ClientHandle.C4Detonate,
                H3MP_ClientHandle.ClaymoreMineDetonate,
                H3MP_ClientHandle.SLAMDetonate,
                H3MP_ClientHandle.Ping,
                H3MP_ClientHandle.TNHSetPhaseHold,
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

            items = new H3MP_TrackedItemData[100];

            sosigs = new H3MP_TrackedSosigData[100];

            autoMeaters = new H3MP_TrackedAutoMeaterData[100];

            encryptions = new H3MP_TrackedEncryptionData[100];

            localItemCounter = 0;
            localSosigCounter = 0;
            localAutoMeaterCounter = 0;
            localEncryptionCounter = 0;

            Mod.LogInfo("Initialized client");
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem)
        {
            Mod.LogInfo("Client AddTrackedItem "+trackedItem.itemID+" with waitingindeX: "+trackedItem.localWaitingIndex);
            // Adjust items size to acommodate if necessary
            if (items.Length <= trackedItem.trackedID)
            {
                IncreaseItemsSize(trackedItem.trackedID);
            }

            if (trackedItem.controller == H3MP_Client.singleton.ID)
            {
                Mod.LogInfo("\tWe are controller");
                // Get our item
                H3MP_TrackedItemData actualTrackedItem = waitingLocalItems[trackedItem.localWaitingIndex];
                waitingLocalItems.Remove(trackedItem.localWaitingIndex);

                Mod.LogInfo("\tGot actual tracked item: "+ actualTrackedItem.itemID);
                // Set its new tracked ID
                actualTrackedItem.trackedID = trackedItem.trackedID;
                Mod.LogInfo("\tSet tracked ID: " + actualTrackedItem.trackedID);

                // Add the item to client global list
                items[actualTrackedItem.trackedID] = actualTrackedItem;
                Mod.LogInfo("\tSet in global list");

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
                Mod.LogInfo("\tAdded to itemsByInstanceByScene");

                actualTrackedItem.OnTrackedIDReceived();
            }
            else
            {
                Mod.LogInfo("\tWe are not controller");
                trackedItem.localTrackedID = -1;

                // Add the item to client global list
                items[trackedItem.trackedID] = trackedItem;
                Mod.LogInfo("\ttrackedItem.trackedID is "+ trackedItem.trackedID);

                // Add to item tracking list
                if (H3MP_GameManager.itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> relevantInstances))
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
                    H3MP_GameManager.itemsByInstanceByScene.Add(trackedItem.scene, newInstances);
                }
                Mod.LogInfo("\tAdded to itemsByInstanceByScene, instnaitating");

                // Instantiate item if it is in the current scene/instance
                if (trackedItem.scene.Equals(SceneManager.GetActiveScene().name) && trackedItem.instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }
        }

        public static void AddTrackedSosig(H3MP_TrackedSosigData trackedSosig)
        {
            // Adjust sosigs size to acommodate if necessary
            if (sosigs.Length <= trackedSosig.trackedID)
            {
                IncreaseSosigsSize(trackedSosig.trackedID);
            }

            if (trackedSosig.controller == H3MP_Client.singleton.ID)
            {
                // Get our sosig
                H3MP_TrackedSosigData actualTrackedSosig = waitingLocalSosigs[trackedSosig.localWaitingIndex];
                waitingLocalSosigs.Remove(trackedSosig.localWaitingIndex);

                // Set its new tracked ID
                actualTrackedSosig.trackedID = trackedSosig.trackedID;

                // Add the sosig to client global list
                sosigs[trackedSosig.trackedID] = actualTrackedSosig;

                // Send queued up orders
                actualTrackedSosig.OnTrackedIDReceived();

                // Only send latest data if not destroyed
                if (sosigs[trackedSosig.trackedID] != null)
                {
                    sosigs[trackedSosig.trackedID].Update(true);

                    // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                    H3MP_ClientSend.TrackedSosig(sosigs[trackedSosig.trackedID]);
                }
            }
            else
            {
                if (sosigs[trackedSosig.trackedID] == null)
                {
                    trackedSosig.localTrackedID = -1;

                    // Add the sosig to client global list
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

                    // Instantiate sosig if it is in the current scene
                    if (trackedSosig.scene.Equals(SceneManager.GetActiveScene().name) && trackedSosig.instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedSosig.Instantiate());
                    }
                }
                else // This is an initial update sosig data
                {
                    H3MP_TrackedSosigData trackedSosigData = sosigs[trackedSosig.trackedID];

                    // Instantiate sosig if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with sosigs we already have the data for
                    if (trackedSosigData.physicalObject == null)
                    {
                        if (trackedSosig.scene.Equals(SceneManager.GetActiveScene().name) && trackedSosig.instance == H3MP_GameManager.instance)
                        {
                            AnvilManager.Run(trackedSosigData.Instantiate());
                        }
                    }

                    trackedSosigData.Update(trackedSosig, true);
                }
            }
        }

        public static void AddTrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater)
        {
            // Adjust AutoMeaters size to acommodate if necessary
            if (autoMeaters.Length <= trackedAutoMeater.trackedID)
            {
                IncreaseAutoMeatersSize(trackedAutoMeater.trackedID);
            }

            if (trackedAutoMeater.controller == H3MP_Client.singleton.ID)
            {
                // Get our autoMeater.
                H3MP_TrackedAutoMeaterData actualTrackedAutoMeater = waitingLocalAutoMeaters[trackedAutoMeater.localWaitingIndex];
                waitingLocalAutoMeaters.Remove(trackedAutoMeater.localWaitingIndex);
                
                // Set its tracked ID
                actualTrackedAutoMeater.trackedID = trackedAutoMeater.trackedID;

                // Add the AutoMeater to client global list
                autoMeaters[trackedAutoMeater.trackedID] = actualTrackedAutoMeater;

                // Send queued up orders
                actualTrackedAutoMeater.OnTrackedIDReceived();
            }
            else
            {
                trackedAutoMeater.localTrackedID = -1;

                // Add the AutoMeater to client global list
                autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;

                // Add to autoMeater tracking list
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

                // Instantiate AutoMeater if it is in the current scene
                if (trackedAutoMeater.scene.Equals(SceneManager.GetActiveScene().name) && trackedAutoMeater.instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                }
            }
        }

        public static void AddTrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption)
        {
            // Adjust Encryptions size to acommodate if necessary
            if (encryptions.Length <= trackedEncryption.trackedID)
            {
                IncreaseEncryptionsSize(trackedEncryption.trackedID);
            }

            if (trackedEncryption.controller == H3MP_Client.singleton.ID)
            {
                // Get our encryption.
                H3MP_TrackedEncryptionData actualTrackedEncryption = waitingLocalEncryptions[trackedEncryption.localWaitingIndex];
                waitingLocalEncryptions.Remove(trackedEncryption.localWaitingIndex);
                
                // Set tis tracked ID
                actualTrackedEncryption.trackedID = trackedEncryption.trackedID;

                // Add the Encryption to client global list
                encryptions[trackedEncryption.trackedID] = actualTrackedEncryption;

                // Send queued up orders
                actualTrackedEncryption.OnTrackedIDReceived();

                // Only send latest data if not destroyed
                if (encryptions[trackedEncryption.trackedID] != null)
                {
                    encryptions[trackedEncryption.trackedID].Update(true);

                    // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                    H3MP_ClientSend.TrackedEncryption(encryptions[trackedEncryption.trackedID]);
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

                    // Instantiate Encryption if it is in the current scene
                    if (trackedEncryption.scene.Equals(SceneManager.GetActiveScene().name) && trackedEncryption.instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedEncryption.Instantiate());
                    }
                }
                else // This is an initial update encryption data
                {
                    H3MP_TrackedEncryptionData trackedEncryptionData = encryptions[trackedEncryption.trackedID];

                    // Instantiate Encryption if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with encryptions we already have the data for
                    if (trackedEncryptionData.physicalObject == null)
                    {
                        if (trackedEncryption.scene.Equals(SceneManager.GetActiveScene().name) && trackedEncryption.instance == H3MP_GameManager.instance)
                        {
                            AnvilManager.Run(trackedEncryptionData.Instantiate());
                        }
                    }

                    trackedEncryptionData.Update(trackedEncryption, true);
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
            H3MP_TrackedItemData[] tempItems = items;
            items = new H3MP_TrackedItemData[minCapacity];
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
            H3MP_TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new H3MP_TrackedSosigData[minCapacity];
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
            H3MP_TrackedAutoMeaterData[] tempAutoMeaters = autoMeaters;
            autoMeaters = new H3MP_TrackedAutoMeaterData[minCapacity];
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
            H3MP_TrackedEncryptionData[] tempEncryptions = encryptions;
            encryptions = new H3MP_TrackedEncryptionData[minCapacity];
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
                        Mod.LogInfo("Disconnecting from server.");
                        break;
                    case 1:
                        Mod.LogInfo("Disconnecting from server, end of stream.");
                        break;
                    case 2:
                        Mod.LogInfo("Disconnecting from server, TCP forced.");
                        break;
                    case 3:
                        Mod.LogInfo("Disconnecting from server, UDP forced.");
                        break;
                }

                if (sendToServer) 
                {
                    // Give control of everything we control
                    H3MP_GameManager.GiveUpAllControl();

                    H3MP_ClientSend.ClientDisconnect();
                }

                // On our side take physical control of everything
                H3MP_GameManager.TakeAllPhysicalControl(true);

                tcp.socket.Close();
                udp.socket.Close();

                H3MP_GameManager.Reset();
                SpecificDisconnect();
                Destroy(Mod.managerObject);
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
                Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, H3MP_GameManager.ID);
            }
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
        }
    }
}
