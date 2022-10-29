using BepInEx;
using FistVR;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using static H3MP.H3MP_TrackedItem;

namespace H3MP
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        // BepinEx
        public const string pluginGuid = "VIP.TommySoucy.H3MP";
        public const string pluginName = "H3MP";
        public const string pluginVersion = "1.0.0";

        // Assets
        public static JObject config;
        public GameObject H3MPMenuPrefab;
        public GameObject playerPrefab;
        public GameObject H3MPMenu;

        // Menu refs
        public static Text mainStatusText;
        public static Text statusLocationText;
        public static Text statusPlayerCountText;
        public GameObject hostButton;
        public GameObject connectButton;
        public GameObject joinButton;

        // Live
        public static GameObject managerObject;
        public static int skipNextFires = 0;
        public static int skipNextInstantiates = 0;

        // Debug
        bool debug;

        private void Start()
        {
            Logger.LogInfo("H3MP Started");

            Init();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                debug = !debug;
            }

            if (!debug)
            {
                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    OnHostClicked();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    OnConnectClicked();
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    LoadConfig();
                }
            }
        }

        private void LoadConfig()
        {
            Logger.LogInfo("Loading config...");
            config = JObject.Parse(File.ReadAllText("BepInEx/Plugins/H3MP/Config.json"));
            Logger.LogInfo("Config loaded");
        }

        private void InitMenu()
        {
            Logger.LogInfo("H3MP InitMenu called");
            H3MPMenu = Instantiate(H3MPMenuPrefab, new Vector3(-1.1418f, 1.3855f, -3.64f), Quaternion.Euler(0, 196.6488f, 0));

            // Add background pointables
            H3MPMenu.transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            H3MPMenu.transform.GetChild(1).gameObject.AddComponent<FVRPointable>();

            // Init refs
            mainStatusText = H3MPMenu.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<Text>();
            statusLocationText = H3MPMenu.transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<Text>();
            statusPlayerCountText = H3MPMenu.transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<Text>();

            // Init buttons
            hostButton = H3MPMenu.transform.GetChild(0).GetChild(1).gameObject;
            FVRPointableButton currentButton = hostButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnHostClicked);
            connectButton = H3MPMenu.transform.GetChild(0).GetChild(2).gameObject;
            currentButton = connectButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnConnectClicked);
            joinButton = H3MPMenu.transform.GetChild(1).GetChild(1).gameObject;
            currentButton = joinButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnJoinClicked);
            Logger.LogInfo("H3MP Menu initialized");
        }

        private void LoadAssets()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile("BepInEx/Plugins/H3MP/H3MP.ab");

            H3MPMenuPrefab = assetBundle.LoadAsset<GameObject>("H3MPMenu");

            playerPrefab = assetBundle.LoadAsset<GameObject>("Player");
            playerPrefab.AddComponent<H3MP_PlayerManager>();
        }

        private void Init()
        {
            Logger.LogInfo("H3MP Init called");

            DoPatching();

            LoadConfig();

            LoadAssets();

            InitMenu();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void DoPatching()
        {
            var harmony = new HarmonyLib.Harmony("VIP.TommySoucy.H3MP");

            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPrefix = typeof(HandCurrentInteractableSetPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handCurrentInteractableSetPatchOriginal, new HarmonyMethod(handCurrentInteractableSetPatchPrefix), new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(setQuickBeltSlotPatchOriginal, null, new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            // FirePatch
            MethodInfo firePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo firePatchPrefix = typeof(FirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(firePatchOriginal, new HarmonyMethod(firePatchPrefix));

            // ChamberEjectRoundPatch
            MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo chamberEjectRoundPatchPrefix = typeof(ChamberEjectRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(chamberEjectRoundPatchOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));

            // Internal_CloneSinglePatch
            MethodInfo internal_CloneSinglePatchOriginal = typeof(Object).GetMethod("Internal_CloneSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSinglePatchPostfix = typeof(Internal_CloneSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_CloneSinglePatchOriginal, null, new HarmonyMethod(internal_CloneSinglePatchPostfix));

            // Internal_CloneSingleWithParentPatch
            MethodInfo internal_CloneSingleWithParentPatchOriginal = typeof(Object).GetMethod("Internal_CloneSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPrefix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPostfix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_CloneSingleWithParentPatchOriginal, new HarmonyMethod(internal_CloneSingleWithParentPatchPrefix), new HarmonyMethod(internal_CloneSingleWithParentPatchPostfix));

            // Internal_InstantiateSinglePatch
            MethodInfo internal_InstantiateSinglePatchOriginal = typeof(Object).GetMethod("Internal_InstantiateSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSinglePatchPostfix = typeof(Internal_InstantiateSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_InstantiateSinglePatchOriginal, null, new HarmonyMethod(internal_InstantiateSinglePatchPostfix));

            // Internal_InstantiateSingleWithParentPatch
            MethodInfo internal_InstantiateSingleWithParentPatchOriginal = typeof(Object).GetMethod("Internal_InstantiateSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPrefix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPostfix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_InstantiateSingleWithParentPatchOriginal, new HarmonyMethod(internal_InstantiateSingleWithParentPatchPrefix), new HarmonyMethod(internal_InstantiateSingleWithParentPatchPostfix));
        }

        private void OnHostClicked()
        {
            Logger.LogInfo("Host button clicked");
            hostButton.GetComponent<BoxCollider>().enabled = false;
            hostButton.transform.GetChild(0).GetComponent<Text>().color = Color.gray;

            //H3MP_Server.IP = config["IP"].ToString();
            CreateManagerObject(true);

            H3MP_Server.Start((ushort)config["MaxClientCount"], (ushort)config["Port"]);

            mainStatusText.text = "Starting...";
            mainStatusText.color = Color.white;
        }

        private void OnConnectClicked()
        {
            CreateManagerObject();

            H3MP_Client client = managerObject.AddComponent<H3MP_Client>();
            client.IP = config["IP"].ToString();
            client.port = (ushort)config["Port"];

            client.ConnectToServer();

            mainStatusText.text = "Connecting...";
            mainStatusText.color = Color.white;
        }

        private void CreateManagerObject(bool host = false)
        {
            if (managerObject == null)
            {
                managerObject = new GameObject();

                H3MP_ThreadManager threadManager = managerObject.AddComponent<H3MP_ThreadManager>();
                H3MP_ThreadManager.host = host;

                H3MP_GameManager gameManager = managerObject.AddComponent<H3MP_GameManager>();
                gameManager.playerPrefab = playerPrefab;

                DontDestroyOnLoad(managerObject);
            }
        }

        private void OnJoinClicked()
        {
            // TODO
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            if (loadedScene.name.Equals("MainMenu3"))
            {
                Logger.LogInfo("H3 Main menu loaded, initializing H3MP menu");
                InitMenu();
            }
        }
    }

    // Patches FVRViveHand.CurrentInteractable.set to keep track of item held
    class HandCurrentInteractableSetPatch
    {
        static FVRInteractiveObject preObject;

        static void Prefix(ref FVRViveHand __instance, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            preObject = ___m_currentInteractable;
        }

        static void Postfix(ref FVRViveHand __instance, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (preObject == null && ___m_currentInteractable != null)
            {
                // Just started interacing with this item
                H3MP_TrackedItem trackedItem = ___m_currentInteractable.GetComponent<H3MP_TrackedItem>();
                if (trackedItem != null)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        if (trackedItem.data.controller != 0)
                        {
                            // Take control

                            // Send to all clients
                            H3MP_ServerSend.GiveControl(trackedItem.data.trackedID, 0);

                            // Update locally
                            trackedItem.data.controller = 0;
                            trackedItem.data.localtrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                            // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                            //if (trackedItem.data.parent == -1)
                            //{
                            //    (___m_currentInteractable as FVRPhysicalObject).RecoverRigidbody();
                            //}
                        }
                        else
                        {
                            Debug.Log("\tAlready in control");
                        }
                    }
                    else
                    {
                        if (trackedItem.data.controller != H3MP_Client.singleton.ID)
                        {
                            // Take control

                            // Send to all clients
                            H3MP_ClientSend.GiveControl(trackedItem.data.trackedID, H3MP_Client.singleton.ID);

                            // Update locally
                            trackedItem.data.controller = H3MP_Client.singleton.ID;
                            trackedItem.data.localtrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                            // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                            //if (trackedItem.data.parent == -1)
                            //{
                            //  (___m_currentInteractable as FVRPhysicalObject).RecoverRigidbody();
                            //}
                        }
                    }
                }
            }
        }
    }

    // Patches FVRPhysicalObject.SetQuickBeltSlot so we can keep track of item control
    class SetQuickBeltSlotPatch
    {
        static void Postfix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (slot != null)
            {
                // Just put this item in a slot
                H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID))
                {
                    if (H3MP_ThreadManager.host)
                    {
                        if (trackedItem.data.controller != 0)
                        {
                            // Take control

                            // Send to all clients
                            H3MP_ServerSend.GiveControl(trackedItem.data.trackedID, 0);

                            // Update locally
                            trackedItem.data.controller = 0;
                            trackedItem.data.localtrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                            // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                            //if (trackedItem.data.parent == -1)
                            //{
                            //  __instance.RecoverRigidbody();
                            //}
                        }
                    }
                    else
                    {
                        if (trackedItem.data.controller != H3MP_Client.singleton.ID)
                        {
                            // Take control

                            // Send to server and all other clients
                            H3MP_ClientSend.GiveControl(trackedItem.data.trackedID, H3MP_Client.singleton.ID);

                            // Update locally
                            trackedItem.data.controller = H3MP_Client.singleton.ID;
                            trackedItem.data.localtrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                            // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                            //if (trackedItem.data.parent == -1)
                            //{
                            //  __instance.RecoverRigidbody();
                            //}
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFireArm.Fire so we can keep track of when a firearm is fired
    // TODO: This depends on the specific firearm type calling base.Fire() or overriding FVRFireArm.Fire, will need to check if this is true for each type
    //       and if not will have to handle the exceptions accordingly
    class FirePatch
    {
        static void Prefix(ref FVRFireArm __instance)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipNextInstantiates;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // Get tracked item
            H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
            if(trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.WeaponFire(0, trackedItem.data.trackedID);
                    }
                }
                else if(trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.WeaponFire(trackedItem.data.trackedID);
                }
            }
        }
    }

    // Patches FVRFireArmChamber.EjectRound so we can keep track of when a round is ejected from a chamber
    class ChamberEjectRoundPatch
    {
        static bool track = false;

        static void Prefix(ref FVRFireArmChamber __instance, ref FVRFireArmRound ___m_round, bool ForceCaseLessEject)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // Check if a round would be ejected
            if(___m_round != null && (!___m_round.IsCaseless || ForceCaseLessEject))
            {
                if (__instance.IsSpent)
                {
                    // Skip the instantiation of the casing because we don't want to sync these between clients
                    ++Mod.skipNextInstantiates;
                }
                else // We are ejecting a whole round, we want the controller of the chamber's parent tracked item to control the round
                {
                    Transform currentParent = __instance.transform;
                    H3MP_TrackedItem trackedItem = null;
                    while (currentParent != null)
                    {
                        trackedItem = currentParent.GetComponent<H3MP_TrackedItem>();
                        if(trackedItem != null)
                        {
                            break;
                        }
                    }

                    // Check if we should control and sync it, if so do it in postfix
                    if (trackedItem == null || trackedItem.data.controller == (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID))
                    {
                        track = true;
                    }
                    else // Round was instantiated from chamber of an item that is controlled by other client
                    {
                        // Skip the instantiate on our side, the controller client will instantiate and sync it with us eventually
                        ++Mod.skipNextInstantiates;
                    }
                }
            }
        }

        static void Postfix(ref FVRFireArmRound __result)
        {
            if (track)
            {
                track = false;

                H3MP_GameManager.SyncTrackedItems(__result.transform, true, null, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches Object.Internal_CloneSingle to keep track of this type of instantiation
    class Internal_CloneSinglePatch
    {
        static void Postfix(ref Object __result)
        {
            if (Mod.skipNextInstantiates > 0)
            {
                --Mod.skipNextInstantiates;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, null, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches Object.Internal_CloneSingleWithParent to keep track of this type of instantiation
    class Internal_CloneSingleWithParentPatch
    {
        static bool track = false;
        static H3MP_TrackedItemData parentData;

        static void Prefix(Object data, Transform parent)
        {
            if (Mod.skipNextInstantiates > 0)
            {
                --Mod.skipNextInstantiates;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }


            // If this is a game object check and sync all physical objects if necessary
            if (data is GameObject)
            {
                // Check if has tracked parent
                Transform currentParent = parent;
                parentData = null;
                while (parent != null)
                {
                    H3MP_TrackedItem trackedItem = parent.GetComponent<H3MP_TrackedItem>();
                    if (trackedItem != null)
                    {
                        parentData = trackedItem.data;
                        break;
                    }
                    currentParent = currentParent.parent;
                }

                // We only want to track this item if no tracked parent or if we control the parent
                track = parentData == null || parentData.controller == (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID);
            }
        }

        static void Postfix(ref Object __result, Transform parent)
        {
            if (track)
            {
                track = false;
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, parentData, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches Object.Internal_InstantiateSingle to keep track of this type of instantiation
    class Internal_InstantiateSinglePatch
    {
        static void Postfix(ref Object __result)
        {
            if (Mod.skipNextInstantiates > 0)
            {
                --Mod.skipNextInstantiates;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, null, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches Object.Internal_InstantiateSingleWithParent to keep track of this type of instantiation
    class Internal_InstantiateSingleWithParentPatch
    {
        static bool track = false;
        static H3MP_TrackedItemData parentData;

        static void Prefix(Object data, Transform parent)
        {
            if (Mod.skipNextInstantiates > 0)
            {
                --Mod.skipNextInstantiates;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }


            // If this is a game object check and sync all physical objects if necessary
            if (data is GameObject)
            {
                // Check if has tracked parent
                Transform currentParent = parent;
                parentData = null;
                while (parent != null)
                {
                    H3MP_TrackedItem trackedItem = parent.GetComponent<H3MP_TrackedItem>();
                    if (trackedItem != null)
                    {
                        parentData = trackedItem.data;
                        break;
                    }
                    currentParent = currentParent.parent;
                }

                // We only want to track this item if no tracked parent or if we control the parent
                track = parentData == null || parentData.controller == (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID);
            }
        }

        static void Postfix(ref Object __result, Transform parent)
        {
            if (track)
            {
                track = false;
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, parentData, SceneManager.GetActiveScene().name);
            }
        }
    }
}
