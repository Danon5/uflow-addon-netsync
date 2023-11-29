using System.Collections.Generic;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetClientAwarenessMap {
        private readonly Dictionary<ushort, HashSet<ushort>> m_cache = new();

        public void AddClientCache(NetClient client) => m_cache.Add(client.id, new HashSet<ushort>());
        
        public void AddClientCache(ushort clientId) => m_cache.Add(clientId, new HashSet<ushort>());

        public void RemoveClientCache(NetClient client) => m_cache.Remove(client.id);
        
        public void RemoveClientCache(ushort clientId) => m_cache.Remove(clientId);

        public void Clear() => m_cache.Clear();

        public IEnumerable<ushort> GetIdsClientIsAwareOfEnumerable(NetClient client) => m_cache[client.id];
        
        public IEnumerable<ushort> GetIdsClientIsAwareOfEnumerable(ushort clientId) => m_cache[clientId];

        public bool ClientIsAwareOf(NetClient client, ushort id) => m_cache[client.id].Contains(id);
        
        public bool ClientIsAwareOf(ushort clientId, ushort id) => m_cache[clientId].Contains(id);

        public void MarkClientAware(NetClient client, ushort id) => m_cache[client.id].Add(id);
        
        public void MarkClientAware(ushort clientId, ushort id) => m_cache[clientId].Add(id);
        
        public void MarkClientUnaware(NetClient client, ushort id) => m_cache[client.id].Remove(id);
        
        public void MarkClientUnaware(ushort clientId, ushort id) => m_cache[clientId].Remove(id);
    }
}