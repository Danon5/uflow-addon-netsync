using UFlow.Addon.Serialization.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            Debug.Log($"Wrote RPC id {RpcTypeIdMap.GetIdAuto(typeof(T))}");
            buffer.Write(RpcTypeIdMap.GetIdAuto(typeof(T)));
            SerializationAPI.Serialize(buffer, ref rpc);
        }
    }
}