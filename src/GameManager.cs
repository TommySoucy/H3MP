using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        public static List<TrackedObjectData> objects = new List<TrackedObjectData>(); // Tracked objects under control of this gameManager
        public static Dictionary<MonoBehaviour, TrackedObject> trackedObjectByObject = new Dictionary<MonoBehaviour, TrackedObject>();
        public static Dictionary<FVRInteractiveObject, TrackedObject> trackedObjectByInteractive = new Dictionary<FVRInteractiveObject, TrackedObject>();
        public static Dictionary<IFVRDamageable, TrackedObject> trackedObjectByDamageable = new Dictionary<IFVRDamageable, TrackedObject>();
        public static Dictionary<UberShatterable, TrackedObject> trackedObjectByShatterable = new Dictionary<UberShatterable, TrackedObject>();
        public static Dictionary<FVRPhysicalObject, TrackedItem> trackedItemByItem = new Dictionary<FVRPhysicalObject, TrackedItem>();
        public static Dictionary<SosigWeapon, TrackedItem> trackedItemBySosigWeapon = new Dictionary<SosigWeapon, TrackedItem>();
        public static Dictionary<Sosig, TrackedSosig> trackedSosigBySosig = new Dictionary<Sosig, TrackedSosig>();
        public static Dictionary<AutoMeater, TrackedAutoMeater> trackedAutoMeaterByAutoMeater = new Dictionary<AutoMeater, TrackedAutoMeater>();
        public static Dictionary<TNH_EncryptionTarget, TrackedEncryption> trackedEncryptionByEncryption = new Dictionary<TNH_EncryptionTarget, TrackedEncryption>();
        public static Dictionary<BreakableGlass, TrackedBreakableGlass> trackedBreakableGlassByBreakableGlass = new Dictionary<BreakableGlass, TrackedBreakableGlass>();
        public static Dictionary<BreakableGlassDamager, TrackedBreakableGlass> trackedBreakableGlassByBreakableGlassDamager = new Dictionary<BreakableGlassDamager, TrackedBreakableGlass>();
        public static Dictionary<wwGatlingGun, TrackedGatlingGun> trackedGatlingGunByGatlingGun = new Dictionary<wwGatlingGun, TrackedGatlingGun>();
        public static Dictionary<BrutBlockSystem, TrackedBrutBlockSystem> trackedBrutBlockSystemByBrutBlockSystem = new Dictionary<BrutBlockSystem, TrackedBrutBlockSystem>();
        public static Dictionary<Construct_Blister, TrackedBlister> trackedBlisterByBlister = new Dictionary<Construct_Blister, TrackedBlister>();
        public static Dictionary<Construct_Floater, TrackedFloater> trackedFloaterByFloater = new Dictionary<Construct_Floater, TrackedFloater>();
        public static Dictionary<Construct_Iris, TrackedIris> trackedIrisByIris = new Dictionary<Construct_Iris, TrackedIris>();
        public static Dictionary<int, int> activeInstances = new Dictionary<int, int>();
        public static Dictionary<int, TNHInstance> TNHInstances = new Dictionary<int, TNHInstance>();
        public static List<int> playersAtLoadStart;
        public static Dictionary<string, Dictionary<int, List<int>>> playersByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static Dictionary<string, Dictionary<int, List<int>>> objectsByInstanceByScene = new Dictionary<string, Dictionary<int, List<int>>>();
        public static List<GameObject> retrack = new List<GameObject>();
        public static bool spectatorHost;
        public static List<int> spectatorHosts = new List<int>(); // List of all spectator hosts, not necessarily available 
        public static int controlledSpectatorHost = -1;
        public static int spectatorHostControlledBy = -1;
        public static bool resetSpectatorHost;
        public static bool instanceBringItems;
        public static long ping = -1;
        public static int reconnectionInstance = -1;

        /// <summary>
        /// CUSTOMIZATION
        /// A dictionary of all scenes we do not want to sychronize
        /// Key: Name of the scene
        /// Value: Irrelevant
        /// </summary>
        public static Dictionary<string, byte> nonSynchronizedScenes = new Dictionary<string, byte>();

        public static int giveControlOfDestroyed;
        public static bool controlOverride;
        public static bool firstPlayerInSceneInstance;
        public static bool dontAddToInstance;
        public static bool inPostSceneLoadTrack;

        public static int ID = 0;
        public static Vector3 torsoOffset = new Vector3(0, -0.4f, 0);
        public static Vector3 overheadDisplayOffset = new Vector3(0, 0.25f, 0);
        public static List<int> playersPresent = new List<int>();
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
        public static string playerPrefabID = "None";
        public static int playerPrefabIndex = -1;
        public static TrackedPlayerBody currentTrackedPlayerBody;
        public static bool playerModelAwaitingInstantiation = false;
        public static PlayerBody currentPlayerBody = null;
        public static bool bodyVisible = true;
        public static bool handsVisible = true;

        /// <summary>
        /// CUSTOMIZATION
        /// A dictionary of all player prefabs. If you add a player prefab, its ID must also be added to playerPrefabIDs
        /// Key: Identifier for the prefab
        /// Value: The prefab
        /// </summary>
        public static Dictionary<string, UnityEngine.Object> playerPrefabs = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// CUSTOMIZATION
        /// A list of all player prefab IDs. Must contain IDs of all player prefabs added to playerPrefabs
        /// </summary>
        public static List<string> playerPrefabIDs = new List<string>();

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnUpdatePlayerHidden event
        /// </summary>
        /// <param name="player">The player we want to update visibility of</param>
        /// <param name="visible">Custom override for whether should be visible or not</param>
        public delegate void OnUpdatePlayerHiddenDelegate(PlayerManager player, ref bool visible);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we update a player's visibility
        /// </summary>
        public static event OnUpdatePlayerHiddenDelegate OnUpdatePlayerHidden;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnPlayerDamage event
        /// </summary>
        /// <param name="damageMultiplier">The damage multiplier</param>
        /// <param name="damage">The damage</param>
        /// <param name="processDamage">Custom override for whether H3MP should process the damage itself</param>
        public delegate void OnPlayerDamageDelegate(float damageMultiplier, Damage damage, ref bool processDamage);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when the player takes damage
        /// </summary>
        public static event OnPlayerDamageDelegate OnPlayerDamage;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSpectatorHostsChanged event
        /// </summary>
        public delegate void OnSpectatorHostsChangedDelegate();

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when the list of spectator hosts changes
        /// </summary>
        public static event OnSpectatorHostsChangedDelegate OnSpectatorHostsChanged;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSpectatorHostToggled event
        /// </summary>
        /// <param name="spectatorHost">The new value</param>
        public delegate void OnSpectatorHostToggledDelegate(bool spectatorHost);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we toggle between being a spectator host or not
        /// </summary>
        public static event OnSpectatorHostToggledDelegate OnSpectatorHostToggled;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnInstanceLeft event
        /// </summary>
        /// <param name="instance">The instance we left</param>
        /// <param name="destination">The destination instance</param>
        public delegate void OnInstanceLeftDelegate(int instance, int destination);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we leave an instance
        /// </summary>
        public static event OnInstanceLeftDelegate OnInstanceLeft;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnInstanceJoined event
        /// </summary>
        /// <param name="instance">The instance we joined</param>
        /// <param name="source">The source instance we come from</param>
        public delegate void OnInstanceJoinedDelegate(int instance, int source);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we join an instance
        /// </summary>
        public static event OnInstanceJoinedDelegate OnInstanceJoined;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSceneLeft event
        /// </summary>
        /// <param name="scene">The scene that we left</param>
        /// <param name="destination">The destination scene</param>
        public delegate void OnSceneLeftDelegate(string scene, string destination);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a scene begins loading
        /// </summary>
        public static event OnSceneLeftDelegate OnSceneLeft;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSceneJoined event
        /// </summary>
        /// <param name="scene">The scene that we joined</param>
        /// <param name="source">The source scene we come from</param>
        public delegate void OnSceneJoinedDelegate(string scene, string source);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a scene is done loading
        /// </summary>
        public static event OnSceneJoinedDelegate OnSceneJoined;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnPlayerBodyInit event
        /// </summary>
        /// <param name="playerBody">The FVRPlayerBody that got initialized</param>
        public delegate void OnPlayerBodyInitDelegate(FVRPlayerBody playerBody);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when FVRPlayerBody.Init is called
        /// </summary>
        public static event OnPlayerBodyInitDelegate OnPlayerBodyInit;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnPlayerAdded event
        /// </summary>
        /// <param name="player">The PlayerManager of the player that just got added</param>
        public delegate void OnPlayerAddedDelegate(PlayerManager player);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a new player gets added to Players dictionary
        /// </summary>
        public static event OnPlayerAddedDelegate OnPlayerAdded;

        private void Awake()
        {
            singleton = this;

            // Init the main instance
            activeInstances.Add(instance, 1);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6) && !sceneLoading)
            {
                spectatorHost = !spectatorHost;

                if (GM.CurrentPlayerBody != null)
                {
                    GM.CurrentPlayerBody.EyeCam.enabled = !spectatorHost;
                }

                if (ThreadManager.host)
                {
                    if (spectatorHost)
                    {
                        spectatorHosts.Add(0);
                        Server.availableSpectatorHosts.Add(0);
                    }
                    else
                    {
                        spectatorHosts.Remove(0);
                        Server.availableSpectatorHosts.Remove(0);
                    }
                    ServerSend.SpectatorHost(0, spectatorHost);
                }
                else
                {
                    spectatorHosts.Add(ID);
                    ClientSend.SpectatorHost(spectatorHost);
                }

                if (spectatorHost)
                {
                    Mod.LogWarning("Player is now a spectator host!");

                    if (!scene.Equals("MainMenu3"))
                    {
                        SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                    }
                }
                else
                {
                    Mod.LogWarning("Player is no longer a spectator host!");
                    spectatorHostControlledBy = -1;
                    Mod.spectatorHostWaitingForTNHSetup = false;
                }

                if(OnSpectatorHostToggled != null)
                {
                    OnSpectatorHostToggled(spectatorHost);
                }
            }
        }

        public static void OnSpectatorHostsChangedInvoke()
        {
            if(OnSpectatorHostsChanged != null)
            {
                OnSpectatorHostsChanged();
            }

            // TODO: Update UI of TNH menu
        }

        public void SpawnPlayer(int ID, string username, string scene, int instance, int IFF, int colorIndex, bool join = false)
        {
            // We dont want to spawn the local player as we will already have spawned when connecting to a server
            if (Client.singleton != null && ID == Client.singleton.ID)
            {
                return;
            }

            Mod.LogInfo($"Spawn player called with ID: {ID}", false);

            GameObject playerRoot = new GameObject("PlayerRoot" + ID);
            DontDestroyOnLoad(playerRoot);
            PlayerManager playerManager = playerRoot.AddComponent<PlayerManager>();
            playerManager.Init(ID, username, scene, instance, IFF, colorIndex);
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
            if (!nonSynchronizedScenes.ContainsKey(scene) && scene.Equals(GameManager.scene) && instance == GameManager.instance)
            {
                playersPresent.Add(ID);

                if (join)
                {
                    // This is a spawn player order from the server since we just joined it, we are not first in the scene
                    GameManager.firstPlayerInSceneInstance = false;
                }
            }

            if(OnPlayerAdded != null)
            {
                OnPlayerAdded(playerManager);
            }
        }

        public static IEnumerator InstantiatePlayerBody(FVRObject playerModelFVRObject, string playerPrefabID)
        {
            GameObject prefab = null;

            yield return playerModelFVRObject.GetGameObjectAsync();
            prefab = playerModelFVRObject.GetGameObject();

            if (prefab == null)
            {
                Mod.LogWarning("Attempted to instantiate player model \""+ playerPrefabID + "\" but failed to get prefab. Using Default instead.");
                GameManager.playerPrefabID = "Default";
            }

            currentPlayerBody = Instantiate(prefab).GetComponent<PlayerBody>();
            playerModelAwaitingInstantiation = false;
        }

        public static void Reset()
        {
            foreach(KeyValuePair<int, PlayerManager> playerEntry in players)
            {
                Destroy(playerEntry.Value.gameObject);
            }
            players.Clear();
            objects.Clear();
            spectatorHosts.Clear();
            trackedObjectByObject.Clear();
            trackedObjectByInteractive.Clear();
            trackedObjectByDamageable.Clear();
            trackedItemByItem.Clear();
            trackedSosigBySosig.Clear();
            trackedItemBySosigWeapon.Clear();
            trackedAutoMeaterByAutoMeater.Clear();
            trackedEncryptionByEncryption.Clear();
            trackedBreakableGlassByBreakableGlass.Clear();
            trackedBreakableGlassByBreakableGlassDamager.Clear();
            trackedBlisterByBlister.Clear();
            trackedFloaterByFloater.Clear();
            trackedGatlingGunByGatlingGun.Clear();
            trackedBrutBlockSystemByBrutBlockSystem.Clear();
            trackedIrisByIris.Clear();
            trackedObjectByShatterable.Clear();
            if (playersAtLoadStart != null)
            {
                playersAtLoadStart.Clear();
            }
            activeInstances.Clear();
            TNHInstances.Clear();
            playersByInstanceByScene.Clear();
            objectsByInstanceByScene.Clear();
            ID = 0;
            instance = 0;
            giveControlOfDestroyed = 0;
            controlOverride = false;
            firstPlayerInSceneInstance = false;
            dontAddToInstance = false;
            playersPresent.Clear();
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
            spectatorHost = false;
            GM.CurrentPlayerBody.EyeCam.enabled = true;
            spectatorHostControlledBy = -1;
            controlledSpectatorHost = -1;

            for (int i=0; i< TrackedObject.trackedReferenceObjects.Length; ++i)
            {
                if (TrackedObject.trackedReferenceObjects[i] != null)
                {
                    Destroy(TrackedObject.trackedReferenceObjects[i]);
                }
            }
            TrackedObject.trackedReferenceObjects = new GameObject[100];
            TrackedObject.trackedReferences = new TrackedObject[100];
            TrackedObject.availableTrackedRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                            10,11,12,13,14,15,16,17,18,19,
                                                                            20,21,22,23,24,25,26,27,28,29,
                                                                            30,31,32,33,34,35,36,37,38,39,
                                                                            40,41,42,43,44,45,46,47,48,49,
                                                                            50,51,52,53,54,55,56,57,58,59,
                                                                            60,61,62,63,64,65,66,67,68,69,
                                                                            70,71,72,73,74,75,76,77,78,79,
                                                                            80,81,82,83,84,85,86,87,88,89,
                                                                            90,91,92,93,94,95,96,97,98,99};

            TrackedObject.unknownTrackedIDs.Clear();
            TrackedObject.unknownParentWaitList.Clear();
            TrackedObject.unknownControlTrackedIDs.Clear();
            TrackedObject.unknownDestroyTrackedIDs.Clear();
            TrackedObject.unknownParentTrackedIDs.Clear();

            TrackedItem.unknownCrateHolding.Clear();
            TrackedItem.unknownSosigInventoryItems.Clear();
            TrackedItem.unknownSosigInventoryObjects.Clear();
            TrackedItem.unknownGasCuboidGout.Clear();
            TrackedItem.unknownGasCuboidDamageHandle.Clear();

            TrackedSosig.unknownBodyStates.Clear();
            TrackedSosig.unknownIFFChart.Clear();
            TrackedSosig.unknownItemInteract.Clear();
            TrackedSosig.unknownSetIFFs.Clear();
            TrackedSosig.unknownSetOriginalIFFs.Clear();
            TrackedSosig.unknownTNHKills.Clear();
            TrackedSosig.unknownCurrentOrder.Clear();
            TrackedSosig.unknownConfiguration.Clear();
            TrackedSosig.unknownWearable.Clear();

            TrackedEncryption.unknownDisableSubTarg.Clear();
            TrackedEncryption.unknownInit.Clear();
            TrackedEncryption.unknownResetGrowth.Clear();
            TrackedEncryption.unknownSpawnGrowth.Clear();
            TrackedEncryption.unknownSpawnSubTarg.Clear();
            TrackedEncryption.unknownSpawnSubTargGeo.Clear();
            TrackedEncryption.unknownUpdateDisplay.Clear();

            TrackedFloater.unknownFloaterBeginExploding.Clear();
            TrackedFloater.unknownFloaterBeginDefusing.Clear();
            TrackedFloater.unknownFloaterExplode.Clear();

            TrackedIris.unknownIrisShatter.Clear();
            TrackedIris.unknownIrisSetState.Clear();
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

            player.EnqueuePositionData(position, rotation, headPos, headRot, torsoPos, torsoRot, leftHandPos,
                leftHandRot, rightHandPos, rightHandRot);


            float previousHealth = player.health;
            player.health = health;
            int previousMaxHealth = player.maxHealth;
            player.maxHealth = maxHealth;
            if(player.playerBody != null && (health != previousHealth || maxHealth != previousMaxHealth))
            {
                player.playerBody.UpdateHealthLabel();
            }

            if((health <= 0 && player.visible) || (health > 0 && !player.visible))
            {
                UpdatePlayerHidden(player);
            }
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

            if (player.scene.Equals(GameManager.scene) && !nonSynchronizedScenes.ContainsKey(player.scene) && instance == player.instance)
            {
                playersPresent.Remove(playerID);
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

            if (sceneName.Equals(GameManager.scene) && !nonSynchronizedScenes.ContainsKey(sceneName) && instance == player.instance)
            {
                playersPresent.Add(playerID);
            }
        }

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

            if (OnUpdatePlayerHidden != null)
            {
                OnUpdatePlayerHidden(player, ref visible);
            }

            // If have not found a reason for player to be hidden, set as visible
            player.SetVisible(visible);
            return visible;
        }

        public static void UpdatePlayerInstance(int playerID, int instance)
        {
            Mod.LogInfo("Player " + playerID + " joining instance " + instance, false);
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
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                        {
                            GM.CurrentPlayerBody.EnableHands();
                        }
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

            if (player.scene.Equals(GameManager.scene) && !nonSynchronizedScenes.ContainsKey(player.scene) && GameManager.instance == player.instance)
            {
                playersPresent.Remove(playerID);
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

            if (player.scene.Equals(GameManager.scene) && !nonSynchronizedScenes.ContainsKey(player.scene) && GameManager.instance == player.instance)
            {
                playersPresent.Add(playerID);
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
            if(Mod.managerObject == null)
            {
                colorIndex = index;
                if (BodyWristMenuSection.colorText != null)
                {
                    BodyWristMenuSection.colorText.text = "Current color: " + colorNames[colorIndex];
                }

                return;
            }

            if(GameManager.ID == ID)
            {
                colorIndex = index;

                if(currentPlayerBody != null)
                {
                    currentPlayerBody.SetColor(colors[colorIndex]);
                }

                if(BodyWristMenuSection.colorText != null)
                {
                    BodyWristMenuSection.colorText.text = "Current color: " + colorNames[colorIndex];
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

        public static void SetPlayerPrefab(string prefabID)
        {
            // Note: If we are here, it is implied that the new prefabID is different than the current one
            string previous = playerPrefabID;
            if (playerPrefabs.ContainsKey(prefabID))
            {
                playerPrefabID = prefabID;
            }
            else // We don't have this player prefab installed, use default
            {
                if(Mod.managerObject == null)
                {
                    playerPrefabID = "None";
                }
                else
                {
                    playerPrefabID = "Default";
                }
            }

            if (previous.Equals(playerPrefabID) && currentPlayerBody != null)
            {
                return;
            }

            if(currentPlayerBody != null)
            {
                Destroy(currentPlayerBody.gameObject);
            }

            if (playerPrefabID.Equals("None"))
            {
                return;
            }

            if (playerPrefabID.Equals("Default"))
            {
                currentPlayerBody = Instantiate(playerPrefabs["Default"] as GameObject).GetComponent<PlayerBody>();
            }
            else // Not default
            {
                // Note: If we are here, it is implied that this player prefab ID is registered, but we have to get it anyway, so might as well ensure we can get it
                bool gotPrefab = false;
                if (playerPrefabs.TryGetValue(playerPrefabID, out UnityEngine.Object prefabObject))
                {
                    if (prefabObject == null)
                    {
                        if (IM.OD.TryGetValue(playerPrefabID, out FVRObject prefabFVRObject) && prefabFVRObject != null)
                        {
                            gotPrefab = true;
                            playerModelAwaitingInstantiation = true;
                            AnvilManager.Run(InstantiatePlayerBody(prefabFVRObject, playerPrefabID));
                        }
                    }
                    else if (prefabObject is FVRObject)
                    {
                        gotPrefab = true;
                        playerModelAwaitingInstantiation = true;
                        AnvilManager.Run(InstantiatePlayerBody(prefabObject as FVRObject, playerPrefabID));
                    }
                }

                if (!gotPrefab)
                {
                    Mod.LogWarning("Attempt to set player prefab to \""+ playerPrefabID+"\" failed, could not obtain prefab, using default");
                    playerPrefabID = "Default";
                    currentPlayerBody = Instantiate(playerPrefabs[playerPrefabID] as GameObject).GetComponent<PlayerBody>();
                }
            }

            // Set option text
            if (BodyWristMenuSection.playerBodyText != null)
            {
                BodyWristMenuSection.playerBodyText.text = "Body: " + playerPrefabID;
            }
        }

        /// <summary>
        /// Registers a new player prefab ID for H3MP to use
        /// </summary>
        /// <param name="newPlayerPrefabID">The new ID to register</param>
        public static void AddPlayerPrefabID(string newPlayerPrefabID)
        {
            if (newPlayerPrefabID.Equals("Default"))
            {
                Mod.LogError("A mod attempted to register \"Default\" player prefab ID:\n"+Environment.StackTrace);
            }
            else
            {
                playerPrefabs.Add(newPlayerPrefabID, null);
                playerPrefabIDs.Add(newPlayerPrefabID);
            }
        }

        public static void SyncTrackedObjects(bool init = false, bool inControl = false)
        {
            // Return right away if haven't received connect sync yet
            if (!ThreadManager.host && !Client.singleton.gotConnectSync)
            {
                return;
            }

            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other items get destroyed. We will receive any item that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach(GameObject root in roots)
            {
                SyncTrackedObjects(root.transform, init ? inControl : controlOverride, null);
            }

            // Track anything in DontDestroyOnLoad
            if (Mod.DDOLScene != null)
            {
                roots = Mod.DDOLScene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    SyncTrackedObjects(root.transform, init ? inControl : controlOverride, null);
                }
            }
        }

        public static void SyncTrackedObjects(Transform root, bool controlEverything, TrackedObjectData parent)
        {
            // Return right away if haven't received connect sync yet
            if (!ThreadManager.host && !Client.singleton.gotConnectSync)
            {
                return;
            }

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
                                    Mod.LogInfo("Server made new object tracked with type ID: "+trackedObject.data.typeIdentifier,false);
                                    // This will also send a packet with the object to be added in the client's global item list
                                    Server.AddTrackedObject(trackedObject.data, 0);
                                }
                                else
                                {
                                    // Tell the server we need to add this item to global tracked objects
                                    trackedObject.data.localWaitingIndex = Client.localObjectCounter++;
                                    Mod.LogInfo("Client made new object tracked with local waiting index: " + trackedObject.data.localWaitingIndex+ " and type ID: "+trackedObject.data.typeIdentifier, false);
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
                                SyncTrackedObjects(child, controlEverything, trackedObject.data);
                            }
                        }
                    }
                    else if(TrackSkipped(root, trackedObjectType))
                    { 
                        // Item will not be controlled by us but is an item that should be tracked by system, so destroy it
                        Destroy(root.gameObject);
                    }
                }
                else if (parent != null) // Already tracked but a parent was passed, add to parent's children list
                {
                    parent.children.Add(currentTrackedObject.data);
                }
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedObjects(child, controlEverything, parent);
                }
            }
        }

        private static TrackedObject MakeObjectTracked(Transform root, TrackedObjectData parent, Type trackedObjectType)
        {
            Mod.LogInfo("Making tracked object from "+root.name, false);
            return (TrackedObject)trackedObjectType.GetMethod("MakeTracked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { root, parent });
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
                    instanceButton.Button.onClick.AddListener(() => { Mod.OnTNHInstanceClicked(freeInstance); });

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
                instanceButton.Button.onClick.AddListener(() => { Mod.OnTNHInstanceClicked(instance.instance); });

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
            Mod.LogInfo("Changing instance from " + GameManager.instance + " to " + instance+":\n"+Environment.StackTrace, false);
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

            // Called instance left event
            if(OnInstanceLeft != null)
            {
                OnInstanceLeft(GameManager.instance, instance);
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
                    if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                    {
                        GM.CurrentPlayerBody.EnableHands();
                    }
                }

                // Remove from currently playing and dead if necessary
                currentInstance.currentlyPlaying.Remove(ID);
                currentInstance.dead.Remove(ID);

                // If we are leaving the spectator host we control in the game but there are other players still
                if(controlledSpectatorHost != -1 && currentInstance.currentlyPlaying.Count > 1 && currentInstance.currentlyPlaying.Contains(controlledSpectatorHost))
                {
                    // We want to give control of the spectator host to another of the playing players
                    if (ThreadManager.host)
                    {
                        ReassignSpectatorHost(0, new List<int>() { 0 });
                    }
                    else
                    {
                        ClientSend.ReassignSpectatorHost(new List<int>() { ID });
                    }

                    Mod.OnSpectatorHostGiveUpInvoke();
                }
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
            int previousInstance = GameManager.instance;
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

            instanceBringItems = !GameManager.playersByInstanceByScene.TryGetValue(sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene, out Dictionary<int, List<int>> ci)
                                 || !ci.TryGetValue(instance, out List<int> op) || op.Count == 0;

            // Called instance joined event
            if (OnInstanceJoined != null)
            {
                OnInstanceJoined(instance, previousInstance);
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
            if (sceneLoading)
            {
                controlOverride = instanceBringItems;
                firstPlayerInSceneInstance = instanceBringItems;
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
            playersPresent.Clear();
            if (!nonSynchronizedScenes.ContainsKey(scene))
            {
                // Check each players scene/instance to know if they are in the one we are going into
                // TODO: Review: Since we are now tracking players' scene/instance through playerBySceneByInstance, we don't need to check for each of them here
                //       We should probably just use playersBySceneByInstance
                foreach (KeyValuePair<int, PlayerManager> player in players)
                {
                    if (player.Value.scene.Equals(scene) && player.Value.instance == instance)
                    {
                        playersPresent.Add(player.Key);

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
            H3MPWristMenuSection.UpdateMaxHealth(scene, instance, -2, -1);
        }

        public static void DestroyTrackedScripts(TrackedObjectData trackedObjectData)
        {
            // Recurse through children first
            if(trackedObjectData.children != null)
            {
                for (int i = 0; i < trackedObjectData.children.Count; ++i)
                {
                    DestroyTrackedScripts(trackedObjectData.children[i]);
                }
            }

            if (trackedObjectData.physical != null)
            {
                DestroyImmediate(trackedObjectData.physical);
            }
        }

        public static void ReassignSpectatorHost(int clientID, List<int> debounce)
        {
            if (Server.spectatorHostByController.TryGetValue(clientID, out int host))
            {
                List<int> whitelist = new List<int>();
                if (GameManager.scene.Equals(Server.clients[clientID].player.scene) && GameManager.instance == Server.clients[clientID].player.instance)
                {
                    whitelist.Add(0);
                }
                if (GameManager.playersByInstanceByScene.TryGetValue(Server.clients[clientID].player.scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(Server.clients[clientID].player.instance, out List<int> otherPlayers))
                {
                    whitelist.AddRange(otherPlayers);
                }
                whitelist.RemoveRange(0, debounce.Count);

                int newController = -1;
                if (whitelist.Count > 0)
                {
                    newController = Mod.GetBestPotentialObjectHost(clientID, false);
                }

                if (newController == -1)
                {
                    Server.spectatorHostByController.Remove(clientID);
                    Server.spectatorHostControllers.Remove(host);

                    if (GameManager.spectatorHosts.Contains(host))
                    {
                        Server.availableSpectatorHosts.Add(host);
                    }

                    if (host == GameManager.ID)
                    {
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
                    else
                    {
                        ServerSend.UnassignSpectatorHost(host);
                    }
                }
                else
                {
                    Server.spectatorHostByController.Remove(clientID);
                    Server.spectatorHostControllers.Remove(host);

                    Server.spectatorHostByController.Add(newController, host);
                    Server.spectatorHostControllers.Add(host, newController);

                    if (host == 0)
                    {
                        GameManager.spectatorHostControlledBy = newController;
                    }
                    else if (newController == 0)
                    {
                        Mod.OnSpectatorHostReceivedInvoke(host, true);
                    }

                    ServerSend.SpectatorHostAssignment(host, newController, true);
                }
            }
        }

        public static bool GetTrackedObjectType(Transform t, out Type trackedObjectType)
        {
            List<Type> trackedObjectTypes = new List<Type>();
            foreach (KeyValuePair<Type, List<Type>> entry in Mod.trackedObjectTypes)
            {
                // Going from last to first in list will go through most specific subtype to most generic
                for (int i = entry.Value.Count - 1; i >= 0; --i)
                {
                    MethodInfo isOfTypeMethod = entry.Value[i].GetMethod("IsOfType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if ((bool)isOfTypeMethod.Invoke(null, new object[] { t }))
                    {
                        trackedObjectTypes.Add(entry.Value[i]);
                        break;
                    }
                }
            }

            if(trackedObjectTypes.Count == 0)
            {
                trackedObjectType = null;
                return false;
            }
            else if(trackedObjectTypes.Count == 1)
            {
                trackedObjectType = trackedObjectTypes[0];
                return true;
            }
            else // More than one tracked type without the same supertype matches, must choose one
            {
                Type firstType = trackedObjectTypes[0];
                for (int i=0; i < trackedObjectTypes.Count; ++i)
                {
                    MethodInfo typeOverrideMethod = trackedObjectTypes[i].GetMethod("GetTypeOverrides", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    if (typeOverrideMethod != null)
                    {
                        Type[] overriddenTypes = typeOverrideMethod.Invoke(null, null) as Type[];
                        if(overriddenTypes != null)
                        {
                            for(int j=0; j < overriddenTypes.Length; ++i)
                            {
                                trackedObjectTypes.Remove(overriddenTypes[i]);
                            }
                        }
                    }
                }

                if (trackedObjectTypes.Count == 0)
                {
                    Mod.LogWarning(t.name + " had conflicting possible tracked types overriding each other. Tracked as: "+firstType.Name);
                    trackedObjectType = firstType;
                }
                else if (trackedObjectTypes.Count == 1)
                {
                    trackedObjectType = trackedObjectTypes[0];
                }
                else
                {
                    Mod.LogWarning(t.name + " had conflicting possible tracked types missing override. Tracked as: "+ trackedObjectTypes[0].Name);
                    trackedObjectType = trackedObjectTypes[0];
                }

                return true;
            }
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

        public static bool TrackSkipped(Transform root, Type trackedObjectType)
        {
            MethodInfo method = trackedObjectType.GetMethod("TrackSkipped", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters()[0].ParameterType == typeof(Transform))
            {
                return (bool)method.Invoke(null, new object[] { root });
            }

            return true;
        }

        public static void OnSceneLoadedVR(bool loading)
        {
            if (loading) // Just started loading
            {
                Mod.LogInfo("Switching scene, from " + GameManager.scene + " to " + LoadLevelBeginPatch.loadingLevel, false);
                sceneLoading = true;
                instanceAtSceneLoadStart = instance;
                sceneAtSceneLoadStart = GameManager.scene;

                if (Mod.managerObject == null)
                {
                    connectedAtLoadStart = false;
                }
                else
                {
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
                    // This makes assumption that player must go through main menu to leave TNH game or lobby
                    // They cannot go from TNH game or lobby to a scene other than main menu
                    //TODO: If this is not always true, will have to handle by "if we leave a TNH scene" instead of "if we go into main menu"
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
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                        {
                            GM.CurrentPlayerBody.EnableHands();
                        }
                        Mod.currentlyPlayingTNH = false;
                    }
                    if (LoadLevelBeginPatch.loadingLevel.Equals("TakeAndHold_Lobby_2") && Mod.currentTNHInstance != null)
                    {
                        Mod.TNHSpectating = false;
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                        {
                            GM.CurrentPlayerBody.EnableHands();
                        }
                    }

                    // Check if there are other players where we are going to prevent things like prefab spawns
                    if (playersByInstanceByScene.TryGetValue(LoadLevelBeginPatch.loadingLevel, out Dictionary<int, List<int>> relevantInstances))
                    {
                        if (relevantInstances.TryGetValue(instance, out List<int> relevantPlayers))
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

                    // Clear any of our tracked items that have not awoken in the previous scene
                    ClearUnawoken();

                    // Raise event
                    if (OnSceneLeft != null)
                    {
                        OnSceneLeft(sceneAtSceneLoadStart, LoadLevelBeginPatch.loadingLevel);
                    }
                }
            }
            else // Finished loading
            {
                scene = LoadLevelBeginPatch.loadingLevel;
                sceneLoading = false;
                Mod.LogInfo("Arrived in scene: " + scene, false);

                if (Mod.managerObject != null)
                {
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
                    playersPresent.Clear();
                    if (!nonSynchronizedScenes.ContainsKey(scene))
                    {
                        controlOverride = true;
                        firstPlayerInSceneInstance = true;
                        foreach (KeyValuePair<int, PlayerManager> player in players)
                        {
                            if (player.Value.scene.Equals(scene) && player.Value.instance == instance)
                            {
                                playersPresent.Add(player.Key);

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

                        if (Mod.spectatorHostWaitingForTNHSetup)
                        {
                            if (scene.Equals("TakeAndHold_Lobby_2"))
                            {
                                if (spectatorHostControlledBy != -1)
                                {
                                    Mod.OnTNHHostClicked();
                                    Mod.TNHOnDeathSpectate = Mod.TNHRequestHostOnDeathSpectate;
                                    Mod.OnTNHHostConfirmClicked();

                                    if (ThreadManager.host)
                                    {
                                        ServerSend.TNHSpectatorHostReady(spectatorHostControlledBy, instance);
                                    }
                                    else
                                    {
                                        Mod.spectatorHostWaitingForTNHInstance = true;
                                    }
                                }

                                Mod.spectatorHostWaitingForTNHSetup = false;
                            }
                            else if (scene.Equals("MainMenu3"))
                            {
                                SteamVR_LoadLevel.Begin("TakeAndHold_Lobby_2", false, 0.5f, 0f, 0f, 0f, 1f);
                                Mod.spectatorHostWaitingForTNHSetup = true;
                            }
                            else
                            {
                                SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                                Mod.spectatorHostWaitingForTNHSetup = true;
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
                    H3MPWristMenuSection.UpdateMaxHealth(scene, instance, -2, -1);

                    // Spectator host stuff
                    if (resetSpectatorHost)
                    {
                        if (!GameManager.scene.Equals("MainMenu3"))
                        {
                            SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                        }

                        resetSpectatorHost = false;
                    }

                    // Raise event
                    if (OnSceneJoined != null)
                    {
                        OnSceneJoined(scene, sceneAtSceneLoadStart);
                    }

                    // Do our own stuff, this is uaually what a mod would do when OnSceneJoined event is raised
                    TrackedGatlingGunData.firstInScene = true;

                    // Process list of objects we want to retrack
                    for (int i = 0; i < retrack.Count; ++i)
                    {
                        if (retrack[i] != null)
                        {
                            SyncTrackedObjects(retrack[i].transform, true, null);
                        }
                    }
                    retrack.Clear();
                }

                Mod.LogInfo("Setting player prefab to " + playerPrefabID);
                // Connected or not, upon arriving in the new scene, we want to set playerbody to selected option
                SetPlayerPrefab(playerPrefabID);
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

        public static void ProcessPlayerDamage(float damageMult, bool head, Damage damage)
        {
            bool processDamage = true;
            if (OnPlayerDamage != null)
            {
                OnPlayerDamage(damageMult, damage, ref processDamage);
            }

            if (processDamage)
            {
                damage.Dam_TotalEnergetic *= damageMult;
                damage.Dam_TotalKinetic *= damageMult;
                if (head)
                {
                    GM.CurrentPlayerBody.Hitboxes[0].Damage(damage);
                }
                else // Apply to torso hitbox, same as neck
                {
                    GM.CurrentPlayerBody.Hitboxes[2].Damage(damage);
                }
            }
        }

        // The types here are the types of the tracked objects you want to have distributed if you only want to distribute specific ones
        public static void DistributeAllControl(int clientID, int overrideController = -1, List<Type> types = null)
        {
            // Get best potential host
            int newController = overrideController == -1 ? Mod.GetBestPotentialObjectHost(clientID, false) : overrideController;

            // TODO: Optimization: Could keep track of items by controller in a dict, go through those specifically
            //                     Or could at least use itemsByInstanceByScene and go only through the item in the same scene/instance as the client

            // Give all objects
            for (int i = 0; i < Server.objects.Length; ++i)
            {
                if (Server.objects[i] != null && (types == null || types.Contains(Server.objects[i].GetType())) && Server.objects[i].controller == clientID)
                {
                    TrackedObjectData trackedObject = Server.objects[i];

                    bool destroyed = newController == -1;

                    if (destroyed) // No other player to take control, destroy
                    {
                        if(trackedObject.physical == null)
                        {
                            ServerSend.DestroyObject(trackedObject.trackedID);
                            Server.objects[trackedObject.trackedID] = null;
                            if (Server.connectedClients.Count > 0)
                            {
                                // Note: Player list being a reference, if a player leaves the scene/instance, they will be removed from the list of clients 
                                //       this index is waiting for as well. This is fine though because if they leave the scene/instance, we can assume they
                                //       got rid of the object on their side
                                if (Server.availableIndexBufferWaitingFor.TryGetValue(trackedObject.trackedID, out List<int> waitingForPlayers))
                                {
                                    for (int j = 0; j < Server.connectedClients.Count; ++j)
                                    {
                                        if (!waitingForPlayers.Contains(Server.connectedClients[j]))
                                        {
                                            waitingForPlayers.Add(Server.connectedClients[j]);
                                        }
                                    }
                                }
                                else
                                {
                                    Server.availableIndexBufferWaitingFor.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                }
                                for (int j = 0; j < Server.connectedClients.Count; ++j)
                                {
                                    if (Server.availableIndexBufferClients.TryGetValue(Server.connectedClients[j], out List<int> existingIndices))
                                    {
                                        // Already waiting for this client's confirmation for some index, just add it to existing list
                                        existingIndices.Add(trackedObject.trackedID);
                                    }
                                    else // Not yet waiting for this client's confirmation for an index, add entry to dict
                                    {
                                        Server.availableIndexBufferClients.Add(Server.connectedClients[j], new List<int>() { trackedObject.trackedID });
                                    }
                                }

                                // Add to dict of IDs to request
                                if(Server.IDsToConfirm.TryGetValue(trackedObject.trackedID, out List<int> clientList))
                                {
                                    for (int j = 0; j < Server.connectedClients.Count; ++j)
                                    {
                                        if (!clientList.Contains(Server.connectedClients[j]))
                                        {
                                            clientList.Add(Server.connectedClients[j]);
                                        }
                                    }
                                }
                                else
                                {
                                    Server.IDsToConfirm.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                }

                                Mod.LogInfo("Added " + trackedObject.trackedID + " to ID buffer");
                            }
                            else // No one to request ID availability from, can just readd directly
                            {
                                Server.availableObjectIndices.Add(trackedObject.trackedID);
                            }
                            if (objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                            {
                                objectList.Remove(trackedObject.trackedID);
                            }
                            trackedObject.awaitingInstantiation = false;

                            if(clientID == ID)
                            {
                                trackedObject.RemoveFromLocal();
                            }
                        }
                        else
                        {
                            Destroy(trackedObject.physical.gameObject);
                        }
                    }
                    else if (clientID != 0 && newController == 0) // If new controller is us, take control
                    {
                        trackedObject.localTrackedID = GameManager.objects.Count;
                        GameManager.objects.Add(trackedObject);
                        // Physical object could be null if we are given control while we are loading, the giving client will think we are in their scene/instance
                        if (trackedObject.physical == null)
                        {
                            // If its is null and we receive this after having finishes loading, we only want to instantiate if it is in our current scene/instance
                            // Otherwise we send destroy order for the object
                            if (!sceneLoading)
                            {
                                if (trackedObject.scene.Equals(scene) && trackedObject.instance == instance)
                                {
                                    if (!trackedObject.awaitingInstantiation)
                                    {
                                        trackedObject.awaitingInstantiation = true;
                                        AnvilManager.Run(trackedObject.Instantiate());
                                    }
                                }
                                else
                                {
                                    if (trackedObject.physical != null)
                                    {
                                        Destroy(trackedObject.physical.gameObject);
                                    }
                                    else
                                    {
                                        ServerSend.DestroyObject(trackedObject.trackedID);
                                        trackedObject.RemoveFromLocal();
                                        Server.objects[trackedObject.trackedID] = null;
                                        if (Server.connectedClients.Count > 0)
                                        {
                                            if (Server.availableIndexBufferWaitingFor.TryGetValue(trackedObject.trackedID, out List<int> waitingForPlayers))
                                            {
                                                for (int j = 0; j < Server.connectedClients.Count; ++j)
                                                {
                                                    if (!waitingForPlayers.Contains(Server.connectedClients[j]))
                                                    {
                                                        waitingForPlayers.Add(Server.connectedClients[j]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Server.availableIndexBufferWaitingFor.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                            }
                                            for (int j = 0; j < Server.connectedClients.Count; ++j)
                                            {
                                                if (Server.availableIndexBufferClients.TryGetValue(Server.connectedClients[j], out List<int> existingIndices))
                                                {
                                                    // Already waiting for this client's confirmation for some index, just add it to existing list
                                                    existingIndices.Add(trackedObject.trackedID);
                                                }
                                                else // Not yet waiting for this client's confirmation for an index, add entry to dict
                                                {
                                                    Server.availableIndexBufferClients.Add(Server.connectedClients[j], new List<int>() { trackedObject.trackedID });
                                                }
                                            }

                                            // Add to dict of IDs to request
                                            if (Server.IDsToConfirm.TryGetValue(trackedObject.trackedID, out List<int> clientList))
                                            {
                                                for (int j = 0; j < Server.connectedClients.Count; ++j)
                                                {
                                                    if (!clientList.Contains(Server.connectedClients[j]))
                                                    {
                                                        clientList.Add(Server.connectedClients[j]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Server.IDsToConfirm.Add(trackedObject.trackedID, new List<int>(Server.connectedClients));
                                            }

                                            Mod.LogInfo("Added " + trackedObject.trackedID + " to ID buffer");
                                        }
                                        else // No one to request ID availability from, can just readd directly
                                        {
                                            Server.availableObjectIndices.Add(trackedObject.trackedID);
                                        }
                                        if (objectsByInstanceByScene.TryGetValue(trackedObject.scene, out Dictionary<int, List<int>> currentInstances) &&
                                            currentInstances.TryGetValue(trackedObject.instance, out List<int> objectList))
                                        {
                                            objectList.Remove(trackedObject.trackedID);
                                        }
                                        trackedObject.awaitingInstantiation = false;
                                    }
                                    destroyed = true;
                                }
                            }
                            // else we will instantiate when we are done loading
                        }
                    }

                    if (!destroyed)
                    {
                        trackedObject.SetController(newController);

                        ServerSend.GiveObjectControl(trackedObject.trackedID, newController, null);
                    }
                }
            }
        }

        public static void TakeAllPhysicalControl(bool destroyTrackedScript)
        {
            TrackedObjectData[] arrToUse = null;
            if (ThreadManager.host)
            {
                arrToUse = Server.objects;
            }
            else
            {
                arrToUse = Client.objects;
            }

            foreach (TrackedObjectData currentObject in arrToUse)
            {
                if(currentObject != null && currentObject.physical != null)
                {
                    // Setting controller like this will ensure that in the case of something like tracked items,
                    // the item will be set non kinematic if necessary
                    currentObject.controller = ID;

                    if (destroyTrackedScript)
                    {
                        currentObject.physical.skipFullDestroy = true;
                        Destroy(currentObject.physical);
                    }
                }
            }
        }

        public static void RaisePlayerBodyInit(FVRPlayerBody playerBody)
        {
            if(OnPlayerBodyInit != null)
            {
                OnPlayerBodyInit(playerBody);
            }
        }

        public static bool InSceneInit()
        {
            return SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine
                || AnvilPrefabSpawnPatch.inInitPrefabSpawn
                || inPostSceneLoadTrack
                || BrutPlacerPatch.inInitBrutPlacer;
        }
    }
}
