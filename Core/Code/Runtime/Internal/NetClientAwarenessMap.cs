using System.Collections.Generic;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetClientAwarenessMap {
        private readonly Dictionary<ushort, HashSet<ushort>> m_map = new();

        public void AddClientCache(NetClient client) => m_map.Add(client.id, new HashSet<ushort>());
        
        public void AddClientCache(ushort clientId) => m_map.Add(clientId, new HashSet<ushort>());

        public void RemoveClientCache(NetClient client) => m_map.Remove(client.id);
        
        public void RemoveClientCache(ushort clientId) => m_map.Remove(clientId);

        public void Clear() => m_map.Clear();

        public IEnumerable<ushort> GetIdsClientIsAwareOfEnumerable(NetClient client) => m_map[client.id];
        
        public IEnumerable<ushort> GetIdsClientIsAwareOfEnumerable(ushort clientId) => m_map[clientId];

        public bool ClientIsAwareOf(NetClient client, ushort id) => m_map[client.id].Contains(id);
        
        public bool ClientIsAwareOf(ushort clientId, ushort id) => m_map[clientId].Contains(id);

        public void MarkClientAware(NetClient client, ushort id) => m_map[client.id].Add(id);
        
        public void MarkClientAware(ushort clientId, ushort id) => m_map[clientId].Add(id);
        
        public void MarkClientUnaware(NetClient client, ushort id) => m_map[client.id].Remove(id);
        
        public void MarkClientUnaware(ushort clientId, ushort id) => m_map[clientId].Remove(id);
    }
}