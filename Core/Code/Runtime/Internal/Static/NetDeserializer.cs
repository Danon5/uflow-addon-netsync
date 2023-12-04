using System;
using System.Collections.Generic;
using LiteNetLib;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetDeserializer {
        private static readonly Dictionary<ushort, DeserializeRpcDelegate> s_deserializeRpcDelegates = new();

        public static void Deserialize(ByteBuffer buffer, NetPacketType packetType, NetPeer peer) {
            switch (packetType) {
                case NetPacketType.Handshake:
                    DeserializeHandshake(buffer);
                    NetSyncModule.InternalSingleton.Transport.ClientPeerHandshakeResponse();
                    break;
                case NetPacketType.HandshakeResponse:
                    DeserializeHandshakeResponse(buffer);
                    NetSyncModule.InternalSingleton.Transport.ServerAuthorizePeer(peer);
                    break;
                case NetPacketType.RPC:
                    DeserializeRpc(buffer, peer);
                    break;
                case NetPacketType.CreateEntity:
                    DeserializeCreateEntity(buffer);
                    break;
                case NetPacketType.CreateSceneEntity:
                    DeserializeCreateSceneEntity(buffer);
                    break;
                case NetPacketType.DestroyEntity:
                    DeserializeDestroyEntity(buffer);
                    break;
                case NetPacketType.EntityDelta:
                    DeserializeDeltaEntityState(buffer);
                    break;
                default:
                    throw new Exception($"Receiving unhandled packet. PacketType: {packetType}");
            }
        }

        private static void DeserializeHandshake(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.Handshake}.");
#endif
            var rpcCount = buffer.ReadUShort();
            for (var i = 0; i < rpcCount; i++) {
                var hash = buffer.ReadULong();
                var id = buffer.ReadUShort();
                NetTypeIdMaps.RpcMap.RegisterNetworkHash(hash, id);
            }
            var componentCount = buffer.ReadUShort();
            for (var i = 0; i < componentCount; i++) {
                var hash = buffer.ReadULong();
                var id = buffer.ReadUShort();
                NetTypeIdMaps.ComponentMap.RegisterNetworkHash(hash, id);
            }
            var prefabCount = buffer.ReadUShort();
            if (prefabCount == 0) return;
            var prefabCache = NetSyncPrefabCache.Get();
            for (var i = 0; i < prefabCount; i++) {
                var hash = buffer.ReadULong();
                var id = buffer.ReadUShort();
                if (prefabCache == null) {
#if UFLOW_DEBUG_ENABLED
                    DebugAPI.LogError($"Client failing to deserialize prefab with hash {hash} because cache is null.");
#endif
                    continue;
                }
                if (!prefabCache.HasLocalHash(hash)) {
#if UFLOW_DEBUG_ENABLED
                    DebugAPI.LogError($"Client failing to deserialize prefab with hash {hash} because it has no matching local hash.");
#endif
                    continue;
                }
                prefabCache.RegisterNetworkPrefab(hash, id);
            }
        }

        private static void DeserializeHandshakeResponse(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.HandshakeResponse}.");
#endif
        }

        private static void DeserializeRpc(ByteBuffer buffer, NetPeer peer) {
            var id = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.RPC}: {NetTypeIdMaps.RpcMap.GetTypeFromNetworkId(id).Name}");
#endif
            if (!s_deserializeRpcDelegates.TryGetValue(id, out var @delegate)) {
                @delegate = typeof(RpcDeserializer<>)
                    .MakeGenericType(NetTypeIdMaps.RpcMap.GetTypeFromNetworkId(id))
                    .GetMethod("DeserializeRpcInternal")!
                    .CreateDelegate(typeof(DeserializeRpcDelegate)) as DeserializeRpcDelegate;
                s_deserializeRpcDelegates.Add(id, @delegate);
            }
            @delegate!.Invoke(buffer, peer);
        }

        private static void DeserializeCreateEntity(ByteBuffer buffer) {
            var netId = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.CreateEntity}. NetID: {netId}");
#endif
            if (NetSyncModule.InternalSingleton == null) return;
            PublisherDelay.StartDelay();
            var entity = NetSyncModule.InternalSingleton.World.CreateEntity();
            entity.Set(new NetSynchronize {
                netId = netId
            });
            DeserializeInitialEntityState(buffer, entity, netId);
            PublisherDelay.EndDelay();
        }
        
        private static void DeserializeCreateSceneEntity(ByteBuffer buffer) {
            var netId = buffer.ReadUShort();
            var prefabId = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.CreateSceneEntity}. NetID: {netId}, PrefabID: {prefabId}");
#endif
            if (NetSyncModule.InternalSingleton == null) return;
            PublisherDelay.StartDelay();
            var entity = NetSyncPrefabCache.Get().GetPrefabFromNetworkId(prefabId).Instantiate().AsEntity();
            entity.Set(new NetSynchronize {
                netId = netId
            });
            DeserializeInitialEntityState(buffer, entity, netId);
            PublisherDelay.EndDelay();
        }
        
        private static void DeserializeDestroyEntity(ByteBuffer buffer) {
            var netId = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.DestroyEntity}. NetID: {netId}");
