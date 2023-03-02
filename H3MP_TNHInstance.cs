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
        public int levelIndex;

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
                                int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, int levelIndex)
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
            this.levelIndex = levelIndex;
        }

        public void AddCurrentlyPlaying(bool send, int ID, bool fromServer = false)
        {
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
                    controller = ID;
                    H3MP_ServerSend.SetTNHController(instance, ID);
                }
                else // The player who got added is not instance host
                {
                    if(currentlyPlaying.Count == 1)
                    {
                        if (!H3MP_GameManager.playersByInstanceByScene.TryGetValue(ID == 0 ? SceneManager.GetActiveScene().name : H3MP_Server.clients[ID].player.scene, out Dictionary<int, List<int>> instances) ||
                            !instances.TryGetValue(instance, out List<int> players) || !players.Contains(playerIDs[0]))
                        {
                            controller = ID;
                            H3MP_ServerSend.SetTNHController(instance, ID);
                        }
                        //else // Instance host loading, just wait for them
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
                    H3MP_ServerSend.SetTNHController(instance, currentLowest);

                    // Update on our side
                    controller = currentLowest;
                }
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

            // The game has reset, a new game will be created when a player goes in again, if we were spectating we want to stop
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance)
            {
                Mod.TNHSpectating = false;
            }
        }
    }
}
