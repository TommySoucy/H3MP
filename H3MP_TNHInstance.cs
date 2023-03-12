using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_TNHInstance
    {
        public int instance = -1;
        public int controller = -1;
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

        public H3MP_TNHInstance(int instance)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            currentlyPlaying = new List<int>();
            played = new List<int>();
            dead = new List<int>();
        }

        public H3MP_TNHInstance(int instance, int hostID, bool letPeopleJoin,
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
            Mod.LogInfo("AddCurrentlyPlaying called to add " + ID + " to instance: " + instance+", currently controlled by "+controller);
            if (!letPeopleJoin && currentlyPlaying.Count == 0 &&
                Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(instance))
            {
                GameObject.Destroy(Mod.joinTNHInstances[instance]);
                Mod.joinTNHInstances.Remove(instance);
            }
            currentlyPlaying.Add(ID);
            if (!played.Contains(ID))
            {
                if(ID == H3MP_GameManager.ID)
                {
                    // This is us and it is the first time we go into this game, init
                    ++TNH_ManagerPatch.addTokensSkip;
                    manager.AddTokens(tokenCount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
                played.Add(ID);
            }

            if (ID != H3MP_GameManager.ID)
            {
                H3MP_GameManager.UpdatePlayerHidden(H3MP_GameManager.players[ID]);
            }

            if (fromServer) // Only manage controller if server made this call
            {
                if (ID == playerIDs[0])
                {
                    Mod.LogInfo("\tClient is instance host, giving control");
                    // If new controller is different, distribute sosigs/automeaters/encryptions be cause those should be controlled by TNH controller
                    if (ID != controller)
                    {
                        H3MP_GameManager.DistributeAllControl(controller, ID, false);
                    }
                    controller = ID;
                    H3MP_ServerSend.SetTNHController(instance, ID);
                }
                else // The player who got added is not instance host
                {
                    Mod.LogInfo("\tClient is NOT instance host");
                    if (currentlyPlaying.Count == 1)
                    {
                        Mod.LogInfo("\t\tIs first player, giving control");
                        // If new controller is different, distribute sosigs/automeaters/encryptions be cause those should be controlled by TNH controller
                        if (ID != controller)
                        {
                            H3MP_GameManager.DistributeAllControl(controller, ID, false);
                        }
                        controller = ID;
                        H3MP_ServerSend.SetTNHController(instance, ID);
                    }
                    //else // The player is not the only one
                }
            }

            if (send)
            {
                // Send to other clients
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.AddTNHCurrentlyPlaying(instance, ID);
                }
                else
                {
                    H3MP_ClientSend.AddTNHCurrentlyPlaying(instance);
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
                instanceButton.Button.onClick.AddListener(() => { Mod.modInstance.OnTNHInstanceClicked(instance); });

                Mod.joinTNHInstances.Add(instance, newInstance);
            }

            currentlyPlaying.Remove(ID);

            if (currentlyPlaying.Count == 0)
            {
                Reset();
            }
            else if (fromServer) // If the server is the one who removed a player
            {
                if (currentlyPlaying.Contains(playerIDs[0]))
                {
                    // If new controller is different, distribute sosigs/automeaters/encryptions because those should be controlled by TNH controller
                    if (playerIDs[0] != controller)
                    {
                        H3MP_GameManager.DistributeAllControl(controller, playerIDs[0], false);
                    }

                    H3MP_ServerSend.SetTNHController(instance, playerIDs[0]);

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
                        H3MP_GameManager.DistributeAllControl(controller, currentLowest, false);
                    }

                    H3MP_ServerSend.SetTNHController(instance, currentLowest);

                    // Update on our side
                    controller = currentLowest;
                }
            }

            // Reset initialization fields if we were waiting for init from this player
            if(initializer == ID && initializationRequested)
            {
                initializer = -1;
                initializationRequested = false;
            }

            if (send)
            {
                // Send to other clients
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.RemoveTNHCurrentlyPlaying(instance, ID);
                }
                else
                {
                    H3MP_ClientSend.RemoveTNHCurrentlyPlaying(instance);
                }
            }
        }

        public void RevivePlayer(int ID, bool received = false)
        {
            if (dead != null)
            {
                dead.Remove(ID);
            }

            if (ID == H3MP_GameManager.ID)
            {
                Mod.TNHSpectating = false;

                if (received && manager != null)
                {
                    Mod.TNH_Manager_InitPlayerPosition.Invoke(manager, null);
                }

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.ReviveTNHPlayer(ID, instance, 0);
                }
                else if(!received)
                {
                    H3MP_ClientSend.ReviveTNHPlayer(ID, instance);
                }
            }
            else
            {
                H3MP_GameManager.UpdatePlayerHidden(H3MP_GameManager.players[ID]);

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.ReviveTNHPlayer(ID, instance, 0);
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

            // The game has reset, a new game will be created when a player goes in again, if we were spectating we want to stop
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance)
            {
                Mod.TNHSpectating = false;
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
            Mod.TNH_Manager_m_level.SetValue(manager, -1);
            Mod.TNH_Manager_m_numTokens.SetValue(manager, 5);
            ((List<TNH_SupplyPoint>)Mod.TNH_Manager_m_supplyPoints.GetValue(manager)).Clear();
            ((List<GameObject>)Mod.TNH_Manager_m_weaponCases.GetValue(manager)).Clear();
            ((List<TNH_Manager.SosigPatrolSquad>)Mod.TNH_Manager_m_patrolSquads.GetValue(manager)).Clear();
            ((List<GameObject>)Mod.TNH_Manager_m_miscEnemies.GetValue(manager)).Clear();
            for(int i=0; i < manager.Nums.Length; ++i)
            {
                manager.Nums[i] = 0;
            }
            for(int i=0; i < manager.Stats.Length; ++i)
            {
                manager.Stats[i] = 0;
            }
            Mod.TNH_Manager_m_hasInit.SetValue(manager, false);
            ((List<int>)Mod.TNH_Manager_m_activeSupplyPointIndicies.GetValue(manager)).Clear();
            Mod.TNH_Manager_m_nextSupplyPanelType.SetValue(manager, 1);
        }
    }
}
