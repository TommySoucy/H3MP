using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP.Patches
{
    public class PatchController
    {
        // Patch verification stuff
        static Type ILManipulatorType;
        static MethodInfo getInstructionsMethod;

        public static Dictionary<string, int> hashes;
        public static bool writeWhenDone;
        public static int breakingPatchVerify = 0;
        public static int warningPatchVerify = 0;

        // Mod compatibility stuff
        public static Assembly[] assemblies;

        // TNH Tweaker
        public static int TNHTweakerAsmIdx = -1;
        public static Type TNHTweaker_TNHTweaker;
        public static FieldInfo TNHTweaker_TNHTweaker_SpawnedBossIndexes;
        public static FieldInfo TNHTweaker_TNHTweaker_PreventOutfitFunctionality;
        public static Type TNHTweaker_TNHPatches;
        public static MethodInfo TNHTweaker_TNHPatches_ConfigureSupplyPoint;
        public static Type TNHTweaker_PatrolPatches;
        public static Type TNHTweaker_Patrol;
        public static Type TNHTweaker_LoadedTemplateManager;
        public static FieldInfo TNHTweaker_LoadedTemplateManager_LoadedCharactersDict;
        public static Type TNHTweaker_CustomCharacter;
        public static MethodInfo TNHTweaker_CustomCharacter_GetCurrentLevel;
        public static FieldInfo TNHTweaker_CustomCharacter_ForceDisableOutfitFunctionality;
        public static FieldInfo TNHTweaker_CustomCharacter_HasPrimaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_PrimaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_HasSecondaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_SecondaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_HasTertiaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_TertiaryWeapon;
        public static FieldInfo TNHTweaker_CustomCharacter_HasPrimaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_PrimaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_HasSecondaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_SecondaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_HasTertiaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_TertiaryItem;
        public static FieldInfo TNHTweaker_CustomCharacter_HasShield;
        public static FieldInfo TNHTweaker_CustomCharacter_Shield;
        public static Type TNHTweaker_EquipmentGroup;
        public static MethodInfo TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups;
        public static MethodInfo TNHTweaker_EquipmentGroup_GetObjects;
        public static FieldInfo TNHTweaker_EquipmentGroup_NumMagsSpawned;
        public static FieldInfo TNHTweaker_EquipmentGroup_NumRoundsSpawned;
        public static FieldInfo TNHTweaker_EquipmentGroup_MinAmmoCapacity;
        public static FieldInfo TNHTweaker_EquipmentGroup_MaxAmmoCapacity;
        public static FieldInfo TNHTweaker_EquipmentGroup_ItemsToSpawn;
        public static Type TNHTweaker_LoadoutEntry;
        public static FieldInfo TNHTweaker_LoadoutEntry_PrimaryGroup;
        public static FieldInfo TNHTweaker_LoadoutEntry_BackupGroup;
        public static Type TNHTweaker_TNHTweakerUtils;
        public static MethodInfo TNHTweaker_TNHTweakerUtils_InstantiateFromEquipmentGroup;

        // Collects fields/types relevant to mod compatibility patches
        private static void GetCompatibilityData()
        {
            // Look for supported mod assemblies we may need to patch for
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; ++i)
            {
                if (assemblies[i].GetName().Name.Equals("TakeAndHoldTweaker"))
                {
                    Mod.LogInfo("Found TNH tweaker assembly at " + i);
                    TNHTweakerAsmIdx = i;
                    TNHTweaker_TNHPatches = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.Patches.TNHPatches");
                    TNHTweaker_TNHPatches_ConfigureSupplyPoint = TNHTweaker_TNHPatches.GetMethod("ConfigureSupplyPoint", BindingFlags.Public | BindingFlags.Static);
                    TNHTweaker_PatrolPatches = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.Patches.PatrolPatches");
                    TNHTweaker_Patrol = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.ObjectTemplates.Patrol");
                    TNHTweaker_LoadedTemplateManager = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.LoadedTemplateManager");
                    TNHTweaker_LoadedTemplateManager_LoadedCharactersDict = TNHTweaker_LoadedTemplateManager.GetField("LoadedCharactersDict", BindingFlags.Public | BindingFlags.Static);
                    TNHTweaker_CustomCharacter = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.ObjectTemplates.CustomCharacter");
                    TNHTweaker_CustomCharacter_GetCurrentLevel = TNHTweaker_CustomCharacter.GetMethod("GetCurrentLevel", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_ForceDisableOutfitFunctionality = TNHTweaker_CustomCharacter.GetField("ForceDisableOutfitFunctionality", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasPrimaryWeapon = TNHTweaker_CustomCharacter.GetField("HasPrimaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_PrimaryWeapon = TNHTweaker_CustomCharacter.GetField("PrimaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasSecondaryWeapon = TNHTweaker_CustomCharacter.GetField("HasSecondaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_SecondaryWeapon = TNHTweaker_CustomCharacter.GetField("SecondaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasTertiaryWeapon = TNHTweaker_CustomCharacter.GetField("HasTertiaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_TertiaryWeapon = TNHTweaker_CustomCharacter.GetField("TertiaryWeapon", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasPrimaryItem = TNHTweaker_CustomCharacter.GetField("HasPrimaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_PrimaryItem = TNHTweaker_CustomCharacter.GetField("PrimaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasSecondaryItem = TNHTweaker_CustomCharacter.GetField("HasSecondaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_SecondaryItem = TNHTweaker_CustomCharacter.GetField("SecondaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasTertiaryItem = TNHTweaker_CustomCharacter.GetField("HasTertiaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_TertiaryItem = TNHTweaker_CustomCharacter.GetField("TertiaryItem", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_HasShield = TNHTweaker_CustomCharacter.GetField("HasShield", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_CustomCharacter_Shield = TNHTweaker_CustomCharacter.GetField("Shield", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.ObjectTemplates.EquipmentGroup");
                    TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups = TNHTweaker_EquipmentGroup.GetMethod("GetSpawnedEquipmentGroups", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_GetObjects = TNHTweaker_EquipmentGroup.GetMethod("GetObjects", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_NumMagsSpawned = TNHTweaker_EquipmentGroup.GetField("NumMagsSpawned", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_NumRoundsSpawned = TNHTweaker_EquipmentGroup.GetField("NumRoundsSpawned", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_MinAmmoCapacity = TNHTweaker_EquipmentGroup.GetField("MinAmmoCapacity", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_MaxAmmoCapacity = TNHTweaker_EquipmentGroup.GetField("MaxAmmoCapacity", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_EquipmentGroup_ItemsToSpawn = TNHTweaker_EquipmentGroup.GetField("ItemsToSpawn", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_LoadoutEntry = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.ObjectTemplates.LoadoutEntry");
                    TNHTweaker_LoadoutEntry_PrimaryGroup = TNHTweaker_LoadoutEntry.GetField("PrimaryGroup", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_LoadoutEntry_BackupGroup = TNHTweaker_LoadoutEntry.GetField("BackupGroup", BindingFlags.Public | BindingFlags.Instance);
                    TNHTweaker_TNHTweaker = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.TNHTweaker");
                    TNHTweaker_TNHTweaker_SpawnedBossIndexes = TNHTweaker_TNHTweaker.GetField("SpawnedBossIndexes", BindingFlags.Public | BindingFlags.Static);
                    TNHTweaker_TNHTweaker_PreventOutfitFunctionality = TNHTweaker_TNHTweaker.GetField("PreventOutfitFunctionality", BindingFlags.Public | BindingFlags.Static);
                    TNHTweaker_TNHTweakerUtils = assemblies[TNHTweakerAsmIdx].GetType("TNHTweaker.Utilities.TNHTweakerUtils");
                    TNHTweaker_TNHTweakerUtils_InstantiateFromEquipmentGroup = TNHTweaker_TNHTweaker.GetMethod("InstantiateFromEquipmentGroup", BindingFlags.Public | BindingFlags.Static);
                }
            }
        }

        // Verifies patch integrity by comparing original method's hash with stored hash
        public static void Verify(MethodInfo methodInfo, Harmony harmony, bool breaking)
        {
            if (hashes == null)
            {
                if (File.Exists(Mod.H3MPPath + "/PatchHashes.json"))
                {
                    hashes = JObject.Parse(File.ReadAllText(Mod.H3MPPath + "/PatchHashes.json")).ToObject<Dictionary<string, int>>();
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
                OpCode oc = instruction.opcode;
                if (oc == null)
                {
                    s += "null opcode" + (instruction.operand == null ? "null operand" : instruction.operand.ToString());
                }
                else
                {
                    // This is done because the code changes if a mod is loaded using MonoMod loader? Some calls become virtual
                    s += (oc == OpCodes.Call || oc == OpCodes.Callvirt ? "c" : oc.ToString()) + (instruction.operand == null ? "null operand" : instruction.operand.ToString());
                }
            }
            int hash = s.GetDeterministicHashCode();

            // Verify hash
            if (hashes.TryGetValue(identifier, out int originalHash))
            {
                if (originalHash != hash)
                {
                    if (breaking)
                    {
#if DEBUG
                        Mod.LogError("PatchVerify: " + identifier + " failed patch verify, this will most probably break H3MP! Update the mod.\nOriginal hash: " + originalHash + ", new hash: " + hash);
#endif
                        ++breakingPatchVerify;
                    }
                    else
                    {
#if DEBUG
                        Mod.LogWarning("PatchVerify: " + identifier + " failed patch verify, this will most probably break some part of H3MP. Update the mod.\nOriginal hash: " + originalHash + ", new hash: " + hash);
#endif
                        ++warningPatchVerify;
                    }

                    hashes[identifier] = hash;
                }
            }
            else
            {
                hashes.Add(identifier, hash);
                if (!writeWhenDone)
                {
#if DEBUG
                    Mod.LogWarning("PatchVerify: " + identifier + " not found in hashes. Most probably a new patch. This warning will remain until new hash file is written.");
#endif
                }
            }
        }

        private static int GetParamArrHash(ParameterInfo[] paramArr)
        {
            int hash = 0;
            foreach (ParameterInfo t in paramArr)
            {
                hash += t.ParameterType.Name.GetDeterministicHashCode();
            }
            return hash;
        }

        public static void DoPatching()
        {
            Harmony harmony = new Harmony("VIP.TommySoucy.H3MP");

            GetCompatibilityData();

            int patchGroupIndex = 0;
            int patchIndex = 0;
            try
            {
                GeneralPatches.DoPatching(harmony, ref patchIndex);
                patchIndex = 0;
                ++patchGroupIndex;
                InteractionPatches.DoPatching(harmony, ref patchIndex);
                patchIndex = 0;
                ++patchGroupIndex;
                ActionPatches.DoPatching(harmony, ref patchIndex);
                patchIndex = 0;
                ++patchGroupIndex;
                InstantiationPatches.DoPatching(harmony, ref patchIndex);
                patchIndex = 0;
                ++patchGroupIndex;
                DamagePatches.DoPatching(harmony, ref patchIndex);
                patchIndex = 0;
                ++patchGroupIndex;
                TNHPatches.DoPatching(harmony, ref patchIndex);
            }
            catch (Exception)
            {
                Mod.LogError("PATCHING EXCEPTION AT "+patchGroupIndex+"-"+patchIndex);
            }

            ProcessPatchResult();
        }

        private static void ProcessPatchResult()
        {
            if (writeWhenDone)
            {
                File.WriteAllText(Mod.H3MPPath + "/PatchHashes.json", JObject.FromObject(hashes).ToString());
            }

            if (breakingPatchVerify > 0)
            {
                Mod.LogError("PatchVerify report: " + breakingPatchVerify + " breaking, " + warningPatchVerify + " warnings.\nIf you have other mods installed this may be normal. Refer to H3MP mod compatibility list in case things break.");
            }
            else if (warningPatchVerify > 0)
            {
                Mod.LogWarning("PatchVerify report: 0 breaking, " + warningPatchVerify + " warnings.\nIf you have other mods installed this may be normal. Refer to H3MP mod compatibility list in case things break.");
            }

            List<string> supportedMods = new List<string>();
            object[] dependencyAttributes = typeof(Mod).GetCustomAttributes(typeof(BepInDependency), false);
            for (int i = 0; i < dependencyAttributes.Length; ++i)
            {
                BepInDependency dependency = dependencyAttributes[i] as BepInDependency;
                if (dependency != null)
                {
                    supportedMods.Add(dependency.DependencyGUID);
                }
            }
            int unknownModCount = 0;
#if DEBUG
            List<string> unknownMods = new List<string>();
#endif
            foreach (KeyValuePair<string, PluginInfo> otherPlugin in Chainloader.PluginInfos)
            {
                if (!otherPlugin.Key.Equals("VIP.TommySoucy.H3MP") && !supportedMods.Contains(otherPlugin.Key))
                {
                    ++unknownModCount;
#if DEBUG
                    unknownMods.Add(otherPlugin.Key);
#endif
                }

                switch (otherPlugin.Key)
                {
                    case "dll.wfiost.h3vrutilities":
                        Mod.LogWarning("You have H3VRUtilities installed! Note that any object/entity that uses the scripts included in this mod may not work with H3MP!");
                        break;
                    case "h3vr.cityrobo.OpenScripts":
                        Mod.LogWarning("You have OpenScripts installed! Note that any object/entity that uses the scripts included in this mod may not work with H3MP!");
                        break;
                    case "h3vr.OpenScripts2":
                        Mod.LogWarning("You have OpenScripts2 installed! Note that any object/entity that uses the scripts included in this mod may not work with H3MP!");
                        break;
                    case "h3vr.andrew_ftw.afcl":
                        Mod.LogWarning("You have FTW Arms AFCL installed! Note that any object/entity that uses the scripts included in this mod may not work with H3MP!");
                        break;
                }
            }
            Mod.LogWarning("You have at least " + unknownModCount + " mods installed that are either unrecognized or unsupported by H3MP");
#if DEBUG
            Mod.LogWarning("Unknown/Unsupported mods:");
            for (int i = 0; i < unknownMods.Count; ++i)
            {
                Mod.LogWarning(unknownMods[i]);
            }
#endif
        }

        // This is a copy of HarmonyX's AccessTools extension method EnumeratorMoveNext (i think)
        // Gets MoveNext() of a Coroutine
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
    }
}
