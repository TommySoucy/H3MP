using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace H3MP
{
    internal class H3MP_ThreadManager : MonoBehaviour
    {
        public static bool host; // Whether this thread manager is the host's

        private static readonly List<Action> executeOnMainThread = new List<Action>();
        private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        private static bool actionToExecuteOnMainThread = false;

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
                Debug.Log("No action to execute on main thread!");
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

                // Also send the host's player state to all clients
                H3MP_ServerSend.PlayerState(0,
                                            GM.CurrentPlayerBody.transform.position,
                                            GM.CurrentPlayerBody.transform.rotation,
                                            GM.CurrentPlayerBody.headPositionFiltered,
                                            GM.CurrentPlayerBody.headRotationFiltered,
                                            GM.CurrentPlayerBody.Torso.position,
                                            GM.CurrentPlayerBody.Torso.rotation,
                                            GM.CurrentPlayerBody.LeftHand.position,
                                            GM.CurrentPlayerBody.LeftHand.rotation,
                                            H3MP_GameManager.hostLeftHandTrackedID,
                                            GM.CurrentPlayerBody.RightHand.position,
                                            GM.CurrentPlayerBody.RightHand.rotation,
                                            H3MP_GameManager.hostRightHandTrackedID);
            }
            else
            {
                // Send this client's up to date trackedItems to host and all other clients
                H3MP_ClientSend.TrackedItems();
            }
        }
    }
}
