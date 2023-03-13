using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace H3MP
{
    internal class H3MP_ThreadManager : MonoBehaviour
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
                Mod.LogInfo("No action to execute on main thread!");
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
            if (actionToExecuteOnMainThread)
            {
                executeCopiedOnMainThread.Clear();
                lock (executeOnMainThread)
                {
                    executeCopiedOnMainThread.AddRange(executeOnMainThread);
                    executeOnMainThread.Clear();
                    actionToExecuteOnMainThread = false;
                }

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
                    if (H3MP_Client.singleton.gotWelcome)
                    {
                        H3MP_ClientSend.Ping(Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalMilliseconds));
                    }
                    else
                    {
                        ++H3MP_Client.singleton.pingAttemptCounter;
                        if(H3MP_Client.singleton.pingAttemptCounter >= 10)
                        {
                            Mod.LogInfo("Have not received server welcome for " + H3MP_Client.singleton.pingAttemptCounter + " seconds, timing out at 30");
                        }
                        if(H3MP_Client.singleton.pingAttemptCounter >= 30)
                        {
                            H3MP_Client.singleton.Disconnect(false, 4);
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
                // Send every client's player state to every other client
                foreach (H3MP_ServerClient client in H3MP_Server.clients.Values)
                {
                    if (client.player != null)
                    {
                        client.player.UpdateState();
                    }
                }

                // Send all trackedItems to all clients
                H3MP_ServerSend.TrackedItems();
                H3MP_ServerSend.TrackedSosigs();
                H3MP_ServerSend.TrackedAutoMeaters();
                H3MP_ServerSend.TrackedEncryptions();

                // Also send the host's player state to all clients
                if (GM.CurrentPlayerBody != null)
                {
                    H3MP_ServerSend.PlayerState(0,
                                                GM.CurrentPlayerBody.transform.position,
                                                GM.CurrentPlayerBody.transform.rotation,
                                                GM.CurrentPlayerBody.headPositionFiltered,
                                                GM.CurrentPlayerBody.headRotationFiltered,
                                                GM.CurrentPlayerBody.headPositionFiltered + H3MP_GameManager.torsoOffset,
                                                GM.CurrentPlayerBody.Torso.rotation,
                                                GM.CurrentPlayerBody.LeftHand.position,
                                                GM.CurrentPlayerBody.LeftHand.rotation,
                                                GM.CurrentPlayerBody.RightHand.position,
                                                GM.CurrentPlayerBody.RightHand.rotation,
                                                GM.CurrentPlayerBody.Health,
                                                GM.CurrentPlayerBody.GetMaxHealthPlayerRaw(),
                                                H3MP_GameManager.scene,
                                                H3MP_GameManager.instance);
                }
            }
            else
            {
                // Send this client's up to date trackedItems to host and all other clients of there are others in the scene
                if (H3MP_GameManager.playersPresent > 0)
                {
                    H3MP_ClientSend.TrackedItems();
                    H3MP_ClientSend.TrackedSosigs();
                    H3MP_ClientSend.TrackedAutoMeaters();
                    H3MP_ClientSend.TrackedEncryptions();

                    // Also send the player state to all clients
                    if (GM.CurrentPlayerBody != null)
                    {
                        H3MP_ClientSend.PlayerState(GM.CurrentPlayerBody.transform.position,
                                                    GM.CurrentPlayerBody.transform.rotation,
                                                    GM.CurrentPlayerBody.headPositionFiltered,
                                                    GM.CurrentPlayerBody.headRotationFiltered,
                                                    GM.CurrentPlayerBody.headPositionFiltered + H3MP_GameManager.torsoOffset,
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
                H3MP_Server.Close();
            }
        }
    }
}
