using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal interface INetVar {
        bool IsDirty { get; }
        void Serialize(ByteBuffer buffer);
        void Deserialize(ByteBuffer buffer);
    }
}