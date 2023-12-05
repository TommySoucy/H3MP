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
    public class DamagePatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // EncryptionSubDamagePatch
            MethodInfo encryptionSubDamagePatchOriginal = typeof(TNH_EncryptionTarget_SubTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionSubDamagePatchPrefix = typeof(EncryptionSubDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionSubDamagePatchOriginal, harmony, true);
            harmony.Patch(encryptionSubDamagePatchOriginal, new HarmonyMethod(encryptionSubDamagePatchPrefix));

            ++patchIndex; // 1

            // EncryptionDamageablePatch
            MethodInfo encryptionDamageablePatchFixedUpdateOriginal = typeof(TNH_EncryptionTarget).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo encryptionDamageablePatchFixedUpdateTranspiler = typeof(EncryptionDamageablePatch).GetMethod("FixedUpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionDamageablePatchFixedUpdateOriginal, harmony, true);
            try 
            { 
                harmony.Patch(encryptionDamageablePatchFixedUpdateOriginal, null, null, new HarmonyMethod(encryptionDamageablePatchFixedUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.EncryptionDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 2

            // UberShatterableShatterPatch
            MethodInfo uberShatterableShatterPatchOriginal = typeof(UberShatterable).GetMethod("Shatter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo uberShatterableShatterPatchPatchPrefix = typeof(UberShatterableShatterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(uberShatterableShatterPatchOriginal, harmony, false);
            harmony.Patch(uberShatterableShatterPatchOriginal, new HarmonyMethod(uberShatterableShatterPatchPatchPrefix));

            ++patchIndex; // 3

            // EncryptionDamagePatch
            MethodInfo encryptionDamagePatchOriginal = typeof(TNH_EncryptionTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo encryptionDamagePatchPrefix = typeof(EncryptionDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo encryptionDamagePatchPostfix = typeof(EncryptionDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(encryptionDamagePatchOriginal, harmony, true);
            harmony.Patch(encryptionDamagePatchOriginal, new HarmonyMethod(encryptionDamagePatchPrefix), new HarmonyMethod(encryptionDamagePatchPostfix));

            ++patchIndex; // 4

            // SosigWeaponDamagePatch
            MethodInfo sosigWeaponDamagePatchOriginal = typeof(SosigWeapon).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWeaponDamagePatchPrefix = typeof(SosigWeaponDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigWeaponDamagePatchOriginal, harmony, false);
            harmony.Patch(sosigWeaponDamagePatchOriginal, new HarmonyMethod(sosigWeaponDamagePatchPrefix));

            ++patchIndex; // 5

            // RemoteMissileDamagePatch
            MethodInfo remoteMissileDamagePatchOriginal = typeof(RemoteMissile).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo remoteMissileDamagePatchPrefix = typeof(RemoteMissileDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(remoteMissileDamagePatchOriginal, harmony, false);
            harmony.Patch(remoteMissileDamagePatchOriginal, new HarmonyMethod(remoteMissileDamagePatchPrefix));

            ++patchIndex; // 6

            // StingerMissileDamagePatch
            MethodInfo stingerMissileDamagePatchOriginal = typeof(StingerMissile).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo stingerMissileDamagePatchPrefix = typeof(StingerMissileDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(stingerMissileDamagePatchOriginal, harmony, false);
            harmony.Patch(stingerMissileDamagePatchOriginal, new HarmonyMethod(stingerMissileDamagePatchPrefix));

            ++patchIndex; // 7

            // ProjectileFirePatch
            MethodInfo projectileFirePatchOriginal = typeof(BallisticProjectile).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float), typeof(Vector3), typeof(FVRFireArm), typeof(bool) }, null);
            MethodInfo projectileFirePatchPostfix = typeof(ProjectileFirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo projectileFirePatchTranspiler = typeof(ProjectileFirePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(projectileFirePatchOriginal, harmony, true);
            try
            { 
                harmony.Patch(projectileFirePatchOriginal, new HarmonyMethod(projectileFirePatchPostfix), null, new HarmonyMethod(projectileFirePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.ProjectileFirePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 8

            // ProjectileDamageablePatch
            MethodInfo ballisticProjectileDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo ballisticProjectileDamageablePatchTranspiler = typeof(BallisticProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(ballisticProjectileDamageablePatchOriginal, harmony, true);
            try
            { 
                harmony.Patch(ballisticProjectileDamageablePatchOriginal, null, null, new HarmonyMethod(ballisticProjectileDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BallisticProjectileDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 9

            // SubMunitionsDamageablePatch
            MethodInfo subMunitionsDamageablePatchOriginal = typeof(BallisticProjectile).GetMethod("FireSubmunitions", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo subMunitionsDamageablePatchTranspiler = typeof(SubMunitionsDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(subMunitionsDamageablePatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(subMunitionsDamageablePatchOriginal, null, null, new HarmonyMethod(subMunitionsDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SubMunitionsDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 10

            // ExplosionDamageablePatch
            MethodInfo explosionDamageablePatchOriginal = typeof(Explosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo explosionDamageablePatchTranspiler = typeof(ExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(explosionDamageablePatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(explosionDamageablePatchOriginal, null, null, new HarmonyMethod(explosionDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.ExplosionDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 11

            // GrenadeExplosionDamageablePatch
            MethodInfo grenadeExplosionDamageablePatchOriginal = typeof(GrenadeExplosion).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeExplosionDamageablePatchTranspiler = typeof(GrenadeExplosionDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(grenadeExplosionDamageablePatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(grenadeExplosionDamageablePatchOriginal, null, null, new HarmonyMethod(grenadeExplosionDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.GrenadeExplosionDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 12

            // FlameThrowerDamageablePatch
            MethodInfo flameThrowerDamageablePatchOriginal = typeof(FlameThrower).GetMethod("AirBlast", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flameThrowerDamageablePatchTranspiler = typeof(FlameThrowerDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(flameThrowerDamageablePatchOriginal, harmony, false);
            try
            { 
                harmony.Patch(flameThrowerDamageablePatchOriginal, null, null, new HarmonyMethod(flameThrowerDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.FlameThrowerDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 13

            // GrenadeDamageablePatch
            MethodInfo grenadeDamageablePatchOriginal = typeof(FVRGrenade).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grenadeDamageablePatchTranspiler = typeof(GrenadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(grenadeDamageablePatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(grenadeDamageablePatchOriginal, null, null, new HarmonyMethod(grenadeDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.GrenadeDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 14

            // DemonadeDamageablePatch
            MethodInfo demonadeDamageablePatchOriginal = typeof(MF2_Demonade).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo demonadeDamageablePatchTranspiler = typeof(DemonadeDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(demonadeDamageablePatchOriginal, harmony, false);
            try 
            { 
                harmony.Patch(demonadeDamageablePatchOriginal, null, null, new HarmonyMethod(demonadeDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.DemonadeDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 15

            // SosigWeaponDamageablePatch
            MethodInfo sosigWeaponDamageablePatchOriginal = typeof(SosigWeapon).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("ExplosionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchCollisionOriginal = typeof(SosigWeapon).GetMethod("DoMeleeDamageInCollision", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchCollisionTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWeaponDamageablePatchUpdateOriginal = typeof(SosigWeapon).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWeaponDamageablePatchUpdateTranspiler = typeof(SosigWeaponDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigWeaponDamageablePatchOriginal, harmony, true);
            PatchController.Verify(sosigWeaponDamageablePatchCollisionOriginal, harmony, true);
            PatchController.Verify(sosigWeaponDamageablePatchUpdateOriginal, harmony, true);
            try 
            { 
                harmony.Patch(sosigWeaponDamageablePatchOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SosigWeaponDamageablePatch on sosigWeaponDamageablePatchOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(sosigWeaponDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SosigWeaponDamageablePatch on sosigWeaponDamageablePatchCollisionOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(sosigWeaponDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(sosigWeaponDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SosigWeaponDamageablePatch on sosigWeaponDamageablePatchUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 16

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

            PatchController.Verify(meleeParamsDamageablePatchStabOriginal, harmony, true);
            PatchController.Verify(meleeParamsDamageablePatchTearOriginal, harmony, true);
            PatchController.Verify(meleeParamsDamageablePatchUpdateOriginal, harmony, true);
            PatchController.Verify(meleeParamsDamageablePatchCollisionOriginal, harmony, true);
            try
            { 
                harmony.Patch(meleeParamsDamageablePatchStabOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchStabTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeleeParamsDamageablePatch on meleeParamsDamageablePatchStabOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(meleeParamsDamageablePatchTearOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchTearTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeleeParamsDamageablePatch on meleeParamsDamageablePatchTearOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(meleeParamsDamageablePatchUpdateOriginal, new HarmonyMethod(meleeParamsDamageablePatchUpdatePrefix), null, new HarmonyMethod(meleeParamsDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeleeParamsDamageablePatch on meleeParamsDamageablePatchUpdateOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try 
            { 
                harmony.Patch(meleeParamsDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeleeParamsDamageablePatch on meleeParamsDamageablePatchCollisionOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 17

            // AIMeleeDamageablePatch
            MethodInfo meleeParamsDamageablePatchFireOriginal = typeof(AIMeleeWeapon).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meleeParamsDamageablePatchFireTranspiler = typeof(AIMeleeDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(meleeParamsDamageablePatchFireOriginal, harmony, true);
            try
            { 
                harmony.Patch(meleeParamsDamageablePatchFireOriginal, null, null, new HarmonyMethod(meleeParamsDamageablePatchFireTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.AIMeleeDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 18

            // AutoMeaterBladeDamageablePatch
            MethodInfo autoMeaterBladeDamageablePatchCollisionOriginal = typeof(AutoMeaterBlade).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo autoMeaterBladeDamageablePatchCollisionTranspiler = typeof(AutoMeaterBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterBladeDamageablePatchCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(autoMeaterBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(autoMeaterBladeDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.AutoMeaterBladeDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 19

            // BangSnapDamageablePatch
            MethodInfo bangSnapDamageablePatchCollisionOriginal = typeof(BangSnap).GetMethod("OnCollisionEnter", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bangSnapDamageablePatchCollisionTranspiler = typeof(BangSnapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(bangSnapDamageablePatchCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(bangSnapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(bangSnapDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BangSnapDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 20

            // BearTrapDamageablePatch
            MethodInfo bearTrapDamageablePatchSnapOriginal = typeof(BearTrapInteractiblePiece).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo bearTrapDamageablePatchSnapTranspiler = typeof(BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(bearTrapDamageablePatchSnapOriginal, harmony, false);
            try 
            { 
                harmony.Patch(bearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(bearTrapDamageablePatchSnapTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BearTrapDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 21

            // ChainsawDamageablePatch
            MethodInfo chainsawDamageablePatchCollisionOriginal = typeof(Chainsaw).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo chainsawDamageablePatchCollisionTranspiler = typeof(ChainsawDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(chainsawDamageablePatchCollisionOriginal, harmony, false);
            try 
            { 
                harmony.Patch(chainsawDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(chainsawDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.ChainsawDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 22

            // DrillDamageablePatch
            MethodInfo drillDamageablePatchCollisionOriginal = typeof(Drill).GetMethod("OnCollisionStay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo drillDamageablePatchCollisionTranspiler = typeof(DrillDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(drillDamageablePatchCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(drillDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(drillDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.DrillDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 23

            // DropTrapDamageablePatch
            MethodInfo dropTrapDamageablePatchCollisionOriginal = typeof(DropTrapLogs).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dropTrapDamageablePatchCollisionTranspiler = typeof(DropTrapDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(dropTrapDamageablePatchCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(dropTrapDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(dropTrapDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.DropTrapDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 24

            // FlipzoDamageablePatch
            MethodInfo flipzoDamageablePatchUpdateOriginal = typeof(Flipzo).GetMethod("FVRUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo flipzoDamageablePatchUpdateTranspiler = typeof(FlipzoDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(flipzoDamageablePatchUpdateOriginal, harmony, false);
            try
            { 
                harmony.Patch(flipzoDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(flipzoDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.FlipzoDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 25

            // IgnitableDamageablePatch
            MethodInfo ignitableDamageablePatchStartOriginal = typeof(FVRIgnitable).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo ignitableDamageablePatchStartTranspiler = typeof(IgnitableDamageablePatch).GetMethod("StartTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(ignitableDamageablePatchStartOriginal, harmony, false);
            try
            { 
                harmony.Patch(ignitableDamageablePatchStartOriginal, null, null, new HarmonyMethod(ignitableDamageablePatchStartTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.IgnitableDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 26

            // SparklerDamageablePatch
            MethodInfo sparklerDamageablePatchCollisionOriginal = typeof(FVRSparkler).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo sparklerDamageablePatchCollisionTranspiler = typeof(SparklerDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sparklerDamageablePatchCollisionOriginal, harmony, false);
            try
            { 
                harmony.Patch(sparklerDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(sparklerDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SparklerDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 27

            // MatchDamageablePatch
            MethodInfo matchDamageablePatchCollisionOriginal = typeof(FVRStrikeAnyWhereMatch).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo matchDamageablePatchCollisionTranspiler = typeof(MatchDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(matchDamageablePatchCollisionOriginal, harmony, false);
            try 
            { 
                harmony.Patch(matchDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(matchDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MatchDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 28

            // HCBBoltDamageablePatch
            MethodInfo HCBBoltDamageablePatchDamageOriginal = typeof(HCBBolt).GetMethod("DamageOtherThing", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo HCBBoltDamageablePatchDamageTranspiler = typeof(HCBBoltDamageablePatch).GetMethod("DamageOtherTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(HCBBoltDamageablePatchDamageOriginal, harmony, false);
            try 
            { 
                harmony.Patch(HCBBoltDamageablePatchDamageOriginal, null, null, new HarmonyMethod(HCBBoltDamageablePatchDamageTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.HCBBoltDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 29

            // KabotDamageablePatch
            MethodInfo kabotDamageablePatchTickOriginal = typeof(Kabot.KSpike).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo kabotDamageablePatchTickTranspiler = typeof(KabotDamageablePatch).GetMethod("TickTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(kabotDamageablePatchTickOriginal, harmony, false);
            try 
            { 
                harmony.Patch(kabotDamageablePatchTickOriginal, null, null, new HarmonyMethod(kabotDamageablePatchTickTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.KabotDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 30

            // MeatCrabDamageablePatch
            MethodInfo meatCrabDamageablePatchLungingOriginal = typeof(MeatCrab).GetMethod("Crabdate_Lunging", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchLungingTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("LungingTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo meatCrabDamageablePatchAttachedOriginal = typeof(MeatCrab).GetMethod("Crabdate_Attached", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo meatCrabDamageablePatchAttachedTranspiler = typeof(MeatCrabDamageablePatch).GetMethod("AttachedTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(meatCrabDamageablePatchLungingOriginal, harmony, false);
            PatchController.Verify(meatCrabDamageablePatchAttachedOriginal, harmony, false);
            try
            { 
                harmony.Patch(meatCrabDamageablePatchLungingOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchLungingTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeatCrabDamageablePatch on meatCrabDamageablePatchLungingOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }
            try
            { 
                harmony.Patch(meatCrabDamageablePatchAttachedOriginal, null, null, new HarmonyMethod(meatCrabDamageablePatchAttachedTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MeatCrabDamageablePatch on meatCrabDamageablePatchAttachedOriginal: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 31

            // MF2_BearTrapDamageablePatch
            MethodInfo MF2_BearTrapDamageablePatchSnapOriginal = typeof(MF2_BearTrapInteractionZone).GetMethod("SnapShut", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo MF2_BearTrapDamageablePatchSnapTranspiler = typeof(MF2_BearTrapDamageablePatch).GetMethod("SnapTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(MF2_BearTrapDamageablePatchSnapOriginal, harmony, false);
            try 
            { 
                harmony.Patch(MF2_BearTrapDamageablePatchSnapOriginal, null, null, new HarmonyMethod(MF2_BearTrapDamageablePatchSnapTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MF2_BearTrapDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 32

            // MG_SwarmDamageablePatch
            MethodInfo MG_SwarmDamageablePatchFireOriginal = typeof(MG_FlyingHotDogSwarm).GetMethod("FireShot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_SwarmDamageablePatchFireTranspiler = typeof(MG_SwarmDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(MG_SwarmDamageablePatchFireOriginal, harmony, false);
            try 
            { 
                harmony.Patch(MG_SwarmDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_SwarmDamageablePatchFireTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MG_SwarmDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 33

            // MG_JerryDamageablePatch
            MethodInfo MG_JerryDamageablePatchFireOriginal = typeof(MG_JerryTheLemon).GetMethod("FireBolt", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo MG_JerryDamageablePatchFireTranspiler = typeof(MG_JerryDamageablePatch).GetMethod("FireTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(MG_JerryDamageablePatchFireOriginal, harmony, false);
            try
            { 
                harmony.Patch(MG_JerryDamageablePatchFireOriginal, null, null, new HarmonyMethod(MG_JerryDamageablePatchFireTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MG_JerryDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 34

            // MicrotorchDamageablePatch
            MethodInfo microtorchDamageablePatchUpdateOriginal = typeof(Microtorch).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo microtorchDamageablePatchUpdateTranspiler = typeof(MicrotorchDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(microtorchDamageablePatchUpdateOriginal, harmony, false);
            try
            { 
                harmony.Patch(microtorchDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(microtorchDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.MicrotorchDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 35

            // CyclopsDamageablePatch
            MethodInfo cyclopsDamageablePatchUpdateOriginal = typeof(PowerUp_Cyclops).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo cyclopsDamageablePatchUpdateTranspiler = typeof(CyclopsDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(cyclopsDamageablePatchUpdateOriginal, harmony, false);
            try 
            { 
                harmony.Patch(cyclopsDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(cyclopsDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.CyclopsDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 36

            // LaserSwordDamageablePatch
            MethodInfo laserSwordDamageablePatchUpdateOriginal = typeof(RealisticLaserSword).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo laserSwordDamageablePatchUpdateTranspiler = typeof(LaserSwordDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(laserSwordDamageablePatchUpdateOriginal, harmony, false);
            try 
            { 
                harmony.Patch(laserSwordDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(laserSwordDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.LaserSwordDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 37

            // CharcoalDamageablePatch
            MethodInfo charcoalDamageablePatchCharcoalOriginal = typeof(RotrwCharcoal).GetMethod("DamageBubble", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo charcoalDamageablePatchCharcoalTranspiler = typeof(CharcoalDamageablePatch).GetMethod("BubbleTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(charcoalDamageablePatchCharcoalOriginal, harmony, false);
            try
            { 
                harmony.Patch(charcoalDamageablePatchCharcoalOriginal, null, null, new HarmonyMethod(charcoalDamageablePatchCharcoalTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.CharcoalDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 38

            // SlicerDamageablePatch
            MethodInfo slicerDamageablePatchUpdateOriginal = typeof(SlicerBladeMaster).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo slicerDamageablePatchUpdateTranspiler = typeof(SlicerDamageablePatch).GetMethod("UpdateTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(slicerDamageablePatchUpdateOriginal, harmony, false);
            try
            {
                harmony.Patch(slicerDamageablePatchUpdateOriginal, null, null, new HarmonyMethod(slicerDamageablePatchUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SlicerDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 39

            // SpinningBladeDamageablePatch
            MethodInfo spinningBladeDamageablePatchCollisionOriginal = typeof(SpinningBladeTrapBase).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo spinningBladeDamageablePatchCollisionTranspiler = typeof(SpinningBladeDamageablePatch).GetMethod("CollisionTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(spinningBladeDamageablePatchCollisionOriginal, harmony, false);
            try 
            { 
                harmony.Patch(spinningBladeDamageablePatchCollisionOriginal, null, null, new HarmonyMethod(spinningBladeDamageablePatchCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.SpinningBladeDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 40

            // ProjectileDamageablePatch
            MethodInfo projectileDamageablePatchOriginal = typeof(FVRProjectile).GetMethod("MoveBullet", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo projectileBladeDamageablePatchTranspiler = typeof(ProjectileDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(projectileDamageablePatchOriginal, harmony, true);
            try 
            { 
                harmony.Patch(projectileDamageablePatchOriginal, null, null, new HarmonyMethod(projectileBladeDamageablePatchTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.ProjectileDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 41

            // SosigLinkDamagePatch
            MethodInfo sosigLinkDamagePatchOriginal = typeof(SosigLink).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkDamagePatchPrefix = typeof(SosigLinkDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigLinkDamagePatchPostfix = typeof(SosigLinkDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigLinkDamagePatchOriginal, harmony, true);
            harmony.Patch(sosigLinkDamagePatchOriginal, new HarmonyMethod(sosigLinkDamagePatchPrefix), new HarmonyMethod(sosigLinkDamagePatchPostfix));

            ++patchIndex; // 42

            // SosigWearableDamagePatch
            MethodInfo sosigWearableDamagePatchOriginal = typeof(SosigWearable).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigWearableDamagePatchPrefix = typeof(SosigWearableDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo sosigWearableDamagePatchPostfix = typeof(SosigWearableDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigWearableDamagePatchOriginal, harmony, true);
            harmony.Patch(sosigWearableDamagePatchOriginal, new HarmonyMethod(sosigWearableDamagePatchPrefix), new HarmonyMethod(sosigWearableDamagePatchPostfix));

            ++patchIndex; // 43

            // AutoMeaterDamagePatch
            MethodInfo autoMeaterDamagePatchOriginal = typeof(AutoMeater).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterDamagePatchPrefix = typeof(AutoMeaterDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterDamagePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterDamagePatchOriginal, new HarmonyMethod(autoMeaterDamagePatchPrefix));

            ++patchIndex; // 44

            // AutoMeaterHitZoneDamagePatch
            MethodInfo autoMeaterHitZoneDamagePatchOriginal = typeof(AutoMeaterHitZone).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo autoMeaterHitZoneDamagePatchPrefix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo autoMeaterHitZoneDamagePatchPostfix = typeof(AutoMeaterHitZoneDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(autoMeaterHitZoneDamagePatchOriginal, harmony, false);
            harmony.Patch(autoMeaterHitZoneDamagePatchOriginal, new HarmonyMethod(autoMeaterHitZoneDamagePatchPrefix), new HarmonyMethod(autoMeaterHitZoneDamagePatchPostfix));

            ++patchIndex; // 45

            // TNH_ShatterableCrateSetHoldingTokenPatch
            MethodInfo TNH_ShatterableCrateSetHoldingTokenPatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("SetHoldingToken", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateSetHoldingTokenPatchPrefix = typeof(TNH_ShatterableCrateSetHoldingTokenPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ShatterableCrateSetHoldingTokenPatchOriginal, harmony, false);
            harmony.Patch(TNH_ShatterableCrateSetHoldingTokenPatchOriginal, new HarmonyMethod(TNH_ShatterableCrateSetHoldingTokenPatchPrefix));

            ++patchIndex; // 46

            // TNH_ShatterableCrateSetHoldingHealthPatch
            MethodInfo TNH_ShatterableCrateSetHoldingHealthPatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("SetHoldingHealth", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateSetHoldingHealthPatchPrefix = typeof(TNH_ShatterableCrateSetHoldingHealthPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ShatterableCrateSetHoldingHealthPatchOriginal, harmony, false);
            harmony.Patch(TNH_ShatterableCrateSetHoldingHealthPatchOriginal, new HarmonyMethod(TNH_ShatterableCrateSetHoldingHealthPatchPrefix));

            ++patchIndex; // 47

            // TNH_ShatterableCrateDestroyPatch
            MethodInfo TNH_ShatterableCrateDestroyPatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Destroy", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDestroyPatchPrefix = typeof(TNH_ShatterableCrateDestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ShatterableCrateDestroyPatchOriginal, harmony, false);
            harmony.Patch(TNH_ShatterableCrateDestroyPatchOriginal, new HarmonyMethod(TNH_ShatterableCrateDestroyPatchPrefix));

            ++patchIndex; // 48

            // TNH_ShatterableCrateDamagePatch
            MethodInfo TNH_ShatterableCrateDamagePatchOriginal = typeof(TNH_ShatterableCrate).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo TNH_ShatterableCrateDamagePatchPrefix = typeof(TNH_ShatterableCrateDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(TNH_ShatterableCrateDamagePatchOriginal, harmony, false);
            harmony.Patch(TNH_ShatterableCrateDamagePatchOriginal, new HarmonyMethod(TNH_ShatterableCrateDamagePatchPrefix));

            ++patchIndex; // 49

            // BreakableGlassDamagerDamagePatch
            MethodInfo BreakableGlassDamagerDamageOriginal = typeof(BreakableGlassDamager).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo BreakableGlassDamagerDamagePrefix = typeof(BreakableGlassDamagerPatch).GetMethod("DamagePrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo BreakableGlassDamagerColOriginal = typeof(BreakableGlassDamager).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo BreakableGlassDamagerColPrefix = typeof(BreakableGlassDamagerPatch).GetMethod("OnCollisionEnterPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo BreakableGlassDamagerShatterOriginal = typeof(BreakableGlassDamager).GetMethod("ShatterGlass", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo BreakableGlassDamagerShatterPrefix = typeof(BreakableGlassDamagerPatch).GetMethod("ShatterGlassPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo BreakableGlassDamagerShatterPostfix = typeof(BreakableGlassDamagerPatch).GetMethod("ShatterGlassPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo BreakableGlassDamagerShatterTranspiler = typeof(BreakableGlassDamagerPatch).GetMethod("ShatterGlassTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(BreakableGlassDamagerDamageOriginal, harmony, false);
            PatchController.Verify(BreakableGlassDamagerShatterOriginal, harmony, false);
            PatchController.Verify(BreakableGlassDamagerColOriginal, harmony, false);
            harmony.Patch(BreakableGlassDamagerDamageOriginal, new HarmonyMethod(BreakableGlassDamagerDamagePrefix));
            harmony.Patch(BreakableGlassDamagerColOriginal, new HarmonyMethod(BreakableGlassDamagerColPrefix));
            try 
            { 
                harmony.Patch(BreakableGlassDamagerShatterOriginal, new HarmonyMethod(BreakableGlassDamagerShatterPrefix), new HarmonyMethod(BreakableGlassDamagerShatterPostfix), new HarmonyMethod(BreakableGlassDamagerShatterTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BreakableGlassDamagerPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 50

            // ReactiveSteelTargetDamagePatch
            MethodInfo ReactiveSteelTargetDamageOriginal = typeof(ReactiveSteelTarget).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo ReactiveSteelTargetDamagePrefix = typeof(ReactiveSteelTargetDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo ReactiveSteelTargetDamagePostfix = typeof(ReactiveSteelTargetDamagePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(ReactiveSteelTargetDamageOriginal, harmony, false);
            harmony.Patch(ReactiveSteelTargetDamageOriginal, new HarmonyMethod(ReactiveSteelTargetDamagePrefix), new HarmonyMethod(ReactiveSteelTargetDamagePostfix));

            ++patchIndex; // 51

            // RoundDamagePatch
            MethodInfo roundDamageOriginal = typeof(FVRFireArmRound).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo roundDamagePrefix = typeof(RoundDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(roundDamageOriginal, harmony, false);
            harmony.Patch(roundDamageOriginal, new HarmonyMethod(roundDamagePrefix));

            ++patchIndex; // 52

            // GasCuboidDamagePatch
            MethodInfo gasCuboidDamageOriginal = typeof(Brut_GasCuboid).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo gasCuboidDamagePrefix = typeof(GasCuboidDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(gasCuboidDamageOriginal, harmony, false);
            harmony.Patch(gasCuboidDamageOriginal, new HarmonyMethod(gasCuboidDamagePrefix));

            ++patchIndex; // 53

            // GasCuboidHandleDamagePatch
            MethodInfo gasCuboidHandleDamageOriginal = typeof(Brut_GasCuboidHandle).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo gasCuboidHandleDamagePrefix = typeof(GasCuboidHandleDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(gasCuboidHandleDamageOriginal, harmony, false);
            harmony.Patch(gasCuboidHandleDamageOriginal, new HarmonyMethod(gasCuboidHandleDamagePrefix));

            ++patchIndex; // 54

            // TransformerPatch
            MethodInfo transformerArcOriginal = typeof(Brut_TransformerSparks).GetMethod("AttemptToGenerateArc", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo transformerArcTranspiler = typeof(TransformerDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(transformerArcOriginal, harmony, false);
            try
            {
                harmony.Patch(transformerArcOriginal, null, null, new HarmonyMethod(transformerArcTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.TransformerPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 55

            // IrisDamageablePatch
            MethodInfo irisUpdateLaserOriginal = typeof(Construct_Iris).GetMethod("UpdateLaser", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo irisUpdateLaserTranspiler = typeof(IrisDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(irisUpdateLaserOriginal, harmony, false);
            try
            {
                harmony.Patch(irisUpdateLaserOriginal, null, null, new HarmonyMethod(irisUpdateLaserTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.IrisDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 56

            // FloaterDamagePatch
            MethodInfo floaterDamageOriginal = typeof(Construct_Floater).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo floaterDamagePrefix = typeof(FloaterDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(floaterDamageOriginal, harmony, false);
            harmony.Patch(floaterDamageOriginal, new HarmonyMethod(floaterDamagePrefix));

            ++patchIndex; // 57

            // FloaterCoreDamagePatch
            MethodInfo floaterCoreDamageOriginal = typeof(Construct_Floater_Core).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo floaterCoreDamagePrefix = typeof(FloaterCoreDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(floaterCoreDamageOriginal, harmony, false);
            harmony.Patch(floaterCoreDamageOriginal, new HarmonyMethod(floaterCoreDamagePrefix));

            ++patchIndex; // 58

            // BrutBlockSystemDamageablePatch
            MethodInfo brutBlockSystemFixedUpdateOriginal = typeof(BrutBlockSystem).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutBlockSystemFixedUpdateTranspiler = typeof(BrutBlockSystemDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(brutBlockSystemFixedUpdateOriginal, harmony, false);
            try
            {
                harmony.Patch(brutBlockSystemFixedUpdateOriginal, null, null, new HarmonyMethod(brutBlockSystemFixedUpdateTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BrutBlockSystemDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 59

            // BrutTurbineDamageablePatch
            MethodInfo brutTurbineCollisionOriginal = typeof(BrutTurbine).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo brutTurbineCollisionTranspiler = typeof(BrutTurbineDamageablePatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(brutTurbineCollisionOriginal, harmony, false);
            try
            {
                harmony.Patch(brutTurbineCollisionOriginal, null, null, new HarmonyMethod(brutTurbineCollisionTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying DamagePatches.BrutTurbineDamageablePatch: " + ex.Message + ":\n" + ex.StackTrace);
            }
        }
    }

    // TODO: Optimization?: Patch IFVRDamageable.Damage and have a way to track damageables so we don't need to have a specific TCP call for each
    //       Or make sure we track damageables, then when we can patch damageable.damage and send the damage and trackedID directly to other clients so they can process it too

    // Patches BallisticProjectile.Fire to keep a reference to the source firearm
    class ProjectileFirePatch
    {
        public static int skipBlast;

        static void Prefix(ref FVRFireArm ___tempFA, ref FVRFireArm firearm)
        {
            ___tempFA = firearm;
        }

        // Patches Fire to prevent blast if flag is set
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ProjectileFirePatch), "skipBlast"))); // Load skipBlast value
            toInsert.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
            Label skipLabel = il.DefineLabel();
            toInsert.Add(new CodeInstruction(OpCodes.Bgt_S, skipLabel)); // Goto label if skipBlast > 0

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("get_CurrentMovementManager"))
                {
                    instructionList[i + 7].labels.Add(skipLabel);
                    instructionList.InsertRange(i, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("ProjectileFirePatch Transpiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches BallisticProjectile.MoveBullet to ignore latest IFVRDamageable if necessary
    class BallisticProjectileDamageablePatch
    {
        // This will only let Damage() be called on the damageable by a single player
        // We will apply damage if:
        //  Damageable not identifiable across network
        //  Firearm unidentified and we are best host
        //  Firearm identified and we are controller
        public static bool GetActualFlag(bool flag2, FVRFireArm tempFA, IFVRDamageable damageable)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return flag2;
            }

            if (flag2)
            {
                // Note: If flag2, damageable != null
                TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(damageable, out damageableObject) ? damageableObject : null;
                if(!(damageable is FVRPlayerHitbox) && damageableObject == null)
                {
                    return true; // Damageable object client side (not tracked), apply damage
                }
                else // Damageable object tracked, only want to apply damage if firearm controller
                {
                    if (tempFA == null) // Can't identify firearm, let the damage be controlled by the best host
                    {
                        int bestHost = Mod.GetBestPotentialObjectHost(-1);
                        return bestHost == -1 || bestHost == GameManager.ID;
                    }
                    else // We have a ref to the firearm that fired this projectile
                    {
                        // We only want to let this projectile do damage if we control the firearm
                        TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(tempFA) ? GameManager.trackedItemByItem[tempFA] : tempFA.GetComponent<TrackedItem>();
                        if (trackedItem == null)
                        {
                            return false;
                        }
                        else
                        {
                            return trackedItem.data.controller == GameManager.ID;
                        }
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
            toInsertFirst.Add(new CodeInstruction(OpCodes.Ldloc_S, 13)); // Load IFVRDamageable
            toInsertFirst.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BallisticProjectileDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
            toInsertFirst.Add(new CodeInstruction(OpCodes.Stloc_S, 14)); // Set flag2

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_1 &&
                    instructionList[i + 1].opcode == OpCodes.Stloc_S && instructionList[i + 1].operand.ToString().Equals("System.Boolean (14)"))
                {
                    instructionList.InsertRange(i + 2, toInsertFirst);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("BallisticProjectileDamageablePatch Transpiler not applied!");
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
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldloc_S, 13)); // Load explosion gameobject
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load projectile instance
            toInsertSecond.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BallisticProjectile), "tempFA"))); // Load tempFA from instance
            toInsertSecond.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionDamageablePatch), "AddControllerReference"))); // Call AddControllerReference

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.Explosion (13)"))
                {
                    instructionList.InsertRange(i, toInsertSecond);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SubMunitionsDamageablePatch Transpiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0 &&
                    instructionList[i + 1].opcode == OpCodes.Ldloc_0)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("FlameThrowerDamageablePatch Transpiler not applied!");
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

            bool applied0 = false;
            bool applied1 = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied0 = true;
                }
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("UnityEngine.GameObject (4)"))
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied1 = true;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("GrenadeDamageablePatch Transpiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("DemonadeDamageablePatch Transpiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("SosigWeaponDamageablePatch ExplosionTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SosigWeaponDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_0)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("SosigWeaponDamageablePatch UpdateTranspiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0 || dest == null)
            {
                return;
            }

            GameObject srcToUse = src == null ? dest : src.gameObject;
            TrackedItem trackedItem = srcToUse.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                ControllerReference reference = dest.GetComponent<ControllerReference>();
                if (reference == null)
                {
                    reference = dest.AddComponent<ControllerReference>();
                }
                reference.controller = trackedItem.data.controller;
            }
        }

        public static IFVRDamageable GetActualDamageable(MonoBehaviour mb, IFVRDamageable original)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return original;
            }

            if (original != null)
            {
                TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(original, out damageableObject) ? damageableObject : null;
                if (!(original is FVRPlayerHitbox) && damageableObject == null)
                {
                    return original; // Damageable object client side (not tracked), apply damage
                }
                else // Damageable object tracked, only want to apply damage if damager controller
                {
                    ControllerReference cr = mb.GetComponent<ControllerReference>();
                    if (cr == null)
                    {
                        TrackedItem ti = mb.GetComponent<TrackedItem>();
                        if (ti == null)
                        {
                            // Controller of damaging item unknown, lest best postential host control it
                            int bestHost = Mod.GetBestPotentialObjectHost(-1);
                            return (bestHost == GameManager.ID || bestHost == -1) ? original : null;
                        }
                        else // We have a ref to the item itself
                        {
                            // We only want to let this item do damage if we control it
                            return ti.data.controller == GameManager.ID ? original : null;
                        }
                    }
                    else // We have a ref to the controller of the item that caused this damage
                    {
                        // We only want to let this item do damage if we control it
                        return cr.controller == GameManager.ID ? original : null;
                    }
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (16)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("ExplosionDamageablePatch Transpiler not applied!");
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

            bool applied = false;
            bool firstSkip = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (19)"))
                {
                    if (!firstSkip)
                    {
                        firstSkip = true;
                        continue;
                    }

                    instructionList.InsertRange(i, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("GrenadeExplosionDamageablePatch Transpiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeleeParamsDamageablePatch StabTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (6)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeleeParamsDamageablePatch TearOutTranspiler not applied!");
            }

            return instructionList;
        }

        // Patches FixedUpdate()
        static bool UpdatePrefix(ref FVRPhysicalObject ___m_obj)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || ___m_obj == null || GameManager.playersPresent.Count == 0)
            {
                return true;
            }

            // Skip if not controller of this melee params' parent object
            TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(___m_obj) ? GameManager.trackedItemByItem[___m_obj] : ___m_obj.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
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

            bool applied = false;
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
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeleeParamsDamageablePatch UpdateTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeleeParamsDamageablePatch CollisionTranspiler not applied!");
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
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return original;
            }

            if (original != null)
            {
                ControllerReference cr = mb.GetComponent<ControllerReference>();
                if (cr == null)
                {
                    TrackedItem ti = mb.GetComponent<TrackedItem>();
                    if (ti == null)
                    {
                        // If we don't have a ref to the controller of the item that caused this damage, let the damage be controlled by the
                        // first player we can find in the same scene
                        // TODO: Optimization: Keep a dictionary of players using the scene as key
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, PlayerManager> player in GameManager.players)
                        {
                            if (player.Value.visible)
                            {
                                firstPlayerInScene = player.Key;
                            }
                            break;
                        }
                        if (firstPlayerInScene != GameManager.ID)
                        {
                            return null;
                        }
                    }
                    else // We have a ref to the item itself
                    {
                        // We only want to let this item do damage if we control it
                        return ti.data.controller == GameManager.ID ? original : null;
                    }
                }
                else // We have a ref to the controller of the item that caused this damage
                {
                    // We only want to let this item do damage if we control it
                    return cr.controller == GameManager.ID ? original : null;
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

            bool applied0 = false;
            bool applied1 = false;
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
                    applied0 = true;

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
                    applied1 = true;

                    skipNext4 = true;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("AIMeleeDamageablePatch FireTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext3 = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("ProjectileDamageablePatch Transpiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("AutoMeaterBladeDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("BangSnapDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("BearTrapDamageablePatch SnapTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext2 = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("ChainsawDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext2 = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("DrillDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("DropTrapDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("FlipzoDamageablePatch UpdateTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("IgnitableDamageablePatch StartTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("SparklerDamageablePatch CollisionTranspiler not applied!");
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
            
            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MatchDamageablePatch CollisionTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("HCBBoltDamageablePatch DamageOtherTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("KabotDamageablePatch TickTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeatCrabDamageablePatch AttachedTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MeatCrabDamageablePatch LungingTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (5)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("MF2_BearTrapDamageablePatch SnapTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MG_SwarmDamageablePatch FireTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MG_JerryDamageablePatch FireTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("MicrotorchDamageablePatch UpdateTranspiler not applied!");
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

            bool applied0 = false;
            bool applied1 = false;
            bool foundBreak = false;
            int popIndex = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Emit"))
                {
                    popIndex = i;
                    instructionList.InsertRange(i + 1, toInsert);
                    applied0 = true;
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
                    applied1 = true;

                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("CyclopsDamageablePatch UpdateTranspiler not applied!");
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

            bool applied0 = false;
            bool applied1 = false;
            int foundBreak = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Emit"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied0 = true;
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
                    applied1 = true;

                    break;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("LaserSwordDamageablePatch UpdateTranspiler not applied!");
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

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_3)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;

                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("CharcoalDamageablePatch BubbleTranspiler not applied!");
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

            bool applied0 = false;
            bool applied1 = false;
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
                    applied0 = true;

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
                    applied1 = true;

                    skipNext4 = true;
                }
            }

            if (!applied0 || !applied1)
            {
                Mod.LogError("SlicerDamageablePatch UpdateTranspiler not applied!");
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

            bool applied = false;
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
                    applied = true;

                    skipNext1 = true;
                }
            }

            if (!applied)
            {
                Mod.LogError("SpinningBladeDamageablePatch CollisionTranspiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches SosigLink.Damage to keep track of damage taken by a sosig
    class SosigLinkDamagePatch
    {
        public static int skip;
        static TrackedSosig trackedSosig;

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
            trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if(trackedSosig.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    // Not in control, we want to send the damage to the controller for them to process it and return the result
                    for (int i = 0; i < __instance.S.Links.Count; ++i)
                    {
                        if (__instance.S.Links[i] == __instance)
                        {
                            if (ThreadManager.host)
                            {
                                ServerSend.SosigLinkDamage(trackedSosig.sosigData, i, d);
                            }
                            else
                            {
                                ClientSend.SosigLinkDamage(trackedSosig.data.trackedID, i, d);
                            }
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
                if (ThreadManager.host)
                {
                    if (trackedSosig.data.controller == 0)
                    {
                        ServerSend.SosigDamageData(trackedSosig);
                    }
                }
                else if (trackedSosig.data.controller == Client.singleton.ID)
                {
                    ClientSend.SosigDamageData(trackedSosig);
                }
            }
        }
    }

    // Patches SosigWearable.Damage to keep track of damage taken by a sosig
    class SosigWearableDamagePatch
    {
        public static int skip;
        static TrackedSosig trackedSosig;

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
            trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                if (trackedSosig.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < __instance.S.Links.Count; ++i)
                    {
                        if (__instance.S.Links[i] == __instance.L)
                        {
                            for (int j = 0; j < __instance.L.m_wearables.Count; ++j)
                            {
                                if (__instance.L.m_wearables[j] == __instance)
                                {
                                    if (ThreadManager.host)
                                    {
                                        ServerSend.SosigWearableDamage(trackedSosig.sosigData, i, j, d);
                                    }
                                    else
                                    {
                                        ClientSend.SosigWearableDamage(trackedSosig.data.trackedID, i, j, d);
                                    }
                                    return false;
                                }
                            }

                            break;
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
            if (trackedSosig != null && trackedSosig.data.controller == GameManager.ID)
            {
                if (ThreadManager.host)
                {
                    ServerSend.SosigDamageData(trackedSosig);
                }
                else
                {
                    ClientSend.SosigDamageData(trackedSosig);
                }
            }
        }
    }

    // Patches TNH_ShatterableCrate to keep track of damage to TNH supply boxes
    class TNH_ShatterableCrateDamagePatch
    {
        public static int skip;
        static TrackedItem trackedItem;

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
            trackedItem = __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it
                        ServerSend.ShatterableCrateDamage(trackedItem.data.trackedID, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.ShatterableCrateDamage(trackedItem.data.trackedID, d);
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
        static TrackedItem trackedItem;

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
            trackedItem = __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.ShatterableCrateDestroy(trackedItem.data.trackedID, dam);
                }
                else
                {
                    ClientSend.ShatterableCrateDestroy(trackedItem.data.trackedID, dam);
                }
            }
        }
    }

    // Patches TNH_ShatterableCrate.SetHoldingHealth to keep track of contents
    class TNH_ShatterableCrateSetHoldingHealthPatch
    {
        public static int skip;
        static TrackedItem trackedItem;

        static void Prefix(ref TNH_ShatterableCrate __instance)
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

            trackedItem = __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller == GameManager.ID)
            {
                trackedItem.itemData.additionalData[3] = 1;

                if (trackedItem.data.trackedID == -1)
                {
                    if (TrackedItem.unknownCrateHolding.TryGetValue(trackedItem.data.localWaitingIndex, out byte current) && current == 1)
                    {
                        TrackedItem.unknownCrateHolding[trackedItem.data.localWaitingIndex] = 2;
                    }
                    else
                    {
                        TrackedItem.unknownCrateHolding.Add(trackedItem.data.localWaitingIndex, 0);
                    }
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.ShatterableCrateSetHoldingHealth(trackedItem.data.trackedID);
                    }
                    else
                    {
                        ClientSend.ShatterableCrateSetHoldingHealth(trackedItem.data.trackedID);
                    }
                }
            }
        }
    }

    // Patches TNH_ShatterableCrate.SetHoldingToken to keep track of contents
    class TNH_ShatterableCrateSetHoldingTokenPatch
    {
        public static int skip;
        static TrackedItem trackedItem;

        static void Prefix(ref TNH_ShatterableCrate __instance)
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

            trackedItem = __instance.GetComponent<TrackedItem>();
            if (trackedItem != null && trackedItem.data.controller == GameManager.ID)
            {
                trackedItem.itemData.additionalData[4] = 1;

                if (trackedItem.data.trackedID == -1)
                {
                    if (TrackedItem.unknownCrateHolding.TryGetValue(trackedItem.data.localWaitingIndex, out byte current) && current == 0)
                    {
                        TrackedItem.unknownCrateHolding[trackedItem.data.localWaitingIndex] = 2;
                    }
                    else
                    {
                        TrackedItem.unknownCrateHolding.Add(trackedItem.data.localWaitingIndex, 1);
                    }
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.ShatterableCrateSetHoldingToken(trackedItem.data.trackedID);
                    }
                    else
                    {
                        ClientSend.ShatterableCrateSetHoldingToken(trackedItem.data.trackedID);
                    }
                }
            }
        }
    }

    // Patches AutoMeater.Damage to keep track of damage taken by an AutoMeater
    class AutoMeaterDamagePatch
    {
        public static int skip;
        static TrackedAutoMeater trackedAutoMeater;

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
            trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance) ? GameManager.trackedAutoMeaterByAutoMeater[__instance] : __instance.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        ServerSend.AutoMeaterDamage(trackedAutoMeater.autoMeaterData, d);
                        return false;
                    }
                }
                else if (trackedAutoMeater.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.AutoMeaterDamage(trackedAutoMeater.data.trackedID, d);
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
        //        if (ThreadManager.host)
        //        {
        //            if (trackedAutoMeater.data.controller == 0)
        //            {
        //                ServerSend.AutoMeaterDamageData(trackedAutoMeater);
        //            }
        //        }
        //        else if (trackedAutoMeater.data.controller == Client.singleton.ID)
        //        {
        //            ClientSend.AutoMeaterDamageData(trackedAutoMeater);
        //        }
        //    }
        //}
    }

    // Patches AutoMeater.Damage to keep track of damage taken by an AutoMeater
    class AutoMeaterHitZoneDamagePatch
    {
        public static int skip;
        static TrackedAutoMeater trackedAutoMeater;

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
            trackedAutoMeater = GameManager.trackedAutoMeaterByAutoMeater.ContainsKey(__instance.M) ? GameManager.trackedAutoMeaterByAutoMeater[__instance.M] : __instance.M.GetComponent<TrackedAutoMeater>();
            if (trackedAutoMeater != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to precess it and return the result
                        ServerSend.AutoMeaterHitZoneDamage(trackedAutoMeater.autoMeaterData, (byte)___Type, d);
                        return false;
                    }
                }
                else if (trackedAutoMeater.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.AutoMeaterHitZoneDamage(trackedAutoMeater.data.trackedID, ___Type, d);
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
                if (ThreadManager.host)
                {
                    if (trackedAutoMeater.data.controller == 0)
                    {
                        ServerSend.AutoMeaterHitZoneDamageData(trackedAutoMeater.data.trackedID, __instance);
                    }
                }
                else if (trackedAutoMeater.data.controller == Client.singleton.ID)
                {
                    if (trackedAutoMeater.data.trackedID != -1)
                    {
                        ClientSend.AutoMeaterHitZoneDamageData(trackedAutoMeater.data.trackedID, __instance);
                    }
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget.Damage to keep track of damage taken by an encryption
    class EncryptionDamagePatch
    {
        public static int skip;
        static TrackedEncryption trackedEncryption;

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
            trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(__instance) ? GameManager.trackedEncryptionByEncryption[__instance] : __instance.GetComponent<TrackedEncryption>();
            if (trackedEncryption != null)
            {
                if(trackedEncryption.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    // Not in control, we want to send the damage to the controller for them to process it and return the result
                    if (ThreadManager.host)
                    {
                        ServerSend.EncryptionDamage(trackedEncryption.encryptionData, d);
                    }
                    else
                    {
                        ClientSend.EncryptionDamage(trackedEncryption.data.trackedID, d);
                    }
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
                if (ThreadManager.host)
                {
                    if (trackedEncryption.data.controller == 0)
                    {
                        ServerSend.EncryptionDamageData(trackedEncryption);
                    }
                }
                else if (trackedEncryption.data.controller == GameManager.ID && trackedEncryption.data.trackedID != -1)
                {
                    ClientSend.EncryptionDamageData(trackedEncryption);
                }
            }
        }
    }

    // Patches TNH_EncryptionTarget_SubTarget.Damage to keep track of damage taken by an encryption's sub target
    class EncryptionSubDamagePatch
    {
        public static int skip;
        static TrackedEncryption trackedEncryption;

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

            if (__instance.Target == null)
            {
                Mod.LogError("Damaged an encryption sub target that is missing reference to main target!");
                return false;
            }

            // If in control of the damaged Encryption, we want to process the damage
            trackedEncryption = GameManager.trackedEncryptionByEncryption.ContainsKey(__instance.Target) ? GameManager.trackedEncryptionByEncryption[__instance.Target] : __instance.Target.GetComponent<TrackedEncryption>();
            if (trackedEncryption != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedEncryption.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        ServerSend.EncryptionSubDamage(trackedEncryption.encryptionData, __instance.Index, d);
                        return false;
                    }
                }
                else if (trackedEncryption.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.EncryptionSubDamage(trackedEncryption.data.trackedID, __instance.Index, d);
                    return false;
                }
            }
            return true;
        }
    }

    // Patches TNH_EncryptionTarget to control damaged caused by encryptions
    class EncryptionDamageablePatch
    {
        // This will only let the encryption do damage if we control it
        public static bool GetActualFlag(TNH_EncryptionTarget encryption, IFVRDamageable damageable)
        {
            // Note: If this got called it is because damageable != null, so flag2 would usually be set to true
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return true;
            }

            // Note: If flag2, damageable != null
            TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(damageable, out damageableObject) ? damageableObject : null;
            if (!(damageable is FVRPlayerHitbox) && damageableObject == null)
            {
                return true; // Damageable object client side (not tracked), apply damage
            }
            else // Damageable object tracked, only want to apply damage if encryption controller
            {
                TrackedEncryption trackedEncryption = TrackedEncryption.trackedEncryptionReferences[int.Parse(encryption.SpawnPoints[encryption.SpawnPoints.Count - 1].name)];
                if (trackedEncryption == null)
                {
                    return false;
                }
                else
                {
                    return trackedEncryption.data.controller == GameManager.ID;
                }
            }
        }

        static IEnumerable<CodeInstruction> FixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load encryption instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_S, 76)); // Load damageable
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EncryptionDamageablePatch), "GetActualFlag"))); // Call our GetActualFlag

            bool applied = false;
            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Contains("77"))
                {
                    if (found)
                    {
                        instructionList.RemoveAt(i - 1);
                        instructionList.InsertRange(i - 1, toInsert0);
                        applied = true;
                        break;
                    }
                    else
                    {
                        found = true;
                    }
                }
            }

            if (!applied)
            {
                Mod.LogError("EncryptionDamageablePatch FixedUpdateTranspiler not applied!");
            }

            return instructionList;
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
            TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.ContainsKey(__instance) ? GameManager.trackedItemBySosigWeapon[__instance] : __instance.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        ServerSend.SosigWeaponDamage(trackedItem.itemData, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.SosigWeaponDamage(trackedItem.data.trackedID, d);
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
            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(___m_launcher, out trackedItem) ? trackedItem : ___m_launcher.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedItem.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        ServerSend.RemoteMissileDamage(trackedItem.itemData, d);
                        return false;
                    }
                }
                else if (trackedItem.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.RemoteMissileDamage(trackedItem.data.trackedID, d);
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
            TrackedObject trackedObject = __instance.GetComponent<Scripts.TrackedObjectReference>().trackedRef;
            if (trackedObject != null)
            {
                if (ThreadManager.host)
                {
                    if (trackedObject.data.controller == 0)
                    {
                        return true;
                    }
                    else
                    {
                        // Not in control, we want to send the damage to the controller for them to process it and return the result
                        ServerSend.StingerMissileDamage(((TrackedItem)trackedObject).itemData, d);
                        return false;
                    }
                }
                else if (trackedObject.data.controller == Client.singleton.ID)
                {
                    return true;
                }
                else
                {
                    ClientSend.StingerMissileDamage(trackedObject.data.trackedID, d);
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

        static bool Prefix(UberShatterable __instance, Vector3 point, Vector3 dir, float intensity)
        {
            if (skip > 0 || Mod.managerObject == null)
            {
                return true;
            }

            TrackedObject trackedObject = GameManager.trackedObjectByShatterable.TryGetValue(__instance, out trackedObject) ? trackedObject : __instance.GetComponent<TrackedObject>();
            if(trackedObject != null)
            {
                return trackedObject.HandleShatter(__instance, point, dir, intensity, false, 0, null);
            }

            return true;
        }
    }

    // Patches BreakableGlassDamager to keep track of damage taken by a breakable glass and shattering event
    class BreakableGlassDamagerPatch
    {
        public static int damageSkip;

        static bool DamagePrefix(BreakableGlassDamager __instance, Damage d)
        {
            if (damageSkip > 0)
            {
                return true;
            }

            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedBreakableGlass trackedBreakableGlass = GameManager.trackedBreakableGlassByBreakableGlassDamager.TryGetValue(__instance, out trackedBreakableGlass) ? trackedBreakableGlass : __instance.GetComponent<TrackedBreakableGlass>();
            if (trackedBreakableGlass != null)
            {
                if(trackedBreakableGlass.data.controller == GameManager.ID)
                {
                    return true;
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.BreakableGlassDamage(trackedBreakableGlass.breakableGlassData, d);
                    }
                    else
                    {
                        ClientSend.BreakableGlassDamage(trackedBreakableGlass.data.trackedID, d);
                    }
                    return false;
                }
            }
            return true;
        }

        static void ShatterGlassPrefix(BreakableGlassDamager __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            ++Mod.skipAllInstantiates;
        }

        static IEnumerable<CodeInstruction> ShatterGlassTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load damager instance
            toInsert0.Add(new CodeInstruction(OpCodes.Ldc_I4_0)); // Load 0
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BreakableGlassDamagerPatch), "ShatterAudio"))); // Call our ShatterAudio

            List<CodeInstruction> toInsert1 = new List<CodeInstruction>();
            toInsert1.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load damager instance
            toInsert1.Add(new CodeInstruction(OpCodes.Ldc_I4_1)); // Load 1
            toInsert1.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BreakableGlassDamagerPatch), "ShatterAudio"))); // Call our ShatterAudio

            List<CodeInstruction> toInsert2 = new List<CodeInstruction>();
            toInsert2.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load damager instance
            toInsert2.Add(new CodeInstruction(OpCodes.Ldc_I4_2)); // Load 2
            toInsert2.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BreakableGlassDamagerPatch), "ShatterAudio"))); // Call our ShatterAudio

            List<CodeInstruction> toInsert3 = new List<CodeInstruction>();
            toInsert3.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load damager instance
            toInsert3.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BreakableGlassDamager), "m_shards"))); // Load shards
            toInsert3.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BreakableGlassDamagerPatch), "TrackShards"))); // Call our TrackShards

            bool[] applied = new bool[4];
            int found = 0;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("PlayCoreSoundDelayed"))
                {
                    if (found == 0)
                    {
                        instructionList.InsertRange(i + 1, toInsert0);
                        i += toInsert0.Count;
                        applied[0] = true;
                    }
                    else if(found == 2)
                    {
                        instructionList.InsertRange(i + 1, toInsert1);
                        i += toInsert1.Count;
                        applied[1] = true;
                    }
                    else if(found == 4)
                    {
                        instructionList.InsertRange(i + 1, toInsert2);
                        i += toInsert2.Count;
                        applied[2] = true;
                    }

                    ++found;
                }
                else if(instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("Clear"))
                {
                    instructionList.InsertRange(i - 2, toInsert3);
                    applied[3] = true;
                    break;
                }
            }

            for (int i = 0; i < applied.Length; ++i)
            {
                if (!applied[i])
                {
                    Mod.LogError("BreakableGlassDamagerPatch ShatterGlassTranspiler not applied!");
                    break;
                }
            }

            return instructionList;
        }

        public static void ShatterAudio(BreakableGlassDamager __instance, int mode)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            TrackedBreakableGlass trackedBreakableGlass = GameManager.trackedBreakableGlassByBreakableGlassDamager.TryGetValue(__instance, out trackedBreakableGlass) ? trackedBreakableGlass : __instance.GetComponent<TrackedBreakableGlass>();
            if (trackedBreakableGlass != null)
            {
                if (ThreadManager.host)
                {
                    ServerSend.WindowShatterSound(trackedBreakableGlass.data.trackedID, mode);
                }
                else
                {
                    ClientSend.WindowShatterSound(trackedBreakableGlass.data.trackedID, mode);
                }
            }
        }

        public static void TrackShards(List<GameObject> shards)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            Mod.LogInfo("Tracking "+ shards.Count+" shards");
            for (int i = 0; i < shards.Count; ++i) 
            {
                GameManager.SyncTrackedObjects(shards[i].transform, true, null);
            }
        }

        static void ShatterGlassPostfix(BreakableGlassDamager __instance)
        {
            // Skip if not connected
            if (Mod.managerObject == null)
            {
                return;
            }

            --Mod.skipAllInstantiates;
        }

        // Prevents processing collision if non controller
        static bool OnCollisionEnterPrefix(BreakableGlassDamager __instance)
        {
            if(Mod.managerObject != null)
            {
                TrackedBreakableGlass trackedBreakableGlass = GameManager.trackedBreakableGlassByBreakableGlassDamager.TryGetValue(__instance, out trackedBreakableGlass) ? trackedBreakableGlass : __instance.GetComponent<TrackedBreakableGlass>();
                if (trackedBreakableGlass != null)
                {
                    return trackedBreakableGlass.data.controller == GameManager.ID;
                }
            }
            return true;
        }
    }

    // Patches ReactiveSteelTarget.Damage to keep track of damage taken by a ReactiveSteelTarget
    class ReactiveSteelTargetDamagePatch
    {
        static TrackedObject trackedObject;

        static bool Prefix(ReactiveSteelTarget __instance, Damage dam)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            { 
                if(trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, do everything apart from adding force to RB
                {
                    if (dam.Class != FistVR.Damage.DamageClass.Projectile)
                    {
                        return false;
                    }
                    Vector3 position = dam.point + dam.hitNormal * UnityEngine.Random.Range(0.001f, 0.005f);
                    if (__instance.BulletHolePrefabs.Length > 0 && __instance.m_useHoles)
                    {
                        float t = Mathf.InverseLerp(400f, 2000f, dam.Dam_TotalKinetic);
                        float num = Mathf.Lerp(__instance.BulletHoleSizeRange.x, __instance.BulletHoleSizeRange.y, t);
                        if (__instance.m_currentHoles.Count > __instance.MaxHoles)
                        {
                            __instance.holeindex++;
                            if (__instance.holeindex > __instance.MaxHoles - 1)
                            {
                                __instance.holeindex = 0;
                            }
                            __instance.m_currentHoles[__instance.holeindex].transform.position = position;
                            __instance.m_currentHoles[__instance.holeindex].transform.rotation = Quaternion.LookRotation(dam.hitNormal, UnityEngine.Random.onUnitSphere);
                            __instance.m_currentHoles[__instance.holeindex].transform.localScale = new Vector3(num, num, num);
                        }
                        else
                        {
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.BulletHolePrefabs[UnityEngine.Random.Range(0, __instance.BulletHolePrefabs.Length)], position, Quaternion.LookRotation(dam.hitNormal, UnityEngine.Random.onUnitSphere));
                            gameObject.transform.SetParent(__instance.transform);
                            gameObject.transform.localScale = new Vector3(num, num, num);
                            __instance.m_currentHoles.Add(gameObject);
                        }
                    }
                    __instance.PlayHitSound(Mathf.Clamp(dam.Dam_TotalKinetic * 0.0025f, 0.05f, 1f));

                    return false;
                }
            }
            return true;
        }

        static void Postfix(ReactiveSteelTarget __instance, Damage dam)
        {
            if (Mod.managerObject == null || dam.Class != Damage.DamageClass.Projectile)
            {
                return;
            }

            if (trackedObject != null)
            {
                TrackedItem trackedItem = trackedObject as TrackedItem;
                int index = -1;
                if (trackedItem.findSecondary != null)
                {
                    index = trackedItem.findSecondary(__instance);
                }

                if (index != -1)
                {
                    if (ThreadManager.host)
                    {
                        if (__instance.m_useHoles)
                        {
                            GameObject currentHole = __instance.m_currentHoles[__instance.holeindex];
                            ServerSend.ReactiveSteelTargetDamage(trackedObject.data.trackedID, index, dam.Dam_TotalKinetic, dam.Dam_Blunt, dam.point, dam.strikeDir, true, currentHole.transform.position, currentHole.transform.rotation, currentHole.transform.localScale.x);
                        }
                        else
                        {
                            ServerSend.ReactiveSteelTargetDamage(trackedObject.data.trackedID, index, dam.Dam_TotalKinetic, dam.Dam_Blunt, dam.point, dam.strikeDir, false, Vector3.zero, Quaternion.identity, 0);
                        }
                    }
                    else
                    {

                        if (__instance.m_useHoles)
                        {
                            GameObject currentHole = __instance.m_currentHoles[__instance.holeindex];
                            ClientSend.ReactiveSteelTargetDamage(trackedObject.data.trackedID, index, dam.Dam_TotalKinetic, dam.Dam_Blunt, dam.point, dam.strikeDir, true, currentHole.transform.position, currentHole.transform.rotation, currentHole.transform.localScale.x);
                        }
                        else
                        {
                            ClientSend.ReactiveSteelTargetDamage(trackedObject.data.trackedID, index, dam.Dam_TotalKinetic, dam.Dam_Blunt, dam.point, dam.strikeDir, false, Vector3.zero, Quaternion.identity, 0);
                        }
                    }
                }
            }
        }
    }

    // Patches FVRFireArmRound.Damage to keep track of damage taken by a round
    class RoundDamagePatch
    {
        static bool Prefix(FVRFireArmRound __instance, Damage d)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            TrackedObject trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            { 
                if(trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, do everything apart from adding force to RB
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.RoundDamage(trackedObject.data.trackedID, d, trackedObject.data.controller);
                    }
                    else
                    {
                        ClientSend.RoundDamage(trackedObject.data.trackedID, d);
                    }

                    return false;
                }
            }
            return true;
        }
    }

    // Patches Brut_GasCuboid.Damage to keep track of damage taken by a gas cuboid
    class GasCuboidDamagePatch
    {
        public static int skip = 0;

        static bool Prefix(Brut_GasCuboid __instance, Damage d)
        {
            if (skip > 0 || Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            TrackedObject trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            { 
                if(trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, send damage to controller for processing
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.GasCuboidDamage(trackedObject.data.trackedID, d, trackedObject.data.controller);
                    }
                    else
                    {
                        ClientSend.GasCuboidDamage(trackedObject.data.trackedID, d);
                    }

                    return false;
                }
            }
            return true;
        }
    }

    // Patches Brut_GasCuboidHandle.Damage to keep track of damage taken by a gas cuboid handle
    class GasCuboidHandleDamagePatch
    {
        public static int skip = 0;

        static bool Prefix(Brut_GasCuboidHandle __instance, Damage d)
        {
            if (skip > 0 || Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            TrackedObject trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            { 
                if(trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, send damage to controller for processing
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.GasCuboidHandleDamage(trackedObject.data.trackedID, d, trackedObject.data.controller);
                    }
                    else
                    {
                        ClientSend.GasCuboidHandleDamage(trackedObject.data.trackedID, d);
                    }

                    return false;
                }
            }
            return true;
        }
    }

    // Patches Brut_TransformerSparks.AttemptToGenerateArc to control who causes damage
    class TransformerDamageablePatch
    {
        // This will only let the arc do damage if we control damageable
        // Note: Unlike most other GetActualFlags, here we do damage if we control the damageable, not the damager
        //       We do it this way instead because the damager, transformer, is not tracked
        public static bool GetActualFlag(IFVRDamageable damageable)
        {
            // Note: If this got called it is because damageable != null, so flag2 would usually be set to true
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0 || damageable is FVRPlayerHitbox)
            {
                return true;
            }

            // Note: If flag2, damageable != null
            TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(damageable, out damageableObject) ? damageableObject : null;
            if (damageableObject == null)
            {
                return true; // Damageable object client side (not tracked), apply damage
            }
            else // Damageable object tracked, only want to apply damage if we control it
            {
                return damageableObject.data.controller == GameManager.ID;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 7)); // Load IFVRDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TransformerDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 8)); // Set flag2

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_1 &&
                    instructionList[i + 1].opcode == OpCodes.Stloc_S && instructionList[i + 1].operand.ToString().Equals("System.Boolean (8)"))
                {
                    instructionList.InsertRange(i+1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("TransformerDamageablePatch Transpiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches Construct_Floater.Damage to keep track of damage taken by a floater
    class FloaterDamagePatch
    {
        public static int skip = 0;

        static bool Prefix(Construct_Floater __instance, Damage D)
        {
            if (skip > 0 || Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            TrackedObject trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            {
                if (trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, send damage to controller for processing
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.FloaterDamage(trackedObject.data.trackedID, D, trackedObject.data.controller);
                    }
                    else
                    {
                        ClientSend.FloaterDamage(trackedObject.data.trackedID, D);
                    }

                    return false;
                }
            }
            return true;
        }
    }

    // Patches Construct_Floater_Core.Damage to keep track of damage taken by a floater's core
    class FloaterCoreDamagePatch
    {
        public static int skip = 0;

        static bool Prefix(Construct_Floater_Core __instance, Damage D)
        {
            if (skip > 0 || Mod.managerObject == null)
            {
                return true;
            }

            // If in control, apply damage, send to everyone else
            // If not in control, apply damage without adding force to RB, then send to everyone, controller will apply force
            TrackedObject trackedObject = GameManager.trackedObjectByDamageable.TryGetValue(__instance, out trackedObject) ? trackedObject : null;
            if (trackedObject != null)
            {
                if (trackedObject.data.controller == GameManager.ID)
                {
                    return true;
                }
                else // Not controller, send damage to controller for processing
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.FloaterCoreDamage(trackedObject.data.trackedID, D, trackedObject.data.controller);
                    }
                    else
                    {
                        ClientSend.FloaterCoreDamage(trackedObject.data.trackedID, D);
                    }

                    return false;
                }
            }
            return true;
        }
    }

    // Patches Construct_Iris.UpdateLaser to control who causes damage
    class IrisDamageablePatch
    {
        // This will only let the laser do damage if we control the construct
        public static bool GetActualFlag(Construct_Iris iris, IFVRDamageable damageable)
        {
            // Note: If this got called it is because damageable != null, so flag2 would usually be set to true
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return true;
            }

            // Note: If flag2, damageable != null
            TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(damageable, out damageableObject) ? damageableObject : null;
            if (!(damageable is FVRPlayerHitbox) && damageableObject == null)
            {
                return true; // Damageable object client side (not tracked), apply damage
            }
            else // Damageable object tracked, only want to apply damage if iris controller
            {
                TrackedIris trackedIris = TrackedObject.trackedReferences[iris.BParams[iris.BParams.Count - 1].Pen] as TrackedIris;
                if (trackedIris == null)
                {
                    return false;
                }
                else
                {
                    return trackedIris.data.controller == GameManager.ID;
                }
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Load iris instance
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load IFVRDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IrisDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 9)); // Set flag2

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_1 &&
                    instructionList[i + 1].opcode == OpCodes.Stloc_S && instructionList[i + 1].operand.ToString().Equals("System.Boolean (9)"))
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("IrisDamageablePatch Transpiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches BrutBlockSystem.FixedUpdate to control who causes damage
    class BrutBlockSystemDamageablePatch
    {
        public static IFVRDamageable GetActualDamageable(IFVRDamageable original)
        {
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0)
            {
                return original;
            }

            if (original != null)
            {
                TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(original, out damageableObject) ? damageableObject : null;
                if (!(original is FVRPlayerHitbox) && damageableObject == null)
                {
                    return original; // Damageable object client side (not tracked), apply damage
                }
                else // Damageable object tracked, only want to apply damage if best potential host
                {
                    int bestHost = Mod.GetBestPotentialObjectHost(-1);
                    return (bestHost == GameManager.ID || bestHost == -1) ? original : null;
                }
            }
            return original;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 4)); // Load IFVRDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BrutBlockSystemDamageablePatch), "GetActualDamageable"))); // Call GetActualDamageable, put return val on stack
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_S, 4)); // Set IFVRDamageable

            bool applied = false;
            bool found = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString().Equals("FistVR.IFVRDamageable (4)"))
                {
                    if (found)
                    {
                        instructionList.InsertRange(i + 1, toInsert);
                        applied = true;
                        break;
                    }
                    else
                    {
                        found = true;
                    }
                }
            }

            if (!applied)
            {
                Mod.LogError("BrutBlockSystemDamageablePatch Transpiler not applied!");
            }

            return instructionList;
        }
    }

    // Patches BrutTurbine.OnCollisionEnter to control who causes damage
    class BrutTurbineDamageablePatch
    {
        // This will only let the turbine do damage if we control damageable
        public static bool GetActualFlag(IFVRDamageable damageable)
        {
            // Note: If this got called it is because damageable != null, so flag2 would usually be set to true
            // Skip if not connected or no one to send data to
            if (Mod.managerObject == null || GameManager.playersPresent.Count == 0 || damageable is FVRPlayerHitbox)
            {
                return true;
            }

            // Note: If flag2, damageable != null
            TrackedObject damageableObject = GameManager.trackedObjectByDamageable.TryGetValue(damageable, out damageableObject) ? damageableObject : null;
            if (damageableObject == null)
            {
                return true; // Damageable object client side (not tracked), apply damage
            }
            else // Damageable object tracked, only want to apply damage if we control it
            {
                return damageableObject.data.controller == GameManager.ID;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 8)); // Load IFVRDamageable
            toInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BrutTurbineDamageablePatch), "GetActualFlag"))); // Call GetActualFlag, put return val on stack
            toInsert.Add(new CodeInstruction(OpCodes.Stloc_3)); // Set flag2

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Ldc_I4_1 &&
                    instructionList[i+1].opcode == OpCodes.Stloc_3)
                {
                    instructionList.InsertRange(i + 1, toInsert);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("BrutTurbineDamageablePatch Transpiler not applied!");
            }

            return instructionList;
        }
    }
}
