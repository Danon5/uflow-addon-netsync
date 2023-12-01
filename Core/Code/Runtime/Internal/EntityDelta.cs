using System.Collections.Generic;
using UFlow.Addon.Serialization.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class EntityDelta {
        private readonly Dictionary<byte, INetVar> m_netVars = new();

        public void Serialize(ByteBuffer buffer) {
            byte count = 0;
            foreach (var (id, netVar) in m_netVars) {
                if (netVar.IsDirty)
                    count++;
            }
            buffer.Write(count);
        }
    }
}