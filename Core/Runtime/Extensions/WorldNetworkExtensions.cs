using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.Networking.Runtime.Extensions {
    public static class WorldNetworkExtensions {
        public static NetworkSystemRunner CreateNetworkSystemRunner(this World world) {
            var systemRunner = new GameObject("NetworkSystemRunner") {
                hideFlags = HideFlags.HideInHierarchy
            }.AddComponent<NetworkSystemRunner>();
            return systemRunner;
        }
    }
}