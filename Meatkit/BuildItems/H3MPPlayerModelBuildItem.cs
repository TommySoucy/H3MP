using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using H3MP;

namespace MeatKit
{
    [CreateAssetMenu(menuName = "MeatKit/Build Items/H3MP Player Model", fileName = "New build item")]
    public class H3MPPlayerModelBuildItem : BuildItem
    {
        [Tooltip("The ID of your player body's prefab")]
        public string playerPrefabID;

        public override IEnumerable<string> RequiredDependencies
        {
            get { return new[] { "VIP-H3MP-1.7.0" }; }
        }

        public override void GenerateLoadAssets(TypeDefinition plugin, ILProcessor il)
        {
#if H3VR_IMPORTED
            EnsurePluginDependsOn(plugin, H3MP.Mod.pluginGuid, H3MP.Mod.pluginVersion);

            // Emit code to add entry to H3MP's player prefab IDs
            TypeReference gameManager = new TypeReference("H3MP", "GameManager", null, null);
            il.Emit(OpCodes.Ldstr, playerPrefabID);
            il.Emit(OpCodes.Call, gameManager.Module.ImportReference(typeof(GameManager).GetMethod("AddPlayerPrefabID")));
#endif
        }
    }
}