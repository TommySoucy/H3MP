using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using static UnityEngine.ParticleSystem;

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
        public static Dictionary<string, int> synchronizedScenes = new Dictionary<string, int>(); // Dict of scenes that can be synced

        public static bool giveControlOfDestroyed;

        public static Vector3 torsoOffset = new Vector3(0, -0.4f, 0);
        public static int playersInSameScene = 0;

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
                    synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
                }
            }
        }

        public void SpawnPlayer(int ID, string username, string scene, Vector3 position, Quaternion rotation)
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
            players.Add(ID, playerManager);

            // Make sure the player is disabled if not in the same scene
            if (!scene.Equals(SceneManager.GetActiveScene().name))
            {
                playerManager.gameObject.SetActive(false);
            }
            else
            {
                ++playersInSameScene;
            }
        }

        public static void UpdatePlayerState(int ID, Vector3 position, Quaternion rotation, Vector3 headPos, Quaternion headRot, Vector3 torsoPos, Quaternion torsoRot,
                                             Vector3 leftHandPos, Quaternion leftHandRot,
                                             Vector3 rightHandPos, Quaternion rightHandRot)
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
        }

        public static void UpdatePlayerScene(int playerID, string sceneName)
        {
            H3MP_PlayerManager player = players[playerID];

            player.scene = sceneName;

            if (H3MP_ThreadManager.host)
            {
                H3MP_Server.clients[playerID].player.scene = sceneName;
            }

            if (sceneName.Equals(SceneManager.GetActiveScene().name) && H3MP_GameManager.synchronizedScenes.ContainsKey(sceneName))
            {
                if (!player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(true);
                    ++playersInSameScene;
                }
            }
            else
            {
                if (player.gameObject.activeSelf)
                {
                    player.gameObject.SetActive(false);
                    --playersInSameScene;
                }
            }
        }

        public static void UpdateTrackedItem(H3MP_TrackedItemData updatedItem)
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
                if(trackedItemData.controller != ID && (updatedItem.order > trackedItemData.order || trackedItemData.order - updatedItem.order > 128))
                {
                    trackedItemData.Update(updatedItem);
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
                SyncTrackedItems(root.transform, init ? inControl : !OtherPlayersInScene(), null, scene.name);
            }
        }

        private static bool OtherPlayersInScene()
        {
            foreach(KeyValuePair<int, H3MP_PlayerManager> player in players)
            {
                if (player.Value.gameObject.activeSelf)
                {
                    return true;
                }
            }
            return false;
        }

        private static void SyncTrackedItems(Transform root, bool controlEverything, H3MP_TrackedItemData parent, string scene)
        {
            // NOTE: When we sync tracked items, we always send the parent before its children, through TCP. This means we are guaranteed 
            //       that if we receive a full item packet on the server or any client and it has a parent,
            //       this parent is guaranteed to be in the global list already
            //       We are later dependent on this fact so if we modify anything here, ensure this remains true
            FVRPhysicalObject physObj = root.GetComponent<FVRPhysicalObject>();
            if (physObj != null)
            {
                if (physObj.ObjectWrapper != null)
                {
                    if (controlEverything || IsControlled(physObj))
                    {
                        H3MP_TrackedItem trackedItem = MakeItemTracked(physObj, parent);
                        if (H3MP_ThreadManager.host)
                        {
                            // This will also send a packet with the item to be added in the client's global item list
                            H3MP_Server.AddTrackedItem(trackedItem.data, scene, 0);
                        }
                        else
                        {
                            // Tell the server we need to add this item to global tracked items
                            H3MP_ClientSend.TrackedItem(trackedItem.data, scene);
                        }

                        // Add to local list
                        trackedItem.data.localtrackedID = items.Count;
                        items.Add(trackedItem.data);

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
            }
            else
            {
                foreach(Transform child in root)
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
            data.physicalObject = trackedItem;

            if(parent != null)
            {
                data.parent = parent.trackedID;
                if(parent.children == null)
                {
                    parent.children = new List<H3MP_TrackedItemData>();
                }
                data.childIndex = parent.children.Count;
                parent.children.Add(data);
            }
            data.itemID = physObj.ObjectWrapper.ItemID;
            data.position = trackedItem.transform.position;
            data.rotation = trackedItem.transform.rotation;
            data.active = trackedItem.gameObject.activeInHierarchy;

            data.controller = H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID;

            return trackedItem;
        }

        // MOD: This will be called to check if the given physObj is controlled by this client
        //      This currently only checks if item is in a slot or is being held
        //      A mod can postfix this to change the return value if it wants to have control of items based on other criteria
        public static bool IsControlled(FVRPhysicalObject physObj)
        {
            return physObj.m_hand != null || physObj.QuickbeltSlot != null;
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
                if (OtherPlayersInScene())
                {
                    giveControlOfDestroyed = true;
                }
            }
            else // Finished loading
            {
                giveControlOfDestroyed = false;

                Scene loadedScene = SceneManager.GetActiveScene();

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

                // Update players' active state depending on which are in the same scene
                if (synchronizedScenes.ContainsKey(loadedScene.name))
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in players)
                    {
                        if (player.Value.scene.Equals(loadedScene))
                        {
                            if (!player.Value.gameObject.activeSelf)
                            {
                                player.Value.gameObject.SetActive(true);
                                ++playersInSameScene;
                            }
                        }
                        else
                        {
                            if (player.Value.gameObject.activeSelf)
                            {
                                player.Value.gameObject.SetActive(false);
                                --playersInSameScene;
                            }
                        }
                    }

                    // Just arrived in syncable scene, sync items with server/clients
                    // NOTE THAT THIS IS DEPENDENT ON US HAVING UPDATED WHICH OTHER PLAYERS ARE VISIBLE LIKE WE DO IN THE ABOVE LOOP
                    SyncTrackedItems();
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
                    playersInSameScene = 0;
                }
            }
        }
    }
}
