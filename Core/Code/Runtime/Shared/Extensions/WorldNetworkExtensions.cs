using UFlow.Addon.Ecs.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Runtime {
    public static class WorldNetworkExtensions {
        public static NetSyncSystemRunner CreateNetworkSystemRunner(this World world) {
            var systemRunner = new GameObject("NetworkSystemRunner") {
                hideFlags = HideFlags.HideInHierarchy
            }.AddComponent<NetSyncSystemRunner>();
            return systemRunner;
        }
    }
}