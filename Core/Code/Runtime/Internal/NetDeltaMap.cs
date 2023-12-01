using System.Collections.Generic;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetDeltaMap {
        private readonly Dictionary<NetClient, EntityDeltaMap> m_map = new();

        public void Clear() {
            
        }
    }
}