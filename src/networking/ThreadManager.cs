using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Networking
{
    internal class ThreadManager : MonoBehaviour
    {
        public static bool host; // Whether this thread manager is the host's

        private static readonly List<Action> executeOnMainThread = new List<Action>();
        private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        private static bool actionToExecuteOnMainThread = false;


        public static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly float pingTime = 1;
        private static float pingTimer = pingTime;

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
                        if(Client.singleton.pingAttemptCounter >= 10)
                        {
                            Mod.LogInfo("Have not received server welcome for " + Client.singleton.pingAttemptCounter + " seconds, timing out at 30", false);
                        }
                        if(Client.singleton.pingAttemptCounter >= 30)
                        {
                            Client.singleton.Disconnect(false, 4);
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