#endif
            if (NetSyncModule.InternalSingleton == null) return;
            NetSyncAPI.GetEntityFromNetId(netId).Destroy();
        }

        private static void DeserializeInitialEntityState(ByteBuffer buffer, in Entity entity, ushort netId) {
            var isEnabled = buffer.ReadBool();
            var stateMaps = NetSyncModule.InternalSingleton.StateMaps;
            using var _1 = PoolingAPI.Checkout<List<ushort>>(out var networkCompIdList);
            using var _2 = PoolingAPI.Checkout<List<bool>>(out var networkCompEnabledList);
            var componentCount = buffer.ReadByte();
            for (var i = 0; i < componentCount; i++) {
                var compId = buffer.ReadUShort();
                var enabled = buffer.ReadBool();
                var varCount = buffer.ReadByte();
                var componentMapFound = stateMaps.TryGetEntityState(netId, out var entityState);
                networkCompIdList.Add(compId);
                networkCompEnabledList.Add(enabled);
                for (var j = 0; j < varCount; j++) {
                    var varId = buffer.ReadByte();
                    if (!componentMapFound) continue;
                    if (!entityState.TryGet(compId, out var componentState))
                        throw new Exception($"Failed to get ComponentState. NetID: {netId}, CompID: {compId}");
                    if (!componentState.TryGet(varId, out var netVar))
                        throw new Exception($"Failed to get NetVar. NetID: {netId}, CompID: {compId}, VarID: {varId}");
                    netVar.Deserialize(buffer);
                }
            }
            using var _3 = PoolingAPI.Checkout<List<Type>>(out var typesToRemove);
            using var _4 = PoolingAPI.Checkout<List<ushort>>(out var localCompIdList);
            foreach (var componentType in entity.ComponentTypes) {
                if (!NetTypeIdMaps.ComponentMap.TryGetNetworkIdFromType(componentType, out var compId)) continue;
                localCompIdList.Add(compId);
                if (networkCompIdList.Contains(compId)) continue;
                typesToRemove.Add(componentType);
            }
            foreach (var type in typesToRemove)
                entity.RemoveRaw(type);
            for (var i = 0; i < componentCount; i++) {
                var compId = networkCompIdList[i];
                var componentType = NetTypeIdMaps.ComponentMap.GetTypeFromNetworkId(compId);
                if (!localCompIdList.Contains(compId))
                    entity.SetRaw(componentType);
                entity.SetEnabledRaw(componentType, networkCompEnabledList[i]);
            }
            entity.SetEnabled(isEnabled);
        }

        private static void DeserializeDeltaEntityState(ByteBuffer buffer) {
            var numDeltaInstructions = buffer.ReadByte();
            var netId = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Deserializing {NetPacketType.EntityDelta}. NetID: {netId}, NumInstructions: {numDeltaInstructions}");
#endif
            if (!NetSyncModule.InternalSingleton.StateMaps.TryGetEntity(netId, out var entity))
                throw new Exception($"Receiving delta for invalid entity. NetID: {netId}");
            for (var i = 0; i < numDeltaInstructions; i++) {
                var instruction = (NetDeltaType)buffer.ReadByte();
                switch (instruction) {
                    case NetDeltaType.EntityEnabled:
                        entity.SetEnabled(true);
                        break;
                    case NetDeltaType.EntityDisabled:
                        entity.SetEnabled(false);
                        break;
                    case NetDeltaType.ComponentEnabled: {
                        var compId = buffer.ReadUShort();
                        entity.SetEnabledRaw(NetTypeIdMaps.ComponentMap.GetTypeFromNetworkId(compId), true);
                    }
                        break;
                    case NetDeltaType.ComponentDisabled: {
                        var compId = buffer.ReadUShort();
                        entity.SetEnabledRaw(NetTypeIdMaps.ComponentMap.GetTypeFromNetworkId(compId), false);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private delegate void DeserializeRpcDelegate(ByteBuffer buffer, NetPeer peer);

        internal static class RpcDeserializer<T> where T : INetRpc {
            public static event ClientRpcHandlerDelegate<T> ClientRpcDeserializedEvent;
            public static event ServerRpcHandlerDelegate<T> ServerRpcDeserializedEvent;

            static RpcDeserializer() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

            [Preserve]
            public static void DeserializeRpcInternal(ByteBuffer buffer, NetPeer peer) {
                if (ReferenceEquals(peer, NetSyncModule.InternalSingleton.Transport.ServerPeer))
                    ClientRpcDeserializedEvent?.Invoke(SerializationAPI.Deserialize<T>(buffer));
                else
                    ServerRpcDeserializedEvent?.Invoke(SerializationAPI.Deserialize<T>(buffer),
                        NetSyncModule.InternalSingleton.Transport.GetClient(peer));
            }

            internal static void InvokeClientRpcDeserializedDirect(in T rpc) => 
                ClientRpcDeserializedEvent?.Invoke(rpc);

            internal static void InvokeServerRpcDeserializedDirect(in T rpc, NetClient client) => 
                ServerRpcDeserializedEvent?.Invoke(rpc, client);

            private static void ClearStaticCache() {
                ClientRpcDeserializedEvent = default;
                ServerRpcDeserializedEvent = default;
            }
        }
    }
}