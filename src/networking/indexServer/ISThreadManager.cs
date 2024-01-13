using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Networking
{
    public class ISThreadManager : MonoBehaviour
    {
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

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="_action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null)
            {
                Mod.LogInfo("ISThreadManager: No action to execute on main thread!", false);
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

            pingTimer -= Time.deltaTime;
            if (pingTimer <= 0)
            {
                pingTimer = pingTime;
                if (ISClient.gotWelcome)
                {
                    ISClientSend.Ping(Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalMilliseconds));
                }
                else
                {
                    ++ISClient.pingAttemptCounter;
                    if (ISClient.pingAttemptCounter >= 5)
                    {
                        Mod.LogWarning("Have not received IS welcome for " + ISClient.pingAttemptCounter + " seconds, timing out at 10");
                    }
                    if (ISClient.pingAttemptCounter >= 10)
                    {
                        ISClient.pingAttemptCounter = 0;
                        ISClient.Disconnect(false, 4);
                    }
                }
            }
        }
    }
}
