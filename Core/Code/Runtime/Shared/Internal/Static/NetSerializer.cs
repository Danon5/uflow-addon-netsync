using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetSerializer {
        public static void SerializeRpc<T>(ByteBuffer buffer, T rpc) where T : INetRpc {
            buffer.Write((byte)NetPacketType.RPC);
            SerializationAPI.Serialize(buffer, ref rpc);
        }
    }
}