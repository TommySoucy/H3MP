using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace H3MP
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _singleton;
        public static GameManager singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Mod.LogInfo($"{nameof(GameManager)} instance already exists, destroying duplicate!", false);
                    Destroy(value);
                }
            }
        }

        public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();
        public static List<int> spectatorHosts = new List<int>(); // List of all spectator hosts, not necessarily available 
        public static List<TrackedObjectData> objects = new List<TrackedObjectData>(); // Tracked objects under control of this gameManager
        public static Dictionary<string, int> nonSynchronizedScenes = new Dictionary<string, int>(); // Dict of scenes that can be synced
        public static Dictionary<MonoBehaviour, TrackedObject> trackedObjectByObject = new Dictionary<MonoBehaviour, TrackedObject>();
        public static Dictionary<FVRPhysicalObject, TrackedItem> trackedItemByItem = new Dictionary<FVRPhysicalObject, TrackedItem>();
        public static Dictionary<SosigWeapon, TrackedItem> trackedItemBySosigWeapon = new Dictionary<SosigWeapon, TrackedItem>();
        public static Dictionary<Sosig, TrackedSosig> trackedSosigBySosig = new Dictionary<Sosig, TrackedSosig>();
        public static Dictionary<AutoMeater, TrackedAutoMeater> trackedAutoMeaterByAutoMeater = new Dictionary<AutoMeater, TrackedAutoMeater>();
        public static Dictionary<TNH_EncryptionTarget, TrackedEncryption> trackedEncryptionByEncryption = new Dictionary<TNH_EncryptionTarget, TrackedEncryption>();
        public static Dictionary<int, int> activeInstances = new Dictionary<int, int>();
        public static Dictionary<int, TNHInstance> TNHInstances = new Dictionary<int, TNHInstance>();
        public static List<int> playersAtLoadStart;
        public static Dictionary<string, Dictionary<int, List<int>>> playersByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> objectsByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> itemsByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> sosigsByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> autoMeatersByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> encryptionsByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();

        public static int giveControlOfDestroyed;
        public static bool controlOverride;
        public static bool firstPlayerInSceneInstance;
        public static bool dontAddToInstance;
        public static bool inPostSceneLoadTrack;

        public static int ID = 0;
        public static Vector3 torsoOffset = new Vector3(0, -0.4f, 0);
        public static Vector3 overheadDisplayOffset = new Vector3(0, 0.25f, 0);
        public static int playersPresent = 0;
        public static int playerStateAddtionalDataSize = -1;
        public static int instance = 0;
        public static string scene = "MainMenu3";
        public static bool sceneLoading;
        public static int instanceAtSceneLoadStart;
        public static string sceneAtSceneLoadStart;
        public static bool connectedAtLoadStart;
        public static int colorIndex = 0;
        public static readonly string[] colorNames = new string[] { "White", "Red", "Green", "Blue", "Black", "Desert", "Forest" };
        public static readonly Color[] colors = new Color[] { Color.white, Color.red, Color.green, Color.blue, Color.black, new Color(0.98431f, 0.86275f, 0.71373f), new Color(0.31373f, 0.31373f, 0.15294f) };
        public static bool colorByIFF = false; 
        public static int nameplateMode = 1; // 0: All, 1: Friendly only (same IFF), 2: None 
        public static int radarMode = 0; // 0: All, 1: Friendly only (same IFF), 2: None 
        public static bool radarColor = true; // True: Colored by IFF, False: Colored by color
        public static bool overrideMaxHealthSetting = false; // Ignore max health setting, used by a mod if health shouldn't be set by max health setting
        public static float[] maxHealths = new float[] { 1, 500, 1000, 2000, 3000, 4000, 5000, 7500, 10000 };
        public static int maxHealthIndex = -1;
        public static Dictionary<string, Dictionary<int, KeyValuePair<float, int>>> maxHealthByInstanceByScene = new Dictionary<string, Dictionary<int, KeyValuePair<float, int>>>();

        public static long ping = -1;

        //public GameObject localPlayerPrefab;
        public GameObject playerPrefab;

        private void Awake()
        {
            singleton = this;

            // Init the main instance
            activeInstances.Add(instance, 1);
        }

        public void SpawnPlayer(int ID, string username, string scene, int instance, Vector3 position, Quaternion rotation, int IFF, int colorIndex, bool join = false)
        {
            Mod.LogInfo($"Spawn player called with ID: {ID}", false);

            GameObject player = null;
            // Always spawn if this is host (client is null)
            if(Client.singleton == null || ID != Client.singleton.ID)
            {
                player = Instantiate(playerPrefab);
                DontDestroyOnLoad(player);
            }
            else
            {
                // We dont want to spawn the local player as we will already have spawned when connecting to a server
                return;
            }

            PlayerManager playerManager = player.GetComponent<PlayerManager>();
            playerManager.ID = ID;
            playerManager.username = username;
            playerManager.scene = scene;
            playerManager.instance = instance;
            playerManager.usernameLabel.text = username;
            playerManager.SetIFF(IFF);
            playerManager.SetColor(colorIndex);
            players.Add(ID, playerManager);

            // Add to scene/instance
            bool firstInSceneInstance = false;
            if (playersByInstanceByScene.TryGetValue(scene, out Dictionary<int,List<int>> relevantInstances))
            {
                if (relevantInstances.TryGetValue(instance, out List<int> relevantPlayers))
                {
                    relevantPlayers.Add(ID);
                    firstInSceneInstance = relevantPlayers.Count == 1;
                }
                else // We have scene but not instance, add instance
                {
                    relevantInstances.Add(instance, new List<int>() { ID });
                    firstInSceneInstance = true;
                }
            }
            else // We don't have scene, add scene
            {
                Dictionary<int,List<int>> newInstances = new Dictionary<int,List<int>>();
                newInstances.Add(instance, new List<int>() { ID });
                playersByInstanceByScene.Add(scene, newInstances);
                firstInSceneInstance = true;
            }

            firstInSceneInstance &= sceneLoading || !scene.Equals(GameManager.scene) || instance != GameManager.instance;

            playerManager.firstInSceneInstance = firstInSceneInstance;

            if (ThreadManager.host)
            {
                Server.clients[ID].player.firstInSceneInstance = firstInSceneInstance;
            }

            // Add to instance
            if (activeInstances.ContainsKey(instance))
            {
                ++activeInstances[instance];
            }
            else
            {
                activeInstances.Add(instance, 1);
            }


            UpdatePlayerHidden(playerManager);

            // Make sure to count the player if in the same scene/instance
            if (!GameManager.nonSynchronizedScenes.ContainsKey(scene) && scene.Equals(GameManager.scene) && instance == GameManager.instance)
            {
                ++playersPresent;

                if (join)
                {
                    // This is a spawn player order from the server since we just joined it, we are not first in the scene
                    GameManager.firstPlayerInSceneInstance = false;
                }
            }
        }

        public static void Reset()
        {
            foreach(KeyValuePair<int, PlayerManager> playerEntry in players)
            {
                Destroy(playerEntry.Value.gameObject);
            }
            players.Clear();
            spectatorHosts.Clear();
            trackedItemByItem.Clear();
            trackedSosigBySosig.Clear();
            trackedItemBySosigWeapon.Clear();
            trackedAutoMeaterByAutoMeater.Clear();
            trackedEncryptionByEncryption.Clear();
            activeInstances.Clear();
            TNHInstances.Clear();
            playersByInstanceByScene.Clear();
            itemsByInstanceByScene.Clear();
            sosigsByInstanceByScene.Clear();
            autoMeatersByInstanceByScene.Clear();
            encryptionsByInstanceByScene.Clear();
            ID = 0;
            instance = 0;
            giveControlOfDestroyed = 0;
            controlOverride = false;
            firstPlayerInSceneInstance = false;
            dontAddToInstance = false;
            playersPresent = 0;
            playerStateAddtionalDataSize = -1;
            sceneLoading = false;
            instanceAtSceneLoadStart = 0;
            ping = -1;
            colorByIFF = false;
            nameplateMode = 1;
            radarMode = 0;
            radarColor = true;
            maxHealthIndex = -1;
            maxHealthByInstanceByScene.Clear();

            for (int i=0; i< TrackedItem.trackedItemRefObjects.Length; ++i)
            {
                if (TrackedItem.trackedItemRefObjects[i] != null)
                {
                    Destroy(TrackedItem.trackedItemRefObjects[i]);
                }
            }
            TrackedItem.trackedItemRefObjects = new GameObject[100];
            TrackedItem.trackedItemReferences = new TrackedItem[100];
            TrackedItem.availableTrackedItemRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                10,11,12,13,14,15,16,17,18,19,
                                                                                20,21,22,23,24,25,26,27,28,29,
                                                                                30,31,32,33,34,35,36,37,38,39,
                                                                                40,41,42,43,44,45,46,47,48,49,
                                                                                50,51,52,53,54,55,56,57,58,59,
                                                                                60,61,62,63,64,65,66,67,68,69,
                                                                                70,71,72,73,74,75,76,77,78,79,
                                                                                80,81,82,83,84,85,86,87,88,89,
                                                                                90,91,92,93,94,95,96,97,98,99};

            TrackedItem.unknownTrackedIDs.Clear();
            TrackedItem.unknownParentWaitList.Clear();
            TrackedItem.unknownControlTrackedIDs.Clear();
            TrackedItem.unknownDestroyTrackedIDs.Clear();
            TrackedItem.unknownParentTrackedIDs.Clear();
            TrackedItem.unknownCrateHolding.Clear();
            TrackedItem.unknownSosigInventoryItems.Clear();
            TrackedItem.unknownSosigInventoryObjects.Clear();

            TrackedSosig.unknownBodyStates.Clear();
            TrackedSosig.unknownControlTrackedIDs.Clear();
            TrackedSosig.unknownDestroyTrackedIDs.Clear();
            TrackedSosig.unknownIFFChart.Clear();
            TrackedSosig.unknownItemInteract.Clear();
            TrackedSosig.unknownSetIFFs.Clear();
            TrackedSosig.unknownSetOriginalIFFs.Clear();
            TrackedSosig.unknownTNHKills.Clear();
            TrackedSosig.unknownCurrentOrder.Clear();
            TrackedSosig.unknownConfiguration.Clear();

            TrackedAutoMeater.unknownControlTrackedIDs.Clear();
            TrackedAutoMeater.unknownDestroyTrackedIDs.Clear();

            TrackedEncryption.unknownControlTrackedIDs.Clear();
            TrackedEncryption.unknownDestroyTrackedIDs.Clear();
            TrackedEncryption.unknownDisableSubTarg.Clear();
            TrackedEncryption.unknownInit.Clear();
            TrackedEncryption.unknownResetGrowth.Clear();
            TrackedEncryption.unknownSpawnGrowth.Clear();
            TrackedEncryption.unknownSpawnSubTarg.Clear();
        }

        public static void UpdatePlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                             Vector3 leftHandPos, Quaternion leftHandRot,
                                             Vector3 rightHandPos, Quaternion rightHandRot,
                                             float health, int maxHealth, byte[] additionalData)
        {
            if (!players.ContainsKey(ID))
            {
                Mod.LogWarning($"Received UDP order to update player {ID} state but player of this ID hasnt been spawned yet");
                return;
            }

            PlayerManager player = players[ID];

            Transform playerTransform = player.transform;

            playerTransform.position = position;
            playerTransform.rotation = rotation;
            player.head.transform.position = headPos;
            player.head.transform.rotation = headRot;
            player.torso.transform.position = torsoPos;
            player.torso.transform.rotation = torsoRot;
            player.leftHand.transform.position = leftHandPos;
            player.leftHand.transform.rotation = leftHandRot;
            player.rightHand.transform.position = rightHandPos;
            player.rightHand.transform.rotation = rightHandRot;
            player.overheadDisplayBillboard.transform.position = player.head.transform.position + overheadDisplayOffset;
            player.health = health;
            if (player.healthIndicator.gameObject.activeSelf)
            {
                player.healthIndicator.text = ((int)health).ToString() + "/" + maxHealth;
            }

            if((health <= 0 && player.visible) || (health > 0 && !player.visible))
            {
                UpdatePlayerHidden(player);
            }

            ProcessAdditionalPlayerData(ID, additionalData);
        }

        public static void UpdatePlayerScene(int playerID, string sceneName)
        {
            Mod.LogInfo("Player " + playerID + " joining scene " + sceneName, false);
            PlayerManager player = null;
            if (!players.TryGetValue(playerID, out player))
            {
                // Player not yet spawned, which can happen if received scene update of another player while server was still sending us welcome
                return;
            }

            // Remove from scene/instance
            playersByInstanceByScene[player.scene][player.instance].Remove(player.ID);
            if (playersByInstanceByScene[player.scene][player.instance].Count == 0)
            {
                playersByInstanceByScene[player.scene].Remove(player.instance);
            }
            if (playersByInstanceByScene[player.scene].Count == 0)
            {
                playersByInstanceByScene.Remove(player.scene);
            }

            if (player.scene.Equals(GameManager.scene) && !GameManager.nonSynchronizedScenes.ContainsKey(player.scene) && instance == player.instance)
            {
                --playersPresent;
            }

            player.scene = sceneName;

            // Add to scene/instance
            bool firstInSceneInstance = false;
            if (playersByInstanceByScene.TryGetValue(player.scene, out Dictionary<int, List<int>> relevantInstances))
            {
                if (relevantInstances.TryGetValue(player.instance, out List<int> relevantPlayers))
                {
                    relevantPlayers.Add(player.ID);
                    firstInSceneInstance = relevantPlayers.Count == 1;
                }
                else // We have scene but not instance, add instance
                {
                    relevantInstances.Add(player.instance, new List<int>() { player.ID });
                    firstInSceneInstance = true;
                }
            }
            else // We don't have scene, add scene
            {
                Dictionary<int, List<int>> newInstances = new Dictionary<int, List<int>>();
                newInstances.Add(player.instance, new List<int>() { player.ID });
                playersByInstanceByScene.Add(player.scene, newInstances);
                firstInSceneInstance = true;
            }

            firstInSceneInstance &= sceneLoading || !player.scene.Equals(scene) || player.instance != instance;

            player.firstInSceneInstance = firstInSceneInstance;

            if (ThreadManager.host)
            {
                Server.clients[playerID].player.scene = sceneName;

                Server.clients[playerID].player.firstInSceneInstance = firstInSceneInstance;
            }

            UpdatePlayerHidden(player);

            if (sceneName.Equals(GameManager.scene) && !GameManager.nonSynchronizedScenes.ContainsKey(sceneName) && instance == player.instance)
            {
                ++playersPresent;
            }
        }

        // MOD: This will be called to set a player as hidden based on certain criteria
        //      Currently sets a player as hidden if they are in the same TNH game as us and are dead for example
        //      A mod could prefix this to base it on other criteria, mainly for other game modes
        public static bool UpdatePlayerHidden(PlayerManager player)
        {
            bool visible = true;

            // Default scene/instance, spectatorHost
            visible &= !nonSynchronizedScenes.ContainsKey(player.scene) && player.scene.Equals(scene) && player.instance == instance && !spectatorHosts.Contains(player.ID) && player.health > 0;

            // TNH
            if (visible && Mod.currentTNHInstance != null)
            {
                // Update visibility
                visible &= !Mod.currentTNHInstance.dead.Contains(player.ID);

                // Process visibility
                if(!visible && player.reticleContact != null)
                {
                    if(Mod.currentTNHInstance.manager != null && Mod.currentTNHInstance.manager.TAHReticle != null)
                    {
                        for (int i = 0; i < Mod.currentTNHInstance.manager.TAHReticle.Contacts.Count; ++i)
                        {
                            if (Mod.currentTNHInstance.manager.TAHReticle.Contacts[i] == player.reticleContact)
                            {
                                GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                UnityEngine.Object.Destroy(Mod.currentTNHInstance.manager.TAHReticle.Contacts[i].gameObject);
                                Mod.currentTNHInstance.manager.TAHReticle.Contacts.RemoveAt(i);
                                player.reticleContact = null;
                                break;
                            }
                        }
                    }
                }
                else if(visible && player.reticleContact == null)
                {
                    if (Mod.currentTNHInstance.manager != null && Mod.currentTNHInstance.currentlyPlaying.Contains(player.ID))
                    {
                        // We are currently in a TNH game with this player, add them to radar depending on mode
                        switch (radarMode)
                        {
                            case 0:
                                player.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(player.head, (TAH_ReticleContact.ContactType)(radarColor ? (player.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : player.colorIndex - 4)); // <= -2 is a custom value handled by TAHReticleContactPatch
                                break;
                            case 1:
                                if (player.IFF == GM.CurrentPlayerBody.GetPlayerIFF())
                                {
                                    player.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(player.head, (TAH_ReticleContact.ContactType)(radarColor ? (player.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : player.colorIndex - 4)); // <= -2 is a custom value handled by TAHReticleContactPatch
                                }
                                break;
                        }
                    }
                }
            }

            // If have not found a reason for player to be hidden, set as visible
            player.SetVisible(visible);
            return visible;
        }

        public static void UpdatePlayerInstance(int playerID, int instance)
        {
            Mod.LogInfo("Player " + playerID + " joining instance " + instance);
            PlayerManager player = players[playerID];

            if (activeInstances.ContainsKey(player.instance))
            {
                --activeInstances[player.instance];
                if (activeInstances[player.instance] == 0)
                {
                    activeInstances.Remove(player.instance);
                }
            }

            if (TNHInstances.TryGetValue(player.instance, out TNHInstance currentInstance))
            {
                int preHost = currentInstance.playerIDs[0];
                currentInstance.playerIDs.Remove(playerID);
                if (currentInstance.playerIDs.Count == 0)
                {
                    TNHInstances.Remove(player.instance);

                    if (Mod.TNHInstanceList != null && Mod.joinTNHInstances != null && Mod.joinTNHInstances.ContainsKey(instance))
                    {
                        GameObject.Destroy(Mod.joinTNHInstances[instance]);
                        Mod.joinTNHInstances.Remove(instance);
                    }

                    if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == player.instance)
                    {
                        Mod.TNHSpectating = false;
                    }
                }
                else
                {
                    // Remove player from active TNH player list
                    if (Mod.TNHMenu != null && Mod.TNHPlayerList != null && Mod.TNHPlayerPrefab != null &&
                        Mod.currentTNHInstancePlayers != null && Mod.currentTNHInstancePlayers.ContainsKey(playerID))
                    {
                        Destroy(Mod.currentTNHInstancePlayers[playerID]);
                        Mod.currentTNHInstancePlayers.Remove(playerID);

                        // Switch host if necessary
                        if (preHost != currentInstance.playerIDs[0])
                        {
                            Mod.currentTNHInstancePlayers[currentInstance.playerIDs[0]].transform.GetChild(0).GetComponent<Text>().text += " (Host)";
                        }
                    }

                    // Remove from currently playing and dead if necessary
                    currentInstance.currentlyPlaying.Remove(playerID);
                    currentInstance.played.Remove(playerID);
                    currentInstance.dead.Remove(playerID);
                }
            }

            // Remove from scene/instance
            playersByInstanceByScene[player.scene][player.instance].Remove(player.ID);
            if(playersByInstanceByScene[player.scene][player.instance].Count == 0)
            {
                playersByInstanceByScene[player.scene].Remove(player.instance);
            }
            // NOTE: No need to check if scene has any instances since here the player's scene doesn't change, only the instance
            // So the scene is guaranteed to remain
            //if(playersByInstanceByScene[player.scene].Count == 0)
            //{
            //    playersByInstanceByScene.Remove(player.scene);
            //}

            if (player.scene.Equals(GameManager.scene) && !GameManager.nonSynchronizedScenes.ContainsKey(player.scene) && GameManager.instance == player.instance)
            {
                --playersPresent;
            }

            player.instance = instance;

            // Add to instance
            bool firstInSceneInstance = false;
            if (playersByInstanceByScene[player.scene].TryGetValue(instance, out List<int> relevantPlayers))
            {
                relevantPlayers.Add(player.ID);
                firstInSceneInstance = relevantPlayers.Count == 1;
            }
            else // We have scene but not instance, add instance
            {
                playersByInstanceByScene[player.scene].Add(instance, new List<int>() { player.ID });
                firstInSceneInstance = true;
            }

            firstInSceneInstance &= sceneLoading || !player.scene.Equals(scene) || player.instance != GameManager.instance;

            player.firstInSceneInstance = firstInSceneInstance;

            if (ThreadManager.host)
            {
                Server.clients[playerID].player.instance = instance;

                Server.clients[playerID].player.firstInSceneInstance = firstInSceneInstance;
            }

            UpdatePlayerHidden(player);

            if (player.scene.Equals(GameManager.scene) && !GameManager.nonSynchronizedScenes.ContainsKey(player.scene) && GameManager.instance == player.instance)
            {
                ++playersPresent;
            }

            if (activeInstances.ContainsKey(instance))
            {
                ++activeInstances[instance];
            }
            else
            {
                activeInstances.Add(instance, 1);
            }

            // The player's ID could already have been added to the TNH instance if their are the host of the instance and
            // have just created it, at which point we just don't want to add them again
            if (TNHInstances.ContainsKey(instance) && !TNHInstances[instance].playerIDs.Contains(playerID))
            {
                TNHInstances[instance].playerIDs.Add(playerID);

                // Add player to active TNH player list
                if (Mod.TNHMenu != null && Mod.TNHPlayerList != null && Mod.TNHPlayerPrefab != null &&
                    Mod.currentTNHInstancePlayers != null && !Mod.currentTNHInstancePlayers.ContainsKey(playerID))
                {
                    GameObject newPlayerElement = Instantiate<GameObject>(Mod.TNHPlayerPrefab, Mod.TNHPlayerList.transform);
                    newPlayerElement.transform.GetChild(0).GetComponent<Text>().text = player.username;
                    newPlayerElement.SetActive(true);

                    Mod.currentTNHInstancePlayers.Add(playerID, newPlayerElement);
                }
            }
        }

        public static void SetPlayerColor(int ID, int index, bool received = false, int clientID = 0, bool send = true)
        {
            if(GameManager.ID == ID)
            {
                colorIndex = index;

                if(WristMenuSection.colorText != null)
                {
                    WristMenuSection.colorText.text = "Current color: " + colorNames[colorIndex];
                }
            }
            else
            {
                players[ID].SetColor(index);

                if (ThreadManager.host)
                {
                    Server.clients[ID].player.colorIndex = index;
                }
            }

            if (send)
            {
                if (ThreadManager.host)
                {
                    ServerSend.PlayerColor(ID, index, clientID);
                }
                else if (!received)
                {
                    ClientSend.PlayerColor(ID, index);
                }
            }
        }
        
        public static void UpdateTrackedItem(TrackedItemData updatedItem, bool ignoreOrder = false)
        {
            if(updatedItem.trackedID == -1)
            {
                return;
            }

            TrackedItemData trackedItemData = null;
            if (ThreadManager.host)
            {
                if (updatedItem.trackedID < Server.items.Length)
                {
                    trackedItemData = Server.items[updatedItem.trackedID];
                }
            }
            else
            {
                if (updatedItem.trackedID < Client.items.Length)
                {
                    trackedItemData = Client.items[updatedItem.trackedID];
                }
            }

            // TODO: Review: Should we keep the up to date data for later if we dont have the tracked item yet?
            //               Concern is that if we send tracked item TCP packet, but before that arrives, we send the insurance updates
            //               meaning we don't have the item for those yet and so when we receive the item itself, we don't have the most up to date
            //               We could keep only the highest order in a dict by trackedID
            if (trackedItemData != null)
            {
                // If we take control of an item, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this item
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedItemData.controller != ID && (ignoreOrder || ((updatedItem.order > trackedItemData.order || trackedItemData.order - updatedItem.order > 128))))
                {
                    trackedItemData.Update(updatedItem);
                }
            }
        }

        public static void UpdateTrackedSosig(TrackedSosigData updatedSosig, bool ignoreOrder = false)
        {
            if(updatedSosig.trackedID == -1)
            {
                return;
            }

            TrackedSosigData trackedSosigData = null;
            if (ThreadManager.host)
            {
                if (updatedSosig.trackedID < Server.sosigs.Length)
                {
                    trackedSosigData = Server.sosigs[updatedSosig.trackedID];
                }
            }
            else
            {
                if (updatedSosig.trackedID < Client.sosigs.Length)
                {
                    trackedSosigData = Client.sosigs[updatedSosig.trackedID];
                }
            }

            if (trackedSosigData != null)
            {
                // If we take control of a sosig, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedSosigData.controller != GameManager.ID && (ignoreOrder || ((updatedSosig.order > trackedSosigData.order || trackedSosigData.order - updatedSosig.order > 128))))
                {
                    trackedSosigData.Update(updatedSosig);
                }
            }
        }

        public static void UpdateTrackedAutoMeater(TrackedAutoMeaterData updatedAutoMeater, bool ignoreOrder = false)
        {
            if(updatedAutoMeater.trackedID == -1)
            {
                return;
            }

            TrackedAutoMeaterData trackedAutoMeaterData = null;
            if (ThreadManager.host)
            {
                if (updatedAutoMeater.trackedID < Server.autoMeaters.Length)
                {
                    trackedAutoMeaterData = Server.autoMeaters[updatedAutoMeater.trackedID];
                }
            }
            else
            {
                if (updatedAutoMeater.trackedID < Client.autoMeaters.Length)
                {
                    trackedAutoMeaterData = Client.autoMeaters[updatedAutoMeater.trackedID];
                }
            }

            if (trackedAutoMeaterData != null)
            {
                // If we take control of a AutoMeater, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if(trackedAutoMeaterData.controller != GameManager.ID && (ignoreOrder || ((updatedAutoMeater.order > trackedAutoMeaterData.order || trackedAutoMeaterData.order - updatedAutoMeater.order > 128))))
                {
                    trackedAutoMeaterData.Update(updatedAutoMeater);
                }
            }
        }

        public static void UpdateTrackedEncryption(TrackedEncryptionData updatedEncryption, bool ignoreOrder = false)
        {
            if(updatedEncryption.trackedID == -1)
            {
                return;
            }

            TrackedEncryptionData trackedEncryptionData = null;
            if (ThreadManager.host)
            {
                if (updatedEncryption.trackedID < Server.encryptions.Length)
                {
                    trackedEncryptionData = Server.encryptions[updatedEncryption.trackedID];
                }
            }
            else
            {
                if (updatedEncryption.trackedID < Client.encryptions.Length)
                {
                    trackedEncryptionData = Client.encryptions[updatedEncryption.trackedID];
                }
            }

            if (trackedEncryptionData != null)
            {
                // If we take control of a encryption, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedEncryptionData.controller != GameManager.ID && (ignoreOrder || ((updatedEncryption.order > trackedEncryptionData.order || trackedEncryptionData.order - updatedEncryption.order > 128))))
                {
                    trackedEncryptionData.Update(updatedEncryption);
                }
            }
        }

        public static void SyncTrackedObjects(bool init = false, bool inControl = false)
        {
            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other items get destroyed. We will receive any item that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach(GameObject root in roots)
            {
                SyncTrackedObjects(root.transform, init ? inControl : controlOverride, null, GameManager.scene);
            }
        }

        public static void SyncTrackedObjects(Transform root, bool controlEverything, TrackedObjectData parent, string scene)
        {
            if (GetTrackedObjectType(root, out Type trackedObjectType))
            {
                // Check if already tracked
                TrackedObject currentTrackedObject = root.GetComponent<TrackedObject>();
                if (currentTrackedObject == null)
                {
                    // Check if we want to track this on our side, so if we are controlling it
                    if (controlEverything || IsControlled(root, trackedObjectType))
                    {
                        TrackedObject trackedObject = MakeObjectTracked(root, parent, trackedObjectType);
                        if (trackedObject != null)
                        {
                            if (trackedObject.awoken)
                            {
                                if (ThreadManager.host)
                                {
                                    // This will also send a packet with the object to be added in the client's global item list
                                    Server.AddTrackedObject(trackedObject.data, 0);
                                }
                                else
                                {
                                    // Tell the server we need to add this item to global tracked objects
                                    trackedObject.data.localWaitingIndex = Client.localObjectCounter++;
                                    Client.waitingLocalObjects.Add(trackedObject.data.localWaitingIndex, trackedObject.data);
                                    ClientSend.TrackedObject(trackedObject.data);
                                }

                                trackedObject.data.OnTracked();
                            }
                            else
                            {
                                trackedObject.sendOnAwake = true;
                            }

                            foreach (Transform child in root)
                            {
                                SyncTrackedObjects(child, controlEverything, trackedObject.data, scene);
                            }
                        }
                    }
                    else // Item will not be controlled by us but is an item that should be tracked by system, so destroy it
                    {
                        Destroy(root.gameObject);
                    }
                }
                else
                {
                    // It is already tracked, this is possible of we received new object from server before we sync
                    return;
                }
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedObjects(child, controlEverything, parent, scene);
                }
            }
        }

        private static TrackedObject MakeObjectTracked(Transform root, TrackedObjectData parent, Type trackedObjectType)
        {
            return (TrackedObject)trackedObjectType.InvokeMember("MakeTracked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, null, new object[] { root, parent });
        }

        private static TrackedItem MakeItemTracked(FVRPhysicalObject physObj, TrackedItemData parent)
        {
            TrackedItem trackedItem = physObj.gameObject.AddComponent<TrackedItem>();
            TrackedItemData data = new TrackedItemData();
            trackedItem.data = data;
            data.physical = trackedItem;
            data.physical.physical = physObj;

            GameManager.trackedItemByItem.Add(physObj, trackedItem);
            if(physObj is SosigWeaponPlayerInterface)
            {
                GameManager.trackedItemBySosigWeapon.Add((physObj as SosigWeaponPlayerInterface).W, trackedItem);
            }

            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<TrackedItemData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);
            }
            SetItemIdentifyingInfo(physObj, data);
            data.position = trackedItem.transform.position;
            data.rotation = trackedItem.transform.rotation;
            data.active = trackedItem.gameObject.activeInHierarchy;
            data.underActiveControl = IsControlled(trackedItem.physicalObject);

            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = instance;
            data.controller = ID;
            data.initTracker = ID;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;

            CollectExternalData(data);

            // Add to local list
            data.localTrackedID = items.Count;
            items.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedItem.updateFunc != null)
            {
                trackedItem.updateFunc();
            }

            return trackedItem;
        }

        // MOD: This will be called upon tracking a new item
        //      From here you will be able to set specific data on the item
        //      For more information and example, take a look at CollectExternalData(TrackedSosigData)
        private static void CollectExternalData(TrackedItemData trackedItemData)
        {
            TNH_ShatterableCrate crate = trackedItemData.physical.GetComponent<TNH_ShatterableCrate>();
            if (crate != null)
            {
                trackedItemData.additionalData = new byte[5];

                trackedItemData.additionalData[0] = TNH_SupplyPointPatch.inSpawnBoxes ? (byte)1 : (byte)0;
                if (TNH_SupplyPointPatch.inSpawnBoxes)
                {
                    BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(trackedItemData.additionalData, 1);
                }

                trackedItemData.identifyingData[3] = crate.m_isHoldingHealth ? (byte)1 : (byte)0;
                trackedItemData.identifyingData[4] = crate.m_isHoldingToken ? (byte)1 : (byte)0;
            }
            else if(trackedItemData.physicalItem.physicalObject is GrappleThrowable)
            {
                GrappleThrowable asGrappleThrowable = (GrappleThrowable)trackedItemData.physicalItem.physicalObject;
                trackedItemData.additionalData = new byte[asGrappleThrowable.finalRopePoints.Count * 12 + 2];

                trackedItemData.additionalData[0] = asGrappleThrowable.m_hasLanded ? (byte)1: (byte)0;
                trackedItemData.additionalData[1] = (byte)asGrappleThrowable.finalRopePoints.Count;
                if (asGrappleThrowable.finalRopePoints.Count > 0)
                {
                    for(int i = 0; i < asGrappleThrowable.finalRopePoints.Count; ++i)
                    {
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].x).CopyTo(trackedItemData.additionalData, i * 12 + 2);
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].y).CopyTo(trackedItemData.additionalData, i * 12 + 6);
                        BitConverter.GetBytes(asGrappleThrowable.finalRopePoints[i].z).CopyTo(trackedItemData.additionalData, i * 12 + 10);
                    }
                }
            }
        }

        // MOD: If you have a type of item (FVRPhysicalObject) that doen't have an ObjectWrapper,
        //      you can set custom identifying info here as we currently do for TNH_ShatterableCrate
        public static void SetItemIdentifyingInfo(FVRPhysicalObject physObj, TrackedItemData trackedItemData)
        {
            if (physObj.ObjectWrapper != null)
            {
                trackedItemData.itemID = physObj.ObjectWrapper.ItemID;
                return;
            }
            if(physObj.IDSpawnedFrom != null)
            {
                if (IM.OD.ContainsKey(physObj.IDSpawnedFrom.name))
                {
                    trackedItemData.itemID = physObj.IDSpawnedFrom.name;
                }
                else if (IM.OD.ContainsKey(physObj.IDSpawnedFrom.ItemID))
                {
                    trackedItemData.itemID = physObj.IDSpawnedFrom.ItemID;
                }
                return;
            }
            TNH_ShatterableCrate crate = physObj.GetComponent<TNH_ShatterableCrate>();
            if(crate != null)
            {
                trackedItemData.itemID = "TNH_ShatterableCrate";
                trackedItemData.identifyingData = new byte[1];
                if (crate.name[9] == 'S') // Small
                {
                    trackedItemData.identifyingData[0] = 2;
                }
                else if (crate.name[9] == 'M') // Medium
                {
                    trackedItemData.identifyingData[0] = 1;
                }
                else // Large
                {
                    trackedItemData.identifyingData[0] = 0;
                }

                return;
            }
        }

        // MOD: When the server receives an item to track, it will first check if it can identify the item on its side
        //      If your mod added something in IsObjectIdentifiable() then you should also support it in here
        public static bool IsItemIdentifiable(TrackedItemData itemData)
        {
            return IM.OD.ContainsKey(itemData.itemID) || itemData.itemID.Equals("TNH_ShatterableCrate");
        }

        public static void SyncTrackedSosigs(bool init = false, bool inControl = false)
        {
            // When we sync our current scene, if we are alone, we sync and take control of all sosigs
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedSosigs(root.transform, init ? inControl : controlOverride, GameManager.scene);
            }
        }

        public static void SyncTrackedSosigs(Transform root, bool controlEverything, string scene)
        {
            Sosig sosigScript = root.GetComponent<Sosig>();
            if (sosigScript != null)
            {
                TrackedSosig trackedSosig = root.GetComponent<TrackedSosig>();
                if (trackedSosig == null)
                {
                    if (controlEverything)
                    {
                        trackedSosig = MakeSosigTracked(sosigScript);
                        if (trackedSosig.awoken) 
                        { 
                            if (ThreadManager.host)
                            {
                                // This will also send a packet with the sosig to be added in the client's global sosig list
                                Server.AddTrackedSosig(trackedSosig.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this item to global tracked items
                                trackedSosig.data.localWaitingIndex = Client.localSosigCounter++;
                                Client.waitingLocalSosigs.Add(trackedSosig.data.localWaitingIndex, trackedSosig.data);
                                ClientSend.TrackedSosig(trackedSosig.data);
                            }
                        }
                        else
                        {
                            trackedSosig.sendOnAwake = true;
                        }
                    }
                    else // Item will not be controlled by us but is an item that should be tracked by system, so destroy it
                    {
                        Destroy(root.gameObject);
                    }
                }
                else
                {
                    // It already has tracked item on it, this is possible of we received new sosig from server before we sync
                    return;
                }
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedSosigs(child, controlEverything, scene);
                }
            }
        }

        private static TrackedSosig MakeSosigTracked(Sosig sosigScript)
        {
            TrackedSosig trackedSosig = sosigScript.gameObject.AddComponent<TrackedSosig>();
            TrackedSosigData data = new TrackedSosigData();
            trackedSosig.data = data;
            data.physicalObject = trackedSosig;
            trackedSosig.physicalSosigScript = sosigScript;
            GameManager.trackedSosigBySosig.Add(sosigScript, trackedSosig);

            data.configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
            data.configTemplate.AppliesDamageResistToIntegrityLoss = sosigScript.AppliesDamageResistToIntegrityLoss;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.TotalMustard = sosigScript.m_maxMustard;
            data.configTemplate.BleedDamageMult = sosigScript.BleedDamageMult;
            data.configTemplate.BleedRateMultiplier = sosigScript.BleedRateMult;
            data.configTemplate.BleedVFXIntensity = sosigScript.BleedVFXIntensity;
            data.configTemplate.SearchExtentsModifier = sosigScript.SearchExtentsModifier;
            data.configTemplate.ShudderThreshold = sosigScript.ShudderThreshold;
            data.configTemplate.ConfusionThreshold = sosigScript.ConfusionThreshold;
            data.configTemplate.ConfusionMultiplier = sosigScript.ConfusionMultiplier;
            data.configTemplate.ConfusionTimeMax = sosigScript.m_maxConfusedTime;
            data.configTemplate.StunThreshold = sosigScript.StunThreshold;
            data.configTemplate.StunMultiplier = sosigScript.StunMultiplier;
            data.configTemplate.StunTimeMax = sosigScript.m_maxStunTime;
            data.configTemplate.HasABrain = sosigScript.HasABrain;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.RegistersPassiveThreats = sosigScript.RegistersPassiveThreats;
            data.configTemplate.CanBeKnockedOut = sosigScript.CanBeKnockedOut;
            data.configTemplate.MaxUnconsciousTime = sosigScript.m_maxUnconsciousTime;
            data.configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = sosigScript.m_assaultPointOverridesSkirmishPointWhenFurtherThan; 
            data.configTemplate.ViewDistance = sosigScript.MaxSightRange;
            data.configTemplate.HearingDistance = sosigScript.MaxHearingRange;
            data.configTemplate.MaxFOV = sosigScript.MaxFOV;
            data.configTemplate.StateSightRangeMults = sosigScript.StateSightRangeMults;
            data.configTemplate.StateHearingRangeMults = sosigScript.StateHearingRangeMults;
            data.configTemplate.StateFOVMults = sosigScript.StateFOVMults;
            data.configTemplate.CanPickup_Ranged = sosigScript.CanPickup_Ranged;
            data.configTemplate.CanPickup_Melee = sosigScript.CanPickup_Melee;
            data.configTemplate.CanPickup_Other = sosigScript.CanPickup_Other;
            data.configTemplate.DoesJointBreakKill_Head = sosigScript.m_doesJointBreakKill_Head; 
            data.configTemplate.DoesJointBreakKill_Upper = sosigScript.m_doesJointBreakKill_Upper;
            data.configTemplate.DoesJointBreakKill_Lower = sosigScript.m_doesJointBreakKill_Lower;
            data.configTemplate.DoesSeverKill_Head = sosigScript.m_doesSeverKill_Head;
            data.configTemplate.DoesSeverKill_Upper = sosigScript.m_doesSeverKill_Upper;
            data.configTemplate.DoesSeverKill_Lower = sosigScript.m_doesSeverKill_Lower;
            data.configTemplate.DoesExplodeKill_Head = sosigScript.m_doesExplodeKill_Head;
            data.configTemplate.DoesExplodeKill_Upper = sosigScript.m_doesExplodeKill_Upper;
            data.configTemplate.DoesExplodeKill_Lower = sosigScript.m_doesExplodeKill_Lower;
            data.configTemplate.CrawlSpeed = sosigScript.Speed_Crawl;
            data.configTemplate.SneakSpeed = sosigScript.Speed_Sneak;
            data.configTemplate.WalkSpeed = sosigScript.Speed_Walk;
            data.configTemplate.RunSpeed = sosigScript.Speed_Run;
            data.configTemplate.TurnSpeed = sosigScript.Speed_Turning;
            data.configTemplate.MovementRotMagnitude = sosigScript.MovementRotMagnitude;
            data.configTemplate.DamMult_Projectile = sosigScript.DamMult_Projectile;
            data.configTemplate.DamMult_Explosive = sosigScript.DamMult_Explosive;
            data.configTemplate.DamMult_Melee = sosigScript.DamMult_Melee;
            data.configTemplate.DamMult_Piercing = sosigScript.DamMult_Piercing;
            data.configTemplate.DamMult_Blunt = sosigScript.DamMult_Blunt;
            data.configTemplate.DamMult_Cutting = sosigScript.DamMult_Cutting;
            data.configTemplate.DamMult_Thermal = sosigScript.DamMult_Thermal;
            data.configTemplate.DamMult_Chilling = sosigScript.DamMult_Chilling;
            data.configTemplate.DamMult_EMP = sosigScript.DamMult_EMP;
            data.configTemplate.CanBeSurpressed = sosigScript.CanBeSuppresed;
            data.configTemplate.SuppressionMult = sosigScript.SuppressionMult;
            data.configTemplate.CanBeGrabbed = sosigScript.CanBeGrabbed;
            data.configTemplate.CanBeSevered = sosigScript.CanBeSevered;
            data.configTemplate.CanBeStabbed = sosigScript.CanBeStabbed;
            data.configTemplate.MaxJointLimit = sosigScript.m_maxJointLimit;
            data.configTemplate.OverrideSpeech = sosigScript.Speech;
            data.configTemplate.LinkDamageMultipliers = new List<float>();
            data.configTemplate.LinkStaggerMultipliers = new List<float>();
            data.configTemplate.StartingLinkIntegrity = new List<Vector2>();
            data.configTemplate.StartingChanceBrokenJoint = new List<float>();
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.configTemplate.LinkDamageMultipliers.Add(sosigScript.Links[i].DamMult);
                data.configTemplate.LinkStaggerMultipliers.Add(sosigScript.Links[i].StaggerMagnitude);
                float actualLinkIntegrity = sosigScript.Links[i].m_integrity;
                data.configTemplate.StartingLinkIntegrity.Add(new Vector2(actualLinkIntegrity, actualLinkIntegrity));
                data.configTemplate.StartingChanceBrokenJoint.Add(sosigScript.Links[i].m_isJointBroken ? 1 : 0);
            }
            if (sosigScript.Priority != null)
            {
                data.configTemplate.TargetCapacity = sosigScript.Priority.m_eventCapacity;
                data.configTemplate.TargetTrackingTime = sosigScript.Priority.m_maxTrackingTime;
                data.configTemplate.NoFreshTargetTime = sosigScript.Priority.m_timeToNoFreshTarget;
            }
            data.position = sosigScript.CoreRB.position;
            data.velocity = sosigScript.CoreRB.velocity;
            data.rotation = sosigScript.CoreRB.rotation;
            data.active = trackedSosig.gameObject.activeInHierarchy;
            data.linkData = new float[sosigScript.Links.Count][];
            data.linkIntegrity = new float[data.linkData.Length];
            for(int i=0; i < sosigScript.Links.Count; ++i)
            {
                data.linkData[i] = new float[5];
                data.linkData[i][0] = sosigScript.Links[i].StaggerMagnitude;
                data.linkData[i][1] = sosigScript.Links[i].DamMult;
                data.linkData[i][2] = sosigScript.Links[i].DamMultAVG;
                data.linkData[i][3] = sosigScript.Links[i].CollisionBluntDamageMultiplier;
                if(sosigScript.Links[i] == null)
                {
                    data.linkData[i][4] = 0;
                    data.linkIntegrity[i] = 0;
                }
                else
                {
                    data.linkData[i][4] = sosigScript.Links[i].m_integrity;
                    data.linkIntegrity[i] = data.linkData[i][4];
                }
            }

            data.wearables = new List<List<string>>();
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.wearables.Add(new List<string>());
                for (int j = 0; j < sosigScript.Links[i].m_wearables.Count; ++j)
                {
                    data.wearables[i].Add(sosigScript.Links[i].m_wearables[j].name);
                    if (data.wearables[i][j].EndsWith("(Clone)"))
                    {
                        data.wearables[i][j] = data.wearables[i][j].Substring(0, data.wearables[i][j].Length - 7);
                    }
                    if (Mod.sosigWearableMap.ContainsKey(data.wearables[i][j]))
                    {
                        data.wearables[i][j] = Mod.sosigWearableMap[data.wearables[i][j]];
                    }
                    else
                    {
                        Mod.LogError("SosigWearable: " + data.wearables[i][j] + " not found in map");
                    }
                }
            }
            data.ammoStores = sosigScript.Inventory.m_ammoStores;
            data.inventory = new int[2 + sosigScript.Inventory.Slots.Count];
            if(sosigScript.Hand_Primary.HeldObject == null)
            {
                data.inventory[0] = -1;
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Hand_Primary.HeldObject, out trackedItem) ? trackedItem : sosigScript.Hand_Primary.HeldObject.O.GetComponent<TrackedItem>();
                if(trackedItem == null)
                {
                    TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Hand_Primary.HeldObject, new KeyValuePair<TrackedSosigData, int>(data, 0));
                    data.inventory[0] = -1;
                }
                else
                {
                    if(trackedItem.data.trackedID == -1)
                    {
                        TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, 0));
                        data.inventory[0] = -1;
                    }
                    else
                    {
                        data.inventory[0] = trackedItem.data.trackedID;
                    }
                }
            }
            if(sosigScript.Hand_Secondary.HeldObject == null)
            {
                data.inventory[1] = -1;
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Hand_Secondary.HeldObject, out trackedItem) ? trackedItem : sosigScript.Hand_Secondary.HeldObject.O.GetComponent<TrackedItem>();
                if(trackedItem == null)
                {
                    TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Hand_Secondary.HeldObject, new KeyValuePair<TrackedSosigData, int>(data, 1));
                    data.inventory[1] = -1;
                }
                else
                {
                    if(trackedItem.data.trackedID == -1)
                    {
                        TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, 1));
                        data.inventory[1] = -1;
                    }
                    else
                    {
                        data.inventory[1] = trackedItem.data.trackedID;
                    }
                }
            }
            for(int i=0; i < sosigScript.Inventory.Slots.Count; ++i)
            {
                if (sosigScript.Inventory.Slots[i].HeldObject == null)
                {
                    data.inventory[i + 2] = -1;
                }
                else
                {
                    TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(sosigScript.Inventory.Slots[i].HeldObject, out trackedItem) ? trackedItem : sosigScript.Inventory.Slots[i].HeldObject.O.GetComponent<TrackedItem>();
                    if (trackedItem == null)
                    {
                        TrackedItem.unknownSosigInventoryObjects.Add(sosigScript.Inventory.Slots[i].HeldObject, new KeyValuePair<TrackedSosigData, int>(data, i + 2));
                        data.inventory[i + 2] = -1;
                    }
                    else
                    {
                        if (trackedItem.data.trackedID == -1)
                        {
                            TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(data, i + 2));
                            data.inventory[i + 2] = -1;
                        }
                        else
                        {
                            data.inventory[i + 2] = trackedItem.data.trackedID;
                        }
                    }
                }
            }
            data.controller = ID;
            data.initTracker = ID;
            data.mustard = sosigScript.Mustard;
            data.bodyPose = sosigScript.BodyPose;
            data.currentOrder = sosigScript.CurrentOrder;
            data.fallbackOrder = sosigScript.FallbackOrder;
            data.IFF = (byte)sosigScript.GetIFF();
            data.IFFChart = sosigScript.Priority.IFFChart;
            data.scene = sceneLoading ? LoadLevelBeginPatch.loadingLevel : scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;

            // Brain
            // GuardPoint
            data.guardPoint = sosigScript.GetGuardPoint();
            data.guardDir = sosigScript.GetGuardDir();
            data.hardGuard = sosigScript.m_hardGuard;
            // Skirmish
            data.skirmishPoint = sosigScript.m_skirmishPoint;
            data.pathToPoint = sosigScript.m_pathToPoint;
            data.assaultPoint = sosigScript.GetAssaultPoint();
            data.faceTowards = sosigScript.m_faceTowards;
            // SearchForEquipment
            data.wanderPoint = sosigScript.m_wanderPoint;
            // Assault
            data.assaultSpeed = sosigScript.m_assaultSpeed;
            // Idle
            data.idleToPoint = sosigScript.m_idlePoint;
            data.idleDominantDir = sosigScript.m_idleDominantDir;
            // PathTo
            data.pathToLookDir = sosigScript.m_pathToLookDir;

            CollectExternalData(data);

            // Add to local list
            data.localTrackedID = sosigs.Count;
            sosigs.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedSosig.awoken)
            {
                trackedSosig.data.Update(true);
            }

            Mod.LogInfo("Made sosig " + trackedSosig.name + " tracked", false);

            return trackedSosig;
        }

        // MOD: This will be called upon tracking a new sosig
        //      From here you will be able to set specific data on the sosig
        //      For example, when we spawn sosigs in TNH we set flags so we know if they were in a patrol/holdpoint/supplypoint so we can set 
        //      this in the trackedSosigData
        //      Considering multiple mods may be writing data, a mod should probably add an int as an identifier for the data, which will
        //      be used to find the mod specific data in the data array
        private static void CollectExternalData(TrackedSosigData trackedSosigData)
        {
            trackedSosigData.data = new byte[9 + (12 * ((TNH_ManagerPatch.inGenerateSentryPatrol || TNH_ManagerPatch.inGeneratePatrol) ? (TNH_ManagerPatch.patrolPoints == null ? 0 : TNH_ManagerPatch.patrolPoints.Count):0))];

            // Write TNH context
            trackedSosigData.data[0] = TNH_HoldPointPatch.inSpawnEnemyGroup ? (byte)1 : (byte)0;
            //trackedSosigData.data[1] = TNH_HoldPointPatch.inSpawnTurrets ? (byte)1 : (byte)0;
            trackedSosigData.data[1] = TNH_SupplyPointPatch.inSpawnTakeEnemyGroup ? (byte)1 : (byte)0;
            BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(trackedSosigData.data, 2);
            //trackedSosigData.data[3] = TNH_SupplyPointPatch.inSpawnDefenses ? (byte)1 : (byte)0;
            trackedSosigData.data[4] = TNH_ManagerPatch.inGenerateSentryPatrol ? (byte)1 : (byte)0;
            trackedSosigData.data[5] = TNH_ManagerPatch.inGeneratePatrol ? (byte)1 : (byte)0;
            if (TNH_ManagerPatch.inGenerateSentryPatrol || TNH_ManagerPatch.inGeneratePatrol)
            {
                BitConverter.GetBytes((short)TNH_ManagerPatch.patrolIndex).CopyTo(trackedSosigData.data, 6);
                if (TNH_ManagerPatch.patrolPoints == null || TNH_ManagerPatch.patrolPoints.Count == 0)
                {
                    trackedSosigData.data[8] = (byte)0;
                }
                else
                {
                    trackedSosigData.data[8] = (byte)TNH_ManagerPatch.patrolPoints.Count;
                    for (int i = 0; i < TNH_ManagerPatch.patrolPoints.Count; ++i)
                    {
                        int index = i * 12 + 9;
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].x).CopyTo(trackedSosigData.data, index);
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].y).CopyTo(trackedSosigData.data, index + 4);
                        BitConverter.GetBytes(TNH_ManagerPatch.patrolPoints[i].z).CopyTo(trackedSosigData.data, index + 8);
                    }
                }
            }
        }

        public static void SyncTrackedAutoMeaters(bool init = false, bool inControl = false)
        {
            // When we sync our current scene, if we are alone, we sync and take control of all AutoMeaters
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedAutoMeaters(root.transform, init ? inControl : controlOverride, GameManager.scene);
            }
        }

        public static void SyncTrackedAutoMeaters(Transform root, bool controlEverything, string scene)
        {
            AutoMeater autoMeaterScript = root.GetComponent<AutoMeater>();
            if (autoMeaterScript != null)
            {
                TrackedAutoMeater trackedAutoMeater = root.GetComponent<TrackedAutoMeater>();
                if (trackedAutoMeater == null)
                {
                    if (controlEverything)
                    {
                        trackedAutoMeater = MakeAutoMeaterTracked(autoMeaterScript);
                        if (trackedAutoMeater.awoken)
                        {
                            if (ThreadManager.host)
                            {
                                // This will also send a packet with the AutoMeater to be added in the client's global AutoMeater list
                                Server.AddTrackedAutoMeater(trackedAutoMeater.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this AutoMeater to global tracked AutoMeaters
                                trackedAutoMeater.data.localWaitingIndex = Client.localAutoMeaterCounter++;
                                Client.waitingLocalAutoMeaters.Add(trackedAutoMeater.data.localWaitingIndex, trackedAutoMeater.data);
                                ClientSend.TrackedAutoMeater(trackedAutoMeater.data);
                            }
                        }
                        else
                        {
                            trackedAutoMeater.sendOnAwake = true;
                        }
                    }
                    else // AutoMeater will not be controlled by us but is an AutoMeater that should be tracked by system, so destroy it
                    {
                        Destroy(root.gameObject);
                    }
                }
                else
                {
                    // It already has tracked AutoMeater on it, this is possible of we received new AutoMeater from server before we sync
                    return;
                }
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedAutoMeaters(child, controlEverything, scene);
                }
            }
        }

        private static TrackedAutoMeater MakeAutoMeaterTracked(AutoMeater autoMeaterScript)
        {
            TrackedAutoMeater trackedAutoMeater = autoMeaterScript.gameObject.AddComponent<TrackedAutoMeater>();
            TrackedAutoMeaterData data = new TrackedAutoMeaterData();
            trackedAutoMeater.data = data;
            data.physicalObject = trackedAutoMeater;
            trackedAutoMeater.physicalAutoMeaterScript = autoMeaterScript;
            GameManager.trackedAutoMeaterByAutoMeater.Add(autoMeaterScript, trackedAutoMeater);

            data.position = autoMeaterScript.RB.position;
            data.rotation = autoMeaterScript.RB.rotation;
            data.active = trackedAutoMeater.gameObject.activeInHierarchy;
            data.IFF = (byte)autoMeaterScript.E.IFFCode;
            if (autoMeaterScript.name.Contains("SMG"))
            {
                data.ID = 0;
            }
            else if (autoMeaterScript.name.Contains("Flak"))
            {
                data.ID = 1;
            }
            else if (autoMeaterScript.name.Contains("Flamethrower"))
            {
                data.ID = 2;
            }
            else if (autoMeaterScript.name.Contains("Machinegun") || autoMeaterScript.name.Contains("MachineGun"))
            {
                data.ID = 3;
            }
            else if (autoMeaterScript.name.Contains("Suppresion") || autoMeaterScript.name.Contains("Suppression"))
            {
                data.ID = 4;
            }
            else if (autoMeaterScript.name.Contains("Blue"))
            {
                data.ID = 5;
            }
            else if (autoMeaterScript.name.Contains("Red"))
            {
                data.ID = 6;
            }
            else
            {
                Mod.LogWarning("Unsupported AutoMeater type tracked");
                data.ID = 7;
            }
            data.sideToSideRotation = autoMeaterScript.SideToSideTransform.localRotation;
            data.hingeTargetPos = autoMeaterScript.SideToSideHinge.spring.targetPosition;
            data.upDownMotorRotation = autoMeaterScript.UpDownTransform.localRotation;
            data.upDownJointTargetPos = autoMeaterScript.UpDownHinge.spring.targetPosition;

            // Get hitzones
            AutoMeaterHitZone[] hitZoneArr = trackedAutoMeater.GetComponentsInChildren<AutoMeaterHitZone>();
            foreach (AutoMeaterHitZone hitZone in hitZoneArr)
            {
                data.hitZones.Add(hitZone.Type, hitZone);
            }

            CollectExternalData(data);

            // Add to local list
            data.localTrackedID = autoMeaters.Count;
            data.controller = ID;
            data.initTracker = ID;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;
            autoMeaters.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedAutoMeater.awoken)
            {
                trackedAutoMeater.data.Update(true);
            }

            return trackedAutoMeater;
        }

        // MOD: This will be called upon tracking a new autoMeater
        //      From here you will be able to set specific data on the autoMeater
        //      For more info and example, take a look at CollectExternalData(TrackedSosigData)
        private static void CollectExternalData(TrackedAutoMeaterData trackedAutoMeaterData)
        {
            trackedAutoMeaterData.data = new byte[4];

            // Write TNH context
            trackedAutoMeaterData.data[0] = TNH_HoldPointPatch.inSpawnTurrets ? (byte)1 : (byte)1;
            trackedAutoMeaterData.data[1] = TNH_SupplyPointPatch.inSpawnDefenses ? (byte)1 : (byte)1;
            BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(trackedAutoMeaterData.data, 2);
        }

        public static void SyncTrackedEncryptions(bool init = false, bool inControl = false)
        {
            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other encryptions get destroyed. We will receive any encryption that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedEncryptions(root.transform, init ? inControl : controlOverride, GameManager.scene);
            }
        }

        public static void SyncTrackedEncryptions(Transform root, bool controlEverything, string scene)
        {
            TNH_EncryptionTarget encryption = root.GetComponent<TNH_EncryptionTarget>();
            if (encryption != null)
            {
                TrackedEncryption currentTrackedEncryption = root.GetComponent<TrackedEncryption>();
                if (currentTrackedEncryption == null)
                {
                    if (controlEverything)
                    {
                        TrackedEncryption trackedEncryption = MakeEncryptionTracked(encryption);
                        if (trackedEncryption.awoken)
                        {
                            if (ThreadManager.host)
                            {
                                // This will also send a packet with the Encryption to be added in the client's global item list
                                Server.AddTrackedEncryption(trackedEncryption.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this Encryption to global tracked Encryptions
                                trackedEncryption.data.localWaitingIndex = Client.localEncryptionCounter++;
                                Client.waitingLocalEncryptions.Add(trackedEncryption.data.localWaitingIndex, trackedEncryption.data);
                                ClientSend.TrackedEncryption(trackedEncryption.data);
                            }
                        }
                        else
                        {
                            trackedEncryption.sendOnAwake = true;
                        }
                    }
                    else // Item will not be controlled by us but is an Encryption that should be tracked by system, so destroy it
                    {
                        Destroy(root.gameObject);
                    }
                }
                else
                {
                    // It already has tracked item on it, this is possible of we received new item from server before we sync
                    return;
                }
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedEncryptions(child, controlEverything, scene);
                }
            }
        }

        private static TrackedEncryption MakeEncryptionTracked(TNH_EncryptionTarget encryption)
        {
            TrackedEncryption trackedEncryption = encryption.gameObject.AddComponent<TrackedEncryption>();
            TrackedEncryptionData data = new TrackedEncryptionData();
            trackedEncryption.data = data;
            data.physicalObject = trackedEncryption;
            data.physicalObject.physicalEncryptionScript = encryption;

            data.type = encryption.Type;
            data.position = trackedEncryption.transform.position;
            data.rotation = trackedEncryption.transform.rotation;
            data.active = trackedEncryption.gameObject.activeInHierarchy;

            data.tendrilsActive = new bool[data.physicalObject.physicalEncryptionScript.Tendrils.Count];
            data.growthPoints = new Vector3[data.physicalObject.physicalEncryptionScript.GrowthPoints.Count];
            data.subTargsPos = new Vector3[data.physicalObject.physicalEncryptionScript.SubTargs.Count];
            data.subTargsActive = new bool[data.physicalObject.physicalEncryptionScript.SubTargs.Count];
            data.tendrilFloats = new float[data.physicalObject.physicalEncryptionScript.TendrilFloats.Count];
            data.tendrilsRot = new Quaternion[data.physicalObject.physicalEncryptionScript.Tendrils.Count];
            data.tendrilsScale = new Vector3[data.physicalObject.physicalEncryptionScript.Tendrils.Count];
            if (data.physicalObject.physicalEncryptionScript.UsesRegenerativeSubTarg)
            {
                for (int i = 0; i < data.physicalObject.physicalEncryptionScript.Tendrils.Count; ++i)
                {
                    if (data.physicalObject.physicalEncryptionScript.Tendrils[i].activeSelf)
                    {
                        data.tendrilsActive[i] = true;
                        data.growthPoints[i] = data.physicalObject.physicalEncryptionScript.GrowthPoints[i];
                        data.subTargsPos[i] = data.physicalObject.physicalEncryptionScript.SubTargs[i].transform.position;
                        data.subTargsActive[i] = data.physicalObject.physicalEncryptionScript.SubTargs[i];
                        data.tendrilFloats[i] = data.physicalObject.physicalEncryptionScript.TendrilFloats[i];
                        data.tendrilsRot[i] = data.physicalObject.physicalEncryptionScript.Tendrils[i].transform.rotation;
                        data.tendrilsScale[i] = data.physicalObject.physicalEncryptionScript.Tendrils[i].transform.localScale;
                    }
                }
            }
            else if (data.physicalObject.physicalEncryptionScript.UsesRecursiveSubTarg)
            {
                for (int i = 0; i < data.physicalObject.physicalEncryptionScript.SubTargs.Count; ++i)
                {
                    if (data.physicalObject.physicalEncryptionScript.SubTargs[i] != null && data.physicalObject.physicalEncryptionScript.SubTargs[i].activeSelf)
                    {
                        data.subTargsActive[i] = data.physicalObject.physicalEncryptionScript.SubTargs[i].activeSelf;
                    }
                }
            }

            // Add to local list
            data.localTrackedID = encryptions.Count;
            data.controller = ID;
            data.initTracker = ID;
            data.scene = GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;
            encryptions.Add(data);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedEncryption.awoken)
            {
                trackedEncryption.data.Update(true);
            }

            return trackedEncryption;
        }

        public static TNHInstance AddNewTNHInstance(int hostID, bool letPeopleJoin,
                                                         int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                                         int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                                         int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, string levelID)
        {
            if (ThreadManager.host)
            {
                int freeInstance = 1; // Start at 1 because 0 is the default instance
                while (activeInstances.ContainsKey(freeInstance))
                {
                    ++freeInstance;
                }
                TNHInstance newInstance = new TNHInstance(freeInstance, hostID, letPeopleJoin,
                                                                    progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                                                    targetModeSetting, AIDifficultyModifier, radarModeModifier,
                                                                    itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelID);
                TNHInstances.Add(freeInstance, newInstance);

                if ((newInstance.letPeopleJoin || newInstance.currentlyPlaying.Count == 0) && Mod.TNHInstanceList != null && Mod.joinTNHInstances != null && !Mod.joinTNHInstances.ContainsKey(freeInstance))
                {
                    GameObject newInstanceElement = GameObject.Instantiate<GameObject>(Mod.TNHInstancePrefab, Mod.TNHInstanceList.transform);
                    newInstanceElement.transform.GetChild(0).GetComponent<Text>().text = "Instance " + freeInstance;
                    newInstanceElement.SetActive(true);

                    FVRPointableButton instanceButton = newInstanceElement.AddComponent<FVRPointableButton>();
                    instanceButton.SetButton();
                    instanceButton.MaxPointingRange = 5;
                    instanceButton.Button.onClick.AddListener(() => { Mod.modInstance.OnTNHInstanceClicked(freeInstance); });

                    Mod.joinTNHInstances.Add(freeInstance, newInstanceElement);
                }

                activeInstances.Add(freeInstance, 0);

                Mod.modInstance.OnTNHInstanceReceived(newInstance);

                return newInstance;
            }
            else
            {
                ClientSend.AddTNHInstance(hostID, letPeopleJoin,
                                               progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                               targetModeSetting, AIDifficultyModifier, radarModeModifier,
                                               itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelID);

                return null;
            }
        }

        public static int AddNewInstance()
        {
            if (ThreadManager.host)
            {
                int freeInstance = 1; // Start at 1 because 0 is the default instance
                while (activeInstances.ContainsKey(freeInstance))
                {
                    ++freeInstance;
                }

                activeInstances.Add(freeInstance, 0);

                Mod.modInstance.OnInstanceReceived(freeInstance);

                ServerSend.AddInstance(freeInstance);

                return freeInstance;
            }
            else
            {
                ClientSend.AddInstance();

                return -1;
            }
        }

        public static void AddTNHInstance(TNHInstance instance)
        {
            if (!activeInstances.ContainsKey(instance.instance))
            {
                activeInstances.Add(instance.instance, instance.playerIDs.Count);
            }
            TNHInstances.Add(instance.instance, instance);

            if ((instance.letPeopleJoin || instance.currentlyPlaying.Count == 0) && Mod.TNHInstanceList != null && Mod.joinTNHInstances != null && !Mod.joinTNHInstances.ContainsKey(instance.instance))
            {
                GameObject newInstanceElement = GameObject.Instantiate<GameObject>(Mod.TNHInstancePrefab, Mod.TNHInstanceList.transform);
                newInstanceElement.transform.GetChild(0).GetComponent<Text>().text = "Instance " + instance.instance;
                newInstanceElement.SetActive(true);

                FVRPointableButton instanceButton = newInstanceElement.AddComponent<FVRPointableButton>();
                instanceButton.SetButton();
                instanceButton.MaxPointingRange = 5;
                instanceButton.Button.onClick.AddListener(() => { Mod.modInstance.OnTNHInstanceClicked(instance.instance); });

                Mod.joinTNHInstances.Add(instance.instance, newInstanceElement);
            }

            // Only want to set dontAddToInstance if we want to join the TNH instance upon receiving it
            dontAddToInstance = Mod.setLatestInstance;
            Mod.modInstance.OnTNHInstanceReceived(instance);
        }

        public static void AddInstance(int instance)
        {
            // The instance could have already been created in UpdatePlayerInstance
            if (!activeInstances.ContainsKey(instance))
            {
                activeInstances.Add(instance, 0);
            }

            Mod.modInstance.OnInstanceReceived(instance);
        }

        public static void SetInstance(int instance)
        {
            Mod.LogInfo("Changing instance from " + GameManager.instance + " to " + instance, false);
            // Remove ourselves from the previous instance and manage dicts accordingly
            if (activeInstances.ContainsKey(GameManager.instance))
            {
                --activeInstances[GameManager.instance];
                if (activeInstances[GameManager.instance] == 0)
                {
                    activeInstances.Remove(GameManager.instance);
                }
            }
            else
            {
                Mod.LogError("Instance we are leaving is missing from active instances!");
            }
            if (TNHInstances.TryGetValue(GameManager.instance, out TNHInstance currentInstance))
            {
                currentInstance.playerIDs.Remove(ID);

                if (currentInstance.playerIDs.Count == 0)
                {
                    TNHInstances.Remove(GameManager.instance);

                    if (Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(instance))
                    {
                        GameObject.Destroy(Mod.joinTNHInstances[instance]);
                        Mod.joinTNHInstances.Remove(instance);
                    }

                    Mod.TNHSpectating = false;
                }

                // Remove from currently playing and dead if necessary
                currentInstance.currentlyPlaying.Remove(ID);
                currentInstance.dead.Remove(ID);
            }

            if (!sceneLoading)
            {
                if (playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances0))
                {
                    if (relevantInstances0.TryGetValue(GameManager.instance, out List<int> relevantPlayers))
                    {
                        playersAtLoadStart = relevantPlayers;
                    }
                    else
                    {
                        playersAtLoadStart = null;
                    }
                }
                else
                {
                    playersAtLoadStart = null;
                }
            }

            // Set locally
            GameManager.instance = instance;

            // Update instance dicts
            if (!activeInstances.ContainsKey(instance))
            {
                activeInstances.Add(instance, 0);
            }
            if (dontAddToInstance)
            {
                dontAddToInstance = false;
            }
            else
            {
                ++activeInstances[instance];
            }
            if (TNHInstances.ContainsKey(instance))
            {
                // PlayerIDs could already contain our ID if this instance was created by us
                if (!TNHInstances[instance].playerIDs.Contains(ID))
                {
                    TNHInstances[instance].playerIDs.Add(ID);
                }

                if (Mod.currentTNHUIManager != null)
                {
                    Mod.InitTNHUIManager(TNHInstances[instance]);
                }
                else
                {
                    Mod.currentTNHUIManager = GameObject.FindObjectOfType<TNH_UIManager>();
                    Mod.currentTNHSceneLoader = GameObject.FindObjectOfType<SceneLoader>();
                    if (Mod.currentTNHUIManager != null)
                    {
                        Mod.InitTNHUIManager(TNHInstances[instance]);
                    }
                }
            }

            // If we switch instance while loading a new scene, we will want to update control override
            // because when we started loading the scene, we didn't necessarily know in which instance we would end up
            bool bringItems = !GameManager.playersByInstanceByScene.TryGetValue(sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene, out Dictionary<int, List<int>> ci) ||
                              !ci.TryGetValue(instance, out List<int> op) || op.Count == 0;

            if (sceneLoading)
            {
                controlOverride = bringItems;
                firstPlayerInSceneInstance = bringItems;
            }
            else // Scene not currently loading, we don't want to manage items if we are loading into a new scene so only do it in this case
            {
                // Item we do not control: Destroy, giveControlOfDestroyed = true will ensure destruction does not get sent
                // Item we control: Destroy, giveControlOfDestroyed = true will ensure item's control is passed on if necessary
                // Item we are interacting with: Send a destruction order to other clients but don't destroy it on our side, since we want to move with these to new instance
                ++giveControlOfDestroyed;
                TrackedItemData[] itemArrToUse = null;
                TrackedSosigData[] sosigArrToUse = null;
                TrackedAutoMeaterData[] autoMeaterArrToUse = null;
                TrackedEncryptionData[] encryptionArrToUse = null;
                if (ThreadManager.host)
                {
                    itemArrToUse = Server.items;
                    sosigArrToUse = Server.sosigs;
                    autoMeaterArrToUse = Server.autoMeaters;
                    encryptionArrToUse = Server.encryptions;
                }
                else
                {
                    itemArrToUse = Client.items;
                    sosigArrToUse = Client.sosigs;
                    autoMeaterArrToUse = Client.autoMeaters;
                    encryptionArrToUse = Client.encryptions;
                }
                List<TrackedItemData> filteredItems = new List<TrackedItemData>();
                for (int i = itemArrToUse.Length - 1; i >= 0; --i)
                {
                    if (itemArrToUse[i] != null && itemArrToUse[i].physicalItem != null)
                    {
                        filteredItems.Add(itemArrToUse[i]);
                    }
                }
                for (int i = 0; i < filteredItems.Count; ++i)
                {
                    if (IsControlled(filteredItems[i].physicalItem.physicalObject))
                    {
                        // Send destruction without removing from global list
                        // We just don't want the other clients to have the item on their side anymore if they had it
                        if (ThreadManager.host)
                        {
                            ServerSend.DestroyItem(i, false);
                        }
                        else
                        {
                            ClientSend.DestroyItem(i, false);
                        }
                    }
                    else // Not being interacted with, just destroy on our side and give control
                    {
                        if (bringItems)
                        {
                            GameObject go = filteredItems[i].physicalItem.gameObject;
                            bool hadNoParent = filteredItems[i].physicalItem.data.parent == -1;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(filteredItems[i].physicalItem);

                            // Only sync the top parent of items. The children will also get retracked as children
                            if (hadNoParent)
                            {
                                SyncTrackedItems(go.transform, true, null, GameManager.scene);
                            }
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(filteredItems[i].physicalItem.gameObject);
                        }
                    }
                }

                List<TrackedSosigData> filteredSosigs = new List<TrackedSosigData>();
                for (int i = sosigArrToUse.Length - 1; i >= 0; --i)
                {
                    if (sosigArrToUse[i] != null && sosigArrToUse[i].physicalObject != null)
                    {
                        filteredSosigs.Add(sosigArrToUse[i]);
                    }
                }
                for (int i = 0; i < filteredSosigs.Count; ++i)
                {
                    if (IsControlled(filteredSosigs[i].physicalObject.physicalSosigScript))
                    {
                        // Send destruction without removing from global list
                        // We just don't want the other clients to have the sosig on their side anymore if they had it
                        if (ThreadManager.host)
                        {
                            ServerSend.DestroySosig(i, false);
                        }
                        else
                        {
                            ClientSend.DestroySosig(i, false);
                        }
                    }
                    else // Not being interacted with, just destroy on our side and give control
                    {
                        if (bringItems)
                        {
                            GameObject go = filteredSosigs[i].physicalObject.gameObject;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(filteredSosigs[i].physicalObject);

                            // Retrack sosig
                            SyncTrackedSosigs(go.transform, true, GameManager.scene);
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(filteredSosigs[i].physicalObject.gameObject);
                        }
                    }
                }

                List<TrackedAutoMeaterData> filteredAutoMeaters = new List<TrackedAutoMeaterData>();
                for (int i = autoMeaterArrToUse.Length - 1; i >= 0; --i)
                {
                    if (autoMeaterArrToUse[i] != null && autoMeaterArrToUse[i].physicalObject != null)
                    {
                        filteredAutoMeaters.Add(autoMeaterArrToUse[i]);
                    }
                }
                for (int i = 0; i < filteredAutoMeaters.Count; ++i)
                {
                    if (IsControlled(filteredAutoMeaters[i].physicalObject.physicalAutoMeaterScript))
                    {
                        // Send destruction without removing from global list
                        // We just don't want the other clients to have the sosig on their side anymore if they had it
                        if (ThreadManager.host)
                        {
                            ServerSend.DestroyAutoMeater(i, false);
                        }
                        else
                        {
                            ClientSend.DestroyAutoMeater(i, false);
                        }
                    }
                    else // Not being interacted with, just destroy on our side and give control
                    {
                        if (bringItems)
                        {
                            GameObject go = filteredAutoMeaters[i].physicalObject.gameObject;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(filteredAutoMeaters[i].physicalObject);

                            // Retrack sosig
                            SyncTrackedAutoMeaters(go.transform, true, GameManager.scene);
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(filteredAutoMeaters[i].physicalObject.gameObject);
                        }
                    }
                }

                List<TrackedEncryptionData> filteredEncryptions = new List<TrackedEncryptionData>();
                for (int i = encryptionArrToUse.Length - 1; i >= 0; --i)
                {
                    if (encryptionArrToUse[i] != null && encryptionArrToUse[i].physicalObject != null)
                    {
                        filteredEncryptions.Add(encryptionArrToUse[i]);
                    }
                }
                for (int i = 0; i < filteredEncryptions.Count; ++i)
                {
                    if (bringItems)
                    {
                        GameObject go = filteredEncryptions[i].physicalObject.gameObject;

                        // Destroy just the tracked script because we want to make a copy for ourselves
                        DestroyImmediate(filteredEncryptions[i].physicalObject);

                        // Retrack sosig
                        SyncTrackedEncryptions(go.transform, true, GameManager.scene);
                    }
                    else // Destroy entire object
                    {
                        // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                        // like we do now if we didn't do immediate because OnDestroy() gets called later
                        // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                        DestroyImmediate(filteredEncryptions[i].physicalObject.gameObject);
                    }
                }
                --giveControlOfDestroyed;
            }

            // Send update to other clients
            if (ThreadManager.host)
            {
                ServerSend.PlayerInstance(0, instance);
            }
            else
            {
                ClientSend.PlayerInstance(instance, sceneLoading);
            }

            // Set players active and playersPresent
            playersPresent = 0;
            if (!nonSynchronizedScenes.ContainsKey(scene))
            {
                // Check each players scene/instance to know if they are in the one we are going into
                // TODO: Review: Since we are now tracking players' scene/instance through playerBySceneByInstance, we don't need to check for each of them here
                //       We should probably just use playersBySceneByInstance
                foreach (KeyValuePair<int, PlayerManager> player in players)
                {
                    if (player.Value.scene.Equals(scene) && player.Value.instance == instance)
                    {
                        ++playersPresent;

                        if (ThreadManager.host && !sceneLoading)
                        {
                            // Request most up to date items from the client
                            // We do this because we may not have the most up to date version of items/sosigs since
                            // clients only send updated data to other players in their scene/instance
                            // But we need the most of to date data to instantiate the object

                            if (Server.clientsWaitingUpDate.TryGetValue(player.Key, out List<int> waitingClients))
                            {
                                waitingClients.Add(0);
                            }
                            else
                            {
                                Server.clientsWaitingUpDate.Add(player.Key, new List<int> { 0 });
                            }
                            if (Server.loadingClientsWaitingFrom.TryGetValue(0, out List<int> waitingFor))
                            {
                                waitingFor.Add(player.Key);
                            }
                            else
                            {
                                Server.loadingClientsWaitingFrom.Add(0, new List<int>() { player.Key });
                            }
                            ServerSend.RequestUpToDateObjects(player.Key, true, 0);
                        }
                    }

                    UpdatePlayerHidden(player.Value);
                }
            }
            else // New scene not syncable, ensure all players are disabled regardless of scene
            {
                foreach (KeyValuePair<int, PlayerManager> player in players)
                {
                    UpdatePlayerHidden(player.Value);
                }
            }

            // Set max health based on setting
            WristMenuSection.UpdateMaxHealth(scene, instance, -2, -1);
        }

        // MOD: When player state data gets sent between clients, the sender will call this
        //      to let mods write any custom data they want to the packet
        //      This is data you want to have communicated with the other clients about yourself (ex.: scores, health, etc.)
        //      To ensure compatibility with other mods you should extend the array as necessary and identify your part of the array with a specific 
        //      code to find it in the array the first time, then keep that index in your mod to always find it in O(1) time later
        public static void WriteAdditionalPlayerState(byte[] data)
        {

        }

        // MOD: This is where your mod would read the byte[] of additional player data
        public static void ProcessAdditionalPlayerData(int playerID, byte[] data)
        {

        }

        public static bool GetTrackedObjectType(Transform t, out Type trackedObjectType)
        {
            foreach(KeyValuePair<string, Type> entry in Mod.trackedObjectTypes)
            {
                if ((bool)entry.Value.InvokeMember("IsOfType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, null, new object[] { t }))
                {
                    trackedObjectType = entry.Value;
                    return true;
                }
            }

            trackedObjectType = null;
            return false;
        }

        public static bool IsControlled(Transform root, Type trackedObjectType)
        {
            MethodInfo method = trackedObjectType.GetMethod("IsControlled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters()[0].ParameterType == typeof(Transform))
            {
                return (bool)method.Invoke(null, new object[] { root });
            }

            return false;
        }

        // MOD: This will be called to check if the given sosig is controlled by this client
        //      This currently checks if any link of the sosig is controlled
        //      A mod can postfix this to change the return value if it wants to have control of sosigs based on other criteria
        public static bool IsControlled(Sosig sosig)
        {
            foreach(SosigLink link in sosig.Links)
            {
                if(link != null && link.O != null && IsControlled(link.O))
                {
                    return true;
                }
            }
            return false;
        }

        // MOD: This will be called to check if the given AutoMeater is controlled by this client
        //      This currently checks if any link of the AutoMeater is controlled
        //      A mod can postfix this to change the return value if it wants to have control of AutoMeaters based on other criteria
        public static bool IsControlled(AutoMeater autoMeater)
        {
            return autoMeater.PO.m_hand != null;
        }

        public static void OnSceneLoadedVR(bool loading)
        {
            // Return right away if we don't have server or client running
            if(Mod.managerObject == null)
            {
                if (loading)
                {
                    sceneLoading = true;
                    instanceAtSceneLoadStart = instance;
                    sceneAtSceneLoadStart = GameManager.scene;
                    connectedAtLoadStart = false;
                }
                else
                {
                    GameManager.scene = LoadLevelBeginPatch.loadingLevel;
                    sceneLoading = false;
                }
                return;
            }

            if (loading) // Just started loading
            {
                Mod.LogInfo("Switching scene, from " + GameManager.scene + " to " + LoadLevelBeginPatch.loadingLevel, false);
                sceneLoading = true;
                instanceAtSceneLoadStart = instance;
                sceneAtSceneLoadStart = GameManager.scene;
                connectedAtLoadStart = true;

                ++giveControlOfDestroyed;

                ++Mod.skipAllInstantiates;

                if (playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances0))
                {
                    if (relevantInstances0.TryGetValue(instance, out List<int> relevantPlayers))
                    {
                        playersAtLoadStart = relevantPlayers;
                    }
                    else
                    {
                        playersAtLoadStart = null;
                    }
                }
                else
                {
                    playersAtLoadStart = null;
                }

                // Get out of TNH instance 
                // This makes assumption that player must go through main menu to leave TNH
                // TODO: If this is not always true, will have to handle by "if we leave a TNH scene" instead of "if we go into main menu"
                if (LoadLevelBeginPatch.loadingLevel.Equals("MainMenu3") && Mod.currentTNHInstance != null) 
                {
                    // The destruction of items as we leave the level with giveControlOfDestroyed to true will handle the handover of 
                    // item and sosig control. SetInstance will handle the update of activeInstances and TNHInstances
                    SetInstance(0);
                    if (Mod.currentlyPlayingTNH)
                    {
                        Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, ID, ThreadManager.host);
                        Mod.currentlyPlayingTNH = false;
                    }
                    Mod.currentTNHInstance = null;
                    Mod.TNHSpectating = false;
                    Mod.currentlyPlayingTNH = false;
                }

                // Check if there are other players where we are going to prevent things like prefab spawns
                Mod.LogInfo("\tChecking if have control at load start");
                if (playersByInstanceByScene.TryGetValue(LoadLevelBeginPatch.loadingLevel, out Dictionary<int, List<int>> relevantInstances))
                {
                    Mod.LogInfo("\t\tThere are "+ relevantInstances .Count+ " instances listed in the level we are loading: "+ LoadLevelBeginPatch.loadingLevel);
                    if (relevantInstances.TryGetValue(instance, out List<int> relevantPlayers))
                    {
                        Mod.LogInfo("\t\t\tThere are " + relevantPlayers.Count + " players listed in the instance we are in: " + instance);
                        controlOverride = relevantPlayers.Count == 0;
                        firstPlayerInSceneInstance = controlOverride;
                    }
                    else
                    {
                        controlOverride = true;
                        firstPlayerInSceneInstance = true;
                    }
                }
                else
                {
                    controlOverride = true;
                    firstPlayerInSceneInstance = true;
                }
                Mod.LogInfo("\tAt load start, controlOverride: " + controlOverride + ", first in scene: " + firstPlayerInSceneInstance);

                // Clear any of our tracked items that have not awoken in the previous scene
                ClearUnawoken();
            }
            else // Finished loading
            {
                scene = LoadLevelBeginPatch.loadingLevel;
                Mod.LogInfo("Arrived in scene: " + scene, false);
                sceneLoading = false;

                // Send an update to let others know we changed scene
                if (ThreadManager.host)
                {
                    // Send the host's scene to clients
                    ServerSend.PlayerScene(0, LoadLevelBeginPatch.loadingLevel);
                }
                else
                {
                    // Send to server, host will update and then send to all other clients
                    ClientSend.PlayerScene(LoadLevelBeginPatch.loadingLevel);
                }

                if (connectedAtLoadStart)
                {
                    --Mod.skipAllInstantiates;

                    --giveControlOfDestroyed;
                }

                // Update players' active state depending on which are in the same scene/instance
                playersPresent = 0;
                if (!nonSynchronizedScenes.ContainsKey(scene))
                {
                    Mod.LogInfo("Arrived in synchronized scene");
                    controlOverride = true;
                    firstPlayerInSceneInstance = true;
                    foreach (KeyValuePair<int, PlayerManager> player in players)
                    {
                        Mod.LogInfo("\tChecking other player "+player.Key+" with scene: "+player.Value.scene+" and instance: "+player.Value.instance);
                        if (player.Value.scene.Equals(scene) && player.Value.instance == instance)
                        {
                            Mod.LogInfo("\t\tThis player is in our scene/instance: "+ scene+"/"+instance);
                            ++playersPresent;

                            // NOTE: Calculating control override when we finish loading here is necessary
                            // Consider the server loading into a scene. When they started loading, they thought they would be the first in the scene, controlOverride = true.
                            // Another client, loads into that scene, track their items, the server accepts them since that client was the first in scene.
                            // Then server arrives, tracks their own init scene items and sends to clients. Reinitializing the scene, causing double instantiation.
                            // Unless we calculate it here again, at which point the server will know someone else is in their new scene, preventing to track their own items.

                            // NOTE: See note above, this also means that scenes that base their initialization on some other criteria will be required to wait 
                            // until we are done loading before spawning any tracked objects, as they will otherwise be destroyed if they did not have control override.
                            // This is the case for TNH, see TNH_ManagerPatch.DelayedInitPrefix, where we prevent it until done loading, even if in control of the TNH instance.
                            controlOverride = false;
                            firstPlayerInSceneInstance = false;

                            if (ThreadManager.host)
                            {
                                // Request most up to date objects from the client
                                // We do this because we may not have the most up to date version of objects since
                                // clients only send updated data when there are others in their scene
                                // But we need the most of to date data to instantiate the object
                                Mod.LogInfo("Server sending request for up to date objects to " + player.Key);

                                if (Server.clientsWaitingUpDate.TryGetValue(player.Key, out List<int> waitingClients))
                                {
                                    waitingClients.Add(0);
                                }
                                else
                                {
                                    Server.clientsWaitingUpDate.Add(player.Key, new List<int> { 0 });
                                }
                                if (Server.loadingClientsWaitingFrom.TryGetValue(0, out List<int> waitingFor))
                                {
                                    waitingFor.Add(player.Key);
                                }
                                else
                                {
                                    Server.loadingClientsWaitingFrom.Add(0, new List<int>() { player.Key });
                                }
                                ServerSend.RequestUpToDateObjects(player.Key, true, 0);
                            }
                        }

                        UpdatePlayerHidden(player.Value);
                    }

                    // Only ever want to track objects if we were connected before we started loading
                    controlOverride &= connectedAtLoadStart;
                    firstPlayerInSceneInstance &= connectedAtLoadStart;

                    // Just arrived in syncable scene, sync items with server/clients
                    // NOTE THAT THIS IS DEPENDENT ON US HAVING UPDATED WHICH OTHER PLAYERS ARE VISIBLE LIKE WE DO IN THE ABOVE LOOP
                    inPostSceneLoadTrack = true;
                    SyncTrackedObjects();
                    inPostSceneLoadTrack = false;

                    controlOverride = false;

                    // Instantiate any object we control that we have not yet instantiated
                    // This could happen if we are given control of an objects while loading
                    for (int i = 0; i < objects.Count; ++i)
                    {
                        if (objects[i].physical == null && !objects[i].awaitingInstantiation)
                        {
                            objects[i].awaitingInstantiation = true;
                            AnvilManager.Run(objects[i].Instantiate());
                        }
                    }
                }
                else // New scene not syncable, ensure all players are disabled regardless of scene
                {
                    foreach (KeyValuePair<int, PlayerManager> player in players)
                    {
                        UpdatePlayerHidden(player.Value);
                    }
                }

                // Set max health based on setting
                WristMenuSection.UpdateMaxHealth(scene, instance, -2, -1);
            }
        }

        public static void ClearUnawoken()
        {
            // Clear any tracked object that we are supposed to be controlling that doesn't have a physical assigned
            // These can build up in certain cases. The main one is when we load into a level which contains objects that are inactive by default
            // These objects will never be awoken, they will therefore be tracked but not synced with other clients. When we leave the scene, these objects 
            // may be destroyed but their OnDestroy will not be called because they were never awoken, meaning they will still be in the objects list
            for(int i = objects.Count-1; i >= 0; --i)
            {
                if ((objects[i].physical != null && !objects[i].physical.awoken) || (objects[i].physical == null && !objects[i].awaitingInstantiation))
                {
                    objects[i].RemoveFromLocal();
                }
            }
        }

        // MOD: If you want to process damage differently, you can patch this
        //      Meatov uses this to apply damage to specific limbs for example
        public static void ProcessPlayerDamage(PlayerHitbox.Part part, Damage damage)
        {
            if (part == PlayerHitbox.Part.Head)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    GM.CurrentPlayerBody.Hitboxes[0].Damage(damage);
                }
                else
                {
                    GM.CurrentPlayerBody.Hitboxes[1].Damage(damage);
                }
            }
            else if (part == PlayerHitbox.Part.Torso)
            {
                GM.CurrentPlayerBody.Hitboxes[2].Damage(damage);
            }
            else
            {
                damage.Dam_TotalEnergetic *= 0.15f;
                damage.Dam_TotalKinetic *= 0.15f;
                GM.CurrentPlayerBody.Hitboxes[2].Damage(damage);
            }
        }

        // MOD: This will get called when a client disconnects from the server
        //      A mod should postfix this to give control of whatever elements it has that are tracked through H3MP that are under this client's control
        //      This will also get called when a TNH controller gives up control to redistribute control of sosigs/automeaters/encryptions
        //      to the new controller. overrideController will be set to the new controller, and all will be false, specifying not to distribute item control
        public static void DistributeAllControl(int clientID, int overrideController = -1, bool all = true)
        {
            // Get best potential host
            int newController = overrideController == -1 ? Mod.GetBestPotentialObjectHost(clientID, false) : overrideController;

            // TODO: Optimization: Could keep track of items by controller in a dict, go through those specifically
            //                     Or could at least use itemsByInstanceByScene and go only through the item in the same scene/instance as the client

            // Give all items
            if (all)
            {
                for (int i = 0; i < Server.items.Length; ++i)
                {
                    if (Server.items[i] != null && Server.items[i].controller == clientID)
                    {
                        TrackedItemData trackedItem = Server.items[i];

                        bool destroyed = newController == -1;

                        if (destroyed) // No other player to take control, destroy
                        {
                            if(trackedItem.physicalItem == null)
                            {
                                ServerSend.DestroyItem(trackedItem.trackedID);
                                Server.items[trackedItem.trackedID] = null;
                                Server.availableItemIndices.Add(trackedItem.trackedID);
                                if (itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                    currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                {
                                    itemList.Remove(trackedItem.trackedID);
                                }
                                trackedItem.awaitingInstantiation = false;

                                if(clientID == GameManager.ID)
                                {
                                    trackedItem.RemoveFromLocal();
                                }
                            }
                            else
                            {
                                Destroy(trackedItem.physicalItem.gameObject);
                            }
                        }
                        else if (clientID != 0 && newController == 0) // If new controller is us, take control
                        {
                            trackedItem.localTrackedID = GameManager.items.Count;
                            GameManager.items.Add(trackedItem);
                            // Physical object could be null if we are given control while we are loading, the giving client will think we are in their scene/instance
                            if (trackedItem.physicalItem == null)
                            {
                                // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                                // Otherwise we send destroy order for the object
                                if (!GameManager.sceneLoading)
                                {
                                    if (trackedItem.scene.Equals(GameManager.scene) && trackedItem.instance == GameManager.instance)
                                    {
                                        if (!trackedItem.awaitingInstantiation)
                                        {
                                            trackedItem.awaitingInstantiation = true;
                                            AnvilManager.Run(trackedItem.Instantiate());
                                        }
                                    }
                                    else
                                    {
                                        if (trackedItem.physicalItem != null)
                                        {
                                            Destroy(trackedItem.physicalItem.gameObject);
                                        }
                                        else
                                        {
                                            ServerSend.DestroyItem(trackedItem.trackedID);
                                            trackedItem.RemoveFromLocal();
                                            Server.items[trackedItem.trackedID] = null;
                                            Server.availableItemIndices.Add(trackedItem.trackedID);
                                            if (itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                                currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                                            {
                                                itemList.Remove(trackedItem.trackedID);
                                            }
                                            trackedItem.awaitingInstantiation = false;
                                        }
                                        destroyed = true;
                                    }
                                }
                                // else we will instantiate when we are done loading
                            }
                            else if (trackedItem.parent == -1)
                            {
                                Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
                            }
                        }
                        else if(clientID == 0 && newController != 0 && trackedItem.physicalItem != null) // If we were the controller
                        {
                            Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, true);
                        }

                        if (!destroyed)
                        {
                            trackedItem.SetController(newController);

                            ServerSend.GiveObjectControl(trackedItem.trackedID, newController, null);
                        }
                    }
                }
            }

            // Give all sosigs
            for (int i = 0; i < Server.sosigs.Length; ++i)
            {
                if (Server.sosigs[i] != null && Server.sosigs[i].controller == clientID)
                {
                    TrackedSosigData trackedSosig = Server.sosigs[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        if (trackedSosig.physicalObject == null)
                        {
                            ServerSend.DestroySosig(trackedSosig.trackedID);
                            Server.sosigs[trackedSosig.trackedID] = null;
                            Server.availableSosigIndices.Add(trackedSosig.trackedID);
                            if (sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                            {
                                sosigList.Remove(trackedSosig.trackedID);
                            }
                            trackedSosig.awaitingInstantiation = false;

                            if (clientID == GameManager.ID)
                            {
                                trackedSosig.RemoveFromLocal();
                            }
                        }
                        else
                        {
                            Destroy(trackedSosig.physicalObject.gameObject);
                        }
                    }
                    else if (clientID != 0 && newController == 0) // If its us, take control
                    {
                        trackedSosig.localTrackedID = GameManager.sosigs.Count;
                        GameManager.sosigs.Add(trackedSosig);
                        if (trackedSosig.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!GameManager.sceneLoading)
                            {
                                if (trackedSosig.scene.Equals(GameManager.scene) && trackedSosig.instance == GameManager.instance)
                                {
                                    if (!trackedSosig.awaitingInstantiation)
                                    {
                                        trackedSosig.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedSosig.Instantiate());
                                    }
                                }
                                else
                                {
                                    if (trackedSosig.physicalObject != null)
                                    {
                                        Destroy(trackedSosig.physicalObject.gameObject);
                                    }
                                    else
                                    {
                                        ServerSend.DestroySosig(trackedSosig.trackedID);
                                        trackedSosig.RemoveFromLocal();
                                        Server.sosigs[trackedSosig.trackedID] = null;
                                        Server.availableSosigIndices.Add(trackedSosig.trackedID);
                                        if (sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                                        {
                                            sosigList.Remove(trackedSosig.trackedID);
                                        }
                                        trackedSosig.awaitingInstantiation = false;
                                    }
                                    destroyed = true;
                                }
                            }
                            // else we will instantiate when we are done loading
                        }
                        else
                        {
                            if (GM.CurrentAIManager != null)
                            {
                                GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                            }
                            trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                        }
                    }
                    else if(clientID == 0 && newController != 0 && trackedSosig.physicalObject != null) // If we were the controller
                    {
                        trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
                    }

                    if (!destroyed)
                    {
                        trackedSosig.controller = newController;

                        ServerSend.GiveSosigControl(trackedSosig.trackedID, newController, null);

                        if (newController == GameManager.ID)
                        {
                            trackedSosig.TakeInventoryControl();
                        }
                    }
                }
            }

            // Give all automeaters
            for (int i = 0; i < Server.autoMeaters.Length; ++i)
            {
                if (Server.autoMeaters[i] != null && Server.autoMeaters[i].controller == clientID)
                {
                    TrackedAutoMeaterData trackedAutoMeater = Server.autoMeaters[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        if (trackedAutoMeater.physicalObject == null)
                        {
                            ServerSend.DestroyAutoMeater(trackedAutoMeater.trackedID);
                            Server.autoMeaters[trackedAutoMeater.trackedID] = null;
                            Server.availableAutoMeaterIndices.Add(trackedAutoMeater.trackedID);
                            if (autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                            {
                                autoMeaterList.Remove(trackedAutoMeater.trackedID);
                            }
                            trackedAutoMeater.awaitingInstantiation = false;

                            if (clientID == GameManager.ID)
                            {
                                trackedAutoMeater.RemoveFromLocal();
                            }
                        }
                        else
                        {
                            Destroy(trackedAutoMeater.physicalObject.gameObject);
                        }
                    }
                    else if (newController == 0) // If its us, take control
                    {
                        trackedAutoMeater.localTrackedID = GameManager.autoMeaters.Count;
                        GameManager.autoMeaters.Add(trackedAutoMeater);
                        if (trackedAutoMeater.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!GameManager.sceneLoading)
                            {
                                if (trackedAutoMeater.scene.Equals(GameManager.scene) && trackedAutoMeater.instance == GameManager.instance)
                                {
                                    if (!trackedAutoMeater.awaitingInstantiation)
                                    {
                                        trackedAutoMeater.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedAutoMeater.Instantiate());
                                    }
                                }
                                else
                                {
                                    if (trackedAutoMeater.physicalObject != null)
                                    {
                                        Destroy(trackedAutoMeater.physicalObject.gameObject);
                                    }
                                    else
                                    {
                                        ServerSend.DestroyAutoMeater(trackedAutoMeater.trackedID);
                                        trackedAutoMeater.RemoveFromLocal();
                                        Server.autoMeaters[trackedAutoMeater.trackedID] = null;
                                        Server.availableAutoMeaterIndices.Add(trackedAutoMeater.trackedID);
                                        if (autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                                        {
                                            autoMeaterList.Remove(trackedAutoMeater.trackedID);
                                        }
                                        trackedAutoMeater.awaitingInstantiation = false;
                                    }
                                    destroyed = true;
                                }
                            }
                            // else we will instantiate when we are done loading
                        }
                        else
                        {
                            if (GM.CurrentAIManager != null)
                            {
                                GM.CurrentAIManager.RegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                            }
                            trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                        }
                    }
                    else if(clientID == 0 && newController != 0 && trackedAutoMeater.physicalObject != null) // If we were the controller
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
                    }

                    if (!destroyed)
                    {
                        trackedAutoMeater.controller = newController;

                        ServerSend.GiveAutoMeaterControl(trackedAutoMeater.trackedID, newController, null);
                    }
                }
            }

            // Give all encryptions
            for (int i = 0; i < Server.encryptions.Length; ++i)
            {
                if (Server.encryptions[i] != null && Server.encryptions[i].controller == clientID)
                {
                    TrackedEncryptionData trackedEncryption = Server.encryptions[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        if (trackedEncryption.physicalObject == null)
                        {
                            ServerSend.DestroyEncryption(trackedEncryption.trackedID);
                            Server.encryptions[trackedEncryption.trackedID] = null;
                            Server.availableEncryptionIndices.Add(trackedEncryption.trackedID);
                            if (encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                            {
                                encryptionList.Remove(trackedEncryption.trackedID);
                            }
                            trackedEncryption.awaitingInstantiation = false;

                            if (clientID == GameManager.ID)
                            {
                                trackedEncryption.RemoveFromLocal();
                            }
                        }
                        else
                        {
                            Destroy(trackedEncryption.physicalObject.gameObject);
                        }
                    }
                    else if (newController == 0)
                    {
                        trackedEncryption.localTrackedID = GameManager.encryptions.Count;
                        GameManager.encryptions.Add(trackedEncryption);
                        if (trackedEncryption.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!GameManager.sceneLoading)
                            {
                                if (trackedEncryption.scene.Equals(GameManager.scene) && trackedEncryption.instance == GameManager.instance)
                                {
                                    if (!trackedEncryption.awaitingInstantiation)
                                    {
                                        trackedEncryption.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedEncryption.Instantiate());
                                    }
                                }
                                else
                                {
                                    if (trackedEncryption.physicalObject != null)
                                    {
                                        Destroy(trackedEncryption.physicalObject.gameObject);
                                    }
                                    else
                                    {
                                        ServerSend.DestroyEncryption(trackedEncryption.trackedID);
                                        trackedEncryption.RemoveFromLocal();
                                        Server.encryptions[trackedEncryption.trackedID] = null;
                                        Server.availableEncryptionIndices.Add(trackedEncryption.trackedID);
                                        if (encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                                        {
                                            encryptionList.Remove(trackedEncryption.trackedID);
                                        }
                                        trackedEncryption.awaitingInstantiation = false;
                                    }
                                    destroyed = true;
                                }
                            }
                            // else we will instantiate when we are done loading
                        }
                    }

                    if (!destroyed)
                    {
                        trackedEncryption.controller = newController;

                        ServerSend.GiveEncryptionControl(trackedEncryption.trackedID, newController, null);
                    }
                }
            }
        }

        public static void TakeAllPhysicalControl(bool destroyTrackedScript)
        {
            TrackedItemData[] itemArrToUse = null;
            TrackedSosigData[] sosigArrToUse = null;
            TrackedAutoMeaterData[] autoMeaterArrToUse = null;
            TrackedEncryptionData[] encryptionArrToUse = null;
            if (ThreadManager.host)
            {
                itemArrToUse = Server.items;
                sosigArrToUse = Server.sosigs;
                autoMeaterArrToUse = Server.autoMeaters;
                encryptionArrToUse = Server.encryptions;
            }
            else
            {
                itemArrToUse = Client.items;
                sosigArrToUse = Client.sosigs;
                autoMeaterArrToUse = Client.autoMeaters;
                encryptionArrToUse = Client.encryptions;
            }

            Mod.LogInfo("Taking all physical control, destroying item scripts? : "+ destroyTrackedScript);
            foreach (TrackedItemData item in itemArrToUse)
            {
                if(item != null && item.physicalItem != null)
                {
                    Mod.SetKinematicRecursive(item.physicalItem.transform, false);
                    if (destroyTrackedScript)
                    {
                        item.physicalItem.skipFullDestroy = true;
                        Destroy(item.physicalItem);
                    }
                }
            }

            foreach (TrackedSosigData sosig in sosigArrToUse)
            {
                if(sosig != null && sosig.physicalObject != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(sosig.physicalObject.physicalSosigScript.E);
                    }
                    sosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                    if (destroyTrackedScript)
                    {
                        sosig.physicalObject.skipFullDestroy = true;
                        Destroy(sosig.physicalObject);
                    }
                }
            }

            foreach (TrackedAutoMeaterData autoMeater in autoMeaterArrToUse)
            {
                if(autoMeater != null && autoMeater.physicalObject != null)
                {
                    if (GM.CurrentAIManager != null)
                    {
                        GM.CurrentAIManager.RegisterAIEntity(autoMeater.physicalObject.physicalAutoMeaterScript.E);
                    }
                    autoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                    if (destroyTrackedScript)
                    {
                        autoMeater.physicalObject.skipFullDestroy = true;
                        Destroy(autoMeater.physicalObject);
                    }
                }
            }

            foreach (TrackedEncryptionData encryption in encryptionArrToUse)
            {
                if(encryption != null && encryption.physicalObject != null)
                {
                    if (destroyTrackedScript)
                    {
                        encryption.physicalObject.skipFullDestroy = true;
                        Destroy(encryption.physicalObject);
                    }
                }
            }
        }
    }
}
