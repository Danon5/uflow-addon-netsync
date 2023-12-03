using UFlow.Addon.ECS.Core.Runtime;
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

        public static void SerializeCreateEntity(ByteBuffer buffer, in Entity entity, ushort netId) {
            buffer.Write(netId);
            SerializeInitialEntityState(buffer, entity, netId);
        }
        
        public static void SerializeCreateSceneEntity(ByteBuffer buffer, in Entity entity, ushort netId, ushort prefabId) {
            buffer.Write(netId);
            buffer.Write(prefabId);
            SerializeInitialEntityState(buffer, entity, netId);
        }
        
        public static void SerializeDestroyEntity(ByteBuffer buffer, ushort netId) {
            buffer.Write(netId);
        }

        private static void SerializeInitialEntityState(ByteBuffer buffer, in Entity entity, ushort netId) {
            buffer.Write(entity.IsEnabled());
            var stateMaps = NetSyncModule.InternalSingleton.StateMaps;
            if (!stateMaps.TryGetComponentStateMap(netId, out var componentStateMap)) return;
            buffer.Write((byte)componentStateMap.Count);
            foreach (var (compId, componentState) in componentStateMap.AsEnumerable()) {
                var componentType = NetTypeIdMaps.ComponentMap.GetTypeFromNetworkId(compId);
                var enabled = entity.IsEnabledRaw(componentType);
                buffer.Write(compId);
                buffer.Write(enabled);
                buffer.Write((byte)componentState.Count);
                foreach (var (varId, netVar) in componentState.AsEnumerable()) {
                    buffer.Write(varId);
                    netVar.Serialize(buffer);
                }
            }
        }
    }
}