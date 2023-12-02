using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetAwarenessMap {
        private readonly EntityAwarenessMap m_entityAwarenessMap = new();

        public EntityAwarenessMap GetEntityAwarenessMap() => m_entityAwarenessMap;
        
        public void Clear() => m_entityAwarenessMap.Clear();

        public abstract class BaseAwarenessMap<TValue> {
            private readonly Dictionary<ushort, HashSet<TValue>> m_map = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void MakeClientAwareOf(ushort clientId, TValue value) {
                if (!m_map.TryGetValue(clientId, out var set)) {
                    set = new HashSet<TValue>();
                    m_map.Add(clientId, set);
                }
                set.Add(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void MakeClientAwareOf(NetClient client, TValue value) =>
                MakeClientAwareOf(client.id, value); 
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void MakeClientUnawareOf(ushort clientId, TValue value) {
                if (!m_map.TryGetValue(clientId, out var set))
                    return;
                set.Remove(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void MakeClientUnawareOf(NetClient client, TValue value) => 
                MakeClientAwareOf(client.id, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool ClientIsAwareOf(ushort clientId, TValue value) =>
                m_map.TryGetValue(clientId, out var set) && set.Contains(value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool ClientIsAwareOf(NetClient client, TValue value) => 
                ClientIsAwareOf(client.id, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void RemoveClient(ushort clientId) =>
                m_map.Remove(clientId);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void RemoveClient(NetClient client) => 
                RemoveClient(client.id);
            
            public virtual void Clear() => m_map.Clear();

            public IEnumerable<TValue> AsEnumerable(ushort clientId) => m_map[clientId];
        }

        public sealed class EntityAwarenessMap : BaseAwarenessMap<ushort> {
            
        }
    }
}