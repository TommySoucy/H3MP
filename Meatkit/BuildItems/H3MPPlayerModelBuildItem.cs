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
        // The item ID of the player model
        public string playerModelID;

        public override IEnumerable<string> RequiredDependencies
        {
            get { return new[] { "VIP-H3MP-1.6.7" }; }
        }

        public override void GenerateLoadAssets(TypeDefinition plugin, ILProcessor il)
        {
#if H3VR_IMPORTED
            EnsurePluginDependsOn(plugin, H3MP.Mod.pluginGuid, H3MP.Mod.pluginVersion);

            // Emit code to add entry to H3MP's player model IDs
            TypeReference gameManager = new TypeReference("H3MP", "GameManager", null, null);
            il.Emit(OpCodes.Ldstr, playerModelID);
            il.Emit(OpCodes.Call, gameManager.Module.ImportReference(typeof(GameManager).GetMethod("AddPlayerModelID")));
#endif
        }
    }
}