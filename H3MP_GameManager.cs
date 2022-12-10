using FistVR;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.VR;

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
                    Debug.Log($"{nameof(H3MP_GameManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public static Dictionary<int, H3MP_PlayerManager> players = new Dictionary<int, H3MP_PlayerManager>();
        public static List<H3MP_TrackedItemData> items = new List<H3MP_TrackedItemData>(); // Tracked items under control of this gameManager
        public static List<H3MP_TrackedSosigData> sosigs = new List<H3MP_TrackedSosigData>(); // Tracked sosigs under control of this gameManager
        public static List<H3MP_TrackedAutoMeaterData> autoMeaters = new List<H3MP_TrackedAutoMeaterData>(); // Tracked AutoMeaters under control of this gameManager
        public static List<H3MP_TrackedEncryptionData> encryptions = new List<H3MP_TrackedEncryptionData>(); // Tracked TNH_EncryptionTarget under control of this gameManager
        public static Dictionary<string, int> synchronizedScenes = new Dictionary<string, int>(); // Dict of scenes that can be synced
        public static Dictionary<int, List<List<string>>> waitingWearables = new Dictionary<int, List<List<string>>>();
        public static Dictionary<Sosig, H3MP_TrackedSosig> trackedSosigBySosig = new Dictionary<Sosig, H3MP_TrackedSosig>();
        public static Dictionary<AutoMeater, H3MP_TrackedAutoMeater> trackedAutoMeaterByAutoMeater = new Dictionary<AutoMeater, H3MP_TrackedAutoMeater>();
        public static Dictionary<TNH_EncryptionTarget, H3MP_TrackedEncryption> trackedEncryptionByEncryption = new Dictionary<TNH_EncryptionTarget, H3MP_TrackedEncryption>();
        public static Dictionary<int, int> activeInstances = new Dictionary<int, int>();
        public static Dictionary<int, H3MP_TNHInstance> TNHInstances = new Dictionary<int, H3MP_TNHInstance>();

        public static bool giveControlOfDestroyed;

        public static int ID = 0;
        public static Vector3 torsoOffset = new Vector3(0, -0.4f, 0);
        public static Vector3 overheadDisplayOffset = new Vector3(0, 0.25f, 0);
        public static int playersPresent = 0;
        public static int playerStateAddtionalDataSize = -1;
        public static int instance = 0;

        //public GameObject localPlayerPrefab;
        public GameObject playerPrefab;

        private void Awake()
        {
            singleton = this;

            SteamVR_Events.Loading.Listen(OnSceneLoadedVR);

            // All vanilla scenes can be synced by default
            if (synchronizedScenes.Count == 0)
            {
                int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                    synchronizedScenes.Add(sceneName, 0);
                }
            }

            // Init the main instance
            activeInstances.Add(instance, 1);
        }

        public void SpawnPlayer(int ID, string username, string scene, int instance, Vector3 position, Quaternion rotation)
        {
            Debug.Log($"Spawn player called with ID: {ID}");

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
            players.Add(ID, playerManager);

            // Add to instance
            if (activeInstances.ContainsKey(instance))
            {
                ++activeInstances[instance];
            }
            else
            {
                activeInstances.Add(instance, 1);
            }

            // Make sure the player is disabled if not in the same scene/instance
            if (!scene.Equals(SceneManager.GetActiveScene().name) || instance != H3MP_GameManager.instance)
            {
                playerManager.gameObject.SetActive(false);

                playerManager.SetEntitiesRegistered(false);
            }
            else
            {
                ++playersPresent;
            }
        }

        public static void UpdatePlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                             Vector3 leftHandPos, Quaternion leftHandRot,
                                             Vector3 rightHandPos, Quaternion rightHandRot,
                                             float health, int maxHealth, byte[] additionalData)
        {
            if (!players.ContainsKey(ID))
            {
                Debug.LogWarning($"Received UDP order to update player {ID} state but player of this ID hasnt been spawned yet");
                return;
            }

            H3MP_PlayerManager player = players[ID];
            if (!player.gameObject.activeSelf)
            {
                return;
            }

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
            if (player.healthIndicator.gameObject.activeSelf)
            {
                player.healthIndicator.text = ((int)health).ToString() + "/" + maxHealth;
            }

            ProcessAdditionalPlayerData(ID, additionalData);
        }

        public static void UpdatePlayerScene(int playerID, string sceneName)
        {
            H3MP_PlayerManager player = players[playerID];

            player.scene = sceneName;

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[playerID].player.scene = sceneName;
            }

            if (sceneName.Equals(SceneManager.GetActiveScene().name) && H3MP_GameManager.synchronizedScenes.ContainsKey(sceneName) && instance == player.instance)
            {
                if (!player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(true);
                    UpdatePlayerHidden(player);
                    ++playersPresent;

                    player.SetEntitiesRegistered(true);
                }
            }
            else
            {
                if (player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(false);
                    --playersPresent;

                    player.SetEntitiesRegistered(false);
                }
            }
        }

        // MOD: This will be called to set a player as hidden based on certain criteria
        //      Currently sets a player as hidden if they are in the same TNH game as us and are dead for example
        //      A mod could prefix this to base it on other criteria, mainly for other game modes
        public static bool UpdatePlayerHidden(H3MP_PlayerManager player)
        {
            // TNH
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == player.instance)
            {
                if (Mod.currentTNHInstance.dead.Contains(player.ID))
                {
                    player.SetVisible(false);
                    return false;
                }
                else // Player not dead
                {
                    if(GM.TNH_Manager != null && Mod.currentTNHInstance.currentlyPlaying.Contains(player.ID))
                    {
                        // We are currently in a TNH game with this player, add them to radar
                        GM.TNH_Manager.TAHReticle.RegisterTrackedObject(player.head, (TAH_ReticleContact.ContactType)(-2)); // -2 is a custom value handled by TAHReticleContactPatch
                    }
                }
            }

            // If have not found a reason for player to be hidden, set as visible
            player.SetVisible(true);
            return true;
        }

        public static void UpdatePlayerInstance(int playerID, int instance)
        {
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

                    if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == player.instance)
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
                    currentInstance.dead.Remove(playerID);
                }
            }

            player.instance = instance;

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[playerID].player.instance = instance;
            }

            if (player.scene.Equals(SceneManager.GetActiveScene().name) && H3MP_GameManager.synchronizedScenes.ContainsKey(player.scene) && H3MP_GameManager.instance == player.instance)
            {
                if (!player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(true);
                    UpdatePlayerHidden(player);
                    ++playersPresent;

                    player.SetEntitiesRegistered(true);
                }
            }
            else
            {
                if (player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(false);
                    --playersPresent;

                    player.SetEntitiesRegistered(false);
                }
            }

            if (activeInstances.ContainsKey(instance))
            {
                ++activeInstances[instance];
            }
            else
            {
                activeInstances.Add(instance, 1);
            }

            if (TNHInstances.ContainsKey(instance))
            {
                if (TNHInstances[instance].playerIDs.Contains(playerID))
                {
                    Debug.LogWarning("UpdatePlayerInstance: player " + playerID + " already in playerIDs of TNH instance " + instance);
                }

                TNHInstances[instance].playerIDs.Add(playerID);

                // Add player to active TNH player list
                if (Mod.TNHMenu != null && Mod.TNHPlayerList != null && Mod.TNHPlayerPrefab != null &&
                    Mod.currentTNHInstancePlayers != null && !Mod.currentTNHInstancePlayers.ContainsKey(playerID))
                {
                    GameObject newPlayerElement = Instantiate<GameObject>(Mod.TNHPlayerPrefab, Mod.TNHPlayerList.transform);
                    newPlayerElement.transform.GetChild(0).GetComponent<Text>().text = Mod.config["Username"].ToString();
                    newPlayerElement.SetActive(true);

                    Mod.currentTNHInstancePlayers.Add(playerID, newPlayerElement);
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
            int ID = -1;
            if (H3MP_ThreadManager.host)
            {
                if (updatedItem.trackedID < H3MP_Server.items.Length)
                {
                    trackedItemData = H3MP_Server.items[updatedItem.trackedID];
                    ID = 0;
                }
            }
            else
            {
                if (updatedItem.trackedID < H3MP_Client.items.Length)
                {
                    trackedItemData = H3MP_Client.items[updatedItem.trackedID];
                    ID = H3MP_Client.singleton.ID;
                }
            }

            if (trackedItemData != null)
            {
                // If we take control of an item, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this item
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if(trackedItemData.controller != ID && (ignoreOrder || ((updatedItem.order > trackedItemData.order || trackedItemData.order - updatedItem.order > 128))))
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
            int ID = -1;
            if (H3MP_ThreadManager.host)
            {
                if (updatedSosig.trackedID < H3MP_Server.sosigs.Length)
                {
                    trackedSosigData = H3MP_Server.sosigs[updatedSosig.trackedID];
                    ID = 0;
                }
            }
            else
            {
                if (updatedSosig.trackedID < H3MP_Client.sosigs.Length)
                {
                    trackedSosigData = H3MP_Client.sosigs[updatedSosig.trackedID];
                    ID = H3MP_Client.singleton.ID;
                }
            }

            if (trackedSosigData != null)
            {
                // If we take control of a sosig, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if(trackedSosigData.controller != ID && (ignoreOrder || ((updatedSosig.order > trackedSosigData.order || trackedSosigData.order - updatedSosig.order > 128))))
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
            int ID = -1;
            if (H3MP_ThreadManager.host)
            {
                if (updatedAutoMeater.trackedID < H3MP_Server.autoMeaters.Length)
                {
                    trackedAutoMeaterData = H3MP_Server.autoMeaters[updatedAutoMeater.trackedID];
                    ID = 0;
                }
            }
            else
            {
                if (updatedAutoMeater.trackedID < H3MP_Client.autoMeaters.Length)
                {
                    trackedAutoMeaterData = H3MP_Client.autoMeaters[updatedAutoMeater.trackedID];
                    ID = H3MP_Client.singleton.ID;
                }
            }

            if (trackedAutoMeaterData != null)
            {
                // If we take control of a AutoMeater, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if(trackedAutoMeaterData.controller != ID && (ignoreOrder || ((updatedAutoMeater.order > trackedAutoMeaterData.order || trackedAutoMeaterData.order - updatedAutoMeater.order > 128))))
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
            int ID = -1;
            if (H3MP_ThreadManager.host)
            {
                if (updatedEncryption.trackedID < H3MP_Server.encryptions.Length)
                {
                    trackedEncryptionData = H3MP_Server.encryptions[updatedEncryption.trackedID];
                    ID = 0;
                }
            }
            else
            {
                if (updatedEncryption.trackedID < H3MP_Client.encryptions.Length)
                {
                    trackedEncryptionData = H3MP_Client.encryptions[updatedEncryption.trackedID];
                    ID = H3MP_Client.singleton.ID;
                }
            }

            if (trackedEncryptionData != null)
            {
                // If we take control of a encryption, we could still receive an updated item from another client
                // if they haven't received the control update yet, so here we check if this actually needs to update
                // AND we don't want to take this update if this is a packet that was sent before the previous update
                // Since the order is kept as a single byte, it will overflow every 256 packets of this sosig
                // Here we consider the update out of order if it is within 128 iterations before the latest
                if (trackedEncryptionData.controller != ID && (ignoreOrder || ((updatedEncryption.order > trackedEncryptionData.order || trackedEncryptionData.order - updatedEncryption.order > 128))))
                {
                    trackedEncryptionData.Update(updatedEncryption);
                }
            }
        }

        public static void SyncTrackedItems(bool init = false, bool inControl = false)
        {
            Debug.Log("SyncTrackedItems called with init: "+init+", in control: "+inControl+", others: "+(playersPresent > 0));
            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other items get destroyed. We will receive any item that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach(GameObject root in roots)
            {
                SyncTrackedItems(root.transform, init ? inControl : playersPresent == 0, null, scene.name);
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
                            H3MP_TrackedItem trackedItem = MakeItemTracked(physObj, parent);
                            if (H3MP_ThreadManager.host)
                            {
                                // This will also send a packet with the item to be added in the client's global item list
                                H3MP_Server.AddTrackedItem(trackedItem.data, scene, instance, 0);
                            }
                            else
                            {
                                Debug.Log("Sending tracked item: " + trackedItem.data.itemID);
                                // Tell the server we need to add this item to global tracked items
                                H3MP_ClientSend.TrackedItem(trackedItem.data, scene, instance);
                            }

                            foreach (Transform child in root)
                            {
                                SyncTrackedItems(child, controlEverything, trackedItem.data, scene);
                            }
                        }
                        else // Item will not be controlled by us but is an item that should be tracked by system, so destroy it
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
            }
            else
            {
                foreach (Transform child in root)
                {
                    SyncTrackedItems(child, controlEverything, null, scene);
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

            data.controller = ID;

            // Add to local list
            data.localTrackedID = items.Count;
            items.Add(data);

            return trackedItem;
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

        // MOD: Certain FVRPhysicalObjects don't have an ObjectWrapper
        //      We would normally not want to track these but here may be some exceptions, like TNH_ShatterableCrates
        public static bool IsObjectIdentifiable(FVRPhysicalObject physObj)
        {
            return physObj.ObjectWrapper != null || physObj.GetComponent<TNH_ShatterableCrate>() != null;
        }

        public static void SyncTrackedSosigs(bool init = false, bool inControl = false)
        {
            Debug.Log("SyncTrackedSosigs called with init: " + init + ", in control: " + inControl + ", others: " + (playersPresent > 0));
            // When we sync our current scene, if we are alone, we sync and take control of all sosigs
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedSosigs(root.transform, init ? inControl : playersPresent == 0, scene.name);
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
                        if (H3MP_ThreadManager.host)
                        {
                            // This will also send a packet with the sosig to be added in the client's global sosig list
                            H3MP_Server.AddTrackedSosig(trackedSosig.data, scene, instance, 0);
                        }
                        else
                        {
                            Debug.Log("Sending tracked sosig");
                            // Tell the server we need to add this item to global tracked items
                            H3MP_ClientSend.TrackedSosig(trackedSosig.data, scene, instance);
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
            Debug.Log("MakeSosigTracked called");
            H3MP_TrackedSosig trackedSosig = sosigScript.gameObject.AddComponent<H3MP_TrackedSosig>();
            H3MP_TrackedSosigData data = new H3MP_TrackedSosigData();
            trackedSosig.data = data;
            data.physicalObject = trackedSosig;
            trackedSosig.physicalSosigScript = sosigScript;
            H3MP_GameManager.trackedSosigBySosig.Add(sosigScript, trackedSosig);

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
                        Debug.LogError("SosigWearable: " + data.wearables[i][j] + " not found in map");
                    }
                }
            }
            data.ammoStores = (int[])Mod.SosigInventory_m_ammoStores.GetValue(sosigScript.Inventory);
            data.controller = ID;
            data.mustard = sosigScript.Mustard;
            data.bodyPose = sosigScript.BodyPose;
            data.IFF = (byte)sosigScript.GetIFF();

            // Add to local list
            data.localTrackedID = sosigs.Count;
            sosigs.Add(data);

            return trackedSosig;
        }

        public static void SyncTrackedAutoMeaters(bool init = false, bool inControl = false)
        {
            Debug.Log("SyncTrackedAutoMeaters called with init: " + init + ", in control: " + inControl + ", others: " + (playersPresent > 0));
            // When we sync our current scene, if we are alone, we sync and take control of all AutoMeaters
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedAutoMeaters(root.transform, init ? inControl : playersPresent == 0, scene.name);
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
                        if (H3MP_ThreadManager.host)
                        {
                            // This will also send a packet with the AutoMeater to be added in the client's global AutoMeater list
                            H3MP_Server.AddTrackedAutoMeater(trackedAutoMeater.data, scene, instance, 0);
                        }
                        else
                        {
                            Debug.Log("Sending tracked AutoMeater");
                            // Tell the server we need to add this AutoMeater to global tracked AutoMeaters
                            H3MP_ClientSend.TrackedAutoMeater(trackedAutoMeater.data, scene, instance);
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
            Debug.Log("MakeSosigTracked called");
            H3MP_TrackedAutoMeater trackedAutoMeater = autoMeaterScript.gameObject.AddComponent<H3MP_TrackedAutoMeater>();
            H3MP_TrackedAutoMeaterData data = new H3MP_TrackedAutoMeaterData();
            trackedAutoMeater.data = data;
            data.physicalObject = trackedAutoMeater;
            trackedAutoMeater.physicalAutoMeaterScript = autoMeaterScript;
            H3MP_GameManager.trackedAutoMeaterByAutoMeater.Add(autoMeaterScript, trackedAutoMeater);

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
                Debug.Log("Unsupported AutoMeater type tracked");
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

            // Add to local list
            data.localTrackedID = autoMeaters.Count;
            autoMeaters.Add(data);

            return trackedAutoMeater;
        }

        public static void SyncTrackedEncryptions(bool init = false, bool inControl = false)
        {
            Debug.Log("SyncTrackedEncryptions called with init: " + init + ", in control: " + inControl + ", others: " + (playersPresent > 0));
            // When we sync our current scene, if we are alone, we sync and take control of everything
            // If we are not alone, we take control only of what we are currently interacting with
            // while all other encryptions get destroyed. We will receive any encryption that the players inside this scene are controlling
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                SyncTrackedEncryptions(root.transform, init ? inControl : playersPresent == 0, scene.name);
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
                        if (H3MP_ThreadManager.host)
                        {
                            // This will also send a packet with the Encryption to be added in the client's global item list
                            H3MP_Server.AddTrackedEncryption(trackedEncryption.data, scene, instance, 0);
                        }
                        else
                        {
                            Debug.Log("Sending tracked Encryption: " + trackedEncryption.data.type);
                            // Tell the server we need to add this Encryption to global tracked Encryptions
                            H3MP_ClientSend.TrackedEncryption(trackedEncryption.data, scene, instance);
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

            data.type = encryption.Type;
            data.position = trackedEncryption.transform.position;
            data.rotation = trackedEncryption.transform.rotation;
            data.active = trackedEncryption.gameObject.activeInHierarchy;
            //TODO: add all the other shit we wanna track like the subtargetS? maybe

            data.controller = ID;

            // Add to local list
            data.localTrackedID = encryptions.Count;
            encryptions.Add(data);

            return trackedEncryption;
        }

        public static H3MP_TNHInstance AddNewTNHInstance(int hostID, bool letPeopleJoin,
                                                         int progressionTypeSetting, int healthModeSetting, int equipmentModeSetting,
                                                         int targetModeSetting, int AIDifficultyModifier, int radarModeModifier,
                                                         int itemSpawnerMode, int backpackMode, int healthMult, int sosiggunShakeReloading, int TNHSeed, int levelIndex)
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
                                                                    itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelIndex);
                TNHInstances.Add(freeInstance, newInstance);
                activeInstances.Add(freeInstance, 0);

                Mod.modInstance.OnTNHInstanceReceived(newInstance);

                H3MP_ServerSend.AddTNHInstance(newInstance);

                return newInstance;
            }
            else
            {
                H3MP_ClientSend.AddTNHInstance(hostID, letPeopleJoin,
                                               progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                               targetModeSetting, AIDifficultyModifier, radarModeModifier,
                                               itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelIndex);

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

            Mod.modInstance.OnTNHInstanceReceived(instance);
        }

        public static void AddInstance(int instance)
        {
            activeInstances.Add(instance, 0);

            Mod.modInstance.OnInstanceReceived(instance);
        }

        public static void SetInstance(int instance)
        {
            // Remove ourselves from the previous instance and manage dicts accordingly
            --activeInstances[H3MP_GameManager.instance];
            if(activeInstances[H3MP_GameManager.instance] == 0 && H3MP_GameManager.instance != 0)
            {
                activeInstances.Remove(H3MP_GameManager.instance);
            }
            if (TNHInstances.TryGetValue(H3MP_GameManager.instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.playerIDs.Remove(ID);

                if (currentInstance.playerIDs.Count == 0)
                {
                    TNHInstances.Remove(H3MP_GameManager.instance);
                    Mod.TNHSpectating = false;
                }

                // Remove from currently playing and dead if necessary
                currentInstance.currentlyPlaying.Remove(ID);
                currentInstance.dead.Remove(ID);
            }

            // Set locally
            H3MP_GameManager.instance = instance;

            bool isNewInstance = false;
            if (!activeInstances.ContainsKey(instance))
            {
                isNewInstance = true;
                activeInstances.Add(instance, 0);
            }
            ++activeInstances[instance];
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
                    if (Mod.currentTNHUIManager != null)
                    {
                        Mod.InitTNHUIManager(TNHInstances[instance]);
                    }
                }
            }

            // Item we do not control: Destroy, giveControlOfDestroyed = true will ensure destruction does not get sent
            // Item we control: Destroy, giveControlOfDestroyed = true will ensure item's control is passed on is necessary
            // Item we are interacting with: Send a destruction order to other clients but don't destroy it on our side, since we want to move with these from instance to instance
            giveControlOfDestroyed = true;
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
            for (int i = itemArrToUse.Length - 1; i >= 0; --i)
            {
                if (itemArrToUse[i] != null && itemArrToUse[i].physicalItem != null)
                {
                    if (IsControlled(itemArrToUse[i].physicalItem.physicalObject))
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
                        if (isNewInstance)
                        {
                            GameObject go = itemArrToUse[i].physicalItem.gameObject;
                            bool hadNoParent = itemArrToUse[i].physicalItem.data.parent == -1;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(itemArrToUse[i].physicalItem);

                            // Only sync the top parent of items. The children will also get retracked as children
                            if (hadNoParent)
                            {
                                SyncTrackedItems(go.transform, true, null, SceneManager.GetActiveScene().name);
                            }
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(itemArrToUse[i].physicalItem.gameObject);
                        }
                    }
                }
            }
            for (int i = sosigArrToUse.Length - 1; i >= 0; --i)
            {
                if (sosigArrToUse[i] != null && sosigArrToUse[i].physicalObject != null)
                {
                    if (IsControlled(sosigArrToUse[i].physicalObject.physicalSosigScript))
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
                        if (isNewInstance)
                        {
                            GameObject go = sosigArrToUse[i].physicalObject.gameObject;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(sosigArrToUse[i].physicalObject);

                            // Retrack sosig
                            SyncTrackedSosigs(go.transform, true, SceneManager.GetActiveScene().name);
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(sosigArrToUse[i].physicalObject.gameObject);
                        }
                    }
                }
            }
            for (int i = autoMeaterArrToUse.Length - 1; i >= 0; --i)
            {
                if (autoMeaterArrToUse[i] != null && autoMeaterArrToUse[i].physicalObject != null)
                {
                    if (IsControlled(autoMeaterArrToUse[i].physicalObject.physicalAutoMeaterScript))
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
                        if (isNewInstance)
                        {
                            GameObject go = autoMeaterArrToUse[i].physicalObject.gameObject;

                            // Destroy just the tracked script because we want to make a copy for ourselves
                            DestroyImmediate(autoMeaterArrToUse[i].physicalObject);

                            // Retrack sosig
                            SyncTrackedAutoMeaters(go.transform, true, SceneManager.GetActiveScene().name);
                        }
                        else // Destroy entire object
                        {
                            // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                            // like we do now if we didn't do immediate because OnDestroy() gets called later
                            // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                            DestroyImmediate(autoMeaterArrToUse[i].physicalObject.gameObject);
                        }
                    }
                }
            }
            for (int i = encryptionArrToUse.Length - 1; i >= 0; --i)
            {
                if (encryptionArrToUse[i] != null && encryptionArrToUse[i].physicalObject != null)
                {
                    if (isNewInstance)
                    {
                        GameObject go = encryptionArrToUse[i].physicalObject.gameObject;

                        // Destroy just the tracked script because we want to make a copy for ourselves
                        DestroyImmediate(encryptionArrToUse[i].physicalObject);

                        // Retrack sosig
                        SyncTrackedEncryptions(go.transform, true, SceneManager.GetActiveScene().name);
                    }
                    else // Destroy entire object
                    {
                        // Uses Immediate here because we need to giveControlOfDestroyed but we wouldn't be able to just wrap it
                        // like we do now if we didn't do immediate because OnDestroy() gets called later
                        // TODO: Check wich is better, using immediate, or having an item specific giveControlOnDestroy that we can set for each individual item we destroy
                        DestroyImmediate(encryptionArrToUse[i].physicalObject.gameObject);
                    }
                }
            }
            giveControlOfDestroyed = false;

            // Send update to other clients
            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.PlayerInstance(0, instance);
            }
            else
            {
                H3MP_ClientSend.PlayerInstance(instance);
            }

            // Set players active and playersPresent
            playersPresent = 0;
            string sceneName = SceneManager.GetActiveScene().name;
            if (synchronizedScenes.ContainsKey(sceneName))
            {
                foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                {
                    if (player.Value.scene.Equals(sceneName) && player.Value.instance == instance)
                    {
                        if (!player.Value.gameObject.activeSelf)
                        {
                            player.Value.gameObject.SetActive(true);
                            UpdatePlayerHidden(player.Value);
                        }
                        ++playersPresent;

                        player.Value.SetEntitiesRegistered(true);

                        if (H3MP_ThreadManager.host)
                        {
                            // Request most up to date items from the client
                            // We do this because we may not have the most up to date version of items/sosigs since
                            // clients only send updated data when there are others in their scene
                            // But we need the most of to date data to instantiate the item/sosig
                            Debug.Log("Requesting up to date objects from " + player.Key);
                            H3MP_ServerSend.RequestUpToDateObjects(player.Key);
                        }
                    }
                    else
                    {
                        if (player.Value.gameObject.activeSelf)
                        {
                            player.Value.gameObject.SetActive(false);
                        }
                    }
                }
            }
            else // New scene not syncable, ensure all players are disabled regardless of scene
            {
                foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                {
                    if (player.Value.gameObject.activeSelf)
                    {
                        player.Value.gameObject.SetActive(false);
                    }
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

        public static bool PlayersPresentSlow()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
            {
                if (player.Value.scene.Equals(currentScene.name) && player.Value.instance == instance)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnSceneLoadedVR(bool loading)
        {
            // Return right away if we don't have server or client running
            if(Mod.managerObject == null)
            {
                return;
            }

            if (loading) // Just started loading
            {
                Debug.Log("Just started loading scene");

                if (playersPresent > 0)
                {
                    giveControlOfDestroyed = true;
                }

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
                        Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, ID);
                        Mod.currentlyPlayingTNH = false;
                    }
                    Mod.currentTNHInstance = null;
                    Mod.TNHSpectating = false;
                }
            }
            else // Finished loading
            {
                --Mod.skipAllInstantiates;
                giveControlOfDestroyed = false;

                Scene loadedScene = SceneManager.GetActiveScene();
                Debug.Log("Just finished loading scene: "+ loadedScene.name);

                // Send an update to all other clients so they can decide whether they can see this client
                if (H3MP_ThreadManager.host)
                {
                    // Send the host's scene to clients
                    H3MP_ServerSend.PlayerScene(0, loadedScene.name);
                }
                else
                {
                    // Send to server, host will update and then send to all other clients
                    H3MP_ClientSend.PlayerScene(loadedScene.name);
                }

                // Update players' active state depending on which are in the same scene/instance
                playersPresent = 0;
                if (synchronizedScenes.ContainsKey(loadedScene.name))
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                    {
                        if (player.Value.scene.Equals(loadedScene.name) && player.Value.instance == instance)
                        {
                            if (!player.Value.gameObject.activeSelf)
                            {
                                player.Value.gameObject.SetActive(true);
                                UpdatePlayerHidden(player.Value);
                            }
                            ++playersPresent;

                            player.Value.SetEntitiesRegistered(true);

                            if (H3MP_ThreadManager.host)
                            {
                                // Request most up to date items from the client
                                // We do this because we may not have the most up to date version of items/sosigs since
                                // clients only send updated data when there are others in their scene
                                // But we need the most of to date data to instantiate the item/sosig
                                Debug.Log("Requesting up to date objects from "+player.Key);
                                H3MP_ServerSend.RequestUpToDateObjects(player.Key);
                            }
                        }
                        else
                        {
                            if (player.Value.gameObject.activeSelf)
                            {
                                player.Value.gameObject.SetActive(false);
                            }
                        }
                    }

                    Debug.Log("Scene is syncable, and has "+playersPresent+" other players in it, syncing");
                    // Just arrived in syncable scene, sync items with server/clients
                    // NOTE THAT THIS IS DEPENDENT ON US HAVING UPDATED WHICH OTHER PLAYERS ARE VISIBLE LIKE WE DO IN THE ABOVE LOOP
                    SyncTrackedSosigs();
                    SyncTrackedAutoMeaters();
                    SyncTrackedItems();
                    SyncTrackedEncryptions();
                }
                else // New scene not syncable, ensure all players are disabled regardless of scene
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                    {
                        if (player.Value.gameObject.activeSelf)
                        {
                            player.Value.gameObject.SetActive(false);
                        }
                    }
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
    }
}
