using FistVR;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace H3MP.Networking
{
    public class ThreadManager : MonoBehaviour
    {
        public static bool host; // Whether this thread manager is the host's

        private static readonly List<Action> executeOnMainThread = new List<Action>();
        private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        private static bool actionToExecuteOnMainThread = false;
        public static Queue<KeyValuePair<int, int>> objectsToUpdate = new Queue<KeyValuePair<int, int>>();
        public static Queue<int> playersToUpdate = new Queue<int>();
        public static byte playerOrder = 0;

        public static float tickRate = 20; // How many times a second we will send updates
        public static float time = 1 / tickRate; // Period between sending updates
        public static float timer = 0; // Amount of time left until next tick

        public static int updateLimit = 10; // Time limit (ms) we have to apply received updates before leaving it to the next frame
        public static int updateState = 0; // The packet ID we are currently at in updates. 0: Main queue, 1: TrackedObjects, 2: Player state
        public static int updateStateIndex = 0; // A generic index that can be used if updates have a sub state (Like in trackedObjects packet ID, we will have the index of the object we are at)
        public static int updateStateLimit = 0; // Limit of how may elements we can process this iteration, in case number of elements grew in between frames

        public static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly float pingTime = 1;
        public static float pingTimer = pingTime;

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

            if(updateState == 0)
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

                            if(start.ElapsedMilliseconds >= updateLimit)
                            {
                                return;
                            }
                        }
                    }
                }

                updateState = 1;
                updateStateIndex = 0;
            }

            // Update objects
            if (host)
            {
                if (updateState == 1)
                {
                    if(updateStateIndex == 0)
                    {
                        updateStateLimit = objectsToUpdate.Count;
                    }

                    while (objectsToUpdate.Count > 0 && updateStateIndex < updateStateLimit)
                    {
                        KeyValuePair<int, int> pair = objectsToUpdate.Dequeue();
                        TrackedObjectData trackedObjectData = Server.objects[pair.Key];
                        trackedObjectData.UpdateFromPacket();

                        // Relay to other clients in the same scene/instance
                        if (GameManager.playersByInstanceByScene.TryGetValue(Server.clients[pair.Value].player.scene, out Dictionary<int, List<int>> instances) &&
                            instances.TryGetValue(Server.clients[pair.Value].player.instance, out List<int> players))
                        {
                            ServerSend.TrackedObjects(trackedObjectData.latestUpdate, players, pair.Value);
                        }

                        trackedObjectData.latestUpdate.Dispose();

                        updateStateIndex++;

                        if (start.ElapsedMilliseconds >= updateLimit)
                        {
                            return;
                        }
                    }

                    updateState = 2;
                    updateStateIndex = 0;
                }

                if(updateState == 2)
                {
                    if (updateStateIndex == 0)
                    {
                        updateStateLimit = objectsToUpdate.Count;
                    }

                    while (playersToUpdate.Count > 0 && updateStateIndex < updateStateLimit)
                    {
                        int playerID = playersToUpdate.Dequeue();
                        Player player = Server.clients[playerID].player;
                        PlayerManager playerManager = GameManager.players[playerID];
                        playerManager.order = playerManager.latestOrder;

                        player.position = playerManager.latestUpdate.ReadVector3();
                        player.rotation = playerManager.latestUpdate.ReadQuaternion();
                        player.headPos = playerManager.latestUpdate.ReadVector3();
                        player.headRot = playerManager.latestUpdate.ReadQuaternion();
                        player.torsoPos = playerManager.latestUpdate.ReadVector3();
                        player.torsoRot = playerManager.latestUpdate.ReadQuaternion();
                        player.leftHandPos = playerManager.latestUpdate.ReadVector3();
                        player.leftHandRot = playerManager.latestUpdate.ReadQuaternion();
                        player.rightHandPos = playerManager.latestUpdate.ReadVector3();
                        player.rightHandRot = playerManager.latestUpdate.ReadQuaternion();
                        player.health = playerManager.latestUpdate.ReadFloat();
                        player.maxHealth = playerManager.latestUpdate.ReadInt();
                        short additionalDataLength = playerManager.latestUpdate.ReadShort();
                        byte[] additionalData = null;
                        if (additionalDataLength > 0)
                        {
                            additionalData = playerManager.latestUpdate.ReadBytes(additionalDataLength);
                        }

                        GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                                      player.leftHandPos, player.leftHandRot,
                                                      player.rightHandPos, player.rightHandRot,
                                                      player.health, player.maxHealth, additionalData);

                        // Relay
                        ServerSend.PlayerState(playerManager.latestUpdate, player);

                        playerManager.latestUpdate.Dispose();

                        updateStateIndex++;

                        if (start.ElapsedMilliseconds >= updateLimit)
                        {
                            return;
                        }
                    }

                    updateState = 0;
                    updateStateIndex = 0;
                }
            }
            else
            {
                if (updateState == 1)
                {
                    if (updateStateIndex == 0)
                    {
                        updateStateLimit = objectsToUpdate.Count;
                    }

                    while (objectsToUpdate.Count > 0)
                    {
                        TrackedObjectData trackedObjectData = Client.objects[objectsToUpdate.Dequeue().Key];
                        trackedObjectData.UpdateFromPacket();
                        trackedObjectData.latestUpdate.Dispose();

                        updateStateIndex++;

                        if (start.ElapsedMilliseconds >= updateLimit)
                        {
                            return;
                        }
                    }

                    updateState = 2;
                    updateStateIndex = 0;
                }

                if (updateState == 2)
                {
                    if (updateStateIndex == 0)
                    {
                        updateStateLimit = objectsToUpdate.Count;
                    }

                    while (playersToUpdate.Count > 0)
                    {
                        PlayerManager playerManager = GameManager.players[playersToUpdate.Dequeue()];
                        playerManager.order = playerManager.latestOrder;

                        Vector3 position = playerManager.latestUpdate.ReadVector3();
                        Quaternion rotation = playerManager.latestUpdate.ReadQuaternion();
                        Vector3 headPos = playerManager.latestUpdate.ReadVector3();
                        Quaternion headRot = playerManager.latestUpdate.ReadQuaternion();
                        Vector3 torsoPos = playerManager.latestUpdate.ReadVector3();
                        Quaternion torsoRot = playerManager.latestUpdate.ReadQuaternion();
                        Vector3 leftHandPos = playerManager.latestUpdate.ReadVector3();
                        Quaternion leftHandRot = playerManager.latestUpdate.ReadQuaternion();
                        Vector3 rightHandPos = playerManager.latestUpdate.ReadVector3();
                        Quaternion rightHandRot = playerManager.latestUpdate.ReadQuaternion();
                        float health = playerManager.latestUpdate.ReadFloat();
                        int maxHealth = playerManager.latestUpdate.ReadInt();
                        short additionalDataLength = playerManager.latestUpdate.ReadShort();
                        byte[] additionalData = null;
                        if (additionalDataLength > 0)
                        {
                            additionalData = playerManager.latestUpdate.ReadBytes(additionalDataLength);
                        }

                        GameManager.UpdatePlayerState(playerManager.ID, position, rotation, headPos, headRot, torsoPos, torsoRot,
                                                      leftHandPos, leftHandRot,
                                                      rightHandPos, rightHandRot,
                                                      health, maxHealth, additionalData);

                        playerManager.latestUpdate.Dispose();

                        updateStateIndex++;

                        if (start.ElapsedMilliseconds >= updateLimit)
                        {
                            return;
                        }
                    }

                    updateState = 0;
                    updateStateIndex = 0;
                }
            }
            objectsToUpdate.Clear();
            playersToUpdate.Clear();

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
                        ServerSend.PlayerState(playerOrder++,
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
                        ClientSend.PlayerState(playerOrder++,
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

        private void OnApplicationQuit()
        {
            if (host)
            {
                Server.Close();
            }
        }
    }
}
