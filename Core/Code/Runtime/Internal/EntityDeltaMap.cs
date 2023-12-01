using System.Collections.Generic;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class EntityDeltaMap {
        private readonly Dictionary<Entity, EntityDelta> m_map = new();
        
    }
}