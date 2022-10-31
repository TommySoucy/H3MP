using BepInEx;
using FistVR;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;
using static RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple;

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
        public static int skipAllInstantiates = 0;

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
                else if (Input.GetKeyDown(KeyCode.Keypad3))
                {
                    SteamVR_LoadLevel.Begin("IndoorRange", false, 0.5f, 0f, 0f, 0f, 1f);
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
            MethodInfo firePatchPostfix = typeof(FirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(firePatchOriginal, new HarmonyMethod(firePatchPrefix), new HarmonyMethod(firePatchPostfix));

            // ChamberEjectRoundPatch
            MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo chamberEjectRoundPatchPrefix = typeof(ChamberEjectRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(chamberEjectRoundPatchOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));

            // Internal_CloneSinglePatch
            MethodInfo internal_CloneSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSinglePatchPostfix = typeof(Internal_CloneSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_CloneSinglePatchOriginal, null, new HarmonyMethod(internal_CloneSinglePatchPostfix));

            // Internal_CloneSingleWithParentPatch
            MethodInfo internal_CloneSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPrefix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPostfix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_CloneSingleWithParentPatchOriginal, new HarmonyMethod(internal_CloneSingleWithParentPatchPrefix), new HarmonyMethod(internal_CloneSingleWithParentPatchPostfix));

            // Internal_InstantiateSinglePatch
            MethodInfo internal_InstantiateSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSinglePatchPostfix = typeof(Internal_InstantiateSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_InstantiateSinglePatchOriginal, null, new HarmonyMethod(internal_InstantiateSinglePatchPostfix));

            // Internal_InstantiateSingleWithParentPatch
            MethodInfo internal_InstantiateSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPrefix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPostfix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(internal_InstantiateSingleWithParentPatchOriginal, new HarmonyMethod(internal_InstantiateSingleWithParentPatchPrefix), new HarmonyMethod(internal_InstantiateSingleWithParentPatchPostfix));

            // LoadDefaultSceneRoutinePatch
            MethodInfo loadDefaultSceneRoutinePatchOriginal = typeof(ItemSpawnerV2).GetMethod("LoadDefaultSceneRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo loadDefaultSceneRoutinePatchPrefix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo loadDefaultSceneRoutinePatchPostfix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadDefaultSceneRoutinePatchOriginal, new HarmonyMethod(loadDefaultSceneRoutinePatchPrefix), new HarmonyMethod(loadDefaultSceneRoutinePatchPostfix));

            // SpawnObjectsPatch
            MethodInfo spawnObjectsPatchOriginal = typeof(VaultSystem).GetMethod("SpawnObjects", BindingFlags.Public | BindingFlags.Static);
            MethodInfo spawnObjectsPatchPrefix = typeof(SpawnObjectsPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(spawnObjectsPatchOriginal, new HarmonyMethod(spawnObjectsPatchPrefix));

            // SpawnVaultFileRoutinePatch
            MethodInfo spawnVaultFileRoutinePatchOriginal = typeof(VaultSystem).GetMethod("SpawnVaultFileRoutine", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchMoveNext = EnumeratorMoveNext(spawnVaultFileRoutinePatchOriginal);
            MethodInfo spawnVaultFileRoutinePatchPrefix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchTranspiler = typeof(SpawnVaultFileRoutinePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchPostfix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(spawnVaultFileRoutinePatchMoveNext, new HarmonyMethod(spawnVaultFileRoutinePatchPrefix), new HarmonyMethod(spawnVaultFileRoutinePatchPostfix), new HarmonyMethod(spawnVaultFileRoutinePatchTranspiler));

            // ProjectileFirePatch
            MethodInfo projectileFirePatchOriginal = typeof(BallisticProjectile).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float), typeof(Vector3), typeof(FVRFireArm), typeof(bool) }, null);
            MethodInfo projectileFirePatchPostfix = typeof(ProjectileFirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(projectileFirePatchOriginal, new HarmonyMethod(projectileFirePatchPostfix));

            // ProjectileDamageablePatch
            MethodInfo projectileDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo projectileDamageablePatchTranspiler = typeof(ProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(projectileDamageablePatchOriginal, null, null, new HarmonyMethod(projectileDamageablePatchTranspiler));

            // SubMunitionsDamageablePatch
            MethodInfo subMunitionsDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("FireSubmunitions", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo subMunitionsDamageablePatchTranspiler = typeof(SubMunitionsDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(subMunitionsDamageablePatchOriginal, null, null, new HarmonyMethod(subMunitionsDamageablePatchTranspiler));

            // ExplosionDamageablePatch
            MethodInfo explosionDamageablePatchOriginal = typeof(Explosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo explosionDamageablePatchTranspiler = typeof(ExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(explosionDamageablePatchOriginal, null, null, new HarmonyMethod(explosionDamageablePatchTranspiler));

            // GrenadeExplosionDamageablePatch
            MethodInfo grenadeExplosionDamageablePatchOriginal = typeof(GrenadeExplosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeExplosionDamageablePatchTranspiler = typeof(GrenadeExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(explosionDamageablePatchOriginal, null, null, new HarmonyMethod(explosionDamageablePatchTranspiler));

            // FlameThrowerDamageablePatch
            MethodInfo flameThrowerDamageablePatchOriginal = typeof(FlameThrower).GetMethod("AirBlast", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flameThrowerDamageablePatchTranspiler = typeof(FlameThrowerDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(flameThrowerDamageablePatchOriginal, null, null, new HarmonyMethod(flameThrowerDamageablePatchTranspiler));

            // GrenadeDamageablePatch
            MethodInfo grenadeDamageablePatchOriginal = typeof(FVRGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeDamageablePatchTranspiler = typeof(GrenadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(grenadeDamageablePatchOriginal, null, null, new HarmonyMethod(grenadeDamageablePatchTranspiler));

            // DemonadeDamageablePatch
            MethodInfo demonadeDamageablePatchOriginal = typeof(MF2_Demonade).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo demonadeDamageablePatchTranspiler = typeof(DemonadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(demonadeDamageablePatchOriginal, null, null, new HarmonyMethod(demonadeDamageablePatchTranspiler));

            // PinnedGrenadeDamageablePatch
            MethodInfo pinnedGrenadeDamageablePatchOriginal = typeof(PinnedGrenade).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadeDamageablePatchTranspiler = typeof(PinnedGrenadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(pinnedGrenadeDamageablePatchOriginal, null, null, new HarmonyMethod(pinnedGrenadeDamageablePatchTranspiler));

            // PinnedGrenadeCollisionDamageablePatch
            MethodInfo pinnedGrenadeCollisionDamageablePatchOriginal = typeof(PinnedGrenade).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadeCollisionDamageablePatchTranspiler = typeof(PinnedGrenadeCollisionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(pinnedGrenadeCollisionDamageablePatchOriginal, null, null, new HarmonyMethod(pinnedGrenadeCollisionDamageablePatchTranspiler));

            // SosigWeaponDamageablePatch
            MethodInfo sosigWeaponDamageablePatchOriginal = typeof(SosigWeapon).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigWeaponDamageablePatchOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchTranspiler));
        }

        // This is a copy of HarmonyX's AccessTools extension method EnumeratorMoveNext (i think)
        public static MethodInfo EnumeratorMoveNext(MethodBase method)
        {
            if (method is null)
            {
                return null;
            }

            var codes = PatchProcessor.ReadMethodBody(method).Where(pair => pair.Key == OpCodes.Newobj);
            if (codes.Count() != 1)
            {
                return null;
            }
            var ctor = codes.First().Value as ConstructorInfo;
            if (ctor == null)
            {
                return null;
            }
            var type = ctor.DeclaringType;
            if (type == null)
            {
                return null;
            }
            return AccessTools.Method(type, nameof(IEnumerator.MoveNext));
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
            ++Mod.skipAllInstantiates;

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

        static void Postfix()
        {
            --Mod.skipAllInstantiates;
        }
    }

    // Patches FVRFireArmChamber.EjectRound so we can keep track of when a round is ejected from a chamber
    class ChamberEjectRoundPatch
    {
        static bool track = false;
        static int incrementedSkip = 0;

        static void Prefix(ref FVRFireArmChamber __instance, ref FVRFireArmRound ___m_round, bool ForceCaseLessEject)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            incrementedSkip = 0;

            // Check if a round would be ejected
            if (___m_round != null && (!___m_round.IsCaseless || ForceCaseLessEject))
            {
                if (__instance.IsSpent)
                {
                    // Skip the instantiation of the casing because we don't want to sync these between clients
                    ++Mod.skipAllInstantiates;
                    ++incrementedSkip;
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
                        ++Mod.skipAllInstantiates;
                        ++incrementedSkip;
                    }
                }
            }
        }

        static void Postfix(ref FVRFireArmRound __result)
        {
            if(incrementedSkip > 0)
            {
                Mod.skipAllInstantiates -= incrementedSkip;
            }

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
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (__result == null || Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // If we want to skip the instantiate because this is a scene load vault file being spawned
            if (SpawnVaultFileRoutinePatch.inSpawnVaultFileRoutineToSkip)
            {
                // If not for this the item would be spawned and then synced with other clients below
                // The scene has presumably already been fully loaded, which means we already synced all items in the scene with other clients
                // But this is still an item spawned by scene initialization, so if we are not the first one in the scene, we want to destroy this item
                // because the client that has initialized the scene spawned these and synced them
                if (H3MP_GameManager.playersInSameScene > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
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

        static void Prefix(UnityEngine.Object data, Transform parent)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (data == null || Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
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

        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {

            // If we want to skip the instantiate because this is a scene load vault file being spawned
            if (SpawnVaultFileRoutinePatch.inSpawnVaultFileRoutineToSkip)
            {
                // If not for this the item would be spawned and then synced with other clients below
                // The scene has presumably already been fully loaded, which means we already synced all items in the scene with other clients
                // But this is still an item spawned by scene initialization, so if we are not the first one in the scene, we want to destroy this item
                // because the client that has initialized the scene spawned these and synced them
                if (H3MP_GameManager.playersInSameScene > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);

                    track = false;
                    return;
                }
            }

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
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (__result == null || Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // If we want to skip the instantiate because this is a scene load vault file being spawned
            if (SpawnVaultFileRoutinePatch.inSpawnVaultFileRoutineToSkip)
            {
                // If not for this the item would be spawned and then synced with other clients below
                // The scene has presumably already been fully loaded, which means we already synced all items in the scene with other clients
                // But this is still an item spawned by scene initialization, so if we are not the first one in the scene, we want to destroy this item
                // because the client that has initialized the scene spawned these and synced them
                if (H3MP_GameManager.playersInSameScene > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
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

        static void Prefix(UnityEngine.Object data, Transform parent)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (data == null || Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
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

        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            // If we want to skip the instantiate because this is a scene load vault file being spawned
            if (SpawnVaultFileRoutinePatch.inSpawnVaultFileRoutineToSkip)
            {
                // If not for this the item would be spawned and then synced with other clients below
                // The scene has presumably already been fully loaded, which means we already synced all items in the scene with other clients
                // But this is still an item spawned by scene initialization, so if we are not the first one in the scene, we want to destroy this item
                // because the client that has initialized the scene spawned these and synced them
                if (H3MP_GameManager.playersInSameScene > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);

                    track = false;
                    return;
                }
            }

            if (track)
            {
                track = false;
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, parentData, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches ItemSpawnerV2.LoadDefaultSceneRoutine so we know when we spawn items from vault as part of scene loading
    class LoadDefaultSceneRoutinePatch
    {
        public static bool inLoadDefaultSceneRoutine;

        static void Prefix()
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            inLoadDefaultSceneRoutine = true;
        }

        static void Postfix()
        {
            inLoadDefaultSceneRoutine = false;
        }
    }

    // Patches VaultSystem.SpawnObjects so we can access the vaultfile that was sent from LoadDefaultSceneRoutine
    class SpawnObjectsPatch
    {
        static void Prefix(VaultFile file)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            if (LoadDefaultSceneRoutinePatch.inLoadDefaultSceneRoutine)
            {
                if(SpawnVaultFileRoutinePatch.filesToSkip == null)
                {
                    SpawnVaultFileRoutinePatch.filesToSkip = new List<string>();
                }
                SpawnVaultFileRoutinePatch.filesToSkip.Add(file.FileName);
            }
        }
    }

    // Patches VaultSystem.SpawnVaultFileRoutine.MoveNext to keep track of whether we are spawning items as part of scene loading
    class SpawnVaultFileRoutinePatch
    {
        // Use https://github.com/BepInEx/HarmonyX/wiki/Enumerator-patches
        // to patch the specific moveNext where the instantiate happens

        // In prefix, set a flag true
        // In postfix set the flag false

        // In between, when instantiate is called in SpawnVaultFileRoutine, in the instantiate patches, we check the flag, if true we skip instantiation
        // Problem is that this routine is used to spawn objects other than default scene, so these will be skipped too

        // We need another flag to know if this was started by load default scene, because only if it was do we want to skip the insntatiation
        // We could do this by having a flag set when we call load default scene and when we start the first step of the coroutine we set our own flag and reset 
        // the load default scene to false. Our own flag tells us we started the coroutine by load default scene
        // So now we only skip the instantiations specific to the default scene

        public static bool inSpawnVaultFileRoutineToSkip;
        public static List<string> filesToSkip;
        public static string currentFile;

        public static Dictionary<string, List<UnityEngine.Object>> routineData = new Dictionary<string, List<UnityEngine.Object>>();

        public static void FinishedRoutine()
        {
            if (inSpawnVaultFileRoutineToSkip && routineData.ContainsKey(currentFile))
            {
                // Destroy any objects that need to be destroyed and remove the data
                foreach (UnityEngine.Object obj in routineData[currentFile])
                {
                    UnityEngine.Object.Destroy(obj);
                }
                routineData.Remove(currentFile);
            }
        }

        static void Prefix(ref VaultFile ___f)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            if (filesToSkip != null && filesToSkip.Contains(___f.FileName))
            {
                inSpawnVaultFileRoutineToSkip = true;

                currentFile = ___f.FileName;
                if (!routineData.ContainsKey(___f.FileName))
                {
                    routineData.Add(___f.FileName, new List<UnityEngine.Object>());
                }
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            CodeInstruction toInsert = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpawnVaultFileRoutinePatch), "FinishedRoutine"));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stfld && instruction.operand.ToString().Equals("System.Int32 $PC") &&
                    instructionList[i + 1].opcode == OpCodes.Ldc_I4_0 && instructionList[i + 2].opcode == OpCodes.Ret)
                {
                    instructionList.Insert(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }

        static void Postfix(ref VaultFile ___f)
        {
            inSpawnVaultFileRoutineToSkip = false;

        }
    }

    #region Damageable Patches
    //TODO: Implement damage prevention for these if we are not controller of the cause of damage. These are all classes taht call IFVRDamageable.Damage
    /*
     * FVRPhysicalObject.MeleeParams
     * SosigWeapon
     * AIMeleeWeapon
     * 
     * AutoMeaterBlade
     * BangSnap
     * BearTrapInteractiblePiece
     * Chainsaw
     * Drill
     * DropTrapLogs
     * Flipzo
     * FVRIgnitable
     * FVRSparkler
     * FVRStrikeAnyWhereMatch
     * HCBBolt
     * Kabot.KSpkie
     * MeatCrab
     * MF2_BearTrapInteractionZone
     * MG_FlyingHotDogSwarm
     * MG_JerryTheLemon
     * Microtorch
     * PowerUp_Cyclops
     * RealisticLaserSword
     * RotrwCharcoal
     * SlicerBladeMaster
     * SpinningBladeTrapBase
     */

    // Patches BallisticProjectile.Fire to keep a reference to the source firearm
    class ProjectileFirePatch
    {
        static void Postfix(ref FVRFireArm ___tempFA, ref FVRFireArm firearm)
        {
            ___tempFA = firearm;
        }
    }

    // Patches BallisticProjectile.MoveBullet to ignore latest IFVRDamageable if necessary
    class ProjectileDamageablePatch
    {
        public static bool GetActualFlag(bool flag2, FVRFireArm tempFA)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return flag2;
            }

            if (flag2)
            {
                if (tempFA == null)
                {
                    // If we don't have a ref to the firearm that fired this projectile, let the damage be controlled by the host
                    if (!H3MP_ThreadManager.host)
                    {
                        return false;
                    }
                }
                else // We have a ref to the firearm that fired this projectile
                {
                    // We only want to let this projectile do damage if we control the firearm
                    H3MP_TrackedItem trackedItem = tempFA.GetComponent<H3MP_TrackedItem>();
                    if (trackedItem == null)
                    {
                        return false;
                    }
                    else
                    {
                        return (H3MP_ThreadManager.host && trackedItem.data.controller == 0) || (!H3MP_ThreadManager.host && trackedItem.data.controller == H3MP_Client.singleton.ID);
                    }
                }
            }
            return flag2;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsertFirst = new List<CodeInstruction>();
            toInsertFirst.Add(new CodeInstruction(OpCodes.Ldloc_S, 14)); // Load flag2
            toInsertFirst.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load projectile instance
            toInsertFirst.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BallisticProjectile), "tempFA"))); // Load tempFA from instance
            toInsertFirst.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProjectileDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
            toInsertFirst.Add(new CodeInstruction(OpCodes.Stloc_S, 14)); // Set flag2
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_1 &&
                    instructionList[i + 1].opcode == OpCodes.Stloc_S && instructionList[i + 1].operand.ToString().Equals("System.Boolean (14)"))
                {
                    instructionList.InsertRange(i + 2, toInsertFirst);
                }
            }
            return instructionList;
        }
    }

    // Patches BallisticProjectile.FireSubmunitions to ignore latest IFVRDamageable if necessary
    class SubMunitionsDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsertSecond = new List<CodeInstruction>();
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load explosion gameobject
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load projectile instance
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BallisticProjectile), "tempFA"))); // Load tempFA from instance
            toInsertSecond.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("FistVR.Explosion (11)") &&
                    instructionList[i + 1].opcode == OpCodes.Ldarg_0)
                {
                    instructionList.InsertRange(i, toInsertSecond);
                }
            }
            return instructionList;
        }
    }

    // Patches FlameThrower.AirBlast to ignore latest IFVRDamageable if necessary
    class FlameThrowerDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load flamethrower instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0 &&
                    instructionList[i + 1].opcode == OpCodes.Ldloc_0)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }
    }

    // Patches FVRGrenade.FVRUpdate to ignore latest IFVRDamageable if necessary
    class GrenadeDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load grenade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load explosion gameobject
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load grenade instance
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (4)"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                }
            }
            return instructionList;
        }
    }

    // Patches MF2_Demonade.Explode to ignore latest IFVRDamageable if necessary
    class DemonadeDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MF2_Demonade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }
    }

    // Patches PinnedGrenade.FVRUpdate to ignore latest IFVRDamageable if necessary
    class PinnedGrenadeDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }
    }

    // Patches PinnedGrenade.OnCollisionEnter to ignore latest IFVRDamageable if necessary
    class PinnedGrenadeCollisionDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }
    }

    // Patches SosigWeapon.Explode to ignore latest IFVRDamageable if necessary
    class SosigWeaponDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SosigWeapon instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }
    }

    // Patches Explosion.Explode to ignore latest IFVRDamageable if necessary
    class ExplosionDamageablePatch
    {
        public static void AddControllerReference(GameObject dest, GameObject src = null)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return;
            }

            GameObject srcToUse = src == null ? dest : src;
            H3MP_TrackedItem trackedItem = srcToUse.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                H3MP_ControllerReference reference = dest.GetComponent<H3MP_ControllerReference>();
                if (reference == null)
                {
                    reference = dest.AddComponent<H3MP_ControllerReference>();
                }
                reference.controller = trackedItem.data.controller;
            }
        }

        public static IFVRDamageable GetActualDamageable(MonoBehaviour mb, IFVRDamageable original)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersInSameScene == 0)
            {
                return original;
            }

            if (original != null)
            {
                H3MP_ControllerReference cr = mb.GetComponent<H3MP_ControllerReference>();
                if (cr == null)
                {
                    // If we don't have a ref to the controller of the item that caused this explosion, let the damage be controlled by the host
                    if (!H3MP_ThreadManager.host)
                    {
                        return null;
                    }
                }
                else // We have a ref to the controller of the item that caused this explosion
                {
                    // We only want to let this projectile do damage if we control the firearm
                    return (H3MP_ThreadManager.host && cr.controller == 0) || (!H3MP_ThreadManager.host && cr.controller == H3MP_Client.singleton.ID) ? original : null;
                }
            }
            return original;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load explosion instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 16)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 16)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (16)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches GrenadeExplosion.Explode to ignore latest IFVRDamageable if necessary
    class GrenadeExplosionDamageablePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load grenade explosion instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 19)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 16)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (19)"))
                {
                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }
    #endregion
}
