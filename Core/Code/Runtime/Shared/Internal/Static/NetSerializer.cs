using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeHandshake(ByteBuffer buffer) {
            buffer.Write((ushort)RpcTypeIdMap.ServerGetTypeCount());
            foreach (var (id, type) in RpcTypeIdMap.ServerGetMapEnumerable()) {
                buffer.Write(id);
                buffer.Write(type.AssemblyQualifiedName);
            }
        }
        
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            buffer.Write(RpcTypeIdMap.GetIdAuto(typeof(T)));
            SerializationAPI.Serialize(buffer, ref rpc);
        }
    }
}