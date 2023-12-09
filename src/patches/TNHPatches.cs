using FistVR;
using H3MP.Networking;
using H3MP.Scripts;
using H3MP.Tracking;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace H3MP.Patches
{
    public class TNHPatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // SetTNHManagerPatch
            MethodInfo setTNHManagerPatchOriginal = typeof(GM).GetMethod("set_TNH_Manager", BindingFlags.Public | BindingFlags.Static);
            MethodInfo setTNHManagerPatchPostfix = typeof(SetTNHManagerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(setTNHManagerPatchOriginal, harmony, true);
            harmony.Patch(setTNHManagerPatchOriginal, null, new HarmonyMethod(setTNHManagerPatchPostfix));

            ++patchIndex; // 1

            // TNH_TokenPatch
            MethodInfo TNH_TokenPatchPatchCollectOriginal = typeof(TNH_Token).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_TokenPatchPatchCollectPrefix = typeof(TNH_TokenPatch).GetMethod("CollectPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_TokenPatchPatchCollectPostfix = typeof(TNH_TokenPatch).GetMethod("CollectPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_TokenPatchPatchCollectOriginal, harmony, true);
            harmony.Patch(TNH_TokenPatchPatchCollectOriginal, new HarmonyMethod(TNH_TokenPatchPatchCollectPrefix), new HarmonyMethod(TNH_TokenPatchPatchCollectPostfix));

            ++patchIndex; // 2

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

            PatchController.Verify(TNH_UIManagerPatchProgressionOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchEquipmentOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchHealthModeOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchTargetModeOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchAIDifficultyOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchRadarModeOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchItemSpawnerModeOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchBackpackModeOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchHealthMultOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchSosigGunReloadOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchSeedOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchNextLevelOriginal, harmony, false);
            PatchController.Verify(TNH_UIManagerPatchPrevLevelOriginal, harmony, false);
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

            ++patchIndex; // 3

            // TNH_ManagerPatch
            MethodInfo TNH_ManagerPatchSetPhaseTakeOriginal = null;
            MethodInfo TNH_ManagerGeneratePatrolOriginal = null;
            if (PatchController.TNHTweakerAsmIdx > -1)
            {
                TNH_ManagerPatchSetPhaseTakeOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SetPhase_Take_Replacement", BindingFlags.Public | BindingFlags.Static);
                Mod.LogInfo("About to patch TNH tweaker SetPhase_Take_Replacement, null?: " + (TNH_ManagerPatchSetPhaseTakeOriginal == null));
                TNH_ManagerGeneratePatrolOriginal = PatchController.TNHTweaker_PatrolPatches.GetMethod("GeneratePatrol", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, new Type[] { typeof(TNH_Manager), PatchController.TNHTweaker_Patrol, typeof(List<Vector3>), typeof(List<Vector3>), typeof(List<Vector3>), typeof(int) }, null);
            }
            else
            {
                TNH_ManagerPatchSetPhaseTakeOriginal = typeof(TNH_Manager).GetMethod("SetPhase_Take", BindingFlags.NonPublic | BindingFlags.Instance);
                TNH_ManagerGeneratePatrolOriginal = typeof(TNH_Manager).GetMethod("GeneratePatrol", BindingFlags.NonPublic | BindingFlags.Instance);
            }
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
            MethodInfo TNH_ManagerPatchInitBeginEquipOriginal = typeof(TNH_Manager).GetMethod("InitBeginningEquipment", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchInitBeginEquipPrefix = typeof(TNH_ManagerPatch).GetMethod("InitBeginEquipPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseTakePrefix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseTakePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseTakePostfix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseTakePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseHoldOriginal = typeof(TNH_Manager).GetMethod("SetPhase_Hold", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetPhaseHoldPrefix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseHoldPostfix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseHoldPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerPatchSetPhaseCompleteOriginal = typeof(TNH_Manager).GetMethod("SetPhase_Completed", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerPatchSetPhaseCompletePrefix = typeof(TNH_ManagerPatch).GetMethod("SetPhaseCompletePrefix", BindingFlags.NonPublic | BindingFlags.Static);
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
            MethodInfo TNH_ManagerGeneratePatrolPrefix = typeof(TNH_ManagerPatch).GetMethod("GeneratePatrolPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerGeneratePatrolPostfix = typeof(TNH_ManagerPatch).GetMethod("GeneratePatrolPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerDelayedInitOriginal = typeof(TNH_Manager).GetMethod("DelayedInit", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerDelayedInitPrefix = typeof(TNH_ManagerPatch).GetMethod("DelayedInitPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerDelayedInitPostfix = typeof(TNH_ManagerPatch).GetMethod("DelayedInitPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_ManagerObjectCleanupInHoldOriginal = typeof(TNH_Manager).GetMethod("ObjectCleanupInHold", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ManagerObjectCleanupInHoldPrefix = typeof(TNH_ManagerPatch).GetMethod("ObjectCleanupInHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ManagerPatchPlayerDiedOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchAddTokensOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSosigKillOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSetPhaseOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchUpdateOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchInitBeginEquipOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSetPhaseTakeOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSetPhaseHoldOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSetPhaseCompleteOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchSetLevelOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchOnShotFiredOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchOnBotShotFiredOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerPatchAddFVRObjectToTrackedListOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerGenerateSentryPatrolOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerGeneratePatrolOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerDelayedInitOriginal, harmony, true);
            PatchController.Verify(TNH_ManagerObjectCleanupInHoldOriginal, harmony, false);
            harmony.Patch(TNH_ManagerPatchPlayerDiedOriginal, new HarmonyMethod(TNH_ManagerPatchPlayerDiedPrefix));
            harmony.Patch(TNH_ManagerPatchAddTokensOriginal, new HarmonyMethod(TNH_ManagerPatchAddTokensPrefix));
            harmony.Patch(TNH_ManagerPatchSosigKillOriginal, new HarmonyMethod(TNH_ManagerPatchSosigKillPrefix));
            harmony.Patch(TNH_ManagerPatchSetPhaseOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhasePrefix));
            harmony.Patch(TNH_ManagerPatchUpdateOriginal, new HarmonyMethod(TNH_ManagerPatchUpdatePrefix));
            harmony.Patch(TNH_ManagerPatchInitBeginEquipOriginal, new HarmonyMethod(TNH_ManagerPatchInitBeginEquipPrefix));
            harmony.Patch(TNH_ManagerPatchSetLevelOriginal, new HarmonyMethod(TNH_ManagerPatchSetLevelPrefix));
            harmony.Patch(TNH_ManagerPatchSetPhaseTakeOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhaseTakePrefix), new HarmonyMethod(TNH_ManagerPatchSetPhaseTakePostfix));
            harmony.Patch(TNH_ManagerPatchSetPhaseHoldOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhaseHoldPrefix), new HarmonyMethod(TNH_ManagerPatchSetPhaseHoldPostfix));
            harmony.Patch(TNH_ManagerPatchSetPhaseCompleteOriginal, new HarmonyMethod(TNH_ManagerPatchSetPhaseCompletePrefix), new HarmonyMethod(TNH_ManagerPatchSetPhaseCompletePostfix));
            harmony.Patch(TNH_ManagerPatchOnShotFiredOriginal, new HarmonyMethod(TNH_ManagerPatchOnShotFiredPrefix));
            harmony.Patch(TNH_ManagerPatchOnBotShotFiredOriginal, new HarmonyMethod(TNH_ManagerPatchOnBotShotFiredPrefix));
            harmony.Patch(TNH_ManagerPatchAddFVRObjectToTrackedListOriginal, new HarmonyMethod(TNH_ManagerPatchAddFVRObjectToTrackedListPrefix));
            harmony.Patch(TNH_ManagerGenerateSentryPatrolOriginal, new HarmonyMethod(TNH_ManagerGenerateSentryPatrolPrefix), new HarmonyMethod(TNH_ManagerGenerateSentryPatrolPostfix));
            harmony.Patch(TNH_ManagerGeneratePatrolOriginal, new HarmonyMethod(TNH_ManagerGeneratePatrolPrefix), new HarmonyMethod(TNH_ManagerGeneratePatrolPostfix));
            harmony.Patch(TNH_ManagerDelayedInitOriginal, new HarmonyMethod(TNH_ManagerDelayedInitPrefix), new HarmonyMethod(TNH_ManagerDelayedInitPostfix));
            harmony.Patch(TNH_ManagerObjectCleanupInHoldOriginal, new HarmonyMethod(TNH_ManagerObjectCleanupInHoldPrefix));

            ++patchIndex; // 4

            // TNHSupplyPointPatch
            if (PatchController.TNHTweakerAsmIdx > -1)
            {
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnSupplyGroup", BindingFlags.Public | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnDefensesOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnSupplyTurrets", BindingFlags.Public | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnBoxesOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnSupplyBoxes", BindingFlags.Public | BindingFlags.Static);
                PatchController.Verify(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, harmony, false);
                PatchController.Verify(TNHSupplyPointPatchSpawnDefensesOriginal, harmony, false);
                PatchController.Verify(TNHSupplyPointPatchSpawnBoxesOriginal, harmony, false);
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnTakeEnemyGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnTakeEnemyGroupPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnDefensesPrefix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnDefensesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnDefensesPostfix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnDefensesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnBoxesPrefix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnBoxesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnBoxesPostfix = typeof(TNH_SupplyPointPatch).GetMethod("TNHTweaker_SpawnBoxesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix));
                harmony.Patch(TNHSupplyPointPatchSpawnDefensesOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPostfix));
                harmony.Patch(TNHSupplyPointPatchSpawnBoxesOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnBoxesPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnBoxesPostfix));
            }
            else
            {
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal = typeof(TNH_SupplyPoint).GetMethod("SpawnTakeEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo TNHSupplyPointPatchSpawnDefensesOriginal = typeof(TNH_SupplyPoint).GetMethod("SpawnDefenses", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo TNHSupplyPointPatchSpawnBoxesOriginal = typeof(TNH_SupplyPoint).GetMethod("SpawnBoxes", BindingFlags.NonPublic | BindingFlags.Instance);
                PatchController.Verify(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, harmony, false);
                PatchController.Verify(TNHSupplyPointPatchSpawnDefensesOriginal, harmony, false);
                PatchController.Verify(TNHSupplyPointPatchSpawnBoxesOriginal, harmony, false);
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnTakeEnemyGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnTakeEnemyGroupPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnDefensesPrefix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnDefensesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnDefensesPostfix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnDefensesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnBoxesPrefix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnBoxesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo TNHSupplyPointPatchSpawnBoxesPostfix = typeof(TNH_SupplyPointPatch).GetMethod("SpawnBoxesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(TNHSupplyPointPatchSpawnTakeEnemyGroupOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnTakeEnemyGroupPostfix));
                harmony.Patch(TNHSupplyPointPatchSpawnDefensesOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnDefensesPostfix));
                harmony.Patch(TNHSupplyPointPatchSpawnBoxesOriginal, new HarmonyMethod(TNHSupplyPointPatchSpawnBoxesPrefix), new HarmonyMethod(TNHSupplyPointPatchSpawnBoxesPostfix));
            }

            ++patchIndex; // 5

            // TAHReticleContactPatch
            MethodInfo TAHReticleContactPatchTickOriginal = typeof(TAH_ReticleContact).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchTickTranspiler = typeof(TAHReticleContactPatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TAHReticleContactPatchSetContactTypeOriginal = typeof(TAH_ReticleContact).GetMethod("SetContactType", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TAHReticleContactPatchSetContactTypePrefix = typeof(TAHReticleContactPatch).GetMethod("SetContactTypePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TAHReticleContactPatchTickOriginal, harmony, false);
            try
            { 
                harmony.Patch(TAHReticleContactPatchTickOriginal, null, null, new HarmonyMethod(TAHReticleContactPatchTickTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying TNHPatches.TAHReticleContactPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(TAHReticleContactPatchSetContactTypeOriginal, new HarmonyMethod(TAHReticleContactPatchSetContactTypePrefix));

            ++patchIndex; // 6

            // TNH_HoldPointPatch
            MethodInfo TNH_HoldPointPatchSpawnTargetGroupOriginal = null;
            MethodInfo TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal = null;
            MethodInfo TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal = null;
            MethodInfo TNH_HoldPointPatchSpawnTurretsOriginal = null;
            if (PatchController.TNHTweakerAsmIdx > -1)
            {
                TNH_HoldPointPatchSpawnTargetGroupOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnEncryptionReplacement", BindingFlags.Public | BindingFlags.Static);
                TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnTakeGroupReplacement", BindingFlags.Public | BindingFlags.Static);
                TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnHoldEnemyGroup", BindingFlags.Public | BindingFlags.Static);
                TNH_HoldPointPatchSpawnTurretsOriginal = PatchController.TNHTweaker_TNHPatches.GetMethod("SpawnTurretsReplacement", BindingFlags.Public | BindingFlags.Static);
            }
            else
            {
                TNH_HoldPointPatchSpawnTargetGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTargetGroup", BindingFlags.NonPublic | BindingFlags.Instance);
                TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTakeEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
                TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnHoldEnemyGroup", BindingFlags.NonPublic | BindingFlags.Instance);
                TNH_HoldPointPatchSpawnTurretsOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTurrets", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            MethodInfo TNH_HoldPointPatchSystemNodeOriginal = typeof(TNH_HoldPoint).GetMethod("ConfigureAsSystemNode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSystemNodePrefix = typeof(TNH_HoldPointPatch).GetMethod("ConfigureAsSystemNodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnTakeChallengeEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnEntitiesPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTakeChallengeEntitiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginHoldOriginal = typeof(TNH_HoldPoint).GetMethod("BeginHoldChallenge", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginHoldPrefix = typeof(TNH_HoldPointPatch).GetMethod("BeginHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersOriginal = typeof(TNH_HoldPoint).GetMethod("RaiseRandomBarriers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersPrefix = typeof(TNH_HoldPointPatch).GetMethod("RaiseRandomBarriersPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseRandomBarriersPostfix = typeof(TNH_HoldPointPatch).GetMethod("RaiseRandomBarriersPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataOriginal = typeof(TNH_DestructibleBarrierPoint).GetMethod("SetCoverPointData", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchRaiseSetCoverPointDataPrefix = typeof(TNH_HoldPointPatch).GetMethod("BarrierSetCoverPointDataPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchCompletePhaseOriginal = typeof(TNH_HoldPoint).GetMethod("CompletePhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchCompletePhasePrefix = typeof(TNH_HoldPointPatch).GetMethod("CompletePhasePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchUpdateOriginal = typeof(TNH_HoldPoint).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchUpdatePrefix = typeof(TNH_HoldPointPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginAnalyzingOriginal = typeof(TNH_HoldPoint).GetMethod("BeginAnalyzing", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginAnalyzingPostfix = typeof(TNH_HoldPointPatch).GetMethod("BeginAnalyzingPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnWarpInMarkersOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnWarpInMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchSpawnWarpInMarkersPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnWarpInMarkersPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTargetGroupPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTargetGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchIdentifyEncryptionOriginal = typeof(TNH_HoldPoint).GetMethod("IdentifyEncryption", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchIdentifyEncryptionPostfix = typeof(TNH_HoldPointPatch).GetMethod("IdentifyEncryptionPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchFailOutOriginal = typeof(TNH_HoldPoint).GetMethod("FailOut", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchFailOutPrefix = typeof(TNH_HoldPointPatch).GetMethod("FailOutPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchBeginPhaseOriginal = typeof(TNH_HoldPoint).GetMethod("BeginPhase", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchBeginPhasePostfix = typeof(TNH_HoldPointPatch).GetMethod("BeginPhasePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchShutDownHoldPointOriginal = typeof(TNH_HoldPoint).GetMethod("ShutDownHoldPoint", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchShutDownHoldPointPrefix = typeof(TNH_HoldPointPatch).GetMethod("ShutDownHoldPointPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchCompleteHoldOriginal = typeof(TNH_HoldPoint).GetMethod("CompleteHold", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_HoldPointPatchCompleteHoldPrefix = typeof(TNH_HoldPointPatch).GetMethod("CompleteHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchCompleteHoldPostfix = typeof(TNH_HoldPointPatch).GetMethod("CompleteHoldPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEnemyGroupPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnEnemyGroupPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnEnemyGroupPostfix = typeof(TNH_HoldPointPatch).GetMethod("SpawnEnemyGroupPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTurretsPrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTurretsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo TNH_HoldPointPatchSpawnTurretsPostfix = typeof(TNH_HoldPointPatch).GetMethod("SpawnTurretsPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo TNH_HoldPointPatchDeletionBurstOriginal = typeof(TNH_HoldPoint).GetMethod("DeletionBurst", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo TNH_HoldPointPatchDeletionBurstPrefix = typeof(TNH_HoldPointPatch).GetMethod("DeletionBurstPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo TNH_HoldPointPatchDeleteAllActiveEntitiesOriginal = typeof(TNH_HoldPoint).GetMethod("DeleteAllActiveEntities", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo TNH_HoldPointPatchDeleteAllActiveEntitiesPrefix = typeof(TNH_HoldPointPatch).GetMethod("DeleteAllActiveEntitiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo TNH_HoldPointPatchDeleteAllActiveTargetsOriginal = typeof(TNH_HoldPoint).GetMethod("DeleteAllActiveTargets", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo TNH_HoldPointPatchDeleteAllActiveTargetsPrefix = typeof(TNH_HoldPointPatch).GetMethod("DeleteAllActiveTargetsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo TNH_HoldPointPatchDeleteSosigsOriginal = typeof(TNH_HoldPoint).GetMethod("DeleteSosigs", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo TNH_HoldPointPatchDeleteSosigsPrefix = typeof(TNH_HoldPointPatch).GetMethod("DeleteSosigsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            //MethodInfo TNH_HoldPointPatchDeleteTurretsOriginal = typeof(TNH_HoldPoint).GetMethod("DeleteTurrets", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo TNH_HoldPointPatchDeleteTurretsPrefix = typeof(TNH_HoldPointPatch).GetMethod("DeleteTurretsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            ////MethodInfo TNH_HoldPointPatchSpawnSystemNodeOriginal = typeof(TNH_HoldPoint).GetMethod("SpawnSystemNode", BindingFlags.NonPublic | BindingFlags.Instance);
            ////MethodInfo TNH_HoldPointPatchSpawnSystemNodePrefix = typeof(TNH_HoldPointPatch).GetMethod("SpawnSystemNodePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_HoldPointPatchSystemNodeOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchSpawnEntitiesOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchBeginHoldOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchRaiseRandomBarriersOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchRaiseSetCoverPointDataOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchCompletePhaseOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchUpdateOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchBeginAnalyzingOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchSpawnWarpInMarkersOriginal, harmony, false);
            PatchController.Verify(TNH_HoldPointPatchSpawnTargetGroupOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchIdentifyEncryptionOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchFailOutOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchBeginPhaseOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchShutDownHoldPointOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchCompleteHoldOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal, harmony, true);
            PatchController.Verify(TNH_HoldPointPatchSpawnTurretsOriginal, harmony, true);
            //Verify(TNH_HoldPointPatchDeletionBurstOriginal, harmony, true);
            //Verify(TNH_HoldPointPatchDeleteAllActiveEntitiesOriginal, harmony, true);
            //Verify(TNH_HoldPointPatchDeleteAllActiveTargetsOriginal, harmony, true);
            //Verify(TNH_HoldPointPatchDeleteSosigsOriginal, harmony, true);
            //Verify(TNH_HoldPointPatchDeleteTurretsOriginal, harmony, true);
            ////Verify(TNH_HoldPointPatchSpawnSystemNodeOriginal, harmony, true);
            harmony.Patch(TNH_HoldPointPatchSystemNodeOriginal, new HarmonyMethod(TNH_HoldPointPatchSystemNodePrefix));
            harmony.Patch(TNH_HoldPointPatchSpawnEntitiesOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEntitiesPrefix));
            harmony.Patch(TNH_HoldPointPatchBeginHoldOriginal, new HarmonyMethod(TNH_HoldPointPatchBeginHoldPrefix));
            harmony.Patch(TNH_HoldPointPatchRaiseRandomBarriersOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseRandomBarriersPrefix), new HarmonyMethod(TNH_HoldPointPatchRaiseRandomBarriersPostfix));
            harmony.Patch(TNH_HoldPointPatchRaiseSetCoverPointDataOriginal, new HarmonyMethod(TNH_HoldPointPatchRaiseSetCoverPointDataPrefix));
            harmony.Patch(TNH_HoldPointPatchCompletePhaseOriginal, new HarmonyMethod(TNH_HoldPointPatchCompletePhasePrefix));
            harmony.Patch(TNH_HoldPointPatchUpdateOriginal, new HarmonyMethod(TNH_HoldPointPatchUpdatePrefix));
            harmony.Patch(TNH_HoldPointPatchBeginAnalyzingOriginal, null, new HarmonyMethod(TNH_HoldPointPatchBeginAnalyzingPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnWarpInMarkersOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnWarpInMarkersPrefix));
            harmony.Patch(TNH_HoldPointPatchSpawnTargetGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnTargetGroupPrefix));
            harmony.Patch(TNH_HoldPointPatchIdentifyEncryptionOriginal, null, new HarmonyMethod(TNH_HoldPointPatchIdentifyEncryptionPostfix));
            harmony.Patch(TNH_HoldPointPatchFailOutOriginal, new HarmonyMethod(TNH_HoldPointPatchFailOutPrefix));
            harmony.Patch(TNH_HoldPointPatchBeginPhaseOriginal, null, new HarmonyMethod(TNH_HoldPointPatchBeginPhasePostfix));
            harmony.Patch(TNH_HoldPointPatchShutDownHoldPointOriginal, new HarmonyMethod(TNH_HoldPointPatchShutDownHoldPointPrefix));
            harmony.Patch(TNH_HoldPointPatchCompleteHoldOriginal, new HarmonyMethod(TNH_HoldPointPatchCompleteHoldPrefix), new HarmonyMethod(TNH_HoldPointPatchCompleteHoldPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnTakeEnemyGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnHoldEnemyGroupOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnEnemyGroupPostfix));
            harmony.Patch(TNH_HoldPointPatchSpawnTurretsOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnTurretsPrefix), new HarmonyMethod(TNH_HoldPointPatchSpawnTurretsPostfix));
            //harmony.Patch(TNH_HoldPointPatchDeletionBurstOriginal, new HarmonyMethod(TNH_HoldPointPatchDeletionBurstPrefix));
            //harmony.Patch(TNH_HoldPointPatchDeleteAllActiveEntitiesOriginal, new HarmonyMethod(TNH_HoldPointPatchDeleteAllActiveEntitiesPrefix));
            //harmony.Patch(TNH_HoldPointPatchDeleteAllActiveTargetsOriginal, new HarmonyMethod(TNH_HoldPointPatchDeleteAllActiveTargetsPrefix));
            //harmony.Patch(TNH_HoldPointPatchDeleteSosigsOriginal, new HarmonyMethod(TNH_HoldPointPatchDeleteSosigsPrefix));
            //harmony.Patch(TNH_HoldPointPatchDeleteTurretsOriginal, new HarmonyMethod(TNH_HoldPointPatchDeleteTurretsPrefix));
            ////harmony.Patch(TNH_HoldPointPatchSpawnSystemNodeOriginal, new HarmonyMethod(TNH_HoldPointPatchSpawnSystemNodePrefix));

            ++patchIndex; // 7

            // TNHWeaponCrateSpawnObjectsPatch
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchOriginal = typeof(TNH_WeaponCrate).GetMethod("SpawnObjectsRaw", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_WeaponCrateSpawnObjectsPatchPrefix = typeof(TNHWeaponCrateSpawnObjectsPatch).GetMethod("SpawnObjectsRawPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_WeaponCrateSpawnObjectsPatchOriginal, harmony, false);
            harmony.Patch(TNH_WeaponCrateSpawnObjectsPatchOriginal, new HarmonyMethod(TNH_WeaponCrateSpawnObjectsPatchPrefix));

            ++patchIndex; // 8

            // SceneLoaderPatch
            MethodInfo SceneLoaderPatchLoadMGOriginal = typeof(SceneLoader).GetMethod("LoadMG", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo SceneLoaderPatchLoadMGPrefix = typeof(SceneLoaderPatch).GetMethod("LoadMGPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(SceneLoaderPatchLoadMGOriginal, harmony, true);
            harmony.Patch(SceneLoaderPatchLoadMGOriginal, new HarmonyMethod(SceneLoaderPatchLoadMGPrefix));
        }
    }

    // Patches GM.set_TNH_Manager() to keep track of TNH Manager instances
    class SetTNHManagerPatch
    {
        static void Postfix()
        {
            Mod.LogInfo("SetTNHManagerPatch postfix", false);
            // Also manage currently playing in the TNH instance
            if (Mod.managerObject != null)
            {
                Mod.LogInfo("\tConnected", false);
                if (Mod.currentTNHInstance != null)
                {
                    Mod.LogInfo("\t\tIn MP TNH instance", false);
                    if (GM.TNH_Manager != null)
                    {
                        Mod.LogInfo("\t\t\tGM.TNH_Manager set", false);
                        // Keep our own reference
                        Mod.currentTNHInstance.manager = GM.TNH_Manager;

                        // Reset TNH_ManagerPatch data
                        TNH_ManagerPatch.patrolIndex = -1;

                        Mod.currentTNHInstance.AddCurrentlyPlaying(true, GameManager.ID, ThreadManager.host);
                        Mod.currentlyPlayingTNH = true;

                        // Anytime we join, if the phase is already passed StartUp it means initial equip has already been 
                        // spawned normally for someone, we want a button instead
                    }
                    else if (Mod.currentlyPlayingTNH) // TNH_Manager was set to null and we are currently playing
                    {
                        Mod.LogInfo("\t\t\tGM.TNH_Manager unset", false);
                        Mod.currentlyPlayingTNH = false;
                        Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, GameManager.ID, ThreadManager.host);
                    }
                }
                else // We just set TNH_Manager but we are not in a TNH instance
                {
                    Mod.LogInfo("\t\tNot in MP TNH instance", false);
                    if (GM.TNH_Manager == null)
                    {
                        if (GameManager.instance != 0)
                        {
                            // Just left a TNH game, must set instance to 0
                            GameManager.SetInstance(0);
                        }
                    }
                    else
                    {
                        // Just started a TNH game, must set instance to a new instance to play TNH solo
                        Mod.setLatestInstance = true;
                        GameManager.AddNewInstance();
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
                    if (Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                    {
                        return false;
                    }

                    // Update locally
                    Mod.currentTNHInstance.progressionTypeSetting = i;

                    // Send update
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHProgression(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHProgression(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHEquipment(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHEquipment(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHHealthMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHHealthMode(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHTargetMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHTargetMode(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHAIDifficulty(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHAIDifficulty(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHRadarMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHRadarMode(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHItemSpawnerMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHItemSpawnerMode(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHBackpackMode(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHBackpackMode(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHHealthMult(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHHealthMult(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHSosigGunReload(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHSosigGunReload(i, Mod.currentTNHInstance.instance);
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHSeed(i, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHSeed(i, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool NextLevelPrefix(ref TNH_UIManager __instance, int ___m_currentLevelIndex)
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

                    // Update locally
                    int nextLevelIndex = ___m_currentLevelIndex + 1;
                    if (nextLevelIndex >= __instance.Levels.Count)
                    {
                        nextLevelIndex = 0;
                    }
                    Mod.currentTNHInstance.levelID = __instance.Levels[nextLevelIndex].LevelID;

                    // Send update
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHLevelID(Mod.currentTNHInstance.levelID, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHLevelID(Mod.currentTNHInstance.levelID, Mod.currentTNHInstance.instance);
                    }
                }
            }

            return true;
        }

        static bool PrevLevelPrefix(ref TNH_UIManager __instance, int ___m_currentLevelIndex)
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

                    // Update locally
                    int prevLevelIndex = ___m_currentLevelIndex - 1;
                    if (prevLevelIndex < 0)
                    {
                        prevLevelIndex = __instance.Levels.Count - 1;
                    }
                    Mod.currentTNHInstance.levelID = __instance.Levels[prevLevelIndex].LevelID;

                    // Send update
                    if (ThreadManager.host)
                    {
                        ServerSend.SetTNHLevelID(Mod.currentTNHInstance.levelID, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.SetTNHLevelID(Mod.currentTNHInstance.levelID, Mod.currentTNHInstance.instance);
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
        public static bool inDelayedInit;
        static bool skipNextSetPhaseTake;
        static bool TNHInitializing;
        static bool TNHInitialized;

        public static bool inGenerateSentryPatrol;
        public static bool inGeneratePatrol;
        public static List<Vector3> patrolPoints;
        public static int patrolIndex = -1;

        static bool PlayerDiedPrefix()
        {
            if (Mod.managerObject != null)
            {
                if (Mod.currentTNHInstance != null)
                {
                    // We don't actually want to set phase to dead as long as there are still players alive
                    //Mod.currentTNHInstance.manager.Phase = TNH_Phase.Dead;

                    // Update locally
                    Mod.currentTNHInstance.dead.Add(GameManager.ID);
                    GM.TNH_Manager.SubtractTokens(GM.TNH_Manager.GetNumTokens());

                    // Send update
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHPlayerDied(Mod.currentTNHInstance.instance, GameManager.ID);
                    }
                    else
                    {
                        ClientSend.TNHPlayerDied(Mod.currentTNHInstance.instance, GameManager.ID);
                    }

                    // Prevent TNH from processing player death if there are other players still in the game
                    bool someStillAlive = false;
                    for (int i = 0; i < Mod.currentTNHInstance.currentlyPlaying.Count; ++i)
                    {
                        if (!Mod.currentTNHInstance.dead.Contains(Mod.currentTNHInstance.currentlyPlaying[i]))
                        {
                            someStillAlive = true;
                            break;
                        }
                    }
                    if (someStillAlive)
                    {
                        if (Mod.TNHOnDeathSpectate)
                        {
                            Mod.TNHSpectating = true;
                            Mod.DropAllItems();
                            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                            {
                                GM.CurrentPlayerBody.DisableHands();
                            }
                        }

                        Mod.currentTNHInstance.manager.FMODController.SwitchTo(0, 2f, false, false);

                        return false;
                    }
                    else if (Mod.currentTNHInstance.controller != GameManager.ID)// Last live player, if not controller we dont want to kill all patrols
                    {
                        Mod.currentTNHInstance.manager.m_patrolSquads.Clear();

                        Mod.currentTNHInstance.manager.FMODController.SwitchTo(0, 2f, false, false);
                        for (int i = 0; i < Mod.currentTNHInstance.manager.HoldPoints.Count; i++)
                        {
                            // ForceClearConfiguration
                            TNH_HoldPoint holdPoint = Mod.currentTNHInstance.manager.HoldPoints[i];
                            holdPoint.m_isInHold = false;
                            holdPoint.m_state = TNH_HoldPoint.HoldState.Beginning;
                            holdPoint.NavBlockers.SetActive(false);
                            holdPoint.m_phaseIndex = 0;
                            holdPoint.m_maxPhases = 0;
                            holdPoint.m_curPhase = null;
                            holdPoint.DeleteSystemNode();

                            // DeleteAllActiveEntities
                            holdPoint.m_activeTargets.Clear();
                            holdPoint.DeleteAllActiveWarpIns();
                            holdPoint.DeleteBarriers();
                            holdPoint.m_activeSosigs.Clear();
                            holdPoint.m_activeTurrets.Clear();
                        }
                        for (int j = 0; j < Mod.currentTNHInstance.manager.SupplyPoints.Count; j++)
                        {
                            // DeleteAllActiveEntities
                            TNH_SupplyPoint supplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[j];
                            supplyPoint.m_activeSosigs.Clear();
                            supplyPoint.m_activeTurrets.Clear();
                            if (supplyPoint.m_trackedObjects.Count > 0)
                            {
                                for (int i = supplyPoint.m_trackedObjects.Count - 1; i >= 0; i--)
                                {
                                    if (supplyPoint.m_trackedObjects[i] != null)
                                    {
                                        UnityEngine.Object.Destroy(supplyPoint.m_trackedObjects[i]);
                                    }
                                }
                            }
                            if (supplyPoint.m_constructor != null)
                            {
                                supplyPoint.m_constructor.GetComponent<TNH_ObjectConstructor>().ClearCase();
                                UnityEngine.Object.Destroy(supplyPoint.m_constructor);
                                supplyPoint.m_constructor = null;
                            }
                            if (supplyPoint.m_panel != null)
                            {
                                UnityEngine.Object.Destroy(supplyPoint.m_panel);
                                supplyPoint.m_panel = null;
                            }
                        }
                        Mod.currentTNHInstance.manager.DispatchScore();

                        Mod.currentTNHInstance.Reset();

                        return false;
                    }
                    else // last live player but we are controller
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

            if (addTokensSkip > 0)
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
                        if (ThreadManager.host)
                        {
                            ServerSend.TNHAddTokens(Mod.currentTNHInstance.instance, i);
                        }
                        else
                        {
                            ClientSend.TNHAddTokens(Mod.currentTNHInstance.instance, i);
                        }

                        Mod.currentTNHInstance.tokenCount += i;
                    }

                    // Prevent TNH from adding tokens if player is dead
                    if (Mod.currentTNHInstance.dead.Contains(GameManager.ID))
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

            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller != GameManager.ID)
                {
                    return false;
                }

                TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(s) ? GameManager.trackedSosigBySosig[s] : s.GetComponent<TrackedSosig>();
                if (trackedSosig != null)
                {
                    if (trackedSosig.data.trackedID == -1)
                    {
                        TrackedSosig.unknownTNHKills.Add(trackedSosig.data.localWaitingIndex, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.TNHSosigKill(Mod.currentTNHInstance.instance, trackedSosig.data.trackedID);
                        }
                        else
                        {
                            ClientSend.TNHSosigKill(Mod.currentTNHInstance.instance, trackedSosig.data.trackedID);
                        }
                    }
                }
            }

            return true;
        }

        static bool SetPhasePrefix(TNH_Phase p)
        {
            // We want to prevent call to SetPhase unless we are controller not init or initializer init
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                bool cont = (TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                            (Mod.currentTNHInstance.controller == GameManager.ID && Mod.currentTNHInstance.initializer != -1 &&
                            (!ThreadManager.host || !Mod.currentTNHInstance.initializationRequested));
                Mod.LogInfo("SetPhasePrefix: phase: " + p + ", continuing: " + cont, false);
                return cont;
            }
            return true;
        }

        static void SetLevelPrefix(int level)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHSetLevel(Mod.currentTNHInstance.instance, level);
                    }
                    else
                    {
                        ClientSend.TNHSetLevel(Mod.currentTNHInstance.instance, level);
                    }
                }
            }
        }

        static bool SetPhaseTakePrefix()
        {
            // TODO: Future: Scoring needs to be properly tracked, when we implement proper support for that
            //               will need to track alerted this phase, took damage this phase, etc. For now we only
            //               reset those here according with SetPhase_Take functionality
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("SetPhaseTakePrefix: In MP TNH", false);
                if (inDelayedInit)
                {
                    Mod.LogInfo("\tIn DelayedInit, this is init SetPhase_Take", false);
                    if (Mod.currentTNHInstance.initializer == GameManager.ID)
                    {
                        Mod.LogInfo("\t\tWe are initializer, set initialized and continuing", false);
                        TNHInitialized = true;
                        Mod.currentTNHInstance.phase = TNH_Phase.Take;
                        return true;
                    }
                    else
                    {
                        Mod.LogInfo("\t\tWe are not initializer, initializing with data", false);
                        // We are controller finishing our init in a TNH instance that has already been inited
                        InitJoinTNH();

                        skipNextSetPhaseTake = true;

                        return false;
                    }
                }
                else
                {
                    Mod.LogInfo("\tNot in DelayedInit", false);
                    if (Mod.currentTNHInstance.controller == GameManager.ID)
                    {
                        Mod.LogInfo("\tWe are controller, continuing", false);
                        Mod.currentTNHInstance.phase = TNH_Phase.Take;
                        return true;
                    }
                    else
                    {
                        Mod.LogInfo("\tWe are not controller, setting take phase with data", false);
                        Mod.currentTNHInstance.phase = TNH_Phase.Take;
                        Mod.currentTNHInstance.manager.Phase = TNH_Phase.Take;

                        Mod.currentTNHInstance.manager.ResetAlertedThisPhase();
                        Mod.currentTNHInstance.manager.ResetPlayerTookDamageThisPhase();
                        Mod.currentTNHInstance.manager.ResetHasGuardBeenKilledThatWasAltered();

                        object level = null;
                        if (PatchController.TNHTweakerAsmIdx > -1)
                        {
                            object character = ((IDictionary)PatchController.TNHTweaker_LoadedTemplateManager_LoadedCharactersDict.GetValue(PatchController.TNHTweaker_LoadedTemplateManager))[GM.TNH_Manager.C];
                            level = PatchController.TNHTweaker_CustomCharacter_GetCurrentLevel.Invoke(character, new object[] { GM.TNH_Manager.m_curLevel });

                            ((List<int>)PatchController.TNHTweaker_TNHTweaker_SpawnedBossIndexes.GetValue(PatchController.TNHTweaker_TNHTweaker)).Clear();

                            // Like we do for vanilla, we don't clear if not controller, we will just set it to the list in the TNH instance
                            //__instance.m_activeSupplyPointIndicies.Clear();

                            PatchController.TNHTweaker_TNHTweaker_PreventOutfitFunctionality.SetValue(PatchController.TNHTweaker_TNHTweaker, PatchController.TNHTweaker_CustomCharacter_ForceDisableOutfitFunctionality.GetValue(character));
                        }

                        if (Mod.currentTNHInstance.manager.RadarMode == TNHModifier_RadarMode.Standard)
                        {
                            Mod.currentTNHInstance.manager.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = Mod.currentTNHInstance.manager.ReticleMask_Take;
                        }
                        else if (Mod.currentTNHInstance.manager.RadarMode == TNHModifier_RadarMode.Omnipresent)
                        {
                            Mod.currentTNHInstance.manager.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = Mod.currentTNHInstance.manager.ReticleMask_Hold;
                        }
                        Mod.currentTNHInstance.manager.m_lastHoldIndex = Mod.currentTNHInstance.manager.m_curHoldIndex;
                        Mod.currentTNHInstance.manager.m_curHoldIndex = Mod.currentTNHInstance.curHoldIndex;
                        Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
                        Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Supply);
                        Mod.currentTNHInstance.manager.m_curHoldPoint = Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex];
                        TNH_Progression.Level curLevel = Mod.currentTNHInstance.manager.m_curLevel;
                        Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                        Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);
                        bool spawnToken = true;
                        Mod.currentTNHInstance.manager.m_activeSupplyPointIndicies = Mod.currentTNHInstance.activeSupplyPointIndices;

                        if (PatchController.TNHTweakerAsmIdx > -1)
                        {
                            if (Mod.currentTNHInstance.manager.m_curPointSequence.UsesExplicitSingleSupplyPoints)
                            {
                                for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                                {
                                    TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                                    // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves

                                    object[] args = new object[] { tnh_SupplyPoint, level, TNH_SupplyPoint.SupplyPanelType.All };
                                    PatchController.TNHTweaker_TNHPatches_ConfigureSupplyPoint.Invoke(PatchController.TNHTweaker_TNHPatches, args);
                                    Mod.currentTNHInstance.nextSupplyPanelType = (int)args[2];

                                    TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                                    tnh_SupplyPoint.SetContact(contact);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                                {
                                    TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];

                                    int panelIndex = Mod.currentTNHInstance.nextSupplyPanelType;
                                    object[] args = new object[] { tnh_SupplyPoint, level, panelIndex };
                                    PatchController.TNHTweaker_TNHPatches_ConfigureSupplyPoint.Invoke(PatchController.TNHTweaker_TNHPatches, args);
                                    Mod.currentTNHInstance.nextSupplyPanelType = (int)args[2];

                                    TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                                    tnh_SupplyPoint.SetContact(contact);
                                }
                            }
                        }
                        else
                        {
                            if (Mod.currentTNHInstance.manager.m_curPointSequence.UsesExplicitSingleSupplyPoints)
                            {
                                for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                                {
                                    TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                                    // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                                    tnh_SupplyPoint.Configure(curLevel.SupplyChallenge, false, false, true, TNH_SupplyPoint.SupplyPanelType.All, 0, 0, true);
                                    TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                                    tnh_SupplyPoint.SetContact(contact);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                                {
                                    TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];

                                    int num6 = i;
                                    if (i > 0)
                                    {
                                        num6 = Mod.currentTNHInstance.nextSupplyPanelType;
                                        Mod.currentTNHInstance.nextSupplyPanelType++;
                                        if (Mod.currentTNHInstance.nextSupplyPanelType > 2)
                                        {
                                            Mod.currentTNHInstance.nextSupplyPanelType = 1;
                                        }
                                    }
                                    TNH_SupplyPoint.SupplyPanelType panelType = (TNH_SupplyPoint.SupplyPanelType)num6;
                                    // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                                    tnh_SupplyPoint.Configure(curLevel.SupplyChallenge, false, false, true, panelType, 0, 0, spawnToken);
                                    spawnToken = false;
                                    TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                                    tnh_SupplyPoint.SetContact(contact);
                                }
                            }
                        }
                        if (Mod.currentTNHInstance.manager.BGAudioMode == TNH_BGAudioMode.Default)
                        {
                            Mod.currentTNHInstance.manager.FMODController.SwitchTo(0, 2f, false, false);
                        }

                        return false;
                    }
                }
            }

            return true;
        }

        static void SetPhaseTakePostfix()
        {
            if (skipNextSetPhaseTake)
            {
                skipNextSetPhaseTake = false;
                return;
            }

            // If we are controller/init collect data and send set phase take order
            if (Mod.managerObject != null && Mod.currentTNHInstance != null &&
                ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                 (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit)))
            {
                Mod.LogInfo("SetPhaseTakePostfix controller not in init or initializer in init, sending", false);
                Mod.currentTNHInstance.curHoldIndex = Mod.currentTNHInstance.manager.m_curHoldIndex;
                Mod.currentTNHInstance.activeSupplyPointIndices = Mod.currentTNHInstance.manager.m_activeSupplyPointIndicies;

                if (ThreadManager.host)
                {
                    ServerSend.TNHSetPhaseTake(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.curHoldIndex, Mod.currentTNHInstance.activeSupplyPointIndices, TNH_ManagerPatch.inDelayedInit);
                }
                else
                {
                    ClientSend.TNHSetPhaseTake(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.curHoldIndex, Mod.currentTNHInstance.activeSupplyPointIndices, TNH_ManagerPatch.inDelayedInit);
                }
            }
        }

        static bool SetPhaseHoldPrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("SetPhaseHoldPrefix", false);
                if (Mod.currentTNHInstance.controller != GameManager.ID)
                {
                    Mod.LogInfo("\tWe are not controller, this was an order, setting from data", false);
                    Mod.currentTNHInstance.manager.Phase = TNH_Phase.Hold;

                    Mod.currentTNHInstance.manager.m_fireThreshold = 0;
                    Mod.currentTNHInstance.manager.m_botKillThreshold = 0;
                    Mod.currentTNHInstance.manager.m_patrolSquads.Clear();

                    Mod.currentTNHInstance.manager.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = Mod.currentTNHInstance.manager.ReticleMask_Hold;
                    Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Supply);
                    if (Mod.currentTNHInstance.manager.BGAudioMode == TNH_BGAudioMode.Default)
                    {
                        Mod.currentTNHInstance.manager.FMODController.SwitchTo(1, 0f, true, false);
                    }
                    for (int i = 0; i < Mod.currentTNHInstance.manager.HoldPoints.Count; i++)
                    {
                        // DeleteAllActiveEntities
                        TNH_HoldPoint holdPoint = Mod.currentTNHInstance.manager.HoldPoints[i];
                        holdPoint.m_activeTargets.Clear();
                        holdPoint.DeleteAllActiveWarpIns();
                        holdPoint.DeleteBarriers();
                        holdPoint.m_activeSosigs.Clear();
                        holdPoint.m_activeTurrets.Clear();
                    }
                    for (int j = 0; j < Mod.currentTNHInstance.manager.SupplyPoints.Count; j++)
                    {
                        // DeleteAllActiveEntities
                        TNH_SupplyPoint supplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[j];
                        supplyPoint.m_activeSosigs.Clear();
                        supplyPoint.m_activeTurrets.Clear();
                        if (supplyPoint.m_trackedObjects.Count > 0)
                        {
                            for (int i = supplyPoint.m_trackedObjects.Count - 1; i >= 0; i--)
                            {
                                if (supplyPoint.m_trackedObjects[i] != null)
                                {
                                    UnityEngine.Object.Destroy(supplyPoint.m_trackedObjects[i]);
                                }
                            }
                        }
                        if (supplyPoint.m_constructor != null)
                        {
                            supplyPoint.m_constructor.GetComponent<TNH_ObjectConstructor>().ClearCase();
                            UnityEngine.Object.Destroy(supplyPoint.m_constructor);
                            supplyPoint.m_constructor = null;
                        }
                        if (supplyPoint.m_panel != null)
                        {
                            UnityEngine.Object.Destroy(supplyPoint.m_panel);
                            supplyPoint.m_panel = null;
                        }
                    }

                    return false;
                }

                Mod.currentTNHInstance.phase = TNH_Phase.Hold;
            }

            return true;
        }

        static void SetPhaseHoldPostfix()
        {
            // If we are controller collect data and send set phase take order
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == GameManager.ID)
            {
                Mod.LogInfo("SetPhaseHoldPrefix, we are controller, sending", false);
                if (ThreadManager.host)
                {
                    ServerSend.TNHSetPhaseHold(Mod.currentTNHInstance.instance);
                }
                else
                {
                    ClientSend.TNHSetPhaseHold(Mod.currentTNHInstance.instance);
                }
            }
        }

        static bool SetPhaseCompletePrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("SetPhaseCompletePrefix", false);
                if (Mod.currentTNHInstance.controller != GameManager.ID)
                {
                    Mod.LogInfo("\tWe are not controller, this was an order, setting from data", false);
                    Mod.currentTNHInstance.manager.Phase = TNH_Phase.Completed;

                    Mod.currentTNHInstance.manager.m_patrolSquads.Clear();

                    Mod.currentTNHInstance.manager.FMODController.SwitchTo(0, 2f, false, false);
                    for (int i = 0; i < Mod.currentTNHInstance.manager.HoldPoints.Count; i++)
                    {
                        // ForceClearConfiguration
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_isInHold = false;
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_state = TNH_HoldPoint.HoldState.Beginning;
                        Mod.currentTNHInstance.manager.HoldPoints[i].NavBlockers.SetActive(false);
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_phaseIndex = 0;
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_maxPhases = 0;
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_curPhase = null;
                        Mod.currentTNHInstance.manager.HoldPoints[i].DeleteSystemNode();

                        // DeleteAllActiveEntities
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_activeTargets.Clear();
                        Mod.currentTNHInstance.manager.HoldPoints[i].DeleteAllActiveWarpIns();
                        Mod.currentTNHInstance.manager.HoldPoints[i].DeleteBarriers();
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_activeSosigs.Clear();
                        Mod.currentTNHInstance.manager.HoldPoints[i].m_activeTurrets.Clear();
                    }
                    for (int j = 0; j < Mod.currentTNHInstance.manager.SupplyPoints.Count; j++)
                    {
                        // DeleteAllActiveEntities
                        Mod.currentTNHInstance.manager.SupplyPoints[j].m_activeSosigs.Clear();
                        Mod.currentTNHInstance.manager.SupplyPoints[j].m_activeTurrets.Clear();
                        if (Mod.currentTNHInstance.manager.SupplyPoints[j].m_trackedObjects.Count > 0)
                        {
                            for (int i = Mod.currentTNHInstance.manager.SupplyPoints[j].m_trackedObjects.Count - 1; i >= 0; i--)
                            {
                                if (Mod.currentTNHInstance.manager.SupplyPoints[j].m_trackedObjects[i] != null)
                                {
                                    UnityEngine.Object.Destroy(Mod.currentTNHInstance.manager.SupplyPoints[j].m_trackedObjects[i]);
                                }
                            }
                        }
                        if (Mod.currentTNHInstance.manager.SupplyPoints[j].m_constructor != null)
                        {
                            Mod.currentTNHInstance.manager.SupplyPoints[j].m_constructor.GetComponent<TNH_ObjectConstructor>().ClearCase();
                            UnityEngine.Object.Destroy(Mod.currentTNHInstance.manager.SupplyPoints[j].m_constructor);
                            Mod.currentTNHInstance.manager.SupplyPoints[j].m_constructor = null;
                        }
                        if (Mod.currentTNHInstance.manager.SupplyPoints[j].m_panel != null)
                        {
                            UnityEngine.Object.Destroy(Mod.currentTNHInstance.manager.SupplyPoints[j].m_panel);
                            Mod.currentTNHInstance.manager.SupplyPoints[j].m_panel = null;
                        }
                    }
                    Mod.currentTNHInstance.manager.EnqueueLine(TNH_VoiceLineID.AI_ReturningToInterface);
                    GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, true);
                    Mod.currentTNHInstance.manager.ItemSpawner.SetActive(true);
                    Mod.currentTNHInstance.manager.ItemSpawner.transform.position = Mod.currentTNHInstance.manager.FinalItemSpawnerPoint.position;
                    Mod.currentTNHInstance.manager.ItemSpawner.transform.rotation = Mod.currentTNHInstance.manager.FinalItemSpawnerPoint.rotation;
                    Mod.currentTNHInstance.manager.DispatchScore();

                    return false;
                }

                Mod.currentTNHInstance.phase = TNH_Phase.Completed;
            }

            return true;
        }

        static void SetPhaseCompletePostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == GameManager.ID)
            {
                Mod.LogInfo("SetPhaseCompletePostfix, we are controller, sending", false);
                Mod.currentTNHInstance.Reset();

                if (ThreadManager.host)
                {
                    ServerSend.TNHSetPhaseComplete(Mod.currentTNHInstance.instance);
                }
                else
                {
                    ClientSend.TNHSetPhaseComplete(Mod.currentTNHInstance.instance);
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

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                // Call updates we don't want to skip
                // Make the call to delayed init, but the SetPhase(Take) call inside it will be blocked by the patch
                bool hadInit = Mod.currentTNHInstance.manager.m_hasInit;
                Mod.currentTNHInstance.manager.DelayedInit();
                if (Mod.currentTNHInstance.manager.BGAudioMode == TNH_BGAudioMode.Default)
                {
                    Mod.currentTNHInstance.manager.FMODController.Tick(Time.deltaTime);
                }

                // Update TAHReticle here because we would usually do it in Update_Hold and Update_Take
                if (Mod.currentTNHInstance.manager.RadarHand == TNH_RadarHand.Right)
                {
                    Mod.currentTNHInstance.manager.TAHReticle.transform.position = GM.CurrentPlayerBody.RightHand.position + GM.CurrentPlayerBody.RightHand.forward * -0.2f;
                }
                else
                {
                    Mod.currentTNHInstance.manager.TAHReticle.transform.position = GM.CurrentPlayerBody.LeftHand.position + GM.CurrentPlayerBody.LeftHand.forward * -0.2f;
                }

                Mod.currentTNHInstance.manager.VoiceUpdate();
                Mod.currentTNHInstance.manager.FMODController.SetMasterVolume(0.25f * GM.CurrentPlayerBody.GlobalHearing);

                // Test visited on the supply points
                if(Mod.currentTNHInstance.manager.Phase == TNH_Phase.Take)
                {
                    for (int i = 0; i < Mod.currentTNHInstance.manager.SupplyPoints.Count; i++)
                    {
                        Mod.currentTNHInstance.manager.SupplyPoints[i].TestVisited();
                    }
                }

                // Since we are not controller, our initialization will not complete fully due to lack of SetPhase
                // So here we check if the instance is initialized (phase not startup) and if we have to and are ready to init
                // Then init with the instance data
                if (Mod.currentTNHInstance.initializer != -1 && Mod.currentTNHInstance.initializer != GameManager.ID &&
                    Mod.currentTNHInstance.phase != TNH_Phase.StartUp && !hadInit &&
                    Mod.currentTNHInstance.manager.m_hasInit && Mod.currentTNHInstance.manager.AIManager.HasInit)
                {
                    Mod.LogInfo("TNH_Manager update, we were waiting for init and just got it, initing with data");
                    InitJoinTNH();
                }

                return false;
            }
            return true;
        }

        static bool DelayedInitPrefix(bool ___m_hasInit)
        {
            inDelayedInit = true;

            if (Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if (!___m_hasInit)
            {
                if (Mod.currentTNHInstance.initializer == -1 && Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("DelayedInitPrefix: No initializer yet and we are controller", false);
                    // Not yet init, we are controller
                    if (ThreadManager.host)
                    {
                        Mod.LogInfo("\tWe are server, taking initialization. Scene still loading?: " + GameManager.sceneLoading, false);
                        // We are server, init right away
                        Mod.currentTNHInstance.initializer = 0;
                        TNHInitializing = true;
                        return !GameManager.sceneLoading;
                    }
                    else if (!Mod.currentTNHInstance.initializationRequested) // Client, request initializtion if haven't already
                    {
                        Mod.LogInfo("\tWe are client and have not yet requested init perm, requesting", false);
                        ClientSend.RequestTNHInitialization(Mod.currentTNHInstance.instance);
                        Mod.currentTNHInstance.initializationRequested = true;
                    }
                    return false;
                }
                else if (Mod.currentTNHInstance.initializer == GameManager.ID)
                {
                    Mod.LogInfo("DelayedInitPrefix: We are initializer", false);
                    // We have an initializer, it's us
                    if (Mod.currentTNHInstance.initializationRequested)
                    {
                        Mod.LogInfo("\tWe were waiting for perm, initializing. Scene still loading?: " + GameManager.sceneLoading, false);
                        // This is the first call to DelayedInit since we received initialization perm, set initializing
                        Mod.currentTNHInstance.initializationRequested = false;
                        TNHInitializing = true;
                    }
                    return !GameManager.sceneLoading;
                }
                else // We are not initializer and no initializer or not controller
                {
                    Mod.LogInfo("DelayedInitPrefix: Waiting for initialization. Scene loading?: " + GameManager.sceneLoading + ", initializer: " + Mod.currentTNHInstance.initializer + ", requested: " + Mod.currentTNHInstance.initializationRequested, false);
                    // Wait until we have initializer before continuing
                    // Also check if not requested here because a server will set the initializer locally and set requested flag indicating that it is waiting for init
                    return !GameManager.sceneLoading && Mod.currentTNHInstance.initializer != -1 && !Mod.currentTNHInstance.initializationRequested;
                }
            }

            return true;
        }

        static void DelayedInitPostfix(bool ___m_hasInit)
        {
            // If we were initializing and we are done
            if (TNHInitializing && TNHInitialized)
            {
                Mod.LogInfo("DelayedInitPostfix: We were initializing and are now initialized, sending initializer", false);
                TNHInitializing = false;
                TNHInitialized = false;

                // Send to others
                if (ThreadManager.host)
                {
                    ServerSend.TNHInitializer(Mod.currentTNHInstance.instance, 0);
                }
                else
                {
                    ClientSend.TNHInitializer(Mod.currentTNHInstance.instance);
                }
            }

            inDelayedInit = false;
        }

        static bool OnShotFiredPrefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
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

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
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

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                return false;
            }
            return true;
        }

        public static void InitJoinTNH()
        {
            Mod.LogInfo("InitJoinTNH called", false);
            Mod.currentTNHInstance.manager.Phase = Mod.currentTNHInstance.phase;
            TNH_HoldPoint curHoldPoint = Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex];
            Mod.currentTNHInstance.manager.m_curHoldPoint = curHoldPoint;
            Mod.currentTNHInstance.manager.m_curHoldIndex = Mod.currentTNHInstance.curHoldIndex;
            Mod.currentTNHInstance.manager.m_activeSupplyPointIndicies = Mod.currentTNHInstance.activeSupplyPointIndices;
            if (Mod.currentTNHInstance.holdOngoing)
            {
                Mod.LogInfo("\tHold " + Mod.currentTNHInstance.curHoldIndex + " ongoing", false);
                // Set the hold
                TNH_Progression.Level curLevel = Mod.currentTNHInstance.manager.m_curLevel;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                ++TNH_HoldPointPatch.beginHoldSendSkip;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasActivated = true;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasInitiatedHold = true;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                --TNH_HoldPointPatch.beginHoldSendSkip;

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
                        point.m_curBarrier = curBarrier;
                        curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                        curBarrier.SetBarrierPoint(point);
                        point.SetCoverPointData(Mod.currentTNHInstance.raisedBarrierPrefabIndices[i]);
                    }
                }

                switch (Mod.currentTNHInstance.holdState)
                {
                    case TNH_HoldPoint.HoldState.Analyzing:
                        curHoldPoint.BeginAnalyzing();
                        for (int i = 0; i < Mod.currentTNHInstance.warpInData.Count; i += 2)
                        {
                            curHoldPoint.m_warpInTargets.Add(UnityEngine.Object.Instantiate<GameObject>(curHoldPoint.M.Prefab_TargetWarpingIn, Mod.currentTNHInstance.warpInData[i], Quaternion.Euler(Mod.currentTNHInstance.warpInData[i + 1])));
                        }
                        break;
                    case TNH_HoldPoint.HoldState.Transition:
                        SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, curHoldPoint.AUDEvent_HoldWave, curHoldPoint.transform.position);
                        UnityEngine.Object.Instantiate<GameObject>(curHoldPoint.VFX_HoldWave, curHoldPoint.m_systemNode.NodeCenter.position, curHoldPoint.m_systemNode.NodeCenter.rotation);
                        curHoldPoint.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
                        curHoldPoint.m_state = TNH_HoldPoint.HoldState.Transition;
                        curHoldPoint.LowerAllBarriers();
                        curHoldPoint.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
                        break;
                    case TNH_HoldPoint.HoldState.Hacking:
                        curHoldPoint.M.EnqueueEncryptionLine(TNH_EncryptionType.Static);
                        curHoldPoint.m_state = TNH_HoldPoint.HoldState.Hacking;
                        curHoldPoint.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);
                        break;
                }
            }
            else
            {
                Mod.LogInfo("\tNo hold ongoing", false);
                // Set the hold
                TNH_Progression.Level curLevel = Mod.currentTNHInstance.manager.m_curLevel;
                Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);

                Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);

                object level = null;
                if (PatchController.TNHTweakerAsmIdx > -1)
                {
                    object character = ((IDictionary)PatchController.TNHTweaker_LoadedTemplateManager_LoadedCharactersDict.GetValue(PatchController.TNHTweaker_LoadedTemplateManager))[GM.TNH_Manager.C];
                    level = PatchController.TNHTweaker_CustomCharacter_GetCurrentLevel.Invoke(character, new object[] { GM.TNH_Manager.m_curLevel });

                    ((List<int>)PatchController.TNHTweaker_TNHTweaker_SpawnedBossIndexes.GetValue(PatchController.TNHTweaker_TNHTweaker)).Clear();

                    // Like we do for vanilla, we don't clear if not controller, we will just set it to the list in the TNH instance
                    //__instance.m_activeSupplyPointIndicies.Clear();

                    PatchController.TNHTweaker_TNHTweaker_PreventOutfitFunctionality.SetValue(PatchController.TNHTweaker_TNHTweaker, PatchController.TNHTweaker_CustomCharacter_ForceDisableOutfitFunctionality.GetValue(character));
                }

                //  Set supply points
                bool spawnToken = true;
                int panelIndex = 0;
                if (PatchController.TNHTweakerAsmIdx > -1)
                {
                    if (Mod.currentTNHInstance.manager.m_curPointSequence.UsesExplicitSingleSupplyPoints)
                    {
                        for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                        {
                            TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                            // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                            PatchController.TNHTweaker_TNHPatches_ConfigureSupplyPoint.Invoke(PatchController.TNHTweaker_TNHPatches, new object[] { tnh_SupplyPoint, level, TNH_SupplyPoint.SupplyPanelType.All });
                            TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                            tnh_SupplyPoint.SetContact(contact);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                        {
                            TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];

                            PatchController.TNHTweaker_TNHPatches_ConfigureSupplyPoint.Invoke(PatchController.TNHTweaker_TNHPatches, new object[] { tnh_SupplyPoint, level, panelIndex });

                            TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                            tnh_SupplyPoint.SetContact(contact);
                        }
                    }
                }
                else
                {
                    if (Mod.currentTNHInstance.manager.m_curPointSequence.UsesExplicitSingleSupplyPoints)
                    {
                        for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                        {
                            TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];
                            // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                            tnh_SupplyPoint.Configure(curLevel.SupplyChallenge, false, false, true, TNH_SupplyPoint.SupplyPanelType.All, 0, 0, true);
                            TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                            tnh_SupplyPoint.SetContact(contact);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Mod.currentTNHInstance.activeSupplyPointIndices.Count; ++i)
                        {
                            TNH_SupplyPoint tnh_SupplyPoint = Mod.currentTNHInstance.manager.SupplyPoints[Mod.currentTNHInstance.activeSupplyPointIndices[i]];

                            int num6 = i;
                            if (i > 0)
                            {
                                num6 = panelIndex;
                                panelIndex++;
                                if (panelIndex > 2)
                                {
                                    panelIndex = 1;
                                }
                            }
                            TNH_SupplyPoint.SupplyPanelType panelType = (TNH_SupplyPoint.SupplyPanelType)num6;
                            // Here we pass false to spawn sosigs,turrets, and 0 for max boxes because since we are not controller we do not want to spawn those ourselves
                            tnh_SupplyPoint.Configure(curLevel.SupplyChallenge, false, false, true, panelType, 0, 0, spawnToken);
                            spawnToken = false;
                            TAH_ReticleContact contact = Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(tnh_SupplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                            tnh_SupplyPoint.SetContact(contact);
                        }
                    }
                }

                // Spawn at initial supply point
                // We will already have been TPed to our char's starting point by delayed init
                // Now check if valid, if not find first player to spawn on
                if (Mod.currentTNHInstance.activeSupplyPointIndices != null)
                {
                    if (Mod.currentTNHInstance.activeSupplyPointIndices.Contains(Mod.currentTNHInstance.manager.m_curPointSequence.StartSupplyPointIndex))
                    {
                        // Starting supply point active, find a player to spawn on
                        bool found = false;
                        if (Mod.currentTNHInstance.currentlyPlaying != null && Mod.currentTNHInstance.currentlyPlaying.Count > 0)
                        {
                            for (int i = 0; i < Mod.currentTNHInstance.currentlyPlaying.Count; ++i)
                            {
                                if (Mod.currentTNHInstance.currentlyPlaying[i] != GameManager.ID)
                                {
                                    Mod.TNHSpawnPoint = GameManager.players[Mod.currentTNHInstance.currentlyPlaying[i]].transform.position;
                                    GM.CurrentMovementManager.TeleportToPoint(Mod.TNHSpawnPoint, true);
                                    Mod.LogInfo("\t\tSpawning on player " + i, false);
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            // Look through all possible supply points, spawn at first one that isn't active
                            for (int i = 0; i < Mod.currentTNHInstance.manager.SupplyPoints.Count; ++i)
                            {
                                if (!Mod.currentTNHInstance.activeSupplyPointIndices.Contains(i))
                                {
                                    Mod.TNHSpawnPoint = Mod.currentTNHInstance.manager.SupplyPoints[i].SpawnPoint_PlayerSpawn.position;
                                    GM.CurrentMovementManager.TeleportToPoint(Mod.TNHSpawnPoint, true);
                                    Mod.LogInfo("\t\tSpawning on inactive supply point: " + i, false);
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                        {
                            Mod.TNHSpawnPoint = GM.CurrentPlayerBody.transform.position;
                            Mod.LogWarning("Not valid supply point or player to spawn on, spawning on default start point, which might be active");
                        }
                    }
                }
            }

            // If this is the first time we join this game, give the player a button 
            // with which they can spawn their own starting equipment
            if (!Mod.currentTNHInstance.spawnedStartEquip && Mod.TNHStartEquipButton == null)
            {
                Mod.LogInfo("\tSpawning init start button", false);
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
                // We want to get inital equipment but there are already players in game, spawn init equip spawn button instead
                if (!Mod.currentTNHInstance.spawnedStartEquip && Mod.TNHStartEquipButton == null)
                {
                    Mod.LogInfo("InitBeginEquipPrefix Spawning init start button", false);
                    Mod.TNHStartEquipButton = GameObject.Instantiate(Mod.TNHStartEquipButtonPrefab, GM.CurrentPlayerBody.Head);
                    Mod.TNHStartEquipButton.transform.GetChild(0).GetComponent<FVRPointableButton>().Button.onClick.AddListener(Mod.OnTNHSpawnStartEquipClicked);
                }

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

        static void GeneratePatrolPrefix()
        {
            if (Mod.managerObject != null)
            {
                inGeneratePatrol = true;
                patrolIndex++;
                List<int> list = new List<int>();
                int i = 0;
                int num = 0;
                while (i < 5)
                {
                    int item = UnityEngine.Random.Range(0, GM.TNH_Manager.HoldPoints.Count);
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
                    patrolPoints.Add(GM.TNH_Manager.HoldPoints[list[j]].SpawnPoints_Sosigs_Defense[UnityEngine.Random.Range(0, GM.TNH_Manager.HoldPoints[list[j]].SpawnPoints_Sosigs_Defense.Count)].position);
                }
            }
        }

        static void GeneratePatrolPostfix()
        {
            inGeneratePatrol = false;
        }

        static void SpawnEnemySosigPrefix()
        {
            if (Mod.managerObject != null)
            {
                Mod.LogWarning("Manager spawned enemy sosig: " + Environment.StackTrace);
            }
        }

        // Patches ObjectCleanupInHold to prevent destruction of objects we do not control
        static bool ObjectCleanupInHoldPrefix(TNH_Manager __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.m_knownObjs.Count <= 0)
            {
                return false;
            }
            __instance.knownObjectCheckIndex++;
            if (__instance.knownObjectCheckIndex >= __instance.m_knownObjs.Count)
            {
                __instance.knownObjectCheckIndex = 0;
            }
            if (__instance.m_knownObjs[__instance.knownObjectCheckIndex] == null)
            {
                __instance.m_knownObjsHash.Remove(__instance.m_knownObjs[__instance.knownObjectCheckIndex]);
                __instance.m_knownObjs.RemoveAt(__instance.knownObjectCheckIndex);
            }
            else
            {
                FVRPhysicalObject fvrphysicalObject = __instance.m_knownObjs[__instance.knownObjectCheckIndex];
                Vector3 position = fvrphysicalObject.transform.position;
                if (!__instance.m_curHoldPoint.IsPointInBounds(position))
                {
                    float num = Vector3.Distance(position, GM.CurrentPlayerBody.transform.position);
                    if (num > 10f)
                    {
                        if (fvrphysicalObject is PinnedGrenade 
                            || fvrphysicalObject is FVRGrenade 
                            || fvrphysicalObject is FVRCappedGrenade 
                            || fvrphysicalObject is Camcorder 
                            || fvrphysicalObject is SLAM 
                            || fvrphysicalObject is FVRPivotLocker 
                            || (fvrphysicalObject is SosigWeaponPlayerInterface 
                                && (fvrphysicalObject as SosigWeaponPlayerInterface).W.Type == SosigWeapon.SosigWeaponType.Grenade))
                        {
                            return false;
                        }

                        // Will prevent destruction if not controller and if within 10m of controller
                        TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(fvrphysicalObject, out trackedItem) ? trackedItem : fvrphysicalObject.GetComponent<TrackedItem>();
                        if (trackedItem != null
                            && trackedItem.data.controller != GameManager.ID
                            && (!GameManager.players.TryGetValue(trackedItem.data.controller, out PlayerManager playerManager) 
                                || Vector3.Distance(position, playerManager.head.position) <= 10f))
                        {
                            return false;
                        }

                        __instance.m_knownObjsHash.Remove(__instance.m_knownObjs[__instance.knownObjectCheckIndex]);
                        UnityEngine.Object.Destroy(__instance.m_knownObjs[__instance.knownObjectCheckIndex].gameObject);
                        __instance.m_knownObjs.RemoveAt(__instance.knownObjectCheckIndex);
                    }
                }
            }

            return false;
        }
    }

    // Patches TNH_HoldPoint to keep track of hold point events
    public class TNH_HoldPointPatch
    {
        public static bool spawnEntitiesSkip;
        public static int beginHoldSendSkip;
        public static int beginPhaseSkip;

        public static bool inSpawnEnemyGroup;
        public static bool inSpawnTurrets;

        static bool UpdatePrefix(ref TNH_HoldPoint __instance, bool ___m_isInHold, ref TNH_HoldPointSystemNode ___m_systemNode, ref bool ___m_hasPlayedTimeWarning1, ref bool ___m_hasPlayedTimeWarning2,
                                 ref int ___m_numWarnings)
        {
            // Skip if connected, have TNH instance, and we are not controller
            if (Mod.managerObject != null && ___m_isInHold && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                try
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
                            }
                            if (!___m_hasPlayedTimeWarning2 && Mod.currentTNHInstance.tickDownToFailure < 30f)
                            {
                                ___m_hasPlayedTimeWarning2 = true;
                                __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Reminder2);
                                ___m_numWarnings++;
                            }
                            ___m_systemNode.SetDisplayString("FAILURE IN: " + __instance.FloatToTime(Mod.currentTNHInstance.tickDownToFailure, "0:00.00"));
                            break;
                        case TNH_HoldPoint.HoldState.Transition:
                            if (___m_systemNode != null)
                            {
                                ___m_systemNode.SetDisplayString("SCANNING SYSTEM");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Mod.LogError("Caught " + ex.Message + "\nIn Holdpoint patch update prefix\nDebug:");
                    Mod.LogError("Hold state: " + Mod.currentTNHInstance.holdState);
                    Mod.LogError("Instance M null?: " + (__instance.M == null));
                    Mod.LogError("Sys node null?: " + (___m_systemNode == null));
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
                    if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                        (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
                    {
                        Mod.LogInfo("ConfigureAsSystemNodePrefix and we init or control, setting and sending", false);
                        int holdPointIndex = -1;
                        for (int i = 0; i < __instance.M.HoldPoints.Count; ++i)
                        {
                            if (__instance.M.HoldPoints[i] == __instance)
                            {
                                holdPointIndex = i;
                                break;
                            }
                        }

                        // Update locally
                        Mod.currentTNHInstance.curHoldIndex = holdPointIndex;
                        Mod.currentTNHInstance.level = GM.TNH_Manager.m_level;

                        if (ThreadManager.host)
                        {
                            ServerSend.TNHHoldPointSystemNode(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.level, holdPointIndex);
                        }
                        else
                        {

                            ClientSend.TNHHoldPointSystemNode(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.level, holdPointIndex);
                        }
                    }
                    else
                    {
                        Mod.LogInfo("ConfigureAsSystemNodePrefix and we DO NOT init or control, skipping next entities spawn", false);
                        spawnEntitiesSkip = true;
                    }
                }
            }
        }

        static bool SpawnTakeChallengeEntitiesPrefix()
        {
            if (spawnEntitiesSkip)
            {
                Mod.LogInfo("SpawnTakeChallengeEntitiesPrefix skipped", false);
                spawnEntitiesSkip = false;
                return false;
            }
            Mod.LogInfo("SpawnTakeChallengeEntitiesPrefix not being skipped", false);

            return true;
        }

        static bool BeginHoldPrefix(TNH_HoldPoint __instance, List<Sosig> ___m_activeSosigs, List<AutoMeater> ___m_activeTurrets, List<TNH_EncryptionTarget> ___m_activeTargets,
                                    ref int ___m_phaseIndex, ref int ___m_maxPhases, ref bool ___m_isInHold, ref int ___m_numWarnings)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("BeginHoldPrefix", false);
                // Update locally
                Mod.currentTNHInstance.holdOngoing = true;
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller", false);
                    if (beginHoldSendSkip == 0)
                    {
                        Mod.LogInfo("\t\tNot skipped sending", false);
                        // Send update
                        if (ThreadManager.host)
                        {
                            ServerSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, true, true, 0);
                        }
                        else
                        {
                            ClientSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, true);
                        }
                    }
                }
                else
                {
                    Mod.LogInfo("\tWe are not controller", false);

                    if (beginHoldSendSkip == 0)
                    {
                        Mod.LogInfo("\t\tNot skipped, sending to controller: "+ Mod.currentTNHInstance.controller, false);
                        if (ThreadManager.host)
                        {
                            ServerSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, false, false, Mod.currentTNHInstance.controller);
                        }
                        else
                        {
                            ClientSend.TNHHoldBeginChallenge(Mod.currentTNHInstance.instance, false);
                        }
                    }
                    else // Told to skip, begin hold was an order from controller, prepare for data
                    {
                        Mod.LogInfo("\t\tSkipped, prepping", false);
                        // Score
                        if (!Mod.currentTNHInstance.manager.HasGuardBeenKilledThatWasAltered())
                        {
                            Mod.currentTNHInstance.manager.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeHoldPointTakenClean, 1);
                        }
                        if (!Mod.currentTNHInstance.manager.HasPlayerAlertedSecurityThisPhase())
                        {
                            Mod.currentTNHInstance.manager.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeCompleteNoAlert, 1);
                        }
                        if (!Mod.currentTNHInstance.manager.HasPlayerTakenDamageThisPhase())
                        {
                            Mod.currentTNHInstance.manager.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeCompleteNoDamage, 1);
                        }

                        // Deletion burst
                        ___m_activeSosigs.Clear();
                        Mod.currentTNHInstance.manager.m_miscEnemies.Clear();

                        Mod.currentTNHInstance.manager.ClearGuards();
                        Mod.currentTNHInstance.manager.ResetAlertedThisPhase();
                        Mod.currentTNHInstance.manager.ResetPlayerTookDamageThisPhase();
                        Mod.currentTNHInstance.manager.ResetHasGuardBeenKilledThatWasAltered();

                        // DeleteAllActiveEntities
                        ___m_activeTargets.Clear();
                        __instance.DeleteAllActiveWarpIns();
                        __instance.DeleteBarriers();
                        //(Mod.TNH_HoldPoint_m_activeSosigs.GetValue(__instance) as List<Sosig>).Clear(); Done in deletion burst
                        ___m_activeTurrets.Clear();

                        __instance.NavBlockers.SetActive(true);
                        ___m_phaseIndex = 0;
                        ___m_maxPhases = __instance.H.Phases.Count;
                        __instance.M.EnqueueLine(TNH_VoiceLineID.BASE_IntrusionDetectedInitiatingLockdown);
                        __instance.M.EnqueueLine(TNH_VoiceLineID.AI_InterfacingWithSystemNode);
                        __instance.M.EnqueueLine(TNH_VoiceLineID.BASE_ResponseTeamEnRoute);
                        ___m_isInHold = true;
                        ___m_numWarnings = 0;
                    }

                    return false;
                }
            }

            return true;
        }

        static void BeginPhasePostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller == GameManager.ID)
            {
                Mod.LogInfo("BeginPhasePostfix and we cotnrol, sending", false);
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (ThreadManager.host)
                {
                    ServerSend.TNHHoldPointBeginPhase(Mod.currentTNHInstance.instance);
                }
                else
                {
                    ClientSend.TNHHoldPointBeginPhase(Mod.currentTNHInstance.instance);
                }
            }
        }

        static bool RaiseRandomBarriersPrefix(int howMany)
        {
            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            // It will also prevent raising barriers if we are not the controller of the instance
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("RaiseRandomBarriersPrefix", false);
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, raising manually", false);
                    int num = howMany;
                    List<int> indices = new List<int>();
                    for (int i = 0; i < Mod.currentTNHInstance.manager.m_curHoldPoint.BarrierPoints.Count; ++i)
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
                        Mod.currentTNHInstance.manager.m_curHoldPoint.BarrierPoints[index].SpawnRandomBarrier();

                        // Set the list in TNHInstance, which will be sent alongside begin hold
                        Mod.currentTNHInstance.raisedBarriers.Add(index);
                    }
                }
                else // Not controller, use instance data
                {
                    Mod.LogInfo("\tWe are not controller, raising from data", false);
                    for (int i = 0; i < Mod.currentTNHInstance.raisedBarriers.Count; ++i)
                    {
                        TNH_DestructibleBarrierPoint point = Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BarrierPoints[Mod.currentTNHInstance.raisedBarriers[i]];
                        TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[Mod.currentTNHInstance.raisedBarrierPrefabIndices[i]];
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                        TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                        point.m_curBarrier = curBarrier;
                        curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                        curBarrier.SetBarrierPoint(point);
                        point.SetCoverPointData(Mod.currentTNHInstance.raisedBarrierPrefabIndices[i]);
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
                Mod.LogInfo("RaiseRandomBarriersPostfix", false);
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldPointRaiseBarriers(0, Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
                    }
                    else
                    {
                        ClientSend.TNHHoldPointRaiseBarriers(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.raisedBarriers, Mod.currentTNHInstance.raisedBarrierPrefabIndices);
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
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("BarrierSetCoverPointDataPrefix", false);
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, collecting index", false);
                    if (Mod.currentTNHInstance.raisedBarrierPrefabIndices == null)
                    {
                        Mod.currentTNHInstance.raisedBarrierPrefabIndices = new List<int>();
                    }
                    Mod.currentTNHInstance.raisedBarrierPrefabIndices.Add(index);
                }
            }
        }

        static void BeginAnalyzingPostfix(ref TNH_HoldPoint __instance, ref List<GameObject> ___m_warpInTargets, float ___m_tickDownToIdentification)
        {
            // This patch will prevent BarrierPoints from being shuffled so barriers can be identified across clients
            // It will also prevent raising barriers if we are not the controller of the instance
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("BeginAnalyzingPostfix", false);
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    // Build data list
                    Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Analyzing;
                    Mod.currentTNHInstance.warpInData = new List<Vector3>();
                    foreach (GameObject target in ___m_warpInTargets)
                    {
                        if (target != null)
                        {
                            Mod.currentTNHInstance.warpInData.Add(target.transform.position);
                            Mod.currentTNHInstance.warpInData.Add(target.transform.rotation.eulerAngles);
                        }
                    }

                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldPointBeginAnalyzing(0, Mod.currentTNHInstance.instance, Mod.currentTNHInstance.warpInData, ___m_tickDownToIdentification);
                    }
                    else
                    {
                        ClientSend.TNHHoldPointBeginAnalyzing(Mod.currentTNHInstance.instance, Mod.currentTNHInstance.warpInData, ___m_tickDownToIdentification);
                    }
                }
            }
        }

        static bool CompletePhasePrefix(TNH_HoldPoint __instance, List<Sosig> ___m_activeSosigs, TNH_HoldPointSystemNode ___m_systemNode, ref int ___m_phaseIndex,
                                        ref TNH_HoldPoint.HoldState ___m_state, ref float ___m_tickDownTransition)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("CompletePhasePrefix", false);
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Transition;

                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldCompletePhase(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.TNHHoldCompletePhase(Mod.currentTNHInstance.instance);
                    }
                }
                else
                {
                    Mod.LogInfo("\tWe are not controller, using data", false);
                    // Deletion burst
                    ___m_activeSosigs.Clear();
                    Mod.currentTNHInstance.manager.m_miscEnemies.Clear();

                    SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, __instance.AUDEvent_HoldWave, __instance.transform.position);
                    UnityEngine.Object.Instantiate<GameObject>(__instance.VFX_HoldWave, ___m_systemNode.NodeCenter.position, ___m_systemNode.NodeCenter.rotation);
                    Mod.currentTNHInstance.manager.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
                    Mod.currentTNHInstance.manager.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldDecisecondsRemaining, (int)(__instance.m_tickDownToFailure * 10f));
                    ++___m_phaseIndex;
                    ___m_state = TNH_HoldPoint.HoldState.Transition;
                    ___m_tickDownTransition = 5f;
                    __instance.LowerAllBarriers();
                    if (!__instance.m_hasBeenDamagedThisPhase)
                    {
                        Mod.currentTNHInstance.manager.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
                    }
                    ___m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);

                    return false;
                }
            }

            return true;
        }

        static bool SpawnTargetGroupPrefix()
        {
            // Skip if connected, have TNH instance, and we are not controller
            return Mod.managerObject == null || Mod.currentTNHInstance == null || Mod.currentTNHInstance.controller == GameManager.ID;
        }

        static void IdentifyEncryptionPostfix(TNH_HoldPoint __instance)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("IdentifyEncryptionPostfix", false);
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Hacking;

                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldIdentifyEncryption(0, Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.TNHHoldIdentifyEncryption(Mod.currentTNHInstance.instance);
                    }
                }
                else
                {
                    Mod.LogInfo("\tWe are not controller, deletign warp ins", false);
                    // Delete all active warpins here because IdentifyEncryption calls SpawnTargetGroup which usually calls delete warpins
                    // but the call will be blocked by non controllers, just do it here for convenience
                    __instance.DeleteAllActiveWarpIns();
                }
            }
        }

        static bool SpawnWarpInMarkersPrefix()
        {
            // Skip if connected, have TNH instance, and we are not controller
            return Mod.managerObject == null || Mod.currentTNHInstance == null || Mod.currentTNHInstance.controller == GameManager.ID;
        }

        static void FailOutPrefix()
        {
            // Note that this is a prefix, sending failout to non controlelrs will be done before we make the HoldPointCompleted->SetLevel call
            // inside failout. So non controllers are going to call their own failout which will increment their level
            // but it will be overriden by the controller sending a SetLevel directly after this
            // If it were a postfix instead, non controllers would end up with an incorrect m_level
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("FailOutPrefix", false);
                Mod.currentTNHInstance.holdOngoing = false;
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldPointFailOut(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.TNHHoldPointFailOut(Mod.currentTNHInstance.instance);
                    }
                }
            }
        }

        static bool ShutDownHoldPointPrefix(TNH_HoldPoint __instance, List<Sosig> ___m_activeSosigs, List<AutoMeater> ___m_activeTurrets, List<TNH_EncryptionTarget> ___m_activeTargets,
                                            ref TNH_HoldPoint.HoldState ___m_state, ref int ___m_phaseIndex, ref int ___m_maxPhases, ref bool ___m_isInHold,
                                            ref TNH_HoldChallenge.Phase ___m_curPhase, List<GameObject> ___m_warpInTargets)
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
            {
                Mod.LogInfo("ShutDownHoldPointPrefix and not controller, shutting down", false);
                ___m_isInHold = false;
                ___m_state = TNH_HoldPoint.HoldState.Beginning;
                __instance.NavBlockers.SetActive(false);
                ___m_phaseIndex = 0;
                ___m_maxPhases = 0;
                ___m_curPhase = null;
                __instance.DeleteSystemNode();
                ___m_activeTargets.Clear();
                __instance.DeleteAllActiveWarpIns();
                ___m_warpInTargets.Clear();
                ___m_activeSosigs.Clear();
                ___m_activeTurrets.Clear();
                __instance.LowerAllBarriers();

                return false;
            }

            return true;
        }

        static void CompleteHoldPrefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                Mod.LogInfo("CompleteHoldPrefix", false);
                Mod.currentTNHInstance.holdOngoing = false;
                Mod.currentTNHInstance.holdState = TNH_HoldPoint.HoldState.Beginning;

                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    Mod.LogInfo("\tWe are controller, sending", false);
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHoldPointCompleteHold(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.TNHHoldPointCompleteHold(Mod.currentTNHInstance.instance);
                    }
                }
                else
                {
                    Mod.LogInfo("\tWe are not controller, skipping token gain", false);
                    ++TNH_ManagerPatch.completeTokenSkip;
                }
            }
        }

        static void CompleteHoldPostfix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null && Mod.currentTNHInstance.controller != GameManager.ID)
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

        static bool DeletionBurstPrefix()
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            return Mod.currentTNHInstance == null || (Mod.currentTNHInstance.controller == GameManager.ID);
        }

        static bool DeleteAllActiveEntitiesPrefix()
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            return Mod.currentTNHInstance == null || (Mod.currentTNHInstance.controller == GameManager.ID);
        }

        static bool DeleteAllActiveTargetsPrefix()
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            return Mod.currentTNHInstance == null || (Mod.currentTNHInstance.controller == GameManager.ID);
        }

        static bool DeleteSosigsPrefix()
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            return Mod.currentTNHInstance == null || (Mod.currentTNHInstance.controller == GameManager.ID);
        }

        static bool DeleteTurretsPrefix()
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            return Mod.currentTNHInstance == null || (Mod.currentTNHInstance.controller == GameManager.ID);
        }

        static void SpawnSystemNodePrefix()
        {
            if (Mod.managerObject != null)
            {
                Mod.LogWarning("SpawnSystemNodePrefix called: " + Environment.StackTrace);
            }
        }
    }

    class TNH_SupplyPointPatch
    {
        public static bool inSpawnTakeEnemyGroup;
        public static bool inSpawnDefenses;
        public static bool inSpawnBoxes;
        public static int supplyPointIndex;

        static bool SpawnTakeEnemyGroupPrefix(TNH_SupplyPoint __instance)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("SpawnTakeEnemyGroupPrefix and we init or control", false);
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

                return true;
            }
            return false;
        }

        static void SpawnTakeEnemyGroupPostfix()
        {
            inSpawnTakeEnemyGroup = false;
        }

        static bool SpawnDefensesPrefix(TNH_SupplyPoint __instance)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("SpawnDefensesPrefix and we init or control", false);
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

                return true;
            }
            return false;
        }

        static void SpawnDefensesPostfix()
        {
            inSpawnDefenses = false;
        }

        static bool SpawnBoxesPrefix(TNH_SupplyPoint __instance)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("SpawnBoxesPrefix and we init or control", false);
                inSpawnBoxes = true;
                supplyPointIndex = -1;
                for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
                {
                    if (__instance == GM.TNH_Manager.SupplyPoints[i])
                    {
                        supplyPointIndex = i;
                        break;
                    }
                }

                return true;
            }
            return false;
        }

        static void SpawnBoxesPostfix()
        {
            inSpawnBoxes = false;
        }

        static bool TNHTweaker_SpawnTakeEnemyGroupPrefix(TNH_SupplyPoint point)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("TNHTweaker_SpawnTakeEnemyGroupPrefix and we init or control", false);
                inSpawnTakeEnemyGroup = true;
                supplyPointIndex = -1;
                for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
                {
                    if (point == GM.TNH_Manager.SupplyPoints[i])
                    {
                        supplyPointIndex = i;
                        break;
                    }
                }

                return true;
            }
            return false;
        }

        static void TNHTweaker_SpawnTakeEnemyGroupPostfix()
        {
            inSpawnTakeEnemyGroup = false;
        }

        static bool TNHTweaker_SpawnDefensesPrefix(TNH_SupplyPoint point)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("TNHTweaker_SpawnDefensesPrefix and we init or control", false);
                inSpawnDefenses = true;
                supplyPointIndex = -1;
                for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
                {
                    if (point == GM.TNH_Manager.SupplyPoints[i])
                    {
                        supplyPointIndex = i;
                        break;
                    }
                }

                return true;
            }
            return false;
        }

        static void TNHTweaker_SpawnDefensesPostfix()
        {
            inSpawnDefenses = false;
        }

        static bool TNHTweaker_SpawnBoxesPrefix(TNH_SupplyPoint point)
        {
            if (Mod.managerObject == null || Mod.currentTNHInstance == null)
            {
                return true;
            }
            else if ((TNH_ManagerPatch.inDelayedInit && Mod.currentTNHInstance.initializer == GameManager.ID) ||
                    (Mod.currentTNHInstance.controller == GameManager.ID && !TNH_ManagerPatch.inDelayedInit))
            {
                Mod.LogInfo("TNHTweaker_SpawnBoxesPrefix and we init or control", false);
                inSpawnBoxes = true;
                supplyPointIndex = -1;
                for (int i = 0; i < GM.TNH_Manager.SupplyPoints.Count; ++i)
                {
                    if (point == GM.TNH_Manager.SupplyPoints[i])
                    {
                        supplyPointIndex = i;
                        break;
                    }
                }

                return true;
            }
            return false;
        }

        static void TNHTweaker_SpawnBoxesPostfix()
        {
            inSpawnBoxes = false;
        }
    }

    // Patches TAH_ReticleContact to enable display of friendlies
    class TAHReticleContactPatch
    {
        static bool SetContactTypePrefix(ref TAH_ReticleContact __instance, TAH_ReticleContact.ContactType t)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            int index = (int)t;
            // -2: Friendly, -3: Enemy, -4: colorIndex 0, -5: colorIndex 1, ...
            if (index <= -2) // Player
            {
                if (__instance.Type != t)
                {
                    __instance.Type = t;
                    __instance.M_Arrow.mesh = __instance.Meshes_Arrow[(int)TAH_ReticleContact.ContactType.Enemy];
                    __instance.M_Icon.mesh = __instance.Meshes_Icon[(int)TAH_ReticleContact.ContactType.Enemy];
                    __instance.R_Arrow.material = Mod.reticleFriendlyContactArrowMat;
                    __instance.R_Icon.material = Mod.reticleFriendlyContactIconMat;

                    __instance.R_Arrow.material.color = index == -2 ? Color.green : (index == -3 ? Color.red : GameManager.colors[Mathf.Abs(index) - 4]);
                    __instance.R_Icon.material.color = index == -2 ? Color.green : (index == -3 ? Color.red : GameManager.colors[Mathf.Abs(index) - 4]);
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Brfalse)
                {
                    toInsert.Add(new CodeInstruction(OpCodes.Brtrue, instruction.operand)); // If true jump to same label as if first if statement is false
                    toInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
                    toInsert.Add(new CodeInstruction(OpCodes.Ret)); // Return
                }
                if (instruction.opcode == OpCodes.Ret)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("TAHReticleContactPatch TickTranspiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches TNH_WeaponCrate.Update to know when the case is open so we can put a timed destroyer on it if necessary
    class TNHWeaponCrateSpawnObjectsPatch
    {
        static void SpawnObjectsRawPrefix(ref TNH_WeaponCrate __instance)
        {
            if (Mod.managerObject != null)
            {
                TimerDestroyer destroyer = __instance.GetComponent<TimerDestroyer>();
                if (destroyer != null)
                {
                    destroyer.triggered = true;
                }
            }
        }
    }

    // Patches SceneLoader.LoadMG to know when we want to start loading into a TNH game
    class SceneLoaderPatch
    {
        static bool LoadMGPrefix(SceneLoader __instance)
        {
            // If we are in a TNH instance hosted by a spectator host but spectator host is not yet in the game
            if (Mod.managerObject != null && !GameManager.spectatorHost && !GameManager.sceneLoading && GameManager.scene.Equals("TakeAndHold_Lobby_2") && 
                Mod.currentTNHInstance != null && Mod.currentTNHInstance.playerIDs.Count > 0 && GameManager.spectatorHosts.Contains(Mod.currentTNHInstance.playerIDs[0]) &&
                !Mod.currentTNHInstance.currentlyPlaying.Contains(Mod.currentTNHInstance.playerIDs[0]))
            {
                // Tell them to go start game and skip original
                if (ThreadManager.host)
                {
                    ServerSend.SpectatorHostStartTNH(Mod.currentTNHInstance.playerIDs[0]);
                }
                else
                {
                    ClientSend.SpectatorHostStartTNH(Mod.currentTNHInstance.playerIDs[0]);
                }

                __instance.gameObject.SetActive(false);

                Mod.TNHMenuPages[4].SetActive(false);
                Mod.TNHMenuPages[6].SetActive(true);

                Mod.waitingForTNHGameStart = true;

                return false;
            }

            return true;
        }
    }
}
