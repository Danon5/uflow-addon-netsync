﻿using System;
using System.Collections.Generic;
using UFlow.Addon.Serialization.Core.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetDeserializer {
        private static readonly Dictionary<ushort, DeserializeRpcDelegate> s_deserializeRpcDelegates = new();

        public static void Deserialize(ByteBuffer buffer, NetPacketType packetType) {
            switch (packetType) {
                case NetPacketType.Handshake:
                    DeserializeHandshake(buffer);
                    break;
                case NetPacketType.RPC:
                    DeserializeRpc(buffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DeserializeHandshake(ByteBuffer buffer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Deserializing handshake.");
#endif
            var count = buffer.ReadUShort();
            for (var i = 0; i < count; i++) {
                var hash = buffer.ReadULong();
                var id = buffer.ReadUShort();
                RpcTypeIdMap.RegisterNetworkRpc(hash, id);
            }
        }

        private static void DeserializeRpc(ByteBuffer buffer) {
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
            @delegate!.Invoke(buffer);
        }

        private delegate void DeserializeRpcDelegate(ByteBuffer buffer);

        internal static class RpcDeserializer<T> where T : new() {
            public static event Action<T> RpcDeserializedEvent;

            [Preserve]
            public static void DeserializeRpcInternal(ByteBuffer buffer) {
                RpcDeserializedEvent?.Invoke(SerializationAPI.Deserialize<T>(buffer));
            }
        } 
    }
}