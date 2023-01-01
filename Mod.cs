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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

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
        public GameObject TNHMenuPrefab;
        public static GameObject TNHStartEquipButtonPrefab;
        public GameObject playerPrefab;
        public static Material reticleFriendlyContactArrowMat;
        public static Material reticleFriendlyContactIconMat;
        public GameObject H3MPMenu;
        public static GameObject TNHMenu;
        public static Dictionary<string, string> sosigWearableMap;

        // Menu refs
        public static Text mainStatusText;
        public static Text statusLocationText;
        public static Text statusPlayerCountText;
        public GameObject hostButton;
        public GameObject connectButton;
        public GameObject joinButton;

        // TNH Menu refs
        public static GameObject[] TNHMenuPages; // Main, Host, Join_Options, Join_Instance, Instance
        public static Text TNHStatusText;
        public static GameObject TNHInstanceList;
        public static GameObject TNHInstancePrefab;
        public static GameObject TNHInstanceListScrollUpArrow;
        public static GameObject TNHInstanceListScrollDownArrow;
        public static Scrollbar TNHInstanceListScrollBar;
        public static GameObject TNHHostButton;
        public static GameObject TNHJoinButton;
        public static GameObject TNHLPJCheck;
        public static GameObject TNHHostOnDeathSpectateRadio;
        public static GameObject TNHHostOnDeathLeaveRadio;
        public static GameObject TNHHostConfirmButton;
        public static GameObject TNHHostCancelButton;
        public static GameObject TNHJoinCancelButton;
        public static GameObject TNHJoinOptionsCancelButton;
        public static GameObject TNHJoinOnDeathSpectateRadio;
        public static GameObject TNHJoinOnDeathLeaveRadio;
        public static GameObject TNHJoinConfirmButton;
        public static GameObject TNHPlayerList;
        public static GameObject TNHPlayerPrefab;
        public static GameObject TNHPlayerListScrollUpArrow;
        public static GameObject TNHPlayerListScrollDownArrow;
        public static Scrollbar TNHPlayerListScrollBar;
        public static GameObject TNHLPJCheckMark;
        public static GameObject TNHHostOnDeathSpectateCheckMark;
        public static GameObject TNHHostOnDeathLeaveCheckMark;
        public static GameObject TNHJoinOnDeathSpectateCheckMark;
        public static GameObject TNHJoinOnDeathLeaveCheckMark;
        public static Text TNHInstanceTitle;

        // Live
        public static Mod modInstance;
        public static GameObject managerObject;
        public static int skipNextFires = 0;
        public static int skipAllInstantiates = 0;
        public static AudioEvent sosigFootstepAudioEvent;
        public static bool TNHMenuLPJ;
        public static bool TNHOnDeathSpectate; // If false, leave
        public static bool TNHSpectating;
        public static bool setLatestInstance; // Whether to set instance screen according to new instance index when we receive server response
        public static H3MP_TNHInstance currentTNHInstance;
        public static bool currentlyPlayingTNH;
        public static Dictionary<int, GameObject> joinTNHInstances;
        public static Dictionary<int, GameObject> currentTNHInstancePlayers;
        public static TNH_UIManager currentTNHUIManager;
        public static GameObject TNHStartEquipButton;
        public static List<int> temporaryHoldSosigIDs = new List<int>();
        public static List<int> temporaryHoldTurretIDs = new List<int>();
        public static Dictionary<int, int> temporarySupplySosigIDs = new Dictionary<int, int>();
        public static Dictionary<int, int> temporarySupplyTurretIDs = new Dictionary<int, int>();
        public static H3MP_TNHData initTNHData;

        // Reused private FieldInfos
        public static readonly FieldInfo Sosig_m_isOnOffMeshLinkField = typeof(Sosig).GetField("m_isOnOffMeshLink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_joints = typeof(Sosig).GetField("m_joints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_isCountingDownToStagger = typeof(Sosig).GetField("m_isCountingDownToStagger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_staggerAmountToApply = typeof(Sosig).GetField("m_staggerAmountToApply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_recoveringFromBallisticState = typeof(Sosig).GetField("m_recoveringFromBallisticState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_recoveryFromBallisticLerp = typeof(Sosig).GetField("m_recoveryFromBallisticLerp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_tickDownToWrithe = typeof(Sosig).GetField("m_tickDownToWrithe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_recoveryFromBallisticTick = typeof(Sosig).GetField("m_recoveryFromBallisticTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_blindTime = typeof(Sosig).GetField("m_blindTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_debuffTime_Freeze = typeof(Sosig).GetField("m_debuffTime_Freeze", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_timeSinceLastDamage = typeof(Sosig).GetField("m_timeSinceLastDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_storedShudder = typeof(Sosig).GetField("m_storedShudder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_isStunned = typeof(Sosig).GetField("m_isStunned", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_lastIFFDamageSource = typeof(Sosig).GetField("m_lastIFFDamageSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_diedFromClass = typeof(Sosig).GetField("m_diedFromClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_isBlinded = typeof(Sosig).GetField("m_isBlinded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_isFrozen = typeof(Sosig).GetField("m_isFrozen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_receivedHeadShot = typeof(Sosig).GetField("m_receivedHeadShot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Sosig_m_isConfused = typeof(Sosig).GetField("m_isConfused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo SosigLink_m_wearables = typeof(SosigLink).GetField("m_wearables", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo SosigLink_m_integrity = typeof(SosigLink).GetField("m_integrity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo SosigInventory_m_ammoStores = typeof(SosigInventory).GetField("m_ammoStores", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_UIManager_m_currentLevelIndex = typeof(TNH_UIManager).GetField("m_currentLevelIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curLevel = typeof(TNH_Manager).GetField("m_curLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curProgression = typeof(TNH_Manager).GetField("m_curProgression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curProgressionEndless = typeof(TNH_Manager).GetField("m_curProgressionEndless", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_level = typeof(TNH_Manager).GetField("m_level", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_activeSupplyPointIndicies = typeof(TNH_Manager).GetField("m_activeSupplyPointIndicies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curHoldIndex = typeof(TNH_Manager).GetField("m_curHoldIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_lastHoldIndex = typeof(TNH_Manager).GetField("m_lastHoldIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curHoldPoint = typeof(TNH_Manager).GetField("m_curHoldPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_curPointSequence = typeof(TNH_Manager).GetField("m_curPointSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_seed = typeof(TNH_Manager).GetField("m_seed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_Manager_m_patrolSquads = typeof(TNH_Manager).GetField("m_patrolSquads", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_HoldPoint_m_activeSosigs = typeof(TNH_HoldPoint).GetField("m_activeSosigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_HoldPoint_m_activeTurrets = typeof(TNH_HoldPoint).GetField("m_activeTurrets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_SupplyPoint_m_activeSosigs = typeof(TNH_SupplyPoint).GetField("m_activeSosigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_SupplyPoint_m_activeTurrets = typeof(TNH_SupplyPoint).GetField("m_activeTurrets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_SupplyPoint_m_constructor = typeof(TNH_SupplyPoint).GetField("m_constructor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_SupplyPoint_m_panel = typeof(TNH_SupplyPoint).GetField("m_panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_DestructibleBarrierPoint_m_curBarrier = typeof(TNH_DestructibleBarrierPoint).GetField("m_curBarrier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_numSubTargsLeft = typeof(TNH_EncryptionTarget).GetField("m_numSubTargsLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_numHitsLeft = typeof(TNH_EncryptionTarget).GetField("m_numHitsLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_maxHits = typeof(TNH_EncryptionTarget).GetField("m_maxHits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_damLeftForAHit = typeof(TNH_EncryptionTarget).GetField("m_damLeftForAHit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_agileStartPos = typeof(TNH_EncryptionTarget).GetField("agileStartPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_fromRot = typeof(TNH_EncryptionTarget).GetField("m_fromRot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_warpSpeed = typeof(TNH_EncryptionTarget).GetField("m_warpSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_EncryptionTarget_m_validAgilePos = typeof(TNH_EncryptionTarget).GetField("m_validAgilePos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_idleLookPoint = typeof(AutoMeater).GetField("m_idleLookPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_idleLookPointCountDown = typeof(AutoMeater).GetField("m_idleLookPointCountDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_idleDestination = typeof(AutoMeater).GetField("m_idleDestination", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_idleDestinationCountDown = typeof(AutoMeater).GetField("m_idleDestinationCountDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_controlledMovement = typeof(AutoMeater).GetField("m_controlledMovement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeater_m_flightRecoveryTime = typeof(AutoMeater).GetField("m_flightRecoveryTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeaterFirearm_M = typeof(AutoMeater.AutoMeaterFirearm).GetField("M", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AutoMeaterHitZone_m_isDestroyed = typeof(AutoMeaterHitZone).GetField("m_isDestroyed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Reused private MethodInfos
        public static readonly MethodInfo Sosig_Speak_State = typeof(Sosig).GetMethod("Speak_State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Sosig_SetBodyPose = typeof(Sosig).GetMethod("SetBodyPose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Sosig_SetBodyState = typeof(Sosig).GetMethod("SetBodyState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Sosig_VaporizeUpdate = typeof(Sosig).GetMethod("VaporizeUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo SosigLink_SeverJoint = typeof(SosigLink).GetMethod("SeverJoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_UIManager_UpdateLevelSelectDisplayAndLoader = typeof(TNH_UIManager).GetMethod("UpdateLevelSelectDisplayAndLoader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_UIManager_UpdateTableBasedOnOptions = typeof(TNH_UIManager).GetMethod("UpdateTableBasedOnOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_UIManager_PlayButtonSound = typeof(TNH_UIManager).GetMethod("PlayButtonSound", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_DelayedInit = typeof(TNH_Manager).GetMethod("DelayedInit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_SetLevel = typeof(TNH_Manager).GetMethod("SetLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_SetPhase = typeof(TNH_Manager).GetMethod("SetPhase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_OnSosigKill = typeof(TNH_Manager).GetMethod("OnSosigKill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_VoiceUpdate = typeof(TNH_Manager).GetMethod("VoiceUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_SetPhase_Take = typeof(TNH_Manager).GetMethod("SetPhase_Take", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_Manager_SetPhase_Completed = typeof(TNH_Manager).GetMethod("SetPhase_Completed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_CompleteHold = typeof(TNH_HoldPoint).GetMethod("CompleteHold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_CompletePhase = typeof(TNH_HoldPoint).GetMethod("CompletePhase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_DestructibleBarrierPoint_SetCoverPointData = typeof(TNH_DestructibleBarrierPoint).GetMethod("SetCoverPointData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_EncryptionTarget_SpawnGrowth = typeof(TNH_EncryptionTarget).GetMethod("SpawnGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_EncryptionTarget_ResetGrowth = typeof(TNH_EncryptionTarget).GetMethod("ResetGrowth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_ShatterableCrate_Destroy = typeof(TNH_ShatterableCrate).GetMethod("Destroy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo AutoMeater_SetState = typeof(AutoMeater).GetMethod("SetState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo AutoMeaterFirearm_FireShot = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("FireShot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo AutoMeaterFirearm_UpdateFlameThrower = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("UpdateFlameThrower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo AutoMeaterFirearm_UpdateFire = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("UpdateFire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Debug
        bool debug;

        private void Start()
        {
            Logger.LogInfo("H3MP Started");

            modInstance = this;

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
                else if (Input.GetKeyDown(KeyCode.Keypad4))
                {
                    SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    SteamVR_LoadLevel.Begin("ProvingGround", false, 0.5f, 0f, 0f, 0f, 1f);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad6))
                {
                    Debug.Log("Building sosigWearable map, iterating through "+IM.OD.Count+" OD entries");
                    Dictionary<string, string> map = new Dictionary<string, string>();
                    foreach(KeyValuePair<string, FVRObject> o in IM.OD)
                    {
                        Debug.Log("trying to add ID: "+o.Key);
                        GameObject prefab = null;
                        try
                        {
                            prefab = o.Value.GetGameObject();

                        }
                        catch(Exception)
                        {
                            Debug.LogError("There was an error trying to retrieve prefab with ID: " + o.Key);
                            continue;
                        }
                        try
                        {
                            SosigWearable wearable = prefab.GetComponent<SosigWearable>();
                            if (wearable != null)
                            {
                                if (map.ContainsKey(prefab.name))
                                {
                                    Debug.LogWarning("Sosig wearable with name: " + prefab.name + " is already in the map with value: " + map[prefab.name] + " and wewanted to add value: " + o.Key);
                                }
                                else
                                {
                                    Debug.Log("Sosig wearable with name: " + prefab.name + " added value: " + o.Key);
                                    map.Add(prefab.name, o.Key);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Debug.LogError("There was an error trying to check if prefab with ID: " + o.Key+" is wearable or adding it to the list");
                            continue;
                        }
                    }
                    Debug.Log("DONE");
                    JObject jDict = JObject.FromObject(map);
                    File.WriteAllText("BepInEx/Plugins/H3MP/Debug/SosigWearableMap.json", jDict.ToString());
                }
                else if (Input.GetKeyDown(KeyCode.Keypad7))
                {
                    SteamVR_LoadLevel.Begin("TakeAndHold_Lobby_2", false, 0.5f, 0f, 0f, 0f, 1f);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad8))
                {
                    OnTNHJoinClicked();
                    OnTNHJoinConfirmClicked();
                    OnTNHInstanceClicked(1);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad9))
                {
                    OnTNHHostClicked();
                    OnTNHHostConfirmClicked();
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
            FVRPointable backgroundPointable = H3MPMenu.transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;
            backgroundPointable = H3MPMenu.transform.GetChild(1).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;

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

        private void InitTNHMenu()
        {
            TNHMenu = Instantiate(TNHMenuPrefab, new Vector3(-2.4418f, 1.04f, 6.2977f), Quaternion.Euler(0,270,0));

            // Add background pointable
            FVRPointable backgroundPointable = TNHMenu.transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;

            // Init refs
            TNHMenuPages = new GameObject[5];
            TNHMenuPages[0] = TNHMenu.transform.GetChild(1).gameObject;
            TNHMenuPages[1] = TNHMenu.transform.GetChild(2).gameObject;
            TNHMenuPages[2] = TNHMenu.transform.GetChild(3).gameObject;
            TNHMenuPages[3] = TNHMenu.transform.GetChild(4).gameObject;
            TNHMenuPages[4] = TNHMenu.transform.GetChild(5).gameObject;
            TNHStatusText = TNHMenu.transform.GetChild(6).GetChild(0).GetComponent<Text>();
            TNHInstanceList = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(0).GetChild(0).gameObject;
            TNHInstancePrefab = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(0).GetChild(0).GetChild(0).gameObject;
            TNHInstanceListScrollBar = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(1).GetComponent<Scrollbar>();
            TNHPlayerList = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).gameObject;
            TNHPlayerPrefab = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).GetChild(0).gameObject;
            TNHPlayerListScrollBar = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(1).GetComponent<Scrollbar>();
            TNHLPJCheckMark = TNHMenu.transform.GetChild(2).GetChild(1).GetChild(0).GetChild(0).gameObject;
            TNHHostOnDeathSpectateCheckMark = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(1).GetChild(0).gameObject;
            TNHHostOnDeathLeaveCheckMark = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(2).GetChild(0).gameObject;
            TNHJoinOnDeathSpectateCheckMark = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(1).GetChild(0).gameObject;
            TNHJoinOnDeathLeaveCheckMark = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(2).GetChild(0).gameObject;
            TNHInstanceTitle = TNHMenu.transform.GetChild(5).GetChild(0).GetComponent<Text>();

            // Init buttons
            TNHHostButton = TNHMenu.transform.GetChild(1).GetChild(1).gameObject;
            FVRPointableButton currentButton = TNHHostButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostClicked);
            TNHJoinButton = TNHMenu.transform.GetChild(1).GetChild(2).gameObject;
            currentButton = TNHJoinButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinClicked);
            TNHLPJCheck = TNHMenu.transform.GetChild(2).GetChild(1).GetChild(0).gameObject;
            currentButton = TNHLPJCheck.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHLPJCheckClicked);
            TNHHostOnDeathSpectateRadio = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(1).gameObject;
            currentButton = TNHHostOnDeathSpectateRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostOnDeathSpectateClicked);
            TNHHostOnDeathLeaveRadio = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(2).gameObject;
            currentButton = TNHHostOnDeathLeaveRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostOnDeathLeaveClicked);
            TNHHostConfirmButton = TNHMenu.transform.GetChild(2).GetChild(3).gameObject;
            currentButton = TNHHostConfirmButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostConfirmClicked);
            TNHHostCancelButton = TNHMenu.transform.GetChild(2).GetChild(4).gameObject;
            currentButton = TNHHostCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostCancelClicked);
            TNHJoinCancelButton = TNHMenu.transform.GetChild(3).GetChild(3).gameObject;
            currentButton = TNHJoinCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinCancelClicked);
            TNHInstanceListScrollUpArrow = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(2).gameObject;
            TNHInstanceListScrollDownArrow = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(3).gameObject;
            H3MP_HoverScroll upScroll = TNHInstanceListScrollUpArrow.AddComponent<H3MP_HoverScroll>();
            H3MP_HoverScroll downScroll = TNHInstanceListScrollDownArrow.AddComponent<H3MP_HoverScroll>();
            upScroll.MaxPointingRange = 5;
            upScroll.scrollbar = TNHInstanceListScrollBar;
            upScroll.other = downScroll;
            upScroll.up = true;
            upScroll.rate = 0.25f;
            downScroll.MaxPointingRange = 5;
            downScroll.scrollbar = TNHInstanceListScrollBar;
            downScroll.other = upScroll;
            downScroll.rate = 0.25f;
            TNHJoinOnDeathSpectateRadio = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(1).gameObject;
            currentButton = TNHJoinOnDeathSpectateRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinOnDeathSpectateClicked);
            TNHJoinOnDeathLeaveRadio = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(2).gameObject;
            currentButton = TNHJoinOnDeathLeaveRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinOnDeathLeaveClicked);
            TNHJoinConfirmButton = TNHMenu.transform.GetChild(3).GetChild(2).gameObject;
            currentButton = TNHJoinConfirmButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinConfirmClicked);
            TNHJoinOptionsCancelButton = TNHMenu.transform.GetChild(3).GetChild(3).gameObject;
            currentButton = TNHJoinOptionsCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinCancelClicked);
            TNHPlayerListScrollUpArrow = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(2).gameObject;
            TNHPlayerListScrollDownArrow = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(3).gameObject;
            upScroll = TNHPlayerListScrollUpArrow.AddComponent<H3MP_HoverScroll>();
            downScroll = TNHPlayerListScrollDownArrow.AddComponent<H3MP_HoverScroll>();
            upScroll.MaxPointingRange = 5;
            upScroll.scrollbar = TNHInstanceListScrollBar;
            upScroll.other = downScroll;
            upScroll.up = true;
            upScroll.rate = 0.25f;
            downScroll.MaxPointingRange = 5;
            downScroll.scrollbar = TNHInstanceListScrollBar;
            downScroll.other = upScroll;
            downScroll.rate = 0.25f;
            TNHJoinOptionsCancelButton = TNHMenu.transform.GetChild(5).GetChild(3).gameObject;
            currentButton = TNHJoinOptionsCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHDisconnectClicked);

            // Set option defaults
            TNHMenuLPJ = true;
            TNHOnDeathSpectate = true;

            // Get ref to the UI Manager
            Mod.currentTNHUIManager = GameObject.FindObjectOfType<TNH_UIManager>();

            // If already in a TNH isntance, which could be the case if we are coming back from being in game
            if(currentTNHInstance != null)
            {
                InitTNHUIManager(currentTNHInstance);
            }
        }

        public static void InitTNHUIManager(H3MP_TNHInstance instance)
        {
            Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(instance.progressionTypeSetting);
            GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)instance.progressionTypeSetting;
            Mod.currentTNHUIManager.OBS_EquipmentMode.SetSelectedButton(instance.equipmentModeSetting);
            GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)instance.equipmentModeSetting;
            Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(instance.healthModeSetting);
            GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)instance.healthModeSetting;
            Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(instance.targetModeSetting);
            GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)instance.targetModeSetting;
            Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(instance.AIDifficultyModifier);
            GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)instance.AIDifficultyModifier;
            Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(instance.radarModeModifier);
            GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)instance.radarModeModifier;
            Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(instance.itemSpawnerMode);
            GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)instance.itemSpawnerMode;
            Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(instance.backpackMode);
            GM.TNHOptions.BackpackMode = (TNH_BackpackMode)instance.backpackMode;
            Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(instance.healthMult);
            GM.TNHOptions.HealthMult = (TNH_HealthMult)instance.healthMult;
            Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(instance.sosiggunShakeReloading);
            GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)instance.sosiggunShakeReloading;
            Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(instance.TNHSeed + 1);
            GM.TNHOptions.TNHSeed = instance.TNHSeed;

            Mod.TNH_UIManager_m_currentLevelIndex.SetValue(Mod.currentTNHUIManager, instance.levelIndex);
            Mod.currentTNHUIManager.CurLevelID = Mod.currentTNHUIManager.Levels[instance.levelIndex].LevelID;
            Mod.TNH_UIManager_UpdateLevelSelectDisplayAndLoader.Invoke(Mod.currentTNHUIManager, null);
            Mod.TNH_UIManager_UpdateTableBasedOnOptions.Invoke(Mod.currentTNHUIManager, null);
            Mod.TNH_UIManager_PlayButtonSound.Invoke(Mod.currentTNHUIManager, new object[] { 2 });
        }

        private void LoadAssets()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile("BepInEx/Plugins/H3MP/H3MP.ab");

            H3MPMenuPrefab = assetBundle.LoadAsset<GameObject>("H3MPMenu");
            TNHMenuPrefab = assetBundle.LoadAsset<GameObject>("TNHMenu");
            reticleFriendlyContactArrowMat = assetBundle.LoadAsset<Material>("ReticleFriendlyContactArrowMat");
            reticleFriendlyContactIconMat = assetBundle.LoadAsset<Material>("ReticleFriendlyContactIconMat");

            playerPrefab = assetBundle.LoadAsset<GameObject>("Player");
            SetupPlayerPrefab();

            sosigWearableMap = JObject.Parse(File.ReadAllText("BepinEx/Plugins/H3MP/SosigWearableMap.json")).ToObject<Dictionary<string, string>>();

            TNHStartEquipButtonPrefab = assetBundle.LoadAsset<GameObject>("TNHStartEquipButton");
            FVRPointableButton startEquipButton = TNHStartEquipButtonPrefab.transform.GetChild(0).gameObject.AddComponent<FVRPointableButton>();
            startEquipButton.SetButton();
            startEquipButton.MaxPointingRange = 1;
        }

        // MOD: If you need to add anything to the player prefab, this is what you should patch to do it
        public void SetupPlayerPrefab()
        {
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

            // LoadLevelBeginPatch
            MethodInfo loadLevelBeginPatchOriginal = typeof(SteamVR_LoadLevel).GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);
            MethodInfo loadLevelBeginPatchPrefix = typeof(LoadLevelBeginPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadLevelBeginPatchOriginal, new HarmonyMethod(loadLevelBeginPatchPrefix));

            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPrefix = typeof(HandCurrentInteractableSetPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handCurrentInteractableSetPatchOriginal, new HarmonyMethod(handCurrentInteractableSetPatchPrefix), new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(setQuickBeltSlotPatchOriginal, null, new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            // SosigPickUpPatch
            MethodInfo sosigPickUpPatchOriginal = typeof(SosigHand).GetMethod("PickUp", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPickUpPatchPostfix = typeof(SosigPickUpPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigPickUpPatchOriginal, null, new HarmonyMethod(sosigPickUpPatchPostfix));

            // SosigPlaceObjectInPatch
            MethodInfo sosigPutObjectInPatchOriginal = typeof(SosigInventory.Slot).GetMethod("PlaceObjectIn", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPutObjectInPatchPostfix = typeof(SosigPlaceObjectInPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigPutObjectInPatchOriginal, null, new HarmonyMethod(sosigPutObjectInPatchPostfix));

            // SosigSlotDetachPatch
            MethodInfo sosigSlotDetachPatchOriginal = typeof(SosigInventory.Slot).GetMethod("DetachHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSlotDetachPatchPrefix = typeof(SosigSlotDetachPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigSlotDetachPatchOriginal, new HarmonyMethod(sosigSlotDetachPatchPrefix));

            // SosigHandDropPatch
            MethodInfo sosigHandDropPatchOriginal = typeof(SosigHand).GetMethod("DropHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandThrowPatchOriginal = typeof(SosigHand).GetMethod("ThrowObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandDropPatchPrefix = typeof(SosigHandDropPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigHandDropPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));
            harmony.Patch(sosigHandThrowPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));

            // FirePatch
            MethodInfo firePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo firePatchPrefix = typeof(FirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo firePatchPostfix = typeof(FirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(firePatchOriginal, new HarmonyMethod(firePatchPrefix), new HarmonyMethod(firePatchPostfix));

            // SosigConfigurePatch
            MethodInfo sosigConfigurePatchOriginal = typeof(Sosig).GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigConfigurePatchPrefix = typeof(SosigConfigurePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigConfigurePatchOriginal, new HarmonyMethod(sosigConfigurePatchPrefix));

            // SosigUpdatePatch
            MethodInfo sosigUpdatePatchOriginal = typeof(Sosig).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigHandPhysUpdatePatchOriginal = typeof(Sosig).GetMethod("HandPhysUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigHandPhysUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("HandPhysUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigUpdatePatchOriginal, new HarmonyMethod(sosigUpdatePatchPrefix));
            harmony.Patch(sosigHandPhysUpdatePatchOriginal, new HarmonyMethod(sosigHandPhysUpdatePatchPrefix));
            // AutoMeaterUpdatePatch
            MethodInfo autoMeaterUpdatePatchOriginal = typeof(AutoMeater).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFixedUpdatePatchOriginal = typeof(AutoMeater).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdatePatchPrefix = typeof(AutoMeaterUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));
            harmony.Patch(autoMeaterFixedUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));

            // AutoMeaterEventPatch
            MethodInfo autoMeaterEventReceivePatchOriginal = typeof(AutoMeater).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterEventReceivePatchPrefix = typeof(AutoMeaterEventPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterEventReceivePatchOriginal, new HarmonyMethod(autoMeaterEventReceivePatchPrefix));

            // AutoMeaterSetStatePatch
            MethodInfo autoMeaterSetStatePatchOriginal = typeof(AutoMeater).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterSetStatePatchPostfix = typeof(AutoMeaterSetStatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterSetStatePatchOriginal, null, new HarmonyMethod(autoMeaterSetStatePatchPostfix));

            // AutoMeaterUpdateFlightPatch
            MethodInfo autoMeaterUpdateFlightPatchOriginal = typeof(AutoMeater).GetMethod("UpdateFlight", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdateFlightPatchPrefix = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterUpdateFlightPatchTranspiler = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterUpdateFlightPatchOriginal, new HarmonyMethod(autoMeaterUpdateFlightPatchPrefix), null, new HarmonyMethod(autoMeaterUpdateFlightPatchTranspiler));

            // InventoryUpdatePatch
            MethodInfo sosigInvUpdatePatchOriginal = typeof(SosigInventory).GetMethod("PhysHold", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigInvUpdatePatchPrefix = typeof(SosigInvUpdatePatch).GetMethod("PhysHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigInvUpdatePatchOriginal, new HarmonyMethod(sosigInvUpdatePatchPrefix));

            // SosigLinkActionPatch
            MethodInfo sosigLinkRegisterWearablePatchOriginal = typeof(SosigLink).GetMethod("RegisterWearable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkRegisterWearablePatchPrefix = typeof(SosigLinkActionPatch).GetMethod("RegisterWearablePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkDeRegisterWearablePatchOriginal = typeof(SosigLink).GetMethod("DeRegisterWearable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkDeRegisterWearablePatchPrefix = typeof(SosigLinkActionPatch).GetMethod("DeRegisterWearablePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkExplodesPatchOriginal = typeof(SosigLink).GetMethod("LinkExplodes", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkExplodesPatchPrefix = typeof(SosigLinkActionPatch).GetMethod("LinkExplodesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkExplodesPatchPosfix = typeof(SosigLinkActionPatch).GetMethod("LinkExplodesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkBreakPatchOriginal = typeof(SosigLink).GetMethod("BreakJoint", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkBreakPatchPrefix = typeof(SosigLinkActionPatch).GetMethod("LinkBreakPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkBreakPatchPosfix = typeof(SosigLinkActionPatch).GetMethod("LinkBreakPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkSeverPatchOriginal = typeof(SosigLink).GetMethod("SeverJoint", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigLinkSeverPatchPrefix = typeof(SosigLinkActionPatch).GetMethod("LinkSeverPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkSeverPatchPosfix = typeof(SosigLinkActionPatch).GetMethod("LinkSeverPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkVaporizePatchOriginal = typeof(SosigLink).GetMethod("Vaporize", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkVaporizePatchPrefix = typeof(SosigLinkActionPatch).GetMethod("LinkVaporizePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkVaporizePatchPosfix = typeof(SosigLinkActionPatch).GetMethod("LinkVaporizePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigLinkRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkRegisterWearablePatchPrefix));
            harmony.Patch(sosigLinkDeRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkDeRegisterWearablePatchPrefix));
            harmony.Patch(sosigLinkExplodesPatchOriginal, new HarmonyMethod(sosigLinkExplodesPatchPrefix), new HarmonyMethod(sosigLinkExplodesPatchPosfix));
            harmony.Patch(sosigLinkBreakPatchOriginal, new HarmonyMethod(sosigLinkBreakPatchPrefix), new HarmonyMethod(sosigLinkBreakPatchPosfix));
            harmony.Patch(sosigLinkSeverPatchOriginal, new HarmonyMethod(sosigLinkSeverPatchPrefix), new HarmonyMethod(sosigLinkSeverPatchPosfix));
            harmony.Patch(sosigLinkVaporizePatchOriginal, new HarmonyMethod(sosigLinkVaporizePatchPrefix), new HarmonyMethod(sosigLinkVaporizePatchPosfix));

            // SosigActionPatch
            MethodInfo sosigDiesPatchOriginal = typeof(Sosig).GetMethod("SosigDies", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigDiesPatchPrefix = typeof(SosigActionPatch).GetMethod("SosigDiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigDiesPatchPosfix = typeof(SosigActionPatch).GetMethod("SosigDiesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigBodyStatePatchOriginal = typeof(Sosig).GetMethod("SetBodyState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigBodyStatePatchPrefix = typeof(SosigActionPatch).GetMethod("SetBodyStatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigBodyUpdatePatchOriginal = typeof(Sosig).GetMethod("BodyUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigBodyUpdatePatchTranspiler = typeof(SosigActionPatch).GetMethod("FootStepTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSpeechUpdatePatchOriginal = typeof(Sosig).GetMethod("SpeechUpdate_State", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigSpeechUpdatePatchTranspiler = typeof(SosigActionPatch).GetMethod("SpeechUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSetCurrentOrderPatchOriginal = typeof(Sosig).GetMethod("SetCurrentOrder", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetCurrentOrderPatchPrefix = typeof(SosigActionPatch).GetMethod("SetCurrentOrderPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigVaporizePatchOriginal = typeof(Sosig).GetMethod("Vaporize", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigVaporizePatchPrefix = typeof(SosigActionPatch).GetMethod("SosigVaporizePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigVaporizePatchPostfix = typeof(SosigActionPatch).GetMethod("SosigVaporizePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigRequestHitDecalPatchOriginal = typeof(Sosig).GetMethod("RequestHitDecal", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(SosigLink) }, null);
            MethodInfo sosigRequestHitDecalPatchPrefix = typeof(SosigActionPatch).GetMethod("RequestHitDecalPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigRequestHitDecalEdgePatchOriginal = typeof(Sosig).GetMethod("RequestHitDecal", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(float), typeof(SosigLink) }, null);
            MethodInfo sosigRequestHitDecalEdgePatchPrefix = typeof(SosigActionPatch).GetMethod("RequestHitDecalEdgePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigDiesPatchOriginal, new HarmonyMethod(sosigDiesPatchPrefix), new HarmonyMethod(sosigDiesPatchPosfix));
            harmony.Patch(sosigBodyStatePatchOriginal, new HarmonyMethod(sosigBodyStatePatchPrefix));
            harmony.Patch(sosigBodyUpdatePatchOriginal, null, null, new HarmonyMethod(sosigBodyUpdatePatchTranspiler));
            harmony.Patch(sosigSpeechUpdatePatchOriginal, null, null, new HarmonyMethod(sosigSpeechUpdatePatchTranspiler));
            harmony.Patch(sosigSetCurrentOrderPatchOriginal, new HarmonyMethod(sosigSetCurrentOrderPatchPrefix));
            harmony.Patch(sosigVaporizePatchOriginal, new HarmonyMethod(sosigVaporizePatchPrefix), new HarmonyMethod(sosigVaporizePatchPostfix));
            harmony.Patch(sosigRequestHitDecalPatchOriginal, new HarmonyMethod(sosigRequestHitDecalPatchPrefix));
            harmony.Patch(sosigRequestHitDecalEdgePatchOriginal, new HarmonyMethod(sosigRequestHitDecalEdgePatchPrefix));

            // SosigIFFPatch
            MethodInfo sosigSetIFFPatchOriginal = typeof(Sosig).GetMethod("SetIFF", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSetOriginalIFFPatchOriginal = typeof(Sosig).GetMethod("SetOriginalIFFTeam", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetOriginalIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetOriginalIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigSetIFFPatchOriginal, new HarmonyMethod(sosigSetIFFPatchPrefix));
            harmony.Patch(sosigSetOriginalIFFPatchOriginal, new HarmonyMethod(sosigSetOriginalIFFPatchPrefix));

            // SosigEventReceivePatch
            MethodInfo sosigEventReceivePatchOriginal = typeof(Sosig).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigEventReceivePatchPrefix = typeof(SosigEventReceivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigEventReceivePatchOriginal, new HarmonyMethod(sosigEventReceivePatchPrefix));

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
            MethodInfo loadDefaultSceneRoutinePatchOriginal = typeof(FVRSceneSettings).GetMethod("LoadDefaultSceneRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
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
            MethodInfo ballisticProjectileDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo ballisticProjectileDamageablePatchTranspiler = typeof(BallisticProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(ballisticProjectileDamageablePatchOriginal, null, null, new HarmonyMethod(ballisticProjectileDamageablePatchTranspiler));

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
            MethodInfo pinnedGrenadeDamageablePatchOriginal = typeof(PinnedGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadeDamageablePatchTranspiler = typeof(PinnedGrenadeDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(pinnedGrenadeDamageablePatchOriginal, null, null, new HarmonyMethod(pinnedGrenadeDamageablePatchTranspiler));

            // PinnedGrenadeCollisionDamageablePatch
            MethodInfo pinnedGrenadeCollisionDamageablePatchOriginal = typeof(PinnedGrenade).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo pinnedGrenadeCollisionDamageablePatchTranspiler = typeof(PinnedGrenadeCollisionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(pinnedGrenadeCollisionDamageablePatchOriginal, null, null, new HarmonyMethod(pinnedGrenadeCollisionDamageablePatchTranspiler));

            // SosigWeaponDamageablePatch
            MethodInfo sosigWeaponDamageablePatchOriginal = typeof(SosigWeapon).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("ExplosionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchCollisionOriginal = typeof(SosigWeapon).GetMethod("DoMeleeDamageInCollision", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchCollisionTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchUpdateOriginal = typeof(SosigWeapon).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchUpdateTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigWeaponDamageablePatchOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchTranspiler));
            harmony.Patch(sosigWeaponDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchCollisionTranspiler));
            harmony.Patch(sosigWeaponDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchUpdateTranspiler));

            // MeleeParamsDamageablePatch
            MethodInfo meleeParamsDamageablePatchStabOriginal = typeof(FVRPhysicalObject.MeleeParams).GetMethod("DoStabDamage", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchStabTranspiler = typeof(MeleeParamsDamageablePatch).GetMethod("StabTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meleeParamsDamageablePatchTearOriginal = typeof(FVRPhysicalObject.MeleeParams).GetMethod("DoTearOutDamage", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchTearTranspiler = typeof(MeleeParamsDamageablePatch).GetMethod("TearOutTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meleeParamsDamageablePatchUpdateOriginal = typeof(FVRPhysicalObject.MeleeParams).GetMethod("FixedUpdate", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchUpdateTranspiler = typeof(MeleeParamsDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meleeParamsDamageablePatchUpdatePrefix = typeof(MeleeParamsDamageablePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meleeParamsDamageablePatchCollisionOriginal = typeof(FVRPhysicalObject.MeleeParams).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchCollisionTranspiler = typeof(MeleeParamsDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(meleeParamsDamageablePatchStabOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchStabTranspiler));
            harmony.Patch(meleeParamsDamageablePatchTearOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchTearTranspiler));
            harmony.Patch(meleeParamsDamageablePatchUpdateOriginal, new HarmonyMethod(meleeParamsDamageablePatchUpdatePrefix), null, new HarmonyMethod(meleeParamsDamageablePatchUpdateTranspiler));
            harmony.Patch(meleeParamsDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchCollisionTranspiler));

            // AIMeleeDamageablePatch
            MethodInfo meleeParamsDamageablePatchFireOriginal = typeof(AIMeleeWeapon).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchFireTranspiler = typeof(AIMeleeDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(meleeParamsDamageablePatchFireOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchFireTranspiler));

            // AutoMeaterBladeDamageablePatch
            MethodInfo autoMeaterBladeDamageablePatchCollisionOriginal = typeof(AutoMeaterBlade).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterBladeDamageablePatchCollisionTranspiler = typeof(AutoMeaterBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(autoMeaterBladeDamageablePatchCollisionTranspiler));

            // BangSnapDamageablePatch
            MethodInfo bangSnapDamageablePatchCollisionOriginal = typeof(BangSnap).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bangSnapDamageablePatchCollisionTranspiler = typeof(BangSnapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(bangSnapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(bangSnapDamageablePatchCollisionTranspiler));

            // BearTrapDamageablePatch
            MethodInfo bearTrapDamageablePatchSnapOriginal = typeof(BearTrapInteractiblePiece).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bearTrapDamageablePatchSnapTranspiler = typeof(BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(bearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(bearTrapDamageablePatchSnapTranspiler));

            // ChainsawDamageablePatch
            MethodInfo chainsawDamageablePatchCollisionOriginal = typeof(Chainsaw).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo chainsawDamageablePatchCollisionTranspiler = typeof(ChainsawDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(chainsawDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(chainsawDamageablePatchCollisionTranspiler));

            // DrillDamageablePatch
            MethodInfo drillDamageablePatchCollisionOriginal = typeof(Drill).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drillDamageablePatchCollisionTranspiler = typeof(DrillDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(drillDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(drillDamageablePatchCollisionTranspiler));

            // DropTrapDamageablePatch
            MethodInfo dropTrapDamageablePatchCollisionOriginal = typeof(DropTrapLogs).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dropTrapDamageablePatchCollisionTranspiler = typeof(DropTrapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(dropTrapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(dropTrapDamageablePatchCollisionTranspiler));

            // FlipzoDamageablePatch
            MethodInfo flipzoDamageablePatchUpdateOriginal = typeof(Flipzo).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flipzoDamageablePatchUpdateTranspiler = typeof(FlipzoDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(flipzoDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(flipzoDamageablePatchUpdateTranspiler));

            // IgnitableDamageablePatch
            MethodInfo ignitableDamageablePatchStartOriginal = typeof(FVRIgnitable).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo ignitableDamageablePatchStartTranspiler = typeof(IgnitableDamageablePatch).GetMethod("StartTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(ignitableDamageablePatchStartOriginal, null, null, new HarmonyMethod(ignitableDamageablePatchStartTranspiler));

            // SparklerDamageablePatch
            MethodInfo sparklerDamageablePatchCollisionOriginal = typeof(FVRSparkler).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sparklerDamageablePatchCollisionTranspiler = typeof(SparklerDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sparklerDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(sparklerDamageablePatchCollisionTranspiler));

            // MatchDamageablePatch
            MethodInfo matchDamageablePatchCollisionOriginal = typeof(FVRStrikeAnyWhereMatch).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo matchDamageablePatchCollisionTranspiler = typeof(MatchDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(matchDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(matchDamageablePatchCollisionTranspiler));

            // HCBBoltDamageablePatch
            MethodInfo HCBBoltDamageablePatchDamageOriginal = typeof(HCBBolt).GetMethod("DamageOtherThing", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo HCBBoltDamageablePatchDamageTranspiler = typeof(HCBBoltDamageablePatch).GetMethod("DamageOtherTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(HCBBoltDamageablePatchDamageOriginal, null, null, new HarmonyMethod(HCBBoltDamageablePatchDamageTranspiler));

            // KabotDamageablePatch
            MethodInfo kabotDamageablePatchTickOriginal = typeof(Kabot.KSpike).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo kabotDamageablePatchTickTranspiler = typeof(KabotDamageablePatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(kabotDamageablePatchTickOriginal, null, null, new HarmonyMethod(kabotDamageablePatchTickTranspiler));

            // MeatCrabDamageablePatch
            MethodInfo meatCrabDamageablePatchLungingOriginal = typeof(MeatCrab).GetMethod("Crabdate_Lunging", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchLungingTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("LungingTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meatCrabDamageablePatchAttachedOriginal = typeof(MeatCrab).GetMethod("Crabdate_Attached", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchAttachedTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("AttachedTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(meatCrabDamageablePatchLungingOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchLungingTranspiler));
            harmony.Patch(meatCrabDamageablePatchAttachedOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchAttachedTranspiler));

            // MF2_BearTrapDamageablePatch
            MethodInfo MF2_BearTrapDamageablePatchSnapOriginal = typeof(MF2_BearTrapInteractionZone).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo MF2_BearTrapDamageablePatchSnapTranspiler = typeof(MF2_BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(MF2_BearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(MF2_BearTrapDamageablePatchSnapTranspiler));

            // MG_SwarmDamageablePatch
            MethodInfo MG_SwarmDamageablePatchFireOriginal = typeof(MG_FlyingHotDogSwarm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_SwarmDamageablePatchFireTranspiler = typeof(MG_SwarmDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(MG_SwarmDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_SwarmDamageablePatchFireTranspiler));

            // MG_JerryDamageablePatch
            MethodInfo MG_JerryDamageablePatchFireOriginal = typeof(MG_JerryTheLemon).GetMethod("FireBolt", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_JerryDamageablePatchFireTranspiler = typeof(MG_JerryDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(MG_JerryDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_JerryDamageablePatchFireTranspiler));

            // MicrotorchDamageablePatch
            MethodInfo microtorchDamageablePatchUpdateOriginal = typeof(Microtorch).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo microtorchDamageablePatchUpdateTranspiler = typeof(MicrotorchDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(microtorchDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(microtorchDamageablePatchUpdateTranspiler));

            // CyclopsDamageablePatch
            MethodInfo cyclopsDamageablePatchUpdateOriginal = typeof(PowerUp_Cyclops).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo cyclopsDamageablePatchUpdateTranspiler = typeof(CyclopsDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(cyclopsDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(cyclopsDamageablePatchUpdateTranspiler));

            // LaserSwordDamageablePatch
            MethodInfo laserSwordDamageablePatchUpdateOriginal = typeof(RealisticLaserSword).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo laserSwordDamageablePatchUpdateTranspiler = typeof(LaserSwordDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(laserSwordDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(laserSwordDamageablePatchUpdateTranspiler));

            // CharcoalDamageablePatch
            MethodInfo charcoalDamageablePatchCharcoalOriginal = typeof(RotrwCharcoal).GetMethod("DamageBubble", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo charcoalDamageablePatchCharcoalTranspiler = typeof(CharcoalDamageablePatch).GetMethod("BubbleTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(charcoalDamageablePatchCharcoalOriginal, null, null, new HarmonyMethod(charcoalDamageablePatchCharcoalTranspiler));

            // SlicerDamageablePatch
            MethodInfo slicerDamageablePatchUpdateOriginal = typeof(SlicerBladeMaster).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo slicerDamageablePatchUpdateTranspiler = typeof(SlicerDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(slicerDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(slicerDamageablePatchUpdateTranspiler));

            // SpinningBladeDamageablePatch
            MethodInfo spinningBladeDamageablePatchCollisionOriginal = typeof(SpinningBladeTrapBase).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo spinningBladeDamageablePatchCollisionTranspiler = typeof(SpinningBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(spinningBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(spinningBladeDamageablePatchCollisionTranspiler));

            // ProjectileDamageablePatch
            MethodInfo projectileDamageablePatchOriginal = typeof(FVRProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo projectileBladeDamageablePatchTranspiler = typeof(ProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(projectileDamageablePatchOriginal, null, null, new HarmonyMethod(projectileBladeDamageablePatchTranspiler));

            // SosigLinkDamagePatch
            MethodInfo sosigLinkDamagePatchOriginal = typeof(SosigLink).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkDamagePatchPrefix = typeof(SosigLinkDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkDamagePatchPostfix = typeof(SosigLinkDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigLinkDamagePatchOriginal, new HarmonyMethod(sosigLinkDamagePatchPrefix), new HarmonyMethod(sosigLinkDamagePatchPostfix));

            // SosigWearableDamagePatch
            MethodInfo sosigWearableDamagePatchOriginal = typeof(SosigWearable).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWearableDamagePatchPrefix = typeof(SosigWearableDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWearableDamagePatchPostfix = typeof(SosigWearableDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigWearableDamagePatchOriginal, new HarmonyMethod(sosigWearableDamagePatchPrefix), new HarmonyMethod(sosigWearableDamagePatchPostfix));

            // AutoMeaterDamagePatch
            MethodInfo autoMeaterDamagePatchOriginal = typeof(AutoMeater).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterDamagePatchPrefix = typeof(AutoMeaterDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterDamagePatchOriginal, new HarmonyMethod(autoMeaterDamagePatchPrefix));

            // AutoMeaterHitZoneDamagePatch
            MethodInfo autoMeaterHitZoneDamagePatchOriginal = typeof(AutoMeaterHitZone).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterHitZoneDamagePatchPrefix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterHitZoneDamagePatchPostfix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterHitZoneDamagePatchOriginal, new HarmonyMethod(autoMeaterHitZoneDamagePatchPrefix), new HarmonyMethod(autoMeaterHitZoneDamagePatchPostfix));

            // AutoMeaterFirearmFireShotPatch
            MethodInfo autoMeaterFirearmFireShotPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireShotPatchPrefix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchPostfix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchTranspiler = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterFirearmFireShotPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireShotPatchPrefix), new HarmonyMethod(autoMeaterFirearmFireShotPatchPostfix), new HarmonyMethod(autoMeaterFirearmFireShotPatchTranspiler));

            // AutoMeaterFirearmFireAtWillPatch
            MethodInfo autoMeaterFirearmFireAtWillPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("SetFireAtWill", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireAtWillPatchPrefix = typeof(AutoMeaterFirearmFireAtWillPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(autoMeaterFirearmFireAtWillPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireAtWillPatchPrefix));

            // EncryptionDamagePatch
            MethodInfo encryptionDamagePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDamagePatchPrefix = typeof(EncryptionDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDamagePatchPostfix = typeof(EncryptionDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionDamagePatchOriginal, new HarmonyMethod(encryptionDamagePatchPrefix), new HarmonyMethod(encryptionDamagePatchPostfix));

            // EncryptionRespawnRandSubPatch
            MethodInfo encryptionRespawnRandSubPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("RespawnRandomSubTarg", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionRespawnRandSubPatchTranspiler = typeof(EncryptionRespawnRandSubPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionRespawnRandSubPatchOriginal, null, null, new HarmonyMethod(encryptionRespawnRandSubPatchTranspiler));

            // EncryptionPopulateInitialRegenPatch
            MethodInfo encryptionPopulateInitialRegenPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("PopulateInitialRegen", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionPopulateInitialRegenPatchPrefix = typeof(EncryptionPopulateInitialRegenPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionPopulateInitialRegenPatchOriginal, new HarmonyMethod(encryptionPopulateInitialRegenPatchPrefix));

            // EncryptionStartPatch
            MethodInfo encryptionStartPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionStartPatchPrefix = typeof(EncryptionStartPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionStartPatchPostfix = typeof(EncryptionStartPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionStartPatchOriginal, new HarmonyMethod(encryptionStartPatchPrefix), new HarmonyMethod(encryptionStartPatchPostfix));

            // EncryptionResetGrowthPatch
            MethodInfo encryptionResetGrowthPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("ResetGrowth", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionResetGrowthPatchPrefix = typeof(EncryptionResetGrowthPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionResetGrowthPatchOriginal, new HarmonyMethod(encryptionResetGrowthPatchPrefix));

            // EncryptionDisableSubtargPatch
            MethodInfo encryptionDisableSubtargPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("DisableSubtarg", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDisableSubtargPatchPrefix = typeof(EncryptionDisableSubtargPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDisableSubtargPatchPostfix = typeof(EncryptionDisableSubtargPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionDisableSubtargPatchOriginal, new HarmonyMethod(encryptionDisableSubtargPatchPrefix), new HarmonyMethod(encryptionDisableSubtargPatchPostfix));

            // EncryptionUpdatePatch
            MethodInfo encryptionUpdatePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionUpdatePatchPrefix = typeof(EncryptionUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionUpdatePatchOriginal, new HarmonyMethod(encryptionUpdatePatchPrefix));

            // EncryptionFixedUpdatePatch
            MethodInfo encryptionFixedUpdatePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionFixedUpdatePatchPrefix = typeof(EncryptionFixedUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionFixedUpdatePatchOriginal, new HarmonyMethod(encryptionFixedUpdatePatchPrefix));

            // EncryptionSubDamagePatch
            MethodInfo encryptionSubDamagePatchOriginal = typeof(TNH_EncryptionTarget_SubTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionSubDamagePatchPrefix = typeof(EncryptionSubDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(encryptionSubDamagePatchOriginal, new HarmonyMethod(encryptionSubDamagePatchPrefix));

            // SetTNHManagerPatch
            MethodInfo setTNHManagerPatchOriginal = typeof(GM).GetMethod("set_TNH_Manager", BindingFlags.Public | BindingFlags.Static);
            MethodInfo setTNHManagerPatchPostfix = typeof(SetTNHManagerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(setTNHManagerPatchOriginal, null, new HarmonyMethod(setTNHManagerPatchPostfix));

            // TNH_TokenPatch
            MethodInfo TNH_TokenPatchPatchCollectOriginal = typeof(TNH_Token).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_TokenPatchPatchCollectPrefix = typeof(TNH_TokenPatch).GetMethod("CollectPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_TokenPatchPatchCollectPostfix = typeof(TNH_TokenPatch).GetMethod("CollectPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_TokenPatchPatchCollectOriginal, new HarmonyMethod(TNH_TokenPatchPatchCollectPrefix), new HarmonyMethod(TNH_TokenPatchPatchCollectPostfix));

            // TNH_ShatterableCrateDamagePatch
            MethodInfo TNH_ShatterableCrateDamagePatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDamagePatchPrefix = typeof(TNH_ShatterableCrateDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_ShatterableCrateDamagePatchOriginal, new HarmonyMethod(TNH_ShatterableCrateDamagePatchPrefix));

            // TNH_ShatterableCrateDestroyPatch
            MethodInfo TNH_ShatterableCrateDestroyPatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Destroy", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDestroyPatchPrefix = typeof(TNH_ShatterableCrateDestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_ShatterableCrateDestroyPatchOriginal, new HarmonyMethod(TNH_ShatterableCrateDestroyPatchPrefix));

            // TNH_UIManagerPatch
            MethodInfo TNH_UIManagerPatchProgressionOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_Progression", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchProgressionPrefix = typeof(TNH_UIManagerPatch).GetMethod("ProgressionPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchEquipmentOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_EquipmentMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchEquipmentPrefix = typeof(TNH_UIManagerPatch).GetMethod("EquipmentPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchHealthModeOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_HealthMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchHealthModePrefix = typeof(TNH_UIManagerPatch).GetMethod("HealthModePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchTargetModeOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_TargetMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchTargetModePrefix = typeof(TNH_UIManagerPatch).GetMethod("TargetPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchAIDifficultyOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_AIDifficulty", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchAIDifficultyPrefix = typeof(TNH_UIManagerPatch).GetMethod("AIDifficultyPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchRadarModeOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_AIRadarMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchRadarModePrefix = typeof(TNH_UIManagerPatch).GetMethod("RadarModePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchItemSpawnerModeOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_ItemSpawner", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchItemSpawnerModePrefix = typeof(TNH_UIManagerPatch).GetMethod("ItemSpawnerModePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchBackpackModeOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_Backpack", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchBackpackModePrefix = typeof(TNH_UIManagerPatch).GetMethod("BackpackModePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchHealthMultOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_HealthMult", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchHealthMultPrefix = typeof(TNH_UIManagerPatch).GetMethod("HealthMultPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchSosigGunReloadOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_SosiggunShakeReloading", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchSosigGunReloadPrefix = typeof(TNH_UIManagerPatch).GetMethod("SosigGunReloadPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchSeedOriginal = typeof(TNH_UIManager).GetMethod("SetOBS_RunSeed", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchSeedPrefix = typeof(TNH_UIManagerPatch).GetMethod("SeedPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchNextLevelOriginal = typeof(TNH_UIManager).GetMethod("BTN_NextLevel", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchNextLevelPrefix = typeof(TNH_UIManagerPatch).GetMethod("NextLevelPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_UIManagerPatchPrevLevelOriginal = typeof(TNH_UIManager).GetMethod("BTN_PrevLevel", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_UIManagerPatchPrevLevelPrefix = typeof(TNH_UIManagerPatch).GetMethod("PrevLevelPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_UIManagerPatchProgressionOriginal, new HarmonyMethod(TNH_UIManagerPatchProgressionPrefix));
            harmony.Patch(TNH_UIManagerPatchEquipmentOriginal, new HarmonyMethod(TNH_UIManagerPatchEquipmentPrefix));
            harmony.Patch(TNH_UIManagerPatchHealthModeOriginal, new HarmonyMethod(TNH_UIManagerPatchHealthModePrefix));
            harmony.Patch(TNH_UIManagerPatchTargetModeOriginal, new HarmonyMethod(TNH_UIManagerPatchTargetModePrefix));
            harmony.Patch(TNH_UIManagerPatchAIDifficultyOriginal, new HarmonyMethod(TNH_UIManagerPatchAIDifficultyPrefix));
            harmony.Patch(TNH_UIManagerPatchRadarModeOriginal, new HarmonyMethod(TNH_UIManagerPatchRadarModePrefix));
            harmony.Patch(TNH_UIManagerPatchItemSpawnerModeOriginal, new HarmonyMethod(TNH_UIManagerPatchItemSpawnerModePrefix));
            harmony.Patch(TNH_UIManagerPatchBackpackModeOriginal, new HarmonyMethod(TNH_UIManagerPatchBackpackModePrefix));
            harmony.Patch(TNH_UIManagerPatchHealthMultOriginal, new HarmonyMethod(TNH_UIManagerPatchHealthMultPrefix));
            harmony.Patch(TNH_UIManagerPatchSosigGunReloadOriginal, new HarmonyMethod(TNH_UIManagerPatchSosigGunReloadPrefix));
            harmony.Patch(TNH_UIManagerPatchSeedOriginal, new HarmonyMethod(TNH_UIManagerPatchSeedPrefix));
            harmony.Patch(TNH_UIManagerPatchNextLevelOriginal, new HarmonyMethod(TNH_UIManagerPatchNextLevelPrefix));
            harmony.Patch(TNH_UIManagerPatchPrevLevelOriginal, new HarmonyMethod(TNH_UIManagerPatchPrevLevelPrefix));

            // TNH_ManagerPatch
            MethodInfo TNH_ManagerPatchPlayerDiedOriginal = typeof(TNH_Manager).GetMethod("PlayerDied", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchPlayerDiedPrefix = typeof(TNH_ManagerPatch).GetMethod("PlayerDiedPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchAddTokensOriginal = typeof(TNH_Manager).GetMethod("AddTokens", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchAddTokensPrefix = typeof(TNH_ManagerPatch).GetMethod("AddTokensPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSosigKillOriginal = typeof(TNH_Manager).GetMethod("OnSosigKill", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSosigKillPrefix = typeof(TNH_ManagerPatch).GetMethod("OnSosigKillPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseOriginal = typeof(TNH_Manager).GetMethod("SetPhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetPhasePrefix = typeof(TNH_ManagerPatch).GetMethod("SetPhasePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchUpdateOriginal = typeof(TNH_Manager).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchUpdatePrefix = typeof(TNH_ManagerPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchUpdatePostfix = typeof(TNH_ManagerPatch).GetMethod("UpdatePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchInitBeginEquipOriginal = typeof(TNH_Manager).GetMethod("InitBeginningEquipment", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchInitBeginEquipPrefix = typeof(TNH_ManagerPatch).GetMethod("InitBeginEquipPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseTakeOriginal = typeof(TNH_Manager).GetMethod("SetPhase_Take", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetPhaseTakePrefix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseTakePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseTakePostfix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseTakePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseCompleteOriginal = typeof(TNH_Manager).GetMethod("SetPhase_Completed", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetPhaseCompletePostfix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseCompletePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetLevelOriginal = typeof(TNH_Manager).GetMethod("SetLevel", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetLevelPrefix = typeof(TNH_ManagerPatch).GetMethod("SetLevelPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchOnShotFiredOriginal = typeof(TNH_Manager).GetMethod("OnShotFired", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchOnShotFiredPrefix = typeof(TNH_ManagerPatch).GetMethod("OnShotFiredPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchOnBotShotFiredOriginal = typeof(TNH_Manager).GetMethod("OnBotShotFired", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchOnBotShotFiredPrefix = typeof(TNH_ManagerPatch).GetMethod("OnBotShotFiredPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchAddFVRObjectToTrackedListOriginal = typeof(TNH_Manager).GetMethod("AddFVRObjectToTrackedList", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchAddFVRObjectToTrackedListPrefix = typeof(TNH_ManagerPatch).GetMethod("AddFVRObjectToTrackedListPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_ManagerPatchPlayerDiedOriginal, new HarmonyMethod(TNH_ManagerPatchPlayerDiedPrefix));
            harmony.Patch(TNH_ManagerPatchAddTokensOriginal, new HarmonyMethod(TNH_ManagerPatchAddTokensPrefix));
            harmony.Patch(TNH_ManagerPatchSosigKillOriginal, new HarmonyMethod(TNH_ManagerPatchSosigKillPrefix));
            harmony.Patch(TNH_ManagerPatchSetPhaseOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhasePrefix));
            harmony.Patch(TNH_ManagerPatchUpdateOriginal, new HarmonyMethod(TNH_ManagerPatchUpdatePrefix), new HarmonyMethod(TNH_ManagerPatchUpdatePostfix));
            harmony.Patch(TNH_ManagerPatchInitBeginEquipOriginal, new HarmonyMethod(TNH_ManagerPatchInitBeginEquipPrefix));
            harmony.Patch(TNH_ManagerPatchSetLevelOriginal, new HarmonyMethod(TNH_ManagerPatchSetLevelPrefix));
            harmony.Patch(TNH_ManagerPatchSetPhaseTakeOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhaseTakePrefix), new HarmonyMethod(TNH_ManagerPatchSetPhaseTakePostfix));
            harmony.Patch(TNH_ManagerPatchSetPhaseCompleteOriginal, null, new HarmonyMethod(TNH_ManagerPatchSetPhaseCompletePostfix));
            harmony.Patch(TNH_ManagerPatchOnShotFiredOriginal, new HarmonyMethod(TNH_ManagerPatchOnShotFiredPrefix));
            harmony.Patch(TNH_ManagerPatchOnBotShotFiredOriginal, new HarmonyMethod(TNH_ManagerPatchOnBotShotFiredPrefix));
            harmony.Patch(TNH_ManagerPatchAddFVRObjectToTrackedListOriginal, new HarmonyMethod(TNH_ManagerPatchAddFVRObjectToTrackedListPrefix));

            // TAHReticleContactPatch
            MethodInfo TAHReticleContactPatchTickOriginal = typeof(TAH_ReticleContact).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchTickTranspiler = typeof(TAHReticleContactPatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TAHReticleContactPatchSetContactTypeOriginal = typeof(TAH_ReticleContact).GetMethod("SetContactType", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchSetContactTypePrefix = typeof(TAHReticleContactPatch).GetMethod("SetContactTypePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TAHReticleContactPatchTickOriginal, null ,null, new HarmonyMethod(TAHReticleContactPatchTickTranspiler));
            harmony.Patch(TAHReticleContactPatchSetContactTypeOriginal, new HarmonyMethod(TAHReticleContactPatchSetContactTypePrefix));

            // TNH_HoldPointPatch
            MethodInfo TNH_HoldPointPatchSystemNodeOriginal = typeof(TNH_HoldPoint).GetMethod("ConfigureAsSystemNode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSystemNodePrefix = typeof(TNH_HoldPointPatch).GetMethod("ConfigureAsSystemNodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTakeChallengeEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTakeChallengeEntitiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginHoldOriginal = typeof(TNH_HoldPoint).GetMethod("BeginHoldChallenge", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginHoldPostfix = typeof(TNH_HoldPointPatch).GetMethod("BeginHoldPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersOriginal = typeof(TNH_HoldPoint).GetMethod("RaiseRandomBarriers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersPrefix = typeof(TNH_HoldPointPatch).GetMethod("RaiseRandomBarriersPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataOriginal = typeof(TNH_DestructibleBarrierPoint).GetMethod("SetCoverPointData", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataPrefix = typeof(TNH_HoldPointPatch).GetMethod("BarrierSetCoverPointDataPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseCompletePhaseOriginal = typeof(TNH_HoldPoint).GetMethod("CompletePhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseCompletePhasePostfix = typeof(TNH_HoldPointPatch).GetMethod("CompletePhasePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchShutDownOriginal = typeof(TNH_HoldPoint).GetMethod("ShutDownHoldPoint", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchShutDownPostfix = typeof(TNH_HoldPointPatch).GetMethod("ShutDownPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_HoldPointPatchSystemNodeOriginal, new HarmonyMethod(TNH_HoldPointPatchSystemNodePrefix));
            harmony.Patch(TNH_HoldPointPatchSpawnEntitiesOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEntitiesPrefix));
            harmony.Patch(TNH_HoldPointPatchBeginHoldOriginal, null, new HarmonyMethod(TNH_HoldPointPatchBeginHoldPostfix));
            harmony.Patch(TNH_HoldPointPatchRaiseRandomBarriersOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseRandomBarriersPrefix));
            harmony.Patch(TNH_HoldPointPatchRaiseSetCoverPointDataOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseSetCoverPointDataPrefix));
            harmony.Patch(TNH_HoldPointPatchRaiseCompletePhaseOriginal, null, new HarmonyMethod(TNH_HoldPointPatchRaiseCompletePhasePostfix));
            harmony.Patch(TNH_HoldPointPatchShutDownOriginal, null, new HarmonyMethod(TNH_HoldPointPatchShutDownPostfix));

            // TNHWeaponCrateSpawnObjectsPatch
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchOriginal = typeof(TNH_WeaponCrate).GetMethod("SpawnObjectsRaw", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchPrefix = typeof(TNHWeaponCrateSpawnObjectsPatch).GetMethod("SpawnObjectsRawPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(TNH_WeaponCrateSpawnObjectsPatchOriginal, new HarmonyMethod(TNH_WeaponCrateSpawnObjectsPatchPrefix));

            //// SetActivePatch
            //MethodInfo setActivePatchOriginal = typeof(UnityEngine.GameObject).GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setActivePatchPrefix = typeof(SetActivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setActivePatchOriginal, new HarmonyMethod(setActivePatchPrefix));
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
            //hostButton.GetComponent<BoxCollider>().enabled = false;
            //hostButton.transform.GetChild(0).GetComponent<Text>().color = Color.gray;

            //H3MP_Server.IP = config["IP"].ToString();
            CreateManagerObject(true);

            H3MP_Server.Start((ushort)config["MaxClientCount"], (ushort)config["Port"]); 
            
            if (SceneManager.GetActiveScene().name.Equals("TakeAndHold_Lobby_2"))
            {
                Logger.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                InitTNHMenu();
            }

            //mainStatusText.text = "Starting...";
            //mainStatusText.color = Color.white;
        }

        private void OnConnectClicked()
        {
            CreateManagerObject();

            H3MP_Client client = managerObject.AddComponent<H3MP_Client>();
            client.IP = config["IP"].ToString();
            client.port = (ushort)config["Port"];

            client.ConnectToServer();

            if (SceneManager.GetActiveScene().name.Equals("TakeAndHold_Lobby_2"))
            {
                Logger.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                InitTNHMenu();
            }

            //mainStatusText.text = "Connecting...";
            //mainStatusText.color = Color.white;
        }

        private void OnTNHHostClicked()
        {
            TNHMenuPages[0].SetActive(false);
            TNHMenuPages[1].SetActive(true);
        }

        private void OnTNHJoinClicked()
        {
            TNHMenuPages[0].SetActive(false);
            TNHMenuPages[2].SetActive(true);
        }

        private void OnTNHLPJCheckClicked()
        {
            TNHLPJCheckMark.SetActive(!TNHLPJCheckMark.activeSelf);

            TNHMenuLPJ = TNHLPJCheckMark.activeSelf;
        }

        private void OnTNHHostOnDeathSpectateClicked()
        {
            TNHHostOnDeathSpectateCheckMark.SetActive(!TNHHostOnDeathSpectateCheckMark.activeSelf);
            TNHHostOnDeathLeaveCheckMark.SetActive(!TNHHostOnDeathSpectateCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHHostOnDeathSpectateCheckMark.activeSelf;
        }

        private void OnTNHHostOnDeathLeaveClicked()
        {
            TNHHostOnDeathLeaveCheckMark.SetActive(!TNHHostOnDeathLeaveCheckMark.activeSelf);
            TNHHostOnDeathSpectateCheckMark.SetActive(!TNHHostOnDeathLeaveCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHHostOnDeathSpectateCheckMark.activeSelf;
        }

        private void OnTNHHostConfirmClicked()
        {
            TNHMenuPages[1].SetActive(false);
            TNHMenuPages[4].SetActive(true);

            setLatestInstance = true;
            H3MP_GameManager.AddNewTNHInstance(H3MP_GameManager.ID, TNHMenuLPJ, (int)GM.TNHOptions.ProgressionTypeSetting,
                                               (int)GM.TNHOptions.HealthModeSetting, (int)GM.TNHOptions.EquipmentModeSetting, (int)GM.TNHOptions.TargetModeSetting,
                                               (int)GM.TNHOptions.AIDifficultyModifier, (int)GM.TNHOptions.RadarModeModifier, (int)GM.TNHOptions.ItemSpawnerMode,
                                               (int)GM.TNHOptions.BackpackMode, (int)GM.TNHOptions.HealthMult, (int)GM.TNHOptions.SosiggunShakeReloading, (int)GM.TNHOptions.TNHSeed,
                                               (int)TNH_UIManager_m_currentLevelIndex.GetValue(Mod.currentTNHUIManager));
        }

        private void OnTNHHostCancelClicked()
        {
            TNHMenuPages[0].SetActive(true);
            TNHMenuPages[1].SetActive(false);
        }

        private void OnTNHJoinCancelClicked()
        {
            TNHMenuPages[0].SetActive(true);
            TNHMenuPages[2].SetActive(false);
            TNHMenuPages[3].SetActive(false);
        }

        private void OnTNHJoinOnDeathSpectateClicked()
        {
            TNHJoinOnDeathSpectateCheckMark.SetActive(!TNHJoinOnDeathSpectateCheckMark.activeSelf);
            TNHJoinOnDeathLeaveCheckMark.SetActive(!TNHJoinOnDeathSpectateCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHJoinOnDeathSpectateCheckMark.activeSelf;
        }

        private void OnTNHJoinOnDeathLeaveClicked()
        {
            TNHJoinOnDeathLeaveCheckMark.SetActive(!TNHJoinOnDeathLeaveCheckMark.activeSelf);
            TNHJoinOnDeathSpectateCheckMark.SetActive(!TNHJoinOnDeathLeaveCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHJoinOnDeathSpectateCheckMark.activeSelf;
        }

        private void OnTNHJoinConfirmClicked()
        {
            TNHMenuPages[2].SetActive(false);
            TNHMenuPages[3].SetActive(true);

            if (joinTNHInstances == null)
            {
                joinTNHInstances = new Dictionary<int, GameObject>();
            }
            else
            {
                foreach(KeyValuePair<int, GameObject> entry in joinTNHInstances)
                {
                    Destroy(entry.Value);
                }
            }
            joinTNHInstances.Clear();

            // Populate instance list
            foreach (KeyValuePair<int, H3MP_TNHInstance> TNHInstance in H3MP_GameManager.TNHInstances)
            {
                if (TNHInstance.Value.currentlyPlaying.Count == 0)
                {
                    GameObject newInstance = Instantiate<GameObject>(TNHInstancePrefab, TNHInstanceList.transform);
                    newInstance.transform.GetChild(0).GetComponent<Text>().text = "Instance " + TNHInstance.Key;
                    newInstance.SetActive(true);

                    int instanceID = TNHInstance.Key;
                    FVRPointableButton instanceButton = newInstance.AddComponent<FVRPointableButton>();
                    instanceButton.SetButton();
                    instanceButton.MaxPointingRange = 5;
                    instanceButton.Button.onClick.AddListener(() => { OnTNHInstanceClicked(instanceID); });

                    joinTNHInstances.Add(TNHInstance.Key, newInstance);
                }
            }
        }

        public void OnTNHInstanceClicked(int instance)
        {
            // Handle joining instance success/fail
            if (SetTNHInstance(H3MP_GameManager.TNHInstances[instance]))
            {
                TNHMenuPages[3].SetActive(false);
                TNHMenuPages[4].SetActive(true);
            }
            else
            {
                joinTNHInstances[instance].transform.GetChild(0).GetComponent<Text>().color = Color.red;
            }
        }

        private void OnTNHDisconnectClicked()
        {
            TNHMenuPages[4].SetActive(false);
            TNHMenuPages[0].SetActive(true);

            H3MP_GameManager.SetInstance(0);
            if (Mod.currentlyPlayingTNH)
            {
                Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, H3MP_GameManager.ID);
                Mod.currentlyPlayingTNH = false;
            }
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
            Mod.temporaryHoldSosigIDs.Clear();
            Mod.temporaryHoldTurretIDs.Clear();
            Mod.temporarySupplySosigIDs.Clear();
            Mod.temporarySupplyTurretIDs.Clear();
        }

        public static void OnTNHSpawnStartEquipClicked()
        {
            if(GM.TNH_Manager != null)
            {
                TNH_Manager M = GM.TNH_Manager;
                TNH_CharacterDef C = M.C;
                Vector3 largeCaseSpawnPos = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.8f;
                Vector3 smallCaseSpawnPos = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.3f + Vector3.down * 0.3f;
                Vector3 shieldSpawnPos = GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.4f + Vector3.up * 0.3f;
                Vector3 headForwardNegXZero = GM.CurrentPlayerBody.Head.forward * -1;
                headForwardNegXZero.x = 0;
                if (C.Has_Weapon_Primary)
                {
                    TNH_CharacterDef.LoadoutEntry weapon_Primary = C.Weapon_Primary;
                    int minAmmo = -1;
                    int maxAmmo = -1;
                    FVRObject weapon;
                    if (weapon_Primary.ListOverride.Count > 0)
                    {
                        weapon = weapon_Primary.ListOverride[UnityEngine.Random.Range(0, weapon_Primary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef objectTableDef = weapon_Primary.TableDefs[UnityEngine.Random.Range(0, weapon_Primary.TableDefs.Count)];
                        ObjectTable objectTable = new ObjectTable();
                        objectTable.Initialize(objectTableDef);
                        weapon = objectTable.GetRandomObject();
                        minAmmo = objectTableDef.MinAmmoCapacity;
                        maxAmmo = objectTableDef.MaxAmmoCapacity;
                    }
                    GameObject gameObject = M.SpawnWeaponCase(M.Prefab_WeaponCaseLarge, largeCaseSpawnPos, headForwardNegXZero, weapon, weapon_Primary.Num_Mags_SL_Clips, weapon_Primary.Num_Rounds, minAmmo, maxAmmo, weapon_Primary.AmmoObjectOverride);
                    gameObject.GetComponent<TNH_WeaponCrate>().M = M;
                    gameObject.AddComponent<H3MP_TimerDestroyer>();
                }
                if (C.Has_Weapon_Secondary)
                {
                    TNH_CharacterDef.LoadoutEntry weapon_Secondary = C.Weapon_Secondary;
                    int minAmmo2 = -1;
                    int maxAmmo2 = -1;
                    FVRObject weapon2;
                    if (weapon_Secondary.ListOverride.Count > 0)
                    {
                        weapon2 = weapon_Secondary.ListOverride[UnityEngine.Random.Range(0, weapon_Secondary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef objectTableDef2 = weapon_Secondary.TableDefs[UnityEngine.Random.Range(0, weapon_Secondary.TableDefs.Count)];
                        ObjectTable objectTable2 = new ObjectTable();
                        objectTable2.Initialize(objectTableDef2);
                        weapon2 = objectTable2.GetRandomObject();
                        minAmmo2 = objectTableDef2.MinAmmoCapacity;
                        maxAmmo2 = objectTableDef2.MaxAmmoCapacity;
                    }
                    GameObject gameObject2 = M.SpawnWeaponCase(M.Prefab_WeaponCaseSmall, smallCaseSpawnPos, headForwardNegXZero, weapon2, weapon_Secondary.Num_Mags_SL_Clips, weapon_Secondary.Num_Rounds, minAmmo2, maxAmmo2, weapon_Secondary.AmmoObjectOverride);
                    gameObject2.GetComponent<TNH_WeaponCrate>().M = M;
                }
                if (C.Has_Weapon_Tertiary)
                {
                    TNH_CharacterDef.LoadoutEntry weapon_Tertiary = C.Weapon_Tertiary;
                    FVRObject fvrobject;
                    if (weapon_Tertiary.ListOverride.Count > 0)
                    {
                        fvrobject = weapon_Tertiary.ListOverride[UnityEngine.Random.Range(0, weapon_Tertiary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef d = weapon_Tertiary.TableDefs[UnityEngine.Random.Range(0, weapon_Tertiary.TableDefs.Count)];
                        ObjectTable objectTable3 = new ObjectTable();
                        objectTable3.Initialize(d);
                        fvrobject = objectTable3.GetRandomObject();
                    }
                    GameObject g = UnityEngine.Object.Instantiate<GameObject>(fvrobject.GetGameObject(), smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                    M.AddObjectToTrackedList(g);
                }
                if (C.Has_Item_Primary)
                {
                    TNH_CharacterDef.LoadoutEntry item_Primary = C.Item_Primary;
                    FVRObject fvrobject2;
                    if (item_Primary.ListOverride.Count > 0)
                    {
                        fvrobject2 = item_Primary.ListOverride[UnityEngine.Random.Range(0, item_Primary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef d2 = item_Primary.TableDefs[UnityEngine.Random.Range(0, item_Primary.TableDefs.Count)];
                        ObjectTable objectTable4 = new ObjectTable();
                        objectTable4.Initialize(d2);
                        fvrobject2 = objectTable4.GetRandomObject();
                    }
                    GameObject g2 = UnityEngine.Object.Instantiate<GameObject>(fvrobject2.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f + Vector3.left * 0.3f, UnityEngine.Random.rotation);
                    M.AddObjectToTrackedList(g2);
                }
                if (C.Has_Item_Secondary)
                {
                    TNH_CharacterDef.LoadoutEntry item_Secondary = C.Item_Secondary;
                    FVRObject fvrobject3;
                    if (item_Secondary.ListOverride.Count > 0)
                    {
                        fvrobject3 = item_Secondary.ListOverride[UnityEngine.Random.Range(0, item_Secondary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef d3 = item_Secondary.TableDefs[UnityEngine.Random.Range(0, item_Secondary.TableDefs.Count)];
                        ObjectTable objectTable5 = new ObjectTable();
                        objectTable5.Initialize(d3);
                        fvrobject3 = objectTable5.GetRandomObject();
                    }
                    GameObject g3 = UnityEngine.Object.Instantiate<GameObject>(fvrobject3.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f + Vector3.right * 0.3f, UnityEngine.Random.rotation);
                    M.AddObjectToTrackedList(g3);
                }
                if (C.Has_Item_Tertiary)
                {
                    TNH_CharacterDef.LoadoutEntry item_Tertiary = C.Item_Tertiary;
                    FVRObject fvrobject4;
                    if (item_Tertiary.ListOverride.Count > 0)
                    {
                        fvrobject4 = item_Tertiary.ListOverride[UnityEngine.Random.Range(0, item_Tertiary.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef d4 = item_Tertiary.TableDefs[UnityEngine.Random.Range(0, item_Tertiary.TableDefs.Count)];
                        ObjectTable objectTable6 = new ObjectTable();
                        objectTable6.Initialize(d4);
                        fvrobject4 = objectTable6.GetRandomObject();
                    }
                    GameObject g4 = UnityEngine.Object.Instantiate<GameObject>(fvrobject4.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f, UnityEngine.Random.rotation);
                    M.AddObjectToTrackedList(g4);
                }
                if (C.Has_Item_Shield)
                {
                    TNH_CharacterDef.LoadoutEntry item_Shield = C.Item_Shield;
                    FVRObject fvrobject5;
                    if (item_Shield.ListOverride.Count > 0)
                    {
                        fvrobject5 = item_Shield.ListOverride[UnityEngine.Random.Range(0, item_Shield.ListOverride.Count)];
                    }
                    else
                    {
                        ObjectTableDef d5 = item_Shield.TableDefs[UnityEngine.Random.Range(0, item_Shield.TableDefs.Count)];
                        ObjectTable objectTable7 = new ObjectTable();
                        objectTable7.Initialize(d5);
                        fvrobject5 = objectTable7.GetRandomObject();
                    }
                    GameObject g5 = UnityEngine.Object.Instantiate<GameObject>(fvrobject5.GetGameObject(), shieldSpawnPos, Quaternion.Euler(Vector3.up));
                    M.AddObjectToTrackedList(g5);
                }
            }
            Destroy(TNHStartEquipButton);
            TNHStartEquipButton = null;
        }

        public void OnTNHInstanceReceived(H3MP_TNHInstance instance)
        {
            if (setLatestInstance)
            {
                setLatestInstance = false;

                SetTNHInstance(instance);
            }
        }

        public void OnInstanceReceived(int instance)
        {
            if (setLatestInstance)
            {
                setLatestInstance = false;

                H3MP_GameManager.SetInstance(instance);
            }
        }

        private bool SetTNHInstance(H3MP_TNHInstance instance)
        {
            if (currentTNHUIManager != null)
            {
                currentTNHUIManager.SetOBS_Progression(instance.progressionTypeSetting);
                currentTNHUIManager.SetOBS_EquipmentMode(instance.equipmentModeSetting);
                currentTNHUIManager.SetOBS_TargetMode(instance.targetModeSetting);
                currentTNHUIManager.SetOBS_HealthMode(instance.healthModeSetting);
                currentTNHUIManager.SetOBS_RunSeed(instance.TNHSeed);
                currentTNHUIManager.SetOBS_AIDifficulty(instance.AIDifficultyModifier);
                currentTNHUIManager.SetOBS_AIRadarMode(instance.radarModeModifier);
                currentTNHUIManager.SetOBS_HealthMult(instance.healthMult);
                currentTNHUIManager.SetOBS_ItemSpawner(instance.itemSpawnerMode);
                currentTNHUIManager.SetOBS_Backpack(instance.backpackMode);
                currentTNHUIManager.SetOBS_SosiggunShakeReloading(instance.sosiggunShakeReloading);
                if (instance.levelIndex < currentTNHUIManager.Levels.Count)
                {
                    TNH_UIManager_m_currentLevelIndex.SetValue(currentTNHUIManager, instance.levelIndex);
                    currentTNHUIManager.CurLevelID = currentTNHUIManager.Levels[instance.levelIndex].LevelID;
                    TNH_UIManager_UpdateLevelSelectDisplayAndLoader.Invoke(currentTNHUIManager, null);
                    TNH_UIManager_UpdateTableBasedOnOptions.Invoke(currentTNHUIManager, null);
                    TNH_UIManager_PlayButtonSound.Invoke(currentTNHUIManager, new object[] { 2 });
                }
                else
                {
                    return false;
                }
            }

            H3MP_GameManager.SetInstance(instance.instance);

            TNHInstanceTitle.text = "Instance " + instance.instance;

            if (currentTNHInstancePlayers == null)
            {
                currentTNHInstancePlayers = new Dictionary<int, GameObject>();
            }
            else
            {
                foreach (KeyValuePair<int, GameObject> entry in currentTNHInstancePlayers)
                {
                    Destroy(entry.Value);
                }
            }
            currentTNHInstancePlayers.Clear();

            // Populate player list
            for(int i=0; i < instance.playerIDs.Count; ++i)
            {
                GameObject newPlayer = Instantiate<GameObject>(TNHPlayerPrefab, TNHPlayerList.transform);
                if (H3MP_GameManager.players.ContainsKey(instance.playerIDs[i]))
                {
                    newPlayer.transform.GetChild(0).GetComponent<Text>().text = H3MP_GameManager.players[instance.playerIDs[i]].username + (i == 0 ? " (Host)" : "");
                }
                else
                {
                    newPlayer.transform.GetChild(0).GetComponent<Text>().text = config["Username"].ToString() + (i == 0 ? " (Host)" : "");
                }
                newPlayer.SetActive(true);

                currentTNHInstancePlayers.Add(instance.playerIDs[i], newPlayer);
            }

            currentTNHInstance = instance;

            return true;
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
            else if (loadedScene.name.Equals("TakeAndHold_Lobby_2"))
            {
                Logger.LogInfo("TNH lobby loaded, initializing H3MP menu if possible");
                if (managerObject != null)
                {
                    InitTNHMenu();
                }
            }
        }

        public static void DropAllItems()
        {
            if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null)
            {
                GM.CurrentMovementManager.Hands[0].ForceSetInteractable(null);
            }
            if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null)
            {
                GM.CurrentMovementManager.Hands[1].ForceSetInteractable(null);
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QuickbeltSlots)
            {
                if(slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Internal)
            {
                if(slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Added)
            {
                if(slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
        }
        
        // MOD: This method will be used to find the ID of which player to give control of this object
        //      Mods should patch this if they have a different method of finding the next host, like TNH here for example
        public static int GetBestPotentialObjectHost(int currentController)
        {
            if (Mod.currentTNHInstance != null)
            {
                if(currentController == -1)
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                    {
                        if (player.Value.gameObject.activeSelf)
                        {
                            return player.Key;
                        }
                    }
                }
                else
                {
                    // Going through each like this, we will go through the host of the instance before any other
                    foreach (int playerID in Mod.currentTNHInstance.currentlyPlaying)
                    {
                        if (playerID != currentController && H3MP_GameManager.players[playerID].gameObject.activeSelf)
                        {
                            return playerID;
                        }
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                {
                    if (player.Value.gameObject.activeSelf)
                    {
                        return player.Key;
                    }
                }
            }
            return -1;
        }

        public static void InitTNHData(H3MP_TNHData data)
        {
            GM.TNH_Manager.Phase = data.phase;
            Mod.TNH_Manager_m_curPointSequence.SetValue(GM.TNH_Manager, GM.TNH_Manager.PossibleSequnces[data.sequenceIndex]);
            TNH_CharacterDef c = null;
            try
            {
                c = GM.TNH_Manager.CharDB.GetDef((TNH_Char)GM.TNHOptions.LastPlayedChar);
            }
            catch
            {
                c = GM.TNH_Manager.CharDB.GetDef(TNH_Char.DD_BeginnerBlake);
            }
            Mod.TNH_Manager_m_curProgression.SetValue(GM.TNH_Manager, c.Progressions[data.progressionIndex]);
            Mod.TNH_Manager_m_curProgressionEndless.SetValue(GM.TNH_Manager, c.Progressions_Endless[data.progressionEndlessIndex]);
            Mod.TNH_Manager_m_level.SetValue(GM.TNH_Manager, data.levelIndex);
            Mod.TNH_Manager_SetLevel.Invoke(GM.TNH_Manager, new object[] { data.levelIndex });
            Mod.TNH_Manager_m_curHoldIndex.SetValue(GM.TNH_Manager, data.curHoldIndex);
            Mod.TNH_Manager_m_lastHoldIndex.SetValue(GM.TNH_Manager, data.lastHoldIndex);
            TNH_HoldPoint curHoldPoint = GM.TNH_Manager.HoldPoints[data.curHoldIndex];
            Mod.TNH_Manager_m_curHoldPoint.SetValue(GM.TNH_Manager, curHoldPoint);
            TNH_Progression.Level level = (TNH_Progression.Level)Mod.TNH_Manager_m_curLevel.GetValue(GM.TNH_Manager);
            curHoldPoint.T = level.TakeChallenge;
            curHoldPoint.H = level.HoldChallenge;

            List<TNH_Manager.SosigPatrolSquad> patrolSquads = (List<TNH_Manager.SosigPatrolSquad>)Mod.TNH_Manager_m_patrolSquads.GetValue(GM.TNH_Manager);
            patrolSquads.Clear();
            foreach (TNH_Manager.SosigPatrolSquad patrol in data.patrols)
            {
                patrolSquads.Add(patrol);
            }

            H3MP_TrackedSosigData[] sosigArrToUse = null;
            H3MP_TrackedAutoMeaterData[] autoMeaterArrToUse = null;
            if (H3MP_ThreadManager.host)
            {
                sosigArrToUse = H3MP_Server.sosigs;
                autoMeaterArrToUse = H3MP_Server.autoMeaters;
            }
            else
            {
                sosigArrToUse = H3MP_Client.sosigs;
                autoMeaterArrToUse = H3MP_Client.autoMeaters;
            }
            List<Sosig> curHoldPointSosigs = (List<Sosig>)Mod.TNH_HoldPoint_m_activeSosigs.GetValue(curHoldPoint);
            curHoldPointSosigs.Clear();
            for (int i = 0; i < data.activeHoldSosigIDs.Length; ++i)
            {
                int sosigID = data.activeHoldSosigIDs[i];
                if (sosigArrToUse[sosigID] != null)
                {
                    if (sosigArrToUse[sosigID].physicalObject != null)
                    {
                        curHoldPointSosigs.Add(sosigArrToUse[sosigID].physicalObject.physicalSosigScript);
                    }
                    else
                    {
                        Mod.temporaryHoldSosigIDs.Add(sosigID);
                    }
                }
            }
            List<AutoMeater> curHoldPointTurrets = (List<AutoMeater>)Mod.TNH_HoldPoint_m_activeTurrets.GetValue(curHoldPoint);
            curHoldPointTurrets.Clear();
            for (int i = 0; i < data.activeHoldTurretIDs.Length; ++i)
            {
                int autoMeaterID = data.activeHoldTurretIDs[i];
                if (autoMeaterArrToUse[autoMeaterID] != null)
                {
                    if (autoMeaterArrToUse[autoMeaterID].physicalObject != null)
                    {
                        curHoldPointTurrets.Add(autoMeaterArrToUse[autoMeaterID].physicalObject.physicalAutoMeaterScript);
                    }
                    else
                    {
                        Mod.temporaryHoldTurretIDs.Add(autoMeaterID);
                    }
                }
            }

            if (Mod.currentTNHInstance.activeSupplyPointIndices == null)
            {
                Mod.currentTNHInstance.activeSupplyPointIndices = new List<int>();
            }
            else
            {
                Mod.currentTNHInstance.activeSupplyPointIndices.Clear();
            }
            for (int i = 0; i < data.activeSupplyIndices.Length; ++i)
            {
                Mod.currentTNHInstance.activeSupplyPointIndices.Add(data.activeSupplyIndices[i]);
                TNH_SupplyPoint curSupplyPoint = GM.TNH_Manager.SupplyPoints[data.activeSupplyIndices[i]];
                List<Sosig> curSupplyPointSosigs = (List<Sosig>)Mod.TNH_SupplyPoint_m_activeSosigs.GetValue(curSupplyPoint);
                curSupplyPointSosigs.Clear();
                for (int j = 0; j < data.supplyPointsSosigIDs[i].Length; ++j)
                {
                    int sosigID = data.supplyPointsSosigIDs[i][j];
                    if (sosigArrToUse[sosigID] != null)
                    {
                        // We might not yet have an instance of the sosig if we just joined 
                        if (sosigArrToUse[sosigID].physicalObject != null)
                        {
                            curSupplyPointSosigs.Add(sosigArrToUse[sosigID].physicalObject.physicalSosigScript);
                        }
                        else
                        {
                            // In this case we want to keep the IDs for later so we can add them once they are instantiated
                            Mod.temporarySupplySosigIDs.Add(sosigID, data.activeSupplyIndices[i]);
                        }
                    }
                }
                List<AutoMeater> curSupplyPointTurrets = (List<AutoMeater>)Mod.TNH_SupplyPoint_m_activeTurrets.GetValue(curSupplyPoint);
                curSupplyPointTurrets.Clear();
                for (int j = 0; j < data.supplyPointsTurretIDs[i].Length; ++j)
                {
                    int autoMeaterID = data.supplyPointsTurretIDs[i][j];
                    if (autoMeaterArrToUse[autoMeaterID] != null)
                    {
                        if (autoMeaterArrToUse[autoMeaterID].physicalObject != null)
                        {
                            curSupplyPointTurrets.Add(autoMeaterArrToUse[autoMeaterID].physicalObject.physicalAutoMeaterScript);
                        }
                        else
                        {
                            // In this case we want to keep the IDs for later so we can add them once they are instantiated
                            Mod.temporarySupplyTurretIDs.Add(autoMeaterID, data.activeSupplyIndices[i]);
                        }
                    }
                }
            }
        }
    }

    #region General Patches
    // Patches SteamVR_LoadLevel.Begin() So we can keep track of which scene we are loading
    class LoadLevelBeginPatch
    {
        public static string loadingLevel;

        static void Prefix(string levelName)
        {
            loadingLevel = levelName;
        }
    }

    // DEBUG PATCH Patches GameObject.SetActive
    class SetActivePatch
    {
        static void Prefix(ref GameObject __instance, bool value)
        {
            if (value)
            {
                Debug.LogWarning("SetActivePatch called with true on " + __instance.name+":\n"+Environment.StackTrace);
            }
        }
    }

    #endregion

    #region Interaction Patches
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
                // If spectating we are just going to force break the interaction
                if (Mod.TNHSpectating)
                {
                    ___m_currentInteractable.ForceBreakInteraction();
                }

                // Just started interacting with this item
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
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
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
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                        }
                    }
                }
                else // Although SosigLinks are FVRPhysicalObjects, they don't have an objectWrapper, so they won't be tracked items
                {
                    SosigLink sosigLink = ___m_currentInteractable.GetComponent<SosigLink>();
                    if(sosigLink != null)
                    {
                        // We just grabbed a sosig
                        H3MP_TrackedSosig trackedSosig = sosigLink.S.GetComponent<H3MP_TrackedSosig>();
                        if(trackedSosig != null && trackedSosig.data.trackedID != -1 && trackedSosig.data.localTrackedID == -1)
                        {
                            if (H3MP_ThreadManager.host)
                            {
                                H3MP_ServerSend.GiveSosigControl(trackedSosig.data.trackedID, 0);

                                // Update locally
                                trackedSosig.data.controller = 0;
                                trackedSosig.data.localTrackedID = H3MP_GameManager.sosigs.Count;
                                H3MP_GameManager.sosigs.Add(trackedSosig.data);
                            }
                            else
                            {
                                H3MP_ClientSend.GiveSosigControl(trackedSosig.data.trackedID, H3MP_Client.singleton.ID);

                                // Update locally
                                trackedSosig.data.controller = H3MP_Client.singleton.ID;
                                trackedSosig.data.localTrackedID = H3MP_GameManager.sosigs.Count;
                                H3MP_GameManager.sosigs.Add(trackedSosig.data);
                            }
                        }
                    }
                    else // Although AutoMeater turrets have FVRPhysicalObjects, they don't have an objectWrapper, so they won't be tracked items
                    {
                        AutoMeater autoMeater = ___m_currentInteractable.GetComponent<AutoMeater>();
                        if (autoMeater != null)
                        {
                            // We just grabbed an AutoMeater
                            H3MP_TrackedAutoMeater trackedAutoMeater = autoMeater.GetComponent<H3MP_TrackedAutoMeater>();
                            if (trackedAutoMeater != null && trackedAutoMeater.data.trackedID != -1 && trackedAutoMeater.data.localTrackedID == -1)
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.GiveAutoMeaterControl(trackedAutoMeater.data.trackedID, 0);

                                    // Update locally
                                    trackedAutoMeater.data.controller = 0;
                                    trackedAutoMeater.data.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                                    H3MP_GameManager.autoMeaters.Add(trackedAutoMeater.data);
                                }
                                else
                                {
                                    H3MP_ClientSend.GiveAutoMeaterControl(trackedAutoMeater.data.trackedID, H3MP_Client.singleton.ID);

                                    // Update locally
                                    trackedAutoMeater.data.controller = H3MP_Client.singleton.ID;
                                    trackedAutoMeater.data.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                                    H3MP_GameManager.autoMeaters.Add(trackedAutoMeater.data);
                                }
                            }
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
                // If spectating we don't want to be able to put things in slots
                if (Mod.TNHSpectating)
                {
                    __instance.SetQuickBeltSlot(null);
                }

                // Just put this item in a slot
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
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
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
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
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
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

    // Patches SosigHand.PickUp so we can keep track of item control
    class SosigPickUpPatch
    {
        static void Postfix(ref SosigHand __instance, SosigWeapon o)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = o.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
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
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                        //if (trackedItem.data.parent == -1)
                        //{
                        //  __instance.RecoverRigidbody();
                        //}
                        bool primaryHand = __instance == __instance.S.Hand_Primary;
                        H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
                        H3MP_ServerSend.SosigPickUpItem(trackedSosig.data.trackedID, trackedItem.data.trackedID, primaryHand);
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
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                        //if (trackedItem.data.parent == -1)
                        //{
                        //  __instance.RecoverRigidbody();
                        //}
                        bool primaryHand = __instance == __instance.S.Hand_Primary; 
                        H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
                        if(trackedSosig.data.trackedID == -1)
                        {
                            if (H3MP_TrackedSosig.unknownItemInteractTrackedIDs.ContainsKey(trackedSosig.data.localTrackedID))
                            {
                                H3MP_TrackedSosig.unknownItemInteractTrackedIDs[trackedSosig.data.localTrackedID].Add(new KeyValuePair<int, KeyValuePair<int, int>>(0, new KeyValuePair<int, int>(trackedItem.data.trackedID, primaryHand ? 1 : 0)));
                            }
                            else
                            {
                                H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Add(trackedSosig.data.localTrackedID, new List<KeyValuePair<int, KeyValuePair<int, int>>>() { new KeyValuePair<int, KeyValuePair<int, int>>(0, new KeyValuePair<int, int>(trackedItem.data.trackedID, primaryHand ? 1 : 0)) });
                            }
                        }
                        else
                        {
                            H3MP_ClientSend.SosigPickUpItem(trackedSosig, trackedItem.data.trackedID, primaryHand);
                        }
                    }
                }
            }
        }
    }

    // Patches SosigInventory.Slot.PlaceObjectIn so we can keep track of item control
    class SosigPlaceObjectInPatch
    {
        static void Postfix(ref SosigInventory.Slot __instance, SosigWeapon o)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = o.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
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
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                        //if (trackedItem.data.parent == -1)
                        //{
                        //  __instance.RecoverRigidbody();
                        //}
                        int slotIndex = 0;
                        for(int i=0; i< __instance.I.Slots.Count; ++i)
                        {
                            if (__instance.I.Slots[i] == __instance)
                            {
                                slotIndex = i;
                                break;
                            }
                        }
                        H3MP_ServerSend.SosigPlaceItemIn(__instance.I.S.GetComponent<H3MP_TrackedSosig>().data.trackedID, slotIndex, trackedItem.data.trackedID);
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
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        // TODO: Check if necessary to manage the rigidbody ourselves in the case of interacting/dropping in QBS or if the game already does it
                        //if (trackedItem.data.parent == -1)
                        //{
                        //  __instance.RecoverRigidbody();
                        //}
                        int slotIndex = 0;
                        for (int i = 0; i < __instance.I.Slots.Count; ++i)
                        {
                            if (__instance.I.Slots[i] == __instance)
                            {
                                slotIndex = i;
                                break;
                            }
                        }
                        H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.I.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.I.S] : __instance.I.S.GetComponent<H3MP_TrackedSosig>();
                        if (trackedSosig.data.trackedID == -1)
                        {
                            if (H3MP_TrackedSosig.unknownItemInteractTrackedIDs.ContainsKey(trackedSosig.data.localTrackedID))
                            {
                                H3MP_TrackedSosig.unknownItemInteractTrackedIDs[trackedSosig.data.localTrackedID].Add(new KeyValuePair<int, KeyValuePair<int, int>>(1, new KeyValuePair<int, int>(trackedItem.data.trackedID, slotIndex)));
                            }
                            else
                            {
                                H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Add(trackedSosig.data.localTrackedID, new List<KeyValuePair<int, KeyValuePair<int, int>>>() { new KeyValuePair<int, KeyValuePair<int, int>>(1, new KeyValuePair<int, int>(trackedItem.data.trackedID, slotIndex)) });
                            }
                        }
                        else
                        {
                            H3MP_ClientSend.SosigPlaceItemIn(trackedSosig.data.trackedID, slotIndex, trackedItem.data.trackedID);
                        }
                    }
                }
            }
        }
    }

    // Patches SosigInventory.Slot.DetachHeldObject so we can keep track of item control
    class SosigSlotDetachPatch
    {
        public static int skip;

        static void Prefix(ref SosigInventory.Slot __instance)
        {
            if (skip > 0)
            {
                return;
            }

            if (Mod.managerObject == null || !__instance.IsHoldingObject)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.I.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.I.S] : __instance.I.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
            {
                if (H3MP_ThreadManager.host)
                {
                    int slotIndex = 0;
                    for (int i = 0; i < __instance.I.Slots.Count; ++i)
                    {
                        if (__instance.I.Slots[i] == __instance)
                        {
                            slotIndex = i;
                            break;
                        }
                    }
                    H3MP_ServerSend.SosigDropSlot(trackedSosig.data.trackedID, slotIndex);
                }
                else
                {
                    int slotIndex = 0;
                    for (int i = 0; i < __instance.I.Slots.Count; ++i)
                    {
                        if (__instance.I.Slots[i] == __instance)
                        {
                            slotIndex = i;
                            break;
                        }
                    }
                    if (trackedSosig.data.trackedID == -1)
                    {
                        if (H3MP_TrackedSosig.unknownItemInteractTrackedIDs.ContainsKey(trackedSosig.data.localTrackedID))
                        {
                            H3MP_TrackedSosig.unknownItemInteractTrackedIDs[trackedSosig.data.localTrackedID].Add(new KeyValuePair<int, KeyValuePair<int, int>>(2, new KeyValuePair<int, int>(slotIndex, slotIndex)));
                        }
                        else
                        {
                            H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Add(trackedSosig.data.localTrackedID, new List<KeyValuePair<int, KeyValuePair<int, int>>>() { new KeyValuePair<int, KeyValuePair<int, int>>(2, new KeyValuePair<int, int>(slotIndex, slotIndex)) });
                        }
                    }
                    else
                    {
                        H3MP_ClientSend.SosigDropSlot(trackedSosig.data.trackedID, slotIndex);
                    }
                }
            }
        }
    }

    // Patches SosigHand.DropHeldObject AND SosigHand.ThrowObject so we can keep track of item control
    class SosigHandDropPatch
    {
        public static int skip;

        static void Prefix(ref SosigHand __instance)
        {
            if(skip > 0)
            {
                return;
            }

            if (Mod.managerObject == null || !__instance.IsHoldingObject)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigHandDrop(trackedSosig.data.trackedID, __instance.S.Hand_Primary == __instance);
                }
                else
                {
                    if (trackedSosig.data.trackedID == -1)
                    {
                        if (H3MP_TrackedSosig.unknownItemInteractTrackedIDs.ContainsKey(trackedSosig.data.localTrackedID))
                        {
                            H3MP_TrackedSosig.unknownItemInteractTrackedIDs[trackedSosig.data.localTrackedID].Add(new KeyValuePair<int, KeyValuePair<int, int>>(3, new KeyValuePair<int, int>(__instance.S.Hand_Primary == __instance ? 1 : 0, 0)));
                        }
                        else
                        {
                            H3MP_TrackedSosig.unknownItemInteractTrackedIDs.Add(trackedSosig.data.localTrackedID, new List<KeyValuePair<int, KeyValuePair<int, int>>>() { new KeyValuePair<int, KeyValuePair<int, int>>(3, new KeyValuePair<int, int>(__instance.S.Hand_Primary == __instance ? 1 : 0, 0)) });
                        }
                    }
                    else
                    {
                        H3MP_ClientSend.SosigHandDrop(trackedSosig.data.trackedID, __instance.S.Hand_Primary == __instance);
                    }
                }
            }
        }
    }
    #endregion

    #region Action Patches
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
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            // Get tracked item
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.WeaponFire(0, trackedItem.data.trackedID);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
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

    // Patches Sosig.Configure to keep a reference to the config template
    class SosigConfigurePatch
    {
        public static bool skipConfigure;

        static void Prefix(ref Sosig __instance, SosigConfigTemplate t)
        {
            if (skipConfigure)
            {
                skipConfigure = false;
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = __instance.GetComponent<H3MP_TrackedSosig>();
            if(trackedSosig != null)
            {
                trackedSosig.data.configTemplate = t;

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigConfigure(trackedSosig.data.trackedID, t);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        H3MP_ClientSend.SosigConfigure(trackedSosig.data.trackedID, t);
                    }
                }
            }
        }
    }

    // Patches Sosig update methods to prevent processing on non controlling client
    class SosigUpdatePatch
    {
        static bool UpdatePrefix(ref Sosig __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if(trackedSosig != null)
            {
                bool runOriginal = trackedSosig.data.controller == H3MP_GameManager.ID;
                if (!runOriginal)
                {
                    // Call Sosig update methods we don't want to skip
                    Mod.Sosig_VaporizeUpdate.Invoke(__instance, null);
                    __instance.HeadIconUpdate();
                }
                return runOriginal;
            }
            return true;
        }
        
        static bool HandPhysUpdatePrefix(ref Sosig __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if(trackedSosig != null)
            {
                return trackedSosig.data.controller == H3MP_GameManager.ID;
            }
            return true;
        }
    }

    // Patches SosigInventory update methods to prevent processing on non controlling client
    class SosigInvUpdatePatch
    {
        static bool PhysHoldPrefix(ref SosigInventory __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if(trackedSosig != null)
            {
                return trackedSosig.data.controller == H3MP_GameManager.ID;
            }
            return true;
        }
    }

    // Patches Sosig to keep track of all actions taken on a sosig
    class SosigActionPatch
    {
        public static int sosigDiesSkip;
        public static int sosigClearSkip;
        public static int sosigSetBodyStateSkip;
        public static int sosigVaporizeSkip;
        public static int sosigSetCurrentOrderSkip;
        public static int sosigRequestHitDecalSkip;

        static void SosigDiesPrefix(ref Sosig __instance, Damage.DamageClass damClass, Sosig.SosigDeathType deathType)
        {
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;

            if (sosigDiesSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigDies(trackedSosig.data.trackedID, damClass, deathType);
                }
                else
                {
                    H3MP_ClientSend.SosigDies(trackedSosig.data.trackedID, damClass, deathType);
                }
            }
        }

        static void SosigDiesPostfix()
        {
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
        }

        static void SosigClearPrefix(ref Sosig __instance)
        {
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;
            ++SosigLinkActionPatch.skipLinkExplodes;

            if (sosigClearSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                trackedSosig.sendDestroy = false;
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigClear(trackedSosig.data.trackedID);
                }
                else
                {
                    H3MP_ClientSend.SosigClear(trackedSosig.data.trackedID);
                }
            }
        }

        static void SosigClearPostfix()
        {
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
            --SosigLinkActionPatch.skipLinkExplodes;
        }

        static void SetBodyStatePrefix(ref Sosig __instance, Sosig.SosigBodyState s)
        {
            if (sosigSetBodyStateSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigSetBodyState(trackedSosig.data.trackedID, s);
                }
                else
                {
                    H3MP_ClientSend.SosigSetBodyState(trackedSosig.data.trackedID, s);
                }
            }
        }

        static IEnumerable<CodeInstruction> FootStepTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsertSecond = new List<CodeInstruction>();
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Sosig gameobject
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 10)); // Load value of FVRPooledAudioType.GenericClose
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Sosig gameobject
            toInsertSecond.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_transform"))); // Get Sosig transform
            toInsertSecond.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_position"))); // Get position of Sosig transform
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load num3
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldc_R4, 0.35f)); // Load 4 byte real literal 0.35
            toInsertSecond.Add(new CodeInstruction(OpCodes.Mul)); // Multiply
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load num3
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldc_R4, 0.4f)); // Load 4 byte real literal 0.4
            toInsertSecond.Add(new CodeInstruction(OpCodes.Mul)); // Multiply
            toInsertSecond.Add(new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), new Type[] { typeof(float), typeof(float) }))); // Create new Vector2
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldc_R4, 0.95f)); // Load 4 byte real literal 0.95
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldc_R4, 1.05f)); // Load 4 byte real literal 1.05
            toInsertSecond.Add(new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), new Type[] { typeof(float), typeof(float) }))); // Create new Vector2
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load delay
            toInsertSecond.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SosigActionPatch), "SendFootStepSound"))); // Call our own method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("PlayCoreSoundDelayedOverrides"))
                {
                    instructionList.InsertRange(i + 1, toInsertSecond);
                }
            }
            return instructionList;
        }

        public static void SendFootStepSound(Sosig sosig, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(sosig) ? H3MP_GameManager.trackedSosigBySosig[sosig] : sosig.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.PlaySosigFootStepSound(trackedSosig.data.trackedID, audioType, position, vol, pitch, delay);
                }
                else
                {
                    H3MP_ClientSend.PlaySosigFootStepSound(trackedSosig.data.trackedID, audioType, position, vol, pitch, delay);
                }
            }
        }

        static IEnumerable<CodeInstruction> SpeechUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Sosig instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Sosig instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Sosig), "CurrentOrder"))); // Load CurrentOrder
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SosigActionPatch), "SendSpeakState"))); // Call our own method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("Speak_State"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }
            }
            return instructionList;
        }

        public static void SendSpeakState(Sosig sosig, Sosig.SosigOrder currentOrder)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(sosig) ? H3MP_GameManager.trackedSosigBySosig[sosig] : sosig.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigSpeakState(trackedSosig.data.trackedID, currentOrder);
                }
                else
                {
                    H3MP_ClientSend.SosigSpeakState(trackedSosig.data.trackedID, currentOrder);
                }
            }
        }

        static void SetCurrentOrderPrefix(ref Sosig __instance, Sosig.SosigOrder o)
        {
            if(sosigSetCurrentOrderSkip > 0)
            {
                return;
            }

            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigSetCurrentOrder(trackedSosig.data.trackedID, o);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        H3MP_ClientSend.SosigSetCurrentOrder(trackedSosig.data.trackedID, o);
                    }
                }
            }
        }

        static void SosigVaporizePrefix(ref Sosig __instance, int iff)
        {
            ++sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++sosigSetBodyStateSkip;

            if(sosigVaporizeSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigVaporize(trackedSosig.data.trackedID, iff);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        H3MP_ClientSend.SosigVaporize(trackedSosig.data.trackedID, iff);
                    }
                }
            }
        }

        static void SosigVaporizePostfix()
        {
            --sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --sosigSetBodyStateSkip;
        }

        static void RequestHitDecalPrefix(ref Sosig __instance, Vector3 point, Vector3 normal, float scale, SosigLink l)
        {
            if (sosigRequestHitDecalSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            for(int i=0; i < __instance.Links.Count; ++i)
            {
                if (__instance.Links[i] == l)
                {
                    H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
                    if (trackedSosig != null)
                    {
                        SendRequestHitDecal(trackedSosig.data.trackedID, point, normal, UnityEngine.Random.onUnitSphere, scale, i);
                    }
                    break;
                }
            }
        }

        static void RequestHitDecalEdgePrefix(ref Sosig __instance, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, SosigLink l)
        {
            if (sosigRequestHitDecalSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            for (int i = 0; i < __instance.Links.Count; ++i)
            {
                if (__instance.Links[i] == l)
                {
                    H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
                    if (trackedSosig != null)
                    {
                        SendRequestHitDecal(trackedSosig.data.trackedID, point, normal, edgeNormal, scale, i);
                    }
                    break;
                }
            }
        }

        static void SendRequestHitDecal(int sosigTrackedID, Vector3 point, Vector3 normal, Vector3 edgeNormal, float scale, int linkIndex)
        {
            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.SosigRequestHitDecal(sosigTrackedID, point, normal, edgeNormal, scale, linkIndex);
            }
            else
            {
                H3MP_ClientSend.SosigRequestHitDecal(sosigTrackedID, point, normal, edgeNormal, scale, linkIndex);
            }
        }
    }

    // Patches SosigLink to keep track of all actions taken on a link
    class SosigLinkActionPatch
    {
        public static string knownWearableID;
        public static int skipRegisterWearable;
        public static int skipDeRegisterWearable;
        public static int skipLinkExplodes;
        public static int sosigLinkBreakSkip;
        public static int sosigLinkSeverSkip;

        static void RegisterWearablePrefix(ref SosigLink __instance, SosigWearable w)
        {
            if (skipRegisterWearable > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                int linkIndex = -1;
                for(int i=0; i<__instance.S.Links.Count;++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if(linkIndex == -1)
                {
                    Debug.LogError("RegisterWearablePrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if(knownWearableID == null)
                    {
                        knownWearableID = w.name;
                        if (knownWearableID.EndsWith("(Clone)"))
                        {
                            knownWearableID = knownWearableID.Substring(0, knownWearableID.Length - 7);
                        }
                        if (Mod.sosigWearableMap.ContainsKey(knownWearableID))
                        {
                            knownWearableID = Mod.sosigWearableMap[knownWearableID];
                        }
                        else
                        {
                            Debug.LogError("SosigWearable: " + knownWearableID + " not found in map");
                        }
                    }
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SosigLinkRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            H3MP_ClientSend.SosigLinkRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                        }
                    }

                    trackedSosig.data.wearables[linkIndex].Add(knownWearableID);

                    knownWearableID = null;
                }
            }
        }

        static void DeRegisterWearablePrefix(ref SosigLink __instance, SosigWearable w)
        {
            if (skipDeRegisterWearable > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                int linkIndex = -1;
                for(int i=0; i<__instance.S.Links.Count;++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if(linkIndex == -1)
                {
                    Debug.LogError("RegisterWearablePrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if(knownWearableID == null)
                    {
                        knownWearableID = w.name;
                        if (knownWearableID.EndsWith("(Clone)"))
                        {
                            knownWearableID = knownWearableID.Substring(0, knownWearableID.Length - 7);
                        }
                        if (Mod.sosigWearableMap.ContainsKey(knownWearableID))
                        {
                            knownWearableID = Mod.sosigWearableMap[knownWearableID];
                        }
                        else
                        {
                            Debug.LogError("SosigWearable: " + knownWearableID + " not found in map");
                        }
                    }
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SosigLinkDeRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            H3MP_ClientSend.SosigLinkDeRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                        }
                    }

                    trackedSosig.data.wearables[linkIndex].Remove(knownWearableID);

                    knownWearableID = null;
                }
            }
        }

        static void LinkExplodesPrefix(ref SosigLink __instance, Damage.DamageClass damClass)
        {
            ++SosigActionPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;

            if (skipLinkExplodes > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                int linkIndex = -1;
                for(int i=0; i<__instance.S.Links.Count;++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if(linkIndex == -1)
                {
                    Debug.LogError("LinkExplodesPrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SosigLinkExplodes(trackedSosig.data.trackedID, linkIndex, damClass);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            H3MP_ClientSend.SosigLinkExplodes(trackedSosig.data.trackedID, linkIndex, damClass);
                        }
                    }
                }
            }
        }

        static void LinkExplodesPostfix()
        {
            --SosigActionPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
        }

        static void LinkBreakPrefix(ref SosigLink __instance, bool isStart, Damage.DamageClass damClass)
        {
            ++SosigActionPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;

            if (sosigLinkBreakSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                int linkIndex = -1;
                for(int i=0; i<__instance.S.Links.Count;++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if(linkIndex == -1)
                {
                    Debug.LogError("LinkBreakPrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SosigLinkBreak(trackedSosig.data.trackedID, linkIndex, isStart, (byte)damClass);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            H3MP_ClientSend.SosigLinkBreak(trackedSosig.data.trackedID, linkIndex, isStart, damClass);
                        }
                    }
                }
            }
        }

        static void LinkBreakPostfix()
        {
            --SosigActionPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
        }

        static void LinkSeverPrefix(ref SosigLink __instance, Damage.DamageClass damClass, bool isPullApart)
        {
            ++SosigActionPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;

            if (sosigLinkSeverSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                int linkIndex = -1;
                for(int i=0; i<__instance.S.Links.Count;++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if(linkIndex == -1)
                {
                    Debug.LogError("LinkSeverPrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SosigLinkSever(trackedSosig.data.trackedID, linkIndex, (byte)damClass, isPullApart);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            H3MP_ClientSend.SosigLinkSever(trackedSosig.data.trackedID, linkIndex, damClass, isPullApart);
                        }
                    }
                }
            }
        }

        static void LinkSeverPostfix()
        {
            --SosigActionPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
        }

        static void LinkVaporizePrefix(ref SosigLink __instance, int IFF)
        {
            ++SosigActionPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigActionPatch.sosigSetBodyStateSkip;

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigVaporize(trackedSosig.data.trackedID, IFF);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        H3MP_ClientSend.SosigVaporize(trackedSosig.data.trackedID, IFF);
                    }
                }
            }
        }

        static void LinkVaporizePostfix()
        {
            --SosigActionPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigActionPatch.sosigSetBodyStateSkip;
        }
    }

    // Patches Sosig IFF methods to keep track of changes to the IFF
    class SosigIFFPatch
    {
        public static int skip;

        static void SetIFFPrefix(ref Sosig __instance, int i)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                trackedSosig.data.IFF = (byte)i;
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigSetIFF(trackedSosig.data.trackedID, i);
                }
                else
                {
                    H3MP_ClientSend.SosigSetIFF(trackedSosig.data.trackedID, i);
                }
            }
        }

        static void SetOriginalIFFPrefix(ref Sosig __instance, int i)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                trackedSosig.data.IFF = (byte)i;
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigSetOriginalIFF(trackedSosig.data.trackedID, i);
                }
                else
                {
                    H3MP_ClientSend.SosigSetOriginalIFF(trackedSosig.data.trackedID, i);
                }
            }
        }
    }

    // Patches Sosig.EventReceive to prevent event processing on non-controlling client
    class SosigEventReceivePatch
    {
        static bool Prefix(ref Sosig __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Possible if instance has been destroyed but still accessible
            if(__instance == null)
            {
                return false;
            }

            H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance) ? H3MP_GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                return trackedSosig.data.controller == H3MP_GameManager.ID;
            }
            return true;
        }
    }

    // Patches AutoMeater.Update and FixedUpdate to prevent updating on non-controlling client
    class AutoMeaterUpdatePatch
    {
        static bool Prefix(ref AutoMeater __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                bool runOriginal = trackedAutoMeater.data.controller == H3MP_GameManager.ID;
                if (!runOriginal)
                {
                    // Call AutoMeater update methods we don't want to skip
                    if(trackedAutoMeater.data.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0].IsFlameThrower)
                    {
                        trackedAutoMeater.data.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0].Tick(Time.deltaTime);
                    }
                }
                return runOriginal;
            }
            return true;
        }
    }

    // Patches AutoMeater.EventReceive to prevent event processing on non-controlling client
    class AutoMeaterEventPatch
    {
        static bool Prefix(ref AutoMeater __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                return trackedAutoMeater.data.controller == H3MP_GameManager.ID;
            }
            return true;
        }
    }

    // Patches AutoMeater.SetState to send to other clients
    class AutoMeaterSetStatePatch
    {
        public static int skip;

        static void Postfix(ref AutoMeater __instance, AutoMeater.AutoMeaterState s)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            if (skip > 0)
            {
                return;
            }

            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.AutoMeaterSetState(trackedAutoMeater.data.trackedID, (byte)s);
                }
                else
                {
                    if(trackedAutoMeater.data.trackedID != -1)
                    {
                        H3MP_ClientSend.AutoMeaterSetState(trackedAutoMeater.data.trackedID, (byte)s);
                    }
                }
            }
        }
    }

    // Patches AutoMeater.UpdateFlight to send to blade activation to other clients ad prevent update on non-controlling clients
    class AutoMeaterUpdateFlightPatch
    {
        static bool Prefix(ref AutoMeater __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                return trackedAutoMeater.data.controller == H3MP_GameManager.ID;
            }
            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsertActive = new List<CodeInstruction>();
            toInsertActive.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AutoMeater gameobject
            toInsertActive.Add(new CodeInstruction(OpCodes.Ldc_I4_1)); // Load true
            toInsertActive.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoMeaterUpdateFlightPatch), "SetAutoMeaterBladesActive"))); // Call SetAutoMeaterBladesActive
            List<CodeInstruction> toInsertInactive = new List<CodeInstruction>();
            toInsertActive.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AutoMeater gameobject
            toInsertActive.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load false
            toInsertActive.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoMeaterUpdateFlightPatch), "SetAutoMeaterBladesActive"))); // Call SetAutoMeaterBladesActive

            bool active = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("UsesBlades"))
                {
                    instructionList.InsertRange(i + 2, active ? toInsertActive : toInsertInactive);
                    active = !active;
                }
            }
            return instructionList;
        }

        public static void SetAutoMeaterBladesActive(AutoMeater autoMeater, bool active)
        {
            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(autoMeater) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[autoMeater] : autoMeater.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.AutoMeaterSetBladesActive(trackedAutoMeater.data.trackedID, active);
                }
                else
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        H3MP_ClientSend.AutoMeaterSetBladesActive(trackedAutoMeater.data.trackedID, active);
                    }
                }
            }
        }
    }

    // Patches AutoMeaterFirearm.FireShot to send to fire action to other clients
    class AutoMeaterFirearmFireShotPatch
    {
        public static int skip;
        public static bool angleOverride;
        public static Vector3 muzzleAngles;

        static void Prefix()
        {
            // Make sure we skip projectile instantiation
            ++Mod.skipAllInstantiates;
        }

        static void Postfix(ref AutoMeater.AutoMeaterFirearm __instance)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            // Get tracked item
            AutoMeater m = (AutoMeater)Mod.AutoMeaterFirearm_M.GetValue(__instance);
            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(m) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[m] : m.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        H3MP_ServerSend.AutoMeaterFirearmFireShot(0, trackedAutoMeater.data.trackedID, __instance.Muzzle.localEulerAngles);
                    }
                }
                else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        H3MP_ClientSend.AutoMeaterFirearmFireShot(trackedAutoMeater.data.trackedID, __instance.Muzzle.localEulerAngles);
                    }
                }
            }

            --Mod.skipAllInstantiates;
        }

        public static Vector3 GetMuzzleAngles(Vector3 currentAngles)
        {
            if(angleOverride)
            {
                angleOverride = false;
                return muzzleAngles;
            }
            return currentAngles;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AutoMeaterFirearm instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AutoMeater.AutoMeaterFirearm), "Muzzle"))); // Load Muzzle
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AutoMeaterFirearm instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AutoMeater.AutoMeaterFirearm), "Muzzle"))); // Load Muzzle
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_localEulerAngles"))); // Get current angles
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoMeaterFirearmFireShotPatch), "GetMuzzleAngles"))); // Call GetMuzzleAngles
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "set_localEulerAngles"))); // Set angles

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("set_localEulerAngles"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches AutoMeaterFirearm.SetFireAtWill to send to sync with other clients
    class AutoMeaterFirearmFireAtWillPatch
    {
        public static int skip;

        static void Prefix(ref AutoMeater.AutoMeaterFirearm __instance, bool b, float d)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            // Get tracked item
            AutoMeater m = (AutoMeater)Mod.AutoMeaterFirearm_M.GetValue(__instance);
            H3MP_TrackedAutoMeater trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(m) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[m] : m.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                // Send the fire at will setting action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        int firearmIndex = -1;
                        for(int i=0; i < trackedAutoMeater.physicalAutoMeaterScript.FireControl.Firearms.Count; ++i)
                        {
                            if (trackedAutoMeater.physicalAutoMeaterScript.FireControl.Firearms[i] == __instance)
                            {
                                firearmIndex = i;
                                break;
                            }
                        }
                        H3MP_ServerSend.AutoMeaterFirearmFireAtWill(trackedAutoMeater.data.trackedID, firearmIndex, b, d);
                    }
                }
                else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        int firearmIndex = -1;
                        for (int i = 0; i < trackedAutoMeater.physicalAutoMeaterScript.FireControl.Firearms.Count; ++i)
                        {
                            if (trackedAutoMeater.physicalAutoMeaterScript.FireControl.Firearms[i] == __instance)
                            {
                                firearmIndex = i;
                                break;
                            }
                        }
                        H3MP_ClientSend.AutoMeaterFirearmFireAtWill(trackedAutoMeater.data.trackedID, firearmIndex, b, d);
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.RespawnRandomSubTarg to sync subtargets with other clients
    class EncryptionRespawnRandSubPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load encryption instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load index
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EncryptionRespawnRandSubPatch), "RespawnSubTarg"))); // Call RespawnSubTarg

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction != null && instruction.operand != null)
                {
                    if (instruction.operand.ToString().Contains("get_activeSelf"))
                    {
                        instructionList.InsertRange(i + 2, toInsert);
                        break;
                    }
                }
            }
            return instructionList;
        }

        public static void RespawnSubTarg(TNH_EncryptionTarget encryption, int index)
        {
            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(encryption) ? H3MP_GameManager.trackedEncryptionByEncryption[encryption] : encryption.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.data.controller == H3MP_GameManager.ID)
                {
                    trackedEncryption.data.subTargsActive[index] = true;

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                    }
                    else
                    {
                        if(trackedEncryption.data.trackedID != -1)
                        {
                            H3MP_ClientSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                        }
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.PopulateInitialRegen to prevent it on non-controllers
    class EncryptionPopulateInitialRegenPatch
    {
        static bool Prefix(ref TNH_EncryptionTarget __instance, ref int ___m_numSubTargsLeft)
        {
            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    ___m_numSubTargsLeft = __instance.StartingRegenSubTarg;
                    return trackedEncryption.data.controller == H3MP_GameManager.ID;
                }
            }
            return true;
        }
    }

    // Patches TNH_EncryptionTarget.SpawnGrowth to sync with other clients
    class EncryptionSpawnGrowthPatch
    {
        public static int skip;

        static void Prefix(ref TNH_EncryptionTarget __instance, int index, Vector3 point)
        {
            if(skip > 0)
            {
                return;
            }

            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.data.controller == H3MP_GameManager.ID)
                {
                    trackedEncryption.data.tendrilsActive[index] = true;
                    trackedEncryption.data.growthPoints[index] = point;
                    trackedEncryption.data.subTargsPos[index] = point;
                    trackedEncryption.data.subTargsActive[index] = true;
                    trackedEncryption.data.tendrilFloats[index] = 1f;
                    Vector3 forward = point - trackedEncryption.physicalEncryptionScript.Tendrils[index].transform.position;
                    trackedEncryption.data.tendrilsRot[index] = Quaternion.LookRotation(forward);
                    trackedEncryption.data.tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.EncryptionSpawnGrowth(trackedEncryption.data.trackedID, index, point);
                    }
                    else
                    {
                        if (trackedEncryption.data.trackedID != -1)
                        {
                            H3MP_ClientSend.EncryptionSpawnGrowth(trackedEncryption.data.trackedID, index, point);
                        }
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.Start to sync recursive init with other clients
    class EncryptionStartPatch
    {
        static bool Prefix(ref TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller != H3MP_GameManager.ID)
                    {
                        Mod.TNH_EncryptionTarget_m_numHitsLeft.SetValue(__instance, __instance.NumHitsTilDestroyed);
                        Mod.TNH_EncryptionTarget_m_maxHits.SetValue(__instance, __instance.NumHitsTilDestroyed);
                        Mod.TNH_EncryptionTarget_m_damLeftForAHit.SetValue(__instance, __instance.DamagePerHit);
                        Mod.TNH_EncryptionTarget_agileStartPos.SetValue(__instance, __instance.transform.position);
                        Mod.TNH_EncryptionTarget_m_fromRot.SetValue(__instance, __instance.transform.rotation);
                        Mod.TNH_EncryptionTarget_m_warpSpeed.SetValue(__instance, UnityEngine.Random.Range(4f, 5f));
                        if (__instance.UsesAgileMovement)
                        {
                            Mod.TNH_EncryptionTarget_m_validAgilePos.SetValue(__instance, new List<Vector3>());
                        }
                        if (__instance.UsesRegenerativeSubTarg)
                        {
                            for (int i = 0; i < __instance.Tendrils.Count; i++)
                            {
                                __instance.Tendrils[i].transform.SetParent(null);
                                __instance.SubTargs[i].transform.SetParent(null);
                            }
                        }
                        if (__instance.UsesSubTargs && !__instance.UsesRecursiveSubTarg && !__instance.UsesRegenerativeSubTarg)
                        {
                            Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(__instance, __instance.SubTargs.Count);
                        }

                        return false;
                    }

                    return true;
                }
            }

            return true;
        }

        static void Postfix(ref TNH_EncryptionTarget __instance)
        {
            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.physicalEncryptionScript.UsesRecursiveSubTarg && trackedEncryption.data.controller == H3MP_GameManager.ID)
                {
                    if(trackedEncryption.data.controller == H3MP_GameManager.ID)
                    {
                        List<int> indices = new List<int>();
                        for(int i=0; i < trackedEncryption.physicalEncryptionScript.SubTargs.Count; i++)
                        {
                            if (trackedEncryption.physicalEncryptionScript.SubTargs[i].activeSelf)
                            {
                                trackedEncryption.data.subTargsActive[i] = true;
                                indices.Add(i);
                            }
                        }

                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.EncryptionRecursiveInit(trackedEncryption.data.trackedID, indices);
                        }
                        else
                        {
                            if (trackedEncryption.data.trackedID != -1)
                            {
                                H3MP_ClientSend.EncryptionRecursiveInit(trackedEncryption.data.trackedID, indices);
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.ResetGrowth to sync with other clients
    class EncryptionResetGrowthPatch
    {
        public static int skip; 

        static bool Prefix(ref TNH_EncryptionTarget __instance, int index, Vector3 point)
        {
            if(skip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller == H3MP_GameManager.ID)
                    {
                        trackedEncryption.data.growthPoints[index] = point;
                        trackedEncryption.data.tendrilFloats[index] = 0;
                        Vector3 forward = point - __instance.Tendrils[index].transform.position;
                        trackedEncryption.data.tendrilsRot[index] = Quaternion.LookRotation(forward);
                        trackedEncryption.data.tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.EncryptionResetGrowth(trackedEncryption.data.trackedID, index, point);
                        }
                        else
                        {
                            if (trackedEncryption.data.trackedID != -1)
                            {
                                H3MP_ClientSend.EncryptionResetGrowth(trackedEncryption.data.trackedID, index, point);
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Patches TNH_EncryptionTarget.DisableSubtarg to sync with other clients
    class EncryptionDisableSubtargPatch
    {
        static bool wasActive;

        static bool Prefix(ref TNH_EncryptionTarget __instance, int i)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller != H3MP_GameManager.ID)
                    {
                        return false;
                    }
                    else
                    {
                        wasActive = __instance.SubTargs[i].activeSelf;
                    }
                }
            }

            return true;
        }

        static void Postfix(ref TNH_EncryptionTarget __instance, int i)
        {
            // Instance could be null if destroyed by the method, in which case we don't need to send anything
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && __instance != null && wasActive && !__instance.SubTargs[i].activeSelf)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller == H3MP_GameManager.ID)
                    {
                        trackedEncryption.data.subTargsActive[i] = false;

                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.EncryptionDisableSubtarg(trackedEncryption.data.trackedID, i);
                        }
                        else
                        {
                            if (trackedEncryption.data.trackedID != -1)
                            {
                                H3MP_ClientSend.EncryptionDisableSubtarg(trackedEncryption.data.trackedID, i);
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.Update to prevent on non-controllers
    class EncryptionUpdatePatch
    {
        static bool Prefix(ref TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller != H3MP_GameManager.ID)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Patches TNH_EncryptionTarget.FixedUpdate to prevent on non-controllers
    class EncryptionFixedUpdatePatch
    {
        static bool Prefix(ref TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller != H3MP_GameManager.ID)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
    #endregion

    #region Instatiation Patches
    // Patches FVRFireArmChamber.EjectRound so we can keep track of when a round is ejected from a chamber
    class ChamberEjectRoundPatch
    {
        static bool track = false;
        static int incrementedSkip = 0;

        static void Prefix(ref FVRFireArmChamber __instance, ref FVRFireArmRound ___m_round, bool ForceCaseLessEject)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
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
                        currentParent = currentParent.parent;
                    }

                    // Check if we should control and sync it, if so do it in postfix
                    if (trackedItem == null || trackedItem.data.controller == H3MP_GameManager.ID)
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

            // Skip if not connected
            if (__result == null || Mod.managerObject == null)
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
                if (H3MP_GameManager.playersPresent > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                H3MP_GameManager.SyncTrackedSosigs((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, null, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedAutoMeaters((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedEncryptions((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
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

            // Skip if not connected
            if (data == null || Mod.managerObject == null)
            {
                return;
            }

            // If this is a game object check and sync all physical objects if necessary
            if (data is GameObject)
            {
                // Check if has tracked parent
                Transform currentParent = parent;
                parentData = null;
                while (currentParent != null)
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
                track = parentData == null || parentData.controller == H3MP_GameManager.ID;
            }
        }

        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }
            if (Mod.managerObject == null)
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
                if (H3MP_GameManager.playersPresent > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
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
                H3MP_GameManager.SyncTrackedSosigs((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, parentData, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedAutoMeaters((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedEncryptions((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
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

            // Skip if not connected
            if (__result == null || Mod.managerObject == null)
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
                if (H3MP_GameManager.playersPresent > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                H3MP_GameManager.SyncTrackedSosigs((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, null, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedAutoMeaters((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedEncryptions((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
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

            // Skip if not connected
            if (data == null || Mod.managerObject == null)
            {
                return;
            }

            // If this is a game object check and sync all physical objects if necessary
            if (data is GameObject)
            {
                // Check if has tracked parent
                Transform currentParent = parent;
                parentData = null;
                while (currentParent != null)
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
                track = parentData == null || parentData.controller == H3MP_GameManager.ID;
            }
        }

        static void Postfix(ref UnityEngine.Object __result, Transform parent)
        {
            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }
            // Skip if not connected
            if (Mod.managerObject == null)
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
                if (H3MP_GameManager.playersPresent > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
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
                H3MP_GameManager.SyncTrackedSosigs((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedItems((__result as GameObject).transform, true, parentData, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedAutoMeaters((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
                H3MP_GameManager.SyncTrackedEncryptions((__result as GameObject).transform, true, SceneManager.GetActiveScene().name);
            }
        }
    }

    // Patches FVRSceneSettings.LoadDefaultSceneRoutine so we know when we spawn items from vault as part of scene loading
    class LoadDefaultSceneRoutinePatch
    {
        public static bool inLoadDefaultSceneRoutine;

        static void Prefix()
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || !H3MP_GameManager.PlayersPresentSlow())
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
            if (Mod.managerObject == null || !H3MP_GameManager.PlayersPresentSlow())
            {
                return;
            }

            if (LoadDefaultSceneRoutinePatch.inLoadDefaultSceneRoutine)
            {
                if (SpawnVaultFileRoutinePatch.filesToSkip == null)
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
                    if(obj == null)
                    {
                        Debug.LogWarning("SpawnVaultFileRoutinePatch.FinishedRoutine object to be destroyed already null");
                        continue;
                    }
                    ++H3MP_TrackedItem.skipDestroy;
                    UnityEngine.Object.Destroy(obj);
                    --H3MP_TrackedItem.skipDestroy;
                }
                routineData.Remove(currentFile);
            }
        }

        static void Prefix(ref VaultFile ___f)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || !H3MP_GameManager.PlayersPresentSlow())
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
    #endregion

    #region Damageable Patches
    // TODO: Patch IFVRDamageable.Damage and have a way to track damageables so we don't need to have a specific TCP call for each
    //       Or make sure we track damageables, then when we can patch damageable.damage and send the damage and trackedID directly to other clients so they can process it too

    // Patches BallisticProjectile.Fire to keep a reference to the source firearm
    class ProjectileFirePatch
    {
        static void Postfix(ref FVRFireArm ___tempFA, ref FVRFireArm firearm)
        {
            ___tempFA = firearm;
        }
    }

    // Patches BallisticProjectile.MoveBullet to ignore latest IFVRDamageable if necessary
    class BallisticProjectileDamageablePatch
    {
        public static bool GetActualFlag(bool flag2, FVRFireArm tempFA)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return flag2;
            }
            Debug.Log("GetActualFlag of projectile patch called");

            if (flag2)
            {
                Debug.Log("\tWe have damageable");
                if (tempFA == null)
                {
                    Debug.Log("\t\ttempFA null");
                    // If we don't have a ref to the firearm that fired this projectile, let the damage be controlled by the best host
                    int bestHost = Mod.GetBestPotentialObjectHost(-1);
                    return bestHost == -1 || bestHost == H3MP_GameManager.ID;
                }
                else // We have a ref to the firearm that fired this projectile
                {
                    Debug.Log("\t\tWe have ref to firearm");
                    // We only want to let this projectile do damage if we control the firearm
                    H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(tempFA) ? H3MP_GameManager.trackedItemByItem[tempFA] : tempFA.GetComponent<H3MP_TrackedItem>();
                    if (trackedItem == null)
                    {
                        Debug.Log("\t\t\tNo tracked item on firearm");
                        return false;
                    }
                    else
                    {
                        Debug.Log("\t\t\tFound tracked item on firearm, control projectile damage?: "+ (trackedItem.data.controller == H3MP_GameManager.ID));
                        return trackedItem.data.controller == H3MP_GameManager.ID;
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
            toInsertFirst.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BallisticProjectileDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
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
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
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
        // Patches Explode()
        static IEnumerable<CodeInstruction> ExplosionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
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

        // Patches DoMeleeDamageInCollision()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SosigWeapon instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    // Skip the first set
                    if (!found)
                    {
                        found = true;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }

        // Patches Update()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SosigWeapon instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    instructionList.InsertRange(i + 1, toInsert);

                    break;
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
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
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
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return original;
            }

            if (original != null)
            {
                H3MP_ControllerReference cr = mb.GetComponent<H3MP_ControllerReference>();
                if (cr == null)
                {
                    H3MP_TrackedItem ti = mb.GetComponent<H3MP_TrackedItem>();
                    if (ti == null)
                    {
                        // Controller of damaging item unknown, lest best postential host control it
                        int bestHost = Mod.GetBestPotentialObjectHost(-1);
                        return (bestHost == H3MP_GameManager.ID || bestHost == -1) ? original : null;
                    }
                    else // We have a ref to the item itself
                    {
                        // We only want to let this item do damage if we control it
                        return ti.data.controller == H3MP_GameManager.ID ? original : null;
                    }
                }
                else // We have a ref to the controller of the item that caused this damage
                {
                    // We only want to let this item do damage if we control it
                    return cr.controller == H3MP_GameManager.ID ? original : null;
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

    // Patches MeleeParams to ignore latest IFVRDamageable if necessary
    class MeleeParamsDamageablePatch
    {
        // Patches DoStabDamage()
        static IEnumerable<CodeInstruction> StabTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load meleeparams instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRPhysicalObject.MeleeParams), "m_obj"))); // Load m_obj from instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 9)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 9)); // Set damageable

            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (9)"))
                {
                    // Skip the first set
                    if (!found)
                    {
                        found = true;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }

        // Patches DoTearOutDamage()
        static IEnumerable<CodeInstruction> TearOutTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load meleeparams instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRPhysicalObject.MeleeParams), "m_obj"))); // Load m_obj from instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 6)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 6)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (6)"))
                {
                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }

        // Patches FixedUpdate()
        static bool UpdatePrefix(ref FVRPhysicalObject ___m_obj)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || ___m_obj == null || H3MP_GameManager.playersPresent == 0)
            {
                return true;
            }

            // Skip if not controller of this melee params' parent object
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(___m_obj) ? H3MP_GameManager.trackedItemByItem[___m_obj] : ___m_obj.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
            {
                return false;
            }
            return true;
        }
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load meleeparams instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRPhysicalObject.MeleeParams), "m_obj"))); // Load m_obj from instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 14)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 14)); // Set damageable

            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (14)"))
                {
                    // Skip the first set
                    if (!found)
                    {
                        found = true;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }

        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load meleeparams instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRPhysicalObject.MeleeParams), "m_obj"))); // Load m_obj from instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 18)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 18)); // Set damageable

            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (18)"))
                {
                    // Skip the first set
                    if (!found)
                    {
                        found = true;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches AIMeleeWeapon to ignore latest IFVRDamageable if necessary
    class AIMeleeDamageablePatch
    {
        public static IFVRReceiveDamageable GetActualReceiveDamageable(MonoBehaviour mb, IFVRReceiveDamageable original)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return original;
            }

            if (original != null)
            {
                H3MP_ControllerReference cr = mb.GetComponent<H3MP_ControllerReference>();
                if (cr == null)
                {
                    H3MP_TrackedItem ti = mb.GetComponent<H3MP_TrackedItem>();
                    if (ti == null)
                    {
                        // If we don't have a ref to the controller of the item that caused this damage, let the damage be controlled by the
                        // first player we can find in the same scene
                        // TODO: Keep a dictionary of players using the scene as key
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            if (player.Value.gameObject.activeSelf)
                            {
                                firstPlayerInScene = player.Key;
                            }
                            break;
                        }
                        if (firstPlayerInScene != H3MP_GameManager.ID)
                        {
                            return null;
                        }
                    }
                    else // We have a ref to the item itself
                    {
                        // We only want to let this item do damage if we control it
                        return ti.data.controller == H3MP_GameManager.ID ? original : null;
                    }
                }
                else // We have a ref to the controller of the item that caused this damage
                {
                    // We only want to let this item do damage if we control it
                    return cr.controller == H3MP_GameManager.ID ? original : null;
                }
            }
            return original;
        }

        // Patches Fire()
        static IEnumerable<CodeInstruction> FireTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AImeleeweapon instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set damageable
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AImeleeweapon instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load receivedamageable
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AIMeleeDamageablePatch), "GetActualReceiveDamageable"))); // Call GetActualDamageable
            toInsert0.Add(new CodeInstruction(OpCodes.Stloc_S, 4)); // Set receivedamageable

            bool skipNext3 = false;
            bool skipNext4 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    // Skip the next stloc 3 after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext3)
                    {
                        skipNext3 = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    skipNext3 = true;
                }
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRReceiveDamageable (4)"))
                {
                    if (skipNext4)
                    {
                        skipNext4 = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert0);

                    skipNext4 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches FVRProjectile to ignore latest IFVRDamageable if necessary
    class ProjectileDamageablePatch
    {
        // Patches MoveBullet()
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load FVRProjectile instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load receivedamageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AIMeleeDamageablePatch), "GetActualReceiveDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set receivedamageable

            bool skipNext3 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    // Skip the next stloc 3 after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext3)
                    {
                        skipNext3 = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    skipNext3 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches AutoMeaterBlade to ignore latest IFVRDamageable if necessary
    class AutoMeaterBladeDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load AutoMeaterBlade instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 10)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 10)); // Set damageable

            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (10)"))
                {
                    // Skip first set
                    if (!found)
                    {
                        found = true;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches BangSnap to ignore latest IFVRDamageable if necessary
    class BangSnapDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load BangSnap instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches BearTrapInteractiblePiece to ignore latest IFVRDamageable if necessary
    class BearTrapDamageablePatch
    {
        // Patches SnapShut()
        static IEnumerable<CodeInstruction> SnapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load BearTrapInteractiblePiece instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 5)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches Chainsaw to ignore latest IFVRDamageable if necessary
    class ChainsawDamageablePatch
    {
        // Patches OnCollisionStay()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Chainsaw instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_2)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_2)); // Set damageable

            bool skipNext2 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    // Skip the next stloc 2 after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext2)
                    {
                        skipNext2 = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    skipNext2 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches Drill to ignore latest IFVRDamageable if necessary
    class DrillDamageablePatch
    {
        // Patches OnCollisionStay()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Drill instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_2)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_2)); // Set damageable

            bool skipNext2 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    // Skip the next stloc 2 after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext2)
                    {
                        skipNext2 = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    skipNext2 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches DropTrapLogs to ignore latest IFVRDamageable if necessary
    class DropTrapDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load DropTrapLogs instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 5)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i+1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches Flipzo to ignore latest IFVRDamageable if necessary
    class FlipzoDamageablePatch
    {
        // Patches FVRUpdate()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load DropTrapLogs instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 7)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (7)"))
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i+1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches FVRIgnitable to ignore latest IFVRDamageable if necessary
    class IgnitableDamageablePatch
    {
        // Patches Start()
        static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load FVRIgnitable instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches FVRSparkler to ignore latest IFVRDamageable if necessary
    class SparklerDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load FVRSparkler instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches FVRStrikeAnyWhereMatch to ignore latest IFVRDamageable if necessary
    class MatchDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load FVRStrikeAnyWhereMatch instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches HCBBolt to ignore latest IFVRDamageable if necessary
    class HCBBoltDamageablePatch
    {
        // Patches DamageOtherThing()
        static IEnumerable<CodeInstruction> DamageOtherTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load FVRStrikeAnyWhereMatch instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches Kabot.KSpike to ignore latest IFVRDamageable if necessary
    class KabotDamageablePatch
    {
        // Patches Tick()
        static IEnumerable<CodeInstruction> TickTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load KSpike instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Kabot.KSpike), "K"))); // Load Kabot
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches MeatCrab to ignore latest IFVRDamageable if necessary
    class MeatCrabDamageablePatch
    {
        // Patches Crabdate_Attached()
        static IEnumerable<CodeInstruction> AttachedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MeatCrab instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }

        // Patches Crabdate_Lunging()
        static IEnumerable<CodeInstruction> LungingTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MeatCrab instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 5)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches MF2_BearTrapInteractionZone to ignore latest IFVRDamageable if necessary
    class MF2_BearTrapDamageablePatch
    {
        // Patches SnapShut()
        static IEnumerable<CodeInstruction> SnapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MF2_BearTrapInteractionZone instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 5)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches MG_FlyingHotDogSwarm to ignore latest IFVRDamageable if necessary
    class MG_SwarmDamageablePatch
    {
        // Patches FireShot()
        static IEnumerable<CodeInstruction> FireTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MG_FlyingHotDogSwarm instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches MG_JerryTheLemon to ignore latest IFVRDamageable if necessary
    class MG_JerryDamageablePatch
    {
        // Patches FireBolt()
        static IEnumerable<CodeInstruction> FireTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load MG_JerryTheLemon instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches Microtorch to ignore latest IFVRDamageable if necessary
    class MicrotorchDamageablePatch
    {
        // Patches Update()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Microtorch instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            bool skipNext = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext)
                    {
                        skipNext = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext = true;
                }
            }
            return instructionList;
        }
    }

    // Patches PowerUp_Cyclops to ignore latest IFVRDamageable if necessary
    class CyclopsDamageablePatch
    {
        // Patches Update()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // Add a local var for damageable
            LocalBuilder localDamageable = il.DeclareLocal(typeof(IFVRDamageable));
            localDamageable.SetLocalSymInfo("damageable");

            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Powerup instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(PowerUp_Cyclops), "m_hit"))); // Load address of m_hit
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RaycastHit), "get_collider"))); // Call get collider on it
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Collider), "get_attachedRigidbody"))); // Call get attached RB on it
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject"))); // Call get go on it
            CodeInstruction newCodeInstruction = CodeInstruction.Call(typeof(GameObject), "GetComponent", null, new Type[] { typeof(IFVRDamageable) });
            newCodeInstruction.opcode = OpCodes.Callvirt;
            toInsert.Add(newCodeInstruction); // Call get damageable on it
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set damageable
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Powerup instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set damageable

            bool foundBreak = false;
            int popIndex = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Emit"))
                {
                    popIndex = i;
                    instructionList.InsertRange(i + 1, toInsert);
                }

                if (instruction.opcode == OpCodes.Brfalse)
                {
                    // Only apply to second brfalse
                    if (!foundBreak)
                    {
                        foundBreak = true;
                        continue;
                    }

                    // Remove getcomponent call lines
                    instructionList.RemoveRange(i + 1, 6);

                    // Load damageable
                    instructionList.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_3));

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches RealisticLaserSword to ignore latest IFVRDamageable if necessary
    class LaserSwordDamageablePatch
    {
        // Patches FVRFixedUpdate()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // Add a local var for damageable
            LocalBuilder localDamageable = il.DeclareLocal(typeof(IFVRDamageable));
            localDamageable.SetLocalSymInfo("damageable");

            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load RealisticLaserSword instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(PowerUp_Cyclops), "m_hit"))); // Load address of m_hit
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RaycastHit), "get_collider"))); // Call get collider on it
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Collider), "get_attachedRigidbody"))); // Call get attached RB on it
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject"))); // Call get go on it
            toInsert.Add(CodeInstruction.Call(typeof(GameObject), "GetComponent", null, new Type[] { typeof(IFVRDamageable) })); // Call get damageable on it
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 8)); // Set damageable
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load RealisticLaserSword instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 8)); // Set damageable

            int foundBreak = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Emit"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                }

                if (instruction.opcode == OpCodes.Brfalse)
                {
                    // Only apply to third brfalse
                    if (foundBreak < 2)
                    {
                        ++foundBreak;
                        continue;
                    }

                    // Remove getcomponent call lines
                    instructionList.RemoveRange(i + 1, 6);

                    // Load damageable
                    instructionList.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_S, 8));

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches RotrwCharcoal to ignore latest IFVRDamageable if necessary
    class CharcoalDamageablePatch
    {
        // Patches DamageBubble()
        static IEnumerable<CodeInstruction> BubbleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load RotrwCharcoal instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    instructionList.InsertRange(i + 1, toInsert);

                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches SlicerBladeMaster to ignore latest IFVRDamageable if necessary
    class SlicerDamageablePatch
    {
        // Patches FixedUpdate()
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SlicerBladeMaster instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SlicerBladeMaster instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load damageable
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert0.Add(new CodeInstruction(OpCodes.Stloc_S, 4)); // Set damageable

            bool skipNext1 = false;
            bool skipNext4 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext1)
                    {
                        skipNext1 = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext1 = true;
                }
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (4)"))
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext4)
                    {
                        skipNext4 = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert0);

                    skipNext4 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches SpinninBladeTrapBase to ignore latest IFVRDamageable if necessary
    class SpinningBladeDamageablePatch
    {
        // Patches OnCollisionEnter()
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load SpinninBladeTrapBase instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load damageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_1)); // Set damageable

            bool skipNext1 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    // Skip the next stloc after patching one because we add one ourselves, if we don't skip ours we end up in inf loop
                    if (skipNext1)
                    {
                        skipNext1 = false;
                        continue;
                    }

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext1 = true;
                }
            }
            return instructionList;
        }
    }

    // Patches SosigLink.Damage to keep track of damage taken by a sosig
    class SosigLinkDamagePatch
    {
        public static int skip;
        static H3MP_TrackedSosig trackedSosig;

        static bool Prefix(ref SosigLink __instance, Damage d)
        {
            if(skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Sosig could have been destroyed by the damage, we can just skip because the destroy order will be sent to other clients
            if(__instance == null)
            {
                return true;
            }

            // If in control of the damaged sosig link, we want to process the damage
            trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if(trackedSosig.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        for (int i=0; i < __instance.S.Links.Count; ++i)
                        {
                            if (__instance.S.Links[i] == __instance)
                            {
                                H3MP_ServerSend.SosigLinkDamage(trackedSosig.data, i, d);
                                break;
                            }
                        }
                        return false;
                    }
                }
                else if(trackedSosig.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < __instance.S.Links.Count; ++i)
                    {
                        if (__instance.S.Links[i] == __instance)
                        {
                            H3MP_ClientSend.SosigLinkDamage(trackedSosig.data.trackedID, i, d);
                            break;
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        static void Postfix(ref SosigLink __instance)
        {
            // If in control of the damaged sosig link, we want to send the damage results to other clients
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedSosig.data.controller == 0)
                    {
                        H3MP_ServerSend.SosigDamageData(trackedSosig);
                    }
                }
                else if (trackedSosig.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.SosigDamageData(trackedSosig);
                }
            }
        }
    }

    // Patches SosigWearable.Damage to keep track of damage taken by a sosig
    class SosigWearableDamagePatch
    {
        public static int skip;
        static H3MP_TrackedSosig trackedSosig;

        static bool Prefix(ref SosigWearable __instance, Damage d)
        {
            // SosigWearable.Damage could call a SosigLink.Damage
            // This would trigger the sosig link damage patch seperataly, but we want to handle these as one, so just skip it
            ++SosigLinkDamagePatch.skip;

            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control of the damaged sosig wearable, we want to process the damage
            trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if(trackedSosig.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        for (int i = 0; i < __instance.S.Links.Count; ++i)
                        {
                            if (__instance.S.Links[i] == __instance.L)
                            {
                                List<SosigWearable> linkWearables = (List<SosigWearable>)Mod.SosigLink_m_wearables.GetValue(__instance.L);
                                for (int j = 0; j < linkWearables.Count; ++j)
                                {
                                    if (linkWearables[j] == __instance)
                                    {
                                        H3MP_ServerSend.SosigWearableDamage(trackedSosig.data, i, j, d);
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
                else if(trackedSosig.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < __instance.S.Links.Count; ++i)
                    {
                        if (__instance.S.Links[i] == __instance.L)
                        {
                            List<SosigWearable> linkWearables = (List<SosigWearable>)Mod.SosigLink_m_wearables.GetValue(__instance.L);
                            for (int j = 0; j < linkWearables.Count; ++j)
                            {
                                if (linkWearables[j] == __instance)
                                {
                                    H3MP_ClientSend.SosigWearableDamage(trackedSosig.data.trackedID, i, j, d);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        static void Postfix(ref SosigWearable __instance)
        {
            --SosigLinkDamagePatch.skip;

            // If in control of the damaged sosig link, we want to send the damage results to other clients
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedSosig.data.controller == 0)
                    {
                        H3MP_ServerSend.SosigDamageData(trackedSosig);
                    }
                }
                else if (trackedSosig.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.SosigDamageData(trackedSosig);
                }
            }
        }
    }

    // Patches TNH_ShatterableCrate to keep track of damage to TNH supply boxes
    class TNH_ShatterableCrateDamagePatch 
    {
        public static int skip;
        static H3MP_TrackedItem trackedItem;

        static bool Prefix(ref TNH_ShatterableCrate __instance, Damage d)
        {
            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            Debug.Log("TNH_ShatterableCrateDamagePatch called");
            // If in control of the damaged crate, we want to process the damage
            trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                Debug.Log("\tCrate tracked");
                if (H3MP_ThreadManager.host)
                {
                    Debug.Log("\t\tWe are host");
                    if (trackedItem.data.controller == 0)
                    {
                        Debug.Log("\t\t\tWe control");
                        return true;
                    }
                    else
                    {
                        Debug.Log("\t\t\tWe don't control, server sending damage");
                        // Not in control, we want to send the damage to the controller for them to process it
                        H3MP_ServerSend.ShatterableCrateDamage(trackedItem.data.trackedID, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    Debug.Log("\t\tWe are not host, we control");
                    return true;
                }
                else
                {
                    Debug.Log("\t\tWe are not host, we dont control, client sending damage");
                    H3MP_ClientSend.ShatterableCrateDamage(trackedItem.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }
    }

    // Patches TNH_ShatterableCrate.Destroy to keep track of destruction
    class TNH_ShatterableCrateDestroyPatch 
    {
        public static int skip;
        static H3MP_TrackedItem trackedItem;

        static void Prefix(ref TNH_ShatterableCrate __instance, Damage dam)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            // Note that this should only ever be called without skip from Damage()
            // And we already check for control in DamagePatch so no need to check for control here
            trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.ShatterableCrateDestroy(trackedItem.data.trackedID, dam);
                }
                else
                {
                    H3MP_ClientSend.ShatterableCrateDestroy(trackedItem.data.trackedID, dam);
                }
            }
        }
    }

    // Patches AutoMeater.Damage to keep track of damage taken by an AutoMeater
    class AutoMeaterDamagePatch
    {
        public static int skip;
        static H3MP_TrackedAutoMeater trackedAutoMeater;

        static bool Prefix(ref AutoMeater __instance, Damage d)
        {
            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control of the damaged AutoMeater, we want to process the damage
            trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        H3MP_ServerSend.AutoMeaterDamage(trackedAutoMeater.data, d);
                        return false;
                    }
                }
                else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.AutoMeaterDamage(trackedAutoMeater.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }

        // TODO: Currently no data is necessary to sync after damage, need review
        //static void Postfix(ref AutoMeater __instance)
        //{
        //    // If in control of the damaged sosig link, we want to send the damage results to other clients
        //    if (trackedAutoMeater != null)
        //    {
        //        if (H3MP_ThreadManager.host)
        //        {
        //            if (trackedAutoMeater.data.controller == 0)
        //            {
        //                H3MP_ServerSend.AutoMeaterDamageData(trackedAutoMeater);
        //            }
        //        }
        //        else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
        //        {
        //            H3MP_ClientSend.AutoMeaterDamageData(trackedAutoMeater);
        //        }
        //    }
        //}
    }

    // Patches AutoMeater.Damage to keep track of damage taken by an AutoMeater
    class AutoMeaterHitZoneDamagePatch
    {
        public static int skip;
        static H3MP_TrackedAutoMeater trackedAutoMeater;

        static bool Prefix(ref AutoMeaterHitZone __instance, ref AutoMeater.AMHitZoneType ___Type, Damage d)
        {
            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control of the damaged AutoMeater, we want to process the damage
            trackedAutoMeater = H3MP_GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance.M) ? H3MP_GameManager.trackedAutoMeaterByAutoMeater[__instance.M] : __instance.M.GetComponent<H3MP_TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        H3MP_ServerSend.AutoMeaterHitZoneDamage(trackedAutoMeater.data, (byte)___Type, d);
                        return false;
                    }
                }
                else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.AutoMeaterHitZoneDamage(trackedAutoMeater.data.trackedID, ___Type, d);
                    return false;
                }
            }
            return true;
        }

        static void Postfix(ref AutoMeaterHitZone __instance, ref AutoMeater.AMHitZoneType ___Type)
        {
            // If in control of the damaged AutoMeater, we want to send the damage results to other clients
            if (trackedAutoMeater != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        H3MP_ServerSend.AutoMeaterHitZoneDamageData(trackedAutoMeater.data.trackedID, __instance);
                    }
                }
                else if (trackedAutoMeater.data.controller == H3MP_Client.singleton.ID)
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        H3MP_ClientSend.AutoMeaterHitZoneDamageData(trackedAutoMeater.data.trackedID, __instance);
                    }
                }
            }
        }
    }
    
    // Patches TNH_EncryptionTarget.Damage to keep track of damage taken by an encryption
    class EncryptionDamagePatch
    {
        public static int skip;
        static H3MP_TrackedEncryption trackedEncryption;

        static bool Prefix(ref TNH_EncryptionTarget __instance, Damage d)
        {
            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control of the damaged Encryption, we want to process the damage
            trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
            if (trackedEncryption != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedEncryption.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        H3MP_ServerSend.EncryptionDamage(trackedEncryption.data, d);
                        return false;
                    }
                }
                else if (trackedEncryption.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.EncryptionDamage(trackedEncryption.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }

        static void Postfix(ref TNH_EncryptionTarget __instance)
        {
            // If in control of the damaged Encryption, we want to send the damage results to other clients
            // Instance could ben ull if damage destroyed it, at which point the destroy order will be sent, we don't need to send damage data
            if (trackedEncryption != null && __instance != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedEncryption.data.controller == 0)
                    {
                        H3MP_ServerSend.EncryptionDamageData(trackedEncryption);
                    }
                }
                else if (trackedEncryption.data.controller == H3MP_Client.singleton.ID && trackedEncryption.data.trackedID != -1)
                {
                    H3MP_ClientSend.EncryptionDamageData(trackedEncryption);
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget_SubTarget.Damage to keep track of damage taken by an encryption's sub target
    class EncryptionSubDamagePatch
    {
        public static int skip;
        static H3MP_TrackedEncryption trackedEncryption;

        static bool Prefix(ref TNH_EncryptionTarget_SubTarget __instance, Damage d)
        {
            if(skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control of the damaged Encryption, we want to process the damage
            trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance.Target) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance.Target] : __instance.Target.GetComponent<H3MP_TrackedEncryption>();
            if (trackedEncryption != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedEncryption.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        H3MP_ServerSend.EncryptionSubDamage(trackedEncryption.data, __instance.Index, d);
                        return false;
                    }
                }
                else if (trackedEncryption.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.EncryptionSubDamage(trackedEncryption.data.trackedID, __instance.Index, d);
                    return false;
                }
            }
            return true;
        }
    }
    #endregion

    #region TNH Patches
    // Patches GM.set_TNH_Manager() to keep track of TNH Manager instances
    class SetTNHManagerPatch
    {
        static void Postfix()
        {
            // Disable the TNH_Manager if we are not the host
            // Also manage currently playing in the TNH instance
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    if (GM.TNH_Manager != null)
                    {
                        // Keep our own reference
                        Mod.currentTNHInstance.manager = GM.TNH_Manager;

                        if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                        {
                            if (Mod.currentTNHInstance.playerIDs[0] == H3MP_GameManager.ID)
                            {
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.SetTNHController(Mod.currentTNHInstance.instance, 0);
                                }
                                else
                                {
                                    H3MP_ClientSend.SetTNHController(Mod.currentTNHInstance.instance, H3MP_Client.singleton.ID);
                                }
                            }
                            //else
                            //{
                            //    ++skip;
                            //    GM.TNH_Manager.enabled = false;
                            //    --skip;
                            //}

                            // If there are already players, it means the TNH game is already in some state
                            // Make sure we are in that state
                            TNH_ManagerPatch.doInit = true;
                        }
                        else
                        {
                            if (H3MP_ThreadManager.host)
                            {
                                Mod.currentTNHInstance.controller = 0;
                                H3MP_ServerSend.SetTNHController(Mod.currentTNHInstance.instance, 0);
                            }
                            else
                            {
                                Mod.currentTNHInstance.controller = H3MP_GameManager.ID;
                                H3MP_ClientSend.SetTNHController(Mod.currentTNHInstance.instance, H3MP_Client.singleton.ID);
                            }
                        }

                        Mod.currentTNHInstance.AddCurrentlyPlaying(true, H3MP_GameManager.ID);
                        Mod.currentlyPlayingTNH = true;
                    }
                    else // TNH_Manager was set to null
                    {
                        Mod.currentlyPlayingTNH = false;
                        Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, H3MP_GameManager.ID);

                        // If was manager controller, give manager control to next currently playing
                        if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                        {
                            int nextID = Mod.currentTNHInstance.currentlyPlaying.Count > 0 ? Mod.currentTNHInstance.currentlyPlaying[0] : -1;
                            if (H3MP_ThreadManager.host)
                            {
                                H3MP_ServerSend.SetTNHController(Mod.currentTNHInstance.instance, nextID);
                                H3MP_ServerSend.TNHData(nextID, Mod.currentTNHInstance.manager);
                            }
                            else
                            {
                                H3MP_ClientSend.SetTNHController(Mod.currentTNHInstance.instance, nextID);
                                H3MP_ClientSend.TNHData(nextID, Mod.currentTNHInstance.manager);
                            }
                        }
                    }
                }
                else // We just set TNH_Manager but we are not in a TNH instance
                {
                    if (GM.TNH_Manager == null)
                    {
                        // Just left a TNH game, must set instance to 0
                        H3MP_GameManager.SetInstance(0);
                    }
                    else
                    {
                        // Just started a TNH game, must set instance to a new instance to play TNH solo
                        Mod.setLatestInstance = true;
                        H3MP_GameManager.AddNewInstance();
                    }
                }
            }
        }
    }

    // Patches TNH_UIManager to keep track of TNH Options
    class TNH_UIManagerPatch
    {
        public static int progressionSkip;
        public static int equipmentSkip;
        public static int healthModeSkip;
        public static int targetSkip;
        public static int AIDifficultySkip;
        public static int radarSkip;
        public static int itemSpawnerSkip;
        public static int backpackSkip;
        public static int healthMultSkip;
        public static int sosigGunReloadSkip;
        public static int seedSkip;

        static bool ProgressionPrefix(int i)
        {
            if (progressionSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if(Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.progressionTypeSetting = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHProgression(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHProgression(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool EquipmentPrefix(int i)
        {
            if (equipmentSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.equipmentModeSetting = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHEquipment(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHEquipment(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool HealthModePrefix(int i)
        {
            if (healthModeSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.healthModeSetting = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHHealthMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHHealthMode(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool TargetPrefix(int i)
        {
            if (targetSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.targetModeSetting = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHTargetMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHTargetMode(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool AIDifficultyPrefix(int i)
        {
            if (AIDifficultySkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.AIDifficultyModifier = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHAIDifficulty(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHAIDifficulty(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool RadarModePrefix(int i)
        {
            if (radarSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.radarModeModifier = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHRadarMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHRadarMode(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool ItemSpawnerModePrefix(int i)
        {
            if (itemSpawnerSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.itemSpawnerMode = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHItemSpawnerMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHItemSpawnerMode(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool BackpackModePrefix(int i)
        {
            if (backpackSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.backpackMode = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHBackpackMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHBackpackMode(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool HealthMultPrefix(int i)
        {
            if (healthMultSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.healthMult = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHHealthMult(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHHealthMult(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool SosigGunReloadPrefix(int i)
        {
            if (sosigGunReloadSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.sosiggunShakeReloading = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHSosigGunReload(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHSosigGunReload(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool SeedPrefix(int i)
        {
            if (seedSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.TNHSeed = i;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHSeed(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHSeed(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool NextLevelPrefix(ref TNH_UIManager __instance, ref int ___m_currentLevelIndex)
        {
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Claculate new level index
                    int levelIndex = ___m_currentLevelIndex + 1;
                    if (levelIndex >= __instance.Levels.Count)
                    {
                        levelIndex = 0;
                    }

                    // Update locally
                    Mod.currentTNHInstance.levelIndex = levelIndex;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHLevelIndex(levelIndex, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHLevelIndex(levelIndex, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool PrevLevelPrefix(ref TNH_UIManager __instance, ref int ___m_currentLevelIndex)
        {
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Prevent setting the option if there is already someone playing on this instance
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Claculate new level index
                    int levelIndex = ___m_currentLevelIndex - 1;
                    if (levelIndex < 0)
                    {
                        levelIndex = __instance.Levels.Count;
                    }

                    // Update locally
                    Mod.currentTNHInstance.levelIndex = levelIndex;

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.SetTNHLevelIndex(levelIndex, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.SetTNHLevelIndex(levelIndex, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }
    }

    // Patches TNH_Token to keep track of token events
    class TNH_TokenPatch
    {
        // Prevent addToken to be passed to other clients if just a token picked up from ground
        // The tokens will be client side
        // This also means that we require that tokens are always spawned on each client
        static void CollectPrefix()
        {
            ++TNH_ManagerPatch.addTokensSkip;
        }

        static void CollectPostfix()
        {
            --TNH_ManagerPatch.addTokensSkip;
        }
    }

    // Patches TNH_Manager to keep track of TNH events
    class TNH_ManagerPatch
    {
        public static int addTokensSkip;
        public static int sosigKillSkip;
        public static bool doInit;

        static bool PlayerDiedPrefix()
        {
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    // Update locally
                    Mod.currentTNHInstance.dead.Add(H3MP_GameManager.ID);
                    GM.TNH_Manager.SubtractTokens(GM.TNH_Manager.GetNumTokens());

                    // Send update
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHPlayerDied(Mod.currentTNHInstance.instance, H3MP_GameManager.ID);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHPlayerDied(Mod.currentTNHInstance.instance, H3MP_GameManager.ID);
                    }

                    //TODO: Handle spectate or leave
                    // Prevent TNH from processing player death if there are other players still in the game
                    if (Mod.currentTNHInstance.dead.Count < Mod.currentTNHInstance.currentlyPlaying.Count)
                    {
                        if (Mod.TNHOnDeathSpectate)
                        {
                            Mod.TNHSpectating = true;
                            Mod.DropAllItems();
                            return false;
                        }
                        // else, TNH_Manager will process player death
                    }
                    else // We were the last live player, the game will now end, reset
                    {
                        Mod.currentTNHInstance.Reset();
                    }
                }
            }

            return true;
        }

        static bool AddTokensPrefix(int i, bool Scorethis)
        {
            if(addTokensSkip > 0)
            {
                return true;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Send update if these are tokens we want to award to every player
                    if (Scorethis)
                    {
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.TNHAddTokens(Mod.currentTNHInstance.instance, i);
                        }
                        else
                        {
                            H3MP_ClientSend.TNHAddTokens(Mod.currentTNHInstance.instance, i);
                        }

                        Mod.currentTNHInstance.tokenCount += i;
                    }
                    
                    // Prevent TNH from adding tokens if player is dead
                    if (Mod.currentTNHInstance.dead.Contains(H3MP_GameManager.ID))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool OnSosigKillPrefix(Sosig s)
        {
            if (sosigKillSkip > 0)
            {
                return true;
            }

            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if(Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
                {
                    return false;
                }

                H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(s) ? H3MP_GameManager.trackedSosigBySosig[s] : s.GetComponent<H3MP_TrackedSosig>();
                if(trackedSosig != null)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHSosigKill(Mod.currentTNHInstance.instance, trackedSosig.data.trackedID);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHSosigKill(Mod.currentTNHInstance.instance, trackedSosig.data.trackedID);
                    }
                }
            }

            return true;
        }

        static bool SetPhasePrefix(TNH_Phase p)
        {
            // We want to prevent call to SetPhase unless we are controller
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if(Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHSetPhase(Mod.currentTNHInstance.instance, (short)p);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHSetPhase(Mod.currentTNHInstance.instance, p);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        static bool SetLevelPrefix(int level)
        {
            // Update TNH:
            //  New Level index
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHSetLevel(Mod.currentTNHInstance.instance, level);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHSetLevel(Mod.currentTNHInstance.instance, level);
                    }

                    return true;
                }
                else // Not controller
                {
                    TNH_Progression progression = (TNH_Progression)Mod.TNH_Manager_m_curProgression.GetValue(Mod.currentTNHInstance.manager);
                    if (Mod.currentTNHInstance.manager.ProgressionMode == TNHSetting_ProgressionType.FiveHold || level < progression.Levels.Count)
                    {
                        Mod.TNH_Manager_m_curLevel.SetValue(Mod.currentTNHInstance.manager, progression.Levels[Mod.currentTNHInstance.level]);
                    }
                    else
                    {
                        TNH_Progression endlessProgression = (TNH_Progression)Mod.TNH_Manager_m_curProgressionEndless.GetValue(Mod.currentTNHInstance.manager);
                        Mod.TNH_Manager_m_curLevel.SetValue(Mod.currentTNHInstance.manager, endlessProgression.Levels[UnityEngine.Random.Range(0, endlessProgression.Levels.Count)]);
                    }

                    return false;
                }
            }

            return true;
        }

        static bool SetPhaseTakePrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
                {
                    Mod.currentTNHInstance.manager.Phase = TNH_Phase.Take;

                    Mod.TNH_Manager_m_activeSupplyPointIndicies.SetValue(Mod.currentTNHInstance.manager, Mod.currentTNHInstance.activeSupplyPointIndices);
                    if (Mod.currentTNHInstance.manager.RadarMode == TNHModifier_RadarMode.Standard)
                    {
                        Mod.currentTNHInstance.manager.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = Mod.currentTNHInstance.manager.ReticleMask_Take;
                    }
                    else if (Mod.currentTNHInstance.manager.RadarMode == TNHModifier_RadarMode.Omnipresent)
                    {
                        Mod.currentTNHInstance.manager.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = Mod.currentTNHInstance.manager.ReticleMask_Hold;
                    }
                    int curHoldIndex = Mod.currentTNHInstance.curHoldIndex;
                    Mod.TNH_Manager_m_curHoldPoint.SetValue(Mod.currentTNHInstance.manager, Mod.currentTNHInstance.manager.HoldPoints[curHoldIndex]);
                    Mod.TNH_Manager_m_lastHoldIndex.SetValue(Mod.currentTNHInstance.manager, curHoldIndex);
                    Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
                    Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Supply);
                    Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager)).SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);
                    bool spawnToken = true;
                    for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                    {
                        TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                        TNH_SupplyPoint.SupplyPanelType panelType = Mod.currentTNHInstance.supplyPanelTypes[i];
                        // Here we pass false to spawn sosigs,turrents, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                        tnh_SupplyPoint.Configure(((TNH_Progression.Level)Mod.TNH_Manager_m_curLevel.GetValue(Mod.currentTNHInstance.manager)).SupplyChallenge, false, false, true, panelType, 1, 0, spawnToken);
                        spawnToken = false;
                        TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                        tnh_SupplyPoint.SetContact(contact);
                    }
                    if (Mod.currentTNHInstance.manager.BGAudioMode == TNH_BGAudioMode.Default)
                    {
                        Mod.currentTNHInstance.manager.FMODController.SwitchTo(0, 2f, false, false);
                    }

                    return false;
                }
            }

            return true;
        }

        static void SetPhaseTakePostfix()
        {
            // Update TNH
            //  Active supply point indices
            //      Secondary panel type
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                List<TNH_SupplyPoint.SupplyPanelType> secondaryPanelTypes = new List<TNH_SupplyPoint.SupplyPanelType>();
                List<int> activeSupplyPointIndicies = (List<int>)Mod.TNH_Manager_m_activeSupplyPointIndicies.GetValue(GM.TNH_Manager);
                for(int i = 0; i < activeSupplyPointIndicies.Count; ++i)
                {
                    int index = activeSupplyPointIndicies[i];
                    GameObject panel = (GameObject)Mod.TNH_SupplyPoint_m_panel.GetValue(GM.TNH_Manager.SupplyPoints[index]);
                    TNH_AmmoReloader ammoReloader = panel.GetComponent<TNH_AmmoReloader>();
                    if(ammoReloader != null)
                    {
                        secondaryPanelTypes.Add(TNH_SupplyPoint.SupplyPanelType.AmmoReloader);
                    }
                    else
                    {
                        TNH_MagDuplicator magDuplicator = panel.GetComponent<TNH_MagDuplicator>();
                        if (magDuplicator != null)
                        {
                            secondaryPanelTypes.Add(TNH_SupplyPoint.SupplyPanelType.MagDuplicator);
                        }
                        else
                        {
                            secondaryPanelTypes.Add(TNH_SupplyPoint.SupplyPanelType.GunRecycler);
                        }
                    }
                }

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHSetPhaseTake(Mod.currentTNHInstance.instance, activeSupplyPointIndicies, secondaryPanelTypes);
                }
                else
                {
                    H3MP_ClientSend.TNHSetPhaseTake(Mod.currentTNHInstance.instance, activeSupplyPointIndicies, secondaryPanelTypes);
                }
            }
        }

        static void SetPhaseCompletePostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHSetPhaseComplete(Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHSetPhaseComplete(Mod.currentTNHInstance.instance);
                }
            }
        }

        static bool UpdatePrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
                {
                    // Call updates we don't want to skip
                    Mod.TNH_Manager_DelayedInit.Invoke(Mod.currentTNHInstance.manager, null);
                    Mod.TNH_Manager_VoiceUpdate.Invoke(Mod.currentTNHInstance.manager, null);
                    Mod.currentTNHInstance.manager.FMODController.SetMasterVolume(0.25f * GM.CurrentPlayerBody.GlobalHearing);

                    if (doInit && Mod.currentTNHInstance.manager.AIManager.HasInit)
                    {
                        Debug.Log("\t\t\tdoing TNH init");
                        doInit = false;
                        if (Mod.initTNHData != null)
                        {
                            Mod.InitTNHData(Mod.initTNHData);
                        }
                        InitJoinTNH();
                        if (Mod.initTNHData != null)
                        {
                            Mod.currentTNHInstance.controller = H3MP_ThreadManager.host ? 0 : H3MP_GameManager.ID;
                            Mod.initTNHData = null;
                        }
                    }

                    return false;
                }
            }
            return true;
        }

        static void UpdatePostfix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            if (Mod.currentTNHInstance != null)
            {
                if (doInit && Mod.currentTNHInstance.manager.AIManager.HasInit)
                {
                    doInit = false;
                    if (Mod.initTNHData != null)
                    {
                        Mod.InitTNHData(Mod.initTNHData);
                    }
                    InitJoinTNH();
                    if(Mod.initTNHData != null)
                    {
                        Mod.currentTNHInstance.controller = H3MP_ThreadManager.host ? 0 : H3MP_GameManager.ID;
                        Mod.initTNHData = null;
                    }
                }
            }
        }

        static bool OnShotFiredPrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                return false;
            }
            return true;
        }

        static bool OnBotShotFiredPrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                return false;
            }
            return true;
        }

        static bool AddFVRObjectToTrackedListPrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                return false;
            }
            return true;
        }

        public static void InitJoinTNH()
        {
            Debug.Log("InitJoinTNH called");
            Mod.TNH_Manager_m_curHoldPoint.SetValue(Mod.currentTNHInstance.manager, Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex]);
            if (Mod.currentTNHInstance.holdOngoing)
            {
                // Set the hold
                TNH_Progression.Level curLevel = (TNH_Progression.Level)Mod.TNH_Manager_m_curLevel.GetValue(Mod.currentTNHInstance.manager);
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                ++TNH_HoldPointPatch.beginHoldSkip;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                --TNH_HoldPointPatch.beginHoldSkip;

                // TP to system node spawn point
                GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode.position, true);

                // Raise barriers
                for (int i = 0; i < Mod.currentTNHInstance.raisedBarriers.Count; ++i)
                {
                    TNH_DestructibleBarrierPoint point = Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BarrierPoints[Mod.currentTNHInstance.raisedBarriers[i]];
                    TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[Mod.currentTNHInstance.raisedBarrierPrefabIndices[i]];
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                    TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                    Mod.TNH_DestructibleBarrierPoint_m_curBarrier.SetValue(point, curBarrier);
                    curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                    curBarrier.SetBarrierPoint(point);
                    Mod.TNH_DestructibleBarrierPoint_SetCoverPointData.Invoke(point, new object[] { Mod.currentTNHInstance.raisedBarrierPrefabIndices[i] });
                }
            }
            else
            {
                // Set the hold
                TNH_Progression.Level curLevel = (TNH_Progression.Level)Mod.TNH_Manager_m_curLevel.GetValue(Mod.currentTNHInstance.manager);
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);

                //  Set supply points
                bool spawnToken = true;
                for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                {
                    TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                    TNH_SupplyPoint.SupplyPanelType panelType = Mod.currentTNHInstance.supplyPanelTypes[i];
                    // Here we pass false to spawn sosigs,turrents, and 0 for max boxes because doing 
                    // this init means we are not the first ones here, meaning those things should already be spawned for this supply point
                    tnh_SupplyPoint.Configure(((TNH_Progression.Level)Mod.TNH_Manager_m_curLevel.GetValue(Mod.currentTNHInstance.manager)).SupplyChallenge, false, false, true, panelType, 1, 0, spawnToken);
                    spawnToken = false;
                    TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                    tnh_SupplyPoint.SetContact(contact);
                }

                // Spawn at intial supply point
                if(Mod.currentTNHInstance.activeSupplyPointIndices == null)
                {
                    // Note that here we cannot have a hold case
                    switch (Mod.currentTNHInstance.phase)
                    {
                        case TNH_Phase.StartUp:
                        case TNH_Phase.Take:
                            GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.SupplyPoints[UnityEngine.Random.Range(0, Mod.currentTNHInstance.manager.SupplyPoints.Count)].SpawnPoint_PlayerSpawn.position, true);
                            break;
                        default:
                            GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, true);
                            break;
                    }
                }
                else
                {
                    // TP to first inactive point
                    for(int i=0; i< Mod.currentTNHInstance.manager.SupplyPoints.Count; ++i)
                    {
                        if (!Mod.currentTNHInstance.activeSupplyPointIndices.Contains(i))
                        {
                            GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.SupplyPoints[i].SpawnPoint_PlayerSpawn.position, true);
                            break;
                        }
                    }
                }
            }

            // If this is the first time we join this game, give the player a button 
            // with which they can spawn their own starting equipment
            if (!Mod.currentTNHInstance.spawnedStartEquip)
            {
                Mod.currentTNHInstance.spawnedStartEquip = true;
                Mod.TNHStartEquipButton = GameObject.Instantiate(Mod.TNHStartEquipButtonPrefab, GM.CurrentPlayerBody.Head);
                Mod.TNHStartEquipButton.transform.GetChild(0).GetComponent<FVRPointableButton>().Button.onClick.AddListener(Mod.OnTNHSpawnStartEquipClicked);
            }
        }

        static bool InitBeginEquipPrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.currentlyPlaying.Count > 1)
            {
                // Don't want to spawn starting equipment right away if there are already players in our TNH instance
                return false;
            }
            return true;
        }
    }

    // Patches TNH_HoldPoint to keep track of hold point events
    public class TNH_HoldPointPatch
    {
        public static bool spawnEntitiesSkip;
        public static int beginHoldSkip;

        static void ConfigureAsSystemNodePrefix(ref TNH_HoldPoint __instance)
        {
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                    {
                        int holdPointIndex = -1;
                        for(int i=0; i<__instance.M.HoldPoints.Count;++i)
                        {
                            if (__instance.M.HoldPoints[i] == __instance)
                            {
                                holdPointIndex = i;
                                break;
                            }
                        }
                        int progressionIndex = -1;
                        for(int i=0; i<__instance.M.C.Progressions.Count;++i)
                        {
                            if (__instance.M.C.Progressions[i] == (TNH_Progression)Mod.TNH_Manager_m_curProgression.GetValue(__instance.M))
                            {
                                progressionIndex = i;
                                break;
                            }
                        }
                        int progressionEndlessIndex = -1;
                        for(int i=0; i<__instance.M.C.Progressions_Endless.Count;++i)
                        {
                            if (__instance.M.C.Progressions_Endless[i] == (TNH_Progression)Mod.TNH_Manager_m_curProgressionEndless.GetValue(__instance.M))
                            {
                                progressionEndlessIndex = i;
                                break;
                            }
                        }
                        if (holdPointIndex == -1)
                        {
                            Debug.LogError("Holdpoint to be set as sytem node missing from manager");
                        }
                        else
                        {
                            if (H3MP_ThreadManager.host)
                            {
                                H3MP_ServerSend.TNHHoldPointSystemNode(Mod.currentTNHInstance.instance, GM.TNHOptions.LastPlayedChar, progressionIndex, progressionEndlessIndex, (int)Mod.TNH_Manager_m_level.GetValue(GM.TNH_Manager), holdPointIndex);
                            }
                            else
                            {

                                H3MP_ClientSend.TNHHoldPointSystemNode(Mod.currentTNHInstance.instance, GM.TNHOptions.LastPlayedChar, progressionIndex, progressionEndlessIndex, (int)Mod.TNH_Manager_m_level.GetValue(GM.TNH_Manager), holdPointIndex);
                            }
                        }
                    }
                    else
                    {
                        spawnEntitiesSkip = true;
                    }
                }
            }
        }

        static bool SpawnTakeChallengeEntitiesPrefix()
        {
            if (spawnEntitiesSkip)
            {
                spawnEntitiesSkip = false;
                return false;
            }

            return true;
        }

        static void BeginHoldPostfix()
        {
            if(beginHoldSkip > 0)
            {
                return;
            }

            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // Update locally
                    Mod.currentTNHInstance.holdOngoing = true;

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
                    }
                }
            }  
        }

        static bool RaiseRandomBarriersPrefix(int howMany)
        {
            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if(Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    int num = howMany;
                    TNH_HoldPoint holdPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);
                    List<int> indices = new List<int>();
                    for(int i=0; i < holdPoint.BarrierPoints.Count; ++i)
                    {
                        indices.Add(i);
                    }
                    indices.Shuffle<int>();
                    Mod.currentTNHInstance.raisedBarriers = new List<int>();
                    Mod.currentTNHInstance.raisedBarrierPrefabIndices = new List<int>();
                    for (int i = 0; i < howMany && indices.Count > 0; i++)
                    {
                        int randIndex = UnityEngine.Random.Range(0, indices.Count);
                        int index = indices[randIndex];
                        indices.RemoveAt(randIndex);
                        holdPoint.BarrierPoints[index].SpawnRandomBarrier();

                        // Set the list in TNHInstance, which will be sent alongside begin hold
                        Mod.currentTNHInstance.raisedBarriers.Add(index);
                    }
                }

                return false;
            }

            return true;
        }

        static void BarrierSetCoverPointDataPrefix(int index)
        {
            if (index == -1)
            {
                return;
            }

            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            if(Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if(Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    if(Mod.currentTNHInstance.raisedBarrierPrefabIndices == null)
                    {
                        Mod.currentTNHInstance.raisedBarrierPrefabIndices = new List<int>();
                    }
                    Mod.currentTNHInstance.raisedBarrierPrefabIndices.Add(index);
                }
            }
        }

        static void CompletePhasePostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHHoldCompletePhase(Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHHoldCompletePhase(Mod.currentTNHInstance.instance);
                }
            }
        }

        static void ShutDownPostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHHoldShutDown(Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHHoldShutDown(Mod.currentTNHInstance.instance);
                }
            }
        }
    }

    // Patches TAH_ReticleContact to enable display of friendlies
    class TAHReticleContactPatch
    {
        static bool SetContactTypePrefix(ref TAH_ReticleContact __instance, TAH_ReticleContact.ContactType t)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            if((int)t == -2) // Friendly
            {
                if (__instance.Type != t)
                {
                    __instance.Type = t;
                    __instance.M_Arrow.mesh = __instance.Meshes_Arrow[(int)TAH_ReticleContact.ContactType.Enemy];
                    __instance.M_Icon.mesh = __instance.Meshes_Icon[(int)TAH_ReticleContact.ContactType.Enemy];
                    __instance.R_Arrow.material = Mod.reticleFriendlyContactArrowMat;
                    __instance.R_Icon.material = Mod.reticleFriendlyContactIconMat;
                }
                return false;
            }
            return true;
        }

        // This transpiler will make sure that Tick will also return false if the transform is not active
        // This is so taht when we make a player inactive because they are dead, we don't want to see them on the reticle either
        static IEnumerable<CodeInstruction> TickTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load TAH_ReticleContact instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TAH_ReticleContact), "TrackedTransform"))); // Load the TrackedTransform
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "get_gameObject"))); // Get the GameObject
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameObject), "get_activeInHierarchy"))); // Get activeInHierarchy

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Brfalse)
                {
                    toInsert.Add(new CodeInstruction(OpCodes.Brtrue, instruction.operand)); // If true jump to same label as if first if statement is false
                    toInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
                    toInsert.Add(new CodeInstruction(OpCodes.Ret)); // Return
                }
                if(instruction.opcode == OpCodes.Ret)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    break;
                }
            }
            return instructionList;
        }
    }

    // Patches TNH_WeaponCrate.Update to know when the case is open so we can put a timed destroyer on it if necessary
    class TNHWeaponCrateSpawnObjectsPatch
    {
        static void SpawnObjectsRawPrefix(ref TNH_WeaponCrate __instance)
        {
            if(Mod.managerObject != null)
            {
                H3MP_TimerDestroyer destroyer = __instance.GetComponent<H3MP_TimerDestroyer>();
                if(destroyer != null)
                {
                    destroyer.triggered = true;
                }
            }
        }
    }
    #endregion
}