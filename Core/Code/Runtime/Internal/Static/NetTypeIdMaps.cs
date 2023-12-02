using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetTypeIdMaps {
        public static GenericNetTypeIdMap<INetRpc> RpcMap { get; } = new();
        public static GenericNetTypeIdMap<IEcsNetComponent> ComponentMap { get; } = new();

        static NetTypeIdMaps() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        private static void ClearStaticCache() {
            RpcMap.ClearAllCaches();
            ComponentMap.ClearAllCaches();
        }
    }
}