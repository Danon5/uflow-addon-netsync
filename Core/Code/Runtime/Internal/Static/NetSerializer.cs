using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeHandshake(ByteBuffer buffer) {
            buffer.Write((ushort)NetTypeIdMaps.RpcMap.GetNetworkRegisteredCount());
            foreach (var (hash, id) in NetTypeIdMaps.RpcMap.GetNetworkHashToIdEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
            buffer.Write((ushort)NetTypeIdMaps.ComponentMap.GetNetworkRegisteredCount());
            foreach (var (hash, id) in NetTypeIdMaps.ComponentMap.GetNetworkHashToIdEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
            var prefabCache = NetSyncPrefabCache.Get();
            if (prefabCache == null) {
                buffer.Write((ushort)0);
                return;
            }
            buffer.Write((ushort)prefabCache.NetworkPrefabCount);
            foreach (var (hash, id) in prefabCache.GetNetworkPrefabHashToIdEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
        }
        
        public static void SerializeHandshakeResponse(ByteBuffer buffer) { }
        
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            buffer.Write(NetTypeIdMaps.RpcMap.GetNetworkIdFromType(typeof(T)));
            SerializationAPI.Serialize(buffer, ref rpc);
        }

        public static void SerializeCreateEntity(ByteBuffer buffer, ushort netId) {
            buffer.Write(netId);
        }
        
        public static void SerializeCreateSceneEntity(ByteBuffer buffer, ushort netId, ushort prefabId) {
            buffer.Write(netId);
            buffer.Write(prefabId);
        }
        
        public static void SerializeDestroyEntity(ByteBuffer buffer, ushort netId) {
            buffer.Write(netId);
        }
    }
}