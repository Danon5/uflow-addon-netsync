using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        private static readonly ByteBuffer s_tempBuffer = new();
        private static byte s_numDeltaTypesInTempBuffer;

        static NetSerializer() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;
        
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

        public static bool TrySerializeDeltaEntityStateIntoTempBuffer(in Entity entity, ushort netId) {
            s_tempBuffer.Reset();
            s_numDeltaTypesInTempBuffer = 0;
            var stateMaps = NetSyncModule.InternalSingleton.StateMaps;
            if (!stateMaps.TryGetEntityState(netId, out var entityState)) return false;
            s_tempBuffer.Write(netId);
            if (entityState.EnabledStateDirty) {
                s_tempBuffer.Write((byte)(entity.IsEnabled() ? 
                    NetDeltaType.EntityEnabled : NetDeltaType.EntityDisabled));
                s_numDeltaTypesInTempBuffer++;
            }
            foreach (var (compId, componentState) in entityState.AsEnumerable()) {
                if (componentState.EnabledStateDirty) {
                    var componentType = NetTypeIdMaps.ComponentMap.GetTypeFromNetworkId(compId);
                    s_tempBuffer.Write((byte)(entity.IsEnabledRaw(componentType) ?
                        NetDeltaType.ComponentEnabled : NetDeltaType.ComponentDisabled));
                    s_tempBuffer.Write(compId);
                    s_numDeltaTypesInTempBuffer++;
                }
                foreach (var (varId, netVar) in componentState.AsEnumerable()) {
                    
                }
            }
            return s_numDeltaTypesInTempBuffer > 0;
        }

        public static void SerializeDeltaEntityState(ByteBuffer buffer) {
            buffer.Write(s_numDeltaTypesInTempBuffer);
            s_tempBuffer.AppendBufferTo(buffer);
        }

        private static void SerializeInitialEntityState(ByteBuffer buffer, in Entity entity, ushort netId) {
            buffer.Write(entity.IsEnabled());
            var stateMaps = NetSyncModule.InternalSingleton.StateMaps;
            if (!stateMaps.TryGetEntityState(netId, out var entityState)) return;
            buffer.Write((byte)entityState.Count);
            foreach (var (compId, componentState) in entityState.AsEnumerable()) {
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

        private static void ClearStaticCache() {
            s_tempBuffer.Reset();
            s_numDeltaTypesInTempBuffer = 0;
        }
    }
}