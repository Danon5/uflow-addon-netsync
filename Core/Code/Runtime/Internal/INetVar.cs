using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal interface INetVar {
        ushort NetId { get; }
        byte VarId { get; }
        bool IsDirty { get; }
        bool Interpolate { get; }
        void Serialize(ByteBuffer buffer);
        void Deserialize(ByteBuffer buffer);
    }
}