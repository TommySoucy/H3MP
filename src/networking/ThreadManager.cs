using FistVR;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace H3MP.Networking
{
    public class ThreadManager : MonoBehaviour
    {
        public static bool host; // Whether this thread manager is the host's

        private static readonly List<Action> executeOnMainThread = new List<Action>();
        private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        private static bool actionToExecuteOnMainThread = false;

        public static float tickRate = 20; // How many times a second we will send updates
        public static float time = 1 / tickRate; // Period between sending updates
        public static float timer = 0; // Amount of time left until next tick

        public static int updateTimeLimit = 10; // Time limit (ms) we have to apply received updates before leaving it to the next frame
        public static int updateState = -1; // The packet ID we are currently at in updates. -1 is Main queue
        public static int updateStateIndex = 0; // Count of how many elements we've gone through this iteration 
        public static int updateSubStateIndex = -1; // Count of how many elements we've gone through this iteration 

        public static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly float pingTime = 1;
        public static float pingTimer = pingTime;

        #region Packet preprocessing
        // CUSTOMIZATION:
        // Packet preprocessing is a system made to prevent packet build up.
        // Consider a server with a queue of packets it must process.
        // Client sends it a packet.
        // Server adds it to its queue and in the next main update, processes it.
        // The client sends another packet.
        // For wtv reason the server is a little slower, and by the time it processes the packet, the client had time to send 2 more.
        // Next frame, the server has to process 2 packets instead of 1.
        // By the time it does, client had time to send 3.
        // This number keeps growing, and if server doesn't recover, it just gets slower until it comes to a stop.
        // Preprocessors are meant to prevent update packets from piling by replacing the old packets with the new one.
        // This way, for 1 updated entity, like a TrackedObject, we only have to process a single packet per main update, instead of however many piled up in between updates.
        // The way a preprocessor does this depends on the type of packet 
        // Check out the H3MP specific PlayerStatePreprocessor and TrackedObjectsPreprocessor for examples
        // Packet preprocessing is handled differently in the updates as well, that is when we receive
        // a packet and preprocess it, it won't be added to the main queue like other packets, and as such, will need
        // a PreprocessedPacketHandler
        // This PreprocessedPacketHandler should handle it the same way as it would a normal packet
        // but should take progress into account, and return the index of its progress if it did not complete.
        // See PlayerStatePacketHandler to see an example of how this is done

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSpectatorHostsChanged event
        /// </summary>
        /// <param name="host">Whether we are host</param>
        public delegate void OnPacketPreprocessorsInitilizationDelegate(bool host);

        /// <summary>
        /// CUSTOMIZATION
        /// Note that when a custom preprocessor gets called by PreprocessPacket, 
        /// customPreprocessedPackets is already locked, meaning you don't need to do it inside your preprocessor
        /// like I do in H3MP's.
        /// Note in H3MP preprocessors, I lock the queues.
        /// You will need to do that too, but on the custom queues, when you want to enqueue new stuff.
        /// Unless you know what you're doing and you know the implications of a deadlock, don't do inner locks.
        /// Only lock one thing at a time and unlock it before locking something else, don't lock inside another lock.
        /// This is specific to the custom packet system due to having to increase the amount of packet types at runtime,
        /// I also lock these in the main thread when I increase their size in ClientHandle.RegisterCustomPacketType and Server.RegisterCustomPacketType
        /// and those locks are done in a specific order which I respect here in threadmanager to prevent deadlocks.
        /// </summary>
        /// <param name="packet">The packet to preprocess</param>
        /// <param name="clientID">The client the packet comes from</param>
        /// <returns>True if we want to put this packet in main queue instead of preprocessing it</returns>
        public delegate bool PacketPreprocessor(Packet packet, int clientID);
        public static PacketPreprocessor[] packetPreprocessors;
        public static PacketPreprocessor[] customPacketPreprocessors;

        /// <summary>
        /// CUSTOMIZATION
        /// Note that once your handler is called customPreprocessedPackets is already locked, so you don't have to in the handler.
        /// 
        /// </summary>
        /// <param name="index">Current substate index. Indicates the progress that has already been done on this handler this iteration. 
        ///                     Where the handler left off in last update iteration.</param>
        /// <param name="start">Stopwatch to check how much time has elapsed since the start of this update iteration</param>
        /// <param name="data">The data relevant for this handler</param>
        /// <returns>True if completed</returns>
        public delegate bool PreprocessedPacketHandler(ref int index, Stopwatch start, object data);
        public static PreprocessedPacketHandler[] preprocessedPacketHandlers;
        public static PreprocessedPacketHandler[] customPreprocessedPacketHandlers;

        public static object[] preprocessedPackets;
        public static Queue<int> packetProcessQueue = new Queue<int>();
        public static Queue<int>[] packetSubProcessQueues;
        public static object[] customPreprocessedPackets;
        public static Queue<int> customPacketProcessQueue = new Queue<int>();
        public static Queue<int>[] customPacketSubProcessQueues;

        public static Queue<int> copiedPacketProcessQueue = new Queue<int>();
        public static Queue<int> copiedPacketSubProcessQueue = new Queue<int>();

        public void InitializePacketPreprocessing()
        {
            customPacketPreprocessors = new PacketPreprocessor[10];
            customPreprocessedPackets = new object[10];
            customPreprocessedPacketHandlers = new PreprocessedPacketHandler[10];
            customPacketSubProcessQueues = new Queue<int>[10];
            if (host)
            {
                int count = Enum.GetValues(typeof(ClientPackets)).Length;
                packetPreprocessors = new PacketPreprocessor[count];
                preprocessedPackets = new object[count];
                preprocessedPacketHandlers = new PreprocessedPacketHandler[count];
                packetSubProcessQueues = new Queue<int>[count];
                for(int i=0; i < count; ++i)
                {
                    packetSubProcessQueues[i] = new Queue<int>();
                }
            }
            else
            {
                int count = Enum.GetValues(typeof(ServerPackets)).Length;
                packetPreprocessors = new PacketPreprocessor[count];
                preprocessedPackets = new object[count];
                preprocessedPacketHandlers = new PreprocessedPacketHandler[count];
                packetSubProcessQueues = new Queue<int>[count];
                for (int i = 0; i < count; ++i)
                {
                    packetSubProcessQueues[i] = new Queue<int>();
                }
            }

            // Setup for H3MP
            if (host)
            {
                packetPreprocessors[2] = PlayerStatePreprocessor;
                packetPreprocessors[9] = TrackedObjectsPreprocessor;

                preprocessedPacketHandlers[2] = PlayerStatePacketHandler;
                preprocessedPacketHandlers[9] = TrackedObjectsPacketHandler;

                lock (preprocessedPackets)
                {
                    preprocessedPackets[2] = new object[] { false, new KeyValuePair<byte, Packet>[(int)Mod.config["MaxClientCount"]] };
                    preprocessedPackets[9] = new object[] { false, new KeyValuePair<byte, Packet>[100] };
                }
            }
            else
            {
                packetPreprocessors[2] = PlayerStatePreprocessor;
                packetPreprocessors[157] = TrackedObjectsPreprocessor;

                preprocessedPacketHandlers[2] = PlayerStatePacketHandler;
                preprocessedPacketHandlers[157] = TrackedObjectsPacketHandler;

                lock (preprocessedPackets)
                {
                    preprocessedPackets[2] = new object[] { false, new KeyValuePair<byte, Packet>[(int)Mod.config["MaxClientCount"]] };
                    preprocessedPackets[157] = new object[] { false, new KeyValuePair<byte, Packet>[100] };
                }
            }
        }

        public static bool PreprocessPacket(Packet packet, int packetID, int clientID = -1)
        {
            if (packetID <= -2)
            {
                int index = packetID * -1 - 2;
                lock (customPacketPreprocessors)
                {
                    lock (customPreprocessedPackets)
                    {
                        if (customPacketPreprocessors.Length > index && customPacketPreprocessors[index] != null)
                        {
                            return customPacketPreprocessors[index](packet, clientID);
                        }
                    }
                }
            }
            else
            {
                if (packetPreprocessors.Length > packetID && packetPreprocessors[packetID] != null)
                {
                    return packetPreprocessors[packetID](packet, clientID);
                }
            }

            return true; // True means that we don't want to preprocess a packet with this ID
        }

        public bool PlayerStatePacketHandler(ref int index, Stopwatch start, object data)
        {
            // Index is not actually used in this handler but must make it at least non zero so
            // update process knows we are not done with this handler if we return false
            ++index;

            KeyValuePair<byte, Packet>[] actualData = (KeyValuePair<byte, Packet>[])data;

            while (copiedPacketSubProcessQueue.Count > 0)
            {
                // Check timer at beginning becuase we know we still have data left to handle 
                //if (start.ElapsedMilliseconds > updateTimeLimit)
                //{
                //    return false;
                //}

                int playerID = copiedPacketSubProcessQueue.Dequeue();
                KeyValuePair<byte, Packet> pair = actualData[playerID];

                if (host)
                {
                    Player player = Server.clients[playerID].player;

                    player.position = pair.Value.ReadVector3();
                    player.rotation = pair.Value.ReadQuaternion();
                    player.headPos = pair.Value.ReadVector3();
                    player.headRot = pair.Value.ReadQuaternion();
                    player.torsoPos = pair.Value.ReadVector3();
                    player.torsoRot = pair.Value.ReadQuaternion();
                    player.leftHandPos = pair.Value.ReadVector3();
                    player.leftHandRot = pair.Value.ReadQuaternion();
                    player.rightHandPos = pair.Value.ReadVector3();
                    player.rightHandRot = pair.Value.ReadQuaternion();
                    player.health = pair.Value.ReadFloat();
                    player.maxHealth = pair.Value.ReadInt();
                    short additionalDataLength = pair.Value.ReadShort();
                    byte[] additionalData = null;
                    if (additionalDataLength > 0)
                    {
                        additionalData = pair.Value.ReadBytes(additionalDataLength);
                    }

                    GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                                    player.leftHandPos, player.leftHandRot,
                                                    player.rightHandPos, player.rightHandRot,
                                                    player.health, player.maxHealth, additionalData);

                    // Relay
                    ServerSend.PlayerState(pair.Value, player);
                }
                else
                {
                    Vector3 position = pair.Value.ReadVector3();
                    Quaternion rotation = pair.Value.ReadQuaternion();
                    Vector3 headPos = pair.Value.ReadVector3();
                    Quaternion headRot = pair.Value.ReadQuaternion();
                    Vector3 torsoPos = pair.Value.ReadVector3();
                    Quaternion torsoRot = pair.Value.ReadQuaternion();
                    Vector3 leftHandPos = pair.Value.ReadVector3();
                    Quaternion leftHandRot = pair.Value.ReadQuaternion();
                    Vector3 rightHandPos = pair.Value.ReadVector3();
                    Quaternion rightHandRot = pair.Value.ReadQuaternion();
                    float health = pair.Value.ReadFloat();
                    int maxHealth = pair.Value.ReadInt();
                    short additionalDataLength = pair.Value.ReadShort();
                    byte[] additionalData = null;
                    if (additionalDataLength > 0)
                    {
                        additionalData = pair.Value.ReadBytes(additionalDataLength);
                    }

                    GameManager.UpdatePlayerState(playerID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                                    leftHandPos, leftHandRot,
                                                    rightHandPos, rightHandRot,
                                                    health, maxHealth, additionalData);
                }

                // Dispose and nullify in data so preprocessor knows we don't yet have an update packet for this object
                pair.Value.Dispose();
                actualData[playerID] = new KeyValuePair<byte, Packet>();
            }

            // If we got here, return true, meaning we are done with handling this packet type
            return true;
        }

        public bool TrackedObjectsPacketHandler(ref int index, Stopwatch start, object data)
        {
            // Index is not actually used in this handler but must make it at least non zero so
            // update process knows we are not done with this handler if we return false
            ++index;

            KeyValuePair<byte, Packet>[] actualData = (KeyValuePair<byte, Packet>[])data;
            TrackedObjectData[] arrToUse;
            if (host)
            {
                arrToUse = Server.objects;
            }
            else
            {
                arrToUse = Client.objects;
            }

            while (copiedPacketSubProcessQueue.Count > 0)
            {
                // Check timer at beginning becuase we know we still have data left to handle 
                //if (start.ElapsedMilliseconds > updateTimeLimit)
                //{
                //    return false;
                //}

                int trackedID = copiedPacketSubProcessQueue.Dequeue();
                KeyValuePair<byte, Packet> pair = actualData[trackedID];
                TrackedObjectData trackedObjectData = arrToUse[trackedID];
                if (trackedObjectData != null)
                {
                    trackedObjectData.UpdateFromPacket(pair.Value);

                    if (host)
                    {
                        // Relay to other clients in the same scene/instance
                        if (GameManager.playersByInstanceByScene.TryGetValue(trackedObjectData.scene, out Dictionary<int, List<int>> instances) &&
                            instances.TryGetValue(trackedObjectData.instance, out List<int> players))
                        {
                            ServerSend.TrackedObjects(pair.Value, players, trackedObjectData.controller);
                        }
                    }
                }

                // Dispose and nullify in data so preprocessor knows we don't yet have an update packet for this object
                pair.Value.Dispose();
                actualData[trackedID] = new KeyValuePair<byte, Packet>();
            }

            // If we got here, return true, meaning we are done with handling this packet type
            return true;
        }

        public bool PlayerStatePreprocessor(Packet packet, int clientID = -1)
        {
            byte order = packet.ReadByte();
            int playerID = clientID;
            int packetID;
            if (host)
            {
                packetID = (int)ClientPackets.playerState;
            }
            else
            {
                playerID = packet.ReadInt();
                packetID = (int)ServerPackets.playerState;
            }

            lock (preprocessedPackets)
            {
                object[] packetData = (object[])preprocessedPackets[packetID];
                KeyValuePair<byte, Packet>[] data = (KeyValuePair<byte, Packet>[])packetData[1];
                if ((bool)packetData[0])
                {
                    if (data[playerID].Value == null)
                    {
                        data[playerID] = new KeyValuePair<byte, Packet>(order, packet);
                        lock (packetSubProcessQueues)
                        {
                            packetSubProcessQueues[packetID].Enqueue(playerID);
                        }
                    }
                    else if (order > data[playerID].Key || data[playerID].Key - order > 128)
                    {
                        data[playerID].Value.Dispose();
                        data[playerID] = new KeyValuePair<byte, Packet>(order, packet);
                    }
                }
                else // No entry yet for this packetID
                {
                    data[playerID] = new KeyValuePair<byte, Packet>(order, packet);
                    packetData[0] = true;
                    lock (packetProcessQueue)
                    {
                        packetProcessQueue.Enqueue(packetID);
                    }
                    lock (packetSubProcessQueues)
                    {
                        packetSubProcessQueues[packetID].Enqueue(playerID);
                    }
                }
            }

            return false;
        }

        public bool TrackedObjectsPreprocessor(Packet packet, int clientID = -1)
        {
            byte order = packet.ReadByte();
            int trackedID = packet.ReadInt();
            int packetID = -1;
            if (host)
            {
                packetID = (int)ClientPackets.trackedObjects;
            }
            else
            {
                packetID = (int)ServerPackets.trackedObjects;
            }

            lock (preprocessedPackets)
            {
                object[] packetData = (object[])preprocessedPackets[packetID];
                KeyValuePair<byte, Packet>[] data = (KeyValuePair<byte, Packet>[])packetData[1];

                // Increase data array size if necessary
                if (trackedID >= data.Length)
                {
                    int newLength = trackedID - trackedID % 100 + 100;
                    KeyValuePair<byte, Packet>[] temp = data;
                    data = new KeyValuePair<byte, Packet>[newLength];
                    for (int i = 0; i < temp.Length; ++i)
                    {
                        data[i] = temp[i];
                    }
                }

                if ((bool)packetData[0])
                {
                    try
                    { 
                        if (data[trackedID].Value == null)
                        {
                            data[trackedID] = new KeyValuePair<byte, Packet>(order, packet);
                            lock (packetSubProcessQueues)
                            {
                                packetSubProcessQueues[packetID].Enqueue(trackedID);
                            }
                        }
                        else if (order > data[trackedID].Key || data[trackedID].Key - order > 128)
                        {
                            data[trackedID].Value.Dispose();
                            data[trackedID] = new KeyValuePair<byte, Packet>(order, packet);
                        }
                    }
                    catch (Exception ex)
                    {
                        Mod.LogError("Exception: " + ex.Message);
                    }
                }
                else
                {
                    data[trackedID] = new KeyValuePair<byte, Packet>(order, packet);
                    packetData[0] = true;
                    lock (packetProcessQueue)
                    {
                        packetProcessQueue.Enqueue(packetID);
                    }
                    lock (packetSubProcessQueues)
                    {
                        packetSubProcessQueues[packetID].Enqueue(trackedID);
                    }
                }
            }

            return false;
        }
        #endregion

        private void Update()
        {
            UpdateMain();
        }

        private void FixedUpdate()
        {
            // Limit sending updates to tickrate
            timer -= Time.fixedDeltaTime;
            if (timer <= 0)
            {
                UpdateMainFixed();
                timer = time;
            }
        }

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="_action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null)
            {
                Mod.LogInfo("No action to execute on main thread!", false);
                return;
            }

            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(_action);
                actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateMain()
        {
            Stopwatch start = Stopwatch.StartNew();

            if(updateState == -1)
            {
#if DEBUG
                if (Input.GetKey(KeyCode.PageDown))
                {
                    Mod.LogInfo("DEBUG UPDATE MAIN");
                }
#endif
                if(updateStateIndex > 0)
                {
                    for (int i = updateStateIndex; i < executeCopiedOnMainThread.Count; i++)
                    {
                        executeCopiedOnMainThread[i]();
                        updateStateIndex = i;

                        if (start.ElapsedMilliseconds >= updateTimeLimit)
                        {
                            Mod.LogWarning("Main packet queue reached time limit at " + i + " of " + executeCopiedOnMainThread.Count);
                            return;
                        }
                    }
                }
                else
                {
                    if (actionToExecuteOnMainThread)
                    {
                        executeCopiedOnMainThread.Clear();
                        lock (executeOnMainThread)
                        {
                            executeCopiedOnMainThread.AddRange(executeOnMainThread);
                            executeOnMainThread.Clear();
                            actionToExecuteOnMainThread = false;
                        }
#if DEBUG
                        if (Input.GetKey(KeyCode.PageDown))
                        {
                            Mod.LogInfo("Actions to execute: " + executeCopiedOnMainThread.Count);
                        }
#endif

                        for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                        {
                            executeCopiedOnMainThread[i]();
                            updateStateIndex = i;

                            //if(start.ElapsedMilliseconds >= updateTimeLimit)
                            //{
                            //    Mod.LogWarning("Main packet queue reached time limit at " + i + " of " + executeCopiedOnMainThread.Count);
                            //    return;
                            //}
                        }
                    }
                }

                updateState = 0;
                updateStateIndex = -1;
                updateSubStateIndex = -1;
            }

            // Do preprocessed updates
            if(updateState >= 0)
            {
                // If not started yet
                if (updateStateIndex == -1)
                {
                    // Copy process queue
                    lock (packetProcessQueue)
                    {
                        copiedPacketProcessQueue = new Queue<int>(packetProcessQueue);
                        packetProcessQueue.Clear();
                    }

                    // Set updateStateIndex to indicate we started
                    updateStateIndex = 0;
                }

                while (copiedPacketProcessQueue.Count > 0)
                {
                    // If we don't have a substate, we want to start processing a new packet type
                    if(updateSubStateIndex == -1)
                    {
                        updateState = copiedPacketProcessQueue.Dequeue();
                        lock (packetSubProcessQueues)
                        {
                            copiedPacketSubProcessQueue = new Queue<int>(packetSubProcessQueues[updateState]);
                            packetSubProcessQueues[updateState].Clear();
                        }

                        // We copied the queue and cleared the original, so set this flag to false, indicating we don't have data for this 
                        // packetID (updateState) yet, so we know to add it to queue again if we process a new packet for it
                        lock (preprocessedPackets)
                        {
                            ((object[])preprocessedPackets[updateState])[0] = false;
                        }
                    }

                    // Then call current preprocessed packet handler with substate
                    lock (preprocessedPackets) 
                    {
                        if (preprocessedPacketHandlers[updateState](ref updateSubStateIndex, start, ((object[])preprocessedPackets[updateState])[1]))
                        {
                            // Completed this handler, increment state index
                            updateStateIndex++;
                            updateSubStateIndex = -1;
                        }
                    }

                    //if (start.ElapsedMilliseconds >= updateTimeLimit)
                    //{
                    //    return;
                    //}
                }

                updateState = -2;
                updateStateIndex = -1;
                updateSubStateIndex = -1;
            }

            // Do custom preprocessed updates
            if (updateState <= -2)
            {
                // Convert current update state to a custom packet ID
                int actualUpdateState = updateState * -1 - 2;

                // If not started yet
                if (updateStateIndex == -1)
                {
                    // Copy process queue
                    lock (customPacketProcessQueue)
                    {
                        copiedPacketProcessQueue = new Queue<int>(customPacketProcessQueue);
                        customPacketProcessQueue.Clear();
                    }

                    // Set updateStateIndex to indicate we started
                    updateStateIndex = 0;
                }

                while (copiedPacketProcessQueue.Count > 0)
                {
                    // If we don't have a substate, we want to start processing a new packet type
                    if (updateSubStateIndex == -1)
                    {
                        actualUpdateState = copiedPacketProcessQueue.Dequeue(); 
                        updateState = actualUpdateState * -1 - 2;
                        lock (customPacketSubProcessQueues)
                        {
                            copiedPacketSubProcessQueue = new Queue<int>(customPacketSubProcessQueues[actualUpdateState]);
                            customPacketSubProcessQueues[actualUpdateState].Clear();
                        }

                        // We copied the queue and cleared the original, so set this flag to false, indicating we don't have data for this 
                        // packetID (updateState) yet, so we know to add it to queue again if we process a new packet for it
                        lock (customPreprocessedPackets)
                        {
                            ((object[])customPreprocessedPackets[actualUpdateState])[0] = false;
                        }
                    }

                    // Then call current preprocessed packet handler with substate
                    lock (customPreprocessedPackets)
                    {
                        lock (customPreprocessedPacketHandlers)
                        {
                            if (customPreprocessedPacketHandlers[actualUpdateState](ref updateSubStateIndex, start, ((object[])customPreprocessedPackets[actualUpdateState])[1]))
                            {
                                // Completed this handler, increment state index
                                updateStateIndex++;
                                updateSubStateIndex = -1;
                            }
                        }
                    }

                    //if (start.ElapsedMilliseconds >= updateTimeLimit)
                    //{
                    //    return;
                    //}
                }

                updateState = -1;
                updateStateIndex = 0;
                updateSubStateIndex = -1;
            }

            if (!host)
            {
                if (Client.punchThrough)
                {
                    pingTimer -= Time.deltaTime;
                    if (pingTimer <= 0)
                    {
                        pingTimer = pingTime;

                        // Waiting means we didn't get a call to the callback, meaning no connection. Try again
                        if (Client.punchThroughWaiting)
                        {
                            if (Client.punchThroughAttemptCounter < 10)
                            {
                                Mod.LogInfo("Client punchthrough connection attempt " + Client.punchThroughAttemptCounter + ", timing out at 10", false);
                                ++Client.punchThroughAttemptCounter;
                                Client.singleton.tcp.socket.EndConnect(Client.connectResult);
                                Client.connectResult = Client.singleton.tcp.socket.BeginConnect(Client.singleton.IP, Client.singleton.port, Client.singleton.tcp.ConnectCallback, Client.singleton.tcp.socket);
                            }
                            else
                            {
                                Client.singleton.Disconnect(false, 4);
                                if(ServerListController.instance != null)
                                {
                                    Mod.LogInfo("Client punchthrough connection timed out", false);
                                    ServerListController.instance.gotEndPoint = false;
                                    ServerListController.instance.joiningEntry = -1;
                                    ServerListController.instance.SetClientPage(true);
                                }
                            }
                        }
                        else // Not waiting for punchthrough connection anymore
                        {
                            Client.punchThrough = false;
                            if (!Client.singleton.tcp.socket.Connected)
                            {
                                // Connection unsuccessful
                                Client.singleton.Disconnect(false, 4);
                                if (ServerListController.instance != null)
                                {
                                    ServerListController.instance.gotEndPoint = false;
                                    ServerListController.instance.joiningEntry = -1;
                                    ServerListController.instance.SetClientPage(true);
                                }
                            }
                            // else, connection successful, updating serverlist will be handled by connection event
                        }
                    }
                }
                else
                {
                    pingTimer -= Time.deltaTime;
                    if (pingTimer <= 0)
                    {
                        pingTimer = pingTime;
                        if (Client.singleton.gotWelcome)
                        {
                            ClientSend.Ping(Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalMilliseconds));
                        }
                        else
                        {
                            ++Client.singleton.pingAttemptCounter;
                            if (Client.singleton.pingAttemptCounter >= 10)
                            {
                                Mod.LogWarning("Have not received server welcome for " + Client.singleton.pingAttemptCounter + " seconds, timing out at 30");
                            }
                            if (Client.singleton.pingAttemptCounter >= 30)
                            {
                                Client.singleton.Disconnect(false, 4);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Server.PTClients.Count > 0)
                {
                    pingTimer -= Time.deltaTime;
                    if (pingTimer <= 0)
                    {
                        pingTimer = pingTime;

                        for (int i = Server.PTClients.Count - 1; i >= 0; --i)
                        {
                            /*
                            if(Server.PTClients[i].PTUDPEstablished)
                            {
                                Mod.LogInfo("Client " + Server.PTClients[i].ID + " connected through punch-through", false);
                                if (Server.PTClients[i].PTUDPEstablished)
                                {
                                    Server.PTClients[i].PTTCP.EndConnect(Server.PTClients[i].PTConnectionResult);
                                }
                                Server.PTClients[i].punchThrough = false;
                                Server.PTClients[i].PTUDPEstablished = false;
                                Server.PTClients[i].attemptingPunchThrough = false;

                                Server.PTClients.RemoveAt(i);

                                continue;
                            }

                            if (Server.PTClients[i].punchThroughAttemptCounter < 10)
                            {
                                Mod.LogInfo("Client "+ Server.PTClients[i].ID + " punch-through connection attempt " + Server.PTClients[i].punchThroughAttemptCounter + ", timing out at 10", false);
                                ++Server.PTClients[i].punchThroughAttemptCounter;
                                if (Server.PTClients[i].PTUDPEstablished)
                                {
                                    Server.PTClients[i].PTTCP.EndConnect(Server.PTClients[i].PTConnectionResult);
                                }
                                else
                                {
                                    Server.PTClients[i].PTUDPEstablished = true;
                                }
                                Server.PTClients[i].PTConnectionResult = Server.PTClients[i].PTTCP.BeginConnect(Server.PTClients[i].PTEndPoint.Address.ToString(), Server.PTClients[i].PTEndPoint.Port, Server.PTClients[i].PTConnectCallback, Server.PTClients[i].PTTCP);
                            }
                            else
                            {
                                Mod.LogInfo("Client " + Server.PTClients[i].ID + " punch-through connection timed out", false);
                                if (Server.PTClients[i].PTUDPEstablished)
                                {
                                    Server.PTClients[i].PTTCP.EndConnect(Server.PTClients[i].PTConnectionResult);
                                }
                                Server.PTClients[i].punchThrough = false;
                                Server.PTClients[i].PTUDPEstablished = false;
                                Server.PTClients[i].attemptingPunchThrough = false;

                                Server.PTClients.RemoveAt(i);
                            }*/
                        }
                    }
                }
            }
        }

        public static void UpdateMainFixed()
        {
            // Can only update clients if this is the host
            if (host)
            {
                if (GameManager.playersPresent.Count > 0)
                {
                    // Send all trackedItems to all clients
                    ServerSend.TrackedObjects();

                    // Also send the host's player state to all clients
                    if (GM.CurrentPlayerBody != null)
                    {
                        ServerSend.PlayerState(GameManager.playerOrder++,
                                               0,
                                               GM.CurrentPlayerBody.transform.position,
                                               GM.CurrentPlayerBody.transform.rotation,
                                               GM.CurrentPlayerBody.headPositionFiltered,
                                               GM.CurrentPlayerBody.headRotationFiltered,
                                               GM.CurrentPlayerBody.headPositionFiltered + GameManager.torsoOffset,
                                               GM.CurrentPlayerBody.Torso.rotation,
                                               GM.CurrentPlayerBody.LeftHand.position,
                                               GM.CurrentPlayerBody.LeftHand.rotation,
                                               GM.CurrentPlayerBody.RightHand.position,
                                               GM.CurrentPlayerBody.RightHand.rotation,
                                               GM.CurrentPlayerBody.Health,
                                               GM.CurrentPlayerBody.GetMaxHealthPlayerRaw(),
                                               GameManager.scene,
                                               GameManager.instance);
                    }
                }

                // Send ID confirmation if there is one
                if(Server.IDsToConfirm.Count > 0)
                {
                    Mod.LogInfo("There are IDs to confirm");
                    int count = 0;
                    List<int> entriesToRemove = new List<int>();
                    foreach (KeyValuePair<int, List<int>> entry in Server.IDsToConfirm)
                    {
                        Mod.LogInfo("\tRequesting confirm for "+entry.Key);
                        ServerSend.IDConfirm(entry);
                        entriesToRemove.Add(entry.Key);

                        if (++count >= Server.IDConfirmLimit)
                        {
                            break;
                        }
                    }

                    for(int i=0; i < entriesToRemove.Count; ++i)
                    {
                        Server.IDsToConfirm.Remove(entriesToRemove[i]);
                    }
                }
            }
            else
            {
                // Send this client's up to date trackedItems to host and all other clients of there are others in the scene
                if (GameManager.playersPresent.Count > 0)
                {
                    ClientSend.TrackedObjects();

                    // Also send the player state to all clients
                    if (GM.CurrentPlayerBody != null)
                    {
                        ClientSend.PlayerState(GameManager.playerOrder++,
                                               GM.CurrentPlayerBody.transform.position,
                                               GM.CurrentPlayerBody.transform.rotation,
                                               GM.CurrentPlayerBody.headPositionFiltered,
                                               GM.CurrentPlayerBody.headRotationFiltered,
                                               GM.CurrentPlayerBody.headPositionFiltered + GameManager.torsoOffset,
                                               GM.CurrentPlayerBody.Torso.rotation,
                                               GM.CurrentPlayerBody.LeftHand.position,
                                               GM.CurrentPlayerBody.LeftHand.rotation,
                                               GM.CurrentPlayerBody.RightHand.position,
                                               GM.CurrentPlayerBody.RightHand.rotation,
                                               GM.CurrentPlayerBody.Health,
                                               GM.CurrentPlayerBody.GetMaxHealthPlayerRaw());
                    }
                }
            }
        }

        public static void Reset()
        {
            packetPreprocessors = null;
            customPacketPreprocessors = null;
            preprocessedPacketHandlers = null;
            customPreprocessedPacketHandlers = null;

            preprocessedPackets = null;
            packetProcessQueue.Clear();
            packetSubProcessQueues = null;
            customPreprocessedPackets = null;
            customPacketProcessQueue.Clear();
            customPacketSubProcessQueues = null;

            copiedPacketProcessQueue.Clear();
            copiedPacketSubProcessQueue.Clear();
        }

        private void OnApplicationQuit()
        {
            if (host)
            {
                Server.Close();
            }
        }
    }
}
