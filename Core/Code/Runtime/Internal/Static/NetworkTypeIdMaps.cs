using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetworkTypeIdMaps {
        public static NetworkTypeIdMap<INetRpc> RpcMap { get; } = new();
        public static NetworkTypeIdMap<IEcsNetComponent> ComponentMap { get; } = new();

        static NetworkTypeIdMaps() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        private static void ClearStaticCache() {
            RpcMap.ClearNetworkCaches();
            ComponentMap.ClearNetworkCaches();
        }
    }
}