using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using H3MP;
using System;
using FFmpeg.AutoGen;
using MonoMod.Utils;

namespace MeatKit
{
    [CreateAssetMenu(menuName = "MeatKit/Build Items/H3MP Player Body", fileName = "New build item")]
    public class H3MPPlayerModelBuildItem : OtherLoaderBuildRoot
    {
        [Header("H3MP relevent stuff")]
        [Tooltip("The ID of your player body's prefab")]
        public string playerPrefabID;

        public override IEnumerable<string> RequiredDependencies
        {
            get { return new[] { "devyndamonster-OtherLoader-1.3.0", "VIP-H3MP-1.7.0" }; }
        }

        public override void GenerateLoadAssets(TypeDefinition plugin, ILProcessor il)
        {
            base.GenerateLoadAssets(plugin, il);

#if H3VR_IMPORTED
            EnsurePluginDependsOn(plugin, H3MP.Mod.pluginGuid, H3MP.Mod.pluginVersion);

            // Emit code to add entry to H3MP's player prefab IDs
            il.Emit(OpCodes.Ldstr, playerPrefabID);
            il.Emit(OpCodes.Call, plugin.Module.ImportReference(typeof(GameManager).GetMethod("AddPlayerPrefabID")));
#endif
        }
    }
}
