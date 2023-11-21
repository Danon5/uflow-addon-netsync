using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeHandshake(ByteBuffer buffer) {
            buffer.Write((ushort)RpcTypeIdMap.GetNetworkRpcCount());
            foreach (var (hash, id) in RpcTypeIdMap.GetNetworkRpcsEnumerable()) {
                buffer.Write(hash);
                buffer.Write(id);
            }
        }
        
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            buffer.Write(RpcTypeIdMap.GetNetworkIdFromType(typeof(T)));
            SerializationAPI.Serialize(buffer, ref rpc);
        }
    }
}