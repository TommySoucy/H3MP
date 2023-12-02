using Anvil;
using FistVR;
using H3MP.Networking;
using H3MP.Tracking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace H3MP.Patches
{
    public class InstantiationPatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // ChamberEjectRoundPatch
            MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool) }, null);
            MethodInfo chamberEjectRoundPatchAnimationOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(bool) }, null);
            MethodInfo chamberEjectRoundPatchPrefix = typeof(ChamberEjectRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(chamberEjectRoundPatchOriginal, harmony, true);
            PatchController.Verify(chamberEjectRoundPatchAnimationOriginal, harmony, true);
            harmony.Patch(chamberEjectRoundPatchOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));
            harmony.Patch(chamberEjectRoundPatchAnimationOriginal, new HarmonyMethod(chamberEjectRoundPatchPrefix), new HarmonyMethod(chamberEjectRoundPatchPostfix));

            ++patchIndex; // 1

            // Internal_CloneSinglePatch
            MethodInfo internal_CloneSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSinglePatchPostfix = typeof(Internal_CloneSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_CloneSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSinglePatchOriginal, null, new HarmonyMethod(internal_CloneSinglePatchPostfix));

            ++patchIndex; // 2

            // Internal_CloneSingleWithParentPatch
            MethodInfo internal_CloneSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_CloneSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPrefix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_CloneSingleWithParentPatchPostfix = typeof(Internal_CloneSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_CloneSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_CloneSingleWithParentPatchOriginal, new HarmonyMethod(internal_CloneSingleWithParentPatchPrefix), new HarmonyMethod(internal_CloneSingleWithParentPatchPostfix));

            ++patchIndex; // 3

            // Internal_InstantiateSinglePatch
            MethodInfo internal_InstantiateSinglePatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingle", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSinglePatchPostfix = typeof(Internal_InstantiateSinglePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_InstantiateSinglePatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSinglePatchOriginal, null, new HarmonyMethod(internal_InstantiateSinglePatchPostfix));

            ++patchIndex; // 4

            // Internal_InstantiateSingleWithParentPatch
            MethodInfo internal_InstantiateSingleWithParentPatchOriginal = typeof(UnityEngine.Object).GetMethod("Internal_InstantiateSingleWithParent", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPrefix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo internal_InstantiateSingleWithParentPatchPostfix = typeof(Internal_InstantiateSingleWithParentPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(internal_InstantiateSingleWithParentPatchOriginal, harmony, true);
            harmony.Patch(internal_InstantiateSingleWithParentPatchOriginal, new HarmonyMethod(internal_InstantiateSingleWithParentPatchPrefix), new HarmonyMethod(internal_InstantiateSingleWithParentPatchPostfix));

            ++patchIndex; // 5

            // LoadDefaultSceneRoutinePatch
            MethodInfo loadDefaultSceneRoutinePatchOriginal = typeof(FVRSceneSettings).GetMethod("LoadDefaultSceneRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo loadDefaultSceneRoutinePatchPrefix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo loadDefaultSceneRoutinePatchPostfix = typeof(LoadDefaultSceneRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(loadDefaultSceneRoutinePatchOriginal, harmony, false);
            harmony.Patch(loadDefaultSceneRoutinePatchOriginal, new HarmonyMethod(loadDefaultSceneRoutinePatchPrefix), new HarmonyMethod(loadDefaultSceneRoutinePatchPostfix));

            ++patchIndex; // 6

            // SpawnObjectsPatch
            MethodInfo spawnObjectsPatchOriginal = typeof(VaultSystem).GetMethod("SpawnObjects", BindingFlags.Public | BindingFlags.Static);
            MethodInfo spawnObjectsPatchPrefix = typeof(SpawnObjectsPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(spawnObjectsPatchOriginal, harmony, false);
            harmony.Patch(spawnObjectsPatchOriginal, new HarmonyMethod(spawnObjectsPatchPrefix));

            ++patchIndex; // 7

            // SpawnVaultFileRoutinePatch
            MethodInfo spawnVaultFileRoutinePatchOriginal = typeof(VaultSystem).GetMethod("SpawnVaultFileRoutine", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchMoveNext = PatchController.EnumeratorMoveNext(spawnVaultFileRoutinePatchOriginal);
            MethodInfo spawnVaultFileRoutinePatchPrefix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchTranspiler = typeof(SpawnVaultFileRoutinePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo spawnVaultFileRoutinePatchPostfix = typeof(SpawnVaultFileRoutinePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(spawnVaultFileRoutinePatchOriginal, harmony, false);
            PatchController.Verify(spawnVaultFileRoutinePatchMoveNext, harmony, false);
            try 
            { 
                harmony.Patch(spawnVaultFileRoutinePatchMoveNext, new HarmonyMethod(spawnVaultFileRoutinePatchPrefix), new HarmonyMethod(spawnVaultFileRoutinePatchPostfix), new HarmonyMethod(spawnVaultFileRoutinePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying InstantiationPatches.SpawnVaultFileRoutinePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 8

            // IDSpawnedFromPatch
            MethodInfo IDSpawnedFromPatchOriginal = typeof(FVRPhysicalObject).GetMethod("set_IDSpawnedFrom", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo IDSpawnedFromPatchPostfix = typeof(IDSpawnedFromPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(IDSpawnedFromPatchOriginal, harmony, true);
            harmony.Patch(IDSpawnedFromPatchOriginal, null, new HarmonyMethod(IDSpawnedFromPatchPostfix));

            ++patchIndex; // 9

            // AnvilPrefabSpawnPatch
            MethodInfo anvilPrefabSpawnPatchOriginal = typeof(AnvilPrefabSpawn).GetMethod("InstantiateAndZero", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo anvilPrefabSpawnPatchPrefix = typeof(AnvilPrefabSpawnPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo anvilPrefabSpawnPatchPostfix = typeof(AnvilPrefabSpawnPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(anvilPrefabSpawnPatchOriginal, harmony, true);
            harmony.Patch(anvilPrefabSpawnPatchOriginal, new HarmonyMethod(anvilPrefabSpawnPatchPrefix), new HarmonyMethod(anvilPrefabSpawnPatchPostfix));

            ++patchIndex; // 10

            // BrutPlacerPatch
            MethodInfo brutPlacerOriginal = typeof(BrutPlacer).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutAssemblagePlacerOriginal = typeof(BrutAssemblagePlacer).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutPlacerPrefix = typeof(BrutPlacerPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo brutPlacerPostfix = typeof(BrutPlacerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(brutPlacerOriginal, harmony, true);
            PatchController.Verify(brutAssemblagePlacerOriginal, harmony, true);
            harmony.Patch(brutPlacerOriginal, new HarmonyMethod(brutPlacerPrefix), new HarmonyMethod(brutPlacerPostfix));
            harmony.Patch(brutAssemblagePlacerOriginal, new HarmonyMethod(brutPlacerPrefix), new HarmonyMethod(brutPlacerPostfix));

            ++patchIndex; // 11

            // ConstructVolumePatch
            MethodInfo constructVolumeSpawnConstructsOriginal = typeof(Construct_Volume).GetMethod("SpawnConstructs", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo constructVolumeSpawnConstructsPrefix = typeof(ConstructVolumePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo constructVolumeSpawnConstructsPostfix = typeof(ConstructVolumePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(constructVolumeSpawnConstructsOriginal, harmony, true);
            harmony.Patch(constructVolumeSpawnConstructsOriginal, new HarmonyMethod(constructVolumeSpawnConstructsPrefix), new HarmonyMethod(constructVolumeSpawnConstructsPostfix));
        }
    }

    // Patches FVRFireArmChamber.EjectRound so we can keep track of when a round is ejected from a chamber
    class ChamberEjectRoundPatch
    {
        public static int overrideFlag = 0;
        static bool track = false;
        static int incrementedSkip = 0;

        static TrackedItem chamberOwnerTrackedItem;

        static void Prefix(ref FVRFireArmChamber __instance, ref FVRFireArmRound ___m_round, bool ForceCaseLessEject)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }
            
            incrementedSkip = 0;

            // Check if a round would be ejected
            if (___m_round != null && (!___m_round.IsCaseless || ForceCaseLessEject || __instance.SuppressCaselessDeletion))
            {
                // TODO: Optimization: Maybe have a dict trackedItemByChamber, in which we keep any item which have a chamber, which we would put in there in trackedItem awake
                //       Because right now we just go up the hierarchy until we find the item, maybe its faster? will need to test, but considering the GetComponent overhead
                //       we might want to do this differently
                Transform currentParent = __instance.transform;
                chamberOwnerTrackedItem = null;
                while (currentParent != null)
                {
                    chamberOwnerTrackedItem = currentParent.GetComponent<TrackedItem>();
                    if (chamberOwnerTrackedItem != null)
                    {
                        break;
                    }
                    currentParent = currentParent.parent;
                }

                if (__instance.IsSpent)
                {
                    // Skip the instantiation of the casing because we don't want to sync these between clients
                    ++Mod.skipAllInstantiates;
                    ++incrementedSkip;
                }
                else // We are ejecting a whole round, we want the controller of the chamber's parent tracked item to control the round
                {
                    // Check if we should control and sync it, if so do it in postfix
                    if (overrideFlag > 0 || chamberOwnerTrackedItem == null || chamberOwnerTrackedItem.data.controller == GameManager.ID)
                    {
                        track = true;
                    }
                    else // Round was instantiated from chamber of an item that is controlled by other client and not overriden
                    {
                        // Skip the instantiate on our side, the controller client will instantiate and sync it with us eventually
                        ++Mod.skipAllInstantiates;
                        ++incrementedSkip;
                    }
                }
            }
        }

        static void Postfix(FVRFireArmChamber __instance, ref FVRFireArmRound __result)
        {
            if (incrementedSkip > 0)
            {
                Mod.skipAllInstantiates -= incrementedSkip;
                incrementedSkip = 0;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            if (track)
            {
                track = false;

                Mod.OnInstantiationTrackInvoke(__result.gameObject);

                GameManager.SyncTrackedObjects(__result.transform, true, null);

                // If overriden it is because we want to control the ejection of this chamber
                // Make sure the controller of the chamber knows this chamber is to be emptied
                if(overrideFlag > 0)
                {
                    if(chamberOwnerTrackedItem != null && chamberOwnerTrackedItem.data.controller != GameManager.ID)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.ChamberRound(chamberOwnerTrackedItem.data.trackedID, (FireArmRoundClass)(-1), chamberOwnerTrackedItem.getChamberIndex(__instance));
                        }
                        else // Note: If we are client non-controller, it implies we have tracked ID
                        {
                            ClientSend.ChamberRound(chamberOwnerTrackedItem.data.trackedID, (FireArmRoundClass)(-1), chamberOwnerTrackedItem.getChamberIndex(__instance));
                        }
                    }
                }
            }
            else
            {
                if (overrideFlag > 0 && chamberOwnerTrackedItem != null && chamberOwnerTrackedItem.data.controller != GameManager.ID)
                {
                    // Override flag means we want to empty a non controlled chamber of a spent round
                    // Despite not wanting to track the spent round, we still want to send order to others to empty the chamber on their side as well
                    if (ThreadManager.host)
                    {
                        ServerSend.ChamberRound(chamberOwnerTrackedItem.data.trackedID, (FireArmRoundClass)(-1), chamberOwnerTrackedItem.getChamberIndex(__instance));
                    }
                    else // Note: If we are client non-controller, it implies we have tracked ID
                    {
                        ClientSend.ChamberRound(chamberOwnerTrackedItem.data.trackedID, (FireArmRoundClass)(-1), chamberOwnerTrackedItem.getChamberIndex(__instance));
                    }
                }
                if (__result != null) // Don't want to track the round, make sure it is spent
                {
                    __result.SetKillCounting(true);
                    __result.Fire();
                }
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
                // because the client that has initialized the scene spawned these and synced them or eventually will
                if (((GameManager.sceneLoading && !GameManager.controlOverride) || (!GameManager.sceneLoading && !GameManager.firstPlayerInSceneInstance)) &&
                    SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                Mod.OnInstantiationTrackInvoke(__result as GameObject);

                GameManager.SyncTrackedObjects((__result as GameObject).transform, true, null);
            }
        }
    }

    // Patches Object.Internal_CloneSingleWithParent to keep track of this type of instantiation
    class Internal_CloneSingleWithParentPatch
    {
        static bool track = false;
        static TrackedItemData parentData;

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
                    TrackedItem trackedItem = parent.GetComponent<TrackedItem>();
                    if (trackedItem != null)
                    {
                        parentData = trackedItem.itemData;
                        break;
                    }
                    currentParent = currentParent.parent;
                }

                // We only want to track this item if no tracked parent or if we control the parent
                track = parentData == null || parentData.controller == GameManager.ID;
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
                if (GameManager.playersPresent.Count > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);

                    track = false;
                    return;
                }
            }

            if (track)
            {
                Mod.OnInstantiationTrackInvoke(__result as GameObject);
                track = false;
                GameManager.SyncTrackedObjects((__result as GameObject).transform, true, parentData);
            }
        }
    }

    // Patches Object.Internal_InstantiateSingle to keep track of this type of instantiation
    class Internal_InstantiateSinglePatch
    {
        static void Postfix(ref UnityEngine.Object __result)
        {
            if (__result.name.Contains("Cascading"))
            {
                Mod.LogInfo("Instantiate single pach for " + __result.name);
            }

            if (Mod.skipAllInstantiates > 0)
            {
                return;
            }

            if (__result.name.Contains("Cascading"))
            {
                Mod.LogInfo("\tNo skip");
            }

            // Skip if not connected
            if (__result == null || Mod.managerObject == null)
            {
                return;
            }

            if (__result.name.Contains("Cascading"))
            {
                Mod.LogInfo("\tNo skip");
            }

            // If we want to skip the instantiate because this is a scene load vault file being spawned
            if (SpawnVaultFileRoutinePatch.inSpawnVaultFileRoutineToSkip)
            {

                if (__result.name.Contains("Cascading"))
                {
                    Mod.LogInfo("\t\tInvault file routine");
                }
                // If not for this the item would be spawned and then synced with other clients below
                // The scene has presumably already been fully loaded, which means we already synced all items in the scene with other clients
                // But this is still an item spawned by scene initialization, so if we are not the first one in the scene, we want to destroy this item
                // because the client that has initialized the scene spawned these and synced them
                if (GameManager.playersPresent.Count > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);
                    return;
                }
            }

            // If this is a game object check and sync all physical objects if necessary
            if (__result is GameObject)
            {
                Mod.OnInstantiationTrackInvoke(__result as GameObject);
                GameManager.SyncTrackedObjects((__result as GameObject).transform, true, null);
            }
        }
    }

    // Patches Object.Internal_InstantiateSingleWithParent to keep track of this type of instantiation
    class Internal_InstantiateSingleWithParentPatch
    {
        static bool track = false;
        static TrackedItemData parentData;

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
                    TrackedItem trackedItem = parent.GetComponent<TrackedItem>();
                    if (trackedItem != null)
                    {
                        parentData = trackedItem.itemData;
                        break;
                    }
                    currentParent = currentParent.parent;
                }

                // We only want to track this item if no tracked parent or if we control the parent
                track = parentData == null || parentData.controller == GameManager.ID;
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
                if (GameManager.playersPresent.Count > 0 && SpawnVaultFileRoutinePatch.routineData.ContainsKey(SpawnVaultFileRoutinePatch.currentFile))
                {
                    List<UnityEngine.Object> objectsToDestroy = SpawnVaultFileRoutinePatch.routineData[SpawnVaultFileRoutinePatch.currentFile];
                    objectsToDestroy.Add(__result);

                    track = false;
                    return;
                }
            }

            if (track)
            {
                Mod.OnInstantiationTrackInvoke(__result as GameObject);
                track = false;
                GameManager.SyncTrackedObjects((__result as GameObject).transform, true, parentData);
            }
        }
    }

    // Patches FVRSceneSettings.LoadDefaultSceneRoutine so we know when we spawn items from vault as part of scene loading
    // The goal is to identify objects spawned for the default scene routine and destroy them right after spawning if we are not 
    // the first player in the scene
    //      We destroy instead of preventing the spawn because SpawnVaultFileRoutine will access the object after instantiation
    // When we are in the LoadDefaultSceneRountine we set a flag
    // LoadDefaultSceneRountine calls SpawnObjects
    //      SpawnObjectsPatch will store the default scene file(s) being spawned while the LoadDefaultSceneRountine flag is set
    //      If any other players were already in the scene, it means they have loaded the scenario, so we don't want to load it again
    //      So we want to destroy any item instantiated from one of those vault files
    // SpawnObjects calls SpawnVaultFile
    // SpawnVaultFile calls AnvilManager.Run(VaultSystem.SpawnVaultFileRoutine)
    //      Note that this is a corountine so we patch the MoveNext of the iterator instead
    //      to keep track of the vault file we are currently spawning so we know which to skip
    //      While we are in a MoveNext call we set the inSpawnVaultFileRoutineToSkip flag
    // Then in instantiation patches, once an object belonging to once of those files gets instantiated (inSpawnVaultFileRoutineToSkip is set)
    // we add the resulting object to the list corresponding to the file
    // Once the SpawnVaultFileRoutine finishes, we destroy every object in the list

    // Note that this whole thing is an attempt at preventing to initialize a scene if already has been initialized.
    // Unfortunately there is still a problem if multiple players started loading a scene at the same time, each thinking they were the first
    // Each of these will let each of their objects spawn, this is why we add the sceneInit flag to full object packets so the server
    // can decide who the one to initialize the scene is in case multiple of them send initial objects.

    class LoadDefaultSceneRoutinePatch
    {
        public static bool inLoadDefaultSceneRoutine;

        static void Prefix()
        {
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
            if (LoadDefaultSceneRoutinePatch.inLoadDefaultSceneRoutine)
            {
                // If first in scene/instance
                if (Mod.managerObject == null || GameManager.firstPlayerInSceneInstance)
                {
                    if (SpawnVaultFileRoutinePatch.initFiles == null)
                    {
                        SpawnVaultFileRoutinePatch.initFiles = new List<string>();
                    }
                    SpawnVaultFileRoutinePatch.initFiles.Add(file.FileName);
                }
                else // Not first player in scene, add to files to skip
                {
                    if (SpawnVaultFileRoutinePatch.filesToSkip == null)
                    {
                        SpawnVaultFileRoutinePatch.filesToSkip = new List<string>();
                    }
                    SpawnVaultFileRoutinePatch.filesToSkip.Add(file.FileName);
                }
            }
        }
    }

    // Patches VaultSystem.SpawnVaultFileRoutine.MoveNext to keep track of whether we are spawning items as part of scene loading
    class SpawnVaultFileRoutinePatch
    {
        public static bool inSpawnVaultFileRoutineToSkip;
        public static bool inInitSpawnVaultFileRoutine;
        public static List<string> filesToSkip;
        public static List<string> initFiles;
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

                    TrackedObject trackedObject = null;
                    if (obj is GameObject)
                    {
                        GameObject go = obj as GameObject;
                        if (go != null)
                        {
                            trackedObject = go.GetComponent<TrackedObject>();
                        }
                    }
                    if (trackedObject != null)
                    {
                        trackedObject.skipFullDestroy = true;
                    }

                    UnityEngine.Object.Destroy(obj);
                }
                routineData.Remove(currentFile);
            }
        }

        static void Prefix(ref VaultFile ___f)
        {
            if (filesToSkip != null && filesToSkip.Contains(___f.FileName))
            {
                inSpawnVaultFileRoutineToSkip = true;

                currentFile = ___f.FileName;
                if (!routineData.ContainsKey(___f.FileName))
                {
                    routineData.Add(___f.FileName, new List<UnityEngine.Object>());
                }
            }
            else if (initFiles != null && initFiles.Contains(___f.FileName))
            {
                inInitSpawnVaultFileRoutine = true;

                currentFile = ___f.FileName;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            CodeInstruction toInsert = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpawnVaultFileRoutinePatch), "FinishedRoutine"));

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stfld && instruction.operand.ToString().Equals("System.Int32 $PC") &&
                    instructionList[i + 1].opcode == OpCodes.Ldc_I4_0 && instructionList[i + 2].opcode == OpCodes.Ret)
                {
                    instructionList.Insert(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SpawnVaultFileRoutinePatch Transpiler not applied!");
            }

            return instructionList;
        }

        static void Postfix(ref VaultFile ___f)
        {
            inSpawnVaultFileRoutineToSkip = false;
            inInitSpawnVaultFileRoutine = false;
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
            GameManager.SyncTrackedObjects(__instance.transform, true, null);
        }
    }

    // Patches AnvilPrefabSpawn.InstantiateAndZero so we know when we spawn items from an anvil prefab spawn
    class AnvilPrefabSpawnPatch
    {
        public static bool inInitPrefabSpawn;

        static bool Prefix(AnvilPrefabSpawn __instance, GameObject result)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            inInitPrefabSpawn = true;

            Mod.LogInfo("AnvilPrefabSpawn: " + __instance.name + ", loading?: " + GameManager.sceneLoading + ", override?: " + GameManager.controlOverride + ", firstPlayerInSceneInstance?: " + GameManager.firstPlayerInSceneInstance);

            // Prevent spawning if loading but we have control override, or we aren't loading but we were first in scene
            return (GameManager.sceneLoading && GameManager.controlOverride) || (!GameManager.sceneLoading && GameManager.firstPlayerInSceneInstance);
        }

        static void Postfix()
        {
            inInitPrefabSpawn = false;
        }
    }

    // Patches BrutPlacer.Start and BrutAssemblagePlacer.Start so we know when we spawn items from a Brut(Assemblage)Placer
    class BrutPlacerPatch
    {
        public static bool inInitBrutPlacer;

        static bool Prefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            inInitBrutPlacer = true;

            Mod.LogInfo("Brut(Assemblage)Placer: loading?: " + GameManager.sceneLoading + ", override?: " + GameManager.controlOverride + ", firstPlayerInSceneInstance?: " + GameManager.firstPlayerInSceneInstance);

            // Prevent spawning if loading but we have control override, or we aren't loading but we were first in scene
            return (GameManager.sceneLoading && GameManager.controlOverride) || (!GameManager.sceneLoading && GameManager.firstPlayerInSceneInstance);
            // TODO: Improvement: After spawning its stuff, the placer is usually destroyed, so if we return false here, we might want to destroy the placer right away
        }

        static void Postfix()
        {
            inInitBrutPlacer = false;
        }
    }

    // Patches Construct_Volume.SpawnConstructs to track scene init
    class ConstructVolumePatch
    {
        public static bool inInitConstructVolume;

        static bool Prefix()
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            inInitConstructVolume = true;

            Mod.LogInfo("Construct volume: loading?: " + GameManager.sceneLoading + ", override?: " + GameManager.controlOverride + ", firstPlayerInSceneInstance?: " + GameManager.firstPlayerInSceneInstance);

            // Prevent spawning if loading but we have control override, or we aren't loading but we were first in scene
            return (GameManager.sceneLoading && GameManager.controlOverride) || (!GameManager.sceneLoading && GameManager.firstPlayerInSceneInstance);
        }

        static void Postfix()
        {
            inInitConstructVolume = false;
        }
    }
}
