using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_TNHInstance
    {
        public int instance = -1;
        public int controller = -1;
        public List<int> playerIDs; // Players in this instance
        public List<int> currentlyPlaying; // Players in-game

        // Settings
        public bool letPeopleJoin;
        public int progressionTypeSetting;
        public int healthModeSetting;
        public int equipmentModeSetting;
        public int targetModeSetting;
        public int AIDifficultyModifier;
        public int radarModeModifier;
        public int itemSpawnerMode;
        public int backpackMode;
        public int healthMult;
        public int sosiggunShakeReloading;
        public int TNHSeed;
        public int levelIndex;

        public H3MP_TNHInstance(int instance, int hostID, bool letPeopleJoin,
                                int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, int levelIndex)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            playerIDs.Add(hostID);

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

        public void AddCurrentlyPlaying(bool send, int ID)
        {
            if (!H3MP_GameManager.TNHInstances[instance].letPeopleJoin && H3MP_GameManager.TNHInstances[instance].currentlyPlaying.Count == 0 &&
                Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(instance))
            {
                GameObject.Destroy(Mod.joinTNHInstances[instance]);
                Mod.joinTNHInstances.Remove(instance);
            }
            currentlyPlaying.Add(ID);

            if (send)
            {
                // Send to other clients
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.AddTNHCurrentlyPlaying(instance);
                }
                else
                {
                    H3MP_ClientSend.AddTNHCurrentlyPlaying(instance);
                }
            }
        }

        public void RemoveCurrentlyPlaying(bool send, int ID)
        {
            if (!H3MP_GameManager.TNHInstances[instance].letPeopleJoin && H3MP_GameManager.TNHInstances[instance].currentlyPlaying.Count - 1 == 0 && Mod.TNHInstanceList != null)
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

            if (send)
            {
                // Send to other clients
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.RemoveTNHCurrentlyPlaying(instance);
                }
                else
                {
                    H3MP_ClientSend.RemoveTNHCurrentlyPlaying(instance);
                }
            }
        }
    }
}
