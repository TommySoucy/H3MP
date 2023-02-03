using Anvil;
using BepInEx;
using FistVR;
using HarmonyLib;
using HarmonyLib.Public.Patching;
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
        public GameObject TNHMenuPrefab;
        public static GameObject TNHStartEquipButtonPrefab;
        public GameObject playerPrefab;
        public static Material reticleFriendlyContactArrowMat;
        public static Material reticleFriendlyContactIconMat;
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

        #region Reused NonPublic MemberInfo
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
        public static readonly FieldInfo LAPD2019_m_capacitorCharge = typeof(LAPD2019).GetField("m_capacitorCharge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo LAPD2019_m_isCapacitorCharged = typeof(LAPD2019).GetField("m_isCapacitorCharged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AttachableBreakActions_m_isBreachOpen = typeof(AttachableBreakActions).GetField("m_isBreachOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo BAP_m_fireSelectorMode = typeof(BAP).GetField("m_fireSelectorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo BAP_m_isHammerCocked = typeof(BAP).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo SosigWeapon_m_shotsLeft = typeof(SosigWeapon).GetField("m_shotsLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_ShatterableCrate_m_isHoldingHealth = typeof(TNH_ShatterableCrate).GetField("m_isHoldingHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_ShatterableCrate_m_isHoldingToken = typeof(TNH_ShatterableCrate).GetField("m_isHoldingToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_HoldPoint_m_warpInTargets = typeof(TNH_HoldPoint).GetField("m_warpInTargets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_HoldPoint_m_systemNode = typeof(TNH_HoldPoint).GetField("m_systemNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo TNH_HoldPoint_m_state = typeof(TNH_HoldPoint).GetField("m_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo LeverActionFirearm_m_isHammerCocked = typeof(LeverActionFirearm).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo LeverActionFirearm_m_isHammerCocked2 = typeof(LeverActionFirearm).GetField("m_isHammerCocked2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Derringer_m_isExternalHammerCocked = typeof(Derringer).GetField("m_isExternalHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlameThrower_m_isFiring = typeof(FlameThrower).GetField("m_isFiring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlameThrower_m_hasFiredStartSound = typeof(FlameThrower).GetField("m_hasFiredStartSound", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Flaregun_m_isHammerCocked = typeof(Flaregun).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlintlockBarrel_m_weapon = typeof(FlintlockBarrel).GetField("m_weapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlintlockPseudoRamRod_m_curBarrel = typeof(FlintlockPseudoRamRod).GetField("m_curBarrel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlintlockWeapon_m_hasFlint = typeof(FlintlockWeapon).GetField("m_hasFlint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlintlockWeapon_m_flintUses = typeof(FlintlockWeapon).GetField("m_flintUses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo GBeamer_m_isBatterySwitchedOn = typeof(GBeamer).GetField("m_isBatterySwitchedOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo GBeamer_m_isCapacitorSwitchedOn = typeof(GBeamer).GetField("m_isCapacitorSwitchedOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo GBeamer_m_isMotorSwitchedOn = typeof(GBeamer).GetField("m_isMotorSwitchedOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo GBeamer_m_capacitorCharge = typeof(GBeamer).GetField("m_capacitorCharge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo GrappleGun_m_curChamber = typeof(GrappleGun).GetField("m_curChamber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo HCB_m_sledState = typeof(HCB).GetField("m_sledState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo HCB_m_cookedAmount = typeof(HCB).GetField("m_cookedAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo OpenBoltReceiver_m_CamBurst = typeof(OpenBoltReceiver).GetField("m_CamBurst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo OpenBoltReceiver_m_fireSelectorMode = typeof(OpenBoltReceiver).GetField("m_fireSelectorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo M72_m_isSafetyEngaged = typeof(M72).GetField("m_isSafetyEngaged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Minigun_m_heat = typeof(Minigun).GetField("m_heat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Minigun_m_motorRate = typeof(Minigun).GetField("m_motorRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo PotatoGun_m_chamberGas = typeof(PotatoGun).GetField("m_chamberGas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo RemoteMissileLauncher_m_missile = typeof(RemoteMissileLauncher).GetField("m_missile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo RemoteMissile_speed = typeof(RemoteMissile).GetField("speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo RemoteMissile_tarSpeed = typeof(RemoteMissile).GetField("tarSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo RollingBlock_m_state = typeof(RollingBlock).GetField("m_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo RPG7_m_isHammerCocked = typeof(RPG7).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo SingleActionRevolver_m_isHammerCocked = typeof(SingleActionRevolver).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo StingerLauncher_m_hasMissile = typeof(StingerLauncher).GetField("m_hasMissile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo PinnedGrenade_m_hasSploded = typeof(PinnedGrenade).GetField("m_hasSploded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo PinnedGrenade_m_rings = typeof(PinnedGrenade).GetField("m_rings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRGrenade_FuseTimings = typeof(FVRGrenade).GetField("FuseTimings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRGrenade_m_hasSploded = typeof(FVRGrenade).GetField("m_hasSploded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRGrenade_m_isLeverReleased = typeof(FVRGrenade).GetField("m_isLeverReleased", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRGrenadePin_m_hasBeenPulled = typeof(FVRGrenadePin).GetField("m_hasBeenPulled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRGrenadePin_m_isDying = typeof(FVRGrenadePin).GetField("m_isDying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Derringer_m_curBarrel = typeof(Derringer).GetField("m_curBarrel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo AttachableTubeFed_m_isHammerCocked = typeof(AttachableTubeFed).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo C4_m_isArmed = typeof(C4).GetField("m_isArmed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo ClaymoreMine_m_isArmed = typeof(ClaymoreMine).GetField("m_isArmed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo ClaymoreMine_m_isPlanted = typeof(ClaymoreMine).GetField("m_isPlanted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo Airgun_m_isHammerCocked = typeof(Airgun).GetField("m_isHammerCocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo sblp_m_isShotEngaged = typeof(sblp).GetField("m_isShotEngaged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FVRFireArmBipod_m_isBipodExpanded = typeof(FVRFireArmBipod).GetField("m_isBipodExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly FieldInfo FlagPoseSwitcher_m_index = typeof(FlagPoseSwitcher).GetField("m_index", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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
        public static readonly MethodInfo LAPD2019_Fire = typeof(LAPD2019).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Minigun_Fire = typeof(Minigun).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Revolver_Fire = typeof(Revolver).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_BeginAnalyzing = typeof(TNH_HoldPoint).GetMethod("BeginAnalyzing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_DeleteAllActiveWarpIns = typeof(TNH_HoldPoint).GetMethod("DeleteAllActiveWarpIns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_IdentifyEncryption = typeof(TNH_HoldPoint).GetMethod("IdentifyEncryption", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_FailOut = typeof(TNH_HoldPoint).GetMethod("FailOut", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_BeginPhase = typeof(TNH_HoldPoint).GetMethod("BeginPhase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TNH_HoldPoint_LowerAllBarriers = typeof(TNH_HoldPoint).GetMethod("LowerAllBarriers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo LeverActionFirearm_Fire = typeof(LeverActionFirearm).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo RevolvingShotgun_Fire = typeof(RevolvingShotgun).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Derringer_CockHammer = typeof(Derringer).GetMethod("CockHammer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Derringer_FireBarrel = typeof(Derringer).GetMethod("FireBarrel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo FlameThrower_StopFiring = typeof(FlameThrower).GetMethod("StopFiring", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo FlintlockBarrel_Fire = typeof(FlintlockBarrel).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo HCB_UpdateStrings = typeof(HCB).GetMethod("UpdateStrings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo HCB_ReleaseSled = typeof(HCB).GetMethod("ReleaseSled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Flaregun_Fire = typeof(Flaregun).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo RemoteMissileLauncher_FireShot = typeof(RemoteMissileLauncher).GetMethod("FireShot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo RollingBlock_Fire = typeof(RollingBlock).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo SingleActionRevolver_Fire = typeof(SingleActionRevolver).GetMethod("Fire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo StingerMissile_Explode = typeof(StingerMissile).GetMethod("Explode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo BangSnap_Splode = typeof(BangSnap).GetMethod("Splode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo ClaymoreMine_Detonate = typeof(ClaymoreMine).GetMethod("Detonate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo Airgun_DropHammer = typeof(Airgun).GetMethod("DropHammer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo sblp_TryToEngageShot = typeof(sblp).GetMethod("TryToEngageShot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo sblp_TryToDisengageShot = typeof(sblp).GetMethod("TryToDisengageShot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo FlipSight_Flip = typeof(FlipSight).GetMethod("Flip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo FlipSightY_Flip = typeof(FlipSightY).GetMethod("Flip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo LaserPointer_ToggleOn = typeof(LaserPointer).GetMethod("ToggleOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo TacticalFlashlight_ToggleOn = typeof(TacticalFlashlight).GetMethod("ToggleOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        public static readonly MethodInfo AlloyAreaLight_get_Light = typeof(AlloyAreaLight).GetMethod("get_Light", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        #endregion

// Debug
#if DEBUG
        bool debug;
#endif
        public static Vector3 TNHSpawnPoint;

        private void Start()
        {
            Logger.LogInfo("H3MP Started");

            modInstance = this;

            Init();
        }

        private void Update()
        {
#if DEBUG
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
                    if(Mod.managerObject != null)
                    {
                        if (H3MP_ThreadManager.host)
                        {
                            Mod.LogInfo("Closing server.");
                            H3MP_Server.Close();
                        }
                        else
                        {
                            Mod.LogInfo("Disconnecting from server.");
                            H3MP_Client.singleton.Disconnect(true, 0);
                        }
                    }
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
                    Dictionary<string, string> map = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, FVRObject> o in IM.OD)
                    {
                        GameObject prefab = null;
                        try
                        {
                            prefab = o.Value.GetGameObject();

                        }
                        catch (Exception)
                        {
                            Mod.LogError("There was an error trying to retrieve prefab with ID: " + o.Key);
                            continue;
                        }
                        try
                        {
                            SosigWearable wearable = prefab.GetComponent<SosigWearable>();
                            if (wearable != null)
                            {
                                if (map.ContainsKey(prefab.name))
                                {
                                    Mod.LogWarning("Sosig wearable with name: " + prefab.name + " is already in the map with value: " + map[prefab.name] + " and wewanted to add value: " + o.Key);
                                }
                                else
                                {
                                    map.Add(prefab.name, o.Key);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Mod.LogError("There was an error trying to check if prefab with ID: " + o.Key + " is wearable or adding it to the list");
                            continue;
                        }
                    }
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
                else if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    SpawnDummyPlayer();
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    GM.CurrentMovementManager.TeleportToPoint(TNHSpawnPoint, true);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMultiply))
                {
                    string dest = "BepInEx/Plugins/H3MP/PatchHashes" + DateTimeOffset.Now.ToString().Replace("/", ".").Replace(":", ".") + ".json";
                    File.Copy("BepInEx/Plugins/H3MP/PatchHashes.json", dest);
                    Mod.LogWarning("Writing new hashes to file!");
                    File.WriteAllText("BepInEx/Plugins/H3MP/PatchHashes.json", JObject.FromObject(PatchVerify.hashes).ToString());
                }
            }
#endif
        }

        private void SpawnDummyPlayer()
        {
            GameObject player = Instantiate(playerPrefab);

            H3MP_PlayerManager playerManager = player.GetComponent<H3MP_PlayerManager>();
            playerManager.ID = -1;
            playerManager.username = "Dummy";
            playerManager.scene = SceneManager.GetActiveScene().name;
            playerManager.instance = H3MP_GameManager.instance;
            playerManager.usernameLabel.text = "Dummy";
            playerManager.SetIFF(GM.CurrentPlayerBody.GetPlayerIFF());
        }

        public void LoadConfig()
        {
            Logger.LogInfo("Loading config...");
            config = JObject.Parse(File.ReadAllText("BepInEx/Plugins/H3MP/Config.json"));
            Logger.LogInfo("Config loaded");
        }

        public void InitTNHMenu()
        {
            TNHMenu = Instantiate(TNHMenuPrefab, new Vector3(-2.4418f, 1.04f, 6.2977f), Quaternion.Euler(0, 270, 0));

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
            if (currentTNHInstance != null)
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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void DoPatching()
        {
            var harmony = new HarmonyLib.Harmony("VIP.TommySoucy.H3MP");

            // First patch harmony itself to be able to extract IL code from methods without having to use a transpiler
            //PatchVerify.writeToMethod = typeof(Harmony).Assembly.GetType("ILManipulator").GetMethod("WriteTo", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo applyTranspilersOriginal = typeof(Harmony).Assembly.GetType("ILManipulator").GetMethod("ApplyTranspilers", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo applyTranspilersPostfix = typeof(PatchVerify).GetMethod("ApplyTranspilersPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            //harmony.Patch(applyTranspilersOriginal, new HarmonyMethod(applyTranspilersPostfix));

            // LoadLevelBeginPatch
            MethodInfo loadLevelBeginPatchOriginal = typeof(SteamVR_LoadLevel).GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);
            MethodInfo loadLevelBeginPatchPrefix = typeof(LoadLevelBeginPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(loadLevelBeginPatchOriginal, harmony, true);
            harmony.Patch(loadLevelBeginPatchOriginal, new HarmonyMethod(loadLevelBeginPatchPrefix));

            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPrefix = typeof(HandCurrentInteractableSetPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(handCurrentInteractableSetPatchOriginal, harmony, true);
            harmony.Patch(handCurrentInteractableSetPatchOriginal, new HarmonyMethod(handCurrentInteractableSetPatchPrefix), new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(setQuickBeltSlotPatchOriginal, harmony, true);
            harmony.Patch(setQuickBeltSlotPatchOriginal, null, new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            // SosigPickUpPatch
            MethodInfo sosigPickUpPatchOriginal = typeof(SosigHand).GetMethod("PickUp", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPickUpPatchPostfix = typeof(SosigPickUpPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigPickUpPatchOriginal, harmony, true);
            harmony.Patch(sosigPickUpPatchOriginal, null, new HarmonyMethod(sosigPickUpPatchPostfix));

            // SosigPlaceObjectInPatch
            MethodInfo sosigPutObjectInPatchOriginal = typeof(SosigInventory.Slot).GetMethod("PlaceObjectIn", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPutObjectInPatchPostfix = typeof(SosigPlaceObjectInPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigPutObjectInPatchOriginal, harmony, true);
            harmony.Patch(sosigPutObjectInPatchOriginal, null, new HarmonyMethod(sosigPutObjectInPatchPostfix));

            // SosigSlotDetachPatch
            MethodInfo sosigSlotDetachPatchOriginal = typeof(SosigInventory.Slot).GetMethod("DetachHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSlotDetachPatchPrefix = typeof(SosigSlotDetachPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigSlotDetachPatchOriginal, harmony, true);
            harmony.Patch(sosigSlotDetachPatchOriginal, new HarmonyMethod(sosigSlotDetachPatchPrefix));

            // SosigHandDropPatch
            MethodInfo sosigHandDropPatchOriginal = typeof(SosigHand).GetMethod("DropHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandThrowPatchOriginal = typeof(SosigHand).GetMethod("ThrowObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandDropPatchPrefix = typeof(SosigHandDropPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigHandDropPatchOriginal, harmony, true);
            PatchVerify.Verify(sosigHandThrowPatchOriginal, harmony, true);
            harmony.Patch(sosigHandDropPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));
            harmony.Patch(sosigHandThrowPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));

            // GrabbityPatch
            MethodInfo grabbityPatchOriginal = typeof(FVRViveHand).GetMethod("BeginFlick", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grabbityPatchPrefix = typeof(GrabbityPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(grabbityPatchOriginal, harmony, false);
            harmony.Patch(grabbityPatchOriginal, new HarmonyMethod(grabbityPatchPrefix));

            // GBeamerPatch
            MethodInfo GBeamerPatchObjectSearchOriginal = typeof(GBeamer).GetMethod("ObjectSearch", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GBeamerPatchObjectSearchPrefix = typeof(GBeamerPatch).GetMethod("ObjectSearchPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo GBeamerPatchObjectSearchPostfix = typeof(GBeamerPatch).GetMethod("ObjectSearchPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo GBeamerPatchWideShuntOriginal = typeof(GBeamer).GetMethod("WideShunt", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GBeamerPatchWideShuntTranspiler = typeof(GBeamerPatch).GetMethod("WideShuntTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(GBeamerPatchObjectSearchOriginal, harmony, false);
            PatchVerify.Verify(GBeamerPatchWideShuntOriginal, harmony, false);
            harmony.Patch(GBeamerPatchObjectSearchOriginal, new HarmonyMethod(GBeamerPatchObjectSearchPrefix), new HarmonyMethod(GBeamerPatchObjectSearchPostfix));
            harmony.Patch(GBeamerPatchWideShuntOriginal, null, null, new HarmonyMethod(GBeamerPatchWideShuntTranspiler));

            // FirePatch
            MethodInfo firePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo firePatchPrefix = typeof(FirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo firePatchTranspiler = typeof(FirePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo firePatchPostfix = typeof(FirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(firePatchOriginal, harmony, true);
            harmony.Patch(firePatchOriginal, new HarmonyMethod(firePatchPrefix), new HarmonyMethod(firePatchPostfix), new HarmonyMethod(firePatchTranspiler));

            // FireFlintlockWeaponPatch
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterOriginal = typeof(FlintlockBarrel).GetMethod("BurnOffOuter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterPrefix = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterTranspiler = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterPostfix = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFireOriginal = typeof(FlintlockBarrel).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireFlintlockWeaponFirePrefix = typeof(FireFlintlockWeaponPatch).GetMethod("FirePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFireTranspiler = typeof(FireFlintlockWeaponPatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFirePostfix = typeof(FireFlintlockWeaponPatch).GetMethod("FirePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireFlintlockWeaponPatchBurnOffOuterOriginal, harmony, false);
            PatchVerify.Verify(fireFlintlockWeaponFireOriginal, harmony, false);
            harmony.Patch(fireFlintlockWeaponPatchBurnOffOuterOriginal, new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterPrefix), new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterPostfix), new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterTranspiler));
            harmony.Patch(fireFlintlockWeaponFireOriginal, new HarmonyMethod(fireFlintlockWeaponFirePrefix), new HarmonyMethod(fireFlintlockWeaponFirePostfix), new HarmonyMethod(fireFlintlockWeaponFireTranspiler));

            // FireStingerLauncherPatch
            MethodInfo fireStingerLauncherOriginal = typeof(StingerLauncher).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo fireStingerLauncherPrefix = typeof(FireStingerLauncherPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerLauncherTranspiler = typeof(FireStingerLauncherPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerLauncherPostfix = typeof(FireStingerLauncherPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerMissileOriginal = typeof(StingerMissile).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(AIEntity) }, null);
            MethodInfo fireStingerMissilePrefix = typeof(FireStingerLauncherPatch).GetMethod("MissileFirePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireStingerLauncherOriginal, harmony, false);
            PatchVerify.Verify(fireStingerMissileOriginal, harmony, false);
            harmony.Patch(fireStingerLauncherOriginal, new HarmonyMethod(fireStingerLauncherPrefix), new HarmonyMethod(fireStingerLauncherPostfix), new HarmonyMethod(fireStingerLauncherTranspiler));
            harmony.Patch(fireStingerMissileOriginal, new HarmonyMethod(fireStingerMissilePrefix));

            // FireSosigWeaponPatch
            MethodInfo fireSosigWeaponPatchOriginal = typeof(SosigWeapon).GetMethod("FireGun", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireSosigWeaponPatchPrefix = typeof(FireSosigWeaponPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSosigWeaponPatchTranspiler = typeof(FireSosigWeaponPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSosigWeaponPatchPostfix = typeof(FireSosigWeaponPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireSosigWeaponPatchOriginal, harmony, true);
            harmony.Patch(fireSosigWeaponPatchOriginal, new HarmonyMethod(fireSosigWeaponPatchPrefix), new HarmonyMethod(fireSosigWeaponPatchPostfix), new HarmonyMethod(fireSosigWeaponPatchTranspiler));

            // FireLAPD2019Patch
            MethodInfo fireLAPD2019PatchOriginal = typeof(LAPD2019).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireLAPD2019PatchPrefix = typeof(FireLAPD2019Patch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLAPD2019PatchTranspiler = typeof(FireLAPD2019Patch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLAPD2019PatchPostfix = typeof(FireLAPD2019Patch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireLAPD2019PatchOriginal, harmony, false);
            harmony.Patch(fireLAPD2019PatchOriginal, new HarmonyMethod(fireLAPD2019PatchPrefix), new HarmonyMethod(fireLAPD2019PatchPostfix), new HarmonyMethod(fireLAPD2019PatchTranspiler));

            // LAPD2019ActionPatch
            MethodInfo LAPD2019PatchLoadOriginal = typeof(LAPD2019).GetMethod("LoadBattery", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo LAPD2019PatchLoadPrefix = typeof(LAPD2019ActionPatch).GetMethod("LoadBatteryPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo LAPD2019PatchExtractOriginal = typeof(LAPD2019).GetMethod("ExtractBattery", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo LAPD2019PatchExtractPrefix = typeof(LAPD2019ActionPatch).GetMethod("ExtractBatteryPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(LAPD2019PatchLoadOriginal, harmony, false);
            PatchVerify.Verify(LAPD2019PatchExtractOriginal, harmony, false);
            harmony.Patch(LAPD2019PatchLoadOriginal, new HarmonyMethod(LAPD2019PatchLoadPrefix));
            harmony.Patch(LAPD2019PatchExtractOriginal, new HarmonyMethod(LAPD2019PatchExtractPrefix));

            // FireMinigunPatch
            MethodInfo fireMinigunPatchOriginal = typeof(Minigun).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireMinigunPatchPrefix = typeof(FireMinigunPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireMinigunPatchTranspiler = typeof(FireMinigunPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireMinigunPatchPostfix = typeof(FireMinigunPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireMinigunPatchOriginal, harmony, false);
            harmony.Patch(fireMinigunPatchOriginal, new HarmonyMethod(fireMinigunPatchPrefix), new HarmonyMethod(fireMinigunPatchPostfix), new HarmonyMethod(fireMinigunPatchTranspiler));

            // FireAttachableFirearmPatch
            MethodInfo fireAttachableFirearmPatchOriginal = typeof(AttachableFirearm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireAttachableFirearmPatchTranspiler = typeof(FireAttachableFirearmPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableBreakActionsPatchOriginal = typeof(AttachableBreakActions).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo fireAttachableBreakActionsPatchPrefix = typeof(FireAttachableFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableBreakActionsPatchPostfix = typeof(FireAttachableFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableClosedBoltWeaponPatchOriginal = typeof(AttachableClosedBoltWeapon).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo fireAttachableClosedBoltWeaponPatchPrefix = typeof(FireAttachableFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableClosedBoltWeaponPatchPostfix = typeof(FireAttachableFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableTubeFedPatchOriginal = typeof(AttachableTubeFed).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo fireAttachableTubeFedPatchPrefix = typeof(FireAttachableFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireAttachableTubeFedPatchPostfix = typeof(FireAttachableFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireGP25PatchOriginal = typeof(GP25).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo fireGP25PatchPrefix = typeof(FireAttachableFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireGP25PatchPostfix = typeof(FireAttachableFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireM203PatchOriginal = typeof(M203).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo fireM203PatchPrefix = typeof(FireAttachableFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireM203PatchPostfix = typeof(FireAttachableFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireAttachableFirearmPatchOriginal, harmony, false);
            PatchVerify.Verify(fireAttachableBreakActionsPatchOriginal, harmony, false);
            PatchVerify.Verify(fireAttachableClosedBoltWeaponPatchOriginal, harmony, false);
            PatchVerify.Verify(fireAttachableTubeFedPatchOriginal, harmony, false);
            PatchVerify.Verify(fireGP25PatchOriginal, harmony, false);
            PatchVerify.Verify(fireM203PatchOriginal, harmony, false);
            harmony.Patch(fireAttachableFirearmPatchOriginal, null, null, new HarmonyMethod(fireAttachableFirearmPatchTranspiler));
            harmony.Patch(fireAttachableBreakActionsPatchOriginal, new HarmonyMethod(fireAttachableBreakActionsPatchPrefix), new HarmonyMethod(fireAttachableBreakActionsPatchPostfix));
            harmony.Patch(fireAttachableClosedBoltWeaponPatchOriginal, new HarmonyMethod(fireAttachableClosedBoltWeaponPatchPrefix), new HarmonyMethod(fireAttachableClosedBoltWeaponPatchPostfix));
            harmony.Patch(fireAttachableTubeFedPatchOriginal, new HarmonyMethod(fireAttachableTubeFedPatchPrefix), new HarmonyMethod(fireAttachableTubeFedPatchPostfix));
            harmony.Patch(fireGP25PatchOriginal, new HarmonyMethod(fireGP25PatchPrefix), new HarmonyMethod(fireGP25PatchPostfix));
            harmony.Patch(fireM203PatchOriginal, new HarmonyMethod(fireM203PatchPrefix), new HarmonyMethod(fireM203PatchPostfix));

            // FireBreakActionWeaponPatch
            MethodInfo fireBreakActionWeaponPatchOriginal = typeof(BreakActionWeapon).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int), typeof(bool), typeof(int) }, null);
            MethodInfo fireBreakActionWeaponPatchPrefix = typeof(FireBreakActionWeaponPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireBreakActionWeaponPatchPostfix = typeof(FireBreakActionWeaponPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireBreakActionWeaponPatchOriginal, harmony, false);
            harmony.Patch(fireBreakActionWeaponPatchOriginal, new HarmonyMethod(fireBreakActionWeaponPatchPrefix), new HarmonyMethod(fireBreakActionWeaponPatchPostfix));

            // FireLeverActionFirearmPatch
            MethodInfo fireLeverActionFirearmPatchOriginal = typeof(LeverActionFirearm).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireLeverActionFirearmPatchPrefix = typeof(FireLeverActionFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLeverActionFirearmPatchPostfix = typeof(FireLeverActionFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireLeverActionFirearmPatchOriginal, harmony, false);
            harmony.Patch(fireLeverActionFirearmPatchOriginal, new HarmonyMethod(fireLeverActionFirearmPatchPrefix), new HarmonyMethod(fireLeverActionFirearmPatchPostfix));

            // FireHCBPatch
            MethodInfo fireHCBPatchOriginal = typeof(HCB).GetMethod("ReleaseSled", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireHCBPatchPrefix = typeof(FireHCBPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireHCBPatchPostfix = typeof(FireHCBPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireHCBPatchOriginal, harmony, false);
            harmony.Patch(fireHCBPatchOriginal, new HarmonyMethod(fireHCBPatchPrefix), new HarmonyMethod(fireHCBPatchPostfix));

            // FireRevolvingShotgunPatch
            MethodInfo fireRevolvingShotgunPatchOriginal = typeof(RevolvingShotgun).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireRevolvingShotgunPatchPrefix = typeof(FireRevolvingShotgunPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireRevolvingShotgunPatchPostfix = typeof(FireRevolvingShotgunPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireRevolvingShotgunPatchOriginal, harmony, false);
            harmony.Patch(fireRevolvingShotgunPatchOriginal, new HarmonyMethod(fireRevolvingShotgunPatchPrefix), new HarmonyMethod(fireRevolvingShotgunPatchPostfix));

            // FireRevolverPatch
            MethodInfo fireRevolverPatchOriginal = typeof(Revolver).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireRevolverPatchPrefix = typeof(FireRevolverPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireRevolverPatchPostfix = typeof(FireRevolverPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireRevolverPatchOriginal, harmony, false);
            harmony.Patch(fireRevolverPatchOriginal, new HarmonyMethod(fireRevolverPatchPrefix), new HarmonyMethod(fireRevolverPatchPostfix));

            // FireSingleActionRevolverPatch
            MethodInfo fireSingleActionRevolverPatchOriginal = typeof(SingleActionRevolver).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireSingleActionRevolverPatchPrefix = typeof(FireSingleActionRevolverPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSingleActionRevolverPatchPostfix = typeof(FireSingleActionRevolverPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireSingleActionRevolverPatchOriginal, harmony, false);
            harmony.Patch(fireSingleActionRevolverPatchOriginal, new HarmonyMethod(fireSingleActionRevolverPatchPrefix), new HarmonyMethod(fireSingleActionRevolverPatchPostfix));

            // FireGrappleGunPatch
            MethodInfo fireGrappleGunPatchOriginal = typeof(GrappleGun).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[0], null);
            MethodInfo fireGrappleGunPatchPrefix = typeof(FireGrappleGunPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireGrappleGunPatchPostfix = typeof(FireGrappleGunPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireGrappleGunPatchOriginal, harmony, false);
            harmony.Patch(fireGrappleGunPatchOriginal, new HarmonyMethod(fireGrappleGunPatchPrefix), new HarmonyMethod(fireGrappleGunPatchPostfix));

            // FireDerringerPatch
            MethodInfo fireDerringerPatchOriginal = typeof(Derringer).GetMethod("FireBarrel", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireDerringerPatchPrefix = typeof(FireDerringerPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireDerringerPatchPostfix = typeof(FireDerringerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(fireDerringerPatchOriginal, harmony, false);
            harmony.Patch(fireDerringerPatchOriginal, new HarmonyMethod(fireDerringerPatchPrefix), new HarmonyMethod(fireDerringerPatchPostfix));

            // RemoteMissileDetonatePatch
            MethodInfo remoteMissileDetonatePatchOriginal = typeof(RemoteMissile).GetMethod("Detonante", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo remoteMissileDetonatePatchPrefix = typeof(RemoteMissileDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(remoteMissileDetonatePatchOriginal, harmony, false);
            harmony.Patch(remoteMissileDetonatePatchOriginal, new HarmonyMethod(remoteMissileDetonatePatchPrefix));

            // StingerMissileExplodePatch
            MethodInfo stingerMissileExplodePatchOriginal = typeof(StingerMissile).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo stingerMissileExplodePatchPrefix = typeof(StingerMissileExplodePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(stingerMissileExplodePatchOriginal, harmony, false);
            harmony.Patch(stingerMissileExplodePatchOriginal, new HarmonyMethod(stingerMissileExplodePatchPrefix));

            // SosigConfigurePatch
            MethodInfo sosigConfigurePatchOriginal = typeof(Sosig).GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigConfigurePatchPrefix = typeof(SosigConfigurePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigConfigurePatchOriginal, harmony, false);
            harmony.Patch(sosigConfigurePatchOriginal, new HarmonyMethod(sosigConfigurePatchPrefix));

            // SosigUpdatePatch
            MethodInfo sosigUpdatePatchOriginal = typeof(Sosig).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigHandPhysUpdatePatchOriginal = typeof(Sosig).GetMethod("HandPhysUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigHandPhysUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("HandPhysUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigUpdatePatchOriginal, harmony, true);
            harmony.Patch(sosigUpdatePatchOriginal, new HarmonyMethod(sosigUpdatePatchPrefix));
            harmony.Patch(sosigHandPhysUpdatePatchOriginal, new HarmonyMethod(sosigHandPhysUpdatePatchPrefix));

            // AutoMeaterUpdatePatch
            MethodInfo autoMeaterUpdatePatchOriginal = typeof(AutoMeater).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFixedUpdatePatchOriginal = typeof(AutoMeater).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdatePatchPrefix = typeof(AutoMeaterUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterUpdatePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));
            harmony.Patch(autoMeaterFixedUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));

            // AutoMeaterEventPatch
            MethodInfo autoMeaterEventReceivePatchOriginal = typeof(AutoMeater).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterEventReceivePatchPrefix = typeof(AutoMeaterEventPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterEventReceivePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterEventReceivePatchOriginal, new HarmonyMethod(autoMeaterEventReceivePatchPrefix));

            // AutoMeaterSetStatePatch
            MethodInfo autoMeaterSetStatePatchOriginal = typeof(AutoMeater).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterSetStatePatchPostfix = typeof(AutoMeaterSetStatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterSetStatePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterSetStatePatchOriginal, null, new HarmonyMethod(autoMeaterSetStatePatchPostfix));

            // AutoMeaterUpdateFlightPatch
            MethodInfo autoMeaterUpdateFlightPatchOriginal = typeof(AutoMeater).GetMethod("UpdateFlight", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdateFlightPatchPrefix = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterUpdateFlightPatchTranspiler = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterUpdateFlightPatchOriginal, harmony, false);
            harmony.Patch(autoMeaterUpdateFlightPatchOriginal, new HarmonyMethod(autoMeaterUpdateFlightPatchPrefix), null, new HarmonyMethod(autoMeaterUpdateFlightPatchTranspiler));

            // InventoryUpdatePatch
            MethodInfo sosigInvUpdatePatchOriginal = typeof(SosigInventory).GetMethod("PhysHold", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigInvUpdatePatchPrefix = typeof(SosigInvUpdatePatch).GetMethod("PhysHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigInvUpdatePatchOriginal, harmony, false);
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

            PatchVerify.Verify(sosigLinkRegisterWearablePatchOriginal, harmony, false);
            PatchVerify.Verify(sosigLinkDeRegisterWearablePatchOriginal, harmony, false);
            //PatchVerify.Verify(sosigLinkExplodesPatchOriginal, harmony, false);
            //PatchVerify.Verify(sosigLinkBreakPatchOriginal, harmony, false);
            //PatchVerify.Verify(sosigLinkSeverPatchOriginal, harmony, false);
            //PatchVerify.Verify(sosigLinkVaporizePatchOriginal, harmony, false);
            harmony.Patch(sosigLinkRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkRegisterWearablePatchPrefix));
            harmony.Patch(sosigLinkDeRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkDeRegisterWearablePatchPrefix));
            //harmony.Patch(sosigLinkExplodesPatchOriginal, new HarmonyMethod(sosigLinkExplodesPatchPrefix), new HarmonyMethod(sosigLinkExplodesPatchPosfix));
            //harmony.Patch(sosigLinkBreakPatchOriginal, new HarmonyMethod(sosigLinkBreakPatchPrefix), new HarmonyMethod(sosigLinkBreakPatchPosfix));
            //harmony.Patch(sosigLinkSeverPatchOriginal, new HarmonyMethod(sosigLinkSeverPatchPrefix), new HarmonyMethod(sosigLinkSeverPatchPosfix));
            //harmony.Patch(sosigLinkVaporizePatchOriginal, new HarmonyMethod(sosigLinkVaporizePatchPrefix), new HarmonyMethod(sosigLinkVaporizePatchPosfix));

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

            //PatchVerify.Verify(sosigDiesPatchOriginal, harmony, false);
            PatchVerify.Verify(sosigBodyStatePatchOriginal, harmony, false);
            PatchVerify.Verify(sosigBodyUpdatePatchOriginal, harmony, true);
            PatchVerify.Verify(sosigSpeechUpdatePatchOriginal, harmony, false);
            PatchVerify.Verify(sosigSetCurrentOrderPatchOriginal, harmony, false);
            PatchVerify.Verify(sosigRequestHitDecalPatchOriginal, harmony, false);
            PatchVerify.Verify(sosigRequestHitDecalEdgePatchOriginal, harmony, false);
            //harmony.Patch(sosigDiesPatchOriginal, new HarmonyMethod(sosigDiesPatchPrefix), new HarmonyMethod(sosigDiesPatchPosfix));
            harmony.Patch(sosigBodyStatePatchOriginal, new HarmonyMethod(sosigBodyStatePatchPrefix));
            harmony.Patch(sosigBodyUpdatePatchOriginal, null, null, new HarmonyMethod(sosigBodyUpdatePatchTranspiler));
            harmony.Patch(sosigSpeechUpdatePatchOriginal, null, null, new HarmonyMethod(sosigSpeechUpdatePatchTranspiler));
            harmony.Patch(sosigSetCurrentOrderPatchOriginal, new HarmonyMethod(sosigSetCurrentOrderPatchPrefix));
            //harmony.Patch(sosigVaporizePatchOriginal, new HarmonyMethod(sosigVaporizePatchPrefix), new HarmonyMethod(sosigVaporizePatchPostfix));
            harmony.Patch(sosigRequestHitDecalPatchOriginal, new HarmonyMethod(sosigRequestHitDecalPatchPrefix));
            harmony.Patch(sosigRequestHitDecalEdgePatchOriginal, new HarmonyMethod(sosigRequestHitDecalEdgePatchPrefix));

            // SosigIFFPatch
            MethodInfo sosigSetIFFPatchOriginal = typeof(Sosig).GetMethod("SetIFF", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSetOriginalIFFPatchOriginal = typeof(Sosig).GetMethod("SetOriginalIFFTeam", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetOriginalIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetOriginalIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigSetIFFPatchOriginal, harmony, false);
            PatchVerify.Verify(sosigSetOriginalIFFPatchOriginal, harmony, false);
            harmony.Patch(sosigSetIFFPatchOriginal, new HarmonyMethod(sosigSetIFFPatchPrefix));
            harmony.Patch(sosigSetOriginalIFFPatchOriginal, new HarmonyMethod(sosigSetOriginalIFFPatchPrefix));

            // SosigEventReceivePatch
            MethodInfo sosigEventReceivePatchOriginal = typeof(Sosig).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigEventReceivePatchPrefix = typeof(SosigEventReceivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigEventReceivePatchOriginal, harmony, false);
            harmony.Patch(sosigEventReceivePatchOriginal, new HarmonyMethod(sosigEventReceivePatchPrefix));

            // ChamberEjectRoundPatch
            MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool) }, null);
            MethodInfo chamberEjectRoundPatchAnimationOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(bool) }, null);
            MethodInfo chamberEjectRoundPatchPrefix = typeof(ChamberEjectRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(chamberEjectRoundPatchOriginal, harmony, true);
            PatchVerify.Verify(chamberEjectRoundPatchAnimationOriginal, harmony, true);
            harmony.Patch(chamberEjectRoundPatchOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));
            harmony.Patch(chamberEjectRoundPatchAnimationOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));

            // Internal_CloneSinglePatch
            MethodInfo internal_CloneSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSinglePatchPostfix = typeof(Internal_CloneSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(internal_CloneSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSinglePatchOriginal, null, new HarmonyMethod(internal_CloneSinglePatchPostfix));

            // Internal_CloneSingleWithParentPatch
            MethodInfo internal_CloneSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPrefix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPostfix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(internal_CloneSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSingleWithParentPatchOriginal, new HarmonyMethod(internal_CloneSingleWithParentPatchPrefix), new HarmonyMethod(internal_CloneSingleWithParentPatchPostfix));

            // Internal_InstantiateSinglePatch
            MethodInfo internal_InstantiateSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSinglePatchPostfix = typeof(Internal_InstantiateSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(internal_InstantiateSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSinglePatchOriginal, null, new HarmonyMethod(internal_InstantiateSinglePatchPostfix));

            // Internal_InstantiateSingleWithParentPatch
            MethodInfo internal_InstantiateSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPrefix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPostfix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(internal_InstantiateSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSingleWithParentPatchOriginal, new HarmonyMethod(internal_InstantiateSingleWithParentPatchPrefix), new HarmonyMethod(internal_InstantiateSingleWithParentPatchPostfix));

            // LoadDefaultSceneRoutinePatch
            MethodInfo loadDefaultSceneRoutinePatchOriginal = typeof(FVRSceneSettings).GetMethod("LoadDefaultSceneRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo loadDefaultSceneRoutinePatchPrefix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo loadDefaultSceneRoutinePatchPostfix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(loadDefaultSceneRoutinePatchOriginal, harmony, false);
            harmony.Patch(loadDefaultSceneRoutinePatchOriginal, new HarmonyMethod(loadDefaultSceneRoutinePatchPrefix), new HarmonyMethod(loadDefaultSceneRoutinePatchPostfix));

            // SpawnObjectsPatch
            MethodInfo spawnObjectsPatchOriginal = typeof(VaultSystem).GetMethod("SpawnObjects", BindingFlags.Public | BindingFlags.Static);
            MethodInfo spawnObjectsPatchPrefix = typeof(SpawnObjectsPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(spawnObjectsPatchOriginal, harmony, false);
            harmony.Patch(spawnObjectsPatchOriginal, new HarmonyMethod(spawnObjectsPatchPrefix));

            // SpawnVaultFileRoutinePatch
            MethodInfo spawnVaultFileRoutinePatchOriginal = typeof(VaultSystem).GetMethod("SpawnVaultFileRoutine", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchMoveNext = EnumeratorMoveNext(spawnVaultFileRoutinePatchOriginal);
            MethodInfo spawnVaultFileRoutinePatchPrefix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchTranspiler = typeof(SpawnVaultFileRoutinePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchPostfix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(spawnVaultFileRoutinePatchOriginal, harmony, false);
            PatchVerify.Verify(spawnVaultFileRoutinePatchMoveNext, harmony, false);
            harmony.Patch(spawnVaultFileRoutinePatchMoveNext, new HarmonyMethod(spawnVaultFileRoutinePatchPrefix), new HarmonyMethod(spawnVaultFileRoutinePatchPostfix), new HarmonyMethod(spawnVaultFileRoutinePatchTranspiler));

            // IDSpawnedFromPatch
            MethodInfo IDSpawnedFromPatchOriginal = typeof(FVRPhysicalObject).GetMethod("set_IDSpawnedFrom", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo IDSpawnedFromPatchPostfix = typeof(IDSpawnedFromPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(IDSpawnedFromPatchOriginal, harmony, true);
            harmony.Patch(IDSpawnedFromPatchOriginal, null, new HarmonyMethod(IDSpawnedFromPatchPostfix));

            // AnvilPrefabSpawnPatch
            MethodInfo anvilPrefabSpawnPatchOriginal = typeof(AnvilPrefabSpawn).GetMethod("InstantiateAndZero", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo anvilPrefabSpawnPatchPrefix = typeof(AnvilPrefabSpawnPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(anvilPrefabSpawnPatchOriginal, harmony, true);
            harmony.Patch(anvilPrefabSpawnPatchOriginal, new HarmonyMethod(anvilPrefabSpawnPatchPrefix));

            // ProjectileFirePatch
            MethodInfo projectileFirePatchOriginal = typeof(BallisticProjectile).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float), typeof(Vector3), typeof(FVRFireArm), typeof(bool) }, null);
            MethodInfo projectileFirePatchPostfix = typeof(ProjectileFirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(projectileFirePatchOriginal, harmony, true);
            harmony.Patch(projectileFirePatchOriginal, new HarmonyMethod(projectileFirePatchPostfix));

            // ProjectileDamageablePatch
            MethodInfo ballisticProjectileDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo ballisticProjectileDamageablePatchTranspiler = typeof(BallisticProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(ballisticProjectileDamageablePatchOriginal, harmony, true);
            harmony.Patch(ballisticProjectileDamageablePatchOriginal, null, null, new HarmonyMethod(ballisticProjectileDamageablePatchTranspiler));

            // SubMunitionsDamageablePatch
            MethodInfo subMunitionsDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("FireSubmunitions", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo subMunitionsDamageablePatchTranspiler = typeof(SubMunitionsDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(subMunitionsDamageablePatchOriginal, harmony, false);
            harmony.Patch(subMunitionsDamageablePatchOriginal, null, null, new HarmonyMethod(subMunitionsDamageablePatchTranspiler));

            // ExplosionDamageablePatch
            MethodInfo explosionDamageablePatchOriginal = typeof(Explosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo explosionDamageablePatchTranspiler = typeof(ExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(explosionDamageablePatchOriginal, harmony, false);
            harmony.Patch(explosionDamageablePatchOriginal, null, null, new HarmonyMethod(explosionDamageablePatchTranspiler));

            // GrenadeExplosionDamageablePatch
            MethodInfo grenadeExplosionDamageablePatchOriginal = typeof(GrenadeExplosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeExplosionDamageablePatchTranspiler = typeof(GrenadeExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(grenadeExplosionDamageablePatchOriginal, harmony, false);
            harmony.Patch(grenadeExplosionDamageablePatchOriginal, null, null, new HarmonyMethod(grenadeExplosionDamageablePatchTranspiler));

            // FlameThrowerDamageablePatch
            MethodInfo flameThrowerDamageablePatchOriginal = typeof(FlameThrower).GetMethod("AirBlast", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flameThrowerDamageablePatchTranspiler = typeof(FlameThrowerDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(flameThrowerDamageablePatchOriginal, harmony, false);
            harmony.Patch(flameThrowerDamageablePatchOriginal, null, null, new HarmonyMethod(flameThrowerDamageablePatchTranspiler));

            // GrenadeDamageablePatch
            MethodInfo grenadeDamageablePatchOriginal = typeof(FVRGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeDamageablePatchTranspiler = typeof(GrenadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(grenadeDamageablePatchOriginal, harmony, false);
            harmony.Patch(grenadeDamageablePatchOriginal, null, null, new HarmonyMethod(grenadeDamageablePatchTranspiler));

            // DemonadeDamageablePatch
            MethodInfo demonadeDamageablePatchOriginal = typeof(MF2_Demonade).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo demonadeDamageablePatchTranspiler = typeof(DemonadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(demonadeDamageablePatchOriginal, harmony, false);
            harmony.Patch(demonadeDamageablePatchOriginal, null, null, new HarmonyMethod(demonadeDamageablePatchTranspiler));

            // SosigWeaponDamageablePatch
            MethodInfo sosigWeaponDamageablePatchOriginal = typeof(SosigWeapon).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("ExplosionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchCollisionOriginal = typeof(SosigWeapon).GetMethod("DoMeleeDamageInCollision", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchCollisionTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchUpdateOriginal = typeof(SosigWeapon).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchUpdateTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigWeaponDamageablePatchOriginal, harmony, true);
            PatchVerify.Verify(sosigWeaponDamageablePatchCollisionOriginal, harmony, true);
            PatchVerify.Verify(sosigWeaponDamageablePatchUpdateOriginal, harmony, true);
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

            PatchVerify.Verify(meleeParamsDamageablePatchStabOriginal, harmony, true);
            PatchVerify.Verify(meleeParamsDamageablePatchTearOriginal, harmony, true);
            PatchVerify.Verify(meleeParamsDamageablePatchUpdateOriginal, harmony, true);
            PatchVerify.Verify(meleeParamsDamageablePatchCollisionOriginal, harmony, true);
            harmony.Patch(meleeParamsDamageablePatchStabOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchStabTranspiler));
            harmony.Patch(meleeParamsDamageablePatchTearOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchTearTranspiler));
            harmony.Patch(meleeParamsDamageablePatchUpdateOriginal, new HarmonyMethod(meleeParamsDamageablePatchUpdatePrefix), null, new HarmonyMethod(meleeParamsDamageablePatchUpdateTranspiler));
            harmony.Patch(meleeParamsDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchCollisionTranspiler));

            // AIMeleeDamageablePatch
            MethodInfo meleeParamsDamageablePatchFireOriginal = typeof(AIMeleeWeapon).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchFireTranspiler = typeof(AIMeleeDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(meleeParamsDamageablePatchFireOriginal, harmony, true);
            harmony.Patch(meleeParamsDamageablePatchFireOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchFireTranspiler));

            // AutoMeaterBladeDamageablePatch
            MethodInfo autoMeaterBladeDamageablePatchCollisionOriginal = typeof(AutoMeaterBlade).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterBladeDamageablePatchCollisionTranspiler = typeof(AutoMeaterBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterBladeDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(autoMeaterBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(autoMeaterBladeDamageablePatchCollisionTranspiler));

            // BangSnapDamageablePatch
            MethodInfo bangSnapDamageablePatchCollisionOriginal = typeof(BangSnap).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bangSnapDamageablePatchCollisionTranspiler = typeof(BangSnapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(bangSnapDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(bangSnapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(bangSnapDamageablePatchCollisionTranspiler));

            // BearTrapDamageablePatch
            MethodInfo bearTrapDamageablePatchSnapOriginal = typeof(BearTrapInteractiblePiece).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bearTrapDamageablePatchSnapTranspiler = typeof(BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(bearTrapDamageablePatchSnapOriginal, harmony, false);
            harmony.Patch(bearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(bearTrapDamageablePatchSnapTranspiler));

            // ChainsawDamageablePatch
            MethodInfo chainsawDamageablePatchCollisionOriginal = typeof(Chainsaw).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo chainsawDamageablePatchCollisionTranspiler = typeof(ChainsawDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(chainsawDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(chainsawDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(chainsawDamageablePatchCollisionTranspiler));

            // DrillDamageablePatch
            MethodInfo drillDamageablePatchCollisionOriginal = typeof(Drill).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drillDamageablePatchCollisionTranspiler = typeof(DrillDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(drillDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(drillDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(drillDamageablePatchCollisionTranspiler));

            // DropTrapDamageablePatch
            MethodInfo dropTrapDamageablePatchCollisionOriginal = typeof(DropTrapLogs).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dropTrapDamageablePatchCollisionTranspiler = typeof(DropTrapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(dropTrapDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(dropTrapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(dropTrapDamageablePatchCollisionTranspiler));

            // FlipzoDamageablePatch
            MethodInfo flipzoDamageablePatchUpdateOriginal = typeof(Flipzo).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flipzoDamageablePatchUpdateTranspiler = typeof(FlipzoDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(flipzoDamageablePatchUpdateOriginal, harmony, false);
            harmony.Patch(flipzoDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(flipzoDamageablePatchUpdateTranspiler));

            // IgnitableDamageablePatch
            MethodInfo ignitableDamageablePatchStartOriginal = typeof(FVRIgnitable).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo ignitableDamageablePatchStartTranspiler = typeof(IgnitableDamageablePatch).GetMethod("StartTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(ignitableDamageablePatchStartOriginal, harmony, false);
            harmony.Patch(ignitableDamageablePatchStartOriginal, null, null, new HarmonyMethod(ignitableDamageablePatchStartTranspiler));

            // SparklerDamageablePatch
            MethodInfo sparklerDamageablePatchCollisionOriginal = typeof(FVRSparkler).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sparklerDamageablePatchCollisionTranspiler = typeof(SparklerDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sparklerDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(sparklerDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(sparklerDamageablePatchCollisionTranspiler));

            // MatchDamageablePatch
            MethodInfo matchDamageablePatchCollisionOriginal = typeof(FVRStrikeAnyWhereMatch).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo matchDamageablePatchCollisionTranspiler = typeof(MatchDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(matchDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(matchDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(matchDamageablePatchCollisionTranspiler));

            // HCBBoltDamageablePatch
            MethodInfo HCBBoltDamageablePatchDamageOriginal = typeof(HCBBolt).GetMethod("DamageOtherThing", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo HCBBoltDamageablePatchDamageTranspiler = typeof(HCBBoltDamageablePatch).GetMethod("DamageOtherTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(HCBBoltDamageablePatchDamageOriginal, harmony, false);
            harmony.Patch(HCBBoltDamageablePatchDamageOriginal, null, null, new HarmonyMethod(HCBBoltDamageablePatchDamageTranspiler));

            // KabotDamageablePatch
            MethodInfo kabotDamageablePatchTickOriginal = typeof(Kabot.KSpike).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo kabotDamageablePatchTickTranspiler = typeof(KabotDamageablePatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(kabotDamageablePatchTickOriginal, harmony, false);
            harmony.Patch(kabotDamageablePatchTickOriginal, null, null, new HarmonyMethod(kabotDamageablePatchTickTranspiler));

            // MeatCrabDamageablePatch
            MethodInfo meatCrabDamageablePatchLungingOriginal = typeof(MeatCrab).GetMethod("Crabdate_Lunging", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchLungingTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("LungingTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meatCrabDamageablePatchAttachedOriginal = typeof(MeatCrab).GetMethod("Crabdate_Attached", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchAttachedTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("AttachedTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(meatCrabDamageablePatchLungingOriginal, harmony, false);
            PatchVerify.Verify(meatCrabDamageablePatchAttachedOriginal, harmony, false);
            harmony.Patch(meatCrabDamageablePatchLungingOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchLungingTranspiler));
            harmony.Patch(meatCrabDamageablePatchAttachedOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchAttachedTranspiler));

            // MF2_BearTrapDamageablePatch
            MethodInfo MF2_BearTrapDamageablePatchSnapOriginal = typeof(MF2_BearTrapInteractionZone).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo MF2_BearTrapDamageablePatchSnapTranspiler = typeof(MF2_BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(MF2_BearTrapDamageablePatchSnapOriginal, harmony, false);
            harmony.Patch(MF2_BearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(MF2_BearTrapDamageablePatchSnapTranspiler));

            // MG_SwarmDamageablePatch
            MethodInfo MG_SwarmDamageablePatchFireOriginal = typeof(MG_FlyingHotDogSwarm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_SwarmDamageablePatchFireTranspiler = typeof(MG_SwarmDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(MG_SwarmDamageablePatchFireOriginal, harmony, false);
            harmony.Patch(MG_SwarmDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_SwarmDamageablePatchFireTranspiler));

            // MG_JerryDamageablePatch
            MethodInfo MG_JerryDamageablePatchFireOriginal = typeof(MG_JerryTheLemon).GetMethod("FireBolt", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_JerryDamageablePatchFireTranspiler = typeof(MG_JerryDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(MG_JerryDamageablePatchFireOriginal, harmony, false);
            harmony.Patch(MG_JerryDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_JerryDamageablePatchFireTranspiler));

            // MicrotorchDamageablePatch
            MethodInfo microtorchDamageablePatchUpdateOriginal = typeof(Microtorch).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo microtorchDamageablePatchUpdateTranspiler = typeof(MicrotorchDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(microtorchDamageablePatchUpdateOriginal, harmony, false);
            harmony.Patch(microtorchDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(microtorchDamageablePatchUpdateTranspiler));

            // CyclopsDamageablePatch
            MethodInfo cyclopsDamageablePatchUpdateOriginal = typeof(PowerUp_Cyclops).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo cyclopsDamageablePatchUpdateTranspiler = typeof(CyclopsDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(cyclopsDamageablePatchUpdateOriginal, harmony, false);
            harmony.Patch(cyclopsDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(cyclopsDamageablePatchUpdateTranspiler));

            // LaserSwordDamageablePatch
            MethodInfo laserSwordDamageablePatchUpdateOriginal = typeof(RealisticLaserSword).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo laserSwordDamageablePatchUpdateTranspiler = typeof(LaserSwordDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(laserSwordDamageablePatchUpdateOriginal, harmony, false);
            harmony.Patch(laserSwordDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(laserSwordDamageablePatchUpdateTranspiler));

            // CharcoalDamageablePatch
            MethodInfo charcoalDamageablePatchCharcoalOriginal = typeof(RotrwCharcoal).GetMethod("DamageBubble", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo charcoalDamageablePatchCharcoalTranspiler = typeof(CharcoalDamageablePatch).GetMethod("BubbleTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(charcoalDamageablePatchCharcoalOriginal, harmony, false);
            harmony.Patch(charcoalDamageablePatchCharcoalOriginal, null, null, new HarmonyMethod(charcoalDamageablePatchCharcoalTranspiler));

            // SlicerDamageablePatch
            MethodInfo slicerDamageablePatchUpdateOriginal = typeof(SlicerBladeMaster).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo slicerDamageablePatchUpdateTranspiler = typeof(SlicerDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(slicerDamageablePatchUpdateOriginal, harmony, false);
            harmony.Patch(slicerDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(slicerDamageablePatchUpdateTranspiler));

            // SpinningBladeDamageablePatch
            MethodInfo spinningBladeDamageablePatchCollisionOriginal = typeof(SpinningBladeTrapBase).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo spinningBladeDamageablePatchCollisionTranspiler = typeof(SpinningBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(spinningBladeDamageablePatchCollisionOriginal, harmony, false);
            harmony.Patch(spinningBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(spinningBladeDamageablePatchCollisionTranspiler));

            // ProjectileDamageablePatch
            MethodInfo projectileDamageablePatchOriginal = typeof(FVRProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo projectileBladeDamageablePatchTranspiler = typeof(ProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(projectileDamageablePatchOriginal, harmony, true);
            harmony.Patch(projectileDamageablePatchOriginal, null, null, new HarmonyMethod(projectileBladeDamageablePatchTranspiler));

            // SosigLinkDamagePatch
            MethodInfo sosigLinkDamagePatchOriginal = typeof(SosigLink).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkDamagePatchPrefix = typeof(SosigLinkDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkDamagePatchPostfix = typeof(SosigLinkDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigLinkDamagePatchOriginal, harmony, true);
            harmony.Patch(sosigLinkDamagePatchOriginal, new HarmonyMethod(sosigLinkDamagePatchPrefix), new HarmonyMethod(sosigLinkDamagePatchPostfix));

            // SosigWearableDamagePatch
            MethodInfo sosigWearableDamagePatchOriginal = typeof(SosigWearable).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWearableDamagePatchPrefix = typeof(SosigWearableDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWearableDamagePatchPostfix = typeof(SosigWearableDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigWearableDamagePatchOriginal, harmony, true);
            harmony.Patch(sosigWearableDamagePatchOriginal, new HarmonyMethod(sosigWearableDamagePatchPrefix), new HarmonyMethod(sosigWearableDamagePatchPostfix));

            // AutoMeaterDamagePatch
            MethodInfo autoMeaterDamagePatchOriginal = typeof(AutoMeater).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterDamagePatchPrefix = typeof(AutoMeaterDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterDamagePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterDamagePatchOriginal, new HarmonyMethod(autoMeaterDamagePatchPrefix));

            // AutoMeaterHitZoneDamagePatch
            MethodInfo autoMeaterHitZoneDamagePatchOriginal = typeof(AutoMeaterHitZone).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterHitZoneDamagePatchPrefix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterHitZoneDamagePatchPostfix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterHitZoneDamagePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterHitZoneDamagePatchOriginal, new HarmonyMethod(autoMeaterHitZoneDamagePatchPrefix), new HarmonyMethod(autoMeaterHitZoneDamagePatchPostfix));

            // AutoMeaterFirearmFireShotPatch
            MethodInfo autoMeaterFirearmFireShotPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireShotPatchPrefix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchPostfix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchTranspiler = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterFirearmFireShotPatchOriginal, harmony, false);
            harmony.Patch(autoMeaterFirearmFireShotPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireShotPatchPrefix), new HarmonyMethod(autoMeaterFirearmFireShotPatchPostfix), new HarmonyMethod(autoMeaterFirearmFireShotPatchTranspiler));

            // AutoMeaterFirearmFireAtWillPatch
            MethodInfo autoMeaterFirearmFireAtWillPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("SetFireAtWill", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireAtWillPatchPrefix = typeof(AutoMeaterFirearmFireAtWillPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(autoMeaterFirearmFireAtWillPatchOriginal, harmony, false);
            harmony.Patch(autoMeaterFirearmFireAtWillPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireAtWillPatchPrefix));

            // EncryptionDamagePatch
            MethodInfo encryptionDamagePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDamagePatchPrefix = typeof(EncryptionDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDamagePatchPostfix = typeof(EncryptionDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionDamagePatchOriginal, harmony, true);
            harmony.Patch(encryptionDamagePatchOriginal, new HarmonyMethod(encryptionDamagePatchPrefix), new HarmonyMethod(encryptionDamagePatchPostfix));

            // SosigWeaponDamagePatch
            MethodInfo sosigWeaponDamagePatchOriginal = typeof(SosigWeapon).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWeaponDamagePatchPrefix = typeof(SosigWeaponDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigWeaponDamagePatchOriginal, harmony, false);
            harmony.Patch(sosigWeaponDamagePatchOriginal, new HarmonyMethod(sosigWeaponDamagePatchPrefix));

            // RemoteMissileDamagePatch
            MethodInfo remoteMissileDamagePatchOriginal = typeof(RemoteMissile).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo remoteMissileDamagePatchPrefix = typeof(RemoteMissileDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(remoteMissileDamagePatchOriginal, harmony, false);
            harmony.Patch(remoteMissileDamagePatchOriginal, new HarmonyMethod(remoteMissileDamagePatchPrefix));

            // StingerMissileDamagePatch
            MethodInfo stingerMissileDamagePatchOriginal = typeof(StingerMissile).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo stingerMissileDamagePatchPrefix = typeof(StingerMissileDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(stingerMissileDamagePatchOriginal, harmony, false);
            harmony.Patch(stingerMissileDamagePatchOriginal, new HarmonyMethod(stingerMissileDamagePatchPrefix));

            // SosigWeaponShatterPatch
            MethodInfo sosigWeaponShatterPatchOriginal = typeof(SosigWeapon).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponShatterPatchPrefix = typeof(SosigWeaponShatterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigWeaponShatterPatchOriginal, harmony, false);
            harmony.Patch(sosigWeaponShatterPatchOriginal, new HarmonyMethod(sosigWeaponShatterPatchPrefix));

            // EncryptionRespawnRandSubPatch
            MethodInfo encryptionRespawnRandSubPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("RespawnRandomSubTarg", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionRespawnRandSubPatchTranspiler = typeof(EncryptionRespawnRandSubPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionRespawnRandSubPatchOriginal, harmony, false);
            harmony.Patch(encryptionRespawnRandSubPatchOriginal, null, null, new HarmonyMethod(encryptionRespawnRandSubPatchTranspiler));

            // EncryptionPopulateInitialRegenPatch
            MethodInfo encryptionPopulateInitialRegenPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("PopulateInitialRegen", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionPopulateInitialRegenPatchPrefix = typeof(EncryptionPopulateInitialRegenPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionPopulateInitialRegenPatchOriginal, harmony, false);
            harmony.Patch(encryptionPopulateInitialRegenPatchOriginal, new HarmonyMethod(encryptionPopulateInitialRegenPatchPrefix));

            // EncryptionStartPatch
            MethodInfo encryptionStartPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionStartPatchPrefix = typeof(EncryptionStartPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionStartPatchPostfix = typeof(EncryptionStartPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionStartPatchOriginal, harmony, false);
            harmony.Patch(encryptionStartPatchOriginal, new HarmonyMethod(encryptionStartPatchPrefix), new HarmonyMethod(encryptionStartPatchPostfix));

            // EncryptionResetGrowthPatch
            MethodInfo encryptionResetGrowthPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("ResetGrowth", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionResetGrowthPatchPrefix = typeof(EncryptionResetGrowthPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionResetGrowthPatchOriginal, harmony, false);
            harmony.Patch(encryptionResetGrowthPatchOriginal, new HarmonyMethod(encryptionResetGrowthPatchPrefix));

            // EncryptionDisableSubtargPatch
            MethodInfo encryptionDisableSubtargPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("DisableSubtarg", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDisableSubtargPatchPrefix = typeof(EncryptionDisableSubtargPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDisableSubtargPatchPostfix = typeof(EncryptionDisableSubtargPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionDisableSubtargPatchOriginal, harmony, false);
            harmony.Patch(encryptionDisableSubtargPatchOriginal, new HarmonyMethod(encryptionDisableSubtargPatchPrefix), new HarmonyMethod(encryptionDisableSubtargPatchPostfix));

            // EncryptionUpdatePatch
            MethodInfo encryptionUpdatePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionUpdatePatchPrefix = typeof(EncryptionUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionUpdatePatchOriginal, harmony, true);
            harmony.Patch(encryptionUpdatePatchOriginal, new HarmonyMethod(encryptionUpdatePatchPrefix));

            // EncryptionFixedUpdatePatch
            MethodInfo encryptionFixedUpdatePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionFixedUpdatePatchPrefix = typeof(EncryptionFixedUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionFixedUpdatePatchOriginal, harmony, true);
            harmony.Patch(encryptionFixedUpdatePatchOriginal, new HarmonyMethod(encryptionFixedUpdatePatchPrefix));

            // EncryptionSubDamagePatch
            MethodInfo encryptionSubDamagePatchOriginal = typeof(TNH_EncryptionTarget_SubTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionSubDamagePatchPrefix = typeof(EncryptionSubDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(encryptionSubDamagePatchOriginal, harmony, true);
            harmony.Patch(encryptionSubDamagePatchOriginal, new HarmonyMethod(encryptionSubDamagePatchPrefix));

            // SetTNHManagerPatch
            MethodInfo setTNHManagerPatchOriginal = typeof(GM).GetMethod("set_TNH_Manager", BindingFlags.Public | BindingFlags.Static);
            MethodInfo setTNHManagerPatchPostfix = typeof(SetTNHManagerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(setTNHManagerPatchOriginal, harmony, true);
            harmony.Patch(setTNHManagerPatchOriginal, null, new HarmonyMethod(setTNHManagerPatchPostfix));

            // TNH_TokenPatch
            MethodInfo TNH_TokenPatchPatchCollectOriginal = typeof(TNH_Token).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_TokenPatchPatchCollectPrefix = typeof(TNH_TokenPatch).GetMethod("CollectPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_TokenPatchPatchCollectPostfix = typeof(TNH_TokenPatch).GetMethod("CollectPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_TokenPatchPatchCollectOriginal, harmony, true);
            harmony.Patch(TNH_TokenPatchPatchCollectOriginal, new HarmonyMethod(TNH_TokenPatchPatchCollectPrefix), new HarmonyMethod(TNH_TokenPatchPatchCollectPostfix));

            // TNH_ShatterableCrateDamagePatch
            MethodInfo TNH_ShatterableCrateDamagePatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDamagePatchPrefix = typeof(TNH_ShatterableCrateDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_ShatterableCrateDamagePatchOriginal, harmony, false);
            harmony.Patch(TNH_ShatterableCrateDamagePatchOriginal, new HarmonyMethod(TNH_ShatterableCrateDamagePatchPrefix));

            // TNH_ShatterableCrateDestroyPatch
            MethodInfo TNH_ShatterableCrateDestroyPatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Destroy", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDestroyPatchPrefix = typeof(TNH_ShatterableCrateDestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_ShatterableCrateDestroyPatchOriginal, harmony, false);
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

            PatchVerify.Verify(TNH_UIManagerPatchProgressionOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchEquipmentOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchHealthModeOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchTargetModeOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchAIDifficultyOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchRadarModeOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchItemSpawnerModeOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchBackpackModeOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchHealthMultOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchSosigGunReloadOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchSeedOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchNextLevelOriginal, harmony, false);
            PatchVerify.Verify(TNH_UIManagerPatchPrevLevelOriginal, harmony, false);
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
            MethodInfo TNH_ManagerGenerateSentryPatrolOriginal = typeof(TNH_Manager).GetMethod("GenerateSentryPatrol", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerGenerateSentryPatrolPrefix = typeof(TNH_ManagerPatch).GetMethod("GenerateSentryPatrolPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerGenerateSentryPatrolPostfix = typeof(TNH_ManagerPatch).GetMethod("GenerateSentryPatrolPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerGeneratePatrolOriginal = typeof(TNH_Manager).GetMethod("GeneratePatrol", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerGeneratePatrolPrefix = typeof(TNH_ManagerPatch).GetMethod("GeneratePatrolPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerGeneratePatrolPostfix = typeof(TNH_ManagerPatch).GetMethod("GeneratePatrolPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_ManagerPatchPlayerDiedOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchAddTokensOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchSosigKillOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchSetPhaseOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchUpdateOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchInitBeginEquipOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchSetPhaseTakeOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchSetPhaseCompleteOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchSetLevelOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchOnShotFiredOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchOnBotShotFiredOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerPatchAddFVRObjectToTrackedListOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerGenerateSentryPatrolOriginal, harmony, true);
            PatchVerify.Verify(TNH_ManagerGeneratePatrolOriginal, harmony, true);
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
            harmony.Patch(TNH_ManagerGenerateSentryPatrolOriginal, new HarmonyMethod(TNH_ManagerGenerateSentryPatrolPrefix), new HarmonyMethod(TNH_ManagerGenerateSentryPatrolPostfix));
            harmony.Patch(TNH_ManagerGeneratePatrolOriginal, new HarmonyMethod(TNH_ManagerGeneratePatrolPrefix), new HarmonyMethod(TNH_ManagerGeneratePatrolPostfix));

            // TNHSupplyPointPatch
            MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal = typeof(TNH_SupplyPoint).GetMethod("SpawnTakeEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnTakeEnemyGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnTakeEnemyGroupPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNHSupplyPointPatchSpawnDefensesOriginal = typeof(TNH_SupplyPoint).GetMethod("SpawnDefenses", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNHSupplyPointPatchSpawnDefensesPrefix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnDefensesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNHSupplyPointPatchSpawnDefensesPostfix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnDefensesPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, harmony, false);
            PatchVerify.Verify(TNHSupplyPointPatchSpawnDefensesOriginal, harmony, false);
            harmony.Patch(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix));
            harmony.Patch(TNHSupplyPointPatchSpawnDefensesOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPostfix));

            // TAHReticleContactPatch
            MethodInfo TAHReticleContactPatchTickOriginal = typeof(TAH_ReticleContact).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchTickTranspiler = typeof(TAHReticleContactPatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TAHReticleContactPatchSetContactTypeOriginal = typeof(TAH_ReticleContact).GetMethod("SetContactType", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchSetContactTypePrefix = typeof(TAHReticleContactPatch).GetMethod("SetContactTypePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TAHReticleContactPatchTickOriginal, harmony, false);
            harmony.Patch(TAHReticleContactPatchTickOriginal, null, null, new HarmonyMethod(TAHReticleContactPatchTickTranspiler));
            harmony.Patch(TAHReticleContactPatchSetContactTypeOriginal, new HarmonyMethod(TAHReticleContactPatchSetContactTypePrefix));

            // TNH_HoldPointPatch
            MethodInfo TNH_HoldPointPatchSystemNodeOriginal = typeof(TNH_HoldPoint).GetMethod("ConfigureAsSystemNode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSystemNodePrefix = typeof(TNH_HoldPointPatch).GetMethod("ConfigureAsSystemNodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTakeChallengeEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTakeChallengeEntitiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginHoldOriginal = typeof(TNH_HoldPoint).GetMethod("BeginHoldChallenge", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginHoldPrefix = typeof(TNH_HoldPointPatch).GetMethod("BeginHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginHoldPostfix = typeof(TNH_HoldPointPatch).GetMethod("BeginHoldPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersOriginal = typeof(TNH_HoldPoint).GetMethod("RaiseRandomBarriers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersPrefix = typeof(TNH_HoldPointPatch).GetMethod("RaiseRandomBarriersPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersPostfix = typeof(TNH_HoldPointPatch).GetMethod("RaiseRandomBarriersPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataOriginal = typeof(TNH_DestructibleBarrierPoint).GetMethod("SetCoverPointData", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataPrefix = typeof(TNH_HoldPointPatch).GetMethod("BarrierSetCoverPointDataPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseCompletePhaseOriginal = typeof(TNH_HoldPoint).GetMethod("CompletePhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseCompletePhasePostfix = typeof(TNH_HoldPointPatch).GetMethod("CompletePhasePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchUpdateOriginal = typeof(TNH_HoldPoint).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchUpdatePrefix = typeof(TNH_HoldPointPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginAnalyzingOriginal = typeof(TNH_HoldPoint).GetMethod("BeginAnalyzing", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginAnalyzingPostfix = typeof(TNH_HoldPointPatch).GetMethod("BeginAnalyzingPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnWarpInMarkersOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnWarpInMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnWarpInMarkersPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnWarpInMarkersPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTargetGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTargetGroup", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnTargetGroupPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTargetGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchIdentifyEncryptionOriginal = typeof(TNH_HoldPoint).GetMethod("IdentifyEncryption", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchIdentifyEncryptionPostfix = typeof(TNH_HoldPointPatch).GetMethod("IdentifyEncryptionPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchFailOutOriginal = typeof(TNH_HoldPoint).GetMethod("FailOut", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchFailOutPrefix = typeof(TNH_HoldPointPatch).GetMethod("FailOutPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginPhaseOriginal = typeof(TNH_HoldPoint).GetMethod("BeginPhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginPhasePrefix = typeof(TNH_HoldPointPatch).GetMethod("BeginPhasePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchCompleteHoldOriginal = typeof(TNH_HoldPoint).GetMethod("CompleteHold", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchCompleteHoldPrefix = typeof(TNH_HoldPointPatch).GetMethod("CompleteHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchCompleteHoldPostfix = typeof(TNH_HoldPointPatch).GetMethod("CompleteHoldPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTakeEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnHoldEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnEnemyGroupPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnEnemyGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEnemyGroupPostfix = typeof(TNH_HoldPointPatch).GetMethod("SpawnEnemyGroupPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTurretsOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTurrets", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnTurretsPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTurretsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTurretsPostfix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTurretsPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_HoldPointPatchSystemNodeOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnEntitiesOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchBeginHoldOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchRaiseRandomBarriersOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchRaiseSetCoverPointDataOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchRaiseCompletePhaseOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchUpdateOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchBeginAnalyzingOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnWarpInMarkersOriginal, harmony, false);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnTargetGroupOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchIdentifyEncryptionOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchFailOutOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchBeginPhaseOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchCompleteHoldOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal, harmony, true);
            PatchVerify.Verify(TNH_HoldPointPatchSpawnTurretsOriginal, harmony, true);
            harmony.Patch(TNH_HoldPointPatchSystemNodeOriginal, new HarmonyMethod(TNH_HoldPointPatchSystemNodePrefix));
            harmony.Patch(TNH_HoldPointPatchSpawnEntitiesOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEntitiesPrefix));
            harmony.Patch(TNH_HoldPointPatchBeginHoldOriginal, new HarmonyMethod(TNH_HoldPointPatchBeginHoldPrefix), new HarmonyMethod(TNH_HoldPointPatchBeginHoldPostfix));
            harmony.Patch(TNH_HoldPointPatchRaiseRandomBarriersOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseRandomBarriersPrefix), new HarmonyMethod(TNH_HoldPointPatchRaiseRandomBarriersPostfix));
            harmony.Patch(TNH_HoldPointPatchRaiseSetCoverPointDataOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseSetCoverPointDataPrefix));
            harmony.Patch(TNH_HoldPointPatchRaiseCompletePhaseOriginal, null, new HarmonyMethod(TNH_HoldPointPatchRaiseCompletePhasePostfix));
            harmony.Patch(TNH_HoldPointPatchUpdateOriginal, new HarmonyMethod(TNH_HoldPointPatchUpdatePrefix));
            harmony.Patch(TNH_HoldPointPatchBeginAnalyzingOriginal, null, new HarmonyMethod(TNH_HoldPointPatchBeginAnalyzingPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnWarpInMarkersOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnWarpInMarkersPrefix));
            harmony.Patch(TNH_HoldPointPatchSpawnTargetGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnTargetGroupPrefix));
            harmony.Patch(TNH_HoldPointPatchIdentifyEncryptionOriginal, null, new HarmonyMethod(TNH_HoldPointPatchIdentifyEncryptionPostfix));
            harmony.Patch(TNH_HoldPointPatchFailOutOriginal, new HarmonyMethod(TNH_HoldPointPatchFailOutPrefix));
            harmony.Patch(TNH_HoldPointPatchBeginPhaseOriginal, new HarmonyMethod(TNH_HoldPointPatchBeginPhasePrefix));
            harmony.Patch(TNH_HoldPointPatchCompleteHoldOriginal, new HarmonyMethod(TNH_HoldPointPatchCompleteHoldPrefix), new HarmonyMethod(TNH_HoldPointPatchCompleteHoldPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnTurretsOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnTurretsPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnTurretsPostfix));

            // TNHWeaponCrateSpawnObjectsPatch
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchOriginal = typeof(TNH_WeaponCrate).GetMethod("SpawnObjectsRaw", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchPrefix = typeof(TNHWeaponCrateSpawnObjectsPatch).GetMethod("SpawnObjectsRawPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(TNH_WeaponCrateSpawnObjectsPatchOriginal, harmony, false);
            harmony.Patch(TNH_WeaponCrateSpawnObjectsPatchOriginal, new HarmonyMethod(TNH_WeaponCrateSpawnObjectsPatchPrefix));

            // KinematicPatch
            MethodInfo kinematicPatchOriginal = typeof(Rigidbody).GetMethod("set_isKinematic", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo kinematicPatchPrefix = typeof(KinematicPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(kinematicPatchOriginal, harmony, true);
            harmony.Patch(kinematicPatchOriginal, new HarmonyMethod(kinematicPatchPrefix));

            // PhysicalObjectRBPatch
            MethodInfo physicalObjectRBOriginal = typeof(FVRPhysicalObject).GetMethod("RecoverRigidbody", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo physicalObjectRBPostfix = typeof(PhysicalObjectRBPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(physicalObjectRBOriginal, harmony, true);
            harmony.Patch(physicalObjectRBOriginal, null, new HarmonyMethod(physicalObjectRBPostfix));

            // SetPlayerIFFPatch
            MethodInfo setPlayerIFFPatchOriginal = typeof(FVRPlayerBody).GetMethod("SetPlayerIFF", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setPlayerIFFPatchPrefix = typeof(SetPlayerIFFPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(setPlayerIFFPatchOriginal, harmony, false);
            harmony.Patch(setPlayerIFFPatchOriginal, new HarmonyMethod(setPlayerIFFPatchPrefix));

            // SosigTargetPrioritySystemPatch
            MethodInfo sosigTargetPrioritySystemPatchDefaultOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetDefaultIFFChart", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchMakeEnemyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("MakeEnemy", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchMakeFriendlyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("MakeFriendly", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllEnemyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllEnemy", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllFriendlyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllFriendly", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllyMatrixOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllyMatrix", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchPostfix = typeof(SosigTargetPrioritySystemPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(sosigTargetPrioritySystemPatchDefaultOriginal, harmony, false);
            PatchVerify.Verify(sosigTargetPrioritySystemPatchMakeEnemyOriginal, harmony, false);
            PatchVerify.Verify(sosigTargetPrioritySystemPatchMakeFriendlyOriginal, harmony, false);
            PatchVerify.Verify(sosigTargetPrioritySystemPatchSetAllEnemyOriginal, harmony, false);
            PatchVerify.Verify(sosigTargetPrioritySystemPatchSetAllFriendlyOriginal, harmony, false);
            PatchVerify.Verify(sosigTargetPrioritySystemPatchSetAllyMatrixOriginal, harmony, false);
            harmony.Patch(sosigTargetPrioritySystemPatchDefaultOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchMakeEnemyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchMakeFriendlyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllEnemyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllFriendlyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllyMatrixOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));

            // SimpleLauncher2CycleModePatch
            MethodInfo simpleLauncher2CycleModePatchOriginal = typeof(SimpleLauncher2).GetMethod("CycleMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo simpleLauncher2CycleModePatchPrefix = typeof(SimpleLauncher2CycleModePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(simpleLauncher2CycleModePatchOriginal, harmony, false);
            harmony.Patch(simpleLauncher2CycleModePatchOriginal, new HarmonyMethod(simpleLauncher2CycleModePatchPrefix));

            // PinnedGrenadePatch
            MethodInfo pinnedGrenadePatchUpdateOriginal = typeof(PinnedGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchFixedUpdateOriginal = typeof(PinnedGrenade).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchUpdatePrefix = typeof(PinnedGrenadePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchUpdateTranspiler = typeof(PinnedGrenadePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchUpdatePostfix = typeof(PinnedGrenadePatch).GetMethod("UpdatePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchCollisionOriginal = typeof(PinnedGrenade).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchCollisionTranspiler = typeof(PinnedGrenadePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(pinnedGrenadePatchUpdateOriginal, harmony, false);
            PatchVerify.Verify(pinnedGrenadePatchFixedUpdateOriginal, harmony, false);
            PatchVerify.Verify(pinnedGrenadePatchCollisionOriginal, harmony, false);
            harmony.Patch(pinnedGrenadePatchUpdateOriginal, new HarmonyMethod(pinnedGrenadePatchUpdatePrefix), new HarmonyMethod(pinnedGrenadePatchUpdatePostfix), new HarmonyMethod(pinnedGrenadePatchUpdateTranspiler));
            harmony.Patch(pinnedGrenadePatchFixedUpdateOriginal, new HarmonyMethod(pinnedGrenadePatchUpdatePrefix));
            harmony.Patch(pinnedGrenadePatchCollisionOriginal, null, null, new HarmonyMethod(pinnedGrenadePatchCollisionTranspiler));

            // FVRGrenadePatch
            MethodInfo FVRGrenadePatchUpdateOriginal = typeof(FVRGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo FVRGrenadePatchUpdatePrefix = typeof(FVRGrenadePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo FVRGrenadePatchUpdatePostfix = typeof(FVRGrenadePatch).GetMethod("UpdatePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(FVRGrenadePatchUpdateOriginal, harmony, false);
            harmony.Patch(FVRGrenadePatchUpdateOriginal, new HarmonyMethod(FVRGrenadePatchUpdatePrefix), new HarmonyMethod(FVRGrenadePatchUpdatePostfix));

            // BangSnapPatch
            MethodInfo bangSnapPatchSplodeOriginal = typeof(BangSnap).GetMethod("Splode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo bangSnapPatchSplodePrefix = typeof(BangSnapPatch).GetMethod("SplodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo bangSnapPatchCollisionOriginal = typeof(BangSnap).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bangSnapPatchCollisionTranspiler = typeof(BangSnapPatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(bangSnapPatchSplodeOriginal, harmony, false);
            PatchVerify.Verify(bangSnapPatchCollisionOriginal, harmony, false);
            harmony.Patch(bangSnapPatchSplodeOriginal, new HarmonyMethod(bangSnapPatchSplodePrefix));
            harmony.Patch(bangSnapPatchCollisionOriginal, null ,null, new HarmonyMethod(bangSnapPatchCollisionTranspiler));

            // C4DetonatePatch
            MethodInfo C4DetonatePatchOriginal = typeof(C4).GetMethod("Detonate", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo C4DetonatePatchPrefix = typeof(C4DetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(C4DetonatePatchOriginal, harmony, false);
            harmony.Patch(C4DetonatePatchOriginal, new HarmonyMethod(C4DetonatePatchPrefix));

            // ClaymoreMineDetonatePatch
            MethodInfo claymoreMineDetonatePatchOriginal = typeof(ClaymoreMine).GetMethod("Detonate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo claymoreMineDetonatePatchPrefix = typeof(ClaymoreMineDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(claymoreMineDetonatePatchOriginal, harmony, false);
            harmony.Patch(claymoreMineDetonatePatchOriginal, new HarmonyMethod(claymoreMineDetonatePatchPrefix));

            // SLAMDetonatePatch
            MethodInfo SLAMDetonatePatchOriginal = typeof(SLAM).GetMethod("Detonate", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo SLAMDetonatePatchPrefix = typeof(SLAMDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(SLAMDetonatePatchOriginal, harmony, false);
            harmony.Patch(SLAMDetonatePatchOriginal, new HarmonyMethod(SLAMDetonatePatchPrefix));

            // WristMenuPatch
            MethodInfo wristMenuPatchUpdateOriginal = typeof(FVRWristMenu2).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchUpdatePrefix = typeof(WristMenuPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo wristMenuPatchAwakeOriginal = typeof(FVRWristMenu2).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchAwakePrefix = typeof(WristMenuPatch).GetMethod("AwakePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchVerify.Verify(wristMenuPatchUpdateOriginal, harmony, true);
            PatchVerify.Verify(wristMenuPatchAwakeOriginal, harmony, true);
            harmony.Patch(wristMenuPatchUpdateOriginal, new HarmonyMethod(wristMenuPatchUpdatePrefix));
            harmony.Patch(wristMenuPatchAwakeOriginal, new HarmonyMethod(wristMenuPatchAwakePrefix));

            //// TeleportToPointPatch
            //MethodInfo teleportToPointPatchOriginal = typeof(FVRMovementManager).GetMethod("TeleportToPoint", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(bool) }, null);
            //MethodInfo teleportToPointPatchPrefix = typeof(TeleportToPointPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(teleportToPointPatchOriginal, new HarmonyMethod(teleportToPointPatchPrefix));

            //// SetActivePatch
            //MethodInfo setActivePatchOriginal = typeof(UnityEngine.GameObject).GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setActivePatchPrefix = typeof(SetActivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setActivePatchOriginal, new HarmonyMethod(setActivePatchPrefix));

            if (PatchVerify.writeWhenDone)
            {
                File.WriteAllText("BepInEx/Plugins/H3MP/PatchHashes.json", JObject.FromObject(PatchVerify.hashes).ToString());
            }
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
                foreach (KeyValuePair<int, GameObject> entry in joinTNHInstances)
                {
                    Destroy(entry.Value);
                }
            }
            joinTNHInstances.Clear();

            // Populate instance list
            foreach (KeyValuePair<int, H3MP_TNHInstance> TNHInstance in H3MP_GameManager.TNHInstances)
            {
                if (TNHInstance.Value.letPeopleJoin || TNHInstance.Value.currentlyPlaying.Count == 0)
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
            if (GM.TNH_Manager != null)
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
            for (int i = 0; i < instance.playerIDs.Count; ++i)
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

        public void CreateManagerObject(bool host = false)
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

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            if (loadedScene.name.Equals("TakeAndHold_Lobby_2"))
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
                if (slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Internal)
            {
                if (slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Added)
            {
                if (slot.CurObject != null)
                {
                    slot.CurObject.SetQuickBeltSlot(null);
                }
            }
        }

        // MOD: This method will be used to find the ID of which player to give control of this object to
        //      Mods should patch this if they have a different method of finding the next host, like TNH here for example
        public static int GetBestPotentialObjectHost(int currentController)
        {
            if (Mod.currentTNHInstance != null)
            {
                if (currentController == -1) // This means the potential host could also be us
                {
                    // Going through each like this, we will go through the host of the instance before any other
                    foreach (int playerID in Mod.currentTNHInstance.currentlyPlaying)
                    {
                        // If the player is us and we are **not spectating**??
                        // OR it is another player who is **not spectating**??
                        // TODO: Spectators can still be in control of things, can't they? review this
                        if ((playerID == H3MP_GameManager.ID/* && !Mod.TNHSpectating*/) ||
                             (H3MP_GameManager.players.ContainsKey(playerID)/* && H3MP_GameManager.players[playerID].gameObject.activeSelf*/))
                        {
                            return playerID;
                        }
                    }
                }
                else
                {
                    // Going through each like this, we will go through the host of the instance before any other
                    // TODO: Spectators can still be in control of things, can't they? review this
                    foreach (int playerID in Mod.currentTNHInstance.currentlyPlaying)
                    {
                        if (playerID != currentController/* && H3MP_GameManager.players[playerID].gameObject.activeSelf*/)
                        {
                            return playerID;
                        }
                    }
                }
            }
            else
            {
                if (currentController == -1) // This means the potential host could also be us
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                    {
                        if (player.Key > H3MP_GameManager.ID)
                        {
                            return H3MP_GameManager.ID;
                        }
                        if (player.Value.gameObject.activeSelf)
                        {
                            return player.Key;
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

        public static void SetKinematicRecursive(Transform root, bool value)
        {
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb != null)
            {
                H3MP_KinematicMarker marker = rb.GetComponent<H3MP_KinematicMarker>();

                // If we want to make kinematic we can just set it and mark
                if (value)
                {
                    if (marker == null)
                    {
                        marker = rb.gameObject.AddComponent<H3MP_KinematicMarker>();
                    }
                    ++KinematicPatch.skip;
                    rb.isKinematic = value;
                    --KinematicPatch.skip;
                }
                else // If we don't want it kinematic, we only want to unset it on marked children, because unmarked were not set by us
                {
                    // For example, a piece of an item that is not a tracked item itself but is a child of a tracked item
                    // got set to kinematic by the game, will have its marker destroyed so we don't set it to non kinematic
                    if (marker != null)
                    {
                        ++KinematicPatch.skip;
                        rb.isKinematic = value;
                        --KinematicPatch.skip;
                        Destroy(marker);
                    }
                }
            }

            foreach (Transform child in root.transform)
            {
                SetKinematicRecursive(child, value);
            }
        }

        public static void RemovePlayerFromLists(int playerID)
        {
            H3MP_PlayerManager player = H3MP_GameManager.players[playerID];

            // Manage instance
            if (H3MP_GameManager.activeInstances.ContainsKey(player.instance))
            {
                --H3MP_GameManager.activeInstances[player.instance];
                if (H3MP_GameManager.activeInstances[player.instance] == 0)
                {
                    H3MP_GameManager.activeInstances.Remove(player.instance);
                }
            }

            // Manage scene/instance
            H3MP_GameManager.playersByInstanceByScene[player.scene][player.instance].Remove(player.ID);
            if (H3MP_GameManager.playersByInstanceByScene[player.scene][player.instance].Count == 0)
            {
                H3MP_GameManager.playersByInstanceByScene[player.scene].Remove(player.instance);
            }
            if(H3MP_GameManager.playersByInstanceByScene[player.scene].Count == 0)
            {
                H3MP_GameManager.playersByInstanceByScene.Remove(player.scene);
            }

            // Manage players present
            if (player.scene.Equals(SceneManager.GetActiveScene().name) && H3MP_GameManager.synchronizedScenes.ContainsKey(player.scene) && H3MP_GameManager.instance == player.instance)
            {
                --H3MP_GameManager.playersPresent;
            }

            RemovePlayerFromSpecificLists(player);

            GameObject.Destroy(H3MP_GameManager.players[playerID].gameObject);
            H3MP_GameManager.players.Remove(playerID);
        }

        // MOD: Will be called by RemovePlayerFromLists before the player finally gets destroyed
        //      This is where you would manage the player being removed from the network if you're keeping a reference of them anywhere
        //      Example here is TNH instances
        public static void RemovePlayerFromSpecificLists(H3MP_PlayerManager player)
        {
            if (H3MP_GameManager.TNHInstances.TryGetValue(player.instance, out H3MP_TNHInstance currentInstance))
            {
                int preHost = currentInstance.playerIDs[0];
                currentInstance.playerIDs.Remove(player.ID);
                if (currentInstance.playerIDs.Count == 0)
                {
                    H3MP_GameManager.TNHInstances.Remove(player.instance);

                    if (Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(player.instance))
                    {
                        GameObject.Destroy(Mod.joinTNHInstances[player.instance]);
                        Mod.joinTNHInstances.Remove(player.instance);
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
                        Mod.currentTNHInstancePlayers != null && Mod.currentTNHInstancePlayers.ContainsKey(player.ID))
                    {
                        Destroy(Mod.currentTNHInstancePlayers[player.ID]);
                        Mod.currentTNHInstancePlayers.Remove(player.ID);

                        // Switch host if necessary
                        if (preHost != currentInstance.playerIDs[0])
                        {
                            Mod.currentTNHInstancePlayers[currentInstance.playerIDs[0]].transform.GetChild(0).GetComponent<Text>().text += " (Host)";
                        }
                    }

                    // Remove from other active lists
                    currentInstance.currentlyPlaying.Remove(player.ID);
                    currentInstance.played.Remove(player.ID);
                    currentInstance.dead.Remove(player.ID);
                }
            }
        }

        public static void LogInfo(string message)
        {
            modInstance.Logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            modInstance.Logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            modInstance.Logger.LogError(message);
        }
    }

#region General Patches
    // Used to verify integrity of other patches by checking if there were any changes to the original methods
    class PatchVerify
    {
        static Type ILManipulatorType;
        static MethodInfo getInstructionsMethod;

        public static Dictionary<string, int> hashes;
        public static bool writeWhenDone;

        public static void Verify(MethodInfo methodInfo, Harmony harmony, bool breaking)
        {
            if (hashes == null)
            {
                if (File.Exists("BepInEx/Plugins/H3MP/PatchHashes.json"))
                {
                    hashes = JObject.Parse(File.ReadAllText("BepInEx/Plugins/H3MP/PatchHashes.json")).ToObject<Dictionary<string, int>>();
                }
                else
                {
                    hashes = new Dictionary<string, int>();
                    writeWhenDone = true;
                }
            }

            if (ILManipulatorType == null)
            {
                ILManipulatorType = typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.Internal.Patching.ILManipulator");
                getInstructionsMethod = ILManipulatorType.GetMethod("GetInstructions", BindingFlags.Public | BindingFlags.Instance);
            }

            string identifier = methodInfo.DeclaringType.Name + "." + methodInfo.Name + GetParamArrHash(methodInfo.GetParameters()).ToString();

            // Get IL instructions of the method
            ILGenerator generator = PatchProcessor.CreateILGenerator(methodInfo);
            Mono.Cecil.Cil.MethodBody bodyCopy = PatchManager.GetMethodPatcher(methodInfo).CopyOriginal().Definition.Body;
            object ilManipulator = Activator.CreateInstance(ILManipulatorType, bodyCopy, false);
            object[] paramArr = new object[] { generator, null };
            List<CodeInstruction> instructions = (List<CodeInstruction>)getInstructionsMethod.Invoke(ilManipulator, paramArr);

            // Build hash from all instructions
            string s = "";
            for (int i = 0; i < instructions.Count; ++i)
            {
                CodeInstruction instruction = instructions[i];
                s += (instruction.opcode == null ? "null opcode" : instruction.opcode.ToString()) + (instruction.operand == null ? "null operand" : instruction.operand.ToString());
            }
            int hash = s.GetHashCode();

            // Verify hash
            if (hashes.TryGetValue(identifier, out int originalHash))
            {
                if (originalHash != hash)
                {
                    if (breaking)
                    {
                        Mod.LogError("PatchVerify: " + identifier + " failed patch verify, this will most probably break H3MP! Update the mod.\nOriginal hash: "+originalHash+", new hash: "+hash);
                    }
                    else
                    {
                        Mod.LogWarning("PatchVerify: " + identifier + " failed patch verify, this will most probably break some part of H3MP. Update the mod.\nOriginal hash: " + originalHash + ", new hash: " + hash);
                    }

                    hashes[identifier] = hash;
                }
            }
            else
            {
                hashes.Add(identifier, hash);
                if (!writeWhenDone)
                {
                    Mod.LogWarning("PatchVerify: " + identifier + " not found in hashes. Most probably a new patch. This warning will remain until new hash file is written.");
                }
            }
        }

        static int GetParamArrHash(ParameterInfo[] paramArr)
        {
            int hash = 0;
            foreach (ParameterInfo t in paramArr)
            {
                hash += t.ParameterType.Name.GetHashCode();
            }
            return hash;
        }
    }

    // Patches SteamVR_LoadLevel.Begin() So we can keep track of which scene we are loading
    class LoadLevelBeginPatch
    {
        public static string loadingLevel;

        static void Prefix(string levelName)
        {
            loadingLevel = levelName;
        }
    }

    // Patches RigidBody.set_isKinematic to keep track of when it is being set
    class KinematicPatch
    {
        public static int skip;

        static bool Prefix(ref Rigidbody __instance, bool value)
        {
            if (Mod.managerObject == null || skip > 0)
            {
                return true;
            }

            // If game is setting this as kinematic
            if (value)
            {
                // Check if we have a marker (meaning H3MP set it as kinematic due to no control)
                H3MP_KinematicMarker marker = __instance.GetComponent<H3MP_KinematicMarker>();
                if (marker != null)
                {
                    // Destroy the marker because the game has now set its own kinematic value, so when we take control of the item
                    // we don't want to set it to non kinematic
                    GameObject.Destroy(marker);
                }
            }
            else // Game is setting this as non-kinematic
            {
                // Check if this is a tracked item under our control
                H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
                {
                    // Return false because we don't want to set this to non-kinematic
                    // Consider the case of an item getting detached from another by some vanilla process
                    // When that is done, the process sets the rigidbody as non-kinematic
                    // But if this item is not under our control, we want it to remain kinematic, otherwise physics are going to break things
                    return false;
                }
                else
                {
                    // We can destroy an existing marker right away because the rigidbody will now be non-kinematic anyway
                    // So when we take control of the item, we don't need to set the kinematic value, so no need for the marker anymore
                    H3MP_KinematicMarker marker = __instance.GetComponent<H3MP_KinematicMarker>();
                    if (marker != null)
                    {
                        GameObject.Destroy(marker);
                    }
                }
            }

            return true;
        }
    }

    // Patches FVRPhysicalObject.RecoverRigidbody to make sure a non-controlled RB is kinematic
    class PhysicalObjectRBPatch
    {
        static void Postfix(FVRPhysicalObject __instance)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            // Check if this is a tracked item under our control
            H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
            {
                // If tracked and not controller, set kinematic
                __instance.RootRigidbody.isKinematic = true;
            }
        }
    }

    // Patches FVRPlayerBody.SetPlayerIFF to keep players' IFFs up to date
    class SetPlayerIFFPatch
    {
        static void Prefix(int iff)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.PlayerIFF(H3MP_GameManager.ID, iff);
            }
            else
            {
                H3MP_ClientSend.PlayerIFF(iff);
            }
        }
    }

    // Patches FVRWristMenu2.Update and Awake to add our H3MP section to it
    class WristMenuPatch
    {
        static void UpdatePrefix(FVRWristMenu2 __instance)
        {
            if (!H3MP_WristMenuSection.init)
            {
                H3MP_WristMenuSection.init = true;

                AddSection(__instance);

                // Regenerate with our new section
                __instance.RegenerateButtons();
            }
        }

        static void AwakePrefix(FVRWristMenu2 __instance)
        {
            AddSection(__instance);
        }

        private static void AddSection(FVRWristMenu2 __instance)
        {
            Mod.LogInfo("Initializing wrist menu section");
            GameObject section = new GameObject("Section_H3MP", typeof(RectTransform));
            section.transform.SetParent(__instance.MenuGO.transform);
            section.transform.localPosition = new Vector3(0, 300, 0);
            section.transform.localRotation = Quaternion.identity;
            section.transform.localScale = Vector3.one;
            section.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 350);
            FVRWristMenuSection sectionScript = section.AddComponent<H3MP_WristMenuSection>();
            sectionScript.ButtonText = "H3MP";
            __instance.Sections.Add(sectionScript);
            section.SetActive(false);
        }
    }

    // DEBUG PATCH Patches GameObject.SetActive
    class SetActivePatch
    {
        static void Prefix(ref GameObject __instance, bool value)
        {
            if (value)
            {
                Mod.LogWarning("SetActivePatch called with true on " + __instance.name + ":\n" + Environment.StackTrace);
            }
        }
    }

    // DEBUG PATCH Patches FVRMovementManager
    class TeleportToPointPatch
    {
        static void Prefix(Vector3 point)
        {
            Mod.LogWarning("TeleportToPoint called with point: (" + point.x + "," + point.y + "," + point.z + "):\n" + Environment.StackTrace);
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(0);
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(H3MP_Client.singleton.ID);
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                        }
                    }
                }
                else // Although SosigLinks are FVRPhysicalObjects, they don't have an objectWrapper, so they won't be tracked items
                {
                    SosigLink sosigLink = ___m_currentInteractable.GetComponent<SosigLink>();
                    if (sosigLink != null)
                    {
                        // We just grabbed a sosig
                        H3MP_TrackedSosig trackedSosig = sosigLink.S.GetComponent<H3MP_TrackedSosig>();
                        if (trackedSosig != null && trackedSosig.data.trackedID != -1 && trackedSosig.data.localTrackedID == -1)
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(0);
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(H3MP_Client.singleton.ID);
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(0);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(H3MP_Client.singleton.ID);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        bool primaryHand = __instance == __instance.S.Hand_Primary;
                        H3MP_TrackedSosig trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
                        if (trackedSosig.data.trackedID == -1)
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(0);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                        int slotIndex = 0;
                        for (int i = 0; i < __instance.I.Slots.Count; ++i)
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(H3MP_Client.singleton.ID);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
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
            if (skip > 0)
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

    // Patches FVRViveHand.BeginFlick to take control of the object
    class GrabbityPatch
    {
        static bool Prefix(FVRPhysicalObject o)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If spectating we prevent the flick entirely
            if (Mod.TNHSpectating)
            {
                return false;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(o, out H3MP_TrackedItem currentItem) ? currentItem : o.GetComponent<H3MP_TrackedItem>();
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(0);
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(H3MP_Client.singleton.ID);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                    }
                }
            }

            return true;
        }
    }

    // Patches GBeamer to take control of manipulated objects
    class GBeamerPatch
    {
        static bool hadObject;

        static void ObjectSearchPrefix(bool ___m_hasObject)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            hadObject = ___m_hasObject;
        }

        static void ObjectSearchPostfix(FVRPhysicalObject ___m_obj)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!hadObject && ___m_obj != null)
            {
                // Just started manipulating this item, take control
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(___m_obj, out H3MP_TrackedItem currentItem) ? currentItem : ___m_obj.GetComponent<H3MP_TrackedItem>();
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(0);
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
                            Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                            trackedItem.data.SetController(H3MP_Client.singleton.ID);
                            trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                            H3MP_GameManager.items.Add(trackedItem.data);
                        }
                    }
                }
            }
        }

        static IEnumerable<CodeInstruction> WideShuntTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To take control of every object we are about to shunt
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load the current physical object
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GBeamerPatch), "TakeControl"))); // Call our TakeControl method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldloc_3)
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    break;
                }
            }
            return instructionList;
        }

        public static void TakeControl(FVRPhysicalObject physObj)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            // Just started manipulating this item, take control
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(physObj, out H3MP_TrackedItem currentItem) ? currentItem : physObj.GetComponent<H3MP_TrackedItem>();
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(0);
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
                        Mod.SetKinematicRecursive(trackedItem.physicalObject.transform, false);
                        trackedItem.data.SetController(H3MP_Client.singleton.ID);
                        trackedItem.data.localTrackedID = H3MP_GameManager.items.Count;
                        H3MP_GameManager.items.Add(trackedItem.data);
                    }
                }
            }
        }
    }
#endregion

#region Action Patches
    // Note: All projectile fire patches are necessary for 2 things: synchronizing fire action,
    //       and making sure that the shot is in same position/direction on all clients
    //       Synchronizing the action is simple, it is the pos/dir that requires transpilers and make these so complex
    // Note: There is an important problem to keep in mind that make passing the round class necessary
    //       When we fire a weapon, consider a handgun, the fire packet is sent
    //       An update of the handgun then gets sent, now telling other clients that this handgun's chamber is empty
    //       The fire is sent through TCP, while the update is sent through UDP. Although the fire gets sent first, the update gets there first
    //       Other client's chambers then return false from their Fire(), preventing the weapon from firing
    //       On other clients, we use the passed round class to fill the chamber prior to firing, and then set it back to its previous state
    //       So, it is necessary to send, alongside the fire packet, data to override the latest update with just what we need to ensure we can fire
    /* TODO: Fire patches for
     * EncryptionBotAgile.Fire // Does not inherit from FVRPhysicalObject, need to check this type's structure to know how to handle it
     * EncryptionBotCrystal.FirePulseShot // Does not inherit from FVRPhysicalObject, need to check this type's structure to know how to handle it
     * EncryptionBotHardened.Fire // Does not inherit from FVRPhysicalObject, need to check this type's structure to know how to handle it
     * RemoteGun.Fire // THIS IS NOT A REMOTE MISSILE LAUNCHER, NEED TO FIND OUT WHAT IT IS
     * AIFireArm.FireBullet // Will have to check if this is necessary (it is actually used?), it is also an FVRDestroyableObject, need to see how to handle that
     * RonchWeapon.Fire // Considering ronch is an enemy type, we will probably have to make it into its own sync object type with its own lists
     * DodecaLauncher // Uses dodeca missiles
     */
    // Patches FVRFireArm.Fire so we can keep track of when a firearm is fired
    class FirePatch
    {
        public static int skipSending;
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        // Update override data
        public static bool fireSuccessful;
        public static FireArmRoundClass roundClass;

        static void Prefix(FVRFireArmChamber chamber)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            FVRFireArmRound round = chamber.GetRound();
            if (round == null || round.IsSpent)
            {
                fireSuccessful = false;
            }
            else
            {
                fireSuccessful = true;
                roundClass = round.RoundClass;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load index
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FirePatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load index
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FirePatch), "GetDirection"))); // Call our GetDirection method

            bool skippedFirstDir = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("op_Subtraction"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    if (skippedFirstDir)
                    {
                        instructionList.InsertRange(i + 1, toInsert1);
                    }
                    else
                    {
                        skippedFirstDir = true;
                    }
                }
            }
            return instructionList;
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void Postfix(ref FVRFireArm __instance)
        {
            // Skip sending will prevent fire patch from handling its own data, as we want to handle it elsewhere
            if (skipSending > 0)
            {
                return;
            }

            --Mod.skipAllInstantiates;

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
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
                        H3MP_ServerSend.WeaponFire(0, trackedItem.data.trackedID, roundClass, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.WeaponFire(trackedItem.data.trackedID, roundClass, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches SosigWeapon.FireGun so we can keep track of when a SosigWeapon is fired
    class FireSosigWeaponPatch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        // Update override data
        static bool fireSuccessful;

        static void Prefix(ref SosigWeapon __instance, int ___m_shotsLeft)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            fireSuccessful = ___m_shotsLeft > 0 && __instance.MechaState == SosigWeapon.SosigWeaponMechaState.ReadyToFire;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load index
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireSosigWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load index
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireSosigWeaponPatch), "GetDirection"))); // Call our GetDirection method

            bool skippedFirstPos = false;
            bool skippedFirstDir = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_1)
                {
                    if (skippedFirstPos)
                    {
                        instructionList.InsertRange(i + 1, toInsert0);
                    }
                    else
                    {
                        skippedFirstPos = true;
                    }
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    if (skippedFirstDir)
                    {
                        instructionList.InsertRange(i + 1, toInsert1);
                        break;
                    }
                    else
                    {
                        skippedFirstDir = true;
                    }
                }
            }
            return instructionList;
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void Postfix(ref SosigWeapon __instance, float recoilMult)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? H3MP_GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.trackedID != -1)
                {
                    // Send the fire action to other clients only if we control it
                    if (H3MP_ThreadManager.host)
                    {
                        if (trackedItem.data.controller == 0)
                        {
                            H3MP_ServerSend.SosigWeaponFire(0, trackedItem.data.trackedID, recoilMult, positions, directions);
                        }
                    }
                    else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                    {
                        H3MP_ClientSend.SosigWeaponFire(trackedItem.data.trackedID, recoilMult, positions, directions);
                    }
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches LAPD2019.Fire so we can keep track of when an LAPD2019 is fired
    class FireLAPD2019Patch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        // Update override data
        static bool fireSucessful;
        static int curChamber;
        static FireArmRoundClass roundClass;

        static void Prefix(ref LAPD2019 __instance, bool ___m_isCapacitorCharged)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            curChamber = __instance.CurChamber;
            if (__instance.Chambers[__instance.CurChamber].GetRound() != null)
            {
                roundClass = __instance.Chambers[__instance.CurChamber].GetRound().RoundClass;
                fireSucessful = true;
            }
            else
            {
                fireSucessful = false;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load index i
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireLAPD2019Patch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load index i
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireLAPD2019Patch), "GetDirection"))); // Call our GetDirection method

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldloc_S, 14)); // Load index j
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireLAPD2019Patch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert3 = new List<CodeInstruction>();
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 14)); // Load index j
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 15)); // Load gameObject
            toInsert3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireLAPD2019Patch), "GetDirection"))); // Call our GetDirection method

            bool foundFirstPos = false;
            bool foundFirstDir = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("GetMuzzle") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_position"))
                {
                    if (foundFirstPos)
                    {
                        instructionList.InsertRange(i + 2, toInsert2);
                    }
                    else
                    {
                        instructionList.InsertRange(i + 2, toInsert0);
                        foundFirstPos = true;
                    }
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_transform") &&
                    instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    if (foundFirstDir)
                    {
                        instructionList.InsertRange(i + 2, toInsert3);
                        break;
                    }
                    else
                    {
                        instructionList.InsertRange(i + 2, toInsert1);
                        foundFirstDir = true;
                    }
                }
            }
            return instructionList;
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void Postfix(ref LAPD2019 __instance)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSucessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
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
                        H3MP_ServerSend.LAPD2019Fire(0, trackedItem.data.trackedID, curChamber, roundClass, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.LAPD2019Fire(trackedItem.data.trackedID, curChamber, roundClass, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches Minigun.Fire so we can keep track of when an LAPD2019 is fired
    class FireMinigunPatch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        static Minigun currentMinigun;
        static H3MP_TrackedItem trackedItem;

        // Update override data
        static bool fireSucessful;
        static FireArmRoundClass roundClass;

        static bool Prefix(ref Minigun __instance, int ___m_numBullets)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            fireSucessful = ___m_numBullets > 0;
            if (__instance != currentMinigun)
            {
                currentMinigun = __instance;
                trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
            }
            if (trackedItem != null && !overriden && trackedItem.data.controller != H3MP_GameManager.ID)
            {
                fireSucessful = false;
                return false;
            }
            roundClass = __instance.LoadedRounds[0].LR_Class;

            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireMinigunPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireMinigunPatch), "GetDirection"))); // Call our GetDirection method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_0)
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_transform") &&
                    instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 2, toInsert1);
                    break;
                }
            }
            return instructionList;
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void Postfix(ref Minigun __instance)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (fireSucessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.MinigunFire(0, trackedItem.data.trackedID, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.MinigunFire(trackedItem.data.trackedID, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches AttachableFirearm.Fire so we can keep track of when an AttachableFirearm is fired
    class FireAttachableFirearmPatch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        // Update override data
        static FireArmRoundClass roundClass;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load index
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireAttachableFirearmPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load index
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireAttachableFirearmPatch), "GetDirection"))); // Call our GetDirection method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == null || instruction.operand == null)
                {
                    continue;
                }

                if (instruction.operand.ToString().Contains("op_Subtraction"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_transform") &&
                    instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 2, toInsert1);
                    break;
                }
            }
            return instructionList;
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void Prefix(ref AttachableFirearm __instance)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance.Attachment, out H3MP_TrackedItem item) ? item : __instance.Attachment.GetComponent<H3MP_TrackedItem>();
            roundClass = trackedItem.attachableFirearmGetChamberFunc().GetRound().RoundClass;
        }

        static void Postfix(ref AttachableFirearm __instance, bool firedFromInterface)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance.Attachment) ? H3MP_GameManager.trackedItemByItem[__instance.Attachment] : __instance.Attachment.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.AttachableFirearmFire(0, trackedItem.data.trackedID, roundClass, firedFromInterface, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.AttachableFirearmFire(trackedItem.data.trackedID, roundClass, firedFromInterface, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches RevolvingShotgun.Fire so we can skip 
    class FireRevolvingShotgunPatch
    {
        static void Prefix()
        {
            ++FirePatch.skipSending;
        }

        static void Postfix(ref RevolvingShotgun __instance)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.RevolvingShotgunFire(0, trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.RevolvingShotgunFire(trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches Revolver.Fire so we can track fire action
    class FireRevolverPatch
    {
        static void Prefix()
        {
            ++FirePatch.skipSending;
        }

        static void Postfix(ref Revolver __instance)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.RevolverFire(0, trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.RevolverFire(trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches SingleActionRevolver.Fire so we can track fire action
    class FireSingleActionRevolverPatch
    {
        static void Prefix()
        {
            ++FirePatch.skipSending;
        }

        static void Postfix(ref SingleActionRevolver __instance)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.SingleActionRevolverFire(0, trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.SingleActionRevolverFire(trackedItem.data.trackedID, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches GrappleGun.Fire so we can skip 
    class FireGrappleGunPatch
    {
        static int preChamber;

        static void Prefix(int ___m_curChamber)
        {
            ++FirePatch.skipSending;

            preChamber = ___m_curChamber;
        }

        static void Postfix(ref GrappleGun __instance)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.GrappleGunFire(0, trackedItem.data.trackedID, FirePatch.roundClass, preChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.GrappleGunFire(trackedItem.data.trackedID, FirePatch.roundClass, preChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches Derringer.FireBarrel so we can skip 
    class FireDerringerPatch
    {
        static void Prefix(ref Derringer __instance)
        {
            ++FirePatch.skipSending;
        }

        static void Postfix(ref Derringer __instance, int i)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.DerringerFire(0, trackedItem.data.trackedID, FirePatch.roundClass, i, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.DerringerFire(trackedItem.data.trackedID, FirePatch.roundClass, i, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches BreakActionWeapon.Fire so we can skip 
    class FireBreakActionWeaponPatch
    {
        static void Prefix()
        {
            ++FirePatch.skipSending;
        }

        static void Postfix(ref BreakActionWeapon __instance, int b)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
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
                        H3MP_ServerSend.BreakActionWeaponFire(0, trackedItem.data.trackedID, FirePatch.roundClass, b, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.BreakActionWeaponFire(trackedItem.data.trackedID, FirePatch.roundClass, b, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches LeverActionFirearm.Fire so we can skip 
    class FireLeverActionFirearmPatch
    {
        static bool hammer1Cocked;
        static bool hammer2Cocked;

        static void Prefix(bool ___m_isHammerCocked, bool ___m_isHammerCocked2)
        {
            ++FirePatch.skipSending;

            hammer1Cocked = ___m_isHammerCocked;
            hammer2Cocked = ___m_isHammerCocked2;
        }

        static void Postfix(ref LeverActionFirearm __instance, bool ___m_isHammerCocked, bool ___m_isHammerCocked2)
        {
            --FirePatch.skipSending;
            --Mod.skipAllInstantiates;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            //  Get which hammer went down
            bool hammer1 = true;
            if (!___m_isHammerCocked2 && hammer2Cocked)
            {
                hammer1 = false;
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
                        H3MP_ServerSend.LeverActionFirearmFire(0, trackedItem.data.trackedID, FirePatch.roundClass, hammer1, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.LeverActionFirearmFire(trackedItem.data.trackedID, FirePatch.roundClass, hammer1, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches FlintlockBarrel to keep track of fire actions
    class FireFlintlockWeaponPatch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;
        public static int burnSkip;
        public static int fireSkip;

        // Update override data
        public static bool fireSuccessful;
        public static FlintlockBarrel.LoadedElementType[] loadedElementTypes;
        public static float[] loadedElementPositions;
        public static int powderAmount;
        public static bool ramRod;
        public static float num2;

        public static int[] loadedElementPowderAmounts;
        public static float num5;

        static void BurnOffOuterPrefix(FlintlockBarrel __instance)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            if (burnSkip > 0 || Mod.managerObject == null)
            {
                return;
            }

            fireSuccessful = __instance.LoadedElements.Count > 0 && __instance.LoadedElements[__instance.LoadedElements.Count - 1].Type == FlintlockBarrel.LoadedElementType.Powder;
            if (fireSuccessful)
            {
                loadedElementTypes = new FlintlockBarrel.LoadedElementType[__instance.LoadedElements.Count];
                loadedElementPositions = new float[__instance.LoadedElements.Count];
                for (int i = 0; i < __instance.LoadedElements.Count; ++i)
                {
                    loadedElementTypes[i] = __instance.LoadedElements[i].Type;
                    loadedElementPositions[i] = __instance.LoadedElements[i].Position;
                }
                powderAmount = __instance.LoadedElements[__instance.LoadedElements.Count - 1].PowderAmount;
                ramRod = ((FlintlockWeapon)Mod.FlintlockBarrel_m_weapon.GetValue(__instance)).RamRod.GetCurBarrel() == __instance;
            }
        }

        static IEnumerable<CodeInstruction> BurnOffOuterTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load index 0
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load index 0
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_S, 6)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetDirection"))); // Call our GetDirection method

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load index
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert3 = new List<CodeInstruction>();
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load index
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 10)); // Load gameObject
            toInsert3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetDirection"))); // Call our GetDirection method

            // To get num2
            List<CodeInstruction> toInsert4 = new List<CodeInstruction>();
            toInsert4.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load num2
            toInsert4.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetNum2"))); // Call our GetNum2
            toInsert4.Add(new CodeInstruction(OpCodes.Stloc_S, 4)); // Set num2

            bool foundFirstPos = false;
            bool skippedSecondDir = false;
            bool foundFirstDir = false;
            bool foundNum2 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (!foundNum2 && instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("System.Single (4)"))
                {
                    instructionList.InsertRange(i + 1, toInsert4);
                    foundNum2 = true;
                    continue;
                }

                if (!foundFirstPos && instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    foundFirstPos = true;
                    continue;
                }
                if (foundFirstPos && instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    continue;
                }

                if (!foundFirstDir && instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 1, toInsert1);
                    foundFirstDir = true;
                    continue;
                }
                if (foundFirstDir && !skippedSecondDir)
                {
                    skippedSecondDir = true;
                    continue;
                }
                if (foundFirstDir && skippedSecondDir && instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 1, toInsert3);
                    break;
                }
            }
            return instructionList;
        }

        public static float GetNum2(float num2)
        {
            if (overriden)
            {
                return FireFlintlockWeaponPatch.num2;
            }
            else
            {
                FireFlintlockWeaponPatch.num2 = num2;
                return num2;
            }
        }

        public static float GetNum5(float num5)
        {
            if (overriden)
            {
                return FireFlintlockWeaponPatch.num5;
            }
            else
            {
                FireFlintlockWeaponPatch.num5 = num5;
                return num5;
            }
        }

        public static Vector3 GetPosition(Vector3 position, int index)
        {
            if (overriden)
            {
                if (positions != null && positions.Count > index)
                {
                    return positions[index];
                }
                else
                {
                    return position;
                }
            }
            else
            {
                AddFirePos(position);
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, int index, GameObject gameObject)
        {
            if (overriden)
            {
                if (directions != null && directions.Count > index)
                {
                    gameObject.transform.rotation = Quaternion.LookRotation(directions[index]);
                    return directions[index];
                }
                else
                {
                    return direction;
                }
            }
            else
            {
                AddFireDir(direction);
                return direction;
            }
        }

        static void AddFirePos(Vector3 pos)
        {
            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            positions.Add(pos);
        }

        static void AddFireDir(Vector3 dir)
        {

            if (Mod.skipNextFires > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            if (positions == null)
            {
                positions = new List<Vector3>();
                directions = new List<Vector3>();
            }

            directions.Add(dir);
        }

        static void BurnOffOuterPostfix(ref FlintlockBarrel __instance)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (burnSkip > 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            FlintlockWeapon FLW = (FlintlockWeapon)Mod.FlintlockBarrel_m_weapon.GetValue(__instance);
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(FLW) ? H3MP_GameManager.trackedItemByItem[FLW] : FLW.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.FlintlockWeaponBurnOffOuter(0, trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, powderAmount, ramRod, num2, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.FlintlockWeaponBurnOffOuter(trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, powderAmount, ramRod, num2, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }

        static void FirePrefix(FlintlockBarrel __instance)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            if (fireSkip > 0 || Mod.managerObject == null)
            {
                return;
            }

            fireSuccessful = __instance.LoadedElements.Count > 0 && __instance.LoadedElements[__instance.LoadedElements.Count - 1].Type == FlintlockBarrel.LoadedElementType.Powder;
            if (fireSuccessful)
            {
                loadedElementTypes = new FlintlockBarrel.LoadedElementType[__instance.LoadedElements.Count];
                loadedElementPositions = new float[__instance.LoadedElements.Count];
                loadedElementPowderAmounts = new int[__instance.LoadedElements.Count];
                for (int i = 0; i < __instance.LoadedElements.Count; ++i)
                {
                    loadedElementTypes[i] = __instance.LoadedElements[i].Type;
                    loadedElementPositions[i] = __instance.LoadedElements[i].Position;
                    loadedElementPowderAmounts[i] = __instance.LoadedElements[i].PowderAmount;
                }
                ramRod = ((FlintlockWeapon)Mod.FlintlockBarrel_m_weapon.GetValue(__instance)).RamRod.GetCurBarrel() == __instance;
            }
        }

        static IEnumerable<CodeInstruction> FireTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load index 0
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load index 0
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_S, 11)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetDirection"))); // Call our GetDirection method

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldloc_S, 12)); // Load index
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert3 = new List<CodeInstruction>();
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 12)); // Load index
            toInsert3.Add(new CodeInstruction(OpCodes.Ldloc_S, 15)); // Load gameObject
            toInsert3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetDirection"))); // Call our GetDirection method

            // To get num5
            List<CodeInstruction> toInsert4 = new List<CodeInstruction>();
            toInsert4.Add(new CodeInstruction(OpCodes.Ldloc_S, 9)); // Load num5
            toInsert4.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetNum5"))); // Call our GetNum5
            toInsert4.Add(new CodeInstruction(OpCodes.Stloc_S, 9)); // Set num5

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert5 = new List<CodeInstruction>();
            toInsert5.Add(new CodeInstruction(OpCodes.Ldloc_S, 20)); // Load index
            toInsert5.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert6 = new List<CodeInstruction>();
            toInsert6.Add(new CodeInstruction(OpCodes.Ldloc_S, 20)); // Load index
            toInsert6.Add(new CodeInstruction(OpCodes.Ldloc_S, 22)); // Load gameObject
            toInsert6.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireFlintlockWeaponPatch), "GetDirection"))); // Call our GetDirection method

            bool foundFirstPos = false;
            bool foundFirstDir = false;
            bool foundNum5 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (!foundNum5 && instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.Single (9)"))
                {
                    instructionList.InsertRange(i + 1, toInsert4);
                    foundNum5 = true;
                    continue;
                }

                if (!foundFirstPos && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("Muzzle") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 2, toInsert0);
                    foundFirstPos = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.Vector3 (13)"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.Vector3 (21)"))
                {
                    instructionList.InsertRange(i + 1, toInsert5);
                    continue;
                }


                if (!foundFirstDir && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("Muzzle") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 2, toInsert1);
                    foundFirstDir = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (15)") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_transform") &&
                    instructionList[i + 2].opcode == OpCodes.Callvirt && instructionList[i + 2].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 3, toInsert3);
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (22)") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_transform") &&
                    instructionList[i + 2].opcode == OpCodes.Callvirt && instructionList[i + 2].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 3, toInsert6);
                    break; ;
                }
            }
            return instructionList;
        }

        static void FirePostfix(ref FlintlockBarrel __instance)
        {
            --Mod.skipAllInstantiates;

            overriden = false;

            if (fireSkip > 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSuccessful || Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            FlintlockWeapon FLW = (FlintlockWeapon)Mod.FlintlockBarrel_m_weapon.GetValue(__instance);
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(FLW) ? H3MP_GameManager.trackedItemByItem[FLW] : FLW.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.FlintlockWeaponFire(0, trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, loadedElementPowderAmounts, ramRod, num5, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.FlintlockWeaponFire(trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, loadedElementPowderAmounts, ramRod, num5, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    // Patches HCB.ReleaseSled to keep track of fire actions
    class FireHCBPatch
    {
        public static bool overriden;
        public static Vector3 position;
        public static Vector3 direction;
        public static int releaseSledSkip;

        static bool Prefix(HCB __instance, ref HCB.SledState ___m_sledState, ref float ___m_cookedAmount)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            if (Mod.managerObject == null)
            {
                return true;
            }

            ___m_sledState = HCB.SledState.Forward;
            __instance.Sled.localPosition = __instance.SledPos_Forward.localPosition;
            Mod.HCB_UpdateStrings.Invoke(__instance, null);
            if (__instance.Chamber.IsFull)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.BoltPrefab, GetPos(__instance.MuzzlePos.position), __instance.MuzzlePos.rotation);
                HCBBolt component = gameObject.GetComponent<HCBBolt>();
                component.Fire(GetDir(__instance.MuzzlePos.forward), GetPos(__instance.MuzzlePos.position), 1f);
                component.SetCookedAmount(___m_cookedAmount);
                __instance.Chamber.SetRound(null, false);
            }
            ___m_cookedAmount = 0;
            __instance.PlayAudioAsHandling(__instance.AudEvent_Fire, __instance.Sled.transform.position);

            if (releaseSledSkip == 0)
            {
                // Get tracked item
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
                if (trackedItem != null)
                {
                    // Send the fire action to other clients only if we control it
                    if (H3MP_ThreadManager.host)
                    {
                        if (trackedItem.data.controller == 0)
                        {
                            H3MP_ServerSend.HCBReleaseSled(0, trackedItem.data.trackedID, ___m_cookedAmount, position, direction);
                        }
                    }
                    else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                    {
                        H3MP_ClientSend.HCBReleaseSled(trackedItem.data.trackedID, ___m_cookedAmount, position, direction);
                    }
                }
            }

            return false;
        }

        static Vector3 GetPos(Vector3 original)
        {
            if (overriden)
            {
                return position;
            }
            else
            {
                position = original;
                return original;
            }
        }

        static Vector3 GetDir(Vector3 original)
        {
            if (overriden)
            {
                return direction;
            }
            else
            {
                direction = original;
                return original;
            }
        }

        static void Postfix()
        {
            --Mod.skipAllInstantiates;

            overriden = false;
        }
    }

    // Patches StingerLauncher.Fire so we can keep track of fire event
    class FireStingerLauncherPatch
    {
        public static bool overriden;
        public static Vector3 targetPos;
        public static Vector3 position;
        public static Vector3 direction;
        static H3MP_TrackedItem trackedItem;
        public static int skip;

        static void Prefix(ref StingerLauncher __instance, AIEntity ___m_targetEntity)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;

            trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out H3MP_TrackedItem item) ? item : __instance.GetComponent<H3MP_TrackedItem>();

            if (___m_targetEntity != null)
            {
                targetPos = ___m_targetEntity.transform.position;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireStingerLauncherPatch), "GetPosition"))); // Call our GetPosition method

            // To get correct dir considering potential override
            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load gameObject
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireStingerLauncherPatch), "GetDirection"))); // Call our GetDirection method

            // To set missle ref in trackedItem
            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load StingerMissile
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireStingerLauncherPatch), "SetStingerMissile"))); // Call our SetStingerMissile method

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    continue;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_rotation"))
                {
                    instructionList.InsertRange(i + 1, toInsert1);
                    continue;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Fire"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    break;
                }
            }
            return instructionList;
        }

        public static void SetStingerMissile(StingerMissile missile)
        {
            if (trackedItem != null)
            {
                trackedItem.stingerMissile = missile;
                H3MP_TrackedItemReference reference = missile.gameObject.AddComponent<H3MP_TrackedItemReference>();
                reference.trackedItemRef = trackedItem;
            }
        }

        public static Vector3 GetPosition(Vector3 position)
        {
            if (overriden)
            {
                return FireStingerLauncherPatch.position;
            }
            else
            {
                FireStingerLauncherPatch.position = position;
                return position;
            }
        }

        public static Vector3 GetDirection(Vector3 direction, GameObject gameObject)
        {
            if (overriden)
            {
                gameObject.transform.rotation = Quaternion.LookRotation(FireStingerLauncherPatch.direction);
                return FireStingerLauncherPatch.direction;
            }
            else
            {
                FireStingerLauncherPatch.direction = direction;
                return direction;
            }
        }

        static void Postfix()
        {
            --Mod.skipAllInstantiates;

            overriden = false;

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
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.StingerLauncherFire(0, trackedItem.data.trackedID, targetPos, position, direction);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.StingerLauncherFire(trackedItem.data.trackedID, targetPos, position, direction);
                }
            }
        }

        static bool MissileFirePrefix(StingerMissile __instance)
        {
            if(Mod.managerObject != null && overriden)
            {
                __instance.Fire(targetPos, 12);
                return false;
            }

            return true;
        }
    }

    // Patches RemoteMissile.Detonante to keep track of the event and prevent it on non controlling clients
    class RemoteMissileDetonatePatch
    {
        public static bool overriden;

        static bool Prefix(RemoteMissile __instance, RemoteMissileLauncher ___m_launcher)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(___m_launcher, out trackedItem) ? trackedItem : ___m_launcher.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == H3MP_GameManager.ID)
                {
                    // Send to other clients
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.RemoteMissileDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        H3MP_ClientSend.RemoteMissileDetonate(trackedItem.data.trackedID, __instance.transform.position);
                    }
                }
                else
                {
                    // In the case in which we do not control the launcher, we do not want to detonate if it wasn't an order from the controller
                    if (overriden)
                    {
                        overriden = false;
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

    // Patches StingerMissile.Explode to keep track of the event and prevent it on non controlling clients
    class StingerMissileExplodePatch
    {
        public static bool overriden;

        static bool Prefix(StingerMissile __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItemReference>().trackedItemRef;
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == H3MP_GameManager.ID)
                {
                    // Send to other clients
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.StingerMissileExplode(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        H3MP_ClientSend.StingerMissileExplode(trackedItem.data.trackedID, __instance.transform.position);
                    }
                }
                else
                {
                    // In the case in which we do not control the launcher, we do not want to detonate if it wasn't an order from the controller
                    if (overriden)
                    {
                        overriden = false;
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

    // Patches SosigWeapon.Shatter so we can keep track of the event
    class SosigWeaponShatterPatch
    {
        public static int skip;

        static void Prefix(ref SosigWeapon __instance)
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
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? H3MP_GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                // Send the shatter action to other clients only if we control it
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        H3MP_ServerSend.SosigWeaponShatter(0, trackedItem.data.trackedID);
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_ClientSend.SosigWeaponShatter(trackedItem.data.trackedID);
                }
            }
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
            if (trackedSosig != null)
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
            if (trackedSosig != null)
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
            if (trackedSosig != null)
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
            if (trackedSosig != null)
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
                if (trackedSosig.data.trackedID == -1)
                {
                    H3MP_TrackedSosig.unknownBodyStates.Add(trackedSosig.data.localTrackedID, s);
                }
                else
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
            if (Mod.managerObject == null)
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
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
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
            if (sosigSetCurrentOrderSkip > 0)
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

            if (sosigVaporizeSkip > 0)
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

            for (int i = 0; i < __instance.Links.Count; ++i)
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
                for (int i = 0; i < __instance.S.Links.Count; ++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if (linkIndex == -1)
                {
                    Mod.LogError("RegisterWearablePrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if (knownWearableID == null)
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
                            Mod.LogError("SosigWearable: " + knownWearableID + " not found in map");
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
                for (int i = 0; i < __instance.S.Links.Count; ++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if (linkIndex == -1)
                {
                    Mod.LogError("RegisterWearablePrefix called on link whos sosig doesn't have the link");
                }
                else
                {
                    if (knownWearableID == null)
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
                            Mod.LogError("SosigWearable: " + knownWearableID + " not found in map");
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
                for (int i = 0; i < __instance.S.Links.Count; ++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if (linkIndex == -1)
                {
                    Mod.LogError("LinkExplodesPrefix called on link whos sosig doesn't have the link");
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
                for (int i = 0; i < __instance.S.Links.Count; ++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if (linkIndex == -1)
                {
                    Mod.LogError("LinkBreakPrefix called on link whos sosig doesn't have the link");
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
                for (int i = 0; i < __instance.S.Links.Count; ++i)
                {
                    if (__instance.S.Links[i] == __instance)
                    {
                        linkIndex = i;
                        break;
                    }
                }

                if (linkIndex == -1)
                {
                    Mod.LogError("LinkSeverPrefix called on link whos sosig doesn't have the link");
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
                if (trackedSosig.data.trackedID == -1)
                {
                    H3MP_TrackedSosig.unknownSetIFFs.Add(trackedSosig.data.localTrackedID, i);
                }
                else
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
                if (trackedSosig.data.trackedID == -1)
                {
                    H3MP_TrackedSosig.unknownSetOriginalIFFs.Add(trackedSosig.data.localTrackedID, i);
                }
                else
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
            if (__instance == null)
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
                    if (trackedAutoMeater.data.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0].IsFlameThrower)
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
                    if (trackedAutoMeater.data.trackedID != -1)
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
            if (angleOverride)
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
                        for (int i = 0; i < trackedAutoMeater.physicalAutoMeaterScript.FireControl.Firearms.Count; ++i)
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
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
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
                        if (trackedEncryption.data.trackedID != -1)
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
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
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
            if (skip > 0)
            {
                return;
            }

            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
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
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                H3MP_TrackedEncryption trackedEncryption = H3MP_GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? H3MP_GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<H3MP_TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.physicalEncryptionScript.UsesRecursiveSubTarg && trackedEncryption.data.controller == H3MP_GameManager.ID)
                {
                    if (trackedEncryption.data.controller == H3MP_GameManager.ID)
                    {
                        List<int> indices = new List<int>();
                        for (int i = 0; i < trackedEncryption.physicalEncryptionScript.SubTargs.Count; i++)
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
            if (skip > 0)
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

    // Patches LAPD2019 to sync actions
    class LAPD2019ActionPatch
    {
        public static int loadBatterySkip;
        public static int extractBatterySkip;

        static void LoadBatteryPrefix(ref LAPD2019 __instance, LAPD2019Battery battery)
        {
            if (loadBatterySkip > 0)
            {
                return;
            }

            if (Mod.managerObject != null)
            {
                H3MP_TrackedItem trackedGun = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
                H3MP_TrackedItem trackedBattery = H3MP_GameManager.trackedItemByItem.ContainsKey(battery) ? H3MP_GameManager.trackedItemByItem[battery] : battery.GetComponent<H3MP_TrackedItem>();
                if (trackedGun != null && trackedBattery != null)
                {
                    if (trackedGun.data.controller != H3MP_GameManager.ID)
                    {
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.LAPD2019LoadBattery(0, trackedGun.data.trackedID, trackedGun.data.trackedID);
                        }
                        else
                        {
                            H3MP_ClientSend.LAPD2019LoadBattery(trackedGun.data.trackedID, trackedGun.data.trackedID);
                        }
                    }
                }
            }
        }

        static void ExtractBatteryPrefix(ref LAPD2019 __instance)
        {
            if (extractBatterySkip > 0)
            {
                return;
            }

            if (Mod.managerObject != null)
            {
                H3MP_TrackedItem trackedGun = H3MP_GameManager.trackedItemByItem.ContainsKey(__instance) ? H3MP_GameManager.trackedItemByItem[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
                if (trackedGun != null)
                {
                    if (trackedGun.data.controller != H3MP_GameManager.ID)
                    {
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.LAPD2019ExtractBattery(0, trackedGun.data.trackedID);
                        }
                        else
                        {
                            H3MP_ClientSend.LAPD2019ExtractBattery(trackedGun.data.trackedID);
                        }
                    }
                }
            }
        }
    }

    // Patches SosigTargetPrioritySystem methods to keep track of changes to IFFChart
    class SosigTargetPrioritySystemPatch
    {
        public static int BoolArrToInt(bool[] arr)
        {
            int i = 0;
            for (int index = 0; index < arr.Length; ++index)
            {
                if (arr[index])
                {
                    i |= (1 << index);
                }
            }
            return i;
        }

        public static bool[] IntToBoolArr(int i)
        {
            bool[] arr = new bool[32];
            for (int index = arr.Length - 1; index >= 0; --index)
            {
                arr[index] = ((i >> index) | 1) == 1;
            }
            return arr;
        }

        static void Postfix(ref SosigTargetPrioritySystem __instance, ref AIEntity ___E)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedSosig trackedSosig = ___E.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SosigPriorityIFFChart(0, trackedSosig.data.trackedID, BoolArrToInt(__instance.IFFChart));
                }
                else if (trackedSosig.data.trackedID != -1)
                {
                    H3MP_ClientSend.SosigPriorityIFFChart(trackedSosig.data.trackedID, BoolArrToInt(__instance.IFFChart));
                }
                else // Unknown tracked ID, keep for late update
                {
                    if (H3MP_TrackedSosig.unknownIFFChart.ContainsKey(trackedSosig.data.localTrackedID))
                    {
                        H3MP_TrackedSosig.unknownIFFChart[trackedSosig.data.localTrackedID] = BoolArrToInt(__instance.IFFChart);
                    }
                    else
                    {
                        H3MP_TrackedSosig.unknownIFFChart.Add(trackedSosig.data.localTrackedID, BoolArrToInt(__instance.IFFChart));
                    }
                }
            }
        }
    }

    // Patches SimpleLauncher2.CycleMode to prevent it from going into DR mode if not in control
    class SimpleLauncher2CycleModePatch
    {
        static bool Prefix(SimpleLauncher2 __instance)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != H3MP_GameManager.ID)
            {
                if (__instance.Mode == SimpleLauncher2.fMode.sa)
                {
                    __instance.Mode = SimpleLauncher2.fMode.tr;
                }
                else if (__instance.Mode == SimpleLauncher2.fMode.tr)
                {
                    __instance.Mode = SimpleLauncher2.fMode.sa;
                }
                else if (__instance.Mode == SimpleLauncher2.fMode.dr)
                {
                    __instance.Mode = SimpleLauncher2.fMode.sa;
                }
                __instance.SetAnimatedComponent(__instance.ModeSwitch, __instance.ModeVars[(int)__instance.Mode], __instance.ModeSwitch_Interp, __instance.ModeSwitch_Axis);
                __instance.PlayAudioEvent(FirearmAudioEventType.FireSelector, 1f);

                return false;
            }

            return true;
        }
    }

    // Patches PinnedGrenade to sync
    class PinnedGrenadePatch
    {
        // This patch is quite complex because pinned grenades work entirely through updates
        // Actions like exploding the grenade are not put into a single method we can patch
        // The state of the grenade must be synced through the item update packets
        // The main problem is that once our grenade is up to date, if the pin is removed, locally the grenade
        // may be held by a remote player but logically physicalObject.IsHeld will still be false
        // causing the local grenade to release its lever and countdown towards explosion while this is not the case 
        // on the grenade's controller's side. This is only one of a few/many such desync problems caused by this
        // update structured behavior
        // The solution to this is to not let update happen to begin with if we are not in control of the grenade
        // The next problem is how to check if we are in control of the grenade EVERY frame efficiently
        // The obvious inefficient solution would be to find the item's trackedItem in trackedItemByItem dict every frame, but this would take too long
        // We want to access our trackedItem in O(1)

        // My solution is hijacking a variable of the PinnedGrenade, in this case its SpawnOnSplode list, to somehow reference the trackedItem
        // To do this, when we track a PinnedGrenade, I add a new GameObject to the SpawnOnSplode list
        // This GameObject has its HideFlags set to HideAndDontSave + an index
        // This index is the index of the tracked item in the trackedItemReferences static array if TrackedItem
        // So to know if our PinnedGrenade is under our control, we get the last gameObject in the SpawnOnSplode list
        // We get tis hideflags, if it is > HideAndDontSave, we get index = hideflag - HideAndDontSave, which we then use to get our TrackedItem
        // We can then check trackedItem.Controller
        // Note: We also now need to prevent the PinnedGrenade from actually spawning the last item in SpawnOnSplode

        static bool exploded;

        // To prevent FVR(Fixed)Update from happening
        static bool UpdatePrefix(PinnedGrenade __instance, bool ___m_hasSploded)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            exploded = ___m_hasSploded;

            if (__instance.SpawnOnSplode != null && __instance.SpawnOnSplode.Count > 0)
            {
                int index = (int)__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].hideFlags - (int)HideFlags.HideAndDontSave;

                // Return true (run original), if dont have an index, index doesn't fit in references (shouldn't happen?), reference null (shouldn't happen), or we control
                return index <= 0 || 
                       H3MP_TrackedItem.trackedItemReferences.Length <= index || 
                       H3MP_TrackedItem.trackedItemReferences[index] == null || 
                       H3MP_TrackedItem.trackedItemReferences[index].data.controller == H3MP_GameManager.ID;
            }

            return true;
        }

        // To prevent spawning of our added element to SpawnOnSplode
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 5)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load index j
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PinnedGrenade), "SpawnOnSplode"))); // Load SpawnOnSplode
            toInsert0.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<GameObject>), "get_Count"))); // Get count
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_1)); // Load 1
            toInsert0.Add(new CodeInstruction(OpCodes.Sub)); // Sub. 1 from count
            Label lastIndexLabel = il.DefineLabel();
            toInsert0.Add(new CodeInstruction(OpCodes.Beq, lastIndexLabel)); // If last index, break to label lastIndexLabel

            Label loopStartLabel = il.DefineLabel();
            CodeInstruction notLastIndexInstruction = new CodeInstruction(OpCodes.Br, loopStartLabel);
            toInsert0.Add(notLastIndexInstruction); // If not last index, break to begin loop as usual

            CodeInstruction controlCheckInstanceLoad = new CodeInstruction(OpCodes.Ldarg_0);
            controlCheckInstanceLoad.labels.Add(lastIndexLabel);
            toInsert0.Add(controlCheckInstanceLoad); // Load PinnedGrenade instance (lastIndexLabel)
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PinnedGrenadePatch), "SkipLast"))); // Call our SkipLast method
            Label skipLabel = il.DefineLabel();
            toInsert0.Add(new CodeInstruction(OpCodes.Brtrue, skipLabel)); // If skip last, break to label controlledLabel

            toInsert0.Add(notLastIndexInstruction); // If not skip last index, break to begin loop as usual

            CodeInstruction skipLastLoadIndex = new CodeInstruction(OpCodes.Ldloc_S, 4);
            skipLastLoadIndex.labels.Add(skipLabel);
            toInsert0.Add(skipLastLoadIndex); // Load index j (controlledLabel)
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_1)); // Load 1
            toInsert0.Add(new CodeInstruction(OpCodes.Add)); // Add 1 to j
            toInsert0.Add(new CodeInstruction(OpCodes.Stloc_S, 4)); // Set index j
            CodeInstruction breakToLoopHead = new CodeInstruction(OpCodes.Br);
            toInsert0.Add(breakToLoopHead); // Break to loop head, where we will check index j against SpawnOnSplode.Count and break out of loop

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (!applied && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("SpawnOnSplode"))
                {
                    breakToLoopHead.operand = instructionList[i - 2].operand;
                    instructionList[i - 1].labels.Add(loopStartLabel);
                    instructionList.InsertRange(i - 1, toInsert0);
                    applied = true;
                }

                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    break;
                }
            }
            return instructionList;
        }

        public static bool SkipLast(PinnedGrenade grenade)
        {
            if(Mod.managerObject == null)
            {
                return false;
            }

            return grenade.SpawnOnSplode != null && grenade.SpawnOnSplode.Count > 0 && grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1].hideFlags > HideFlags.HideAndDontSave;
        }

        // To know if grenade exploded in latest update
        static void UpdatePostfix(PinnedGrenade __instance, bool ___m_hasSploded)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            if(!exploded && ___m_hasSploded)
            {
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
                if(trackedItem != null && trackedItem.data.controller == H3MP_GameManager.ID)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.PinnedGrenadeExplode(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        H3MP_ClientSend.PinnedGrenadeExplode(trackedItem.data.trackedID, __instance.transform.position);
                    }
                }
            }
        }

        // To prevent collision explosion if not in control
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load explosion gameobject
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load PinnedGrenade instance
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PinnedGrenadePatch), "GrenadeControlled"))); // Call our GrenadeControlled method
            Label l = il.DefineLabel();
            toInsert0.Add(new CodeInstruction(OpCodes.Brtrue, l)); // If controlled, break to continue as usual

            toInsert0.Add(new CodeInstruction(OpCodes.Ret)); // If not controlled return right away

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("OnCollisionEnter"))
                {
                    instructionList[i + 1].labels.Add(l);
                    instructionList.InsertRange(i + 1, toInsert0);
                }

                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    break;
                }
            }
            return instructionList;
        }

        public static bool GrenadeControlled(PinnedGrenade grenade)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (grenade.SpawnOnSplode != null && grenade.SpawnOnSplode.Count > 0)
            {
                int index = (int)grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1].hideFlags - (int)HideFlags.HideAndDontSave;

                // Return true (controlled), if have an index, index fits in references (should always be true?), reference not null (should always be true), and we control
                return index > 0 &&
                       H3MP_TrackedItem.trackedItemReferences.Length > index &&
                       H3MP_TrackedItem.trackedItemReferences[index] != null &&
                       H3MP_TrackedItem.trackedItemReferences[index].data.controller == H3MP_GameManager.ID;
            }

            return true;
        }

        public static void ExplodePinnedGrenade(PinnedGrenade grenade, Vector3 pos)
        {
            Mod.PinnedGrenade_m_hasSploded.SetValue(grenade, true);
            for (int i = 0; i < grenade.SpawnOnSplode.Count; i++)
            {
                if(i == grenade.SpawnOnSplode.Count - 1 && grenade.SpawnOnSplode[i].hideFlags > HideFlags.HideAndDontSave)
                {
                    break;
                }

                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(grenade.SpawnOnSplode[i], pos, Quaternion.identity);
                Explosion component = gameObject.GetComponent<Explosion>();
                if (component != null)
                {
                    component.IFF = grenade.IFF;
                }
                ExplosionSound component2 = gameObject.GetComponent<ExplosionSound>();
                if (component2 != null)
                {
                    component2.IFF = grenade.IFF;
                }
                GrenadeExplosion component3 = gameObject.GetComponent<GrenadeExplosion>();
                if (component3 != null)
                {
                    component3.IFF = grenade.IFF;
                }
            }
            if (grenade.SmokeEmitter != null)
            {
                grenade.SmokeEmitter.Engaged = true;
            }
            else
            {
                if (grenade.IsHeld)
                {
                    grenade.m_hand.ForceSetInteractable(null);
                    grenade.EndInteraction(grenade.m_hand);
                }
                UnityEngine.Object.Destroy(grenade.gameObject);
            }
        }
    }

    // Patches FVRGrenade to sync
    class FVRGrenadePatch
    {
        static bool exploded;

        // To prevent FVRUpdate from happening if not in control
        static bool UpdatePrefix(FVRGrenade __instance, bool ___m_hasSploded, Dictionary<int, float> ___FuseTimings)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            exploded = ___m_hasSploded;

            if (___FuseTimings != null && ___FuseTimings.TryGetValue(-1, out float indexFloat))
            {
                // Return true (run original), if dont have an index, index doesn't fit in references (shouldn't happen?), reference null (shouldn't happen), or we control
                int index = (int)indexFloat;
                return index <= 0 || 
                       H3MP_TrackedItem.trackedItemReferences.Length <= index || 
                       H3MP_TrackedItem.trackedItemReferences[index] == null || 
                       H3MP_TrackedItem.trackedItemReferences[index].data.controller == H3MP_GameManager.ID;
            }

            return true;
        }

        // To know if grenade exploded in latest update
        static void UpdatePostfix(FVRGrenade __instance, bool ___m_hasSploded)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            if(!exploded && ___m_hasSploded)
            {
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
                if(trackedItem != null && trackedItem.data.controller == H3MP_GameManager.ID)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.FVRGrenadeExplode(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        H3MP_ClientSend.FVRGrenadeExplode(trackedItem.data.trackedID, __instance.transform.position);
                    }
                }
            }
        }

        public static void ExplodeGrenade(FVRGrenade grenade, Vector3 pos)
        {
            Mod.FVRGrenade_m_hasSploded.SetValue(grenade, true);
            if (grenade.ExplosionFX != null)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(grenade.ExplosionFX, grenade.transform.position, Quaternion.identity);
                Explosion component = gameObject.GetComponent<Explosion>();
                if (component != null)
                {
                    component.IFF = grenade.IFF;
                }
                ExplosionSound component2 = gameObject.GetComponent<ExplosionSound>();
                if (component2 != null)
                {
                    component2.IFF = grenade.IFF;
                }
            }
            if (grenade.ExplosionSoundFX != null)
            {
                GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(grenade.ExplosionSoundFX, grenade.transform.position, Quaternion.identity);
                Explosion component3 = gameObject2.GetComponent<Explosion>();
                if (component3 != null)
                {
                    component3.IFF = grenade.IFF;
                }
                ExplosionSound component4 = gameObject2.GetComponent<ExplosionSound>();
                if (component4 != null)
                {
                    component4.IFF = grenade.IFF;
                }
            }
            if (grenade.SmokeEmitter != null)
            {
                grenade.SmokeEmitter.Engaged = true;
            }
            else
            {
                if (grenade.IsHeld)
                {
                    grenade.m_hand.ForceSetInteractable(null);
                    grenade.EndInteraction(grenade.m_hand);
                }
                UnityEngine.Object.Destroy(grenade.gameObject);
            }
        }
    }

    // Patches BangSnap to send explosion and prevent collision on non controllers
    class BangSnapPatch
    {
        public static int skip;

        // To send explosion
        static void SplodePrefix(BangSnap __instance)
        {
            if(skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.BangSnapSplode(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    H3MP_ClientSend.BangSnapSplode(trackedItem.data.trackedID, __instance.transform.position);
                }
            }
        }

        // To prevent collision explosion if not in control
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load BangSnap instance
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BangSnapPatch), "Controlled"))); // Call our Controlled method
            Label l = il.DefineLabel();
            toInsert0.Add(new CodeInstruction(OpCodes.Brtrue, l)); // If controlled, break to continue as usual

            toInsert0.Add(new CodeInstruction(OpCodes.Ret)); // If not controlled return right away

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Brtrue)
                {
                    instructionList[i + 1].labels.Add(l);
                    instructionList.InsertRange(i + 1, toInsert0);
                    break;
                }
            }
            return instructionList;
        }

        public static bool Controlled(BangSnap bangSnap)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(bangSnap, out trackedItem) ? trackedItem : bangSnap.GetComponent<H3MP_TrackedItem>();
            if(trackedItem != null)
            {
                return trackedItem.data.controller == H3MP_GameManager.ID;
            }

            return true;
        }
    }

    // Patches C4.Detonate to track detonation
    class C4DetonatePatch
    {
        public static int skip;

        static void Prefix(C4 __instance)
        {
            if(skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.C4Detonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    H3MP_ClientSend.C4Detonate(trackedItem.data.trackedID, __instance.transform.position);
                }
            }
        }
    }

    // Patches ClaymoreMine.Detonate to track detonation
    class ClaymoreMineDetonatePatch
    {
        public static int skip;

        static void Prefix(ClaymoreMine __instance)
        {
            if(skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.ClaymoreMineDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    H3MP_ClientSend.ClaymoreMineDetonate(trackedItem.data.trackedID, __instance.transform.position);
                }
            }
        }
    }

    // Patches SLAM.Detonate to track detonation
    class SLAMDetonatePatch
    {
        public static int skip;

        static void Prefix(ClaymoreMine __instance)
        {
            if(skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.SLAMDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    H3MP_ClientSend.SLAMDetonate(trackedItem.data.trackedID, __instance.transform.position);
                }
            }
        }
    }
#endregion

#region Instantiation Patches
    // Patches FVRFireArmChamber.EjectRound so we can keep track of when a round is ejected from a chamber
    class ChamberEjectRoundPatch
    {
        static bool track = false;
        static int incrementedSkip = 0;

        static void Prefix(ref FVRFireArmChamber __instance, ref FVRFireArmRound ___m_round, bool ForceCaseLessEject)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
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
                    // TODO: Optimization: Maybe have a list trackedItemByChamber, in which we keep any item which have a chamber, which we would put in there in trackedItem awake
                    //       Because right now we just go up the hierarchy until we find the item, maybe its faster? will need to test, but considering the GetComponent overhead
                    //       we might want to do this differently
                    Transform currentParent = __instance.transform;
                    H3MP_TrackedItem trackedItem = null;
                    while (currentParent != null)
                    {
                        trackedItem = currentParent.GetComponent<H3MP_TrackedItem>();
                        if (trackedItem != null)
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
            if (incrementedSkip > 0)
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
                    if (obj == null)
                    {
                        Mod.LogWarning("SpawnVaultFileRoutinePatch.FinishedRoutine object to be destroyed already null");
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

    // Patches FVRPhysicalObject.set_IDSpawnedFrom in case it makes the item identifiable
    class IDSpawnedFromPatch
    {
        static void Postfix(ref FVRPhysicalObject __instance)
        {
            // Skip if not connected
            if (__instance.IDSpawnedFrom == null || Mod.managerObject == null)
            {
                return;
            }

            // Try syncing
            H3MP_GameManager.SyncTrackedItems(__instance.transform, true, null, SceneManager.GetActiveScene().name);
        }
    }

    // Patches AnvilPrefabSpawn.InstantiateAndZero so we know when we spawn items from an anvil prefab spawn
    class AnvilPrefabSpawnPatch
    {
        static bool Prefix(AnvilPrefabSpawn __instance, GameObject result)
        {
            // Skip if not connected or no one else in the scene/instance
            if (Mod.managerObject == null || !H3MP_GameManager.PlayersPresentSlow())
            {
                return true;
            }

            // If the item is marked as being there when we arrived in the scene
            // And if it can be tracked
            // Then we don't even want to instantiate it because it will already have been instantiated and tracked in this scene/instance  
            if (__instance.GetComponent<H3MP_TrackedItemReference>() != null)
            {
                FVRPhysicalObject physObj = result.GetComponent<FVRPhysicalObject>();
                if(physObj != null && H3MP_GameManager.IsObjectIdentifiable(physObj))
                {
                    Mod.LogInfo("Skipping AnvilPrefabSpawn instantiate on " + __instance.name);
                    return false;
                }
            }

            return true;
        }
    }
    #endregion

    #region Damageable Patches
    // TODO: Optimization?: Patch IFVRDamageable.Damage and have a way to track damageables so we don't need to have a specific TCP call for each
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

            if (flag2)
            {
                if (tempFA == null)
                {
                    // If we don't have a ref to the firearm that fired this projectile, let the damage be controlled by the best host
                    int bestHost = Mod.GetBestPotentialObjectHost(-1);
                    return bestHost == -1 || bestHost == H3MP_GameManager.ID;
                }
                else // We have a ref to the firearm that fired this projectile
                {
                    // We only want to let this projectile do damage if we control the firearm
                    H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.ContainsKey(tempFA) ? H3MP_GameManager.trackedItemByItem[tempFA] : tempFA.GetComponent<H3MP_TrackedItem>();
                    if (trackedItem == null)
                    {
                        return false;
                    }
                    else
                    {
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
        public static void AddControllerReference(GameObject dest, Component src = null)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || H3MP_GameManager.playersPresent == 0)
            {
                return;
            }

            GameObject srcToUse = src == null ? dest : src.gameObject;
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
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 19)); // Set damageable

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (19)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);

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

                    instructionList.InsertRange(i + 1, toInsert);

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
                    instructionList.InsertRange(i + 1, toInsert);

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
                        // TODO: Optimization: Keep a dictionary of players using the scene as key
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

                    instructionList.InsertRange(i + 1, toInsert);

                    skipNext3 = true;
                }
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRReceiveDamageable (4)"))
                {
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

                    instructionList.InsertRange(i + 1, toInsert);

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

                    instructionList.InsertRange(i + 1, toInsert);

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
                    instructionList.InsertRange(i + 1, toInsert);

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
                    instructionList.InsertRange(i + 1, toInsert);

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

                    instructionList.InsertRange(i + 1, toInsert);

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

                    instructionList.InsertRange(i + 1, toInsert);

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
                    instructionList.InsertRange(i + 1, toInsert);

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

                    instructionList.InsertRange(i + 1, toInsert);

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
            if (skip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Sosig could have been destroyed by the damage, we can just skip because the destroy order will be sent to other clients
            if (__instance == null)
            {
                return true;
            }

            // If in control of the damaged sosig link, we want to process the damage
            trackedSosig = H3MP_GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? H3MP_GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<H3MP_TrackedSosig>();
            if (trackedSosig != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedSosig.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        for (int i = 0; i < __instance.S.Links.Count; ++i)
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
                else if (trackedSosig.data.controller == H3MP_Client.singleton.ID)
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
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
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
                    if (trackedSosig.data.controller == 0)
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
                else if (trackedSosig.data.controller == H3MP_Client.singleton.ID)
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

            // If in control of the damaged crate, we want to process the damage
            trackedItem = __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it
                        H3MP_ServerSend.ShatterableCrateDamage(trackedItem.data.trackedID, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
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

        // TODO: Future: Currently no data is necessary to sync after damage, need review
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

    // Patches SosigWeapon.Damage to keep track of damage taken by a SosigWeapon
    class SosigWeaponDamagePatch
    {
        public static int skip;

        static bool Prefix(ref SosigWeapon __instance, Damage d)
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

            // If in control of the damaged SosigWeapon, we want to process the damage
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? H3MP_GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        H3MP_ServerSend.SosigWeaponDamage(trackedItem.data, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.SosigWeaponDamage(trackedItem.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }
    }

    // Patches RemoteMissile.Damage to keep track of damage taken by a remote missile
    class RemoteMissileDamagePatch
    {
        public static int skip;

        static bool Prefix(RemoteMissileLauncher ___m_launcher, Damage d)
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

            // If in control of the damaged RemoteMissile, we want to process the damage
            H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(___m_launcher, out trackedItem) ? trackedItem : ___m_launcher.GetComponent<H3MP_TrackedItem>();
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        H3MP_ServerSend.RemoteMissileDamage(trackedItem.data, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.RemoteMissileDamage(trackedItem.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }
    }

    // Patches StingerMissile.Damage to keep track of damage taken by a stinger missile
    class StingerMissileDamagePatch
    {
        public static int skip;

        static bool Prefix(StingerMissile __instance, Damage d)
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

            // If in control of the damaged StingerMissile, we want to process the damage
            H3MP_TrackedItem trackedItem = __instance.GetComponent<H3MP_TrackedItemReference>().trackedItemRef;
            if (trackedItem != null)
            {
                if (H3MP_ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        H3MP_ServerSend.StingerMissileDamage(trackedItem.data, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == H3MP_Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    H3MP_ClientSend.StingerMissileDamage(trackedItem.data.trackedID, d);
                    return false;
                }
            }
            return true;
        }
    }

    // Patches UberShatterable.Shatter to keep track of shatter event
    class UberShatterableShatterPatch
    {
        public static int skip;

        static void Prefix(ref UberShatterable __instance, Vector3 point, Vector3 dir, float intensity)
        {
            if(skip > 0 || Mod.managerObject != null)
            {
                return;
            }

            if(__instance.O != null)
            {
                H3MP_TrackedItem trackedItem = H3MP_GameManager.trackedItemByItem.TryGetValue(__instance.O, out H3MP_TrackedItem item) ? item : __instance.O.GetComponent<H3MP_TrackedItem>();
                if(trackedItem != null)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.UberShatterableShatter(trackedItem.data.trackedID, point, dir, intensity);
                    }
                    else
                    {
                        H3MP_ClientSend.UberShatterableShatter(trackedItem.data.trackedID, point, dir, intensity);
                    }
                }
            }
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

                        // Reset TNH_ManagerPatch data
                        TNH_ManagerPatch.patrolIndex = -1;

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
                                H3MP_ServerSend.TNHData(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.manager);
                            }
                            else
                            {
                                H3MP_ClientSend.SetTNHController(Mod.currentTNHInstance.instance, nextID);
                                H3MP_ClientSend.TNHData(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.manager);
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
        public static int completeTokenSkip;
        public static int sosigKillSkip;
        public static bool doInit;

        public static bool inGenerateSentryPatrol;
        public static bool inGeneratePatrol;
        public static List<Vector3> patrolPoints;
        public static int patrolIndex = -1;

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
            // To be incremented on CompleteHold
            // So that the TNH instance non-controller will not add their own token, they will wait until
            // the controller sends them a TNHAddTokens
            if (completeTokenSkip > 0)
            {
                return false;
            }

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
                    if (trackedSosig.data.trackedID == -1)
                    {
                        H3MP_TrackedSosig.unknownTNHKills.Add(trackedSosig.data.localTrackedID, Mod.currentTNHInstance.instance);
                    }
                    else
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

                    // Could be null if we receive set level from controller before we set it in delayed init
                    // If it is null here though, the progression setting will be handled when we receive TNH data from controller
                    if (progression != null)
                    { 
                        if (Mod.currentTNHInstance.manager.ProgressionMode == TNHSetting_ProgressionType.FiveHold || level < progression.Levels.Count)
                        {
                            Mod.TNH_Manager_m_curLevel.SetValue(Mod.currentTNHInstance.manager, progression.Levels[Mod.currentTNHInstance.level]);
                        }
                        else
                        {
                            TNH_Progression endlessProgression = (TNH_Progression)Mod.TNH_Manager_m_curProgressionEndless.GetValue(Mod.currentTNHInstance.manager);
                            Mod.TNH_Manager_m_curLevel.SetValue(Mod.currentTNHInstance.manager, endlessProgression.Levels[UnityEngine.Random.Range(0, endlessProgression.Levels.Count)]);
                        }
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
                    if (Mod.currentTNHInstance.manager.RadarHand == TNH_RadarHand.Right)
                    {
                        Mod.currentTNHInstance.manager.TAHReticle.transform.position = GM.CurrentPlayerBody.RightHand.position + GM.CurrentPlayerBody.RightHand.forward * -0.2f;
                    }
                    else
                    {
                        Mod.currentTNHInstance.manager.TAHReticle.transform.position = GM.CurrentPlayerBody.LeftHand.position + GM.CurrentPlayerBody.LeftHand.forward * -0.2f;
                    }

                    if (doInit && Mod.currentTNHInstance.manager.AIManager.HasInit)
                    {
                        Mod.LogInfo("\t\t\tdoing TNH init");
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
            TNH_HoldPoint curHoldPoint = Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex];
            Mod.TNH_Manager_m_curHoldPoint.SetValue(Mod.currentTNHInstance.manager, curHoldPoint);
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
                if (Mod.currentTNHInstance.raisedBarriers != null)
                {
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

                TNH_HoldPointSystemNode sysNode = (TNH_HoldPointSystemNode)Mod.TNH_HoldPoint_m_systemNode.GetValue(curHoldPoint);
                switch (Mod.currentTNHInstance.holdState)
                {
                    case TNH_HoldPoint.HoldState.Analyzing:
                        Mod.TNH_HoldPoint_BeginAnalyzing.Invoke(curHoldPoint, null);
                        for (int i = 0; i < Mod.currentTNHInstance.warpInData.Count; i += 2)
                        {
                            List<GameObject> warpInTargets = (List<GameObject>)Mod.TNH_HoldPoint_m_warpInTargets.GetValue(curHoldPoint);
                            warpInTargets.Add(UnityEngine.Object.Instantiate<GameObject>(curHoldPoint.M.Prefab_TargetWarpingIn, Mod.currentTNHInstance.warpInData[i], Quaternion.Euler(Mod.currentTNHInstance.warpInData[i + 1])));
                        }
                        break;
                    case TNH_HoldPoint.HoldState.Transition:
                        SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, curHoldPoint.AUDEvent_HoldWave, curHoldPoint.transform.position);
                        UnityEngine.Object.Instantiate<GameObject>(curHoldPoint.VFX_HoldWave, sysNode.NodeCenter.position, sysNode.NodeCenter.rotation);
                        curHoldPoint.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
                        Mod.TNH_HoldPoint_m_state.SetValue(curHoldPoint, TNH_HoldPoint.HoldState.Transition);
                        Mod.TNH_HoldPoint_LowerAllBarriers.Invoke(curHoldPoint, null);
                        sysNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
                        break;
                    case TNH_HoldPoint.HoldState.Hacking:
                        curHoldPoint.M.EnqueueEncryptionLine(TNH_EncryptionType.Static);
                        Mod.TNH_HoldPoint_m_state.SetValue(curHoldPoint, TNH_HoldPoint.HoldState.Hacking);
                        sysNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);
                        break;
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
                // We will already have been TPed to our char's starting point by delayed init
                // Now check if valid, if not find first player to spawn on
                if (Mod.currentTNHInstance.activeSupplyPointIndices != null)
                {
                    if (Mod.currentTNHInstance.activeSupplyPointIndices.Contains(((TNH_PointSequence)Mod.TNH_Manager_m_curPointSequence.GetValue(Mod.currentTNHInstance.manager)).StartSupplyPointIndex))
                    {
                        // Starting point invalid, find a player to spawn on
                        if(Mod.currentTNHInstance.currentlyPlaying != null && Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                        {
                            Mod.TNHSpawnPoint = H3MP_GameManager.players[Mod.currentTNHInstance.currentlyPlaying[0]].transform.position;
                            GM.CurrentMovementManager.TeleportToPoint(H3MP_GameManager.players[Mod.currentTNHInstance.currentlyPlaying[0]].transform.position, true);
                        }
                        else
                        {
                            Mod.TNHSpawnPoint = GM.CurrentPlayerBody.transform.position;
                            Mod.LogWarning("Not valid supply point or player to spawn on, spawning on default start point, which might be active");
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

        static void GenerateSentryPatrolPrefix(List<Vector3> PatrolPoints)
        {
            inGenerateSentryPatrol = true;
            patrolIndex++;
            patrolPoints = PatrolPoints;
        }

        static void GenerateSentryPatrolPostfix()
        {
            inGenerateSentryPatrol = false;
        }

        static void GeneratePatrolPrefix(TNH_Manager __instance)
        {
            if(Mod.managerObject != null)
            {
                inGeneratePatrol = true;
                patrolIndex++;
                List<int> list = new List<int>();
                int i = 0;
                int num = 0;
                while (i < 5)
                {
                    int item = UnityEngine.Random.Range(0, __instance.HoldPoints.Count);
                    if (!list.Contains(item))
                    {
                        list.Add(item);
                        i++;
                    }
                    num++;
                    if (num > 200)
                    {
                        break;
                    }
                }
                patrolPoints = new List<Vector3>();
                for (int j = 0; j < list.Count; j++)
                {
                    patrolPoints.Add(__instance.HoldPoints[list[j]].SpawnPoints_Sosigs_Defense[UnityEngine.Random.Range(0, __instance.HoldPoints[list[j]].SpawnPoints_Sosigs_Defense.Count)].position);
                }
            }
        }

        static void GeneratePatrolPostfix()
        {
            inGeneratePatrol = false;
        }
    }

    // Patches TNH_HoldPoint to keep track of hold point events
    public class TNH_HoldPointPatch
    {
        public static bool spawnEntitiesSkip;
        public static int beginHoldSkip;
        public static int beginPhaseSkip;

        public static bool inSpawnEnemyGroup;
        public static bool inSpawnTurrets;

        static bool UpdatePrefix(ref TNH_HoldPoint __instance, bool ___m_isInHold, ref TNH_HoldPointSystemNode ___m_systemNode, ref bool ___m_hasPlayedTimeWarning1, ref bool ___m_hasPlayedTimeWarning2,
                                 ref int ___m_numWarnings)
        {
            // Skip if connected, have TNH instance, and we are not controller
            if(Mod.managerObject != null && ___m_isInHold && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                switch (Mod.currentTNHInstance.holdState)
                {
                    case TNH_HoldPoint.HoldState.Beginning:
                        ___m_systemNode.SetDisplayString("SCANNING SYSTEM");
                        break;
                    case TNH_HoldPoint.HoldState.Analyzing:
                        Mod.currentTNHInstance.tickDownToID -= Time.deltaTime;
                        if (__instance.M.TargetMode == TNHSetting_TargetMode.NoTargets)
                        {
                            ___m_systemNode.SetDisplayString("ANALYZING " + __instance.FloatToTime(Mod.currentTNHInstance.tickDownToID, "0:00.00"));
                        }
                        else
                        {
                            ___m_systemNode.SetDisplayString("ANALYZING");
                        }
                        break;
                    case TNH_HoldPoint.HoldState.Hacking:
                        Mod.currentTNHInstance.tickDownToFailure -= Time.deltaTime;
                        if (!___m_hasPlayedTimeWarning1 && Mod.currentTNHInstance.tickDownToFailure < 60f)
                        {
                            ___m_hasPlayedTimeWarning1 = true;
                            __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Reminder1);
                            __instance.M.Increment(1, false);
                        }
                        if (!___m_hasPlayedTimeWarning2 && Mod.currentTNHInstance.tickDownToFailure < 30f)
                        {
                            ___m_hasPlayedTimeWarning2 = true;
                            __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Reminder2);
                            ___m_numWarnings++;
                            __instance.M.Increment(1, false);
                        }
                        ___m_systemNode.SetDisplayString("FAILURE IN: " + __instance.FloatToTime(Mod.currentTNHInstance.tickDownToFailure, "0:00.00"));
                        break;
                    case TNH_HoldPoint.HoldState.Transition:
                        if(___m_systemNode != null)
                        {
                            ___m_systemNode.SetDisplayString("SCANNING SYSTEM");
                        }
                        break;
                }

                return false;
            }
            return true;
        }

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
                            Mod.LogError("Holdpoint to be set as sytem node missing from manager");
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

        static bool BeginHoldPrefix()
        {
            if(beginHoldSkip > 0)
            {
                return true;
            }

            ++beginPhaseSkip;

            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    // Update locally
                    Mod.currentTNHInstance.holdOngoing = true;
                    Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, true, true, 0);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, true);
                    }
                }
                else
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, false, false, Mod.currentTNHInstance.controller);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, false);
                    }

                    return false;
                }
            }

            return true;
        }

        static void BeginHoldPostfix()
        {
            if(beginHoldSkip > 0)
            {
                return;
            }

            --beginPhaseSkip;
        }

        static bool RaiseRandomBarriersPrefix(int howMany)
        {
            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            // It will also prevent raising barriers if we are not the controller of the instance
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
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

        static void RaiseRandomBarriersPostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldPointRaiseBarriers(0, Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldPointRaiseBarriers(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
                    }
                }
            }
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
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Transition;

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

        static bool SpawnTargetGroupPrefix()
        {
            // Skip if connected, have TNH instance, and we are not controller
            if(Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                Mod.TNH_HoldPoint_DeleteAllActiveWarpIns.Invoke(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex], null);
                return false;
            }
            return true;
        }

        static void IdentifyEncryptionPostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Hacking;

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHHoldIdentifyEncryption(0, Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHHoldIdentifyEncryption(Mod.currentTNHInstance.instance);
                }
            }
        }

        static bool SpawnWarpInMarkersPrefix()
        {
            // Skip if connected, have TNH instance, and we are not controller
            return Mod.managerObject == null || Mod.currentTNHInstance == null || Mod.currentTNHInstance.controller == H3MP_GameManager.ID;
        }

        static void BeginAnalyzingPostfix(ref TNH_HoldPoint __instance, ref List<GameObject> ___m_warpInTargets, float ___m_tickDownToIdentification)
        {
            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            // It will also prevent raising barriers if we are not the controller of the instance
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    // Build data list
                    Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Analyzing;
                    Mod.currentTNHInstance.warpInData = new List<Vector3>();
                    foreach(GameObject target in ___m_warpInTargets)
                    {
                        Mod.currentTNHInstance.warpInData.Add(target.transform.position);
                        Mod.currentTNHInstance.warpInData.Add(target.transform.rotation.eulerAngles);
                    }

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldPointBeginAnalyzing(0, Mod.currentTNHInstance.instance, Mod.currentTNHInstance.warpInData, ___m_tickDownToIdentification);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldPointBeginAnalyzing(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.warpInData, ___m_tickDownToIdentification);
                    }
                }
            }
        }

        static void FailOutPrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                Mod.currentTNHInstance.holdOngoing = false;
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHHoldPointFailOut(Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHHoldPointFailOut(Mod.currentTNHInstance.instance);
                }
            }
        }

        static void BeginPhasePrefix()
        {
            if(beginPhaseSkip > 0)
            {
                return;
            }

            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
            {
                Mod.currentTNHInstance.holdOngoing = true;
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (H3MP_ThreadManager.host)
                {
                    H3MP_ServerSend.TNHHoldPointBeginPhase(Mod.currentTNHInstance.instance);
                }
                else
                {
                    H3MP_ClientSend.TNHHoldPointBeginPhase(Mod.currentTNHInstance.instance);
                }
            }
        }

        static void CompleteHoldPrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == H3MP_GameManager.ID)
                {
                    Mod.currentTNHInstance.holdOngoing = false;
                    Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.TNHHoldPointCompleteHold(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        H3MP_ClientSend.TNHHoldPointCompleteHold(Mod.currentTNHInstance.instance);
                    }
                }
                else
                {
                    ++TNH_ManagerPatch.completeTokenSkip;
                }
            }
        }

        static void CompleteHoldPostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
            {
                --TNH_ManagerPatch.completeTokenSkip;
            }
        }

        static void SpawnEnemyGroupPrefix()
        {
            inSpawnEnemyGroup = true;
        }

        static void SpawnEnemyGroupPostfix()
        {
            inSpawnEnemyGroup = false;
        }

        static void SpawnTurretsPrefix()
        {
            inSpawnTurrets = true;
        }

        static void SpawnTurretsPostfix()
        {
            inSpawnTurrets = false;
        }
    }

    class TNH_SupplyPointPatch
    {
        public static bool inSpawnTakeEnemyGroup;
        public static bool inSpawnDefenses;
        public static int supplyPointIndex;

        static void SpawnTakeEnemyGroupPrefix(TNH_SupplyPoint __instance)
        {
            inSpawnTakeEnemyGroup = true;
            supplyPointIndex = -1;
            for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
            {
                if (__instance == GM.TNH_Manager.SupplyPoints[i])
                {
                    supplyPointIndex = i;
                    break;
                }
            }
        }

        static void SpawnTakeEnemyGroupPostfix()
        {
            inSpawnTakeEnemyGroup = false;
        }

        static void SpawnDefensesPrefix(TNH_SupplyPoint __instance)
        {
            inSpawnDefenses = true;
            supplyPointIndex = -1;
            for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
            {
                if (__instance == GM.TNH_Manager.SupplyPoints[i])
                {
                    supplyPointIndex = i;
                    break;
                }
            }
        }

        static void SpawnDefensesPostfix()
        {
            inSpawnDefenses = false;
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
        // This is so that when we make a player inactive because they are dead, we don't want to see them on the reticle either
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