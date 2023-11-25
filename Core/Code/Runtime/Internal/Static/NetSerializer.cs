using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeHandshake(ByteBuffer buffer) {
            buffer.Write((ushort)RpcTypeIdMap.GetNetworkRpcCount());
            foreach (var (hash, id) in RpcTypeIdMap.GetNetworkRpcsHashToIdEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
            var prefabCache = NetSyncPrefabCache.Get();
            if (prefabCache == null) {
                buffer.Write((ushort)0);
                return;
            }
            buffer.Write((ushort)prefabCache.LocalPrefabCount);
            foreach (var (hash, id) in prefabCache.GetNetworkPrefabHashToIdEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
        }
        
        public static void SerializeHandshakeResponse(ByteBuffer buffer) { }
        
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            buffer.Write(RpcTypeIdMap.GetNetworkIdFromType(typeof(T)));
            SerializationAPI.Serialize(buffer, ref rpc);
        }
    }
}