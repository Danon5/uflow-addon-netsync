using System;
using System.Collections.Generic;
using LiteNetLib;
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
                default:
                    throw new Exception($"Receiving unhandled packet {packetType}.");
            }
        }

        private static void DeserializeHandshake(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Deserializing handshake.");
#endif
            var rpcCount = buffer.ReadUShort();
            for (var i = 0; i < rpcCount; i++) {
                var hash = buffer.ReadULong();
                var id = buffer.ReadUShort();
                RpcTypeIdMap.RegisterNetworkRpc(hash, id);
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
            Debug.Log("Deserializing handshake response.");
#endif
        }

        private static void DeserializeRpc(ByteBuffer buffer, NetPeer peer) {
            var id = buffer.ReadUShort();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Deserializing rpc {RpcTypeIdMap.GetTypeFromNetworkId(id).Name}.");
#endif
            if (!s_deserializeRpcDelegates.TryGetValue(id, out var @delegate)) {
                @delegate = typeof(RpcDeserializer<>)
                    .MakeGenericType(RpcTypeIdMap.GetTypeFromNetworkId(id))
                    .GetMethod("DeserializeRpcInternal")!
                    .CreateDelegate(typeof(DeserializeRpcDelegate)) as DeserializeRpcDelegate;
                s_deserializeRpcDelegates.Add(id, @delegate);
            }
            @delegate!.Invoke(buffer, peer);
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

            private static void ClearStaticCache() {
                ClientRpcDeserializedEvent = default;
                ServerRpcDeserializedEvent = default;
            }
        } 
    }
}