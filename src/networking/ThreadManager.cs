using FistVR;
using H3MP.Scripts;
using RootMotion;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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


        public static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly float pingTime = 1;
        public static float pingTimer = pingTime;

        private void Update()
        {
            UpdateMain();
        }

        private void FixedUpdate()
        {
            UpdateMainFixed();
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
#if DEBUG
            if (Input.GetKey(KeyCode.PageDown))
            {
                Mod.LogInfo("DEBUG UPDATE MAIN");
            }
#endif
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
                    Mod.LogInfo("Actions to execute: "+ executeCopiedOnMainThread.Count);
                }
#endif

                for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                {
                    executeCopiedOnMainThread[i]();
                }
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
                        ServerSend.PlayerState(0,
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
                        ClientSend.PlayerState(GM.CurrentPlayerBody.transform.position,
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
