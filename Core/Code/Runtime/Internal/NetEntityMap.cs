using System.Collections.Generic;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetEntityMap {
        private readonly Dictionary<ushort, Entity> m_cache = new();

        public void Add(ushort netId, in Entity entity) => m_cache.Add(netId, entity);

        public void Remove(ushort netId) => m_cache.Remove(netId);

        public Entity Get(ushort netId) => m_cache[netId];

        public bool TryGet(ushort netId, out Entity entity) => m_cache.TryGetValue(netId, out entity);
        
        public void Clear() => m_cache.Clear();
    }
}