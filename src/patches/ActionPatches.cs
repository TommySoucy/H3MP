using FistVR;
using H3MP.Networking;
using H3MP.Scripts;
using H3MP.Tracking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace H3MP.Patches
{
    public class ActionPatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // FirePatch
            MethodInfo firePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo firePatchPrefix = typeof(FirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo firePatchTranspiler = typeof(FirePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo firePatchPostfix = typeof(FirePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(firePatchOriginal, harmony, true);
            try
            {
                harmony.Patch(firePatchOriginal, new HarmonyMethod(firePatchPrefix), new HarmonyMethod(firePatchPostfix), new HarmonyMethod(firePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FirePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 1

            // FireSosigWeaponPatch
            MethodInfo fireSosigWeaponPatchOriginal = typeof(SosigWeapon).GetMethod("FireGun", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireSosigWeaponPatchPrefix = typeof(FireSosigWeaponPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSosigWeaponPatchTranspiler = typeof(FireSosigWeaponPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSosigWeaponPatchPostfix = typeof(FireSosigWeaponPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireSosigWeaponPatchOriginal, harmony, true);
            try
            {
                harmony.Patch(fireSosigWeaponPatchOriginal, new HarmonyMethod(fireSosigWeaponPatchPrefix), new HarmonyMethod(fireSosigWeaponPatchPostfix), new HarmonyMethod(fireSosigWeaponPatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireSosigWeaponPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 2

            // FireLAPD2019Patch
            MethodInfo fireLAPD2019PatchOriginal = typeof(LAPD2019).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireLAPD2019PatchPrefix = typeof(FireLAPD2019Patch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLAPD2019PatchTranspiler = typeof(FireLAPD2019Patch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLAPD2019PatchPostfix = typeof(FireLAPD2019Patch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireLAPD2019PatchOriginal, harmony, false);
            try
            {
                harmony.Patch(fireLAPD2019PatchOriginal, new HarmonyMethod(fireLAPD2019PatchPrefix), new HarmonyMethod(fireLAPD2019PatchPostfix), new HarmonyMethod(fireLAPD2019PatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireLAPD2019Patch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 3

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

            PatchController.Verify(fireAttachableFirearmPatchOriginal, harmony, false);
            PatchController.Verify(fireAttachableBreakActionsPatchOriginal, harmony, false);
            PatchController.Verify(fireAttachableClosedBoltWeaponPatchOriginal, harmony, false);
            PatchController.Verify(fireAttachableTubeFedPatchOriginal, harmony, false);
            PatchController.Verify(fireGP25PatchOriginal, harmony, false);
            PatchController.Verify(fireM203PatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(fireAttachableFirearmPatchOriginal, null, null, new HarmonyMethod(fireAttachableFirearmPatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireAttachableFirearmPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(fireAttachableBreakActionsPatchOriginal, new HarmonyMethod(fireAttachableBreakActionsPatchPrefix), new HarmonyMethod(fireAttachableBreakActionsPatchPostfix));
            harmony.Patch(fireAttachableClosedBoltWeaponPatchOriginal, new HarmonyMethod(fireAttachableClosedBoltWeaponPatchPrefix), new HarmonyMethod(fireAttachableClosedBoltWeaponPatchPostfix));
            harmony.Patch(fireAttachableTubeFedPatchOriginal, new HarmonyMethod(fireAttachableTubeFedPatchPrefix), new HarmonyMethod(fireAttachableTubeFedPatchPostfix));
            harmony.Patch(fireGP25PatchOriginal, new HarmonyMethod(fireGP25PatchPrefix), new HarmonyMethod(fireGP25PatchPostfix));
            harmony.Patch(fireM203PatchOriginal, new HarmonyMethod(fireM203PatchPrefix), new HarmonyMethod(fireM203PatchPostfix));

            ++patchIndex; // 4

            // FireRevolvingShotgunPatch
            MethodInfo fireRevolvingShotgunPatchOriginal = typeof(RevolvingShotgun).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireRevolvingShotgunPatchPrefix = typeof(FireRevolvingShotgunPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireRevolvingShotgunPatchPostfix = typeof(FireRevolvingShotgunPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireRevolvingShotgunPatchOriginal, harmony, false);
            harmony.Patch(fireRevolvingShotgunPatchOriginal, new HarmonyMethod(fireRevolvingShotgunPatchPrefix), new HarmonyMethod(fireRevolvingShotgunPatchPostfix));

            ++patchIndex; // 5

            // FireRevolverPatch
            MethodInfo fireRevolverPatchOriginal = typeof(Revolver).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireRevolverPatchPrefix = typeof(FireRevolverPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireRevolverPatchPostfix = typeof(FireRevolverPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireRevolverPatchOriginal, harmony, false);
            harmony.Patch(fireRevolverPatchOriginal, new HarmonyMethod(fireRevolverPatchPrefix), new HarmonyMethod(fireRevolverPatchPostfix));

            ++patchIndex; // 6

            // FireSingleActionRevolverPatch
            MethodInfo fireSingleActionRevolverPatchOriginal = typeof(SingleActionRevolver).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireSingleActionRevolverPatchPrefix = typeof(FireSingleActionRevolverPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireSingleActionRevolverPatchPostfix = typeof(FireSingleActionRevolverPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireSingleActionRevolverPatchOriginal, harmony, false);
            harmony.Patch(fireSingleActionRevolverPatchOriginal, new HarmonyMethod(fireSingleActionRevolverPatchPrefix), new HarmonyMethod(fireSingleActionRevolverPatchPostfix));

            ++patchIndex; // 7

            // FireGrappleGunPatch
            MethodInfo fireGrappleGunPatchOriginal = typeof(GrappleGun).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[0], null);
            MethodInfo fireGrappleGunPatchPrefix = typeof(FireGrappleGunPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireGrappleGunPatchPostfix = typeof(FireGrappleGunPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireGrappleGunPatchOriginal, harmony, false);
            harmony.Patch(fireGrappleGunPatchOriginal, new HarmonyMethod(fireGrappleGunPatchPrefix), new HarmonyMethod(fireGrappleGunPatchPostfix));

            ++patchIndex; // 8

            // FireDerringerPatch
            MethodInfo fireDerringerPatchOriginal = typeof(Derringer).GetMethod("FireBarrel", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireDerringerPatchPrefix = typeof(FireDerringerPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireDerringerPatchPostfix = typeof(FireDerringerPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireDerringerPatchOriginal, harmony, false);
            harmony.Patch(fireDerringerPatchOriginal, new HarmonyMethod(fireDerringerPatchPrefix), new HarmonyMethod(fireDerringerPatchPostfix));

            ++patchIndex; // 9

            // FireBreakActionWeaponPatch
            MethodInfo fireBreakActionWeaponPatchOriginal = typeof(BreakActionWeapon).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int), typeof(bool), typeof(int) }, null);
            MethodInfo fireBreakActionWeaponPatchPrefix = typeof(FireBreakActionWeaponPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireBreakActionWeaponPatchPostfix = typeof(FireBreakActionWeaponPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireBreakActionWeaponPatchOriginal, harmony, false);
            harmony.Patch(fireBreakActionWeaponPatchOriginal, new HarmonyMethod(fireBreakActionWeaponPatchPrefix), new HarmonyMethod(fireBreakActionWeaponPatchPostfix));

            ++patchIndex; // 10

            // FireLeverActionFirearmPatch
            MethodInfo fireLeverActionFirearmPatchOriginal = typeof(LeverActionFirearm).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireLeverActionFirearmPatchPrefix = typeof(FireLeverActionFirearmPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireLeverActionFirearmPatchPostfix = typeof(FireLeverActionFirearmPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireLeverActionFirearmPatchOriginal, harmony, false);
            harmony.Patch(fireLeverActionFirearmPatchOriginal, new HarmonyMethod(fireLeverActionFirearmPatchPrefix), new HarmonyMethod(fireLeverActionFirearmPatchPostfix));

            ++patchIndex; // 11

            // FireFlintlockWeaponPatch
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterOriginal = typeof(FlintlockBarrel).GetMethod("BurnOffOuter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterPrefix = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterTranspiler = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponPatchBurnOffOuterPostfix = typeof(FireFlintlockWeaponPatch).GetMethod("BurnOffOuterPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFireOriginal = typeof(FlintlockBarrel).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireFlintlockWeaponFirePrefix = typeof(FireFlintlockWeaponPatch).GetMethod("FirePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFireTranspiler = typeof(FireFlintlockWeaponPatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireFlintlockWeaponFirePostfix = typeof(FireFlintlockWeaponPatch).GetMethod("FirePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireFlintlockWeaponPatchBurnOffOuterOriginal, harmony, false);
            PatchController.Verify(fireFlintlockWeaponFireOriginal, harmony, false);
            try
            {
                harmony.Patch(fireFlintlockWeaponPatchBurnOffOuterOriginal, new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterPrefix), new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterPostfix), new HarmonyMethod(fireFlintlockWeaponPatchBurnOffOuterTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireFlintlockWeaponPatch to fireFlintlockWeaponPatchBurnOffOuterOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try 
            { 
                harmony.Patch(fireFlintlockWeaponFireOriginal, new HarmonyMethod(fireFlintlockWeaponFirePrefix), new HarmonyMethod(fireFlintlockWeaponFirePostfix), new HarmonyMethod(fireFlintlockWeaponFireTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireFlintlockWeaponPatch to fireFlintlockWeaponFireOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 12

            // FireHCBPatch
            MethodInfo fireHCBPatchOriginal = typeof(HCB).GetMethod("ReleaseSled", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireHCBPatchPrefix = typeof(FireHCBPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireHCBPatchPostfix = typeof(FireHCBPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireHCBPatchOriginal, harmony, false);
            harmony.Patch(fireHCBPatchOriginal, new HarmonyMethod(fireHCBPatchPrefix), new HarmonyMethod(fireHCBPatchPostfix));

            ++patchIndex; // 13

            // SimpleLauncherPatch
            MethodInfo simpleLauncherCollisionOriginal = typeof(SimpleLauncher).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo simpleLauncherCollisionTranspiler = typeof(SimpleLauncherPatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo simpleLauncherDamageOriginal = typeof(SimpleLauncherFireOnDamage).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo simpleLauncherDamagePrefix = typeof(SimpleLauncherPatch).GetMethod("DamagePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(simpleLauncherCollisionOriginal, harmony, false);
            PatchController.Verify(simpleLauncherDamageOriginal, harmony, false);
            try 
            { 
                harmony.Patch(simpleLauncherCollisionOriginal, null, null, new HarmonyMethod(simpleLauncherCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.SimpleLauncherPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(simpleLauncherDamageOriginal, new HarmonyMethod(simpleLauncherDamagePrefix));

            ++patchIndex; // 14

            // FireStingerLauncherPatch
            MethodInfo fireStingerLauncherOriginal = typeof(StingerLauncher).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo fireStingerLauncherPrefix = typeof(FireStingerLauncherPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerLauncherTranspiler = typeof(FireStingerLauncherPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerLauncherPostfix = typeof(FireStingerLauncherPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireStingerMissileOriginal = typeof(StingerMissile).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(AIEntity) }, null);
            MethodInfo fireStingerMissilePrefix = typeof(FireStingerLauncherPatch).GetMethod("MissileFirePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireStingerLauncherOriginal, harmony, false);
            PatchController.Verify(fireStingerMissileOriginal, harmony, false);
            try 
            { 
                harmony.Patch(fireStingerLauncherOriginal, new HarmonyMethod(fireStingerLauncherPrefix), new HarmonyMethod(fireStingerLauncherPostfix), new HarmonyMethod(fireStingerLauncherTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.FireStingerLauncherPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(fireStingerMissileOriginal, new HarmonyMethod(fireStingerMissilePrefix));

            ++patchIndex; // 15

            // RemoteMissileDetonatePatch
            MethodInfo remoteMissileDetonatePatchOriginal = typeof(RemoteMissile).GetMethod("Detonante", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo remoteMissileDetonatePatchPrefix = typeof(RemoteMissileDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(remoteMissileDetonatePatchOriginal, harmony, false);
            harmony.Patch(remoteMissileDetonatePatchOriginal, new HarmonyMethod(remoteMissileDetonatePatchPrefix));

            ++patchIndex; // 16

            // StingerMissileExplodePatch
            MethodInfo stingerMissileExplodePatchOriginal = typeof(StingerMissile).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo stingerMissileExplodePatchPrefix = typeof(StingerMissileExplodePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(stingerMissileExplodePatchOriginal, harmony, false);
            harmony.Patch(stingerMissileExplodePatchOriginal, new HarmonyMethod(stingerMissileExplodePatchPrefix));

            ++patchIndex; // 17

            // SosigWeaponShatterPatch
            MethodInfo sosigWeaponShatterPatchOriginal = typeof(SosigWeapon).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponShatterPatchPrefix = typeof(SosigWeaponShatterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigWeaponShatterPatchOriginal, harmony, false);
            harmony.Patch(sosigWeaponShatterPatchOriginal, new HarmonyMethod(sosigWeaponShatterPatchPrefix));

            ++patchIndex; // 18

            // SosigConfigurePatch
            MethodInfo sosigConfigurePatchOriginal = typeof(Sosig).GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigConfigurePatchPrefix = typeof(SosigConfigurePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigConfigurePatchOriginal, harmony, false);
            harmony.Patch(sosigConfigurePatchOriginal, new HarmonyMethod(sosigConfigurePatchPrefix));

            ++patchIndex; // 19

            // SosigUpdatePatch
            MethodInfo sosigUpdatePatchOriginal = typeof(Sosig).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigHandPhysUpdatePatchOriginal = typeof(Sosig).GetMethod("HandPhysUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigHandPhysUpdatePatchPrefix = typeof(SosigUpdatePatch).GetMethod("HandPhysUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigUpdatePatchOriginal, harmony, true);
            harmony.Patch(sosigUpdatePatchOriginal, new HarmonyMethod(sosigUpdatePatchPrefix));
            harmony.Patch(sosigHandPhysUpdatePatchOriginal, new HarmonyMethod(sosigHandPhysUpdatePatchPrefix));

            ++patchIndex; // 20

            // InventoryUpdatePatch
            MethodInfo sosigInvUpdatePatchOriginal = typeof(SosigInventory).GetMethod("PhysHold", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigInvUpdatePatchPrefix = typeof(SosigInvUpdatePatch).GetMethod("PhysHoldPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigInvUpdatePatchOriginal, harmony, false);
            harmony.Patch(sosigInvUpdatePatchOriginal, new HarmonyMethod(sosigInvUpdatePatchPrefix));

            ++patchIndex; // 21

            // SosigPatch
            MethodInfo sosigDiesPatchOriginal = typeof(Sosig).GetMethod("SosigDies", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigDiesPatchPrefix = typeof(SosigPatch).GetMethod("SosigDiesPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigDiesPatchPosfix = typeof(SosigPatch).GetMethod("SosigDiesPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigBodyStatePatchOriginal = typeof(Sosig).GetMethod("SetBodyState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigBodyStatePatchPrefix = typeof(SosigPatch).GetMethod("SetBodyStatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigBodyUpdatePatchOriginal = typeof(Sosig).GetMethod("BodyUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigBodyUpdatePatchTranspiler = typeof(SosigPatch).GetMethod("FootStepTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSpeechUpdatePatchOriginal = typeof(Sosig).GetMethod("SpeechUpdate_State", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigSpeechUpdatePatchTranspiler = typeof(SosigPatch).GetMethod("SpeechUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSetCurrentOrderPatchOriginal = typeof(Sosig).GetMethod("SetCurrentOrder", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetCurrentOrderPatchPrefix = typeof(SosigPatch).GetMethod("SetCurrentOrderPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigVaporizePatchOriginal = typeof(Sosig).GetMethod("Vaporize", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigVaporizePatchPrefix = typeof(SosigPatch).GetMethod("SosigVaporizePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigVaporizePatchPostfix = typeof(SosigPatch).GetMethod("SosigVaporizePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigRequestHitDecalPatchOriginal = typeof(Sosig).GetMethod("RequestHitDecal", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(SosigLink) }, null);
            MethodInfo sosigRequestHitDecalPatchPrefix = typeof(SosigPatch).GetMethod("RequestHitDecalPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigRequestHitDecalEdgePatchOriginal = typeof(Sosig).GetMethod("RequestHitDecal", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(float), typeof(SosigLink) }, null);
            MethodInfo sosigRequestHitDecalEdgePatchPrefix = typeof(SosigPatch).GetMethod("RequestHitDecalEdgePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandGuardPointPatchOriginal = typeof(Sosig).GetMethod("CommandGuardPoint", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigCommandGuardPointPatchPrefix = typeof(SosigPatch).GetMethod("CommandGuardPointPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandGuardPointPatchPostfix = typeof(SosigPatch).GetMethod("CommandGuardPointPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandAssaultPatchOriginal = typeof(Sosig).GetMethod("CommandAssaultPoint", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigCommandAssaultPatchPrefix = typeof(SosigPatch).GetMethod("CommandAssaultPointPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandAssaultPatchPostfix = typeof(SosigPatch).GetMethod("CommandAssaultPointPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandIdlePatchOriginal = typeof(Sosig).GetMethod("CommandIdle", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigCommandIdlePatchPrefix = typeof(SosigPatch).GetMethod("CommandIdlePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandIdlePatchPostfix = typeof(SosigPatch).GetMethod("CommandIdlePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigCommandPathToPatchOriginalTransform = typeof(Sosig).GetMethod("CommandPathTo", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(List<Transform>), typeof(float), typeof(Vector2), typeof(float), typeof(Sosig.SosigMoveSpeed), typeof(Sosig.PathLoopType), typeof(List<Sosig>), typeof(float), typeof(float), typeof(bool), typeof(float) }, null);
            MethodInfo sosigCommandPathToPatchOriginalVector = typeof(Sosig).GetMethod("CommandPathTo", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(List<Vector3>), typeof(List<Vector3>), typeof(float), typeof(Vector2), typeof(float), typeof(Sosig.SosigMoveSpeed), typeof(Sosig.PathLoopType), typeof(List<Sosig>), typeof(float), typeof(float), typeof(bool), typeof(float) }, null);
            MethodInfo sosigCommandPathToPatchPostfix = typeof(SosigPatch).GetMethod("CommandPathToPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigProcessColOriginal = typeof(Sosig).GetMethod("ProcessCollision", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigProcessColPrefix = typeof(SosigPatch).GetMethod("ProcessCollisionPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigDiesPatchOriginal, harmony, false);
            PatchController.Verify(sosigBodyStatePatchOriginal, harmony, false);
            PatchController.Verify(sosigBodyUpdatePatchOriginal, harmony, true);
            PatchController.Verify(sosigSpeechUpdatePatchOriginal, harmony, false);
            PatchController.Verify(sosigSetCurrentOrderPatchOriginal, harmony, false);
            PatchController.Verify(sosigRequestHitDecalPatchOriginal, harmony, false);
            PatchController.Verify(sosigRequestHitDecalEdgePatchOriginal, harmony, false);
            PatchController.Verify(sosigCommandGuardPointPatchOriginal, harmony, false);
            PatchController.Verify(sosigCommandAssaultPatchOriginal, harmony, false);
            PatchController.Verify(sosigCommandPathToPatchOriginalTransform, harmony, false);
            PatchController.Verify(sosigCommandPathToPatchOriginalVector, harmony, false);
            PatchController.Verify(sosigProcessColOriginal, harmony, false);
            harmony.Patch(sosigDiesPatchOriginal, new HarmonyMethod(sosigDiesPatchPrefix), new HarmonyMethod(sosigDiesPatchPosfix));
            harmony.Patch(sosigBodyStatePatchOriginal, new HarmonyMethod(sosigBodyStatePatchPrefix));
            try 
            { 
                harmony.Patch(sosigBodyUpdatePatchOriginal, null, null, new HarmonyMethod(sosigBodyUpdatePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.SosigActionPatch to sosigBodyUpdatePatchOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            {
                harmony.Patch(sosigSpeechUpdatePatchOriginal, null, null, new HarmonyMethod(sosigSpeechUpdatePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.SosigActionPatch to sosigSpeechUpdatePatchOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(sosigSetCurrentOrderPatchOriginal, new HarmonyMethod(sosigSetCurrentOrderPatchPrefix));
            //harmony.Patch(sosigVaporizePatchOriginal, new HarmonyMethod(sosigVaporizePatchPrefix), new HarmonyMethod(sosigVaporizePatchPostfix));
            harmony.Patch(sosigRequestHitDecalPatchOriginal, new HarmonyMethod(sosigRequestHitDecalPatchPrefix));
            harmony.Patch(sosigRequestHitDecalEdgePatchOriginal, new HarmonyMethod(sosigRequestHitDecalEdgePatchPrefix));
            harmony.Patch(sosigCommandGuardPointPatchOriginal, new HarmonyMethod(sosigCommandGuardPointPatchPrefix), new HarmonyMethod(sosigCommandGuardPointPatchPostfix));
            harmony.Patch(sosigCommandAssaultPatchOriginal, new HarmonyMethod(sosigCommandAssaultPatchPrefix), new HarmonyMethod(sosigCommandAssaultPatchPostfix));
            harmony.Patch(sosigCommandPathToPatchOriginalTransform, null, new HarmonyMethod(sosigCommandPathToPatchPostfix));
            harmony.Patch(sosigCommandPathToPatchOriginalVector, null, new HarmonyMethod(sosigCommandPathToPatchPostfix));
            harmony.Patch(sosigProcessColOriginal, new HarmonyMethod(sosigProcessColPrefix));

            ++patchIndex; // 22

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

            PatchController.Verify(sosigLinkRegisterWearablePatchOriginal, harmony, false);
            PatchController.Verify(sosigLinkDeRegisterWearablePatchOriginal, harmony, false);
            //Verify(sosigLinkExplodesPatchOriginal, harmony, false);
            PatchController.Verify(sosigLinkBreakPatchOriginal, harmony, false);
            //Verify(sosigLinkSeverPatchOriginal, harmony, false);
            //Verify(sosigLinkVaporizePatchOriginal, harmony, false);
            harmony.Patch(sosigLinkRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkRegisterWearablePatchPrefix));
            harmony.Patch(sosigLinkDeRegisterWearablePatchOriginal, new HarmonyMethod(sosigLinkDeRegisterWearablePatchPrefix));
            //harmony.Patch(sosigLinkExplodesPatchOriginal, new HarmonyMethod(sosigLinkExplodesPatchPrefix), new HarmonyMethod(sosigLinkExplodesPatchPosfix));
            harmony.Patch(sosigLinkBreakPatchOriginal, new HarmonyMethod(sosigLinkBreakPatchPrefix), new HarmonyMethod(sosigLinkBreakPatchPosfix));
            //harmony.Patch(sosigLinkSeverPatchOriginal, new HarmonyMethod(sosigLinkSeverPatchPrefix), new HarmonyMethod(sosigLinkSeverPatchPosfix));
            //harmony.Patch(sosigLinkVaporizePatchOriginal, new HarmonyMethod(sosigLinkVaporizePatchPrefix), new HarmonyMethod(sosigLinkVaporizePatchPosfix));

            ++patchIndex; // 23

            // SosigIFFPatch
            MethodInfo sosigSetIFFPatchOriginal = typeof(Sosig).GetMethod("SetIFF", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigSetOriginalIFFPatchOriginal = typeof(Sosig).GetMethod("SetOriginalIFFTeam", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSetOriginalIFFPatchPrefix = typeof(SosigIFFPatch).GetMethod("SetOriginalIFFPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigSetIFFPatchOriginal, harmony, false);
            PatchController.Verify(sosigSetOriginalIFFPatchOriginal, harmony, false);
            harmony.Patch(sosigSetIFFPatchOriginal, new HarmonyMethod(sosigSetIFFPatchPrefix));
            harmony.Patch(sosigSetOriginalIFFPatchOriginal, new HarmonyMethod(sosigSetOriginalIFFPatchPrefix));

            ++patchIndex; // 24

            // SosigEventReceivePatch
            MethodInfo sosigEventReceivePatchOriginal = typeof(Sosig).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigEventReceivePatchPrefix = typeof(SosigEventReceivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigEventReceivePatchOriginal, harmony, false);
            harmony.Patch(sosigEventReceivePatchOriginal, new HarmonyMethod(sosigEventReceivePatchPrefix));

            ++patchIndex; // 25

            // AutoMeaterUpdatePatch
            MethodInfo autoMeaterUpdatePatchOriginal = typeof(AutoMeater).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFixedUpdatePatchOriginal = typeof(AutoMeater).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdatePatchPrefix = typeof(AutoMeaterUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterUpdatePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));
            harmony.Patch(autoMeaterFixedUpdatePatchOriginal, new HarmonyMethod(autoMeaterUpdatePatchPrefix));

            ++patchIndex; // 26

            // AutoMeaterEventPatch
            MethodInfo autoMeaterEventReceivePatchOriginal = typeof(AutoMeater).GetMethod("EventReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterEventReceivePatchPrefix = typeof(AutoMeaterEventPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterEventReceivePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterEventReceivePatchOriginal, new HarmonyMethod(autoMeaterEventReceivePatchPrefix));

            ++patchIndex; // 27

            // LAPD2019ActionPatch
            MethodInfo LAPD2019PatchLoadOriginal = typeof(LAPD2019).GetMethod("LoadBattery", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo LAPD2019PatchLoadPrefix = typeof(LAPD2019ActionPatch).GetMethod("LoadBatteryPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo LAPD2019PatchExtractOriginal = typeof(LAPD2019).GetMethod("ExtractBattery", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo LAPD2019PatchExtractPrefix = typeof(LAPD2019ActionPatch).GetMethod("ExtractBatteryPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(LAPD2019PatchLoadOriginal, harmony, false);
            PatchController.Verify(LAPD2019PatchExtractOriginal, harmony, false);
            harmony.Patch(LAPD2019PatchLoadOriginal, new HarmonyMethod(LAPD2019PatchLoadPrefix));
            harmony.Patch(LAPD2019PatchExtractOriginal, new HarmonyMethod(LAPD2019PatchExtractPrefix));

            ++patchIndex; // 28

            // AutoMeaterSetStatePatch
            MethodInfo autoMeaterSetStatePatchOriginal = typeof(AutoMeater).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterSetStatePatchPostfix = typeof(AutoMeaterSetStatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterSetStatePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterSetStatePatchOriginal, null, new HarmonyMethod(autoMeaterSetStatePatchPostfix));

            ++patchIndex; // 29

            // AutoMeaterUpdateFlightPatch
            MethodInfo autoMeaterUpdateFlightPatchOriginal = typeof(AutoMeater).GetMethod("UpdateFlight", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterUpdateFlightPatchPrefix = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterUpdateFlightPatchTranspiler = typeof(AutoMeaterUpdateFlightPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterUpdateFlightPatchOriginal, harmony, false);
            try
            { 
                harmony.Patch(autoMeaterUpdateFlightPatchOriginal, new HarmonyMethod(autoMeaterUpdateFlightPatchPrefix), null, new HarmonyMethod(autoMeaterUpdateFlightPatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.AutoMeaterUpdateFlightPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 30

            // AutoMeaterFirearmFireShotPatch
            MethodInfo autoMeaterFirearmFireShotPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireShotPatchPrefix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchPostfix = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterFirearmFireShotPatchTranspiler = typeof(AutoMeaterFirearmFireShotPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterFirearmFireShotPatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(autoMeaterFirearmFireShotPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireShotPatchPrefix), new HarmonyMethod(autoMeaterFirearmFireShotPatchPostfix), new HarmonyMethod(autoMeaterFirearmFireShotPatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.AutoMeaterFirearmFireShotPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 31

            // AutoMeaterFirearmFireAtWillPatch
            MethodInfo autoMeaterFirearmFireAtWillPatchOriginal = typeof(AutoMeater.AutoMeaterFirearm).GetMethod("SetFireAtWill", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterFirearmFireAtWillPatchPrefix = typeof(AutoMeaterFirearmFireAtWillPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterFirearmFireAtWillPatchOriginal, harmony, false);
            harmony.Patch(autoMeaterFirearmFireAtWillPatchOriginal, new HarmonyMethod(autoMeaterFirearmFireAtWillPatchPrefix));

            ++patchIndex; // 32

            // EncryptionRespawnRandSubPatch
            MethodInfo encryptionRespawnRandSubPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("RespawnRandomSubTarg", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionRespawnRandSubPatchTranspiler = typeof(EncryptionRespawnRandSubPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionRespawnRandSubPatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(encryptionRespawnRandSubPatchOriginal, null, null, new HarmonyMethod(encryptionRespawnRandSubPatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.EncryptionRespawnRandSubPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 33

            // EncryptionResetGrowthPatch
            MethodInfo encryptionResetGrowthPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("ResetGrowth", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionResetGrowthPatchPrefix = typeof(EncryptionResetGrowthPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionResetGrowthPatchOriginal, harmony, false);
            harmony.Patch(encryptionResetGrowthPatchOriginal, new HarmonyMethod(encryptionResetGrowthPatchPrefix));

            ++patchIndex; // 34

            // EncryptionDisableSubtargPatch
            MethodInfo encryptionDisableSubtargPatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("DisableSubtarg", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDisableSubtargPatchPrefix = typeof(EncryptionDisableSubtargPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDisableSubtargPatchPostfix = typeof(EncryptionDisableSubtargPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionDisableSubtargPatchOriginal, harmony, false);
            harmony.Patch(encryptionDisableSubtargPatchOriginal, new HarmonyMethod(encryptionDisableSubtargPatchPrefix), new HarmonyMethod(encryptionDisableSubtargPatchPostfix));

            ++patchIndex; // 35

            // SosigTargetPrioritySystemPatch
            MethodInfo sosigTargetPrioritySystemPatchDefaultOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetDefaultIFFChart", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchMakeEnemyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("MakeEnemy", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchMakeFriendlyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("MakeFriendly", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllEnemyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllEnemy", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllFriendlyOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllFriendly", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchSetAllyMatrixOriginal = typeof(SosigTargetPrioritySystem).GetMethod("SetAllyMatrix", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigTargetPrioritySystemPatchPostfix = typeof(SosigTargetPrioritySystemPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigTargetPrioritySystemPatchDefaultOriginal, harmony, false);
            PatchController.Verify(sosigTargetPrioritySystemPatchMakeEnemyOriginal, harmony, false);
            PatchController.Verify(sosigTargetPrioritySystemPatchMakeFriendlyOriginal, harmony, false);
            PatchController.Verify(sosigTargetPrioritySystemPatchSetAllEnemyOriginal, harmony, false);
            PatchController.Verify(sosigTargetPrioritySystemPatchSetAllFriendlyOriginal, harmony, false);
            PatchController.Verify(sosigTargetPrioritySystemPatchSetAllyMatrixOriginal, harmony, false);
            harmony.Patch(sosigTargetPrioritySystemPatchDefaultOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchMakeEnemyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchMakeFriendlyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllEnemyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllFriendlyOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));
            harmony.Patch(sosigTargetPrioritySystemPatchSetAllyMatrixOriginal, null, new HarmonyMethod(sosigTargetPrioritySystemPatchPostfix));

            ++patchIndex; // 36

            // SimpleLauncher2CycleModePatch
            MethodInfo simpleLauncher2CycleModePatchOriginal = typeof(SimpleLauncher2).GetMethod("CycleMode", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo simpleLauncher2CycleModePatchPrefix = typeof(SimpleLauncher2CycleModePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(simpleLauncher2CycleModePatchOriginal, harmony, false);
            harmony.Patch(simpleLauncher2CycleModePatchOriginal, new HarmonyMethod(simpleLauncher2CycleModePatchPrefix));

            ++patchIndex; // 37

            // PinnedGrenadePatch
            MethodInfo pinnedGrenadePatchUpdateOriginal = typeof(PinnedGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchFixedUpdateOriginal = typeof(PinnedGrenade).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchUpdatePrefix = typeof(PinnedGrenadePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchUpdateTranspiler = typeof(PinnedGrenadePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchUpdatePostfix = typeof(PinnedGrenadePatch).GetMethod("UpdatePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo pinnedGrenadePatchCollisionOriginal = typeof(PinnedGrenade).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo pinnedGrenadePatchCollisionTranspiler = typeof(PinnedGrenadePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(pinnedGrenadePatchUpdateOriginal, harmony, false);
            PatchController.Verify(pinnedGrenadePatchFixedUpdateOriginal, harmony, false);
            PatchController.Verify(pinnedGrenadePatchCollisionOriginal, harmony, false);
            try 
            { 
                harmony.Patch(pinnedGrenadePatchUpdateOriginal, new HarmonyMethod(pinnedGrenadePatchUpdatePrefix), new HarmonyMethod(pinnedGrenadePatchUpdatePostfix), new HarmonyMethod(pinnedGrenadePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.PinnedGrenadePatch on pinnedGrenadePatchUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(pinnedGrenadePatchFixedUpdateOriginal, new HarmonyMethod(pinnedGrenadePatchUpdatePrefix));
            try 
            { 
                harmony.Patch(pinnedGrenadePatchCollisionOriginal, null, null, new HarmonyMethod(pinnedGrenadePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.PinnedGrenadePatch on pinnedGrenadePatchCollisionOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 38

            // FVRGrenadePatch
            MethodInfo FVRGrenadePatchUpdateOriginal = typeof(FVRGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo FVRGrenadePatchUpdatePrefix = typeof(FVRGrenadePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo FVRGrenadePatchUpdatePostfix = typeof(FVRGrenadePatch).GetMethod("UpdatePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(FVRGrenadePatchUpdateOriginal, harmony, false);
            harmony.Patch(FVRGrenadePatchUpdateOriginal, new HarmonyMethod(FVRGrenadePatchUpdatePrefix), new HarmonyMethod(FVRGrenadePatchUpdatePostfix));

            ++patchIndex; // 39

            // FusePatch
            MethodInfo FVRFusePatchIgniteOriginal = typeof(FVRFuse).GetMethod("Ignite", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo FVRFusePatchIgnitePostfix = typeof(FusePatch).GetMethod("IgnitePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo FVRFusePatchBoomOriginal = typeof(FVRFuse).GetMethod("Boom", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo FVRFusePatchBoomPrefix = typeof(FusePatch).GetMethod("BoomPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(FVRFusePatchIgniteOriginal, harmony, false);
            PatchController.Verify(FVRFusePatchBoomOriginal, harmony, false);
            harmony.Patch(FVRFusePatchIgniteOriginal, null, new HarmonyMethod(FVRFusePatchIgnitePostfix));
            harmony.Patch(FVRFusePatchBoomOriginal, new HarmonyMethod(FVRFusePatchBoomPrefix));

            ++patchIndex; // 40

            // MolotovPatch
            MethodInfo MolotovPatchShatterOriginal = typeof(Molotov).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MolotovPatchShatterPrefix = typeof(MolotovPatch).GetMethod("ShatterPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo MolotovPatchDamageOriginal = typeof(Molotov).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo MolotovPatchDamagePrefix = typeof(MolotovPatch).GetMethod("DamagePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(MolotovPatchShatterOriginal, harmony, false);
            PatchController.Verify(MolotovPatchDamageOriginal, harmony, false);
            harmony.Patch(MolotovPatchShatterOriginal, new HarmonyMethod(MolotovPatchShatterPrefix));
            harmony.Patch(MolotovPatchDamageOriginal, new HarmonyMethod(MolotovPatchDamagePrefix));

            ++patchIndex; // 41

            // EncryptionPatch
            MethodInfo EncryptionPatchUpdateOriginal = typeof(TNH_EncryptionTarget).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo EncryptionPatchUpdatePrefix = typeof(EncryptionPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchUpdateTranspiler = typeof(EncryptionPatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchFixedUpdateOriginal = typeof(TNH_EncryptionTarget).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo EncryptionPatchFixedUpdatePrefix = typeof(EncryptionPatch).GetMethod("FixedUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchStartOriginal = typeof(TNH_EncryptionTarget).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo EncryptionPatchStartPrefix = typeof(EncryptionPatch).GetMethod("StartPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchStartPostfix = typeof(EncryptionPatch).GetMethod("StartPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchUpdateDisplayOriginal = typeof(TNH_EncryptionTarget).GetMethod("UpdateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo EncryptionPatchUpdateDisplayPostfix = typeof(EncryptionPatch).GetMethod("UpdateDisplayPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchDestroyOriginal = typeof(TNH_EncryptionTarget).GetMethod("Destroy", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo EncryptionPatchDestroyTranspiler = typeof(EncryptionPatch).GetMethod("DestroyTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo EncryptionPatchFireGunOriginal = typeof(TNH_EncryptionTarget).GetMethod("FireGun", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo EncryptionPatchFireGunPrefix = typeof(EncryptionPatch).GetMethod("FireGunPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(EncryptionPatchUpdateOriginal, harmony, true);
            PatchController.Verify(EncryptionPatchFixedUpdateOriginal, harmony, true);
            PatchController.Verify(EncryptionPatchStartOriginal, harmony, true);
            PatchController.Verify(EncryptionPatchUpdateDisplayOriginal, harmony, true);
            PatchController.Verify(EncryptionPatchDestroyOriginal, harmony, true);
            PatchController.Verify(EncryptionPatchFireGunOriginal, harmony, true);
            try 
            { 
                harmony.Patch(EncryptionPatchUpdateOriginal, new HarmonyMethod(EncryptionPatchUpdatePrefix), null, new HarmonyMethod(EncryptionPatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.EncryptionPatch on EncryptionPatchUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(EncryptionPatchFixedUpdateOriginal, new HarmonyMethod(EncryptionPatchFixedUpdatePrefix));
            harmony.Patch(EncryptionPatchStartOriginal, new HarmonyMethod(EncryptionPatchStartPrefix), new HarmonyMethod(EncryptionPatchStartPostfix));
            harmony.Patch(EncryptionPatchUpdateDisplayOriginal, null, new HarmonyMethod(EncryptionPatchUpdateDisplayPostfix));
            try 
            { 
                harmony.Patch(EncryptionPatchDestroyOriginal, null, null, new HarmonyMethod(EncryptionPatchDestroyTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.EncryptionPatch on EncryptionPatchDestroyOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(EncryptionPatchFireGunOriginal, new HarmonyMethod(EncryptionPatchFireGunPrefix));

            ++patchIndex; // 42

            // BangSnapPatch
            MethodInfo bangSnapPatchSplodeOriginal = typeof(BangSnap).GetMethod("Splode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo bangSnapPatchSplodePrefix = typeof(BangSnapPatch).GetMethod("SplodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo bangSnapPatchCollisionOriginal = typeof(BangSnap).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bangSnapPatchCollisionTranspiler = typeof(BangSnapPatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(bangSnapPatchSplodeOriginal, harmony, false);
            PatchController.Verify(bangSnapPatchCollisionOriginal, harmony, false);
            harmony.Patch(bangSnapPatchSplodeOriginal, new HarmonyMethod(bangSnapPatchSplodePrefix));
            try 
            { 
                harmony.Patch(bangSnapPatchCollisionOriginal, null, null, new HarmonyMethod(bangSnapPatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.BangSnapPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 43

            // C4DetonatePatch
            MethodInfo C4DetonatePatchOriginal = typeof(C4).GetMethod("Detonate", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo C4DetonatePatchPrefix = typeof(C4DetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(C4DetonatePatchOriginal, harmony, false);
            harmony.Patch(C4DetonatePatchOriginal, new HarmonyMethod(C4DetonatePatchPrefix));

            ++patchIndex; // 44

            // ClaymoreMineDetonatePatch
            MethodInfo claymoreMineDetonatePatchOriginal = typeof(ClaymoreMine).GetMethod("Detonate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo claymoreMineDetonatePatchPrefix = typeof(ClaymoreMineDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(claymoreMineDetonatePatchOriginal, harmony, false);
            harmony.Patch(claymoreMineDetonatePatchOriginal, new HarmonyMethod(claymoreMineDetonatePatchPrefix));

            ++patchIndex; // 45

            // SLAMDetonatePatch
            MethodInfo SLAMDetonatePatchOriginal = typeof(SLAM).GetMethod("Detonate", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo SLAMDetonatePatchPrefix = typeof(SLAMDetonatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(SLAMDetonatePatchOriginal, harmony, false);
            harmony.Patch(SLAMDetonatePatchOriginal, new HarmonyMethod(SLAMDetonatePatchPrefix));

            ++patchIndex; // 46

            // RoundPatch
            MethodInfo roundPatchFixedUpdateOriginal = typeof(FVRFireArmRound).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo roundPatchFixedUpdateTranspiler = typeof(RoundPatch).GetMethod("FixedUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo roundPatchSplodeOriginal = typeof(FVRFireArmRound).GetMethod("Splode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo roundPatchSplodePrefix = typeof(RoundPatch).GetMethod("SplodePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(roundPatchFixedUpdateOriginal, harmony, false);
            PatchController.Verify(roundPatchSplodeOriginal, harmony, false);
            try 
            { 
                harmony.Patch(roundPatchFixedUpdateOriginal, null, null, new HarmonyMethod(roundPatchFixedUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.RoundPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(roundPatchSplodeOriginal, new HarmonyMethod(roundPatchSplodePrefix));

            ++patchIndex; // 47

            // MagazinePatch
            MethodInfo magAddRoundClassOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundClassTranspiler = typeof(MagazinePatch).GetMethod("AddRoundClassTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magAddRoundRoundOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundRoundTranspiler = typeof(MagazinePatch).GetMethod("AddRoundRoundTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magLoadFireArmOriginal = typeof(FVRFireArmMagazine).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArm) }, null);
            MethodInfo magLoadFireArmPrefix = typeof(MagazinePatch).GetMethod("LoadFireArmPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magLoadIntoSecondaryOriginal = typeof(FVRFireArmMagazine).GetMethod("LoadIntoSecondary", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo magLoadIntoSecondaryPrefix = typeof(MagazinePatch).GetMethod("LoadIntoSecondaryPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magLoadAttachableOriginal = typeof(FVRFireArmMagazine).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(AttachableFirearm) }, null);
            MethodInfo magLoadAttachablePrefix = typeof(MagazinePatch).GetMethod("LoadAttachablePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(magAddRoundClassOriginal, harmony, false);
            PatchController.Verify(magAddRoundRoundOriginal, harmony, false);
            PatchController.Verify(magLoadFireArmOriginal, harmony, false);
            PatchController.Verify(magLoadIntoSecondaryOriginal, harmony, false);
            PatchController.Verify(magLoadAttachableOriginal, harmony, false);
            try 
            { 
                harmony.Patch(magAddRoundClassOriginal, null, null, new HarmonyMethod(magAddRoundClassTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.MagazinePatch on magAddRoundClassOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(magAddRoundRoundOriginal, null, null, new HarmonyMethod(magAddRoundRoundTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.MagazinePatch on magAddRoundRoundOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(magLoadFireArmOriginal, new HarmonyMethod(magLoadFireArmPrefix));
            harmony.Patch(magLoadIntoSecondaryOriginal, new HarmonyMethod(magLoadIntoSecondaryPrefix));
            harmony.Patch(magLoadAttachableOriginal, new HarmonyMethod(magLoadAttachablePrefix));

            ++patchIndex; // 48

            // ClipPatch
            MethodInfo clipAddRoundClassOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundClassTranspiler = typeof(ClipPatch).GetMethod("AddRoundClassTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipAddRoundRoundOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundRoundTranspiler = typeof(ClipPatch).GetMethod("AddRoundRoundTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipLoadOriginal = typeof(FVRFireArmClip).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipLoadPrefix = typeof(ClipPatch).GetMethod("LoadPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(clipAddRoundClassOriginal, harmony, false);
            PatchController.Verify(clipAddRoundRoundOriginal, harmony, false);
            PatchController.Verify(clipLoadOriginal, harmony, false);
            try 
            { 
                harmony.Patch(clipAddRoundClassOriginal, null, null, new HarmonyMethod(clipAddRoundClassTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.ClipPatch on clipAddRoundClassOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try 
            { 
                harmony.Patch(clipAddRoundRoundOriginal, null, null, new HarmonyMethod(clipAddRoundRoundTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.ClipPatch on clipAddRoundRoundOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            harmony.Patch(clipLoadOriginal, new HarmonyMethod(clipLoadPrefix));

            ++patchIndex; // 49

            // SpeedloaderChamberPatch
            MethodInfo SLChamberLoadOriginal = typeof(SpeedloaderChamber).GetMethod("Load", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo SLChamberLoadPrefix = typeof(SpeedloaderChamberPatch).GetMethod("AddRoundPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(SLChamberLoadOriginal, harmony, false);
            harmony.Patch(SLChamberLoadOriginal, new HarmonyMethod(SLChamberLoadPrefix));

            ++patchIndex; // 50

            // RemoteGunPatch
            MethodInfo remoteGunChamberOriginal = typeof(RemoteGun).GetMethod("ChamberCartridge", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo remoteGunChamberPrefix = typeof(RemoteGunPatch).GetMethod("ChamberCartridgePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(remoteGunChamberOriginal, harmony, false);
            harmony.Patch(remoteGunChamberOriginal, new HarmonyMethod(remoteGunChamberPrefix));

            ++patchIndex; // 51

            // ChamberPatch
            MethodInfo chamberSetRoundClassOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(Vector3), typeof(Quaternion) }, null);
            MethodInfo chamberSetRoundClassPrefix = typeof(ChamberPatch).GetMethod("SetRoundClassPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo chamberSetRoundRoundVectorOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(Vector3), typeof(Quaternion) }, null);
            MethodInfo chamberSetRoundRoundBoolOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool) }, null);
            MethodInfo chamberSetRoundRoundPrefix = typeof(ChamberPatch).GetMethod("SetRoundRoundPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(chamberSetRoundClassOriginal, harmony, false);
            PatchController.Verify(chamberSetRoundRoundVectorOriginal, harmony, false);
            PatchController.Verify(chamberSetRoundRoundBoolOriginal, harmony, false);
            harmony.Patch(chamberSetRoundClassOriginal, new HarmonyMethod(chamberSetRoundClassPrefix));
            harmony.Patch(chamberSetRoundRoundVectorOriginal, new HarmonyMethod(chamberSetRoundRoundPrefix));
            harmony.Patch(chamberSetRoundRoundBoolOriginal, new HarmonyMethod(chamberSetRoundRoundPrefix));

            ++patchIndex; // 52

            // SpeedloaderPatch
            MethodInfo speedLoaderFixedUpdateOriginal = typeof(Speedloader).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo speedLoaderFixedUpdateTranspiler = typeof(SpeedloaderPatch).GetMethod("FixedUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo speedLoaderUpdateOriginal = typeof(Speedloader).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo speedLoaderUpdateTranspiler = typeof(SpeedloaderPatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(speedLoaderFixedUpdateOriginal, harmony, false);
            PatchController.Verify(speedLoaderUpdateOriginal, harmony, false);
            try 
            { 
                harmony.Patch(speedLoaderFixedUpdateOriginal, null, null, new HarmonyMethod(speedLoaderFixedUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.SpeedloaderPatch on speedLoaderFixedUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            {
                harmony.Patch(speedLoaderUpdateOriginal, null, null, new HarmonyMethod(speedLoaderUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.SpeedloaderPatch on speedLoaderUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 53

            // RevolverCylinderPatch
            MethodInfo revolverCylinderLoadOriginal = typeof(RevolverCylinder).GetMethod("LoadFromSpeedLoader", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo revolverCylinderLoadPrefix = typeof(RevolverCylinderPatch).GetMethod("LoadFromSpeedLoaderPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(revolverCylinderLoadOriginal, harmony, false);
            harmony.Patch(revolverCylinderLoadOriginal, new HarmonyMethod(revolverCylinderLoadPrefix));

            ++patchIndex; // 54

            // RevolvingShotgunPatch
            MethodInfo revolvingShotgunLoadOriginal = typeof(RevolvingShotgun).GetMethod("LoadCylinder", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo revolvingShotgunLoadPrefix = typeof(RevolvingShotgunPatch).GetMethod("LoadCylinderPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(revolvingShotgunLoadOriginal, harmony, false);
            harmony.Patch(revolvingShotgunLoadOriginal, new HarmonyMethod(revolvingShotgunLoadPrefix));

            ++patchIndex; // 55

            // GrappleGunPatch
            MethodInfo grappleGunLoadOriginal = typeof(GrappleGun).GetMethod("LoadCylinder", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo grappleGunLoadPrefix = typeof(GrappleGunPatch).GetMethod("LoadCylinderPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(grappleGunLoadOriginal, harmony, false);
            harmony.Patch(grappleGunLoadOriginal, new HarmonyMethod(grappleGunLoadPrefix));

            ++patchIndex; // 56

            // CarlGustafLatchPatch
            MethodInfo carlGustafLatchUpdateOriginal = typeof(CarlGustafLatch).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo carlGustafLatchUpdateTranspiler = typeof(CarlGustafLatchPatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(carlGustafLatchUpdateOriginal, harmony, false);
            try 
            { 
                harmony.Patch(carlGustafLatchUpdateOriginal, null, null, new HarmonyMethod(carlGustafLatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.CarlGustafLatchPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 57

            // CarlGustafShellInsertEjectPatch
            MethodInfo carlGustafShellSlideUpdateOriginal = typeof(CarlGustafShellInsertEject).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo carlGustafShellSlideUpdateTranspiler = typeof(CarlGustafShellInsertEjectPatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(carlGustafShellSlideUpdateOriginal, harmony, false);
            try 
            { 
                harmony.Patch(carlGustafShellSlideUpdateOriginal, null, null, new HarmonyMethod(carlGustafShellSlideUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.CarlGustafShellInsertEjectPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 58

            // GrappleThrowablePatch
            MethodInfo grappleThrowableCollisionOriginal = typeof(GrappleThrowable).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo grappleThrowableCollisionTranspiler = typeof(GrappleThrowablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(grappleThrowableCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(grappleThrowableCollisionOriginal, null, null, new HarmonyMethod(grappleThrowableCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.GrappleThrowablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 59

            // EncryptionSpawnGrowthPatch
            MethodInfo encryptionSpawnGrowthOriginal = typeof(TNH_EncryptionTarget).GetMethod("SpawnGrowth", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionSpawnGrowthPrefix = typeof(EncryptionSpawnGrowthPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionSpawnGrowthOriginal, harmony, false);
            harmony.Patch(encryptionSpawnGrowthOriginal, new HarmonyMethod(encryptionSpawnGrowthPrefix));

            ++patchIndex; // 60

            // FireArmPatch
            MethodInfo fireArmAwakeOriginal = typeof(FVRFireArm).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo fireArmAwakePostfix = typeof(FireArmPatch).GetMethod("AwakePostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireArmPlayAudioGunShotOriginalRound = typeof(FVRFireArm).GetMethod("PlayAudioGunShot", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(FVRFireArmRound), typeof(FVRSoundEnvironment), typeof(float) }, null);
            MethodInfo fireArmPlayAudioGunShotRoundPrefix = typeof(FireArmPatch).GetMethod("PlayAudioGunShotRoundPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireArmPlayAudioGunShotOriginalBool = typeof(FVRFireArm).GetMethod("PlayAudioGunShot", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(bool), typeof(FVRTailSoundClass), typeof(FVRTailSoundClass), typeof(FVRSoundEnvironment) }, null);
            MethodInfo fireArmPlayAudioGunShotBoolPrefix = typeof(FireArmPatch).GetMethod("PlayAudioGunShotBoolPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(fireArmAwakeOriginal, harmony, false);
            PatchController.Verify(fireArmPlayAudioGunShotOriginalRound, harmony, false);
            PatchController.Verify(fireArmPlayAudioGunShotOriginalBool, harmony, false);
            harmony.Patch(fireArmAwakeOriginal, null, new HarmonyMethod(fireArmAwakePostfix));
            harmony.Patch(fireArmPlayAudioGunShotOriginalRound, new HarmonyMethod(fireArmPlayAudioGunShotRoundPrefix));
            harmony.Patch(fireArmPlayAudioGunShotOriginalBool, new HarmonyMethod(fireArmPlayAudioGunShotBoolPrefix));

            ++patchIndex; // 61

            // SteelPopTargetPatch
            MethodInfo steelPopTargetStartOriginal = typeof(SteelPopTarget).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo steelPopTargetStartPostfix = typeof(SteelPopTargetPatch).GetMethod("StartPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(steelPopTargetStartOriginal, harmony, false);
            harmony.Patch(steelPopTargetStartOriginal, null, new HarmonyMethod(steelPopTargetStartPostfix));

            ++patchIndex; // 62

            // FlameThrowerPatch
            MethodInfo flameThrowerUpdateControlsOriginal = typeof(FlameThrower).GetMethod("UpdateControls", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flameThrowerUpdateControlsPrefix = typeof(FlameThrowerPatch).GetMethod("UpdateControlsPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(flameThrowerUpdateControlsOriginal, harmony, false);
            harmony.Patch(flameThrowerUpdateControlsOriginal, new HarmonyMethod(flameThrowerUpdateControlsPrefix));

            ++patchIndex; // 63

            // AR15SightFlipperPatch
            MethodInfo AR15SightFlipperInteractOriginal = typeof(AR15HandleSightFlipper).GetMethod("SimpleInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo AR15SightFlipperInteractPostfix = typeof(AR15SightFlipperPatch).GetMethod("InteractPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(AR15SightFlipperInteractOriginal, harmony, false);
            harmony.Patch(AR15SightFlipperInteractOriginal, null, new HarmonyMethod(AR15SightFlipperInteractPostfix));

            ++patchIndex; // 64

            // AR15SightRaiserPatch
            MethodInfo AR15SightRaiserInteractOriginal = typeof(AR15HandleSightRaiser).GetMethod("SimpleInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo AR15SightRaiserInteractPostfix = typeof(AR15SightRaiserPatch).GetMethod("InteractPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(AR15SightRaiserInteractOriginal, harmony, false);
            harmony.Patch(AR15SightRaiserInteractOriginal, null, new HarmonyMethod(AR15SightRaiserInteractPostfix));

            ++patchIndex; // 65

            // GatlingGunPatch
            MethodInfo gatlingGunFireShotOriginal = typeof(wwGatlingGun).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo gatlingGunFireShotPostfix = typeof(GatlingGunPatch).GetMethod("FireShotPostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(gatlingGunFireShotOriginal, harmony, false);
            harmony.Patch(gatlingGunFireShotOriginal, null, new HarmonyMethod(gatlingGunFireShotPostfix));

            ++patchIndex; // 66

            // GasCuboidPatch
            MethodInfo gasCuboidGenerateGoutOriginal = typeof(Brut_GasCuboid).GetMethod("GenerateGout", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo gasCuboidGenerateGoutPrefix = typeof(GasCuboidPatch).GetMethod("GenerateGoutPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo gasCuboidDamageHandleOriginal = typeof(Brut_GasCuboid).GetMethod("DamageHandle", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo gasCuboidDamageHandlePrefix = typeof(GasCuboidPatch).GetMethod("DamageHandlePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo gasCuboidUpdateOriginal = typeof(Brut_GasCuboid).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo gasCuboidFixedUpdateOriginal = typeof(Brut_GasCuboid).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo gasCuboidUpdatePrefix = typeof(GasCuboidPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo gasCuboidExplodeOriginal = typeof(Brut_GasCuboid).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo gasCuboidExplodePrefix = typeof(GasCuboidPatch).GetMethod("ExplodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo gasCuboidExplodePostfix = typeof(GasCuboidPatch).GetMethod("ExplodePostfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(gasCuboidGenerateGoutOriginal, harmony, false);
            PatchController.Verify(gasCuboidDamageHandleOriginal, harmony, false);
            PatchController.Verify(gasCuboidUpdateOriginal, harmony, false);
            PatchController.Verify(gasCuboidFixedUpdateOriginal, harmony, false);
            PatchController.Verify(gasCuboidExplodeOriginal, harmony, false);
            harmony.Patch(gasCuboidGenerateGoutOriginal, new HarmonyMethod(gasCuboidGenerateGoutPrefix));
            harmony.Patch(gasCuboidDamageHandleOriginal, new HarmonyMethod(gasCuboidDamageHandlePrefix));
            harmony.Patch(gasCuboidUpdateOriginal, new HarmonyMethod(gasCuboidUpdatePrefix));
            harmony.Patch(gasCuboidFixedUpdateOriginal, new HarmonyMethod(gasCuboidUpdatePrefix));
            harmony.Patch(gasCuboidExplodeOriginal, new HarmonyMethod(gasCuboidExplodePrefix), new HarmonyMethod(gasCuboidExplodePostfix));

            ++patchIndex; // 67

            // FloaterPatch
            MethodInfo floaterBeginExplodingOriginal = typeof(Construct_Floater).GetMethod("BeginExploding", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo floaterBeginExplodingPrefix = typeof(FloaterPatch).GetMethod("BeginExplodingPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo floaterBeginDefusingOriginal = typeof(Construct_Floater).GetMethod("BeginDefusing", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo floaterBeginDefusingPrefix = typeof(FloaterPatch).GetMethod("BeginDefusingPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo floaterExplodeOriginal = typeof(Construct_Floater).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo floaterExplodePrefix = typeof(FloaterPatch).GetMethod("ExplodePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo floaterUpdateOriginal = typeof(Construct_Floater).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo floaterUpdatePrefix = typeof(FloaterPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo floaterFixedUpdateOriginal = typeof(Construct_Floater).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo floaterFixedUpdatePrefix = typeof(FloaterPatch).GetMethod("FixedUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(floaterBeginExplodingOriginal, harmony, false);
            PatchController.Verify(floaterBeginDefusingOriginal, harmony, false);
            PatchController.Verify(floaterExplodeOriginal, harmony, false);
            PatchController.Verify(floaterUpdateOriginal, harmony, false);
            PatchController.Verify(floaterFixedUpdateOriginal, harmony, false);
            harmony.Patch(floaterBeginExplodingOriginal, new HarmonyMethod(floaterBeginExplodingPrefix));
            harmony.Patch(floaterBeginDefusingOriginal, new HarmonyMethod(floaterBeginDefusingPrefix));
            harmony.Patch(floaterExplodeOriginal, new HarmonyMethod(floaterExplodePrefix));
            harmony.Patch(floaterUpdateOriginal, new HarmonyMethod(floaterUpdatePrefix));
            harmony.Patch(floaterFixedUpdateOriginal, new HarmonyMethod(floaterFixedUpdatePrefix));

            ++patchIndex; // 68

            // IrisPatch
            MethodInfo irisUpdateOriginal = typeof(Construct_Iris).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo irisUpdatePrefix = typeof(IrisPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo irisFixedUpdateOriginal = typeof(Construct_Iris).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo irisFixedUpdatePrefix = typeof(IrisPatch).GetMethod("FixedUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo irisSetStateOriginal = typeof(Construct_Iris).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo irisSetStatePrefix = typeof(IrisPatch).GetMethod("SetStatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(irisUpdateOriginal, harmony, false);
            PatchController.Verify(irisSetStateOriginal, harmony, false);
            PatchController.Verify(irisFixedUpdateOriginal, harmony, false);
            harmony.Patch(irisUpdateOriginal, new HarmonyMethod(irisUpdatePrefix));
            harmony.Patch(irisFixedUpdateOriginal, new HarmonyMethod(irisFixedUpdatePrefix));
            harmony.Patch(irisSetStateOriginal, new HarmonyMethod(irisSetStatePrefix));

            ++patchIndex; // 69

            // BrutBlockSystemPatch
            MethodInfo brutBlockSystemUpdateOriginal = typeof(BrutBlockSystem).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutBlockSystemUpdatePrefix = typeof(BrutBlockSystemPatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo brutBlockSystemStartOriginal = typeof(BrutBlockSystem).GetMethod("TryToStartBlock", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutBlockSystemStartPrefix = typeof(BrutBlockSystemPatch).GetMethod("TryToStartBlockPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(brutBlockSystemUpdateOriginal, harmony, false);
            PatchController.Verify(brutBlockSystemStartOriginal, harmony, false);
            harmony.Patch(brutBlockSystemUpdateOriginal, new HarmonyMethod(brutBlockSystemUpdatePrefix));
            harmony.Patch(brutBlockSystemStartOriginal, new HarmonyMethod(brutBlockSystemStartPrefix));

            ++patchIndex; // 70

            // NodePatch
            MethodInfo nodeInitOriginal = typeof(Construct_Node).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo nodeInitPrefix = typeof(NodePatch).GetMethod("InitPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo nodeInitPostfix = typeof(NodePatch).GetMethod("InitPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo nodeUpdateOriginal = typeof(Construct_Node).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo nodeUpdatePrefix = typeof(NodePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo nodeFixedUpdateOriginal = typeof(Construct_Node).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo nodeFixedUpdatePrefix = typeof(NodePatch).GetMethod("FixedUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo nodeFixedUpdateTranspiler = typeof(NodePatch).GetMethod("FixedUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(nodeInitOriginal, harmony, false);
            PatchController.Verify(nodeUpdateOriginal, harmony, false);
            PatchController.Verify(nodeFixedUpdateOriginal, harmony, false);
            harmony.Patch(nodeInitOriginal, new HarmonyMethod(nodeInitPrefix), new HarmonyMethod(nodeInitPostfix));
            harmony.Patch(nodeUpdateOriginal, new HarmonyMethod(nodeUpdatePrefix));
            try
            {
                harmony.Patch(nodeFixedUpdateOriginal, new HarmonyMethod(nodeFixedUpdatePrefix), null, new HarmonyMethod(nodeFixedUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying ActionPatches.NodePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 71

            // HazePatch
            MethodInfo hazeUpdateOriginal = typeof(Construct_Haze).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo hazeUpdatePrefix = typeof(HazePatch).GetMethod("UpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo hazeFixedUpdateOriginal = typeof(Construct_Haze).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo hazeFixedUpdatePrefix = typeof(HazePatch).GetMethod("FixedUpdatePrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(hazeUpdateOriginal, harmony, false);
            PatchController.Verify(hazeFixedUpdateOriginal, harmony, false);
            harmony.Patch(hazeUpdateOriginal, new HarmonyMethod(hazeUpdatePrefix));
            harmony.Patch(hazeFixedUpdateOriginal, new HarmonyMethod(hazeFixedUpdatePrefix));
        }
    }

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
        public static FireArmRoundType roundType;

        static void Prefix(FVRFireArmChamber chamber)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at firepatch prefix, setting to 1"); Mod.skipAllInstantiates = 1; }

            FVRFireArmRound round = chamber.GetRound();
            if (round == null)
            {
                fireSuccessful = false;
            }
            else
            {
                fireSuccessful = true;
                roundClass = round.RoundClass;
                roundType = round.RoundType;
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

            bool applied0 = false;
            bool applied1 = false;
            bool skippedFirstDir = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("op_Subtraction"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied0 = true;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_forward"))
                {
                    if (skippedFirstDir)
                    {
                        instructionList.InsertRange(i + 1, toInsert1);
                        applied1 = true;
                    }
                    else
                    {
                        skippedFirstDir = true;
                    }
                }
            }

            if(!applied0 || !applied1)
            {
                Mod.LogError("FirePatch Transpiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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

        static void Postfix(ref FVRFireArm __instance, FVRFireArmChamber chamber)
        {
            --Mod.skipAllInstantiates;

            // Skip sending will prevent fire patch from handling its own data, as we want to handle it elsewhere
            if (skipSending > 0)
            {
                return;
            }

            overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                positions = null;
                directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        int chamberIndex = -1;
                        for (int i = 0; i < __instance.GetChambers().Count; ++i)
                        {
                            if (__instance.GetChambers()[i] == chamber)
                            {
                                chamberIndex = i;
                                break;
                            }
                        }
                        ServerSend.WeaponFire(0, trackedItem.data.trackedID, roundType, roundClass, positions, directions, chamberIndex);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    int chamberIndex = 0;
                    for (int i = 0; i < __instance.GetChambers().Count; ++i)
                    {
                        if (__instance.GetChambers()[i] == chamber)
                        {
                            chamberIndex = i;
                            break;
                        }
                    }
                    ClientSend.WeaponFire(trackedItem.data.trackedID, roundType, roundClass, positions, directions, chamberIndex);
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at sosig weapon fire patch, setting to 1"); Mod.skipAllInstantiates = 1; }

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

            bool applied0 = false;
            bool applied1 = false;
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
                        applied0 = true;
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
                        applied1 = true;
                        break;
                    }
                    else
                    {
                        skippedFirstDir = true;
                    }
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("FireSosigWeaponPatch Transpiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (!fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.trackedID != -1)
                {
                    // Send the fire action to other clients only if we control it
                    if (ThreadManager.host)
                    {
                        if (trackedItem.data.controller == 0)
                        {
                            ServerSend.SosigWeaponFire(0, trackedItem.data.trackedID, recoilMult, positions, directions);
                        }
                    }
                    else if (trackedItem.data.controller == Client.singleton.ID)
                    {
                        ClientSend.SosigWeaponFire(trackedItem.data.trackedID, recoilMult, positions, directions);
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
        static FireArmRoundType roundType;

        static void Prefix(ref LAPD2019 __instance, bool ___m_isCapacitorCharged)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at FireLAPD2019Patch, setting to 1"); Mod.skipAllInstantiates = 1; }

            curChamber = __instance.CurChamber;
            if (__instance.Chambers[__instance.CurChamber].GetRound() != null)
            {
                roundType = __instance.Chambers[__instance.CurChamber].GetRound().RoundType;
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

            bool applied0 = false;
            bool applied1 = false;
            bool applied2 = false;
            bool applied3 = false;
            bool foundFirstPos = false;
            bool foundFirstDir = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("GetMuzzle") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_position"))
                {
                    if (foundFirstPos)
                    {
                        instructionList.InsertRange(i + 2, toInsert2);
                        applied1 = true;
                    }
                    else
                    {
                        instructionList.InsertRange(i + 2, toInsert0);
                        applied0 = true;
                        foundFirstPos = true;
                    }
                }

                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_transform") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_forward"))
                {
                    if (foundFirstDir)
                    {
                        instructionList.InsertRange(i + 2, toInsert3);
                        applied3 = true;
                        break;
                    }
                    else
                    {
                        instructionList.InsertRange(i + 2, toInsert1);
                        applied2 = true;
                        foundFirstDir = true;
                    }
                }
            }

            if (!applied0 || !applied1 || !applied2 || !applied3)
            {
                Mod.LogError("FireLAPD2019Patch Transpiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (!fireSucessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.LAPD2019Fire(0, trackedItem.data.trackedID, curChamber, roundType, roundClass, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.LAPD2019Fire(trackedItem.data.trackedID, curChamber, roundType, roundClass, positions, directions);
                }
            }

            positions = null;
            directions = null;
        }
    }

    class FireAttachableFirearmPatch
    {
        public static bool overriden;
        public static List<Vector3> positions;
        public static List<Vector3> directions;

        // Update override data
        static bool fireSuccessful;
        static FireArmRoundType roundType;
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

            bool applied0 = false;
            bool applied1 = false;
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
                    applied0 = true;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_transform") &&
                    instructionList[i + 1].opcode == OpCodes.Callvirt && instructionList[i + 1].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 2, toInsert1);
                    applied1 = true;
                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("FireAttachableFirearmPatch Transpiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at FireAttachableFirearmPatch, setting to 1"); Mod.skipAllInstantiates = 1; }

            if (__instance.Attachment == null)
            {
                if (__instance.OverrideFA)
                {
                    TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.OverrideFA, out TrackedItem item) ? item : __instance.OverrideFA.GetComponent<TrackedItem>();
                    if (trackedItem != null && trackedItem.attachableFirearmGetChamberFunc().GetRound() != null)
                    {
                        fireSuccessful = true;
                        roundType = trackedItem.attachableFirearmGetChamberFunc().GetRound().RoundType;
                        roundClass = trackedItem.attachableFirearmGetChamberFunc().GetRound().RoundClass;
                    }
                    else
                    {
                        fireSuccessful = false;
                    }
                }
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.Attachment, out TrackedItem item) ? item : __instance.Attachment.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.attachableFirearmGetChamberFunc().GetRound() != null)
                {
                    fireSuccessful = true;
                    roundType = trackedItem.attachableFirearmGetChamberFunc().GetRound().RoundType;
                    roundClass = trackedItem.attachableFirearmGetChamberFunc().GetRound().RoundClass;
                }
                else
                {
                    fireSuccessful = false;
                }
            }
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
            if (!fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = null;
            if (__instance.Attachment != null)
            {
                trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance.Attachment) ? GameManager.trackedItemByItem[__instance.Attachment] : __instance.Attachment.GetComponent<TrackedItem>();
            }
            if (trackedItem == null) // This AttachableFirearm isn't independent, it is integrated into its OverrideFA
            {
                trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance.OverrideFA) ? GameManager.trackedItemByItem[__instance.OverrideFA] : __instance.OverrideFA.GetComponent<TrackedItem>();
                if (trackedItem != null)
                {
                    if (ThreadManager.host)
                    {
                        if (trackedItem.data.controller == 0)
                        {
                            ServerSend.IntegratedFirearmFire(0, trackedItem.data.trackedID, roundType, roundClass, positions, directions);
                        }
                    }
                    else if (trackedItem.data.controller == Client.singleton.ID)
                    {
                        ClientSend.IntegratedFirearmFire(trackedItem.data.trackedID, roundType, roundClass, positions, directions);
                    }
                }
            }
            else
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.AttachableFirearmFire(0, trackedItem.data.trackedID, roundType, roundClass, firedFromInterface, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.AttachableFirearmFire(trackedItem.data.trackedID, roundType, roundClass, firedFromInterface, positions, directions);
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

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.RevolvingShotgunFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.RevolvingShotgunFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
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

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.RevolverFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.RevolverFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches SingleActionRevolver.Fire so we can track fire action
    class FireSingleActionRevolverPatch
    {
        static void Prefix(SingleActionRevolver __instance)
        {
            ++FirePatch.skipSending;

            FVRFireArmChamber chamber = __instance.Cylinder.Chambers[__instance.CurChamber];
            FirePatch.fireSuccessful = chamber.IsFull && chamber.GetRound() != null && !chamber.IsSpent;
        }

        static void Postfix(ref SingleActionRevolver __instance)
        {
            --FirePatch.skipSending;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.SingleActionRevolverFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.SingleActionRevolverFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, __instance.CurChamber, FirePatch.positions, FirePatch.directions);
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

        static void Prefix(GrappleGun __instance, int ___m_curChamber)
        {
            ++FirePatch.skipSending;

            FVRFireArmChamber chamber = __instance.Chambers[___m_curChamber];
            FirePatch.fireSuccessful = chamber.IsFull && chamber.GetRound() != null && !chamber.IsSpent;

            preChamber = ___m_curChamber;
        }

        static void Postfix(ref GrappleGun __instance)
        {
            --FirePatch.skipSending;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.GrappleGunFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, preChamber, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.GrappleGunFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, preChamber, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches Derringer.FireBarrel so we can skip 
    class FireDerringerPatch
    {
        static void Prefix(Derringer __instance, Derringer.HingeState ___m_hingeState, int ___m_curBarrel)
        {
            ++FirePatch.skipSending;

            FVRFireArmChamber chamber = __instance.Barrels[___m_curBarrel].Chamber;
            FirePatch.fireSuccessful = ___m_hingeState == Derringer.HingeState.Closed && chamber.IsFull && chamber.GetRound() != null && !chamber.IsSpent;
        }

        static void Postfix(ref Derringer __instance, int i)
        {
            --FirePatch.skipSending;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.DerringerFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, i, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.DerringerFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, i, FirePatch.positions, FirePatch.directions);
                }
            }

            FirePatch.positions = null;
            FirePatch.directions = null;
        }
    }

    // Patches BreakActionWeapon.Fire so we can skip 
    class FireBreakActionWeaponPatch
    {
        static void Prefix(BreakActionWeapon __instance, int b)
        {
            ++FirePatch.skipSending;

            FVRFireArmChamber chamber = __instance.Barrels[b].Chamber;
            FirePatch.fireSuccessful = chamber.IsFull && chamber.GetRound() != null && !chamber.IsSpent;
        }

        static void Postfix(ref BreakActionWeapon __instance, int b)
        {
            --FirePatch.skipSending;

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.BreakActionWeaponFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, b, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.BreakActionWeaponFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, b, FirePatch.positions, FirePatch.directions);
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

            FirePatch.overriden = false;

            if (Mod.skipNextFires > 0)
            {
                --Mod.skipNextFires;
                FirePatch.positions = null;
                FirePatch.directions = null;
                return;
            }

            // Skip if not connected or no one to send data to
            if (!FirePatch.fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.LeverActionFirearmFire(0, trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, hammer1, FirePatch.positions, FirePatch.directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.LeverActionFirearmFire(trackedItem.data.trackedID, FirePatch.roundType, FirePatch.roundClass, hammer1, FirePatch.positions, FirePatch.directions);
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at BurnOffOuterPrefix, setting to 1"); Mod.skipAllInstantiates = 1; }

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
                ramRod = __instance.GetWeapon().RamRod.GetCurBarrel() == __instance;
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

            bool[] applied = new bool[5];
            bool foundFirstPos = false;
            bool skippedSecondDir = false;
            bool foundFirstDir = false;
            bool foundNum2 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (!foundNum2 && instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Contains("4"))
                {
                    instructionList.InsertRange(i + 1, toInsert4);
                    applied[0] = true;
                    foundNum2 = true;
                    continue;
                }

                if (!foundFirstPos && (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied[1] = true;
                    foundFirstPos = true;
                    continue;
                }
                if (foundFirstPos && (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    applied[2] = true;
                    continue;
                }

                if (!foundFirstDir && (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 1, toInsert1);
                    applied[3] = true;
                    foundFirstDir = true;
                    continue;
                }
                if (foundFirstDir && !skippedSecondDir && (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_forward"))
                {
                    skippedSecondDir = true;
                    continue;
                }
                if (foundFirstDir && skippedSecondDir && (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 1, toInsert3);
                    applied[4] = true;
                    break;
                }
            }

            for(int i=0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("FireFlintlockWeaponPatch BurnOffOuterTranspiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
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
            if (!fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance.GetWeapon()) ? GameManager.trackedItemByItem[__instance.GetWeapon()] : __instance.GetWeapon().GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.FlintlockWeaponBurnOffOuter(0, trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, powderAmount, ramRod, num2, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.FlintlockWeaponBurnOffOuter(trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, powderAmount, ramRod, num2, positions, directions);
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at flint lock FirePrefix, setting to 1"); Mod.skipAllInstantiates = 1; }

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
                ramRod = __instance.GetWeapon().RamRod.GetCurBarrel() == __instance;
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

            bool[] applied = new bool[7];
            bool foundFirstPos = false;
            bool foundFirstDir = false;
            bool foundNum5 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (!foundNum5 && instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Contains("System.Single (9)"))
                {
                    instructionList.InsertRange(i + 1, toInsert4);
                    applied[0] = true;
                    foundNum5 = true;
                    continue;
                }

                if (!foundFirstPos && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("Muzzle") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 2, toInsert0);
                    applied[1] = true;
                    foundFirstPos = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.Vector3 (13)"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    applied[2] = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.Vector3 (21)"))
                {
                    instructionList.InsertRange(i + 1, toInsert5);
                    applied[3] = true;
                    continue;
                }


                if (!foundFirstDir && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("Muzzle") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 2, toInsert1);
                    applied[4] = true;
                    foundFirstDir = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (15)") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_transform") &&
                    (instructionList[i + 2].opcode == OpCodes.Callvirt || instructionList[i + 2].opcode == OpCodes.Call) && instructionList[i + 2].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 3, toInsert3);
                    applied[5] = true;
                    continue;
                }
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (22)") &&
                    (instructionList[i + 1].opcode == OpCodes.Callvirt || instructionList[i + 1].opcode == OpCodes.Call) && instructionList[i + 1].operand.ToString().Contains("get_transform") &&
                    (instructionList[i + 2].opcode == OpCodes.Callvirt || instructionList[i + 2].opcode == OpCodes.Call) && instructionList[i + 2].operand.ToString().Contains("get_forward"))
                {
                    instructionList.InsertRange(i + 3, toInsert6);
                    applied[6] = true;
                    break;
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("FireFlintlockWeaponPatch FireTranspiler not applied!");
                    break;
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
            if (!fireSuccessful || Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                positions = null;
                directions = null;
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance.GetWeapon()) ? GameManager.trackedItemByItem[__instance.GetWeapon()] : __instance.GetWeapon().GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.FlintlockWeaponFire(0, trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, loadedElementPowderAmounts, ramRod, num5, positions, directions);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.FlintlockWeaponFire(trackedItem.data.trackedID, loadedElementTypes, loadedElementPositions, loadedElementPowderAmounts, ramRod, num5, positions, directions);
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at FireHCBPatch, setting to 1"); Mod.skipAllInstantiates = 1; }

            if (Mod.managerObject == null)
            {
                return true;
            }

            ___m_sledState = HCB.SledState.Forward;
            __instance.Sled.localPosition = __instance.SledPos_Forward.localPosition;
            __instance.UpdateStrings();
            if (__instance.Chamber.IsFull)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.BoltPrefab, GetPos(__instance.MuzzlePos.position), __instance.MuzzlePos.rotation);
                HCBBolt component = gameObject.GetComponent<HCBBolt>();
                component.Fire(GetDir(__instance.MuzzlePos.forward), GetPos(__instance.MuzzlePos.position), 1f);
                component.SetCookedAmount(___m_cookedAmount);
                __instance.Chamber.SetRound(null, false);
            }
            __instance.PlayAudioAsHandling(__instance.AudEvent_Fire, __instance.Sled.transform.position);

            if (releaseSledSkip == 0)
            {
                // Get tracked item
                TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
                if (trackedItem != null)
                {
                    // Send the fire action to other clients only if we control it
                    if (ThreadManager.host)
                    {
                        if (trackedItem.data.controller == 0)
                        {
                            ServerSend.HCBReleaseSled(0, trackedItem.data.trackedID, __instance.Chamber.IsFull ? ___m_cookedAmount : -1, position, direction);
                        }
                    }
                    else if (trackedItem.data.controller == Client.singleton.ID)
                    {
                        ClientSend.HCBReleaseSled(trackedItem.data.trackedID, __instance.Chamber.IsFull ? ___m_cookedAmount : -1, position, direction);
                    }
                }
            }

            ___m_cookedAmount = 0;

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

    // Patches SimpleLauncher and SimpleLauncherFireOnDamage to prevent firing for non controller
    class SimpleLauncherPatch
    {
        static bool DamagePrefix(SimpleLauncher __instance)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            return trackedItem == null || trackedItem.data.controller == GameManager.ID;
        }

        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.operand.ToString().Contains("Fire"))
                {
                    instructionList.RemoveAt(i - 1);
                    instructionList.RemoveAt(i - 1);
                    instructionList.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_0));
                    instructionList.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SimpleLauncherPatch), "ConditionalFire")));
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SimpleLauncherPatch CollisionTranspiler not applied!");
            }

            return instructionList;
        }

        public static void ConditionalFire(SimpleLauncher simpleLauncher)
        {
            if(Mod.managerObject == null)
            {
                simpleLauncher.Fire();
            }
            else
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(simpleLauncher, out trackedItem) ? trackedItem : simpleLauncher.GetComponent<TrackedItem>();
                if(trackedItem==null || trackedItem.data.controller == GameManager.ID)
                {
                    simpleLauncher.Fire();
                }
            }
        }
    }

    // Patches StingerLauncher.Fire so we can keep track of fire event
    class FireStingerLauncherPatch
    {
        public static bool overriden;
        public static Vector3 targetPos;
        public static Vector3 position;
        public static Quaternion rotation;
        static TrackedItem trackedItem;
        public static int skip;

        static void Prefix(ref StingerLauncher __instance, AIEntity ___m_targetEntity)
        {
            // Make sure we skip projectile instantiation
            // Do this before skip checks because we want to skip instantiate patch for projectiles regardless
            ++Mod.skipAllInstantiates;
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at FireStingerLauncherPatch, setting to 1"); Mod.skipAllInstantiates = 1; }

            trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out TrackedItem item) ? item : __instance.GetComponent<TrackedItem>();

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
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireStingerLauncherPatch), "GetRotation"))); // Call our GetRotation method

            // To set missle ref in trackedItem
            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load StingerMissile
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FireStingerLauncherPatch), "SetStingerMissile"))); // Call our SetStingerMissile method

            bool[] applied = new bool[3];
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_position"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied[0] = true;
                    continue;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_rotation"))
                {
                    instructionList.InsertRange(i + 1, toInsert1);
                    applied[1] = true;
                    continue;
                }

                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Fire"))
                {
                    instructionList.InsertRange(i + 1, toInsert2);
                    applied[2] = true;
                    break;
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("FireStingerLauncherPatch Transpiler not applied!");
                    break;
                }
            }

            return instructionList;
        }

        public static void SetStingerMissile(StingerMissile missile)
        {
            Mod.LogInfo("Setting stinger missile");
            if (trackedItem != null)
            {
                Mod.LogInfo("\tGot trackedItem");
                trackedItem.stingerMissile = missile;
                TrackedObjectReference reference = missile.gameObject.AddComponent<TrackedObjectReference>();
                reference.trackedRef = trackedItem;
            }
            else
            {
                Mod.LogInfo("\tNO trackedItem");
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

        public static Quaternion GetRotation(Quaternion rotation)
        {
            if (overriden)
            {
                return FireStingerLauncherPatch.rotation;
            }
            else
            {
                FireStingerLauncherPatch.rotation = rotation;
                return rotation;
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return;
            }

            // Get tracked item
            if (trackedItem != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.StingerLauncherFire(0, trackedItem.data.trackedID, targetPos, position, rotation);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.StingerLauncherFire(trackedItem.data.trackedID, targetPos, position, rotation);
                }
            }
        }

        static bool MissileFirePrefix(StingerMissile __instance)
        {
            if (Mod.managerObject != null && overriden)
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

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(___m_launcher, out trackedItem) ? trackedItem : ___m_launcher.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == GameManager.ID)
                {
                    // Send to other clients
                    if (ThreadManager.host)
                    {
                        ServerSend.RemoteMissileDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        ClientSend.RemoteMissileDetonate(trackedItem.data.trackedID, __instance.transform.position);
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

            TrackedObject trackedObject = __instance.GetComponent<Scripts.TrackedObjectReference>().trackedRef;
            if (trackedObject != null)
            {
                if (trackedObject.data.controller == GameManager.ID)
                {
                    // Send to other clients
                    if (ThreadManager.host)
                    {
                        ServerSend.StingerMissileExplode(0, trackedObject.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        ClientSend.StingerMissileExplode(trackedObject.data.trackedID, __instance.transform.position);
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return;
            }

            // Get tracked item
            TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Send the shatter action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        ServerSend.SosigWeaponShatter(0, trackedItem.data.trackedID);
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    ClientSend.SosigWeaponShatter(trackedItem.data.trackedID);
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

            TrackedSosig trackedSosig = __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                trackedSosig.sosigData.configTemplate = t;

                if (ThreadManager.host)
                {
                    ServerSend.SosigConfigure(trackedSosig.data.trackedID, t);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigConfigure(trackedSosig.data.trackedID, t);
                    }
                    else
                    {
                        if (TrackedSosig.unknownConfiguration.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownConfiguration[trackedSosig.data.localWaitingIndex] = t;
                        }
                        else
                        {
                            TrackedSosig.unknownConfiguration.Add(trackedSosig.data.localWaitingIndex, t);
                        }
                    }
                }
            }
        }
    }

    // Patches Sosig update methods to prevent processing on non controlling client
    class SosigUpdatePatch
    {
        static bool UpdatePrefix(Sosig __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            if(__instance.BuffSystems.Length >= 15 && __instance.BuffSystems[14] != null && int.TryParse(__instance.BuffSystems[14].name, out int parsed))
            {
                TrackedSosig trackedSosig = (TrackedSosig)TrackedObject.trackedReferences[parsed];
                if (trackedSosig != null)
                {
                    bool runOriginal = trackedSosig.data.controller == GameManager.ID;
                    if (!runOriginal)
                    {
                        // Call Sosig update methods we don't want to skip
                        if(__instance.Links[__instance.m_linkIndex] == null)
                        {
                            for(int i=0; i < __instance.Links.Count; ++i)
                            {
                                if(__instance.Links[i] != null)
                                {
                                    __instance.m_linkIndex = i;
                                    __instance.fakeEntityPos = __instance.Links[__instance.m_linkIndex].transform.position + UnityEngine.Random.onUnitSphere * 0.2f + __instance.Links[__instance.m_linkIndex].transform.up * 0.25f;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            __instance.fakeEntityPos = __instance.Links[__instance.m_linkIndex].transform.position + UnityEngine.Random.onUnitSphere * 0.2f + __instance.Links[__instance.m_linkIndex].transform.up * 0.25f;
                        }
                        __instance.E.FakePos = __instance.fakeEntityPos;
                        __instance.VaporizeUpdate();
                        __instance.HeadIconUpdate();
                        if (__instance.m_recoveringFromBallisticState)
                        {
                            __instance.UpdateJoints(__instance.m_recoveryFromBallisticLerp);
                        }
                    }
                    return runOriginal;
                }
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

            if (__instance.BuffSystems.Length >= 15 && __instance.BuffSystems[14] != null && int.TryParse(__instance.BuffSystems[14].name, out int parsed))
            {
                TrackedSosig trackedSosig = (TrackedSosig)TrackedObject.trackedReferences[parsed];
                if (trackedSosig != null)
                {
                    return trackedSosig.data.controller == GameManager.ID;
                }
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

            if (__instance.S.BuffSystems.Length >= 15 && __instance.S.BuffSystems[14] != null && int.TryParse(__instance.S.BuffSystems[14].name, out int parsed))
            {
                TrackedSosig trackedSosig = (TrackedSosig)TrackedObject.trackedReferences[parsed];
                if (trackedSosig != null)
                {
                    return trackedSosig.data.controller == GameManager.ID;
                }
            }
            return true;
        }
    }

    // Patches Sosig to keep track of all actions taken on a sosig
    class SosigPatch
    {
        public static int sosigDiesSkip;
        public static int sosigClearSkip;
        public static int sosigSetBodyStateSkip;
        public static int sosigVaporizeSkip;
        public static int sosigSetCurrentOrderSkip;
        public static int sosigRequestHitDecalSkip;
        public static int skipSendingOrder;

        static void SosigDiesPrefix(ref Sosig __instance, Damage.DamageClass damClass, Sosig.SosigDeathType deathType)
        {
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;

            if (sosigDiesSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigDies(trackedSosig.data.trackedID, damClass, deathType);
                }
                else
                {
                    ClientSend.SosigDies(trackedSosig.data.trackedID, damClass, deathType);
                }
            }
        }

        static void SosigDiesPostfix()
        {
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
        }

        static void SosigClearPrefix(ref Sosig __instance)
        {
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                trackedSosig.sendDestroy = false;
                if (ThreadManager.host)
                {
                    ServerSend.SosigClear(trackedSosig.data.trackedID);
                }
                else
                {
                    ClientSend.SosigClear(trackedSosig.data.trackedID);
                }
            }
        }

        static void SosigClearPostfix()
        {
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (trackedSosig.data.trackedID == -1)
                {
                    if (TrackedSosig.unknownBodyStates.ContainsKey(trackedSosig.data.localWaitingIndex))
                    {
                        TrackedSosig.unknownBodyStates[trackedSosig.data.localWaitingIndex] = s;
                    }
                    else
                    {
                        TrackedSosig.unknownBodyStates.Add(trackedSosig.data.localWaitingIndex, s);
                    }
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigSetBodyState(trackedSosig.data.trackedID, s);
                    }
                    else
                    {
                        ClientSend.SosigSetBodyState(trackedSosig.data.trackedID, s);
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
            toInsertSecond.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SosigPatch), "SendFootStepSound"))); // Call our own method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("PlayCoreSoundDelayedOverrides"))
                {
                    instructionList.InsertRange(i + 1, toInsertSecond);
                    applied = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("SosigActionPatch FootStepTranspiler not applied!");
            }

            return instructionList;
        }

        public static void SendFootStepSound(Sosig sosig, FVRPooledAudioType audioType, Vector3 position, Vector2 vol, Vector2 pitch, float delay)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (sosig.BuffSystems.Length >= 15 && sosig.BuffSystems[14] != null && int.TryParse(sosig.BuffSystems[14].name, out int parsed))
            {
                TrackedSosig trackedSosig = (TrackedSosig)TrackedObject.trackedReferences[parsed];
                if (trackedSosig != null)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.PlaySosigFootStepSound(trackedSosig.data.trackedID, audioType, position, vol, pitch, delay);
                    }
                    else
                    {
                        ClientSend.PlaySosigFootStepSound(trackedSosig.data.trackedID, audioType, position, vol, pitch, delay);
                    }
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
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load Sosig instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Sosig), "m_speakingSource"))); // Load m_speakingSource
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SosigPatch), "SendSpeakState"))); // Call our own method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.operand.ToString().Contains("Speak_State"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("SosigActionPatch SpeechUpdateTranspiler not applied!");
            }

            return instructionList;
        }

        public static void SendSpeakState(Sosig sosig, Sosig.SosigOrder currentOrder, FVRPooledAudioSource m_speakingSource)
        {
            if (Mod.managerObject == null || m_speakingSource == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(sosig) ? GameManager.trackedSosigBySosig[sosig] : sosig.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigSpeakState(trackedSosig.data.trackedID, currentOrder);
                }
                else
                {
                    ClientSend.SosigSpeakState(trackedSosig.data.trackedID, currentOrder);
                }
            }
        }

        static void SetCurrentOrderPrefix(ref Sosig __instance, Sosig.SosigOrder o)
        {
            if (Mod.managerObject == null || sosigSetCurrentOrderSkip > 0 || __instance == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                trackedSosig.sosigData.currentOrder = o;

                if (skipSendingOrder == 0)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigSetCurrentOrder(trackedSosig.sosigData, o);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            ClientSend.SosigSetCurrentOrder(trackedSosig.sosigData, o);
                        }
                        else
                        {
                            if (TrackedSosig.unknownCurrentOrder.ContainsKey(trackedSosig.data.localWaitingIndex))
                            {
                                TrackedSosig.unknownCurrentOrder[trackedSosig.data.localWaitingIndex] = o;
                            }
                            else
                            {
                                TrackedSosig.unknownCurrentOrder.Add(trackedSosig.data.localWaitingIndex, o);
                            }
                        }
                    }
                }
            }
        }

        static void CommandGuardPointPrefix()
        {
            ++skipSendingOrder;
        }

        static void CommandGuardPointPostfix(Sosig __instance, Vector3 point, bool hardguard)
        {
            --skipSendingOrder;
            if (sosigSetCurrentOrderSkip > 0)
            {
                return;
            }

            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                trackedSosig.sosigData.guardPoint = point;
                trackedSosig.sosigData.hardGuard = hardguard;

                if (ThreadManager.host)
                {
                    ServerSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.GuardPoint);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.GuardPoint);
                    }
                    else
                    {
                        if (TrackedSosig.unknownCurrentOrder.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownCurrentOrder[trackedSosig.data.localWaitingIndex] = Sosig.SosigOrder.GuardPoint;
                        }
                        else
                        {
                            TrackedSosig.unknownCurrentOrder.Add(trackedSosig.data.localWaitingIndex, Sosig.SosigOrder.GuardPoint);
                        }
                    }
                }
            }
        }

        static void CommandAssaultPointPrefix()
        {
            ++skipSendingOrder;
        }

        static void CommandAssaultPointPostfix(Sosig __instance, Vector3 point)
        {
            --skipSendingOrder;
            if (sosigSetCurrentOrderSkip > 0)
            {
                return;
            }

            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                trackedSosig.sosigData.assaultPoint = point;

                if (ThreadManager.host)
                {
                    ServerSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.Assault);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.Assault);
                    }
                    else
                    {
                        if (TrackedSosig.unknownCurrentOrder.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownCurrentOrder[trackedSosig.data.localWaitingIndex] = Sosig.SosigOrder.Assault;
                        }
                        else
                        {
                            TrackedSosig.unknownCurrentOrder.Add(trackedSosig.data.localWaitingIndex, Sosig.SosigOrder.Assault);
                        }
                    }
                }
            }
        }

        static void CommandIdlePrefix()
        {
            ++skipSendingOrder;
        }

        static void CommandIdlePostfix(Sosig __instance, Vector3 point, Vector3 dominantDir)
        {
            --skipSendingOrder;
            if (sosigSetCurrentOrderSkip > 0)
            {
                return;
            }

            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                trackedSosig.sosigData.idleToPoint = point;
                trackedSosig.sosigData.idleDominantDir = dominantDir;

                if (ThreadManager.host)
                {
                    ServerSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.Idle);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.Idle);
                    }
                    else
                    {
                        if (TrackedSosig.unknownCurrentOrder.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownCurrentOrder[trackedSosig.data.localWaitingIndex] = Sosig.SosigOrder.Idle;
                        }
                        else
                        {
                            TrackedSosig.unknownCurrentOrder.Add(trackedSosig.data.localWaitingIndex, Sosig.SosigOrder.Idle);
                        }
                    }
                }
            }
        }

        static void CommandPathToPostfix(Sosig __instance)
        {
            if (sosigSetCurrentOrderSkip > 0)
            {
                return;
            }

            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                trackedSosig.sosigData.pathToPoint = trackedSosig.physicalSosig.m_pathToPoint;

                if (ThreadManager.host)
                {
                    ServerSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.PathTo);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigSetCurrentOrder(trackedSosig.sosigData, Sosig.SosigOrder.PathTo);
                    }
                    else
                    {
                        if (TrackedSosig.unknownCurrentOrder.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownCurrentOrder[trackedSosig.data.localWaitingIndex] = Sosig.SosigOrder.PathTo;
                        }
                        else
                        {
                            TrackedSosig.unknownCurrentOrder.Add(trackedSosig.data.localWaitingIndex, Sosig.SosigOrder.PathTo);
                        }
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigVaporize(trackedSosig.data.trackedID, iff);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigVaporize(trackedSosig.data.trackedID, iff);
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
                    TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
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
                    TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
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
            if (ThreadManager.host)
            {
                ServerSend.SosigRequestHitDecal(sosigTrackedID, point, normal, edgeNormal, scale, linkIndex);
            }
            else
            {
                ClientSend.SosigRequestHitDecal(sosigTrackedID, point, normal, edgeNormal, scale, linkIndex);
            }
        }

        static bool ProcessCollisionPrefix(Sosig __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            return trackedSosig == null || trackedSosig.data.controller == GameManager.ID;
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
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
                            return;
                        }
                    }
                    if (trackedSosig.data.trackedID == -1)
                    {
                        if(TrackedSosig.unknownWearable.TryGetValue(trackedSosig.data.localWaitingIndex, out Dictionary<string, List<int>> dict))
                        {
                            if(dict.TryGetValue(knownWearableID, out List<int> entryLinkIndex))
                            {
                                entryLinkIndex.Add(linkIndex);
                            }
                            else
                            {
                                dict.Add(knownWearableID, new List<int>() { linkIndex });
                            }
                        }
                        else
                        {
                            Dictionary<string, List<int>> newDict = new Dictionary<string, List<int>>();
                            newDict.Add(knownWearableID, new List<int>() { linkIndex });
                            TrackedSosig.unknownWearable.Add(trackedSosig.data.localWaitingIndex, newDict);
                        }
                    }
                    else
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.SosigLinkRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                        }
                        else
                        {
                            ClientSend.SosigLinkRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                        }
                    }

                    trackedSosig.sosigData.wearables[linkIndex].Add(knownWearableID);

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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigLinkDeRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            ClientSend.SosigLinkDeRegisterWearable(trackedSosig.data.trackedID, linkIndex, knownWearableID);
                        }
                    }

                    trackedSosig.sosigData.wearables[linkIndex].Remove(knownWearableID);

                    knownWearableID = null;
                }
            }
        }

        static void LinkExplodesPrefix(ref SosigLink __instance, Damage.DamageClass damClass)
        {
            ++SosigPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;

            if (skipLinkExplodes > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigLinkExplodes(trackedSosig.data.trackedID, linkIndex, damClass);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            ClientSend.SosigLinkExplodes(trackedSosig.data.trackedID, linkIndex, damClass);
                        }
                    }
                }
            }
        }

        static void LinkExplodesPostfix()
        {
            --SosigPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
        }

        static bool LinkBreakPrefix(ref SosigLink __instance, bool isStart, Damage.DamageClass damClass)
        {
            ++SosigPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;

            if (sosigLinkBreakSkip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (trackedSosig.data.controller != GameManager.ID)
                {
                    return false;
                }

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
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigLinkBreak(trackedSosig.data.trackedID, linkIndex, isStart, (byte)damClass);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            ClientSend.SosigLinkBreak(trackedSosig.data.trackedID, linkIndex, isStart, damClass);
                        }
                    }
                }
            }

            return true;
        }

        static void LinkBreakPostfix()
        {
            --SosigPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
        }

        static void LinkSeverPrefix(ref SosigLink __instance, Damage.DamageClass damClass, bool isPullApart)
        {
            ++SosigPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;

            if (sosigLinkSeverSkip > 0)
            {
                return;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
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
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigLinkSever(trackedSosig.data.trackedID, linkIndex, (byte)damClass, isPullApart);
                    }
                    else
                    {
                        if (trackedSosig.data.trackedID != -1)
                        {
                            ClientSend.SosigLinkSever(trackedSosig.data.trackedID, linkIndex, damClass, isPullApart);
                        }
                    }
                }
            }
        }

        static void LinkSeverPostfix()
        {
            --SosigPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
        }

        static void LinkVaporizePrefix(ref SosigLink __instance, int IFF)
        {
            ++SosigPatch.sosigDiesSkip;
            ++SosigHandDropPatch.skip;
            ++SosigSlotDetachPatch.skip;
            ++SosigPatch.sosigSetBodyStateSkip;

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigVaporize(trackedSosig.data.trackedID, IFF);
                }
                else
                {
                    if (trackedSosig.data.trackedID != -1)
                    {
                        ClientSend.SosigVaporize(trackedSosig.data.trackedID, IFF);
                    }
                }
            }
        }

        static void LinkVaporizePostfix()
        {
            --SosigPatch.sosigDiesSkip;
            --SosigHandDropPatch.skip;
            --SosigSlotDetachPatch.skip;
            --SosigPatch.sosigSetBodyStateSkip;
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (trackedSosig.data.trackedID == -1)
                {
                    TrackedSosig.unknownSetIFFs.Add(trackedSosig.data.localWaitingIndex, i);
                }
                else
                {
                    trackedSosig.sosigData.IFF = (byte)i;
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigSetIFF(trackedSosig.data.trackedID, i);
                    }
                    else
                    {
                        ClientSend.SosigSetIFF(trackedSosig.data.trackedID, i);
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (trackedSosig.data.trackedID == -1)
                {
                    TrackedSosig.unknownSetOriginalIFFs.Add(trackedSosig.data.localWaitingIndex, i);
                }
                else
                {
                    trackedSosig.sosigData.IFF = (byte)i;
                    if (ThreadManager.host)
                    {
                        ServerSend.SosigSetOriginalIFF(trackedSosig.data.trackedID, i);
                    }
                    else
                    {
                        ClientSend.SosigSetOriginalIFF(trackedSosig.data.trackedID, i);
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

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance) ? GameManager.trackedSosigBySosig[__instance] : __instance.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                return trackedSosig.data.controller == GameManager.ID;
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

            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                bool runOriginal = trackedAutoMeater.data.controller == GameManager.ID;
                if (!runOriginal)
                {
                    // Call AutoMeater update methods we don't want to skip
                    if (trackedAutoMeater.physicalAutoMeater.FireControl.Firearms[0].IsFlameThrower)
                    {
                        trackedAutoMeater.physicalAutoMeater.FireControl.Firearms[0].Tick(Time.deltaTime);
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

            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                return trackedAutoMeater.data.controller == GameManager.ID;
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

            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.AutoMeaterSetState(trackedAutoMeater.data.trackedID, (byte)s);
                }
                else
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        ClientSend.AutoMeaterSetState(trackedAutoMeater.data.trackedID, (byte)s);
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

            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                return trackedAutoMeater.data.controller == GameManager.ID;
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

            bool applied = false;
            bool active = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("UsesBlades"))
                {
                    instructionList.InsertRange(i + 2, active ? toInsertActive : toInsertInactive);
                    applied = true;
                    active = !active;
                }
            }

            if (!applied)
            {
                Mod.LogError("AutoMeaterUpdateFlightPatch Transpiler not applied!");
            }

            return instructionList;
        }

        public static void SetAutoMeaterBladesActive(AutoMeater autoMeater, bool active)
        {
            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(autoMeater) ? GameManager.trackedAutoMeaterByAutoMeater[autoMeater] : autoMeater.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.AutoMeaterSetBladesActive(trackedAutoMeater.data.trackedID, active);
                }
                else
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        ClientSend.AutoMeaterSetBladesActive(trackedAutoMeater.data.trackedID, active);
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
            if (Mod.skipAllInstantiates <= 0) { Mod.LogError("SkipAllInstantiates negative or 0 at AutoMeaterFirearmFireShotPatch, setting to 1"); Mod.skipAllInstantiates = 1; }
        }

        static void Postfix(ref AutoMeater.AutoMeaterFirearm __instance)
        {
            if (skip > 0)
            {
                return;
            }

            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return;
            }

            // Get tracked item
            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance.M) ? GameManager.trackedAutoMeaterByAutoMeater[__instance.M] : __instance.M.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                // Send the fire action to other clients only if we control it
                if (ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        ServerSend.AutoMeaterFirearmFireShot(0, trackedAutoMeater.data.trackedID, __instance.Muzzle.localEulerAngles);
                    }
                }
                else if (trackedAutoMeater.data.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        ClientSend.AutoMeaterFirearmFireShot(trackedAutoMeater.data.trackedID, __instance.Muzzle.localEulerAngles);
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("set_localEulerAngles"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("AutoMeaterFirearmFireShotPatch Transpiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0 || __instance.m_fireAtWill == b)
            {
                return;
            }

            // Get tracked item
            TrackedAutoMeater trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance.M) ? GameManager.trackedAutoMeaterByAutoMeater[__instance.M] : __instance.M.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (trackedAutoMeater.data.controller == GameManager.ID)
                {
                    int firearmIndex = -1;
                    for (int i = 0; i < trackedAutoMeater.physicalAutoMeater.FireControl.Firearms.Count; ++i)
                    {
                        if (trackedAutoMeater.physicalAutoMeater.FireControl.Firearms[i] == __instance)
                        {
                            firearmIndex = i;
                            break;
                        }
                    }

                    if (ThreadManager.host)
                    {
                        ServerSend.AutoMeaterFirearmFireAtWill(trackedAutoMeater.data.trackedID, firearmIndex, b, d);
                    }
                    else
                    {
                        ClientSend.AutoMeaterFirearmFireAtWill(trackedAutoMeater.data.trackedID, firearmIndex, b, d);
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction != null && instruction.operand != null)
                {
                    if (instruction.operand.ToString().Contains("get_activeSelf"))
                    {
                        instructionList.InsertRange(i + 2, toInsert);
                        applied = true;
                        break;
                    }
                }
            }

            if (!applied)
            {
                Mod.LogError("EncryptionRespawnRandSubPatch Transpiler not applied!");
            }

            return instructionList;
        }

        public static void RespawnSubTarg(TNH_EncryptionTarget encryption, int index)
        {
            if (Mod.managerObject != null)
            {
                TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(encryption) ? GameManager.trackedEncryptionByEncryption[encryption] : encryption.GetComponent<TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.data.controller == GameManager.ID)
                {
                    trackedEncryption.encryptionData.subTargsActive[index] = true;

                    if (ThreadManager.host)
                    {
                        ServerSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                    }
                    else
                    {
                        if (trackedEncryption.data.trackedID == -1)
                        {
                            if (TrackedEncryption.unknownSpawnSubTarg.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<int> l))
                            {
                                l.Add(index);
                            }
                            else
                            {
                                TrackedEncryption.unknownSpawnSubTarg.Add(trackedEncryption.data.localWaitingIndex, new List<int>() { index });
                            }
                        }
                        else
                        {
                            ClientSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                        }
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.SpawnGrowth to sync with other clients
    class EncryptionSpawnGrowthPatch
    {
        public static int skip;

        static void Prefix(ref TNH_EncryptionTarget __instance, int index, Vector3 point)
        {
            Mod.LogInfo("EncryptionSpawnGrowthPatch prefix");
            if (skip > 0)
            {
                return;
            }

            if (Mod.managerObject != null)
            {
                TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<TrackedEncryption>();
                if (trackedEncryption != null && trackedEncryption.data.controller == GameManager.ID)
                {
                    trackedEncryption.encryptionData.subTargsActive[index] = true;
                    Vector3 forward = point - trackedEncryption.physicalEncryption.Tendrils[index].transform.position;

                    if (ThreadManager.host)
                    {
                        Mod.LogInfo("\rServer sending");
                        ServerSend.EncryptionSpawnGrowth(trackedEncryption.data.trackedID, index, point);
                    }
                    else
                    {
                        if (trackedEncryption.data.trackedID == -1)
                        {
                            if (TrackedEncryption.unknownSpawnGrowth.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<KeyValuePair<int, Vector3>> l))
                            {
                                l.Add(new KeyValuePair<int, Vector3>(index, point));
                            }
                            else
                            {
                                TrackedEncryption.unknownSpawnGrowth.Add(trackedEncryption.data.localWaitingIndex, new List<KeyValuePair<int, Vector3>>() { new KeyValuePair<int, Vector3>(index, point) });
                            }
                        }
                        else
                        {
                            ClientSend.EncryptionSpawnGrowth(trackedEncryption.data.trackedID, index, point);
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

            if (Mod.managerObject != null)
            {
                TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller == GameManager.ID)
                    {
                        Vector3 forward = point - __instance.Tendrils[index].transform.position;

                        if (ThreadManager.host)
                        {
                            ServerSend.EncryptionResetGrowth(trackedEncryption.data.trackedID, index, point);
                        }
                        else
                        {
                            if (trackedEncryption.data.trackedID == -1)
                            {
                                if (TrackedEncryption.unknownResetGrowth.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<KeyValuePair<int, Vector3>> l))
                                {
                                    l.Add(new KeyValuePair<int, Vector3>(index, point));
                                }
                                else
                                {
                                    TrackedEncryption.unknownResetGrowth.Add(trackedEncryption.data.localWaitingIndex, new List<KeyValuePair<int, Vector3>>() { new KeyValuePair<int, Vector3>(index, point) });
                                }
                            }
                            else
                            {
                                ClientSend.EncryptionResetGrowth(trackedEncryption.data.trackedID, index, point);
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
        static bool geoWasActive;
        static TrackedEncryption trackedEncryption = null;

        static void Prefix(ref TNH_EncryptionTarget __instance, int i)
        {
            if (Mod.managerObject != null)
            {
                trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    wasActive = __instance.SubTargs[i].activeSelf;
                    if (__instance.UsesRegeneratingSubtargs)
                    {
                        geoWasActive = __instance.SubTargGeo[i].gameObject.activeSelf;
                    }
                }
            }
        }

        static void Postfix(ref TNH_EncryptionTarget __instance, int i)
        {
            // Instance could be null if destroyed by the method, in which case we don't need to send anything, the destruction will be sent instead
            if (Mod.managerObject != null && __instance != null)
            {
                if (trackedEncryption != null)
                {
                    bool switched = false;
                    if(wasActive && !__instance.SubTargs[i].activeSelf)
                    {
                        trackedEncryption.encryptionData.subTargsActive[i] = false;
                        switched = true;
                    }
                    if (__instance.UsesRegeneratingSubtargs && geoWasActive && !__instance.SubTargGeo[i].gameObject.activeSelf)
                    {
                        trackedEncryption.encryptionData.subTargGeosActive[i] = false;
                        switched = true;
                    }

                    if (switched)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.EncryptionDisableSubtarg(trackedEncryption.data.trackedID, i);
                        }
                        else
                        {
                            if (trackedEncryption.data.trackedID == -1)
                            {
                                if (TrackedEncryption.unknownDisableSubTarg.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<int> l))
                                {
                                    l.Add(i);
                                }
                                else
                                {
                                    TrackedEncryption.unknownDisableSubTarg.Add(trackedEncryption.data.localWaitingIndex, new List<int>() { i });
                                }
                            }
                            else
                            {
                                ClientSend.EncryptionDisableSubtarg(trackedEncryption.data.trackedID, i);
                            }
                        }
                    }
                }
            }
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
                TrackedItem trackedGun = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
                TrackedItem trackedBattery = GameManager.trackedItemByItem.ContainsKey(battery) ? GameManager.trackedItemByItem[battery] : battery.GetComponent<TrackedItem>();
                if (trackedGun != null && trackedBattery != null)
                {
                    if (trackedGun.data.controller != GameManager.ID)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.LAPD2019LoadBattery(0, trackedGun.data.trackedID, trackedGun.data.trackedID);
                        }
                        else
                        {
                            ClientSend.LAPD2019LoadBattery(trackedGun.data.trackedID, trackedGun.data.trackedID);
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
                TrackedItem trackedGun = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
                if (trackedGun != null)
                {
                    if (trackedGun.data.controller != GameManager.ID)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.LAPD2019ExtractBattery(0, trackedGun.data.trackedID);
                        }
                        else
                        {
                            ClientSend.LAPD2019ExtractBattery(trackedGun.data.trackedID);
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

            TrackedSosig trackedSosig = ___E.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigPriorityIFFChart(0, trackedSosig.data.trackedID, BoolArrToInt(__instance.IFFChart));
                }
                else if (trackedSosig.data.trackedID != -1)
                {
                    ClientSend.SosigPriorityIFFChart(trackedSosig.data.trackedID, BoolArrToInt(__instance.IFFChart));
                }
                else // Unknown tracked ID, keep for late update
                {
                    if (TrackedSosig.unknownIFFChart.ContainsKey(trackedSosig.data.localWaitingIndex))
                    {
                        TrackedSosig.unknownIFFChart[trackedSosig.data.localWaitingIndex] = BoolArrToInt(__instance.IFFChart);
                    }
                    else
                    {
                        TrackedSosig.unknownIFFChart.Add(trackedSosig.data.localWaitingIndex, BoolArrToInt(__instance.IFFChart));
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
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
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
        // This GameObject has its name set to an index
        // This index is the index of the tracked item in the trackedItemReferences static array if TrackedItem
        // So to know if our PinnedGrenade is under our control, we get the last gameObject in the SpawnOnSplode list
        // We get its name, which we then use to get our TrackedItem
        // We can then check trackedItem.Controller
        // Note: We also now need to prevent the PinnedGrenade from actually spawning the last item in SpawnOnSplode

        static bool exploded;

        // To prevent FVR(Fixed)Update from happening
        static bool UpdatePrefix(PinnedGrenade __instance, bool ___m_hasSploded, List<PinnedGrenadeRing> ___m_rings, ref bool ___m_isPinPulled)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            exploded = ___m_hasSploded;

            if (__instance.SpawnOnSplode != null && __instance.SpawnOnSplode.Count > 0 && __instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1] != null &&
                int.TryParse(__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].name, out int index))
            {
                // Return true (run original), index doesn't fit in references, reference null, or we control
                if (TrackedObject.trackedReferences.Length <= index ||
                    TrackedObject.trackedReferences[index] == null ||
                    TrackedObject.trackedReferences[index].data.controller == GameManager.ID)
                {
                    return true;
                }
                else // This is tracked pinned grenade we are not in control of
                {
                    // Do part of update we still want as non controller
                    bool prePulled = ___m_isPinPulled;
                    if (___m_rings.Count > 0)
                    {
                        ___m_isPinPulled = true;
                        for (int i = 0; i < ___m_rings.Count; i++)
                        {
                            if (!___m_rings[i].HasPinDetached())
                            {
                                ___m_isPinPulled = false;
                            }
                        }
                    }

                    // Even if not controller, if we pulled the pin on this grenade we want to tell controller to do the same
                    // We also check if ___m_isPinPulled because for some reason it can end up with prePulled true but ___m_isPinPulled false on first update
                    if (prePulled != ___m_isPinPulled && ___m_isPinPulled)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.PinnedGrenadePullPin(TrackedObject.trackedReferences[index].data.trackedID);
                        }
                        else
                        {
                            ClientSend.PinnedGrenadePullPin(TrackedObject.trackedReferences[index].data.trackedID);
                        }
                    }

                    return false;
                }
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

            bool applied0 = false;
            bool applied1 = false;
            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (!found && instruction.opcode == OpCodes.Ldfld && instruction.operand.ToString().Contains("SpawnOnSplode"))
                {
                    breakToLoopHead.operand = instructionList[i - 2].operand;
                    instructionList[i - 1].labels.Add(loopStartLabel);
                    instructionList.InsertRange(i - 1, toInsert0);
                    found = true;
                    applied0 = true;
                }

                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied1 = true;
                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("PinnedGrenadePatch UpdateTranspiler not applied!");
            }

            return instructionList;
        }

        public static bool SkipLast(PinnedGrenade grenade)
        {
            if (Mod.managerObject == null)
            {
                return false;
            }

            return grenade.SpawnOnSplode != null && grenade.SpawnOnSplode.Count > 0 && (grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1] == null || int.TryParse(grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1].name, out int index));
        }

        // To know if grenade exploded in latest update
        static void UpdatePostfix(PinnedGrenade __instance, bool ___m_hasSploded)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!exploded && ___m_hasSploded)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.data.trackedID != -1 && trackedItem.data.controller == GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.PinnedGrenadeExplode(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        ClientSend.PinnedGrenadeExplode(trackedItem.data.trackedID, __instance.transform.position);
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

            bool applied0 = false;
            bool applied1 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("OnCollisionEnter"))
                {
                    instructionList[i + 1].labels.Add(l);
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied0 = true;
                }

                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied1 = true;
                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("PinnedGrenadePatch CollisionTranspiler not applied!");
            }

            return instructionList;
        }

        public static bool GrenadeControlled(PinnedGrenade grenade)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (grenade.SpawnOnSplode != null && grenade.SpawnOnSplode.Count > 0 && grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1] != null &&
                int.TryParse(grenade.SpawnOnSplode[grenade.SpawnOnSplode.Count - 1].name, out int index))
            {
                // Return true (controlled), index fits in references, reference not null, and we control
                return TrackedObject.trackedReferences.Length <= index &&
                       TrackedObject.trackedReferences[index] != null &&
                       TrackedObject.trackedReferences[index].data.controller == GameManager.ID;
            }

            return true;
        }

        public static void ExplodePinnedGrenade(PinnedGrenade grenade, Vector3 pos)
        {
            grenade.m_hasSploded = true;
            for (int i = 0; i < grenade.SpawnOnSplode.Count; i++)
            {
                if (i == grenade.SpawnOnSplode.Count - 1 && int.TryParse(grenade.SpawnOnSplode[i].name, out int index))
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

    // Patches FVRFuse to keep track of events
    class FusePatch
    {
        public static int igniteSkip;

        static void IgnitePostfix(FVRFuse __instance, ref bool ___m_isIgnited)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.Dynamite, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // If not controller we don't want the ignite flag to be set because we don't want it to update
                if (trackedItem.data.controller != GameManager.ID)
                {
                    ___m_isIgnited = false;
                }

                if (igniteSkip == 0)
                {
                    // Send order to others
                    if (ThreadManager.host)
                    {
                        ServerSend.FuseIgnite(trackedItem.data.trackedID);
                    }
                    else
                    {
                        ClientSend.FuseIgnite(trackedItem.data.trackedID);
                    }
                }
            }
        }

        static void BoomPrefix(FVRFuse __instance, ref bool ___m_isIgnited)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.Dynamite, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller == GameManager.ID)
            {
                // Send order to others
                if (ThreadManager.host)
                {
                    ServerSend.FuseBoom(trackedItem.data.trackedID);
                }
                else
                {
                    ClientSend.FuseBoom(trackedItem.data.trackedID);
                }
            }
        }
    }

    // Patches Molotov to keep track of events
    class MolotovPatch
    {
        public static int shatterSkip;
        public static int damageSkip;

        static bool ShatterPrefix(Molotov __instance)
        {
            if (Mod.managerObject == null || shatterSkip > 0)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == GameManager.ID)
                {
                    // Send order to others
                    if (ThreadManager.host)
                    {
                        ServerSend.MolotovShatter(trackedItem.data.trackedID, __instance.Igniteable.IsOnFire());
                    }
                    else
                    {
                        ClientSend.MolotovShatter(trackedItem.data.trackedID, __instance.Igniteable.IsOnFire());
                    }
                }
                else
                {
                    // Don't want to let shatter happen on its own if not controller, like on collision for example
                    return false;
                }
            }

            return true;
        }

        static bool DamagePrefix(Molotov __instance, Damage d)
        {
            if (Mod.managerObject == null || damageSkip > 0)
            {
                return true;
            }

            // If in control of the damaged molotov, we want to process the damage
            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not in control, send damage to controller
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.MolotovDamage(trackedItem.itemData, d);
                    }
                    else
                    {
                        ClientSend.MolotovDamage(trackedItem.data.trackedID, d);
                    }
                    return false;
                }
            }
            return true;
        }
    }

    // Patches TNH_EncryptionTarget to sync
    class EncryptionPatch
    {
        public static int updateDisplaySkip;
        public static bool preActive = false;
        public static int cascadingDestroyIndex;
        public static int cascadingDestroyDepth;
        static TrackedEncryption trackedEncryption;

        // To prevent Update from happening
        static bool UpdatePrefix(TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.SpawnPoints != null && __instance.SpawnPoints.Count > 0 && int.TryParse(__instance.SpawnPoints[__instance.SpawnPoints.Count - 1].name, out int index))
            {
                // Return true (run original), index doesn't fit in references, reference null, or we control
                return TrackedEncryption.trackedEncryptionReferences.Length <= index ||
                       TrackedEncryption.trackedEncryptionReferences[index] == null ||
                       TrackedEncryption.trackedEncryptionReferences[index].data.controller == GameManager.ID;
            }

            return true;
        }

        // To prevent FixedUpdate from happening
        static bool FixedUpdatePrefix(TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.SpawnPoints != null && __instance.SpawnPoints.Count > 0 && int.TryParse(__instance.SpawnPoints[__instance.SpawnPoints.Count - 1].name, out int index))
            {
                // Return true (run original), index doesn't fit in references, reference null, or we control
                if (TrackedEncryption.trackedEncryptionReferences.Length <= index ||
                    TrackedEncryption.trackedEncryptionReferences[index] == null ||
                    TrackedEncryption.trackedEncryptionReferences[index].data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    // Do update we don't want to prevent
                    if (__instance.UseReturnToSpawnForce && __instance.m_returnToSpawnLine != null)
                    {
                        __instance.UpdateLine();
                    }
                    if (__instance.Type == TNH_EncryptionType.Orthagonal && __instance.isOrthagonalBeamFiring)
                    {
                        for (int k = 0; k < __instance.BeamDirs.Count; k++)
                        {
                            Vector3 vector3 = __instance.transform.rotation * __instance.BeamDirs[k];
                            float num16 = 200f;
                            if (Physics.Raycast(__instance.RB.position, vector3, out __instance.m_hit, 200f, __instance.OrthagonalBeamLM, QueryTriggerInteraction.Collide))
                            {
                                num16 = __instance.m_hit.distance;
                            }
                            __instance.OrthagonalBeams[k].rotation = Quaternion.LookRotation(vector3);
                            __instance.OrthagonalBeams[k].localScale = new Vector3(0.06f, 0.06f, num16);
                            __instance.HitPoints[k].transform.position = __instance.transform.position + vector3 * num16;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        // To track subtargGeo and subtarg activation for regenerative 
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Dup)); // Dupe subTarg/Geo GameObject
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_activeSelf"))); // Get activeSelf
            toInsert.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(EncryptionPatch), "preActive"))); // Set our preActive

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load encryption instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load i
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EncryptionPatch), "ActivateSubTargGeo"))); // Call our ActivateSubTargGeo method

            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load encryption instance
            toInsert1.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load i
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EncryptionPatch), "ActivateSubTarg"))); // Call our ActivateSubTarg method

            bool[] applied = new bool[4];
            bool found = false;
            int foundCount = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("SetActive"))
                {
                    if (found)
                    {
                        instructionList.InsertRange(i + 1, toInsert1);
                        applied[0] = true;
                        break;

                    }
                    else
                    {
                        instructionList.InsertRange(i + 1, toInsert0);
                        applied[1] = true;
                        found = true;
                    }
                }

                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_gameObject"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied[2] = true;
                }

                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && instruction.operand.ToString().Contains("get_Item"))
                {
                    ++foundCount;
                    if(foundCount == 4)
                    {
                        instructionList.InsertRange(i + 1, toInsert);
                        applied[3] = true;
                    }
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("EncryptionPatch UpdateTranspiler not applied!");
                    break;
                }
            }

            return instructionList;
        }

        public static void ActivateSubTargGeo(TNH_EncryptionTarget encryption, int index)
        {
            if (Mod.managerObject != null && !preActive)
            {
                TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.TryGetValue(encryption, out trackedEncryption) ? trackedEncryption : encryption.GetComponent<TrackedEncryption>();
                if (trackedEncryption != null)
                {
                    // Note: Update not being prevented implies we are controller
                    trackedEncryption.encryptionData.subTargGeosActive[index] = true;
                    trackedEncryption.encryptionData.subTargsActive[index] = true;

                    if (ThreadManager.host)
                    {
                        ServerSend.EncryptionRespawnSubTargGeo(trackedEncryption.data.trackedID, index);
                    }
                    else
                    {
                        if (trackedEncryption.data.trackedID == -1)
                        {
                            if (TrackedEncryption.unknownSpawnSubTargGeo.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<int> l))
                            {
                                l.Add(index);
                            }
                            else
                            {
                                TrackedEncryption.unknownSpawnSubTargGeo.Add(trackedEncryption.data.localWaitingIndex, new List<int>() { index });
                            }
                        }
                        else
                        {
                            ClientSend.EncryptionRespawnSubTargGeo(trackedEncryption.data.trackedID, index);
                        }
                    }
                }
            }
        }

        public static void ActivateSubTarg(TNH_EncryptionTarget encryption, int index)
        {
            if(Mod.managerObject != null && !preActive)
            {
                TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.TryGetValue(encryption, out trackedEncryption) ? trackedEncryption : encryption.GetComponent<TrackedEncryption>();
                if(trackedEncryption != null)
                {
                    // Note: Update not being prevented implies we are controller
                    trackedEncryption.encryptionData.subTargsActive[index] = true;

                    if (ThreadManager.host)
                    {
                        ServerSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                    }
                    else
                    {
                        if (trackedEncryption.data.trackedID == -1)
                        {
                            if (TrackedEncryption.unknownSpawnSubTarg.TryGetValue(trackedEncryption.data.localWaitingIndex, out List<int> l))
                            {
                                l.Add(index);
                            }
                            else
                            {
                                TrackedEncryption.unknownSpawnSubTarg.Add(trackedEncryption.data.localWaitingIndex, new List<int>() { index });
                            }
                        }
                        else
                        {
                            ClientSend.EncryptionRespawnSubTarg(trackedEncryption.data.trackedID, index);
                        }
                    }
                }
            }
        }

        // To prevent Start from overriding initial data we got from controller
        static bool StartPrefix(TNH_EncryptionTarget __instance, ref int ___m_numHitsLeft, ref int ___m_maxHits, ref float ___m_damLeftForAHit,
                                ref Vector3 ___initialPos, ref Quaternion ___m_fromRot, ref float ___m_timeTilWarp, ref float ___m_warpSpeed,
                                ref List<Vector3> ___m_validAgilePos, ref int ___m_numSubTargsLeft, float ___AgileBaseSpeed, bool ___UsesRegeneratingSubtargs,
                                ref float[] ___SubTargScales)
        {
            ++EncryptionSpawnGrowthPatch.skip;

            if (Mod.managerObject == null)
            {
                return true;
            }

            trackedEncryption = GameManager.trackedEncryptionByEncryption.TryGetValue(__instance, out trackedEncryption) ? trackedEncryption : __instance.GetComponent<TrackedEncryption>();
            if (trackedEncryption != null)
            {
                if (trackedEncryption.data.controller != GameManager.ID)
                {
                    __instance.PrimeDics();
                    ___m_numHitsLeft = __instance.NumHitsTilDestroyed;
                    ___m_maxHits = __instance.NumHitsTilDestroyed;
                    ___m_damLeftForAHit = __instance.DamagePerHit;
                    ___initialPos = __instance.transform.position;
                    ___m_fromRot = __instance.transform.rotation;
                    ___m_timeTilWarp = 0f;
                    ___m_warpSpeed = UnityEngine.Random.Range(4f, 5f) * ___AgileBaseSpeed;
                    if (__instance.UsesAgileMovement)
                    {
                        ___m_validAgilePos = new List<Vector3>();
                    }
                    if (__instance.UsesRegenerativeSubTarg)
                    {
                        for (int i = 0; i < __instance.Tendrils.Count; i++)
                        {
                            __instance.Tendrils[i].transform.SetParent(null);
                            __instance.SubTargs[i].transform.SetParent(null);
                        }
                        ___m_numSubTargsLeft = __instance.StartingRegenSubTarg;
                    }
                    if (__instance.UsesRefractiveTeleportation && __instance.RefractivePreview != null)
                    {
                        __instance.RefractivePreview.SetParent(null);
                        __instance.SetNextPos();
                    }
                    if (___UsesRegeneratingSubtargs)
                    {
                        ___SubTargScales = new float[__instance.SubTargs.Count];
                    }
                    if (__instance.UsesRecursiveSubTarg)
                    {
                        for (int i = 0; i < trackedEncryption.encryptionData.subTargsActive.Length; ++i)
                        {
                            if (trackedEncryption.encryptionData.subTargsActive[i])
                            {
                                ++___m_numSubTargsLeft;
                            }
                        }
                    }
                    if (__instance.UsesSubTargs && !__instance.UsesRecursiveSubTarg && !__instance.UsesRegenerativeSubTarg)
                    {
                        ___m_numSubTargsLeft = __instance.SubTargs.Count;
                    }
                    return false;
                }
            }

            return true;
        }

        static void StartPostfix(TNH_EncryptionTarget __instance)
        {
            --EncryptionSpawnGrowthPatch.skip;

            if (Mod.managerObject != null && trackedEncryption != null && trackedEncryption.data.controller == GameManager.ID)
            {
                List<int> indices = null;
                List<Vector3> points = null;
                if (__instance.Type == TNH_EncryptionType.Regenerative && __instance.UsesRegenerativeSubTarg)
                {
                    indices = new List<int>();
                    points = new List<Vector3>();
                    for (int i = 0; i < __instance.SubTargs.Count; ++i)
                    {
                        if (__instance.SubTargs[i].activeSelf)
                        {
                            trackedEncryption.encryptionData.subTargsActive[i] = true;
                            trackedEncryption.encryptionData.subTargPos[i] = __instance.GrowthPoints[i];
                            indices.Add(i);
                            points.Add(__instance.SubTargs[i].transform.position);
                        }
                    }
                }
                else if (__instance.Type == TNH_EncryptionType.Recursive || __instance.Type == TNH_EncryptionType.Polymorphic)
                {
                    indices = new List<int>();
                    for (int i = 0; i < __instance.SubTargs.Count; ++i)
                    {
                        if (__instance.SubTargs[i].activeSelf)
                        {
                            trackedEncryption.encryptionData.subTargsActive[i] = true;
                            indices.Add(i);
                        }
                    }
                }
                trackedEncryption.encryptionData.initialPos = trackedEncryption.physicalEncryption.initialPos;
                trackedEncryption.encryptionData.numHitsLeft = trackedEncryption.physicalEncryption.m_numHitsLeft;

                if (ThreadManager.host)
                {
                    ServerSend.EncryptionInit(0, trackedEncryption.data.trackedID, indices, points, trackedEncryption.encryptionData.initialPos, trackedEncryption.encryptionData.numHitsLeft);
                }
                else
                {
                    if (trackedEncryption.data.trackedID == -1)
                    {
                        if (TrackedEncryption.unknownInit.ContainsKey(trackedEncryption.data.localWaitingIndex))
                        {
                            TrackedEncryption.unknownInit[trackedEncryption.data.localWaitingIndex] = new KeyValuePair<List<int>, List<Vector3>>(indices, points);
                        }
                        else
                        {
                            TrackedEncryption.unknownInit.Add(trackedEncryption.data.localWaitingIndex, new KeyValuePair<List<int>, List<Vector3>>(indices, points));
                        }
                    }
                    else
                    {
                        ClientSend.EncryptionInit(trackedEncryption.data.trackedID, indices, points, trackedEncryption.encryptionData.initialPos, trackedEncryption.encryptionData.numHitsLeft);
                    }
                }
            }
        }

        // To sync display update
        static void UpdateDisplayPostfix(TNH_EncryptionTarget __instance)
        {
            if (updateDisplaySkip > 0 || Mod.managerObject == null)
            {
                return;
            }

            TrackedEncryption trackedEncryption = GameManager.trackedEncryptionByEncryption.TryGetValue(__instance, out trackedEncryption) ? trackedEncryption : __instance.GetComponent<TrackedEncryption>();
            if (trackedEncryption != null && trackedEncryption.data.controller == GameManager.ID)
            {
                trackedEncryption.encryptionData.numHitsLeft = __instance.m_numHitsLeft;

                if (ThreadManager.host)
                {
                    ServerSend.UpdateEncryptionDisplay(trackedEncryption.data.trackedID, __instance.m_numHitsLeft);
                }
                else
                {
                    if(trackedEncryption.data.trackedID == -1)
                    {
                        if (TrackedEncryption.unknownUpdateDisplay.ContainsKey(trackedEncryption.data.localWaitingIndex))
                        {
                            TrackedEncryption.unknownUpdateDisplay[trackedEncryption.data.localWaitingIndex] = __instance.m_numHitsLeft;
                        }
                        else
                        {
                            TrackedEncryption.unknownUpdateDisplay.Add(trackedEncryption.data.localWaitingIndex, __instance.m_numHitsLeft);
                        }
                    }
                    else
                    {
                        ClientSend.UpdateEncryptionDisplay(trackedEncryption.data.trackedID, __instance.m_numHitsLeft);
                    }
                }
            }
        }

        // To track everything instantiated from SpawnOnDestruction
        static IEnumerable<CodeInstruction> DestroyTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load encryption instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_1)); // Load j
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_2)); // Load the newly instantiated GameObject
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), "get_transform"))); // Get transform
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EncryptionPatch), "EncryptionSpawnOnDestroy"))); // Call our method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Stloc_2)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("EncryptionPatch DestroyTranspiler not applied!");
            }

            return instructionList;
        }

        public static void EncryptionSpawnOnDestroy(TNH_EncryptionTarget encryption, int index, Transform t)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            cascadingDestroyIndex = index;
            int strIndex = encryption.name.LastIndexOf("SubShard");
            if (strIndex == -1) // This shard is product of destruction of Main
            {
                cascadingDestroyDepth = 1;
            }
            else // This shard is product of destruction of another subshard
            {
                // If source subshard is A, depth is 2, so the letter we get in the name after "SubShard" as an int - 'A' as an int, will give 0-based offset from A
                // +2 will give the depth we want
                // So if name has SubShardA, - 'A' will be 0, +2 will give depth of 2
                cascadingDestroyDepth = 2 + encryption.name[strIndex + 8] - 'A';
            }

            GameManager.SyncTrackedObjects(t, true, null);

            cascadingDestroyIndex = 0;
            cascadingDestroyDepth = 0;
        }

        static bool FireGunPrefix(TNH_EncryptionTarget __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (int.TryParse(__instance.SpawnPoints[__instance.SpawnPoints.Count - 1].name, out int index))
            {
                TrackedEncryption trackedEncryption = TrackedEncryption.trackedEncryptionReferences[index];
                if (trackedEncryption != null)
                {
                    if (trackedEncryption.data.controller == GameManager.ID)
                    {
                        float[] vels = new float[__instance.RefractiveMuzzles.Count];
                        Vector3[] dirs = new Vector3[__instance.RefractiveMuzzles.Count];
                        for (int i = 0; i < __instance.RefractiveMuzzles.Count; i++)
                        {
                            Vector3 position = __instance.RefractiveMuzzles[i].position;
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.RefractiveProjectile, position, __instance.RefractiveMuzzles[i].rotation);
                            BallisticProjectile component = gameObject.GetComponent<BallisticProjectile>();
                            component.FlightVelocityMultiplier = 0.04f;
                            float muzzleVelocityBase = component.MuzzleVelocityBase;
                            component.Fire(muzzleVelocityBase, gameObject.transform.forward, null, true);

                            vels[i] = muzzleVelocityBase;
                            dirs[i] = gameObject.transform.forward;

                            if (ThreadManager.host)
                            {
                                ServerSend.EncryptionFireGun(trackedEncryption.data.trackedID, vels, dirs);
                            }
                            else
                            {
                                ClientSend.EncryptionFireGun(trackedEncryption.data.trackedID, vels, dirs);
                            }
                        }
                        if (__instance.GunShotProfile != null)
                        {
                            FVRSoundEnvironment se = __instance.PlayShotEvent(__instance.RefractiveMuzzles[0].position);
                            float soundTravelDistanceMultByEnvironment = SM.GetSoundTravelDistanceMultByEnvironment(se);
                        }
                    }

                    return false;
                }
            }

            return true;
        }
    }

    // Patches FVRGrenade to sync
    class FVRGrenadePatch
    {
        static bool exploded;

        // To prevent FVRUpdate from happening if not in control
        static bool UpdatePrefix(FVRGrenade __instance, bool ___m_hasSploded, Dictionary<int, float> ___FuseTimings)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            exploded = ___m_hasSploded;

            if (___FuseTimings != null && ___FuseTimings.TryGetValue(-1, out float indexFloat))
            {
                // Return true (run original), if dont have an index, index doesn't fit in references (shouldn't happen?), reference null (shouldn't happen), or we control
                int index = (int)indexFloat;
                return index <= 0 ||
                       TrackedObject.trackedReferences.Length <= index ||
                       TrackedObject.trackedReferences[index] == null ||
                       TrackedObject.trackedReferences[index].data.controller == GameManager.ID;
            }

            return true;
        }

        // To know if grenade exploded in latest update
        static void UpdatePostfix(FVRGrenade __instance, bool ___m_hasSploded)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!exploded && ___m_hasSploded)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller == GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.FVRGrenadeExplode(0, trackedItem.data.trackedID, __instance.transform.position);
                    }
                    else
                    {
                        ClientSend.FVRGrenadeExplode(trackedItem.data.trackedID, __instance.transform.position);
                    }
                }
            }
        }

        public static void ExplodeGrenade(FVRGrenade grenade, Vector3 pos)
        {
            grenade.m_hasSploded = true;
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
            if (skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.BangSnapSplode(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    ClientSend.BangSnapSplode(trackedItem.data.trackedID, __instance.transform.position);
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Brtrue)
                {
                    instructionList[i + 1].labels.Add(l);
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("BangSnapPatch CollisionTranspiler not applied!");
            }

            return instructionList;
        }

        public static bool Controlled(BangSnap bangSnap)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(bangSnap, out trackedItem) ? trackedItem : bangSnap.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                return trackedItem.data.controller == GameManager.ID;
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
            if (skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.C4Detonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    ClientSend.C4Detonate(trackedItem.data.trackedID, __instance.transform.position);
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
            if (skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.ClaymoreMineDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    ClientSend.ClaymoreMineDetonate(trackedItem.data.trackedID, __instance.transform.position);
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
            if (skip > 0 || Mod.managerObject == null)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SLAMDetonate(0, trackedItem.data.trackedID, __instance.transform.position);
                }
                else
                {
                    ClientSend.SLAMDetonate(trackedItem.data.trackedID, __instance.transform.position);
                }
            }
        }
    }

    // Patches FVRFireArmRound
    class RoundPatch
    {
        public static int splodeSkip = 0;
        public static int splodeInDamage = 0;

        // Patches FVRFixedUpdate to prevent insertion into ammo container if we are not round controller
        static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // (8) Declare local for the tracked item we got on this round
            LocalBuilder localTrackedItem = il.DeclareLocal(typeof(TrackedItem));
            localTrackedItem.SetLocalSymInfo("trackedItem");

            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // Chamber
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Mod), "managerObject"))); // Load managerObject
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality"))); // Compare for inequality (true if connected) ***
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 8)); // Init trackedItem to null
            toInsert.Add(new CodeInstruction(OpCodes.Dup)); // Dupe inequality call result on stack (true if connected)
            int labelIndex0 = toInsert.Count;
            Label afterGettingTrackedItemChamberLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemChamberLabel)); // If false (not connected) skip trying to get a tracked item

            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "trackedItemByItem"))); // Load trackedItemByItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load round instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloca_S, 8)); // Load trackedItem address
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<FVRPhysicalObject, TrackedItem>), "TryGetValue"))); // Call TryGetValue trackedItemByItem
            int labelIndex1 = toInsert.Count;
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemChamberLabel)); // If true (found round in trackedItemByItem) skip trying to get a tracked item component directly

            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load round instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "GetComponent", null, new Type[] { typeof(TrackedItem) }))); // Get TrackedItem component directly from round
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 8)); // Set trackedItem

            int labelIndex2 = toInsert.Count;
            Label startLoadChamberLabel = il.DefineLabel();
            CodeInstruction afterGettingTrackedItem = new CodeInstruction(OpCodes.Brfalse_S, startLoadChamberLabel); // If false (not connected) (see *** above), skip or to start of chamber load
            afterGettingTrackedItem.labels.Add(afterGettingTrackedItemChamberLabel);
            toInsert.Add(afterGettingTrackedItem);

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality"))); // Compare for equality (true if dont have trackedItem)
            int labelIndex3 = toInsert.Count;
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, startLoadChamberLabel)); // If true (trackedItem is null), goto start chamber load

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TrackedItem), "data"))); // Load trackedItem data
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TrackedItemData), "get_controller"))); // Load trackedItem's controller index
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "ID"))); // Load our ID
            int labelIndex4 = toInsert.Count;
            Label afterLoadChamberLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Bne_Un, afterLoadChamberLabel)); // Compare our ID with controller, if we are not controller skip load into chamber

            // Define clip label
            Label startLoadClipLabel = il.DefineLabel();

            // Define SLChamber label
            Label startLoadSLChamberLabel = il.DefineLabel();

            // Define RemoteGun label
            Label startLoadRemoteGunLabel = il.DefineLabel();

            // Define Mag label
            Label startLoadMagLabel = il.DefineLabel();

            // Define final return label
            Label returnLabel = il.DefineLabel();

            bool[] applied = new bool[5];
            bool doubleInstanceFound = false;
            bool chamberEndFound = false;
            int getCountFound = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldarg_0 && instructionList[i + 1].opcode == OpCodes.Ldarg_0)
                {
                    if (!doubleInstanceFound)
                    {
                        instruction.labels.Add(startLoadChamberLabel);
                        instructionList.InsertRange(i, toInsert);
                        applied[0] = true;
                        i += toInsert.Count;

                        doubleInstanceFound = true;
                    }
                }
                if (instruction.opcode == OpCodes.Ldarg_0 && instructionList[i + 1].opcode == OpCodes.Ldnull)
                {
                    if (!chamberEndFound)
                    {
                        instruction.labels.Add(afterLoadChamberLabel);

                        chamberEndFound = true;

                        // Set labels for Clip
                        Label afterGettingTrackedItemClipLabel = il.DefineLabel();
                        toInsert[labelIndex0] = new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemClipLabel); // If false (not connected) skip trying to get a tracked item

                        toInsert[labelIndex1] = new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemClipLabel); // If true (found round in trackedItemByItem) skip trying to get a tracked item component directly

                        CodeInstruction afterGettingTrackedItemClip = new CodeInstruction(OpCodes.Brfalse_S, startLoadClipLabel); // If false (not connected) (see *** above), skip or to start of chamber load
                        afterGettingTrackedItemClip.labels.Add(afterGettingTrackedItemClipLabel);
                        toInsert[labelIndex2] = afterGettingTrackedItemClip;

                        toInsert[labelIndex3] = new CodeInstruction(OpCodes.Brtrue_S, startLoadClipLabel); // If false (trackedItem is null), goto start chamber load

                        toInsert[labelIndex4] = new CodeInstruction(OpCodes.Bne_Un, returnLabel); // Compare our ID with controller, if we are not controller skip load into chamber
                    }
                }
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_Count"))
                {
                    if (getCountFound == 1)
                    {
                        instructionList[i - 2].labels.Add(startLoadClipLabel);
                        instructionList.InsertRange(i - 2, toInsert);
                        i += toInsert.Count;
                        applied[1] = true;

                        // Set labels for SL Chamber
                        Label afterGettingTrackedItemSLChamberLabel = il.DefineLabel();
                        toInsert[labelIndex0] = new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemSLChamberLabel); // If false (not connected) skip trying to get a tracked item

                        toInsert[labelIndex1] = new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemSLChamberLabel); // If true (found round in trackedItemByItem) skip trying to get a tracked item component directly

                        CodeInstruction afterGettingTrackedItemSLChamber = new CodeInstruction(OpCodes.Brfalse_S, startLoadSLChamberLabel); // If false (not connected) (see *** above), skip or to start of chamber load
                        afterGettingTrackedItemSLChamber.labels.Add(afterGettingTrackedItemSLChamberLabel);
                        toInsert[labelIndex2] = afterGettingTrackedItemSLChamber;

                        toInsert[labelIndex3] = new CodeInstruction(OpCodes.Brtrue_S, startLoadSLChamberLabel); // If false (trackedItem is null), goto start chamber load

                        toInsert[labelIndex4] = new CodeInstruction(OpCodes.Bne_Un, returnLabel); // Compare our ID with controller, if we are not controller skip load into chamber
                    }
                    else if (getCountFound == 2)
                    {
                        instructionList[i - 2].labels.Add(startLoadSLChamberLabel);
                        instructionList.InsertRange(i - 2, toInsert);
                        i += toInsert.Count;
                        applied[2] = true;

                        // Set labels for RemoteGun
                        Label afterGettingTrackedItemRemoteGunLabel = il.DefineLabel();
                        toInsert[labelIndex0] = new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemRemoteGunLabel); // If false (not connected) skip trying to get a tracked item

                        toInsert[labelIndex1] = new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemRemoteGunLabel); // If true (found round in trackedItemByItem) skip trying to get a tracked item component directly

                        CodeInstruction afterGettingTrackedItemRemoteGun = new CodeInstruction(OpCodes.Brfalse_S, startLoadRemoteGunLabel); // If false (not connected) (see *** above), skip or to start of chamber load
                        afterGettingTrackedItemRemoteGun.labels.Add(afterGettingTrackedItemRemoteGunLabel);
                        toInsert[labelIndex2] = afterGettingTrackedItemRemoteGun;

                        toInsert[labelIndex3] = new CodeInstruction(OpCodes.Brtrue_S, startLoadRemoteGunLabel); // If false (trackedItem is null), goto start chamber load

                        toInsert[labelIndex4] = new CodeInstruction(OpCodes.Bne_Un, returnLabel); // Compare our ID with controller, if we are not controller skip load into chamber
                    }
                    else if (getCountFound == 3)
                    {
                        instructionList[i - 2].labels.Add(startLoadRemoteGunLabel);
                        instructionList.InsertRange(i - 2, toInsert);
                        i += toInsert.Count;
                        applied[3] = true;

                        // Set labels for Mag
                        Label afterGettingTrackedItemMagLabel = il.DefineLabel();
                        toInsert[labelIndex0] = new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemMagLabel); // If false (not connected) skip trying to get a tracked item

                        toInsert[labelIndex1] = new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemMagLabel); // If true (found round in trackedItemByItem) skip trying to get a tracked item component directly

                        CodeInstruction afterGettingTrackedItemMag = new CodeInstruction(OpCodes.Brfalse_S, startLoadMagLabel); // If false (not connected) (see *** above), skip or to start of chamber load
                        afterGettingTrackedItemMag.labels.Add(afterGettingTrackedItemMagLabel);
                        toInsert[labelIndex2] = afterGettingTrackedItemMag;

                        toInsert[labelIndex3] = new CodeInstruction(OpCodes.Brtrue_S, startLoadMagLabel); // If false (trackedItem is null), goto start chamber load

                        toInsert[labelIndex4] = new CodeInstruction(OpCodes.Bne_Un, returnLabel); // Compare our ID with controller, if we are not controller skip load into chamber
                    }
                    else if (getCountFound == 5)
                    {
                        instructionList[i - 2].labels.Add(startLoadMagLabel);
                        instructionList.InsertRange(i - 2, toInsert);
                        applied[4] = true;
                        i += toInsert.Count;
                    }
                    ++getCountFound;
                }
                if (instruction.opcode == OpCodes.Ret)
                {
                    instruction.labels.Add(returnLabel);
                    break;
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("RoundPatch FixedUpdateTranspiler not applied!");
                    break;
                }
            }

            return instructionList;
        }

        static bool SplodePrefix(FVRFireArmRound __instance, float velMultiplier, bool isRandomDir)
        {
            if(Mod.managerObject == null || splodeSkip > 0)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if(trackedItem != null)
            {
                // A player cannot trigger the splode of a round in their own belt,
                // so if we Splode while splodeInDamage, it means that the Splode was called 
                // from a damage order received by another player, at which point we only want to
                // actually go through with the Splode iff the round is not in a QBS
                // Note: A remote player cannot trigger a Splode directly in Damage on a non controlled round
                //       because Damage() calls are sent to controller for processing
                if (trackedItem.data.controller == GameManager.ID && splodeInDamage > 0)
                {
                    trackedItem.data.IsControlled(out int interactID);
                    if(interactID > 2 && interactID < 515)
                    {
                        return false;
                    }
                }

                if (ThreadManager.host)
                {
                    ServerSend.RoundSplode(trackedItem.data.trackedID, velMultiplier, isRandomDir);
                }
                else if(trackedItem.data.trackedID != -1)
                {
                    ClientSend.RoundSplode(trackedItem.data.trackedID, velMultiplier, isRandomDir);
                }
            }

            return true;
        }
    }

    // Patches FVRFireArmMagazine
    class MagazinePatch
    {
        public static int addRoundSkip;
        public static int loadSkip;

        // Patches AddRound(FireArmRoundClass) to keep track of event
        static IEnumerable<CodeInstruction> AddRoundClassTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load mag instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load rClass
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MagazinePatch), "AddRound"))); // Call our AddRound method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Bge)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MagazinePatch AddRoundClassTranspiler not applied!");
            }

            return instructionList;
        }

        // Patches AddRound(FVRFireArmRound) to keep track of event
        static IEnumerable<CodeInstruction> AddRoundRoundTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load mag instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load round
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), "RoundClass"))); // Load round class
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MagazinePatch), "AddRound"))); // Call our AddRound method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Bge)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MagazinePatch AddRoundRoundTranspiler not applied!");
            }

            return instructionList;
        }

        public static void AddRound(FVRFireArmMagazine mag, FireArmRoundClass roundClass)
        {
            if (Mod.managerObject == null || addRoundSkip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(mag, out trackedItem) ? trackedItem : mag.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.MagazineAddRound(trackedItem.data.trackedID, roundClass);
                }
                else
                {
                    ClientSend.MagazineAddRound(trackedItem.data.trackedID, roundClass);
                }
            }
        }

        // Patches Load(FVRFireArm) to control and keep track of event
        static bool LoadFireArmPrefix(FVRFireArmMagazine __instance, FVRFireArm fireArm)
        {
            return Load(__instance, fireArm);
        }

        // Patches LoadIntoSecondary() to control and keep track of event
        static bool LoadIntoSecondaryPrefix(FVRFireArmMagazine __instance, FVRFireArm fireArm, int slot)
        {
            return Load(__instance, fireArm, slot);
        }

        // Only allow load if we control mag
        private static bool Load(FVRFireArmMagazine magInstance, FVRFireArm fireArm, int slot = -1)
        {
            if (Mod.managerObject == null || loadSkip > 0)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(magInstance, out trackedItem) ? trackedItem : magInstance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Don't want to load if we are not controller
                if (trackedItem.data.controller != GameManager.ID)
                {
                    return false;
                }

                TrackedItem FATrackedItem = GameManager.trackedItemByItem.TryGetValue(fireArm, out FATrackedItem) ? FATrackedItem : fireArm.GetComponent<TrackedItem>();
                // Only need to send order to load if we are not firearm controller
                if (FATrackedItem != null && FATrackedItem.data.controller != GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.MagazineLoad(trackedItem.data.trackedID, FATrackedItem.data.trackedID, slot);
                    }
                    else
                    {
                        ClientSend.MagazineLoad(trackedItem.data.trackedID, FATrackedItem.data.trackedID, slot);
                    }
                }
            }

            return true;
        }

        // Patches Load(AttachableFirearm) to control and keep track of event
        static bool LoadAttachablePrefix(FVRFireArmMagazine __instance, AttachableFirearm fireArm)
        {
            if (Mod.managerObject == null || loadSkip > 0)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Don't want to load if we are not controller
                if (trackedItem.data.controller != GameManager.ID)
                {
                    return false;
                }

                TrackedItem FATrackedItem = GameManager.trackedItemByItem.TryGetValue(fireArm.Attachment, out FATrackedItem) ? FATrackedItem : fireArm.Attachment.GetComponent<TrackedItem>();
                // Only need to send order to load if we are not firearm controller
                if (FATrackedItem != null && FATrackedItem.data.controller != GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.MagazineLoadAttachable(trackedItem.data.trackedID, FATrackedItem.data.trackedID);
                    }
                    else
                    {
                        ClientSend.MagazineLoadAttachable(trackedItem.data.trackedID, FATrackedItem.data.trackedID);
                    }
                }
            }

            return true;
        }
    }

    // Patches FVRFireArmClip
    class ClipPatch
    {
        public static int addRoundSkip;
        public static int loadSkip;

        // Patches AddRound(FireArmRoundClass) to keep track of event
        static IEnumerable<CodeInstruction> AddRoundClassTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load clip instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load rClass
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClipPatch), "AddRound"))); // Call our AddRound method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Bge)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("ClipPatch AddRoundClassTranspiler not applied!");
            }

            return instructionList;
        }

        // Patches AddRound(FVRFireArmRound) to keep track of event
        static IEnumerable<CodeInstruction> AddRoundRoundTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load clip instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_1)); // Load round
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FVRFireArmRound), "RoundClass"))); // Load round class
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ClipPatch), "AddRound"))); // Call our AddRound method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Bge)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("ClipPatch AddRoundClassTranspiler not applied!");
            }

            return instructionList;
        }

        public static void AddRound(FVRFireArmClip clip, FireArmRoundClass roundClass)
        {
            if (Mod.managerObject == null || addRoundSkip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(clip, out trackedItem) ? trackedItem : clip.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.ClipAddRound(trackedItem.data.trackedID, roundClass);
                }
                else
                {
                    ClientSend.ClipAddRound(trackedItem.data.trackedID, roundClass);
                }
            }
        }

        // Patches Load() to control and keep track of event
        static bool LoadPrefix(FVRFireArmClip __instance, FVRFireArm fireArm)
        {
            if (Mod.managerObject == null || loadSkip > 0)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                // Don't want to load if we are not controller
                if (trackedItem.data.controller != GameManager.ID)
                {
                    return false;
                }

                TrackedItem FATrackedItem = GameManager.trackedItemByItem.TryGetValue(fireArm, out FATrackedItem) ? FATrackedItem : fireArm.GetComponent<TrackedItem>();
                // Only need to send order to load if we are not firearm controller
                if (FATrackedItem != null && FATrackedItem.data.controller != GameManager.ID)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.ClipLoad(trackedItem.data.trackedID, FATrackedItem.data.trackedID);
                    }
                    else
                    {
                        ClientSend.ClipLoad(trackedItem.data.trackedID, FATrackedItem.data.trackedID);
                    }
                }
            }

            return true;
        }
    }

    // Patches SpeedloaderChamber
    class SpeedloaderChamberPatch
    {
        public static int loadSkip;

        // Patches Load() to track event
        static void AddRoundPrefix(SpeedloaderChamber __instance, FireArmRoundClass rclass)
        {
            if (Mod.managerObject == null || loadSkip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.SpeedLoader, out trackedItem) ? trackedItem : __instance.SpeedLoader.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                Speedloader speedLoader = trackedItem.physicalItem as Speedloader;
                int chamberIndex = -1;
                for (int i = 0; i < speedLoader.Chambers.Count; ++i)
                {
                    if (speedLoader.Chambers[i] == __instance)
                    {
                        chamberIndex = i;
                        break;
                    }
                }

                if (chamberIndex > -1)
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.SpeedloaderChamberLoad(trackedItem.data.trackedID, rclass, chamberIndex);
                    }
                    else
                    {
                        ClientSend.SpeedloaderChamberLoad(trackedItem.data.trackedID, rclass, chamberIndex);
                    }
                }
            }
        }
    }

    // Patches RemoteGun
    class RemoteGunPatch
    {
        public static int chamberSkip;

        // Patches ChamberCartridge() to track event
        static void ChamberCartridgePrefix(RemoteGun __instance, FVRFireArmRound round)
        {
            if (Mod.managerObject == null || chamberSkip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.RemoteGunChamber(trackedItem.data.trackedID, round.RoundClass, round.RoundType);
                }
                else
                {
                    ClientSend.RemoteGunChamber(trackedItem.data.trackedID, round.RoundClass, round.RoundType);
                }
            }
        }
    }

    // Patches FVRFireArmChamber
    class ChamberPatch
    {
        public static int chamberSkip;

        static void SetRoundClassPrefix(FVRFireArmChamber __instance, FireArmRoundClass rclass)
        {
            if (Mod.managerObject == null || chamberSkip > 0)
            {
                return;
            }

            // Find item this chamber is attached to
            TrackedItem trackedItem = null;
            Transform currentParent = __instance.transform;
            while (currentParent != null)
            {
                trackedItem = currentParent.GetComponent<TrackedItem>();
                if (trackedItem != null)
                {
                    break;
                }
                currentParent = currentParent.parent;
            }

            // If we have a tracked item and we are not its controller, we need to send order to controller to set the round on their side
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID && trackedItem.getChamberIndex != null)
            {
                // Find the chamber's index on the tracked item
                int chamberIndex = trackedItem.getChamberIndex(__instance);
                if (chamberIndex == -1)
                {
                    Mod.LogError("SetRound(Class) called on chamber attached to " + trackedItem.name + " but chamber was not found on the item!");
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.ChamberRound(trackedItem.data.trackedID, rclass, chamberIndex);
                    }
                    else
                    {
                        ClientSend.ChamberRound(trackedItem.data.trackedID, rclass, chamberIndex);
                    }
                }
            }
        }

        static void SetRoundRoundPrefix(FVRFireArmChamber __instance, FVRFireArmRound round)
        {
            if (Mod.managerObject == null || chamberSkip > 0 || round == null)
            {
                return;
            }

            // Find item this chamber is attached to
            TrackedItem trackedItem = null;
            Transform currentParent = __instance.transform;
            while (currentParent != null)
            {
                trackedItem = currentParent.GetComponent<TrackedItem>();
                if (trackedItem != null)
                {
                    break;
                }
                currentParent = currentParent.parent;
            }

            // If we have a tracked item and we are not its controller, we need to send order to controller to set the round on their side
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID && trackedItem.getChamberIndex != null)
            {
                // Find the chamber's index on the tracked item
                int chamberIndex = trackedItem.getChamberIndex(__instance);
                if (chamberIndex == -1)
                {
                    Mod.LogError("SetRound(Round) called on chamber attached to " + trackedItem.name + " but chamber was not found on the item!");
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.ChamberRound(trackedItem.data.trackedID, round.RoundClass, chamberIndex);
                    }
                    else
                    {
                        ClientSend.ChamberRound(trackedItem.data.trackedID, round.RoundClass, chamberIndex);
                    }
                }
            }
        }
    }

    // Patches Speedloader
    class SpeedloaderPatch
    {
        // Patches FVRFixedUpdate to prevent load in cylinder if we are not loader controller
        static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // (3) Declare local for the tracked item we got on this round
            LocalBuilder localTrackedItem = il.DeclareLocal(typeof(TrackedItem));
            localTrackedItem.SetLocalSymInfo("trackedItem");

            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Mod), "managerObject"))); // Load managerObject
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality"))); // Compare for inequality (true if connected) ***
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Init trackedItem to null
            toInsert.Add(new CodeInstruction(OpCodes.Dup)); // Dupe inequality call result on stack (true if connected)
            Label afterGettingTrackedItemLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemLabel)); // If false (not connected) skip trying to get a tracked item

            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "trackedItemByItem"))); // Load trackedItemByItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load speedloader instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloca_S, 3)); // Load trackedItem address
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<FVRPhysicalObject, TrackedItem>), "TryGetValue"))); // Call TryGetValue trackedItemByItem
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemLabel)); // If true (found speedloader in trackedItemByItem) skip trying to get a tracked item component directly

            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load speedloader instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "GetComponent", null, new Type[] { typeof(TrackedItem) }))); // Get TrackedItem component directly from speedloader
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set trackedItem

            Label startLoadLabel = il.DefineLabel();
            CodeInstruction afterGettingTrackedItem = new CodeInstruction(OpCodes.Brfalse_S, startLoadLabel); // If false (not connected) (see *** above), skip or to start of cylinder load
            afterGettingTrackedItem.labels.Add(afterGettingTrackedItemLabel);
            toInsert.Add(afterGettingTrackedItem);

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality"))); // Compare for equality (true if dont have trackedItem)
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, startLoadLabel)); // If true (trackedItem is null), goto start cylinder load

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TrackedItem), "data"))); // Load trackedItem data
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TrackedItemData), "get_controller"))); // Load trackedItem's controller index
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "ID"))); // Load our ID
            Label afterLoadLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Bne_Un, afterLoadLabel)); // Compare our ID with controller, if we are not controller skip load into cylinder

            bool applied = false;
            bool startFound = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stfld && instruction.operand.ToString().Contains("TimeTilLoadAttempt"))
                {
                    if (!startFound)
                    {
                        instructionList[i + 1].labels.Add(startLoadLabel);
                        instructionList.InsertRange(i + 1, toInsert);
                        i += toInsert.Count;
                        applied = true;

                        startFound = true;
                    }
                }
                if (instruction.opcode == OpCodes.Ret)
                {
                    instruction.labels.Add(afterLoadLabel);
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SpeedloaderPatch FixedUpdateTranspiler not applied!");
            }

            return instructionList;
        }

        // Patches FVRUpdate to prevent load in cylinder if we are not loader controller
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // (0) Declare local for the tracked item we got on this round
            LocalBuilder localTrackedItem = il.DeclareLocal(typeof(TrackedItem));
            localTrackedItem.SetLocalSymInfo("trackedItem");

            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();

            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Mod), "managerObject"))); // Load managerObject
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality"))); // Compare for inequality (true if connected) ***
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Init trackedItem to null
            toInsert.Add(new CodeInstruction(OpCodes.Dup)); // Dupe inequality call result on stack (true if connected)
            int labelIndex0 = toInsert.Count;
            Label afterGettingTrackedItemLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Brfalse_S, afterGettingTrackedItemLabel)); // If false (not connected) skip trying to get a tracked item

            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "trackedItemByItem"))); // Load trackedItemByItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load speedloader instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloca_S, 0)); // Load trackedItem address
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<FVRPhysicalObject, TrackedItem>), "TryGetValue"))); // Call TryGetValue trackedItemByItem
            int labelIndex3 = toInsert.Count;
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, afterGettingTrackedItemLabel)); // If true (found speedloader in trackedItemByItem) skip trying to get a tracked item component directly

            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load speedloader instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "GetComponent", null, new Type[] { typeof(TrackedItem) }))); // Get TrackedItem component directly from speedloader
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_0)); // Set trackedItem

            int labelIndex1 = toInsert.Count;
            Label startLoadLabel = il.DefineLabel();
            CodeInstruction afterGettingTrackedItem = new CodeInstruction(OpCodes.Brfalse_S, startLoadLabel); // If false (not connected) (see *** above), skip or to start of cylinder load
            afterGettingTrackedItem.labels.Add(afterGettingTrackedItemLabel);
            toInsert.Add(afterGettingTrackedItem);

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldnull)); // Load null
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality"))); // Compare for equality (true if dont have trackedItem)
            int labelIndex2 = toInsert.Count;
            toInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, startLoadLabel)); // If true (trackedItem is null), goto start cylinder load

            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load trackedItem
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TrackedItem), "data"))); // Load trackedItem data
            toInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TrackedItemData), "get_controller"))); // Load trackedItem's controller index
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), "ID"))); // Load our ID
            Label afterLoadLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Bne_Un, afterLoadLabel)); // Compare our ID with controller, if we are not controller skip load into cylinder

            // Define grappling start label
            Label afterGettingGrapplingTrackedItemLabel = il.DefineLabel();
            Label startGrapplingLoadLabel = il.DefineLabel();

            bool applied0 = false;
            bool applied1 = false;
            bool firstStartFound = false;
            bool secondStartFound = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("LoadCylinder"))
                {
                    if (!firstStartFound)
                    {
                        instructionList[i - 4].labels.Add(startLoadLabel);
                        instructionList.InsertRange(i - 4, toInsert);
                        i += toInsert.Count;
                        applied0 = true;

                        firstStartFound = true;

                        // Switch the start label instructions for the grappling one
                        toInsert[labelIndex0] = new CodeInstruction(OpCodes.Brfalse_S, afterGettingGrapplingTrackedItemLabel);
                        toInsert[labelIndex1] = new CodeInstruction(OpCodes.Brfalse_S, startGrapplingLoadLabel);
                        toInsert[labelIndex1].labels.Add(afterGettingGrapplingTrackedItemLabel);
                        toInsert[labelIndex2] = new CodeInstruction(OpCodes.Brtrue_S, startGrapplingLoadLabel);
                        toInsert[labelIndex3] = new CodeInstruction(OpCodes.Brtrue_S, afterGettingGrapplingTrackedItemLabel);
                    }
                    else if (!secondStartFound)
                    {
                        instructionList[i - 4].labels.Add(startGrapplingLoadLabel);
                        instructionList.InsertRange(i - 4, toInsert);
                        i += toInsert.Count;
                        applied1 = true;

                        secondStartFound = true;
                    }
                }
                if (instruction.opcode == OpCodes.Ret)
                {
                    instruction.labels.Add(afterLoadLabel);
                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("SpeedloaderPatch UpdateTranspiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches RevolverCylinder
    class RevolverCylinderPatch
    {
        // public static int loadSkip;

        // Patches LoadFromSpeedLoader() to track the event
        static void LoadFromSpeedLoaderPrefix(RevolverCylinder __instance, Speedloader loader)
        {
            if (Mod.managerObject == null /*|| loadSkip > 0*/)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance.Revolver, out trackedItem) ? trackedItem : __instance.Revolver.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.RevolverCylinderLoad(trackedItem.data.trackedID, loader);
                }
                else
                {
                    ClientSend.RevolverCylinderLoad(trackedItem.data.trackedID, loader);
                }
            }
        }
    }

    // Patches RevolvingShotgun
    class RevolvingShotgunPatch
    {
        // public static int loadSkip;

        // Patches LoadCylinder() to track the event
        static void LoadCylinderPrefix(RevolvingShotgun __instance, Speedloader s)
        {
            if (Mod.managerObject == null /*|| loadSkip > 0*/)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.RevolvingShotgunLoad(trackedItem.data.trackedID, s);
                }
                else
                {
                    ClientSend.RevolvingShotgunLoad(trackedItem.data.trackedID, s);
                }
            }
        }
    }

    // Patches GrappleGun
    class GrappleGunPatch
    {
        // public static int loadSkip;

        // Patches LoadCylinder() to track the event
        static void LoadCylinderPrefix(GrappleGun __instance, Speedloader s)
        {
            if (Mod.managerObject == null /*|| loadSkip > 0*/)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GrappleGunLoad(trackedItem.data.trackedID, s);
                }
                else
                {
                    ClientSend.GrappleGunLoad(trackedItem.data.trackedID, s);
                }
            }
        }
    }

    // Patches CarlGustafLatch
    class CarlGustafLatchPatch
    {
        public static int skip;

        // Patches FVRUpdate to keep track of events
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load CarlGustafLatch instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0 (false)
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CarlGustafLatchPatch), "SetLatchState"))); // Call our method

            bool applied0 = false;
            bool applied1 = false;
            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("PlayAudioAsHandling"))
                {
                    if (!found)
                    {
                        instructionList.InsertRange(i, toInsert);
                        i += toInsert.Count;
                        applied0 = true;

                        found = true;

                        // Switch load int instruction to load 1 (true)
                        toInsert[1] = new CodeInstruction(OpCodes.Ldc_I4_1);
                    }
                    else
                    {
                        instructionList.InsertRange(i, toInsert);
                        applied1 = true;

                        break;
                    }
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("CarlGustafLatchPatch UpdateTranspiler not applied!");
            }

            return instructionList;
        }

        public static void SetLatchState(CarlGustafLatch latch, bool open)
        {
            if (Mod.managerObject == null || skip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(latch.CG, out trackedItem) ? trackedItem : latch.CG.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.CarlGustafLatchSate(trackedItem.data.trackedID, latch.LType, latch.LState);
                }
                else
                {
                    ClientSend.CarlGustafLatchSate(trackedItem.data.trackedID, latch.LType, latch.LState);
                }
            }
        }
    }

    // Patches CarlGustafShellInsertEject
    class CarlGustafShellInsertEjectPatch
    {
        public static int skip;

        // Patches FVRUpdate to keep track of events and set the flag for chamber eject round
        static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load CarlGustafShellInsertEject instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0 (false)
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CarlGustafShellInsertEjectPatch), "SetShellSlideState"))); // Call our method

            CodeInstruction incInstruction = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CarlGustafShellInsertEjectPatch), "IncShellSlideEjectFlag"));
            CodeInstruction decInstruction = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CarlGustafShellInsertEjectPatch), "DecShellSlideEjectFlag"));

            bool[] applied = new bool[3];
            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("PlayAudioAsHandling"))
                {
                    if (!found)
                    {
                        instructionList.InsertRange(i, toInsert);
                        i += toInsert.Count;
                        applied[0] = true;

                        found = true;

                        // Switch load int instruction to load 1 (true)
                        toInsert[1] = new CodeInstruction(OpCodes.Ldc_I4_1);
                    }
                    else
                    {
                        instructionList.InsertRange(i, toInsert);
                        applied[1] = true;

                        break;
                    }
                }
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("EjectRound"))
                {
                    instructionList.Insert(i, incInstruction);
                    instructionList.Insert(i + 2, decInstruction);
                    i += 2;
                    applied[2] = true;
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("CarlGustafShellInsertEjectPatch UpdateTranspiler not applied!");
                    break;
                }
            }

            return instructionList;
        }

        public static void SetShellSlideState(CarlGustafShellInsertEject slide, bool slideIn)
        {
            if (Mod.managerObject == null || skip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(slide.CG, out trackedItem) ? trackedItem : slide.CG.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.CarlGustafShellSlideSate(trackedItem.data.trackedID, slide.CSState);
                }
                else
                {
                    ClientSend.CarlGustafShellSlideSate(trackedItem.data.trackedID, slide.CSState);
                }
            }
        }

        public static void IncShellSlideEjectFlag()
        {
            ++ChamberEjectRoundPatch.overrideFlag;
        }

        public static void DecShellSlideEjectFlag()
        {
            --ChamberEjectRoundPatch.overrideFlag;
        }
    }

    class GrappleThrowablePatch
    {
        private static TrackedItem trackedItem;

        // Patches OnCollisionEnter to keep track of when we attach to surcface
        static IEnumerable<CodeInstruction> CollisionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load GrappleThrowable instance
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrappleThrowablePatch), "AttachToSurface"))); // Call our method

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load GrappleThrowable instance
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrappleThrowablePatch), "CheckController"))); // Call our method

            bool applied0 = false;
            bool applied1 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("OnCollisionEnter"))
                {
                    toInsert0.Add(instructionList[i + 3]);
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied0 = true;
                }
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("SetActive"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied1 = true;
                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("GrappleThrowablePatch CollisionTranspiler not applied!");
            }

            return instructionList;
        }

        public static bool CheckController(GrappleThrowable grappleThrowable)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            trackedItem = GameManager.trackedItemByItem.TryGetValue(grappleThrowable, out trackedItem) ? trackedItem : grappleThrowable.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (trackedItem.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    trackedItem = null;
                    return false;
                }
            }

            return true;
        }

        public static void AttachToSurface(GrappleThrowable grappleThrowable)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (trackedItem != null && trackedItem.data.controller == GameManager.ID)
            {
                trackedItem.itemData.additionalData = new byte[grappleThrowable.finalRopePoints.Count * 12 + 2];

                trackedItem.itemData.additionalData[0] = grappleThrowable.m_hasLanded ? (byte)1 : (byte)0;
                trackedItem.itemData.additionalData[1] = (byte)grappleThrowable.finalRopePoints.Count;
                if (grappleThrowable.finalRopePoints.Count > 0)
                {
                    for (int i = 0; i < grappleThrowable.finalRopePoints.Count; ++i)
                    {
                        BitConverter.GetBytes(grappleThrowable.finalRopePoints[i].x).CopyTo(trackedItem.itemData.additionalData, i * 12 + 2);
                        BitConverter.GetBytes(grappleThrowable.finalRopePoints[i].y).CopyTo(trackedItem.itemData.additionalData, i * 12 + 6);
                        BitConverter.GetBytes(grappleThrowable.finalRopePoints[i].z).CopyTo(trackedItem.itemData.additionalData, i * 12 + 10);
                    }
                }

                if (ThreadManager.host)
                {
                    ServerSend.GrappleAttached(trackedItem.data.trackedID, trackedItem.itemData.additionalData);
                }
                else
                {
                    ClientSend.GrappleAttached(trackedItem.data.trackedID, trackedItem.itemData.additionalData);
                }
            }
        }
    }

    class FireArmPatch
    {
        // Patches Awake to modify audio pools
        static void AwakePostfix(FVRFireArm __instance)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            // TODO: Improvement: Possibly just edit the pooled audio source prefab at H3MP start, instead of having to edit it on every awake
            //                    Or will have to set these dynamically on each shot anyway because we want to change the distance depending on environment?
            //                    Something like that should also be dependent on something like round type
            // Configure shot pool
            if (__instance.m_pool_shot != null)
            {
                foreach (FVRPooledAudioSource audioSource in __instance.m_pool_shot.SourceQueue_Disabled)
                {
                    audioSource.Source.maxDistance = 75;
                }
            }

            // Configure tail pool
            if (__instance.m_pool_tail != null)
            {
                foreach (FVRPooledAudioSource audioSource in __instance.m_pool_tail.SourceQueue_Disabled)
                {
                    audioSource.Source.maxDistance = 150;
                    audioSource.Source.spatialBlend = 1;
                }
            }
        }

        // Patches PlayAudioGunShot(Round) to adapt to MP
        static bool PlayAudioGunShotRoundPrefix(FVRFireArm __instance, FVRFireArmRound round, float globalLoudnessMultiplier)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Get actual environment
            FVRSoundEnvironment env = SM.GetSoundEnvironment(__instance.transform.position);

            // Get actual IFF
            int IFF = GM.CurrentPlayerBody.GetPlayerIFF();
            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                IFF = GameManager.players[trackedItem.data.controller].IFF;
            }

            // Get distance and delay
            Vector3 pos = __instance.transform.position;
            float dist = Vector3.Distance(pos, GM.CurrentPlayerBody.Head.position);
            float delay = dist / 343f;

            // Do original but using delay
            FVRTailSoundClass tailClass = FVRTailSoundClass.Tiny;
            if (__instance.IsSuppressed())
            {
                __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_Suppressed, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_Suppressed.VolumeRange, __instance.AudioClipSet.Shots_Suppressed.PitchRange);
                if (__instance.IsHeld)
                {
                    __instance.m_hand.ForceTubeKick(__instance.AudioClipSet.FTP.Kick_Shot);
                    __instance.m_hand.ForceTubeRumble(__instance.AudioClipSet.FTP.Rumble_Shot_Intensity, __instance.AudioClipSet.FTP.Rumble_Shot_Duration);
                }
                if (__instance.AudioClipSet.UsesTail_Suppressed)
                {
                    tailClass = round.TailClassSuppressed;
                    AudioEvent tailSet = SM.GetTailSet(tailClass, env);
                    __instance.m_pool_tail.PlayDelayedClip(delay, tailSet, pos, tailSet.VolumeRange * globalLoudnessMultiplier, __instance.AudioClipSet.TailPitchMod_Suppressed * tailSet.PitchRange.x);
                }
            }
            else if (__instance.AudioClipSet.UsesLowPressureSet)
            {
                if (round.IsHighPressure)
                {
                    __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_Main, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_Main.VolumeRange, __instance.AudioClipSet.Shots_Main.PitchRange);
                    if (__instance.AudioClipSet.UsesTail_Main)
                    {
                        tailClass = round.TailClass;
                        AudioEvent tailSet2 = SM.GetTailSet(tailClass, env);
                        __instance.m_pool_tail.PlayDelayedClip(delay, tailSet2, pos, tailSet2.VolumeRange * globalLoudnessMultiplier, __instance.AudioClipSet.TailPitchMod_Main * tailSet2.PitchRange.x);
                    }
                }
                else
                {
                    __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_LowPressure, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_LowPressure.VolumeRange, __instance.AudioClipSet.Shots_LowPressure.PitchRange);
                    if (__instance.AudioClipSet.UsesTail_Main)
                    {
                        tailClass = round.TailClass;
                        AudioEvent tailSet3 = SM.GetTailSet(round.TailClass, env);
                        __instance.m_pool_tail.PlayDelayedClip(delay, tailSet3, pos, tailSet3.VolumeRange * globalLoudnessMultiplier, __instance.AudioClipSet.TailPitchMod_LowPressure * tailSet3.PitchRange.x);
                    }
                }
            }
            else
            {
                __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_Main, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_Main.VolumeRange, __instance.AudioClipSet.Shots_Main.PitchRange);
                if (__instance.AudioClipSet.UsesTail_Main)
                {
                    tailClass = round.TailClass;
                    AudioEvent tailSet4 = SM.GetTailSet(round.TailClass, env);
                    __instance.m_pool_tail.PlayDelayedClip(delay, tailSet4, pos, tailSet4.VolumeRange * globalLoudnessMultiplier, __instance.AudioClipSet.TailPitchMod_Main * tailSet4.PitchRange.x);
                }
            }
            float soundTravelDistanceMultByEnvironment = SM.GetSoundTravelDistanceMultByEnvironment(env);
            if (__instance.IsSuppressed())
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Suppressed, __instance.AudioClipSet.Loudness_Suppressed * soundTravelDistanceMultByEnvironment * 0.5f * globalLoudnessMultiplier, pos, IFF);
            }
            else if (__instance.AudioClipSet.UsesLowPressureSet && !round.IsHighPressure)
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Primary * 0.6f, __instance.AudioClipSet.Loudness_Primary * 0.6f * soundTravelDistanceMultByEnvironment * globalLoudnessMultiplier, pos, IFF);
            }
            else
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Primary, __instance.AudioClipSet.Loudness_Primary * soundTravelDistanceMultByEnvironment * globalLoudnessMultiplier, pos, IFF);
            }
            if (!__instance.IsSuppressed())
            {
                __instance.SceneSettings.PingReceivers(__instance.MuzzlePos.position);
            }
            __instance.RattleSuppresor();
            for (int i = 0; i < __instance.MuzzleDevices.Count; i++)
            {
                __instance.MuzzleDevices[i].OnShot(__instance, tailClass);
            }

            // Do distant shot audio if necessary
            float maxDist = 1000;
            if (__instance.IsSuppressed())
            {
                maxDist /= 6;
            }
            if ((int)env >= 2 && (int)env <= 17) // Inside
            {
                maxDist /= 3;
            }

            if (dist > 75 && dist < maxDist)
            {
                SM.PlayCoreSoundDelayedOverrides(FVRPooledAudioType.NPCShotFarDistant, Mod.distantShotSets[env], __instance.transform.position, Mod.distantShotSets[env].VolumeRange, Mod.distantShotSets[env].PitchRange, delay);
            }

            return false;
        }

        // Patches PlayAudioGunShot(Bool) to adapt to MP
        static bool PlayAudioGunShotBoolPrefix(FVRFireArm __instance, bool IsHighPressure, FVRTailSoundClass TailClass, FVRTailSoundClass TailClassSuppressed)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // Get actual environment
            FVRSoundEnvironment env = SM.GetSoundEnvironment(__instance.transform.position);

            // Get actual IFF
            int IFF = GM.CurrentPlayerBody.GetPlayerIFF();
            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
            {
                IFF = GameManager.players[trackedItem.data.controller].IFF;
            }

            // Get distance and delay
            Vector3 pos = __instance.transform.position;
            float dist = Vector3.Distance(pos, GM.CurrentPlayerBody.Head.position);
            float delay = dist / 343f;

            // Do original but using delay
            FVRTailSoundClass tailClass = FVRTailSoundClass.Tiny;
            if (__instance.IsSuppressed())
            {
                __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_Suppressed, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_Suppressed.VolumeRange, __instance.AudioClipSet.Shots_Suppressed.PitchRange);
                if (__instance.IsHeld)
                {
                    __instance.m_hand.ForceTubeKick(__instance.AudioClipSet.FTP.Kick_Shot);
                    __instance.m_hand.ForceTubeRumble(__instance.AudioClipSet.FTP.Rumble_Shot_Intensity, __instance.AudioClipSet.FTP.Rumble_Shot_Duration);
                }
                if (__instance.AudioClipSet.UsesTail_Suppressed)
                {
                    tailClass = TailClassSuppressed;
                    AudioEvent tailSet = SM.GetTailSet(TailClassSuppressed, env);
                    __instance.m_pool_tail.PlayDelayedClip(delay, tailSet, pos, tailSet.VolumeRange, __instance.AudioClipSet.TailPitchMod_Suppressed * tailSet.PitchRange.x);
                }
            }
            else
            {
                float num = 1f;
                if (__instance.IsBraked())
                {
                    num = 0.92f;
                }
                if (IsHighPressure)
                {
                    __instance.PlayAudioEvent(FirearmAudioEventType.Shots_Main, num);
                    if (__instance.AudioClipSet.UsesTail_Main)
                    {
                        tailClass = TailClass;
                        AudioEvent tailSet2 = SM.GetTailSet(TailClass, env);
                        __instance.m_pool_tail.PlayDelayedClip(delay, tailSet2, pos, tailSet2.VolumeRange, __instance.AudioClipSet.TailPitchMod_Main * tailSet2.PitchRange.x * num);
                    }
                }
                else
                {
                    __instance.m_pool_shot.PlayDelayedClip(delay, __instance.AudioClipSet.Shots_LowPressure, __instance.GetMuzzle().position, __instance.AudioClipSet.Shots_LowPressure.VolumeRange, __instance.AudioClipSet.Shots_LowPressure.PitchRange * num);
                    if (__instance.AudioClipSet.UsesTail_Main)
                    {
                        tailClass = TailClass;
                        AudioEvent tailSet3 = SM.GetTailSet(TailClass, env);
                        __instance.m_pool_tail.PlayDelayedClip(delay, tailSet3, pos, tailSet3.VolumeRange, __instance.AudioClipSet.TailPitchMod_LowPressure * tailSet3.PitchRange.x * num);
                    }
                }
            }
            float soundTravelDistanceMultByEnvironment = SM.GetSoundTravelDistanceMultByEnvironment(env);
            if (__instance.IsSuppressed())
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Suppressed, __instance.AudioClipSet.Loudness_Suppressed * soundTravelDistanceMultByEnvironment * 0.4f, pos, IFF);
            }
            else if (__instance.AudioClipSet.UsesLowPressureSet && !IsHighPressure)
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Primary * 0.6f, __instance.AudioClipSet.Loudness_Primary * 0.6f * soundTravelDistanceMultByEnvironment, pos, IFF);
            }
            else
            {
                GM.CurrentSceneSettings.OnPerceiveableSound(__instance.AudioClipSet.Loudness_Primary, __instance.AudioClipSet.Loudness_Primary * soundTravelDistanceMultByEnvironment, pos, IFF);
            }
            if (!__instance.IsSuppressed())
            {
                __instance.SceneSettings.PingReceivers(__instance.MuzzlePos.position);
            }
            __instance.RattleSuppresor();
            for (int i = 0; i < __instance.MuzzleDevices.Count; i++)
            {
                __instance.MuzzleDevices[i].OnShot(__instance, tailClass);
            }

            // Do distant shot audio if necessary
            float maxDist = 1000;
            if (__instance.IsSuppressed())
            {
                maxDist /= 6;
            }
            if ((int)env >= 2 && (int)env <= 17) // Inside
            {
                maxDist /= 3;
            }

            if (dist > 75 && dist < maxDist)
            {
                SM.PlayCoreSoundDelayedOverrides(FVRPooledAudioType.NPCShotFarDistant, Mod.distantShotSets[env], __instance.transform.position, Mod.distantShotSets[env].VolumeRange, Mod.distantShotSets[env].PitchRange, delay);
            }

            return false;
        }
    }

    // Patches SteelPopTarget
    class SteelPopTargetPatch
    {
        // Postfixes start to set joint rots correctly if got data from controller
        static void StartPostfix(SteelPopTarget __instance)
        {
            if (Mod.managerObject != null)
            {
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(__instance, out trackedItem) ? trackedItem : __instance.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
                {
                    for (int i = 0; i < __instance.Joints.Count; ++i)
                    {
                        __instance.Joints[i].transform.localEulerAngles = new Vector3(BitConverter.ToSingle(trackedItem.itemData.data, i * 12 + 1), BitConverter.ToSingle(trackedItem.itemData.data, i * 12 + 5), BitConverter.ToSingle(trackedItem.itemData.data, i * 12 + 9));
                    }
                }
            }
        }
    }

    // Patches FlameThrower
    class FlameThrowerPatch
    {
        // Patches UpdateControls to prevent on non controller
        static bool UpdateControlsPrefix(FlameThrower __instance)
        {
            if(Mod.managerObject != null && __instance.BeltBoxMountPos != null && int.TryParse(__instance.BeltBoxMountPos.name, out int parsed))
            {
                return TrackedObject.trackedReferences.Length <= parsed
                       || TrackedObject.trackedReferences[parsed] == null
                       || TrackedObject.trackedReferences[parsed].data.controller == GameManager.ID;
            }

            return true;
        }
    }

    // Patches AR15HandleSightFlipper
    class AR15SightFlipperPatch
    {
        // TODO: Future: Tracking only the event and not storing the state in TrackedItem means that if a sight is flipped
        //               and then another player instantiates the sight, it will not be flippied, because no data they have
        //               dictates it should be flipped. Idea would be that upon InitItemType
        //               of specific types that may have this flipper on them, build an array of all ComponentsInChildren<Flipper>
        //               and their current state. This way we could refer to them by index and the item could init itself with its
        //               flipper's set properly. Do the same for Raiser
        // Patches SimpleInteraction to track event
        static void InteractPostfix(AR15HandleSightFlipper __instance)
        {
            if(Mod.managerObject != null)
            {
                // Check if this flipper has a TrackedObject above it
                Transform t = __instance.transform;
                TrackedObject trackedObject = null;
                while(t != null)
                {
                    trackedObject = t.GetComponent<TrackedObject>();
                    if(trackedObject != null)
                    {
                        break;
                    }
                    else
                    {
                        t = t.parent;
                    }
                }

                if(trackedObject != null)
                {
                    int index = 0;
                    AR15HandleSightFlipper[] flippers = trackedObject.GetComponentsInChildren<AR15HandleSightFlipper>();
                    if(flippers != null && flippers.Length != 0)
                    {
                        if (flippers.Length > 1)
                        {
                            for(int i=0; i < flippers.Length; ++i)
                            {
                                if (flippers[i] == __instance)
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        // else, length is 1,, index will remain to default of 0

                        if (ThreadManager.host)
                        {
                            ServerSend.SightFlipperState(trackedObject.data.trackedID, index, __instance.m_isLargeAperture);
                        }
                        else if(trackedObject.data.trackedID != -1)
                        {
                            ClientSend.SightFlipperState(trackedObject.data.trackedID, index, __instance.m_isLargeAperture);
                        }
                    }
                }
            }
        }
    }

    // Patches AR15HandleSightRaiser
    class AR15SightRaiserPatch
    {
        // Patches SimpleInteraction to track event
        static void InteractPostfix(AR15HandleSightRaiser __instance)
        {
            if(Mod.managerObject != null)
            {
                // Check if this raiser has a TrackedObject above it
                Transform t = __instance.transform;
                TrackedObject trackedObject = null;
                while(t != null)
                {
                    trackedObject = t.GetComponent<TrackedObject>();
                    if(trackedObject != null)
                    {
                        break;
                    }
                    else
                    {
                        t = t.parent;
                    }
                }

                if(trackedObject != null)
                {
                    int index = 0;
                    AR15HandleSightRaiser[] raisers = trackedObject.GetComponentsInChildren<AR15HandleSightRaiser>();
                    if(raisers != null && raisers.Length != 0)
                    {
                        if (raisers.Length > 1)
                        {
                            for(int i=0; i < raisers.Length; ++i)
                            {
                                if (raisers[i] == __instance)
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        // else, length is 1,, index will remain to default of 0

                        if (ThreadManager.host)
                        {
                            ServerSend.SightRaiserState(trackedObject.data.trackedID, index, __instance.height);
                        }
                        else if (trackedObject.data.trackedID != -1)
                        {
                            ClientSend.SightRaiserState(trackedObject.data.trackedID, index, __instance.height);
                        }
                    }
                }
            }
        }
    }

    // Patches wwGatlingGun
    class GatlingGunPatch
    {
        // Patches FireShot to track event
        static void FireShotPostfix(wwGatlingGun __instance)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            TrackedGatlingGun trackedGatlingGun = GameManager.trackedGatlingGunByGatlingGun.TryGetValue(__instance, out trackedGatlingGun) ? trackedGatlingGun : __instance.GetComponent<TrackedGatlingGun>();
            if(trackedGatlingGun != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.GatlingGunFire(trackedGatlingGun.data.trackedID, __instance.MuzzlePos.position, __instance.MuzzlePos.rotation, __instance.MuzzlePos.forward);
                }
                else
                {
                    ClientSend.GatlingGunFire(trackedGatlingGun.data.trackedID, __instance.MuzzlePos.position, __instance.MuzzlePos.rotation, __instance.MuzzlePos.forward);
                }
            }
        }
    }

    // Patches Brut_GasCuboid
    class GasCuboidPatch
    {
        public static int generateGoutSkip = 0;
        public static int explodeSkip = 0;
        public static bool inExplode;
        public static int shatterSkip = 0;

        // Patches GenerateGout to track event
        static void GenerateGoutPrefix(Brut_GasCuboid __instance, Vector3 point, Vector3 normal)
        {
            if(generateGoutSkip > 0 || Mod.managerObject == null || __instance.hasGeneratedGoutYet || __instance.m_isDestroyed || __instance.m_fuel <= 0)
            {
                return;
            }

            if(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1] != null)
            {
                if(int.TryParse(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1].name, out int refIndex))
                {
                    TrackedItem trackedGasCuboid = (TrackedItem)TrackedObject.trackedReferences[refIndex];
                    if (trackedGasCuboid != null && trackedGasCuboid.itemData.additionalData[0] < 255)
                    {
                        byte[] temp = trackedGasCuboid.itemData.additionalData;
                        trackedGasCuboid.itemData.additionalData = new byte[temp.Length + 24];
                        for (int i = 0; i < temp.Length; ++i)
                        {
                            trackedGasCuboid.itemData.additionalData[i] = temp[i];
                        }
                        ++trackedGasCuboid.itemData.additionalData[1];

                        if (ThreadManager.host)
                        {
                            ServerSend.GasCuboidGout(trackedGasCuboid.itemData.trackedID, point, normal);
                        }
                        else if (trackedGasCuboid.itemData.trackedID != -1)
                        {
                            ClientSend.GasCuboidGout(trackedGasCuboid.itemData.trackedID, point, normal);
                        }
                        else
                        {
                            if (TrackedItem.unknownGasCuboidGout.TryGetValue(trackedGasCuboid.data.localWaitingIndex, out List<KeyValuePair<Vector3, Vector3>> current))
                            {
                                current.Add(new KeyValuePair<Vector3, Vector3>(point, normal));
                            }
                            else
                            {
                                TrackedItem.unknownGasCuboidGout.Add(trackedGasCuboid.data.localWaitingIndex, new List<KeyValuePair<Vector3, Vector3>>() { new KeyValuePair<Vector3, Vector3>(point, normal) });
                            }
                        }
                    }
                }
            }
        }

        // Patches DamageHandle to track event
        static void DamageHandlePrefix(Brut_GasCuboid __instance)
        {
            if(Mod.managerObject == null || __instance.m_isHandleBrokenOff || __instance.m_fuel <= 0)
            {
                return;
            }

            if (__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1].name, out int refIndex))
                {
                    TrackedItem trackedGasCuboid = (TrackedItem)TrackedObject.trackedReferences[refIndex];
                    if (trackedGasCuboid != null)
                    {
                        trackedGasCuboid.itemData.additionalData[0] = 1;

                        if (ThreadManager.host)
                        {
                            ServerSend.GasCuboidDamageHandle(trackedGasCuboid.itemData.trackedID);
                        }
                        else if (trackedGasCuboid.itemData.trackedID != -1)
                        {
                            ClientSend.GasCuboidDamageHandle(trackedGasCuboid.itemData.trackedID);
                        }
                        else
                        {
                            TrackedItem.unknownGasCuboidDamageHandle.Add(trackedGasCuboid.data.localWaitingIndex);
                        }
                    }
                }
            }
        }

        // Patches Update and FixedUpdate to prevent for non controllers
        static bool UpdatePrefix(Brut_GasCuboid __instance)
        {
            if(Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1].name, out int refIndex))
                {
                    TrackedItem trackedGasCuboid = (TrackedItem)TrackedObject.trackedReferences[refIndex];
                    if (trackedGasCuboid != null)
                    {
                        return trackedGasCuboid.itemData.controller == GameManager.ID;
                    }
                }
            }

            return true;
        }

        // Patches Explode to track event
        static void ExplodePrefix(Brut_GasCuboid __instance, Vector3 point, Vector3 dir, bool isBig)
        {
            inExplode = true;

            if (explodeSkip > 0 || Mod.managerObject == null || __instance.m_isDestroyed)
            {
                return;
            }

            if (__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1].name, out int refIndex))
                {
                    TrackedItem trackedGasCuboid = (TrackedItem)TrackedObject.trackedReferences[refIndex];
                    if (trackedGasCuboid != null)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.GasCuboidExplode(trackedGasCuboid.itemData.trackedID, point, dir, isBig);
                        }
                        else
                        {
                            ClientSend.GasCuboidExplode(trackedGasCuboid.itemData.trackedID, point, dir, isBig);
                        }
                    }
                }
            }
        }

        static void ExplodePostfix()
        {
            inExplode = false;
        }

        // Patches Explode to track event
        static void ShatterPrefix(Brut_GasCuboid __instance, Vector3 point, Vector3 dir)
        {
            if (shatterSkip > 0 || inExplode || Mod.managerObject == null || __instance.m_isDestroyed)
            {
                return;
            }

            if (__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplodePoints[__instance.SpawnOnSplodePoints.Count - 1].name, out int refIndex))
                {
                    TrackedItem trackedGasCuboid = (TrackedItem)TrackedObject.trackedReferences[refIndex];
                    if (trackedGasCuboid != null)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.GasCuboidShatter(trackedGasCuboid.itemData.trackedID, point, dir);
                        }
                        else
                        {
                            ClientSend.GasCuboidShatter(trackedGasCuboid.itemData.trackedID, point, dir);
                        }
                    }
                }
            }
        }
    }

    // Patches Construct_Floater
    class FloaterPatch
    {
        public static bool beginExplodingOverride;
        public static int explodeSkip;

        // Patches Update to prevent unless exploding
        static bool UpdatePrefix(Construct_Floater __instance)
        {
            if (Mod.managerObject == null || __instance.m_isExploding)
            {
                return true;
            }

            if (__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].name, out int refIndex))
                {
                    // Note: If we got here it is because we are not exploding, meaning that we don't want to continue no matter what if we are not controller
                    TrackedFloater trackedFloater = TrackedObject.trackedReferences[refIndex] as TrackedFloater;
                    return trackedFloater == null || trackedFloater.data.controller == GameManager.ID;
                }
            }

            return true;
        }

        // Patches FixedUpdate to prevent unless exploding
        static bool FixedUpdatePrefix(Construct_Floater __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].name, out int refIndex))
                {
                    TrackedFloater trackedFloater = TrackedObject.trackedReferences[refIndex] as TrackedFloater;
                    return trackedFloater == null || trackedFloater.data.controller == GameManager.ID;
                }
            }

            return true;
        }

        // Patches BeginExploding to track event
        static bool BeginExplodingPrefix(Construct_Floater __instance)
        {
            if (Mod.managerObject == null || __instance.m_isExploding)
            {
                return true;
            }

            if (__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].name, out int refIndex))
                {
                    TrackedFloater trackedFloater = TrackedObject.trackedReferences[refIndex] as TrackedFloater;
                    if (trackedFloater != null)
                    {
                        bool control = trackedFloater.data.controller == GameManager.ID;

                        if (!beginExplodingOverride)
                        {
                            if (ThreadManager.host)
                            {
                                ServerSend.FloaterBeginExploding(trackedFloater.data.trackedID, control);
                            }
                            else if (trackedFloater.data.trackedID != -1)
                            {
                                ClientSend.FloaterBeginExploding(trackedFloater.data.trackedID, control);
                            }
                            else // Note that this is only possible if we are the controller
                            {
                                TrackedFloater.unknownFloaterBeginExploding.Add(trackedFloater.data.localWaitingIndex);
                            }
                        }

                        return beginExplodingOverride || control;
                    }
                }
            }

            return true;
        }

        // Patches BeginDefusing to track event
        static bool BeginDefusingPrefix(Construct_Floater __instance)
        {
            if (Mod.managerObject == null || __instance.m_isExploding)
            {
                return true;
            }

            if (__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1] != null)
            {
                if (int.TryParse(__instance.SpawnOnSplode[__instance.SpawnOnSplode.Count - 1].name, out int refIndex))
                {
                    TrackedFloater trackedFloater = TrackedObject.trackedReferences[refIndex] as TrackedFloater;
                    if (trackedFloater != null)
                    {
                        bool control = trackedFloater.data.controller == GameManager.ID;

                        if (!beginExplodingOverride)
                        {
                            if (ThreadManager.host)
                            {
                                ServerSend.FloaterBeginDefusing(trackedFloater.data.trackedID, control);
                            }
                            else if (trackedFloater.data.trackedID != -1)
                            {
                                ClientSend.FloaterBeginDefusing(trackedFloater.data.trackedID, control);
                            }
                            else // Note that this is only possible if we are the controller
                            {
                                TrackedFloater.unknownFloaterBeginDefusing.Add(trackedFloater.data.localWaitingIndex);
                            }
                        }

                        return beginExplodingOverride || control;
                    }
                }
            }

            return true;
        }

        // Patches Explode to track event and prevent instantiation of ref object
        static bool ExplodePrefix(Construct_Floater __instance)
        {
            TrackedFloater trackedFloater = GameManager.trackedFloaterByFloater.TryGetValue(__instance, out trackedFloater) ? trackedFloater : null;
            if (trackedFloater != null && trackedFloater.data.controller == GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.FloaterExplode(trackedFloater.data.trackedID, __instance.isExplosionDefuse);
                }
                else if (trackedFloater.data.trackedID != -1)
                {
                    ClientSend.FloaterExplode(trackedFloater.data.trackedID, __instance.isExplosionDefuse);
                }
                else // Note that this is only possible if we are the controller
                {
                    TrackedFloater.unknownFloaterBeginExploding.Remove(trackedFloater.data.localWaitingIndex);
                    TrackedFloater.unknownFloaterBeginDefusing.Remove(trackedFloater.data.localWaitingIndex);
                    TrackedFloater.unknownFloaterExplode.Add(trackedFloater.data.localWaitingIndex, __instance.isExplosionDefuse);
                }
            }

            if (__instance.m_isExploded)
            {
                return false;
            }
            __instance.m_isExploded = true;
            if (__instance.E != null)
            {
                __instance.E.AIEventReceiveEvent -= __instance.EventReceive;
            }
            if (__instance.E != null)
            {
                UnityEngine.Object.Destroy(__instance.E.gameObject);
            }
            ParticleSystem.EmissionModule tempEM = __instance.PSystem.emission;
            tempEM.rateOverTime = 0;
            if (__instance.isExplosionDefuse)
            {
                for (int i = 0; i < __instance.SpawnOnDefuse.Count; i++)
                {
                    UnityEngine.Object.Instantiate<GameObject>(__instance.SpawnOnDefuse[i], __instance.transform.position, __instance.transform.rotation);
                }
                for (int j = 0; j < __instance.Shards.Count; j++)
                {
                    __instance.Shards[j].gameObject.SetActive(true);
                    __instance.Shards[j].velocity = __instance.RB.velocity;
                    __instance.Shards[j].angularVelocity = __instance.RB.angularVelocity;
                    __instance.Shards[j].AddExplosionForce(UnityEngine.Random.Range(__instance.ShardDefuseMag.x, __instance.ShardDefuseMag.y) * UnityEngine.Random.Range(0.1f, 0.5f), __instance.transform.position, 1f, 0.1f, ForceMode.VelocityChange);
                    __instance.Shards[j].AddTorque(UnityEngine.Random.onUnitSphere * 90f, ForceMode.VelocityChange);
                }
            }
            else
            {
                if (GM.TNH_Manager != null)
                {
                    GM.TNH_Manager.TakingBotKill();
                }
                // Here we don't iterate over the last element, which is our ref object
                for (int k = 0; k < __instance.SpawnOnSplode.Count - 1; k++)
                {
                    UnityEngine.Object.Instantiate<GameObject>(__instance.SpawnOnSplode[k], __instance.transform.position, __instance.transform.rotation);
                }
                for (int l = 0; l < __instance.Shards.Count; l++)
                {
                    __instance.Shards[l].gameObject.SetActive(true);
                    __instance.Shards[l].velocity = __instance.RB.velocity;
                    __instance.Shards[l].angularVelocity = __instance.RB.angularVelocity;
                    __instance.Shards[l].AddExplosionForce(UnityEngine.Random.Range(__instance.ShardExplodeMag.x, __instance.ShardExplodeMag.y) * UnityEngine.Random.Range(0.1f, 0.5f), __instance.transform.position, 1f, 0.1f, ForceMode.VelocityChange);
                    __instance.Shards[l].AddTorque(UnityEngine.Random.onUnitSphere * 90f, ForceMode.VelocityChange);
                }
            }
            __instance.C.enabled = false;
            __instance.m_isExploding = false;
            __instance.MainGO.SetActive(false);
            UnityEngine.Object.Destroy(__instance.RB);

            return false;
        }
    }

    // Patches Construct_Iris
    class IrisPatch
    {
        public static int stateSkip;

        // Patches Update to prevent for non controllers
        static bool UpdatePrefix(Construct_Iris __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedIris trackedIris = TrackedObject.trackedReferences[__instance.BParams[__instance.BParams.Count - 1].Pen] as TrackedIris;
            if (trackedIris != null)
            {
                if (trackedIris.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    if(__instance.IState != Construct_Iris.IrisState.Dead && __instance.m_isShotEngaged)
                    {
                        __instance.UpdateLaser();
                    }
                    return false;
                }
            }

            return true;
        }

        // Patches FixedUpdate to prevent for non controllers
        static bool FixedUpdatePrefix(Construct_Iris __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedIris trackedIris = TrackedObject.trackedReferences[__instance.BParams[__instance.BParams.Count - 1].Pen] as TrackedIris;
            if (trackedIris != null)
            {
                if (trackedIris.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        // Patches SetState to track event
        static bool SetStatePrefix(Construct_Iris __instance, Construct_Iris.IrisState s)
        {
            if (Mod.managerObject == null || stateSkip > 0)
            {
                return true;
            }

            TrackedIris trackedIris = TrackedObject.trackedReferences[__instance.BParams[__instance.BParams.Count - 1].Pen] as TrackedIris;
            if (trackedIris != null)
            {
                if(trackedIris.data.controller == GameManager.ID)
                {
                    trackedIris.irisData.state = s;

                    if (ThreadManager.host)
                    {
                        ServerSend.IrisSetState(trackedIris.data.trackedID, s);
                    }
                    else if (trackedIris.data.trackedID != -1)
                    {
                        ClientSend.IrisSetState(trackedIris.data.trackedID, s);
                    }
                    else
                    {
                        TrackedIris.unknownIrisSetState.Add(trackedIris.data.localWaitingIndex, s);
                    }

                    return true;
                }

                return false;
            }

            return true;
        }
    }

    // Patches BrutBlockSystem
    class BrutBlockSystemPatch
    {
        public static int startSkip;

        // Patches Update to prevent for non controllers
        static bool UpdatePrefix(BrutBlockSystem __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.BlockPointUppers[__instance.BlockPointUppers.Count - 1] != null)
            {
                if (int.TryParse(__instance.BlockPointUppers[__instance.BlockPointUppers.Count - 1].name, out int refIndex))
                {
                    TrackedBrutBlockSystem trackedBrutBlockSystem = TrackedObject.trackedReferences[refIndex] as TrackedBrutBlockSystem;
                    if (trackedBrutBlockSystem != null)
                    {
                        return trackedBrutBlockSystem.data.controller == GameManager.ID;
                    }
                }
            }

            return true;
        }

        // Patches TryToStartBlock to track event
        static bool TryToStartBlockPrefix(BrutBlockSystem __instance)
        {
            if (Mod.managerObject == null || startSkip > 0)
            {
                return true;
            }

            if (__instance.BlockPointUppers[__instance.BlockPointUppers.Count - 1] != null)
            {
                if (int.TryParse(__instance.BlockPointUppers[__instance.BlockPointUppers.Count - 1].name, out int refIndex))
                {
                    TrackedBrutBlockSystem trackedBrutBlockSystem = TrackedObject.trackedReferences[refIndex] as TrackedBrutBlockSystem;
                    if (trackedBrutBlockSystem != null)
                    {
                        if (trackedBrutBlockSystem.data.controller == GameManager.ID)
                        {
                            if (ThreadManager.host)
                            {
                                ServerSend.BrutBlockSystemStart(trackedBrutBlockSystem.data.trackedID, __instance.isNextBlock0);
                            }
                            else if (trackedBrutBlockSystem.data.trackedID != -1)
                            {
                                ClientSend.BrutBlockSystemStart(trackedBrutBlockSystem.data.trackedID, __instance.isNextBlock0);
                            }

                            return true;
                        }

                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Patches Construct_Node
    class NodePatch
    {
        // Patches Init to prevent for non controllers 
        static bool InitPrefix(Construct_Node __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.Stems[__instance.Stems.Count - 1] != null)
            {
                if (int.TryParse(__instance.Stems[__instance.Stems.Count - 1].name, out int refIndex))
                {
                    TrackedNode trackedNode = TrackedObject.trackedReferences[refIndex] as TrackedNode;
                    if (trackedNode != null)
                    {
                        return trackedNode.data.controller == GameManager.ID;
                    }
                }
            }

            return true;
        }

        // Patches Init to collect data to send to non controllers 
        static void InitPostfix(Construct_Node __instance)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (__instance.Stems[__instance.Stems.Count - 1] != null)
            {
                if (int.TryParse(__instance.Stems[__instance.Stems.Count - 1].name, out int refIndex))
                {
                    TrackedNode trackedNode = TrackedObject.trackedReferences[refIndex] as TrackedNode;
                    if (trackedNode != null && trackedNode.data.controller == GameManager.ID)
                    {
                        trackedNode.nodeData.points = trackedNode.physicalNode.Points;
                        trackedNode.nodeData.ups = trackedNode.physicalNode.Ups;

                        if (ThreadManager.host)
                        {
                            ServerSend.NodeInit(trackedNode.data.trackedID, __instance.Points, __instance.Ups);
                        }
                        else if (trackedNode.data.trackedID != -1)
                        {
                            ClientSend.NodeInit(trackedNode.data.trackedID, __instance.Points, __instance.Ups);
                        }
                        else
                        {
                            if (!TrackedNode.unknownInit.Contains(trackedNode.data.localWaitingIndex))
                            {
                                TrackedNode.unknownInit.Add(trackedNode.data.localWaitingIndex);
                            }
                        }
                    }
                }
            }
        }

        // Patches FVRUpdate 
        static bool UpdatePrefix(Construct_Node __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.Stems[__instance.Stems.Count - 1] != null)
            {
                if (int.TryParse(__instance.Stems[__instance.Stems.Count - 1].name, out int refIndex))
                {
                    TrackedNode trackedNode = TrackedObject.trackedReferences[refIndex] as TrackedNode;
                    if (trackedNode != null)
                    {
                        if (trackedNode.data.controller != GameManager.ID)
                        {
                            if (trackedNode.nodeData.underActiveControl || __instance.soundSilenceTimer < 3f)
                            {
                                if (!__instance.AudSource_Loop.isPlaying)
                                {
                                    __instance.AudSource_Loop.Play();
                                }
                                __instance.AudSource_Loop.volume = Mathf.Lerp(0f, 0.4f, __instance.RB.velocity.magnitude / 1f);
                                __instance.AudSource_Loop.pitch = Mathf.Lerp(0.85f, 1.15f, __instance.RB.velocity.magnitude / 1f);
                            }
                            else if (__instance.AudSource_Loop.isPlaying)
                            {
                                __instance.AudSource_Loop.Stop();
                            }

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Patches FVRFixedUpdate 
        static bool FixedUpdatePrefix(Construct_Node __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.Stems[__instance.Stems.Count - 1] != null)
            {
                if (int.TryParse(__instance.Stems[__instance.Stems.Count - 1].name, out int refIndex))
                {
                    TrackedNode trackedNode = TrackedObject.trackedReferences[refIndex] as TrackedNode;
                    if (trackedNode != null)
                    {
                        if (trackedNode.data.controller != GameManager.ID)
                        {
                            if (trackedNode.nodeData.underActiveControl)
                            {
                                if (__instance.damperRecoverTimer < 1f)
                                {
                                    __instance.damperRecoverTimer += Time.deltaTime;
                                }
                                else
                                {
                                    __instance.Joint.damper = 10f;
                                    __instance.RB.drag = 4f;
                                }
                                if (__instance.soundSilenceTimer < 3f)
                                {
                                    __instance.soundSilenceTimer += Time.deltaTime;
                                }
                            }
                            if (trackedNode.nodeData.underActiveControl || __instance.RB.velocity.magnitude > 2f)
                            {
                                __instance.PSystem.gameObject.SetActive(true);
                            }
                            else
                            {
                                __instance.PSystem.gameObject.SetActive(false);
                            }
                            __instance.UpdateCageStems();

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Patches FVRFixedUpdate to keep track of firing event
        static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To get correct pos considering potential override
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load node instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_2)); // Load velocity multiplier
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load node instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Construct_Node), "m_firingDir"))); // Load firing direction
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NodePatch), "Fire"))); // Call our Fire method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Fire"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("NodePatch Transpiler not applied!");
            }

            return instructionList;
        }

        public static void Fire(Construct_Node instance, float velMult, Vector3 firingDir)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (instance.Stems[instance.Stems.Count - 1] != null)
            {
                if (int.TryParse(instance.Stems[instance.Stems.Count - 1].name, out int refIndex))
                {
                    TrackedNode trackedNode = TrackedObject.trackedReferences[refIndex] as TrackedNode;
                    if (trackedNode != null && trackedNode.data.controller == GameManager.ID)
                    {
                        if (ThreadManager.host)
                        {
                            ServerSend.NodeFire(trackedNode.data.trackedID, velMult, firingDir);
                        }
                        else if (trackedNode.data.trackedID != -1)
                        {
                            ClientSend.NodeFire(trackedNode.data.trackedID, velMult, firingDir);
                        }
                    }
                }
            }
        }
    }

    // Patches Construct_Haze
    class HazePatch
    {
        // Patches Update 
        static bool UpdatePrefix(Construct_Haze __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.PSystem2 != null)
            {
                if (int.TryParse(__instance.PSystem2.name, out int refIndex))
                {
                    TrackedHaze trackedHaze = TrackedObject.trackedReferences[refIndex] as TrackedHaze;
                    if (trackedHaze != null)
                    {
                        if (trackedHaze.data.controller != GameManager.ID)
                        {
                            float num = Vector3.Distance(GM.CurrentPlayerBody.Head.position, __instance.transform.position);
                            if (num < 1f && GM.TNH_Manager != null)
                            {
                                GM.TNH_Manager.TeleportToRandomHoldPoint();
                            }
                            if (__instance.KEBattery > 0f)
                            {
                                __instance.KEBattery -= Time.deltaTime * __instance.KEBatteryDecaySpeed;
                                ParticleSystem.MainModule temp = __instance.PSystem.main;
                                temp.startSize = 0.1f;
                            }
                            else
                            {
                                ParticleSystem.MainModule temp = __instance.PSystem.main;
                                temp.startSize = 0.7f;
                                if (!__instance.DamSphere.enabled)
                                {
                                    __instance.DamSphere.enabled = true;
                                }
                            }
                            float t = Mathf.InverseLerp(__instance.PSystemEnergyRange.x, __instance.PSystemEnergyRange.y, __instance.KEBattery);
                            float radius = Mathf.Lerp(__instance.PSystemEmitRange.x, __instance.PSystemEmitRange.y, t);
                            __instance.sh.radius = radius;
                            Vector3 vector = GM.CurrentPlayerBody.Head.position - __instance.transform.position;
                            if (vector.magnitude > __instance.MaxSoundDist)
                            {
                                if (__instance.AudSource_Haze.isPlaying)
                                {
                                    __instance.AudSource_Haze.Stop();
                                    __instance.curLPFreq = 100f;
                                    __instance.tarLPFreq = 100f;
                                }
                            }
                            else
                            {
                                if (Physics.Linecast(GM.CurrentPlayerBody.Head.position, __instance.transform.position, __instance.LM_Env, QueryTriggerInteraction.Ignore))
                                {
                                    __instance.AudSource_Haze.volume = __instance.BaseVolume * __instance.OcclusionVolumeCurve.Evaluate(vector.magnitude);
                                    __instance.tarLPFreq = __instance.OcclusionFactorCurve.Evaluate(vector.magnitude);
                                }
                                else
                                {
                                    __instance.AudSource_Haze.volume = __instance.BaseVolume;
                                    __instance.tarLPFreq = 22000f;
                                }
                                __instance.curLPFreq = Mathf.MoveTowards(__instance.curLPFreq, __instance.tarLPFreq, Time.deltaTime * 50000f);
                                __instance.AudLowPass.cutoffFrequency = __instance.curLPFreq;
                                if (!__instance.AudSource_Haze.isPlaying)
                                {
                                    __instance.AudSource_Haze.Play();
                                }
                            }

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Patches FixedUpdate 
        static bool FixedUpdatePrefix(Construct_Haze __instance)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            if (__instance.PSystem2 != null)
            {
                if (int.TryParse(__instance.PSystem2.name, out int refIndex))
                {
                    TrackedHaze trackedHaze = TrackedObject.trackedReferences[refIndex] as TrackedHaze;
                    if (trackedHaze != null)
                    {
                        return trackedHaze.data.controller == GameManager.ID;
                    }
                }
            }

            return true;
        }
    }
}
