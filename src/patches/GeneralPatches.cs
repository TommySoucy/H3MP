using FistVR;
using H3MP.Networking;
using H3MP.Scripts;
using H3MP.Tracking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace H3MP.Patches
{
    public class GeneralPatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // LoadLevelBeginPatch
            MethodInfo loadLevelBeginPatchOriginal = typeof(SteamVR_LoadLevel).GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);
            MethodInfo loadLevelBeginPatchPrefix = typeof(LoadLevelBeginPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(loadLevelBeginPatchOriginal, harmony, true);
            harmony.Patch(loadLevelBeginPatchOriginal, new HarmonyMethod(loadLevelBeginPatchPrefix));

            ++patchIndex; // 1

            // KinematicPatch
            MethodInfo kinematicPatchOriginal = typeof(Rigidbody).GetMethod("set_isKinematic", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo kinematicPatchPrefix = typeof(KinematicPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(kinematicPatchOriginal, harmony, true);
            harmony.Patch(kinematicPatchOriginal, new HarmonyMethod(kinematicPatchPrefix));

            ++patchIndex; // 2

            // PhysicalObjectRBPatch
            MethodInfo physicalObjectRBOriginal = typeof(FVRPhysicalObject).GetMethod("RecoverRigidbody", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo physicalObjectRBPostfix = typeof(PhysicalObjectRBPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(physicalObjectRBOriginal, harmony, true);
            harmony.Patch(physicalObjectRBOriginal, null, new HarmonyMethod(physicalObjectRBPostfix));

            ++patchIndex; // 3

            // SetPlayerIFFPatch
            MethodInfo setPlayerIFFPatchOriginal = typeof(FVRPlayerBody).GetMethod("SetPlayerIFF", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setPlayerIFFPatchPrefix = typeof(SetPlayerIFFPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(setPlayerIFFPatchOriginal, harmony, false);
            harmony.Patch(setPlayerIFFPatchOriginal, new HarmonyMethod(setPlayerIFFPatchPrefix));

            ++patchIndex; // 4

            // WristMenuPatch
            MethodInfo wristMenuPatchUpdateOriginal = typeof(FVRWristMenu2).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchUpdatePrefix = typeof(WristMenuPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo wristMenuPatchAwakeOriginal = typeof(FVRWristMenu2).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo wristMenuPatchAwakePrefix = typeof(WristMenuPatch).GetMethod("AwakePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(wristMenuPatchUpdateOriginal, harmony, true);
            PatchController.Verify(wristMenuPatchAwakeOriginal, harmony, true);
            harmony.Patch(wristMenuPatchUpdateOriginal, new HarmonyMethod(wristMenuPatchUpdatePrefix));
            harmony.Patch(wristMenuPatchAwakeOriginal, new HarmonyMethod(wristMenuPatchAwakePrefix));

            ++patchIndex; // 5

            // GMInitScenePatch
            MethodInfo GMInitScenePatchOriginal = typeof(GM).GetMethod("InitScene", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GMInitScenePatchPostfix = typeof(GMInitScenePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(GMInitScenePatchOriginal, harmony, true);
            harmony.Patch(GMInitScenePatchOriginal, null, new HarmonyMethod(GMInitScenePatchPostfix));

            ++patchIndex; // 6

            // TNH_ScoreDisplayReloadPatch
            MethodInfo TNH_ScoreDisplayReloadPatchOriginal = typeof(TNH_ScoreDisplay).GetMethod("ReloadLevel", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ScoreDisplayReloadPatchPrefix = typeof(TNH_ScoreDisplayReloadPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ScoreDisplayReloadPatchOriginal, harmony, true);
            harmony.Patch(TNH_ScoreDisplayReloadPatchOriginal, new HarmonyMethod(TNH_ScoreDisplayReloadPatchPrefix));

            ++patchIndex; // 7

            // SetCurrentAIManagerPatch
            MethodInfo SetCurrentAIManagerPatchOriginal = typeof(GM).GetMethod("set_CurrentAIManager", BindingFlags.Public | BindingFlags.Static);
            MethodInfo SetCurrentAIManagerPatchPostfix = typeof(SetCurrentAIManagerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(SetCurrentAIManagerPatchOriginal, harmony, true);
            harmony.Patch(SetCurrentAIManagerPatchOriginal, null, new HarmonyMethod(SetCurrentAIManagerPatchPostfix));

            ++patchIndex; // 8

            // CleanUpPatch
            MethodInfo ClearExistingSaveableObjectsOriginal = typeof(VaultSystem).GetMethod("ClearExistingSaveableObjects", BindingFlags.Public | BindingFlags.Static);
            MethodInfo ClearExistingSaveableObjectsPrefix = typeof(CleanUpPatch).GetMethod("ClearExistingSaveableObjectsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo CleanUpScene_EmptiesOriginal = typeof(FVRWristMenuSection_CleanUp).GetMethod("CleanUpScene_Empties", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo CleanUpScene_EmptiesPrefix = typeof(CleanUpPatch).GetMethod("EmptiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo CleanUpScene_AllMagsOriginal = typeof(FVRWristMenuSection_CleanUp).GetMethod("CleanUpScene_AllMags", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo CleanUpScene_AllMagsPrefix = typeof(CleanUpPatch).GetMethod("AllMagsPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo CleanUpScene_GunsOriginal = typeof(FVRWristMenuSection_CleanUp).GetMethod("CleanUpScene_Guns", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo CleanUpScene_GunsPrefix = typeof(CleanUpPatch).GetMethod("GunsPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(ClearExistingSaveableObjectsOriginal, harmony, true);
            PatchController.Verify(CleanUpScene_EmptiesOriginal, harmony, false);
            PatchController.Verify(CleanUpScene_AllMagsOriginal, harmony, false);
            PatchController.Verify(CleanUpScene_GunsOriginal, harmony, false);
            harmony.Patch(ClearExistingSaveableObjectsOriginal, new HarmonyMethod(ClearExistingSaveableObjectsPrefix));
            harmony.Patch(CleanUpScene_EmptiesOriginal, new HarmonyMethod(CleanUpScene_EmptiesPrefix));
            harmony.Patch(CleanUpScene_AllMagsOriginal, new HarmonyMethod(CleanUpScene_AllMagsPrefix));
            harmony.Patch(CleanUpScene_GunsOriginal, new HarmonyMethod(CleanUpScene_GunsPrefix));

            ++patchIndex; // 9

            // SetHealthThresholdPatch
            MethodInfo SetHealthThresholdOriginal = typeof(FVRPlayerBody).GetMethod("SetHealthThreshold", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo SetHealthThresholdPrefix = typeof(SetHealthThresholdPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(SetHealthThresholdOriginal, harmony, false);
            harmony.Patch(SetHealthThresholdOriginal, new HarmonyMethod(SetHealthThresholdPrefix));

            ++patchIndex; // 10

            // PlayerBodyInitPatch
            MethodInfo playerBodyInitOriginal = typeof(FVRPlayerBody).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo playerBodyInitPostfix = typeof(PlayerBodyInitPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(playerBodyInitOriginal, harmony, true);
            harmony.Patch(playerBodyInitOriginal, null, new HarmonyMethod(playerBodyInitPostfix));

            ++patchIndex; // 11

            //// TeleportToPointPatch
            //MethodInfo teleportToPointPatchOriginal = typeof(FVRMovementManager).GetMethod("TeleportToPoint", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(bool) }, null);
            //MethodInfo teleportToPointPatchPrefix = typeof(TeleportToPointPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(teleportToPointPatchOriginal, new HarmonyMethod(teleportToPointPatchPrefix));

            //// SetActivePatch
            //MethodInfo setActivePatchOriginal = typeof(UnityEngine.GameObject).GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setActivePatchPrefix = typeof(SetActivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setActivePatchOriginal, new HarmonyMethod(setActivePatchPrefix));

            //// DestroyPatch
            //MethodInfo destroyPatchOriginal = typeof(UnityEngine.Object).GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, new Type[] { typeof(UnityEngine.Object) }, null);
            //MethodInfo destroyPatchPrefix = typeof(DestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(destroyPatchOriginal, new HarmonyMethod(destroyPatchPrefix));

            //// AIEntityCheckPatch
            //MethodInfo destroyPatchOriginal = typeof(AIManager).GetMethod("EntityCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo destroyPatchPrefix = typeof(AIEntityCheckPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(destroyPatchOriginal, new HarmonyMethod(destroyPatchPrefix));
        }
    }

    // Patches SteamVR_LoadLevel.Begin() So we can keep track of which scene we are loading
    class LoadLevelBeginPatch
    {
        public static string loadingLevel;

        static void Prefix(string levelName)
        {
            loadingLevel = levelName;
            loadingLevel = loadingLevel.Replace("Assets/", "");
            loadingLevel = loadingLevel.Replace(".unity", "");
        }
    }

    // Patches RigidBody.set_isKinematic to keep track of when it is being set
    class KinematicPatch
    {
        public static int skip;

        static bool Prefix(ref Rigidbody __instance, bool value)
        {
            if (Mod.managerObject == null || skip > 0 || __instance == null)
            {
                return true;
            }

            // If game is setting this as kinematic
            if (value)
            {
                // Check if we have a marker (meaning H3MP set it as kinematic due to no control)
                KinematicMarker marker = __instance.GetComponent<KinematicMarker>();
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
                TrackedItem trackedItem = __instance.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
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
                    KinematicMarker marker = __instance.GetComponent<KinematicMarker>();
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
            TrackedItem trackedItem = __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
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

            if (ThreadManager.host)
            {
                ServerSend.PlayerIFF(GameManager.ID, iff);
            }
            else
            {
                ClientSend.PlayerIFF(iff);
            }
        }
    }

    // Patches FVRWristMenu2.Update and Awake to add our H3MP section to it
    class WristMenuPatch
    {
        static void UpdatePrefix(FVRWristMenu2 __instance)
        {
            if (!H3MPWristMenuSection.init)
            {
                H3MPWristMenuSection.init = true;

                AddSections(__instance);

                // Regenerate with our new section
                __instance.RegenerateButtons();
            }
        }

        static void AwakePrefix(FVRWristMenu2 __instance)
        {
            AddSections(__instance);
        }

        private static void AddSections(FVRWristMenu2 __instance)
        {
            GameObject section = new GameObject("Section_H3MP", typeof(RectTransform));
            section.transform.SetParent(__instance.MenuGO.transform);
            section.transform.localPosition = new Vector3(0, 300, 0);
            section.transform.localRotation = Quaternion.identity;
            section.transform.localScale = Vector3.one;
            section.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 350);
            FVRWristMenuSection sectionScript = section.AddComponent<H3MPWristMenuSection>();
            sectionScript.ButtonText = "H3MP";
            __instance.Sections.Add(sectionScript);
            section.SetActive(false);

            section = new GameObject("Section_Body", typeof(RectTransform));
            section.transform.SetParent(__instance.MenuGO.transform);
            section.transform.localPosition = new Vector3(0, 300, 0);
            section.transform.localRotation = Quaternion.identity;
            section.transform.localScale = Vector3.one;
            section.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 350);
            sectionScript = section.AddComponent<BodyWristMenuSection>();
            sectionScript.ButtonText = "Body";
            __instance.Sections.Add(sectionScript);
            section.SetActive(false);
        }
    }

    // Patches GM.InitScene to keep track of when CurrentPlayerBody is set
    class GMInitScenePatch
    {
        static void Postfix(FVRPlayerBody ___m_currentPlayerBody)
        {
            if (Mod.managerObject != null && ___m_currentPlayerBody != null)
            {
                ___m_currentPlayerBody.EyeCam.enabled = !GameManager.spectatorHost;
            }
        }
    }

    // Patches TNH_ScoreDisplay.ReloadLevel to handle MP TNH
    class TNH_ScoreDisplayReloadPatch
    {
        static bool Prefix()
        {
            if (Mod.managerObject != null && Mod.currentTNHInstance != null)
            {
                if (Mod.currentTNHInstance.phase != TNH_Phase.Completed)
                {
                    for (int i = 0; i < Mod.currentTNHInstance.currentlyPlaying.Count; ++i)
                    {
                        if (Mod.currentTNHInstance.currentlyPlaying[i] != GameManager.ID && !Mod.currentTNHInstance.dead.Contains(Mod.currentTNHInstance.currentlyPlaying[i]))
                        {
                            // If players to spectate, teleport to first one
                            GM.CurrentMovementManager.TeleportToPoint(GameManager.players[Mod.currentTNHInstance.currentlyPlaying[i]].transform.position, true);

                            // In this case, don't want to reload level
                            return false;
                        }
                    }
                }

                // If controller we want to restart the game for everyone
                if (Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    // Tell everyone to reset
                    if (ThreadManager.host)
                    {
                        ServerSend.ResetTNH(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.ResetTNH(Mod.currentTNHInstance.instance);
                    }

                    // Don't continue, if we are actually controller (decided by server), we will receive order to reset also
                    return false;
                }
            }

            return true;
        }
    }

    // Patches GM.set_CurrentAIManager to register player AI entities when it gets set
    class SetCurrentAIManagerPatch
    {
        static void Postfix()
        {
            if (Mod.managerObject != null)
            {
                return;
            }

            if (GM.CurrentAIManager != null)
            {
                // Make sure players' AIEntities are registered
                foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                {
                    if (playerEntry.Value.visible && playerEntry.Value.playerBody != null)
                    {
                        playerEntry.Value.playerBody.physicalPlayerBody.SetEntitiesRegistered(true);
                    }
                }

                // Make sure AIEntities are registered
                for (int i = 0; i < GameManager.objects.Count; ++i)
                {
                    if (GameManager.objects[i] is TrackedSosigData)
                    {
                        AIEntity e = (GameManager.objects[i] as TrackedSosigData).physicalSosig.physicalSosig.E;
                        if (GameManager.objects[i].physical != null && !GM.CurrentAIManager.m_knownEntities.Contains(e))
                        {
                            GM.CurrentAIManager.RegisterAIEntity(e);
                        }
                    }
                    else if(GameManager.objects[i] is TrackedAutoMeaterData)
                    {
                        AIEntity e = (GameManager.objects[i] as TrackedAutoMeaterData).physicalAutoMeater.physicalAutoMeater.E;
                        if (GameManager.objects[i].physical != null && !GM.CurrentAIManager.m_knownEntities.Contains(e))
                        {
                            GM.CurrentAIManager.RegisterAIEntity(e);
                        }
                    }
                }
            }
        }
    }

    // Patches the clean up functions to make sure we clean up only certain things
    class CleanUpPatch
    {
        static bool EmptiesPrefix(FVRWristMenuSection_CleanUp __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, __instance.transform.position);

            if (!__instance.askConfirm_CleanupEmpties)
            {
                __instance.ResetConfirm();
                __instance.AskConfirm_CleanupEmpties();
                return false;
            }
            __instance.ResetConfirm();

            FVRFireArmMagazine[] array = UnityEngine.Object.FindObjectsOfType<FVRFireArmMagazine>();
            for (int i = array.Length - 1; i >= 0; i--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array[i], out TrackedItem currentTrackedItem) ? currentTrackedItem : array[i].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array[i].IsHeld && array[i].QuickbeltSlot == null && array[i].FireArm == null && array[i].m_numRounds == 0 && !array[i].IsIntegrated)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }
            FVRFireArmRound[] array2 = UnityEngine.Object.FindObjectsOfType<FVRFireArmRound>();
            for (int j = array2.Length - 1; j >= 0; j--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array2[j], out TrackedItem currentTrackedItem) ? currentTrackedItem : array2[j].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array2[j].IsHeld && array2[j].QuickbeltSlot == null && array2[j].RootRigidbody != null)
                {
                    UnityEngine.Object.Destroy(array2[j].gameObject);
                }
            }
            FVRFireArmClip[] array3 = UnityEngine.Object.FindObjectsOfType<FVRFireArmClip>();
            for (int k = array3.Length - 1; k >= 0; k--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array3[k], out TrackedItem currentTrackedItem) ? currentTrackedItem : array3[k].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array3[k].IsHeld && array3[k].QuickbeltSlot == null && array3[k].FireArm == null && array3[k].m_numRounds == 0)
                {
                    UnityEngine.Object.Destroy(array3[k].gameObject);
                }
            }
            Speedloader[] array4 = UnityEngine.Object.FindObjectsOfType<Speedloader>();
            for (int l = array4.Length - 1; l >= 0; l--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array4[l], out TrackedItem currentTrackedItem) ? currentTrackedItem : array4[l].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array4[l].IsHeld && array4[l].QuickbeltSlot == null)
                {
                    UnityEngine.Object.Destroy(array4[l].gameObject);
                }
            }

            return false;
        }

        static bool AllMagsPrefix(FVRWristMenuSection_CleanUp __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, __instance.transform.position);

            if (!__instance.askConfirm_CleanupAllMags)
            {
                __instance.AskConfirm_CleanupAllMags();
                return false;
            }
            __instance.ResetConfirm();

            FVRFireArmMagazine[] array = UnityEngine.Object.FindObjectsOfType<FVRFireArmMagazine>();
            for (int i = array.Length - 1; i >= 0; i--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array[i], out TrackedItem currentTrackedItem) ? currentTrackedItem : array[i].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array[i].IsHeld && array[i].QuickbeltSlot == null && array[i].FireArm == null && !array[i].IsIntegrated)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }
            FVRFireArmRound[] array2 = UnityEngine.Object.FindObjectsOfType<FVRFireArmRound>();
            for (int j = array2.Length - 1; j >= 0; j--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array2[j], out TrackedItem currentTrackedItem) ? currentTrackedItem : array2[j].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array2[j].IsHeld && array2[j].QuickbeltSlot == null && array2[j].RootRigidbody != null)
                {
                    UnityEngine.Object.Destroy(array2[j].gameObject);
                }
            }
            FVRFireArmClip[] array3 = UnityEngine.Object.FindObjectsOfType<FVRFireArmClip>();
            for (int k = array3.Length - 1; k >= 0; k--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array3[k], out TrackedItem currentTrackedItem) ? currentTrackedItem : array3[k].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array3[k].IsHeld && array3[k].QuickbeltSlot == null && array3[k].FireArm == null)
                {
                    UnityEngine.Object.Destroy(array3[k].gameObject);
                }
            }
            Speedloader[] array4 = UnityEngine.Object.FindObjectsOfType<Speedloader>();
            for (int l = array4.Length - 1; l >= 0; l--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array4[l], out TrackedItem currentTrackedItem) ? currentTrackedItem : array4[l].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array4[l].IsHeld && array4[l].QuickbeltSlot == null)
                {
                    UnityEngine.Object.Destroy(array4[l].gameObject);
                }
            }

            return false;
        }

        static bool GunsPrefix(FVRWristMenuSection_CleanUp __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, __instance.transform.position);

            if (!__instance.askConfirm_CleanupGuns)
            {
                __instance.AskConfirm_CleanupGuns();
                return false;
            }
            __instance.ResetConfirm();

            FVRFireArm[] array = UnityEngine.Object.FindObjectsOfType<FVRFireArm>();
            for (int i = array.Length - 1; i >= 0; i--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array[i], out TrackedItem currentTrackedItem) ? currentTrackedItem : array[i].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array[i].IsHeld && array[i].QuickbeltSlot == null)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }
            SosigWeapon[] array2 = UnityEngine.Object.FindObjectsOfType<SosigWeapon>();
            for (int j = array2.Length - 1; j >= 0; j--)
            {
                TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(array2[j], out TrackedItem currentTrackedItem) ? currentTrackedItem : array2[j].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array2[j].O.IsHeld && array2[j].O.QuickbeltSlot == null && !array2[j].IsHeldByBot && !array2[j].IsInBotInventory)
                {
                    UnityEngine.Object.Destroy(array2[j].gameObject);
                }
            }
            FVRMeleeWeapon[] array3 = UnityEngine.Object.FindObjectsOfType<FVRMeleeWeapon>();
            for (int k = array3.Length - 1; k >= 0; k--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array3[k], out TrackedItem currentTrackedItem) ? currentTrackedItem : array3[k].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array3[k].IsHeld && array3[k].QuickbeltSlot == null)
                {
                    UnityEngine.Object.Destroy(array3[k].gameObject);
                }
            }
            FVRFireArmAttachment[] array4 = UnityEngine.Object.FindObjectsOfType<FVRFireArmAttachment>();
            for (int l = array4.Length - 1; l >= 0; l--)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array4[l], out TrackedItem currentTrackedItem) ? currentTrackedItem : array4[l].GetComponent<TrackedItem>();
                if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                    !array4[l].IsHeld && array4[l].QuickbeltSlot == null && array4[l].curMount == null)
                {
                    UnityEngine.Object.Destroy(array4[l].gameObject);
                }
            }

            return false;
        }

        static bool ClearExistingSaveableObjectsPrefix(FVRWristMenuSection_CleanUp __instance, bool ClearNonSaveLoadable)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Don't want to clear scene if in init scene spawn vault file routine
            if (!SpawnVaultFileRoutinePatch.inInitSpawnVaultFileRoutine)
            {
                FVRPhysicalObject[] array = UnityEngine.Object.FindObjectsOfType<FVRPhysicalObject>();
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    FVRPhysicalObject fvrphysicalObject = array[i];
                    TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(array[i], out TrackedItem currentTrackedItem) ? currentTrackedItem : array[i].GetComponent<TrackedItem>();
                    if (((ThreadManager.host && (trackedItem == null || trackedItem.data.controller == GameManager.ID || !trackedItem.itemData.underActiveControl)) || (trackedItem == null || trackedItem.data.controller == GameManager.ID)) &&
                        !(fvrphysicalObject == null) && fvrphysicalObject.gameObject.activeSelf && !fvrphysicalObject.IsHeld && !(fvrphysicalObject.QuickbeltSlot != null) && !(fvrphysicalObject.ObjectWrapper == null) && !(fvrphysicalObject.gameObject.transform.parent != null) && (fvrphysicalObject.GetIsSaveLoadable() || !ClearNonSaveLoadable) && IM.HasSpawnedID(fvrphysicalObject.ObjectWrapper.SpawnedFromId))
                    {
                        UnityEngine.Object.Destroy(fvrphysicalObject.GameObject);
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRPlayerBody.SetHealthThreshold to override with max health setting if necessary
    class SetHealthThresholdPatch
    {
        public static int skip;

        static bool Prefix()
        {
            if (Mod.managerObject == null || skip > 0 || GameManager.overrideMaxHealthSetting)
            {
                return true;
            }

            if (GameManager.maxHealthByInstanceByScene.TryGetValue(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene, out Dictionary<int, KeyValuePair<float, int>> instancesDict) &&
                instancesDict.ContainsKey(GameManager.instance))
            {
                return false;
            }

            return true;
        }
    }

    // Patches FVRPlayerBody.Init to raise the event
    class PlayerBodyInitPatch
    {
        static void Postfix(FVRPlayerBody __instance)
        {
            // TNH
            if (Mod.TNHSpectating)
            {
                __instance.DisableHands();
            }

            GameManager.RaisePlayerBodyInit(__instance);
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

    // DEBUG PATCH Patches Object.Destroy
    class DestroyPatch
    {
        static void Prefix(UnityEngine.Object obj)
        {
            Mod.LogInfo("Destroying " + obj + ":\n" + Environment.StackTrace);
        }
    }

    // DEBUG PATCH Patches AIManager.EntityCheck to debug why AutoMeater does not detect remote Sosigs
    class AIEntityCheckPatch
    {
        static bool Prefix(AIManager __instance, AIEntity e)
        {
            e.ResetTick();
            if (e.ReceivesEvent_Visual)
            {
                Vector3 pos = e.GetPos();
                Vector3 vector = pos;
                Vector3 forward = e.SensoryFrame.forward;
                if (!e.IsVisualCheckOmni)
                {
                    vector += forward * e.MaximumSightRange;
                }
                Collider[] array = Physics.OverlapSphere(vector, e.MaximumSightRange, __instance.LM_Entity, QueryTriggerInteraction.Collide);
                if (array.Length > 0)
                {
                    Mod.LogInfo("EntityCheck on " + e.name+" with parent: "+(e.transform.parent == null ? "null" : e.transform.parent.name)+", got "+array.Length+" entities");
                    for (int i = 0; i < array.Length; i++)
                    {
                        Mod.LogInfo("\tChecking "+i+" " + array[i].name + " with parent: " + (array[i].transform.parent == null ? "null" : array[i].transform.parent.name));
                        AIEntity component = array[i].GetComponent<AIEntity>();
                        if (!(component == null))
                        {
                            Mod.LogInfo("\t\tHas AIEntity");
                            if (!(component == e))
                            {
                                Mod.LogInfo("\t\t\tNot current AIEntity");
                                if (component.IFFCode >= -1)
                                {
                                    Mod.LogInfo("\t\t\t\tGot valid IFF: "+ component.IFFCode);
                                    if (!component.IsPassiveEntity || e.PerceivesPassiveEntities)
                                    {
                                        Vector3 pos2 = component.GetPos();
                                        Vector3 to = pos2 - pos;
                                        float num = to.magnitude;
                                        float dist = num;
                                        float num2 = e.MaximumSightRange;
                                        Mod.LogInfo("\t\t\t\t\tNot passive or we perceive, num = "+num+" from "+to.magnitude+", from pos2: "+pos2+" and pos: "+pos+ ", while num2 = "+num2);
                                        if (num <= component.MaxDistanceVisibleFrom)
                                        {
                                            Mod.LogInfo("\t\t\t\t\t\t"+ num+" <= max dist vis from: "+ component.MaxDistanceVisibleFrom);
                                            if (component.VisibilityMultiplier <= 2f)
                                            {
                                                if (component.VisibilityMultiplier > 1f)
                                                {
                                                    num = Mathf.Lerp(num, num2, component.VisibilityMultiplier - 1f);
                                                }
                                                else
                                                {
                                                    num = Mathf.Lerp(0f, num, component.VisibilityMultiplier);
                                                }
                                                if (!e.IsVisualCheckOmni)
                                                {
                                                    float num3 = Vector3.Angle(forward, to);
                                                    num2 = e.MaximumSightRange * e.SightDistanceByFOVMultiplier.Evaluate(num3 / e.MaximumSightFOV);
                                                }
                                                Mod.LogInfo("\t\t\t\t\t\t\tGot valid vis mult, num: "+ num+", num2: "+ num2);
                                                if (num <= num2)
                                                {
                                                    Mod.LogInfo("\t\t\t\t\t\t\t\tValid");
                                                    if (!Physics.Linecast(pos, pos2, e.LM_VisualOcclusionCheck, QueryTriggerInteraction.Collide))
                                                    {
                                                        Mod.LogInfo("\t\t\t\t\t\t\t\t\tGot line of sight, sending event receive for visual event");
                                                        float v = num / e.MaximumSightRange * component.DangerMultiplier;
                                                        AIEvent e2 = new AIEvent(component, AIEvent.AIEType.Visual, v, dist);
                                                        e.OnAIEventReceive(e2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
