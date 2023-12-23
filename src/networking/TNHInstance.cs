using FistVR;
using H3MP.Patches;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Networking
{
    public class TNHInstance
    {
        public int instance = -1;
        private int _controller = -1;
        public int controller { set { Mod.LogInfo("TNH instance at "+instance+" controller being set to "+value+":\n"+Environment.StackTrace, false); _controller = value; } get { return _controller; } }
        public TNH_Manager manager;
        public List<int> playerIDs; // Players in this instance
        public List<int> currentlyPlaying; // Players in-game
        public List<int> played; // Players who have been in-game
        public List<int> dead; // in-game players who are dead
        public int initializer = -1;
        public bool initializationRequested;
        public int tokenCount;
        public bool holdOngoing; // Whether the current hold point has an ongoing hold
        public TNH_HoldPoint.HoldState holdState;
        public List<Vector3> warpInData;
        public float tickDownToID;
        public float tickDownToFailure;
        public bool spawnedStartEquip; // Whether this client has already gotten its start equip spawned
        public int curHoldIndex;
        public int level;
        public TNH_Phase phase = TNH_Phase.StartUp;
        public List<int> activeSupplyPointIndices;
        public List<int> raisedBarriers;
        public List<int> raisedBarrierPrefabIndices;
        public int nextSupplyPanelType = 1;

        // Settings
        public bool letPeopleJoin;
        public int progressionTypeSetting;
        public int healthModeSetting;
        public int equipmentModeSetting;
        public int targetModeSetting;
        public int AIDifficultyModifier = 1; // AI Diff and radar mode defaults are at index 1
        public int radarModeModifier = 1;
        public int itemSpawnerMode;
        public int backpackMode;
        public int healthMult;
        public int sosiggunShakeReloading;
        public int TNHSeed;
        public string levelID;

        public TNHInstance(int instance)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            currentlyPlaying = new List<int>();
            played = new List<int>();
            dead = new List<int>();
        }

        public TNHInstance(int instance, int hostID, bool letPeopleJoin,
                                int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, string levelID)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            playerIDs.Add(hostID);
            currentlyPlaying = new List<int>();
            played = new List<int>();
            dead = new List<int>();

            this.letPeopleJoin = letPeopleJoin;
            this.progressionTypeSetting = progressionTypeSetting;
            this.healthModeSetting = healthModeSetting;
            this.equipmentModeSetting = equipmentModeSetting;
            this.targetModeSetting = targetModeSetting;
            this.AIDifficultyModifier = AIDifficultyModifier;
            this.radarModeModifier = radarModeModifier;
            this.itemSpawnerMode = itemSpawnerMode;
            this.backpackMode = backpackMode;
            this.healthMult = healthMult;
            this.sosiggunShakeReloading = sosiggunShakeReloading;
            this.TNHSeed = TNHSeed;
            this.levelID = levelID;
        }

        public void AddCurrentlyPlaying(bool send, int ID, bool fromServer = false)
        {
            Mod.LogInfo("Adding "+ID+" to TNH instance "+instance+" currently playing", false);
            if (!letPeopleJoin && currentlyPlaying.Count == 0 &&
                Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(instance))
            {
                GameObject.Destroy(Mod.joinTNHInstances[instance]);
                Mod.joinTNHInstances.Remove(instance);
            }
            currentlyPlaying.Add(ID);
            if (!played.Contains(ID))
            {
                if(ID == GameManager.ID)
                {
                    // This is us and it is the first time we go into this game, init
                    ++TNH_ManagerPatch.addTokensSkip;
                    manager.AddTokens(tokenCount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
                played.Add(ID);
            }

            if (ID != GameManager.ID)
            {
                GameManager.UpdatePlayerHidden(GameManager.players[ID]);
            }

            if (fromServer) // Only manage controller if server made this call
            {
                Mod.LogInfo("\tServer", false);
                if (ID == playerIDs[0])
                {
                    Mod.LogInfo("\t\tNew player is host", false);
                    // If new controller is different, distribute sosigs/automeaters/encryptions because those should be controlled by TNH controller
                    if (ID != controller)
                    {
                        Mod.LogInfo("\t\t\tbut not controller, distributing control", false);
                        GameManager.DistributeAllControl(controller, ID, new List<System.Type>() { typeof(TrackedSosigData), typeof(TrackedAutoMeaterData), typeof(TrackedEncryptionData) });
                    }
                    Mod.LogInfo("\t\tSending", false);
                    controller = ID;
                    ServerSend.SetTNHController(instance, ID);
                }
                else // The player who got added is not instance host
                {
                    Mod.LogInfo("\t\tNew player is not host", false);
                    if (currentlyPlaying.Count == 1)
                    {
                        Mod.LogInfo("\t\t\tOnly player", false);
                        // If new controller is different, distribute sosigs/automeaters/encryptions because those should be controlled by TNH controller
                        if (ID != controller)
                        {
                            Mod.LogInfo("\t\t\t\tNot yet controller, distributing control", false);
                            GameManager.DistributeAllControl(controller, ID, new List<System.Type>() { typeof(TrackedSosigData), typeof(TrackedAutoMeaterData), typeof(TrackedEncryptionData) });
                        }
                        Mod.LogInfo("\t\t\tSending", false);
                        controller = ID;
                        ServerSend.SetTNHController(instance, ID);
                    }
                    //else // The player is not the only one
                }
            }

            if (send)
            {
                // Send to other clients
                if (ThreadManager.host)
                {
                    ServerSend.AddTNHCurrentlyPlaying(instance, ID);
                }
                else
                {
                    ClientSend.AddTNHCurrentlyPlaying(instance);
                }
            }
        }

        public void RemoveCurrentlyPlaying(bool send, int ID, bool fromServer = false)
        {
            if ((letPeopleJoin || currentlyPlaying.Count == 0) && Mod.TNHInstanceList != null && Mod.joinTNHInstances != null && !Mod.joinTNHInstances.ContainsKey(instance))
            {
                GameObject newInstance = GameObject.Instantiate<GameObject>(Mod.TNHInstancePrefab, Mod.TNHInstanceList.transform);
                newInstance.transform.GetChild(0).GetComponent<Text>().text = "Instance " + instance;
                newInstance.SetActive(true);

                FVRPointableButton instanceButton = newInstance.AddComponent<FVRPointableButton>();
                instanceButton.SetButton();
                instanceButton.MaxPointingRange = 5;
                instanceButton.Button.onClick.AddListener(() => { Mod.OnTNHInstanceClicked(instance); });

                Mod.joinTNHInstances.Add(instance, newInstance);
            }

            currentlyPlaying.Remove(ID);

            if (currentlyPlaying.Count == 0)
            {
                Reset();
            }
            else if (fromServer) // If the server is the one who removed a player
            {
                // Manager TNH controller
                if (currentlyPlaying.Contains(playerIDs[0]))
                {
                    // If new controller is different, distribute sosigs/automeaters/encryptions because those should be controlled by TNH controller
                    if (playerIDs[0] != controller)
                    {
                        GameManager.DistributeAllControl(controller, playerIDs[0], new List<System.Type>() { typeof(TrackedSosigData), typeof(TrackedAutoMeaterData), typeof(TrackedEncryptionData) });
                    }

                    ServerSend.SetTNHController(instance, playerIDs[0]);

                    // Update on our side
                    controller = playerIDs[0];
                }
                else // New instance host is not currently playing
                {
                    int currentLowest = int.MaxValue;
                    for (int i = 0; i < currentlyPlaying.Count; ++i)
                    {
                        if (currentlyPlaying[i] < currentLowest)
                        {
                            currentLowest = currentlyPlaying[i];
                        }
                    }

                    // If new controller is different, distribute sosigs/automeaters/encryptions be cause those should be controlled by TNH controller
                    if (currentLowest != controller)
                    {
                        GameManager.DistributeAllControl(controller, currentLowest, new List<System.Type>() { typeof(TrackedSosigData), typeof(TrackedAutoMeaterData), typeof(TrackedEncryptionData) });
                    }

                    ServerSend.SetTNHController(instance, currentLowest);

                    // Update on our side
                    controller = currentLowest;
                }
            }

            // Manage if last player spectator host
            if(currentlyPlaying.Count == 1 && GameManager.spectatorHosts.Contains(currentlyPlaying[0]))
            {
                if(currentlyPlaying[0] == GameManager.ID) // Its us
                {
                    // Reset
                    GameManager.spectatorHostControlledBy = -1;

                    if (!GameManager.sceneLoading)
                    {
                        if (!GameManager.scene.Equals("MainMenu3"))
                        {
                            SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                        }
                    }
                    else
                    {
                        GameManager.resetSpectatorHost = true;
                    }
                }
                else if(GameManager.controlledSpectatorHost == currentlyPlaying[0]) // It is a host we control
                {
                    // Give up the host
                    Mod.OnSpectatorHostGiveUpInvoke();
                }

                // If server, need to manage lists/dicts
                if (ThreadManager.host)
                {
                    if (Server.spectatorHostControllers.TryGetValue(currentlyPlaying[0], out int controller))
                    {
                        Server.spectatorHostByController.Remove(controller);
                        Server.spectatorHostControllers.Remove(currentlyPlaying[0]);

                        if (GameManager.spectatorHosts.Contains(currentlyPlaying[0]))
                        {
                            Server.availableSpectatorHosts.Add(currentlyPlaying[0]);
                        }
                    }
                }
            }

            // Reset initialization fields if we were waiting for init from this player
            if (initializer == ID && initializationRequested)
            {
                initializer = -1;
                initializationRequested = false;
            }

            if (send)
            {
                // Send to other clients
                if (ThreadManager.host)
                {
                    ServerSend.RemoveTNHCurrentlyPlaying(instance, ID);
                }
                else
                {
                    ClientSend.RemoveTNHCurrentlyPlaying(instance);
                }
            }
        }

        public bool PlayersStillAlive()
        {
            for (int i = 0; i < Mod.currentTNHInstance.currentlyPlaying.Count; ++i)
            {
                if (!Mod.currentTNHInstance.dead.Contains(Mod.currentTNHInstance.currentlyPlaying[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public void RevivePlayer(int ID, bool received = false)
        {
            if (dead != null)
            {
                dead.Remove(ID);
            }

            if (ID == GameManager.ID)
            {
                Mod.TNHSpectating = false;
                if (GM.CurrentPlayerBody != null)
                {
                    GM.CurrentPlayerBody.SetPlayerIFF(GM.CurrentSceneSettings.DefaultPlayerIFF);
                    if (GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                    {
                        GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>().Mode = FVRViveHand.HandMode.Neutral;
                        GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>().Mode = FVRViveHand.HandMode.Neutral;
                    }
                }

                if (manager != null)
                {
                    if (Mod.TNHStartEquipButton == null)
                    {
                        Mod.TNHStartEquipButton = GameObject.Instantiate(Mod.TNHStartEquipButtonPrefab, GM.CurrentPlayerBody.Head);
                        Mod.TNHStartEquipButton.transform.GetChild(0).GetComponent<FVRPointableButton>().Button.onClick.AddListener(Mod.OnTNHSpawnStartEquipClicked);
                    }

                    if (received)
                    {
                        manager.InitPlayerPosition();
                    }
                }

                if (ThreadManager.host)
                {
                    ServerSend.ReviveTNHPlayer(ID, instance, 0);
                }
                else if(!received)
                {
                    ClientSend.ReviveTNHPlayer(ID, instance);
                }
            }
            else
            {
                GameManager.UpdatePlayerHidden(GameManager.players[ID]);

                if (!received && ThreadManager.host)
                {
                    ServerSend.ReviveTNHPlayer(ID, instance, 0);
                }
            }
        }

        public void Reset()
        {
            dead.Clear();
            played.Clear();
            controller = -1;
            tokenCount = 0;
            holdOngoing = false;
            holdState = TNH_HoldPoint.HoldState.Beginning;
            warpInData = null;
            curHoldIndex = -1;
            level = 0;
            phase = TNH_Phase.StartUp;
            activeSupplyPointIndices = null;
            raisedBarriers = null;
            raisedBarrierPrefabIndices = null;
            spawnedStartEquip = false;
            tickDownToFailure = 120;
            initializationRequested = false;
            initializer = -1;
            nextSupplyPanelType = 1;

            // The game has reset, a new game will be created when a player goes in again, if we were spectating we want to stop
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance)
            {
                Mod.TNHSpectating = false;

                // We make the following checks because we could be resetting because we are changing scene, meaning the vanilla playerbody 
                // may have been destroyed
                if (GM.CurrentPlayerBody != null)
                {
                    GM.CurrentPlayerBody.SetPlayerIFF(GM.CurrentSceneSettings.DefaultPlayerIFF);
                    if (GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                    {
                        GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>().Mode = FVRViveHand.HandMode.Neutral;
                        GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>().Mode = FVRViveHand.HandMode.Neutral;
                    }
                }
            }
        }

        public void ResetManager()
        {
            if(manager == null)
            {
                return;
            }

            phase = TNH_Phase.StartUp;
            manager.UsesClassicPatrolBehavior = true;
            manager.m_level = -1;
            manager.m_numTokens = 5;
            manager.m_supplyPoints.Clear();
            manager.m_weaponCases.Clear();
            manager.m_patrolSquads.Clear();
            manager.m_miscEnemies.Clear();
            for(int i=0; i < manager.Nums.Length; ++i)
            {
                manager.Nums[i] = 0;
            }
            for(int i=0; i < manager.Stats.Length; ++i)
            {
                manager.Stats[i] = 0;
            }
            manager.m_hasInit = false;
            manager.m_activeSupplyPointIndicies.Clear();
            manager.m_nextSupplyPanelType = 1;
        }
    }
}
