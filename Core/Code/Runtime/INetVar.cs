using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public interface INetVar {
        internal ushort NetId { get; }
        internal byte VarId { get; }
        internal bool IsDirty { get; }
        internal bool Interpolate { get; }
        internal void Serialize(ByteBuffer buffer);
        internal void Deserialize(ByteBuffer buffer);
    }
}