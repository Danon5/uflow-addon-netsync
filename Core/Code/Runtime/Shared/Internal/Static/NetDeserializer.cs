using System;
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
                    Debug.Log("Handshake received");
                    break;
                case NetPacketType.RPC:
                    var id = buffer.ReadUShort();
                    Debug.Log($"Read RPC id {id}");
                    if (!s_deserializeRpcDelegates.TryGetValue(id, out var @delegate)) {
                        @delegate = typeof(RpcDeserializer<>)
                            .MakeGenericType(RpcTypeIdMap.GetTypeAuto(id))
                            .GetMethod("DeserializeRpc")!
                            .CreateDelegate(typeof(DeserializeRpcDelegate)) as DeserializeRpcDelegate;
                        s_deserializeRpcDelegates.Add(id, @delegate);
                        Debug.Log($"Caching delegate for rpc {RpcTypeIdMap.GetTypeAuto(id).Name}");
                    }
                    @delegate!.Invoke(buffer);
                    Debug.Log($"Rpc received with id {id}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private delegate void DeserializeRpcDelegate(ByteBuffer buffer);

        internal static class RpcDeserializer<T> where T : new() {
            public static event Action<T> RpcDeserializedEvent;

            [Preserve]
            public static void DeserializeRpc(ByteBuffer buffer) {
                RpcDeserializedEvent?.Invoke(SerializationAPI.Deserialize<T>(buffer));
                Debug.Log($"Deserialized {typeof(T)}");
            }
        } 
    }
}