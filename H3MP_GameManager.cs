using FistVR;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace H3MP
{
    internal class H3MP_GameManager : MonoBehaviour
    {
        private static H3MP_GameManager _singleton;
        public static H3MP_GameManager singleton
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
                    Mod.LogInfo($"{nameof(H3MP_GameManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public static Dictionary<int, H3MP_PlayerManager> players = new Dictionary<int, H3MP_PlayerManager>();
        public static List<int> spectatorHosts = new List<int>(); // List of all spectator hosts, not necessarily available 
        public static List<H3MP_TrackedItemData> items = new List<H3MP_TrackedItemData>(); // Tracked items under control of this gameManager
        public static List<H3MP_TrackedSosigData> sosigs = new List<H3MP_TrackedSosigData>(); // Tracked sosigs under control of this gameManager
        public static List<H3MP_TrackedAutoMeaterData> autoMeaters = new List<H3MP_TrackedAutoMeaterData>(); // Tracked AutoMeaters under control of this gameManager
        public static List<H3MP_TrackedEncryptionData> encryptions = new List<H3MP_TrackedEncryptionData>(); // Tracked TNH_EncryptionTarget under control of this gameManager
        public static Dictionary<string, int> nonSynchronizedScenes = new Dictionary<string, int>(); // Dict of scenes that can be synced
        public static Dictionary<FVRPhysicalObject, H3MP_TrackedItem> trackedItemByItem = new Dictionary<FVRPhysicalObject, H3MP_TrackedItem>();
        public static Dictionary<SosigWeapon, H3MP_TrackedItem> trackedItemBySosigWeapon = new Dictionary<SosigWeapon, H3MP_TrackedItem>();
        public static Dictionary<Sosig, H3MP_TrackedSosig> trackedSosigBySosig = new Dictionary<Sosig, H3MP_TrackedSosig>();
        public static Dictionary<AutoMeater, H3MP_TrackedAutoMeater> trackedAutoMeaterByAutoMeater = new Dictionary<AutoMeater, H3MP_TrackedAutoMeater>();
        public static Dictionary<TNH_EncryptionTarget, H3MP_TrackedEncryption> trackedEncryptionByEncryption = new Dictionary<TNH_EncryptionTarget, H3MP_TrackedEncryption>();
        public static Dictionary<int, int> activeInstances = new Dictionary<int, int>();
        public static Dictionary<int, H3MP_TNHInstance> TNHInstances = new Dictionary<int, H3MP_TNHInstance>();
        public static List<int> playersAtLoadStart;
        public static Dictionary<string, Dictionary<int, List<int>>> playersByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
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
        public static int colorIndex = 0;
        public static readonly string[] colorNames = new string[] { "White", "Red", "Green", "Blue", "Black", "Desert", "Forest" };
        public static readonly Color[] colors = new Color[] { Color.white, Color.red, Color.green, Color.blue, Color.black, new Color(0.98431f, 0.86275f, 0.71373f), new Color(0.31373f, 0.31373f, 0.15294f) };
        public static bool colorByIFF = false; 
        public static int nameplateMode = 1; // 0: All, 1: Friendly only (same IFF), 2: None 
        public static int radarMode = 0; // 0: All, 1: Friendly only (same IFF), 2: None 
        public static bool radarColor = true; // True: Colored by IFF, False: Colored by color

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
            Mod.LogInfo($"Spawn player called with ID: {ID}");

            GameObject player = null;
            // Always spawn if this is host (client is null)
            if(H3MP_Client.singleton == null || ID != H3MP_Client.singleton.ID)
            {
                player = Instantiate(playerPrefab);
                DontDestroyOnLoad(player);
            }
            else
            {
                // We dont want to spawn the local player as we will already have spawned when connecting to a server
                return;
            }

            H3MP_PlayerManager playerManager = player.GetComponent<H3MP_PlayerManager>();
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

            firstInSceneInstance &= sceneLoading || !scene.Equals(H3MP_GameManager.scene) || instance != H3MP_GameManager.instance;

            playerManager.firstInSceneInstance = firstInSceneInstance;

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[ID].player.firstInSceneInstance = firstInSceneInstance;
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
            if (!H3MP_GameManager.nonSynchronizedScenes.ContainsKey(scene) && scene.Equals(H3MP_GameManager.scene) && instance == H3MP_GameManager.instance)
            {
                ++playersPresent;

                if (join)
                {
                    // This is a spawn player order from the server since we just joined it, we are not first in the scene
                    H3MP_GameManager.firstPlayerInSceneInstance = false;
                }
            }
        }

        public static void Reset()
        {
            foreach(KeyValuePair<int, H3MP_PlayerManager> playerEntry in players)
            {
                Destroy(playerEntry.Value.gameObject);
            }
            players.Clear();
            spectatorHosts.Clear();
            items.Clear();
            sosigs.Clear();
            autoMeaters.Clear();
            encryptions.Clear();
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

            for (int i=0; i< H3MP_TrackedItem.trackedItemRefObjects.Length; ++i)
            {
                if (H3MP_TrackedItem.trackedItemRefObjects[i] != null)
                {
                    Destroy(H3MP_TrackedItem.trackedItemRefObjects[i]);
                }
            }
            H3MP_TrackedItem.trackedItemRefObjects = new GameObject[100];
            H3MP_TrackedItem.trackedItemReferences = new H3MP_TrackedItem[100];
            H3MP_TrackedItem.availableTrackedItemRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                10,11,12,13,14,15,16,17,18,19,
                                                                                20,21,22,23,24,25,26,27,28,29,
                                                                                30,31,32,33,34,35,36,37,38,39,
                                                                                40,41,42,43,44,45,46,47,48,49,
                                                                                50,51,52,53,54,55,56,57,58,59,
                                                                                60,61,62,63,64,65,66,67,68,69,
                                                                                70,71,72,73,74,75,76,77,78,79,
                                                                                80,81,82,83,84,85,86,87,88,89,
                                                                                90,91,92,93,94,95,96,97,98,99};

            H3MP_TrackedItem.unknownTrackedIDs.Clear();
            H3MP_TrackedItem.unknownControlTrackedIDs.Clear();
            H3MP_TrackedItem.unknownDestroyTrackedIDs.Clear();
            H3MP_TrackedItem.unknownParentTrackedIDs.Clear();
            H3MP_TrackedItem.unknownCrateHolding.Clear();

            H3MP_TrackedSosig.unknownBodyStates.Clear();
            H3MP_TrackedSosig.unknownControlTrackedIDs.Clear();
            H3MP_TrackedSosig.unknownDestroyTrackedIDs.Clear();
            H3MP_TrackedSosig.unknownIFFChart.Clear();
            H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Clear();
            H3MP_TrackedSosig.unknownSetIFFs.Clear();
            H3MP_TrackedSosig.unknownSetOriginalIFFs.Clear();
            H3MP_TrackedSosig.unknownTNHKills.Clear();

            H3MP_TrackedAutoMeater.unknownControlTrackedIDs.Clear();
            H3MP_TrackedAutoMeater.unknownDestroyTrackedIDs.Clear();

            H3MP_TrackedEncryption.unknownControlTrackedIDs.Clear();
            H3MP_TrackedEncryption.unknownDestroyTrackedIDs.Clear();
            H3MP_TrackedEncryption.unknownDisableSubTarg.Clear();
            H3MP_TrackedEncryption.unknownInit.Clear();
            H3MP_TrackedEncryption.unknownResetGrowth.Clear();
            H3MP_TrackedEncryption.unknownSpawnGrowth.Clear();
            H3MP_TrackedEncryption.unknownSpawnSubTarg.Clear();
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

            H3MP_PlayerManager player = players[ID];

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
            Mod.LogInfo("Player " + playerID + " joining scene " + sceneName);
            H3MP_PlayerManager player = players[playerID];

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

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[playerID].player.scene = sceneName;

                H3MP_Server.clients[playerID].player.firstInSceneInstance = firstInSceneInstance;
            }

            UpdatePlayerHidden(player);

            if (sceneName.Equals(H3MP_GameManager.scene) && !H3MP_GameManager.nonSynchronizedScenes.ContainsKey(sceneName) && instance == player.instance)
            {
                ++playersPresent;
            }
            else
            {
                --playersPresent;
            }
        }

        // MOD: This will be called to set a player as hidden based on certain criteria
        //      Currently sets a player as hidden if they are in the same TNH game as us and are dead for example
        //      A mod could prefix this to base it on other criteria, mainly for other game modes
        public static bool UpdatePlayerHidden(H3MP_PlayerManager player)
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
                                ((HashSet<Transform>)Mod.TAH_Reticle_m_trackedTransforms.GetValue(GM.TNH_Manager.TAHReticle)).Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
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
            H3MP_PlayerManager player = players[playerID];

            if (activeInstances.ContainsKey(player.instance))
            {
                --activeInstances[player.instance];
                if (activeInstances[player.instance] == 0)
                {
                    activeInstances.Remove(player.instance);
                }
            }

            if (TNHInstances.TryGetValue(player.instance, out H3MP_TNHInstance currentInstance))
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

            firstInSceneInstance &= sceneLoading || !player.scene.Equals(scene) || player.instance != H3MP_GameManager.instance;

            player.firstInSceneInstance = firstInSceneInstance;

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[playerID].player.instance = instance;

                H3MP_Server.clients[playerID].player.firstInSceneInstance = firstInSceneInstance;
            }

            UpdatePlayerHidden(player);

            if (player.scene.Equals(H3MP_GameManager.scene) && !H3MP_GameManager.nonSynchronizedScenes.ContainsKey(player.scene) && H3MP_GameManager.instance == player.instance)
            {
                ++playersPresent;
            }
            else
            {
                --playersPresent;
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
            if(H3MP_GameManager.ID == ID)
            {
                colorIndex = index;

                if(H3MP_WristMenuSection.colorText != null)
                {
                    H3MP_WristMenuSection.colorText.text = "Current color: " + colorNames[colorIndex];
                }
            }
            else
            {
                players[ID].SetColor(index);

                if (H3MP_ThreadManager.host)
                {
                    H3MP_Server.clients[ID].player.colorIndex = index;
                }
            }

            if (send)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.PlayerColor(ID, index, clientID);
                }
                else if (!received)
                {
                    H3MP_ClientSend.PlayerColor(ID, index);
                }
            }
        }
        
        public static void UpdateTrackedItem(H3MP_TrackedItemData updatedItem, bool ignoreOrder = false)
        {
            if(updatedItem.trackedID == -1)
            {
                return;
            }

            H3MP_TrackedItemData trackedItemData = null;
            if (H3MP_ThreadManager.host)
            {
                if (updatedItem.trackedID < H3MP_Server.items.Length)
                {
                    trackedItemData = H3MP_Server.items[updatedItem.trackedID];
                }
            }
            else
            {
                if (updatedItem.trackedID < H3MP_Client.items.Length)
                {
                    trackedItemData = H3MP_Client.items[updatedItem.trackedID];
                }
            }

            // TODO: Review: Should we keep the up to date data for later if we dont have the tracked item yet?
            //               Concern is that if we send tracked item TCP packet, but before that arrives, we send the insurance updates
            //               meaning we don't have the item for those yet and so when we receive the item itself, we don't have the most up to date
            //               We could keep only the highest order in a dict by trackedID
            if (trackedItemData != null)
            {
                if (trackedItemData.itemID.Equals("ReflexArco"))
                {
                    Mod.LogInfo("\tGot item, controller: "+ trackedItemData.controller + ", ignore order: "+ ignoreOrder + ", updated order: "+ updatedItem.order+", current order: "+ trackedItemData.order);
                }
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

        public static void UpdateTrackedSosig(H3MP_TrackedSosigData updatedSosig, bool ignoreOrder = false)
        {
            if(updatedSosig.trackedID == -1)
            {
                return;
            }

            H3MP_TrackedSosigData trackedSosigData = null;
            if (H3MP_ThreadManager.host)
            {
                if (updatedSosig.trackedID < H3MP_Server.sosigs.Length)
                {
                    trackedSosigData = H3MP_Server.sosigs[updatedSosig.trackedID];
                }
            }
            else
            {
                if (updatedSosig.trackedID < H3MP_Client.sosigs.Length)
                {
                    trackedSosigData = H3MP_Client.sosigs[updatedSosig.trackedID];
                }
            }

            if (trackedSosigData != null)
            {
                // If we take control of a sosig, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedSosigData.controller != H3MP_GameManager.ID && (ignoreOrder || ((updatedSosig.order > trackedSosigData.order || trackedSosigData.order - updatedSosig.order > 128))))
                {
                    trackedSosigData.Update(updatedSosig);
                }
            }
        }

        public static void UpdateTrackedAutoMeater(H3MP_TrackedAutoMeaterData updatedAutoMeater, bool ignoreOrder = false)
        {
            if(updatedAutoMeater.trackedID == -1)
            {
                return;
            }

            H3MP_TrackedAutoMeaterData trackedAutoMeaterData = null;
            if (H3MP_ThreadManager.host)
            {
                if (updatedAutoMeater.trackedID < H3MP_Server.autoMeaters.Length)
                {
                    trackedAutoMeaterData = H3MP_Server.autoMeaters[updatedAutoMeater.trackedID];
                }
            }
            else
            {
                if (updatedAutoMeater.trackedID < H3MP_Client.autoMeaters.Length)
                {
                    trackedAutoMeaterData = H3MP_Client.autoMeaters[updatedAutoMeater.trackedID];
                }
            }

            if (trackedAutoMeaterData != null)
            {
                // If we take control of a AutoMeater, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if(trackedAutoMeaterData.controller != H3MP_GameManager.ID && (ignoreOrder || ((updatedAutoMeater.order > trackedAutoMeaterData.order || trackedAutoMeaterData.order - updatedAutoMeater.order > 128))))
                {
                    trackedAutoMeaterData.Update(updatedAutoMeater);
                }
            }
        }

        public static void UpdateTrackedEncryption(H3MP_TrackedEncryptionData updatedEncryption, bool ignoreOrder = false)
        {
            if(updatedEncryption.trackedID == -1)
            {
                return;
            }

            H3MP_TrackedEncryptionData trackedEncryptionData = null;
            if (H3MP_ThreadManager.host)
            {
                if (updatedEncryption.trackedID < H3MP_Server.encryptions.Length)
                {
                    trackedEncryptionData = H3MP_Server.encryptions[updatedEncryption.trackedID];
                }
            }
            else
            {
                if (updatedEncryption.trackedID < H3MP_Client.encryptions.Length)
                {
                    trackedEncryptionData = H3MP_Client.encryptions[updatedEncryption.trackedID];
                }
            }

            if (trackedEncryptionData != null)
            {
                // If we take control of a encryption, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedEncryptionData.controller != H3MP_GameManager.ID && (ignoreOrder || ((updatedEncryption.order > trackedEncryptionData.order || trackedEncryptionData.order - updatedEncryption.order > 128))))
                {
                    trackedEncryptionData.Update(updatedEncryption);
                }
            }
        }

        public static void SyncTrackedItems(bool init = false, bool inControl = false)
        {
            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other items get destroyed. We will receive any item that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach(GameObject root in roots)
            {
                SyncTrackedItems(root.transform, init ? inControl : controlOverride, null, H3MP_GameManager.scene);
            }
        }

        public static void SyncTrackedItems(Transform root, bool controlEverything, H3MP_TrackedItemData parent, string scene)
        {
            // NOTE: When we sync tracked items, we always send the parent before its children, through TCP. This means we are guaranteed 
            //       that if we receive a full item packet on the server or any client and it has a parent,
            //       this parent is guaranteed to be in the global list already
            //       We are later dependent on this fact so if we modify anything here, ensure this remains true
            FVRPhysicalObject physObj = root.GetComponent<FVRPhysicalObject>();
            if (physObj != null)
            {
                if (IsObjectIdentifiable(physObj))
                {
                    H3MP_TrackedItem currentTrackedItem = root.GetComponent<H3MP_TrackedItem>();
                    if (currentTrackedItem == null)
                    {
                        if (controlEverything || IsControlled(physObj))
                        {
                            Mod.LogInfo("Tracking " + physObj.name);
                            H3MP_TrackedItem trackedItem = MakeItemTracked(physObj, parent);
                            if (trackedItem.awoken)
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    Mod.LogInfo("\tAwake, we are server, adding");
                                    // This will also send a packet with the item to be added in the client's global item list
                                    H3MP_Server.AddTrackedItem(trackedItem.data, 0);
                                }
                                else
                                {
                                    // Tell the server we need to add this item to global tracked items
                                    trackedItem.data.localWaitingIndex = H3MP_Client.localItemCounter++;
                                    H3MP_Client.waitingLocalItems.Add(trackedItem.data.localWaitingIndex, trackedItem.data);
                                    Mod.LogInfo("\tAwake, we are client, sending a "+ trackedItem.data.itemID+ " to server with index: "+ trackedItem.data.localWaitingIndex+" with controller: "+trackedItem.data.controller);
                                    H3MP_ClientSend.TrackedItem(trackedItem.data);
                                }
                            }
                            else
                            {
                                Mod.LogInfo("\tNot awake setting flag");
                                trackedItem.sendOnAwake = true;
                            }

                            foreach (Transform child in root)
                            {
                                SyncTrackedItems(child, controlEverything, trackedItem.data, scene);
                            }
                        }
                        else // Item will not be controlled by us but is an item that should be tracked by system, so destroy it
                        {
                            Mod.LogInfo("We do not want to track "+ root.name+" destroying");
                            Destroy(root.gameObject);
                        }
                    }
                    else
                    {
                        // It already has tracked item on it, this is possible of we received new item from server before we sync
                        return;
                    }
                }
            }
        }

        private static H3MP_TrackedItem MakeItemTracked(FVRPhysicalObject physObj, H3MP_TrackedItemData parent)
        {
            H3MP_TrackedItem trackedItem = physObj.gameObject.AddComponent<H3MP_TrackedItem>();
            H3MP_TrackedItemData data = new H3MP_TrackedItemData();
            trackedItem.data = data;
            data.physicalItem = trackedItem;
            data.physicalItem.physicalObject = physObj;

            // Call an init update because the one in awake won't be called because data was not set yet
            if(trackedItem.updateFunc != null)
            {
                trackedItem.updateFunc();
            }

            H3MP_GameManager.trackedItemByItem.Add(physObj, trackedItem);
            if(physObj is SosigWeaponPlayerInterface)
            {
                H3MP_GameManager.trackedItemBySosigWeapon.Add((physObj as SosigWeaponPlayerInterface).W, trackedItem);
            }

            if (parent != null)
            {
                data.parent = parent.trackedID;
                if (parent.children == null)
                {
                    parent.children = new List<H3MP_TrackedItemData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);
            }
            SetItemIdentifyingInfo(physObj, data);
            data.position = trackedItem.transform.position;
            data.rotation = trackedItem.transform.rotation;
            data.active = trackedItem.gameObject.activeInHierarchy;
            data.underActiveControl = IsControlled(trackedItem.physicalObject);

            data.scene = H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene;
            data.instance = instance;
            data.controller = ID;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;

            CollectExternalData(data);

            // Add to local list
            data.localTrackedID = items.Count;
            items.Add(data);

            return trackedItem;
        }

        // MOD: This will be called upon tracking a new item
        //      From here you will be able to set specific data on the item
        //      For more information and example, take a look at CollectExternalData(H3MP_TrackedSosigData)
        private static void CollectExternalData(H3MP_TrackedItemData trackedItemData)
        {
            trackedItemData.additionalData = new byte[3];

            trackedItemData.additionalData[0] = TNH_SupplyPointPatch.inSpawnBoxes ? (byte)1 : (byte)0;
            if (TNH_SupplyPointPatch.inSpawnBoxes)
            {
                BitConverter.GetBytes((short)TNH_SupplyPointPatch.supplyPointIndex).CopyTo(trackedItemData.additionalData, 1);
            }
        }

        // MOD: If you have a type of item (FVRPhysicalObject) that doen't have an ObjectWrapper,
        //      you can set custom identifying info here as we currently do for TNH_ShatterableCrate
        public static void SetItemIdentifyingInfo(FVRPhysicalObject physObj, H3MP_TrackedItemData trackedItemData)
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
                trackedItemData.identifyingData = new byte[3];
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

                trackedItemData.identifyingData[1] = (bool)Mod.TNH_ShatterableCrate_m_isHoldingHealth.GetValue(crate) ? (byte)1 : (byte)0;
                trackedItemData.identifyingData[2] = (bool)Mod.TNH_ShatterableCrate_m_isHoldingToken.GetValue(crate) ? (byte)1 : (byte)0;

                return;
            }
        }

        // MOD: Certain FVRPhysicalObjects don't have an ObjectWrapper or an IDSpawnedFrom
        //      We would normally not want to track these but there may be some exceptions, like TNH_ShatterableCrates
        public static bool IsObjectIdentifiable(FVRPhysicalObject physObj)
        {
            return physObj.ObjectWrapper != null ||
                   (physObj.IDSpawnedFrom != null && (IM.OD.ContainsKey(physObj.IDSpawnedFrom.name) || IM.OD.ContainsKey(physObj.IDSpawnedFrom.ItemID))) ||
                   physObj.GetComponent<TNH_ShatterableCrate>() != null;
        }

        // MOD: When the server receives an item to track, it will first check if it can identify the item on its side
        //      If your mod added something in IsObjectIdentifiable() then you should also support it in here
        public static bool IsItemIdentifiable(H3MP_TrackedItemData itemData)
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
                SyncTrackedSosigs(root.transform, init ? inControl : controlOverride, H3MP_GameManager.scene);
            }
        }

        public static void SyncTrackedSosigs(Transform root, bool controlEverything, string scene)
        {
            Sosig sosigScript = root.GetComponent<Sosig>();
            if (sosigScript != null)
            {
                H3MP_TrackedSosig trackedSosig = root.GetComponent<H3MP_TrackedSosig>();
                if (trackedSosig == null)
                {
                    if (controlEverything)
                    {
                        trackedSosig = MakeSosigTracked(sosigScript);
                        if (trackedSosig.awoken) 
                        { 
                            if (H3MP_ThreadManager.host)
                            {
                                // This will also send a packet with the sosig to be added in the client's global sosig list
                                H3MP_Server.AddTrackedSosig(trackedSosig.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this item to global tracked items
                                trackedSosig.data.localWaitingIndex = H3MP_Client.localSosigCounter++;
                                H3MP_Client.waitingLocalSosigs.Add(trackedSosig.data.localWaitingIndex, trackedSosig.data);
                                H3MP_ClientSend.TrackedSosig(trackedSosig.data);
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

        private static H3MP_TrackedSosig MakeSosigTracked(Sosig sosigScript)
        {
            H3MP_TrackedSosig trackedSosig = sosigScript.gameObject.AddComponent<H3MP_TrackedSosig>();
            H3MP_TrackedSosigData data = new H3MP_TrackedSosigData();
            trackedSosig.data = data;
            data.physicalObject = trackedSosig;
            trackedSosig.physicalSosigScript = sosigScript;
            H3MP_GameManager.trackedSosigBySosig.Add(sosigScript, trackedSosig);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedSosig.awoken)
            {
                trackedSosig.data.Update(true);
            }

            data.configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
            data.configTemplate.AppliesDamageResistToIntegrityLoss = sosigScript.AppliesDamageResistToIntegrityLoss;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.TotalMustard = (float)typeof(Sosig).GetField("m_maxMustard", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.BleedDamageMult = sosigScript.BleedDamageMult;
            data.configTemplate.BleedRateMultiplier = sosigScript.BleedRateMult;
            data.configTemplate.BleedVFXIntensity = sosigScript.BleedVFXIntensity;
            data.configTemplate.SearchExtentsModifier = sosigScript.SearchExtentsModifier;
            data.configTemplate.ShudderThreshold = sosigScript.ShudderThreshold;
            data.configTemplate.ConfusionThreshold = (float)typeof(Sosig).GetField("ConfusionThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.ConfusionMultiplier = (float)typeof(Sosig).GetField("ConfusionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.ConfusionTimeMax = (float)typeof(Sosig).GetField("m_maxConfusedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.StunThreshold = (float)typeof(Sosig).GetField("StunThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.StunMultiplier = (float)typeof(Sosig).GetField("StunMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.StunTimeMax = (float)typeof(Sosig).GetField("m_maxStunTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.HasABrain = sosigScript.HasABrain;
            data.configTemplate.DoesDropWeaponsOnBallistic = sosigScript.DoesDropWeaponsOnBallistic;
            data.configTemplate.RegistersPassiveThreats = sosigScript.RegistersPassiveThreats;
            data.configTemplate.CanBeKnockedOut = sosigScript.CanBeKnockedOut;
            data.configTemplate.MaxUnconsciousTime = (float)typeof(Sosig).GetField("m_maxUnconsciousTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.AssaultPointOverridesSkirmishPointWhenFurtherThan = (float)typeof(Sosig).GetField("m_assaultPointOverridesSkirmishPointWhenFurtherThan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.ViewDistance = sosigScript.MaxSightRange;
            data.configTemplate.HearingDistance = sosigScript.MaxHearingRange;
            data.configTemplate.MaxFOV = sosigScript.MaxFOV;
            data.configTemplate.StateSightRangeMults = sosigScript.StateSightRangeMults;
            data.configTemplate.StateHearingRangeMults = sosigScript.StateHearingRangeMults;
            data.configTemplate.StateFOVMults = sosigScript.StateFOVMults;
            data.configTemplate.CanPickup_Ranged = sosigScript.CanPickup_Ranged;
            data.configTemplate.CanPickup_Melee = sosigScript.CanPickup_Melee;
            data.configTemplate.CanPickup_Other = sosigScript.CanPickup_Other;
            data.configTemplate.DoesJointBreakKill_Head = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesJointBreakKill_Upper = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesJointBreakKill_Lower = (bool)typeof(Sosig).GetField("m_doesJointBreakKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesSeverKill_Head = (bool)typeof(Sosig).GetField("m_doesSeverKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesSeverKill_Upper = (bool)typeof(Sosig).GetField("m_doesSeverKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesSeverKill_Lower = (bool)typeof(Sosig).GetField("m_doesSeverKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesExplodeKill_Head = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesExplodeKill_Upper = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Upper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.DoesExplodeKill_Lower = (bool)typeof(Sosig).GetField("m_doesExplodeKill_Lower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
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
            data.configTemplate.MaxJointLimit = (float)typeof(Sosig).GetField("m_maxJointLimit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript);
            data.configTemplate.OverrideSpeech = sosigScript.Speech;
            FieldInfo linkIntegrity = typeof(SosigLink).GetField("m_integrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            FieldInfo linkJointBroken = typeof(SosigLink).GetField("m_isJointBroken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            data.configTemplate.LinkDamageMultipliers = new List<float>();
            data.configTemplate.LinkStaggerMultipliers = new List<float>();
            data.configTemplate.StartingLinkIntegrity = new List<Vector2>();
            data.configTemplate.StartingChanceBrokenJoint = new List<float>();
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.configTemplate.LinkDamageMultipliers.Add(sosigScript.Links[i].DamMult);
                data.configTemplate.LinkStaggerMultipliers.Add(sosigScript.Links[i].StaggerMagnitude);
                float actualLinkIntegrity = (float)linkIntegrity.GetValue(sosigScript.Links[i]);
                data.configTemplate.StartingLinkIntegrity.Add(new Vector2(actualLinkIntegrity, actualLinkIntegrity));
                data.configTemplate.StartingChanceBrokenJoint.Add(((bool)linkJointBroken.GetValue(sosigScript.Links[i])) ? 1 : 0);
            }
            if (sosigScript.Priority != null)
            {
                data.configTemplate.TargetCapacity = (int)typeof(SosigTargetPrioritySystem).GetField("m_eventCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript.Priority); 
                data.configTemplate.TargetTrackingTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_maxTrackingTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript.Priority);
                data.configTemplate.NoFreshTargetTime = (float)typeof(SosigTargetPrioritySystem).GetField("m_timeToNoFreshTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sosigScript.Priority);
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
                    data.linkData[i][4] = (float)typeof(SosigLink).GetField("m_integrity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sosigScript.Links[i]);
                    data.linkIntegrity[i] = data.linkData[i][4];
                }
            }

            data.wearables = new List<List<string>>();
            FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < sosigScript.Links.Count; ++i)
            {
                data.wearables.Add(new List<string>());
                List<SosigWearable> sosigWearables = (List<SosigWearable>)wearablesField.GetValue(sosigScript.Links[i]);
                for (int j = 0; j < sosigWearables.Count; ++j)
                {
                    data.wearables[i].Add(sosigWearables[j].name);
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
            data.ammoStores = (int[])Mod.SosigInventory_m_ammoStores.GetValue(sosigScript.Inventory);
            data.controller = ID;
            data.mustard = sosigScript.Mustard;
            data.bodyPose = sosigScript.BodyPose;
            data.currentOrder = sosigScript.CurrentOrder;
            data.fallbackOrder = sosigScript.FallbackOrder;
            data.IFF = (byte)sosigScript.GetIFF();
            data.IFFChart = sosigScript.Priority.IFFChart;
            data.scene = H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;

            CollectExternalData(data);

            // Add to local list
            data.localTrackedID = sosigs.Count;
            sosigs.Add(data);

            return trackedSosig;
        }

        // MOD: This will be called upon tracking a new sosig
        //      From here you will be able to set specific data on the sosig
        //      For example, when we spawn sosigs in TNH we set flags so we know if they were in a patrol/holdpoint/supplypoint so we can set 
        //      this in the trackedSosigData
        //      Considering multiple mods may be writing data, a mod should probably add an int as an identifier for the data, which will
        //      be used to find the mod specific data in the data array
        private static void CollectExternalData(H3MP_TrackedSosigData trackedSosigData)
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
                SyncTrackedAutoMeaters(root.transform, init ? inControl : controlOverride, H3MP_GameManager.scene);
            }
        }

        public static void SyncTrackedAutoMeaters(Transform root, bool controlEverything, string scene)
        {
            AutoMeater autoMeaterScript = root.GetComponent<AutoMeater>();
            if (autoMeaterScript != null)
            {
                H3MP_TrackedAutoMeater trackedAutoMeater = root.GetComponent<H3MP_TrackedAutoMeater>();
                if (trackedAutoMeater == null)
                {
                    if (controlEverything)
                    {
                        trackedAutoMeater = MakeAutoMeaterTracked(autoMeaterScript);
                        if (trackedAutoMeater.awoken)
                        {
                            if (H3MP_ThreadManager.host)
                            {
                                // This will also send a packet with the AutoMeater to be added in the client's global AutoMeater list
                                H3MP_Server.AddTrackedAutoMeater(trackedAutoMeater.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this AutoMeater to global tracked AutoMeaters
                                trackedAutoMeater.data.localWaitingIndex = H3MP_Client.localAutoMeaterCounter++;
                                H3MP_Client.waitingLocalAutoMeaters.Add(trackedAutoMeater.data.localWaitingIndex, trackedAutoMeater.data);
                                H3MP_ClientSend.TrackedAutoMeater(trackedAutoMeater.data);
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

        private static H3MP_TrackedAutoMeater MakeAutoMeaterTracked(AutoMeater autoMeaterScript)
        {
            H3MP_TrackedAutoMeater trackedAutoMeater = autoMeaterScript.gameObject.AddComponent<H3MP_TrackedAutoMeater>();
            H3MP_TrackedAutoMeaterData data = new H3MP_TrackedAutoMeaterData();
            trackedAutoMeater.data = data;
            data.physicalObject = trackedAutoMeater;
            trackedAutoMeater.physicalAutoMeaterScript = autoMeaterScript;
            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Add(autoMeaterScript, trackedAutoMeater);

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedAutoMeater.awoken)
            {
                trackedAutoMeater.data.Update(true);
            }

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
            data.scene = H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;
            autoMeaters.Add(data);

            return trackedAutoMeater;
        }

        // MOD: This will be called upon tracking a new autoMeater
        //      From here you will be able to set specific data on the autoMeater
        //      For more info and example, take a look at CollectExternalData(H3MP_TrackedSosigData)
        private static void CollectExternalData(H3MP_TrackedAutoMeaterData trackedAutoMeaterData)
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
                SyncTrackedEncryptions(root.transform, init ? inControl : controlOverride, H3MP_GameManager.scene);
            }
        }

        public static void SyncTrackedEncryptions(Transform root, bool controlEverything, string scene)
        {
            TNH_EncryptionTarget encryption = root.GetComponent<TNH_EncryptionTarget>();
            if (encryption != null)
            {
                H3MP_TrackedEncryption currentTrackedEncryption = root.GetComponent<H3MP_TrackedEncryption>();
                if (currentTrackedEncryption == null)
                {
                    if (controlEverything)
                    {
                        H3MP_TrackedEncryption trackedEncryption = MakeEncryptionTracked(encryption);
                        if (trackedEncryption.awoken)
                        {
                            if (H3MP_ThreadManager.host)
                            {
                                // This will also send a packet with the Encryption to be added in the client's global item list
                                H3MP_Server.AddTrackedEncryption(trackedEncryption.data, 0);
                            }
                            else
                            {
                                // Tell the server we need to add this Encryption to global tracked Encryptions
                                trackedEncryption.data.localWaitingIndex = H3MP_Client.localEncryptionCounter++;
                                H3MP_Client.waitingLocalEncryptions.Add(trackedEncryption.data.localWaitingIndex, trackedEncryption.data);
                                H3MP_ClientSend.TrackedEncryption(trackedEncryption.data);
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

        private static H3MP_TrackedEncryption MakeEncryptionTracked(TNH_EncryptionTarget encryption)
        {
            H3MP_TrackedEncryption trackedEncryption = encryption.gameObject.AddComponent<H3MP_TrackedEncryption>();
            H3MP_TrackedEncryptionData data = new H3MP_TrackedEncryptionData();
            trackedEncryption.data = data;
            data.physicalObject = trackedEncryption;
            data.physicalObject.physicalEncryptionScript = encryption;

            // Call an init update because the one in awake won't be called because data was not set yet
            if (trackedEncryption.awoken)
            {
                trackedEncryption.data.Update(true);
            }

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

            data.controller = ID;

            // Add to local list
            data.localTrackedID = encryptions.Count;
            data.scene = H3MP_GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene;
            data.instance = instance;
            data.sceneInit = SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine || AnvilPrefabSpawnPatch.inInitPrefabSpawn || inPostSceneLoadTrack;
            encryptions.Add(data);

            return trackedEncryption;
        }

        public static H3MP_TNHInstance AddNewTNHInstance(int hostID, bool letPeopleJoin,
                                                         int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                                         int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                                         int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, string levelID)
        {
            if (H3MP_ThreadManager.host)
            {
                int freeInstance = 1; // Start at 1 because 0 is the default instance
                while (activeInstances.ContainsKey(freeInstance))
                {
                    ++freeInstance;
                }
                H3MP_TNHInstance newInstance = new H3MP_TNHInstance(freeInstance, hostID, letPeopleJoin,
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
                H3MP_ClientSend.AddTNHInstance(hostID, letPeopleJoin,
                                               progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                               targetModeSetting, AIDifficultyModifier, radarModeModifier,
                                               itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelID);

                return null;
            }
        }

        public static int AddNewInstance()
        {
            if (H3MP_ThreadManager.host)
            {
                int freeInstance = 1; // Start at 1 because 0 is the default instance
                while (activeInstances.ContainsKey(freeInstance))
                {
                    ++freeInstance;
                }

                activeInstances.Add(freeInstance, 0);

                Mod.modInstance.OnInstanceReceived(freeInstance);

                H3MP_ServerSend.AddInstance(freeInstance);

                return freeInstance;
            }
            else
            {
                H3MP_ClientSend.AddInstance();

                return -1;
            }
        }

        public static void AddTNHInstance(H3MP_TNHInstance instance)
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
            Mod.LogInfo("Changing instance from " + H3MP_GameManager.instance + " to " + instance);
            // Remove ourselves from the previous instance and manage dicts accordingly
            if (activeInstances.ContainsKey(H3MP_GameManager.instance))
            {
                --activeInstances[H3MP_GameManager.instance];
                if (activeInstances[H3MP_GameManager.instance] == 0)
                {
                    activeInstances.Remove(H3MP_GameManager.instance);
                }
            }
            else
            {
                Mod.LogError("Instance we are leaving is missing from active instances!");
            }
            if (TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.playerIDs.Remove(ID);

                if (currentInstance.playerIDs.Count == 0)
                {
                    TNHInstances.Remove(H3MP_GameManager.instance);

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

            if (playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances0))
            {
                if (relevantInstances0.TryGetValue(H3MP_GameManager.instance, out List<int> relevantPlayers))
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

            // Set locally
            H3MP_GameManager.instance = instance;

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
            bool bringItems = !H3MP_GameManager.playersByInstanceByScene.TryGetValue(sceneLoading ? LoadLevelBeginPatch.loadingLevel : H3MP_GameManager.scene, out Dictionary<int, List<int>> ci) ||
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
                H3MP_TrackedItemData[] itemArrToUse = null;
                H3MP_TrackedSosigData[] sosigArrToUse = null;
                H3MP_TrackedAutoMeaterData[] autoMeaterArrToUse = null;
                H3MP_TrackedEncryptionData[] encryptionArrToUse = null;
                if (H3MP_ThreadManager.host)
                {
                    itemArrToUse = H3MP_Server.items;
                    sosigArrToUse = H3MP_Server.sosigs;
                    autoMeaterArrToUse = H3MP_Server.autoMeaters;
                    encryptionArrToUse = H3MP_Server.encryptions;
                }
                else
                {
                    itemArrToUse = H3MP_Client.items;
                    sosigArrToUse = H3MP_Client.sosigs;
                    autoMeaterArrToUse = H3MP_Client.autoMeaters;
                    encryptionArrToUse = H3MP_Client.encryptions;
                }
                List<H3MP_TrackedItemData> filteredItems = new List<H3MP_TrackedItemData>();
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
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.DestroyItem(i, false);
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyItem(i, false);
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
                                SyncTrackedItems(go.transform, true, null, H3MP_GameManager.scene);
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

                List<H3MP_TrackedSosigData> filteredSosigs = new List<H3MP_TrackedSosigData>();
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
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.DestroySosig(i, false);
                        }
                        else
                        {
                            H3MP_ClientSend.DestroySosig(i, false);
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
                            SyncTrackedSosigs(go.transform, true, H3MP_GameManager.scene);
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

                List<H3MP_TrackedAutoMeaterData> filteredAutoMeaters = new List<H3MP_TrackedAutoMeaterData>();
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
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.DestroyAutoMeater(i, false);
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyAutoMeater(i, false);
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
                            SyncTrackedAutoMeaters(go.transform, true, H3MP_GameManager.scene);
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

                List<H3MP_TrackedEncryptionData> filteredEncryptions = new List<H3MP_TrackedEncryptionData>();
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
                        SyncTrackedEncryptions(go.transform, true, H3MP_GameManager.scene);
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
            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.PlayerInstance(0, instance);
            }
            else
            {
                H3MP_ClientSend.PlayerInstance(instance, sceneLoading);
            }

            // Set players active and playersPresent
            playersPresent = 0;
            string sceneName = H3MP_GameManager.scene;
            if (!nonSynchronizedScenes.ContainsKey(sceneName))
            {
                // Check each players scene/instance to know if they are in the one we are going into
                // TODO: Review: Since we are now tracking players' scene/instance through playerBySceneByInstance, we don't need to check for each of them here
                //       We should probably just use playersBySceneByInstance
                foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                {
                    if (player.Value.scene.Equals(sceneName) && player.Value.instance == instance)
                    {
                        ++playersPresent;

                        if (H3MP_ThreadManager.host && !sceneLoading)
                        {
                            // Request most up to date items from the client
                            // We do this because we may not have the most up to date version of items/sosigs since
                            // clients only send updated data to other players in their scene/instance
                            // But we need the most of to date data to instantiate the object
                            H3MP_ServerSend.RequestUpToDateObjects(player.Key, true, 0);
                        }
                    }

                    UpdatePlayerHidden(player.Value);
                }
            }
            else // New scene not syncable, ensure all players are disabled regardless of scene
            {
                foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                {
                    UpdatePlayerHidden(player.Value);
                }
            }
        }

        // MOD: When a client takes control of an item that is under our control, we will need to make sure that we are not 
        //      in control of the item anymore. If your mod patched IsControlled() then it should also patch this to ensure
        //      that the checks made in IsControlled() are false
        public static void EnsureUncontrolled(FVRPhysicalObject physObj)
        {
            if (physObj.m_hand != null)
            {
                physObj.ForceBreakInteraction();
            }
            if (physObj.QuickbeltSlot != null)
            {
                physObj.SetQuickBeltSlot(null);
            }
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

        // MOD: This will be called to check if the given physObj is controlled by this client
        //      This currently only checks if item is in a slot or is being held
        //      A mod can postfix this to change the return value if it wants to have control of items based on other criteria
        public static bool IsControlled(FVRPhysicalObject physObj)
        {
            return physObj.m_hand != null || physObj.QuickbeltSlot != null;
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
                    sceneAtSceneLoadStart = H3MP_GameManager.scene;
                }
                else
                {
                    H3MP_GameManager.scene = LoadLevelBeginPatch.loadingLevel;
                    sceneLoading = false;
                }
                return;
            }

            if (loading) // Just started loading
            {
                Mod.LogInfo("Switching scene, from " + H3MP_GameManager.scene + " to " + LoadLevelBeginPatch.loadingLevel);
                sceneLoading = true;
                instanceAtSceneLoadStart = instance;
                sceneAtSceneLoadStart = H3MP_GameManager.scene;

                ++giveControlOfDestroyed;

                ++Mod.skipAllInstantiates;

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
                        Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, ID, H3MP_ThreadManager.host);
                        Mod.currentlyPlayingTNH = false;
                    }
                    Mod.currentTNHInstance = null;
                    Mod.TNHSpectating = false;
                    Mod.currentlyPlayingTNH = false;
                }

                // Check if there are other players where we are going
                if (playersByInstanceByScene.TryGetValue(LoadLevelBeginPatch.loadingLevel, out Dictionary<int, List<int>> relevantInstances))
                {
                    if(relevantInstances.TryGetValue(instance, out List<int> relevantPlayers))
                    {
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
                Mod.LogInfo("Started loading, control override: " + controlOverride);

                if(playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> relevantInstances0))
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
            }
            else // Finished loading
            {
                H3MP_GameManager.scene = LoadLevelBeginPatch.loadingLevel;
                Mod.LogInfo("Arrived in scene: " + H3MP_GameManager.scene);
                sceneLoading = false;

                // Send an update to let others know we changed scene
                if (H3MP_ThreadManager.host)
                {
                    // Send the host's scene to clients
                    H3MP_ServerSend.PlayerScene(0, LoadLevelBeginPatch.loadingLevel);
                }
                else
                {
                    // Send to server, host will update and then send to all other clients
                    H3MP_ClientSend.PlayerScene(LoadLevelBeginPatch.loadingLevel);
                }

                --Mod.skipAllInstantiates;

                --giveControlOfDestroyed;

                // Update players' active state depending on which are in the same scene/instance
                playersPresent = 0;
                if (!nonSynchronizedScenes.ContainsKey(scene))
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                    {
                        if (player.Value.scene.Equals(scene) && player.Value.instance == instance)
                        {
                            ++playersPresent;

                            if (H3MP_ThreadManager.host)
                            {
                                // Request most up to date objects from the client
                                // We do this because we may not have the most up to date version of objects since
                                // clients only send updated data when there are others in their scene
                                // But we need the most of to date data to instantiate the object
                                Mod.LogInfo("Server sending request for up to date objects to " + player.Key);
                                H3MP_ServerSend.RequestUpToDateObjects(player.Key, true, 0);
                            }
                        }

                        UpdatePlayerHidden(player.Value);
                    }
                    Mod.LogInfo("Started loading, control override: " + controlOverride);

                    // Just arrived in syncable scene, sync items with server/clients
                    // NOTE THAT THIS IS DEPENDENT ON US HAVING UPDATED WHICH OTHER PLAYERS ARE VISIBLE LIKE WE DO IN THE ABOVE LOOP
                    inPostSceneLoadTrack = true;
                    SyncTrackedSosigs();
                    SyncTrackedAutoMeaters();
                    SyncTrackedItems();
                    SyncTrackedEncryptions();
                    inPostSceneLoadTrack = false;

                    controlOverride = false;

                    // Instantiate any objects we are in control of that don't have a phys yet
                    // This could happen if object control was given to us while we were loading
                    // Note that we check if the trackedID is initialized becaue we don't want to reinstantiate items that are unawoken
                    // which will not have a trackedID yet
                    // TODO: Review: This may not be a possible case anymore considering we only send our new scene once we are done loading, so no one is going to 
                    //               give us control because they didn't know we were coming here
                    for(int i=0; i < items.Count; ++i)
                    {
                        if (items[i].physicalItem == null && !items[i].awaitingInstantiation && items[i].trackedID > -1)
                        {
                            items[i].awaitingInstantiation = true;
                            AnvilManager.Run(items[i].Instantiate());
                        }
                    }
                    for(int i=0; i < sosigs.Count; ++i)
                    {
                        if (sosigs[i].physicalObject == null && !sosigs[i].awaitingInstantiation && sosigs[i].trackedID > -1)
                        {
                            sosigs[i].awaitingInstantiation = true;
                            AnvilManager.Run(sosigs[i].Instantiate());
                        }
                    }
                    for(int i=0; i < autoMeaters.Count; ++i)
                    {
                        if (autoMeaters[i].physicalObject == null && !autoMeaters[i].awaitingInstantiation && autoMeaters[i].trackedID > -1)
                        {
                            autoMeaters[i].awaitingInstantiation = true;
                            AnvilManager.Run(autoMeaters[i].Instantiate());
                        }
                    }
                    for(int i=0; i < encryptions.Count; ++i)
                    {
                        if (encryptions[i].physicalObject == null && !encryptions[i].awaitingInstantiation && encryptions[i].trackedID > -1)
                        {
                            encryptions[i].awaitingInstantiation = true;
                            AnvilManager.Run(encryptions[i].Instantiate());
                        }
                    }

                    // If client, tell server we are done loading
                    if (!H3MP_ThreadManager.host)
                    {
                        H3MP_ClientSend.DoneLoadingScene();
                    }
                }
                else // New scene not syncable, ensure all players are disabled regardless of scene
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                    {
                        UpdatePlayerHidden(player.Value);
                    }
                }

                // Clear any of our tracked items that may not exist anymore
                ClearUnawoken();
            }
        }

        public static void ClearUnawoken()
        {
            // Clear any tracked object that we are supposed to be controlling that doesn't have a physicalItem assigned
            // These can build up in certain cases. The main one is when we load into a level which contains items that are inactive by default
            // These items will never be awoken, they will therefore be tracked but not synced with other clients. When we leave the scene, these items 
            // may be destroyed but their OnDestroy will not be called because they were never awoken, meaning they will still be in the items list
            for(int i=0; i < items.Count; ++i)
            {
                if (items[i].physicalItem == null)
                {
                    items[i].RemoveFromLocal();
                }
            }
            for(int i=0; i < sosigs.Count; ++i)
            {
                if (sosigs[i].physicalObject == null)
                {
                    sosigs[i].RemoveFromLocal();
                }
            }
            for(int i=0; i < autoMeaters.Count; ++i)
            {
                if (autoMeaters[i].physicalObject == null)
                {
                    autoMeaters[i].RemoveFromLocal();
                }
            }
            for(int i=0; i < encryptions.Count; ++i)
            {
                if (encryptions[i].physicalObject == null)
                {
                    encryptions[i].RemoveFromLocal();
                }
            }
        }

        // MOD: If you want to process damage differently, you can patch this
        //      Meatov uses this to apply damage to specific limbs for example
        public static void ProcessPlayerDamage(H3MP_PlayerHitbox.Part part, Damage damage)
        {
            if (part == H3MP_PlayerHitbox.Part.Head)
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
            else if (part == H3MP_PlayerHitbox.Part.Torso)
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
                for (int i = 0; i < H3MP_Server.items.Length; ++i)
                {
                    if (H3MP_Server.items[i] != null && H3MP_Server.items[i].controller == clientID)
                    {
                        H3MP_TrackedItemData trackedItem = H3MP_Server.items[i];

                        bool destroyed = newController == -1;

                        if (destroyed) // No other player to take control, destroy
                        {
                            H3MP_ServerSend.DestroyItem(trackedItem.trackedID);
                            H3MP_Server.items[trackedItem.trackedID] = null;
                            H3MP_Server.availableItemIndices.Add(trackedItem.trackedID);
                            if (itemsByInstanceByScene.TryGetValue(trackedItem.scene, out Dictionary<int, List<int>> currentInstances) &&
                                currentInstances.TryGetValue(trackedItem.instance, out List<int> itemList))
                            {
                                itemList.Remove(trackedItem.trackedID);
                            }
                            trackedItem.awaitingInstantiation = false;
                        }
                        else if (newController == 0) // If its us, take control
                        {
                            trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem);
                            // Physical object could be null if we are given control while we are loading, the giving client will think we are in their scene/instance
                            if (trackedItem.physicalItem == null)
                            {
                                // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                                // Otherwise we send destroy order for the object
                                if (!H3MP_GameManager.sceneLoading)
                                {
                                    if (trackedItem.scene.Equals(H3MP_GameManager.scene) && trackedItem.instance == H3MP_GameManager.instance)
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
                                            // TrackedItem.OnDestroy will handle removal from relevant lists
                                            Destroy(trackedItem.physicalItem.gameObject);
                                        }
                                        else
                                        {
                                            H3MP_ServerSend.DestroyItem(trackedItem.trackedID);
                                            trackedItem.RemoveFromLocal();
                                            H3MP_Server.items[trackedItem.trackedID] = null;
                                            H3MP_Server.availableItemIndices.Add(trackedItem.trackedID);
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

                        if (!destroyed)
                        {
                            trackedItem.SetController(newController);

                            H3MP_ServerSend.GiveControl(trackedItem.trackedID, newController);
                        }
                    }
                }
            }

            // Give all sosigs
            for (int i = 0; i < H3MP_Server.sosigs.Length; ++i)
            {
                if (H3MP_Server.sosigs[i] != null && H3MP_Server.sosigs[i].controller == clientID)
                {
                    H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        H3MP_ServerSend.DestroySosig(trackedSosig.trackedID);
                        H3MP_Server.sosigs[trackedSosig.trackedID] = null;
                        H3MP_Server.availableSosigIndices.Add(trackedSosig.trackedID);
                        if (sosigsByInstanceByScene.TryGetValue(trackedSosig.scene, out Dictionary<int, List<int>> currentInstances) &&
                            currentInstances.TryGetValue(trackedSosig.instance, out List<int> sosigList))
                        {
                            sosigList.Remove(trackedSosig.trackedID);
                        }
                        trackedSosig.awaitingInstantiation = false;
                    }
                    else if (newController == 0) // If its us, take control
                    {
                        trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                        H3MP_GameManager.sosigs.Add(trackedSosig);
                        if (trackedSosig.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!H3MP_GameManager.sceneLoading)
                            {
                                if (trackedSosig.scene.Equals(H3MP_GameManager.scene) && trackedSosig.instance == H3MP_GameManager.instance)
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
                                        H3MP_ServerSend.DestroySosig(trackedSosig.trackedID);
                                        trackedSosig.RemoveFromLocal();
                                        H3MP_Server.sosigs[trackedSosig.trackedID] = null;
                                        H3MP_Server.availableSosigIndices.Add(trackedSosig.trackedID);
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

                    if (!destroyed)
                    {
                        trackedSosig.controller = newController;

                        H3MP_ServerSend.GiveSosigControl(trackedSosig.trackedID, newController);
                    }
                }
            }

            // Give all automeaters
            for (int i = 0; i < H3MP_Server.autoMeaters.Length; ++i)
            {
                if (H3MP_Server.autoMeaters[i] != null && H3MP_Server.autoMeaters[i].controller == clientID)
                {
                    H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        H3MP_ServerSend.DestroyAutoMeater(trackedAutoMeater.trackedID);
                        H3MP_Server.autoMeaters[trackedAutoMeater.trackedID] = null;
                        H3MP_Server.availableAutoMeaterIndices.Add(trackedAutoMeater.trackedID);
                        if (autoMeatersByInstanceByScene.TryGetValue(trackedAutoMeater.scene, out Dictionary<int, List<int>> currentInstances) &&
                            currentInstances.TryGetValue(trackedAutoMeater.instance, out List<int> autoMeaterList))
                        {
                            autoMeaterList.Remove(trackedAutoMeater.trackedID);
                        }
                        trackedAutoMeater.awaitingInstantiation = false;
                    }
                    else if (newController == 0) // If its us, take control
                    {
                        trackedAutoMeater.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                        H3MP_GameManager.autoMeaters.Add(trackedAutoMeater);
                        if (trackedAutoMeater.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!H3MP_GameManager.sceneLoading)
                            {
                                if (trackedAutoMeater.scene.Equals(H3MP_GameManager.scene) && trackedAutoMeater.instance == H3MP_GameManager.instance)
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
                                        H3MP_ServerSend.DestroyAutoMeater(trackedAutoMeater.trackedID);
                                        trackedAutoMeater.RemoveFromLocal();
                                        H3MP_Server.autoMeaters[trackedAutoMeater.trackedID] = null;
                                        H3MP_Server.availableAutoMeaterIndices.Add(trackedAutoMeater.trackedID);
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

                    if (!destroyed)
                    {
                        trackedAutoMeater.controller = newController;

                        H3MP_ServerSend.GiveAutoMeaterControl(trackedAutoMeater.trackedID, newController);
                    }
                }
            }

            // Give all encryptions
            for (int i = 0; i < H3MP_Server.encryptions.Length; ++i)
            {
                if (H3MP_Server.encryptions[i] != null && H3MP_Server.encryptions[i].controller == clientID)
                {
                    H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        H3MP_ServerSend.DestroyEncryption(trackedEncryption.trackedID);
                        H3MP_Server.encryptions[trackedEncryption.trackedID] = null;
                        H3MP_Server.availableEncryptionIndices.Add(trackedEncryption.trackedID);
                        if (encryptionsByInstanceByScene.TryGetValue(trackedEncryption.scene, out Dictionary<int, List<int>> currentInstances) &&
                            currentInstances.TryGetValue(trackedEncryption.instance, out List<int> encryptionList))
                        {
                            encryptionList.Remove(trackedEncryption.trackedID);
                        }
                        trackedEncryption.awaitingInstantiation = false;
                    }
                    else if (newController == 0)
                    {
                        trackedEncryption.localTrackedID = H3MP_GameManager.encryptions.Count;
                        H3MP_GameManager.encryptions.Add(trackedEncryption);
                        if (trackedEncryption.physicalObject == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!H3MP_GameManager.sceneLoading)
                            {
                                if (trackedEncryption.scene.Equals(H3MP_GameManager.scene) && trackedEncryption.instance == H3MP_GameManager.instance)
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
                                        H3MP_ServerSend.DestroyEncryption(trackedEncryption.trackedID);
                                        trackedEncryption.RemoveFromLocal();
                                        H3MP_Server.encryptions[trackedEncryption.trackedID] = null;
                                        H3MP_Server.availableEncryptionIndices.Add(trackedEncryption.trackedID);
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

                        H3MP_ServerSend.GiveEncryptionControl(trackedEncryption.trackedID, newController);
                    }
                }
            }
        }

        public static void TakeAllPhysicalControl(bool destroyTrackedScript)
        {
            H3MP_TrackedItemData[] itemArrToUse = null;
            H3MP_TrackedSosigData[] sosigArrToUse = null;
            H3MP_TrackedAutoMeaterData[] autoMeaterArrToUse = null;
            H3MP_TrackedEncryptionData[] encryptionArrToUse = null;
            if (H3MP_ThreadManager.host)
            {
                itemArrToUse = H3MP_Server.items;
                sosigArrToUse = H3MP_Server.sosigs;
                autoMeaterArrToUse = H3MP_Server.autoMeaters;
                encryptionArrToUse = H3MP_Server.encryptions;
            }
            else
            {
                itemArrToUse = H3MP_Client.items;
                sosigArrToUse = H3MP_Client.sosigs;
                autoMeaterArrToUse = H3MP_Client.autoMeaters;
                encryptionArrToUse = H3MP_Client.encryptions;
            }

            Mod.LogInfo("Taking all physical control, destroying item scripts? : "+ destroyTrackedScript);
            foreach (H3MP_TrackedItemData item in itemArrToUse)
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

            foreach (H3MP_TrackedSosigData sosig in sosigArrToUse)
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

            foreach (H3MP_TrackedAutoMeaterData autoMeater in autoMeaterArrToUse)
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

            foreach (H3MP_TrackedEncryptionData encryption in encryptionArrToUse)
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
