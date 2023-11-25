using UFlow.Addon.NetSync.Core.Runtime;
using UnityEditor;

namespace UFlowAddons.Addons.NetSync.Core.Code {
    internal sealed class PrefabCacheAssetPostprocessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets,
                                                   string[] deletedAssets,
                                                   string[] movedAssets,
                                                   string[] movedFromAssetPaths) => 
            EditorApplication.delayCall += NetSyncPrefabCache.FullRefreshEditorOnly;
    }
}