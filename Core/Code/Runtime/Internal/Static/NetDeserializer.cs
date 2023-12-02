using System;
using System.Collections.Generic;
using LiteNetLib;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;
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
                default:
                    throw new Exception($"Receiving unhandled packet. PacketType: {packetType}");
            }
        }

        private static void DeserializeHandshake(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Deserializing Handshake.");
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
                    Debug.LogWarning($"Client failing to deserialize prefab with hash {hash} because cache is null.");
#endif
                    continue;
                }
                if (!prefabCache.HasLocalHash(hash)) {
#if UFLOW_DEBUG_ENABLED
                    Debug.LogWarning($"Client failing to deserialize prefab with hash {hash} because it has no matching local hash.");
#endif
                    continue;
                }
                prefabCache.RegisterNetworkPrefab(hash, id);
            }
        }

        private static void DeserializeHandshakeResponse(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Deserializing HandshakeResponse.");
#endif
        }

        private static void DeserializeRpc(ByteBuffer buffer, NetPeer peer) {
            var id = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Deserializing RPC: {NetTypeIdMaps.RpcMap.GetTypeFromNetworkId(id).Name}");
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
            if (NetSyncModule.InternalSingleton == null) return;
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Creating network entity. NetID: {netId}");
#endif
            NetSyncModule.InternalSingleton.World.CreateEntity().Set(new NetSynchronize {
                netId = netId
            });
        }
        
        private static void DeserializeCreateSceneEntity(ByteBuffer buffer) {
            var netId = buffer.ReadUShort();
            var prefabId = buffer.ReadUShort();
            if (NetSyncModule.InternalSingleton == null) return;
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Creating network scene entity. NetID: {netId}, PrefabID: {prefabId}");
#endif
            var entity = NetSyncPrefabCache.Get().GetPrefabFromNetworkId(prefabId).Instantiate().AsEntity(true);
            entity.Set(new NetSynchronize {
                netId = netId
            });
            entity.Get<SceneEntityRef>().value.BakeAndFinalize();
        }
        
        private static void DeserializeDestroyEntity(ByteBuffer buffer) {
            var netId = buffer.ReadUShort();
            if (NetSyncModule.InternalSingleton == null) return;
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Destroying network entity. NetID: {netId}");
#endif
            NetSyncAPI.GetEntityFromNetId(netId).Destroy();
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