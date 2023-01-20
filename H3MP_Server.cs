using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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

            Console.WriteLine($"Server started on: {tcpListener.LocalEndpoint}");

            // Just connected, sync if current scene is syncable
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(SceneManager.GetActiveScene().name))
            {
                H3MP_GameManager.SyncTrackedItems(true, true);
                H3MP_GameManager.SyncTrackedSosigs(true, true);
                H3MP_GameManager.SyncTrackedAutoMeaters(true, true);
                H3MP_GameManager.SyncTrackedEncryptions(true, true);
            }
        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            Console.WriteLine("TCP connect callback");
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}");

            for (int i = 1; i <= maxClientCount; ++i)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect, server full");
        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
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
                Debug.Log($"Error receiving UDP data: {ex}");
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
                Debug.Log($"Error sending UDP data to {clientEndPoint}: {ex}");
            }
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem, int clientID)
        {
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

            // Instantiate item if it is in the current scene and not controlled by us
            if (clientID != 0)
            {
                if (trackedItem.scene.Equals(SceneManager.GetActiveScene().name) && trackedItem.instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }

            // Send to all clients, including controller because they need confirmation from server that this item was added and its trackedID
            H3MP_ServerSend.TrackedItem(trackedItem, clientID);

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
                    if (trackedSosig.scene.Equals(SceneManager.GetActiveScene().name) && trackedSosig.instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedSosig.Instantiate());
                    }
                }

                // Send to all clients, including controller because they need confirmation from server that this item was added and its trackedID
                H3MP_ServerSend.TrackedSosig(trackedSosig, clientID);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedSosig.controller != 0)
                {
                    trackedSosig.localTrackedID = -1;
                }
            }
            else
            {
                // This is a sosig we already received full data for and assigned a tracked ID to but things may
                // have appened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                sosigs[trackedSosig.trackedID].Update(trackedSosig, true);

                H3MP_ServerSend.TrackedSosig(trackedSosig, clientID, false);
            }
        }

        public static void AddTrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, int clientID)
        {
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
                if (trackedAutoMeater.scene.Equals(SceneManager.GetActiveScene().name) && trackedAutoMeater.instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                }
            }

            // Send to all clients, including controller because they need confirmation from server that this item was added and its trackedID
            H3MP_ServerSend.TrackedAutoMeater(trackedAutoMeater, clientID);

            // Update the local tracked ID at the end because we need to send that back to the original client intact
            if (trackedAutoMeater.controller != 0)
            {
                trackedAutoMeater.localTrackedID = -1;
            }
        }

        public static void AddTrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption, int clientID)
        {
            if (trackedEncryption.trackedID == -1)
            {
                Debug.Log("Received order to add tracked Encryption");
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
                    if (trackedEncryption.scene.Equals(SceneManager.GetActiveScene().name) && trackedEncryption.instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedEncryption.Instantiate());
                    }
                }

                // Send to all clients, including controller because they need confirmation from server that this item was added and its trackedID
                H3MP_ServerSend.TrackedEncryption(trackedEncryption, clientID);

                // Update the local tracked ID at the end because we need to send that back to the original client intact
                if (trackedEncryption.controller != 0)
                {
                    trackedEncryption.localTrackedID = -1;
                }
            }
            else
            {
                // This is a encryption we already received full data for and assigned a tracked ID to but things may
                // have happened to it since we sent the tracked ID, so use this data to update our's and everyones else's
                encryptions[trackedEncryption.trackedID].Update(trackedEncryption, true);

                H3MP_ServerSend.TrackedEncryption(trackedEncryption, clientID, false);
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
                H3MP_ServerHandle.AddSyncScene,
                H3MP_ServerHandle.TrackedItems,
                H3MP_ServerHandle.TrackedItem,
                H3MP_ServerHandle.TakeControl,
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
                H3MP_ServerHandle.SetTNHLevelIndex,
                H3MP_ServerHandle.AddInstance,
                H3MP_ServerHandle.SetTNHController,
                H3MP_ServerHandle.TNHData,
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
                H3MP_ServerHandle.EncryptionRecursiveInit,
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
             
        Debug.Log("Initialized server");
        }
    }
}
