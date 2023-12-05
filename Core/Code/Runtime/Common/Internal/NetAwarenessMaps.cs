using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetAwarenessMaps {
        private readonly Dictionary<ushort, EntityAwarenessMap> m_entityAwarenessMaps = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClientIsAwareOf(ushort clientId, ushort netId) =>
            m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap) && 
            entityAwarenessMap.ClientIsAwareOf(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientAwareOf(ushort clientId, ushort netId) =>
            GetOrCreateEntityAwarenessMap(clientId)
                .MakeClientAwareOf(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientUnawareOf(ushort clientId, ushort netId) {
            if (!m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap)) return;
            entityAwarenessMap.MakeClientUnawareOf(netId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClientIsAwareOf(ushort clientId, ushort netId, ushort compId) =>
            m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap) && 
            entityAwarenessMap.ClientIsAwareOf(netId) &&
            entityAwarenessMap.TryGetComponentAwarenessMap(netId, out var componentAwarenessMap) &&
            componentAwarenessMap.ClientIsAwareOf(compId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientAwareOf(ushort clientId, ushort netId, ushort compId) => 
            GetOrCreateEntityAwarenessMap(clientId)
                .GetOrCreateComponentAwarenessMap(netId)
                .MakeClientAwareOf(compId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientUnawareOf(ushort clientId, ushort netId, ushort compId) {
            if (!m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap)) return;
            if (!entityAwarenessMap.TryGetComponentAwarenessMap(netId, out var componentAwarenessMap)) return;
            componentAwarenessMap.MakeClientUnawareOf(compId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClientIsAwareOf(ushort clientId, ushort netId, ushort compId, byte varId) =>
            m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap) && 
            entityAwarenessMap.ClientIsAwareOf(netId) &&
            entityAwarenessMap.TryGetComponentAwarenessMap(netId, out var componentAwarenessMap) &&
            componentAwarenessMap.ClientIsAwareOf(compId) &&
            componentAwarenessMap.TryGetVarAwarenessMap(compId, out var varAwarenessMap) &&
            varAwarenessMap.ClientIsAwareOf(varId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientAwareOf(ushort clientId, ushort netId, ushort compId, byte varId) => 
            GetOrCreateEntityAwarenessMap(clientId)
                .GetOrCreateComponentAwarenessMap(netId)
                .GetOrCreateVarAwarenessMap(compId)
                .MakeClientAwareOf(varId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MakeClientUnawareOf(ushort clientId, ushort netId, ushort compId, byte varId) {
            if (!m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap)) return;
            if (!entityAwarenessMap.TryGetComponentAwarenessMap(netId, out var componentAwarenessMap)) return;
            if (!componentAwarenessMap.TryGetVarAwarenessMap(compId, out var varAwarenessMap)) return;
            varAwarenessMap.MakeClientUnawareOf(varId);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveClientMaps(ushort clientId) =>
            m_entityAwarenessMaps.Remove(clientId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => m_entityAwarenessMaps.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClientShouldBeAwareOf(NetClient client, ushort netId) => true;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClientShouldBeAwareOf(NetClient client, ushort netId, ushort compId) => 
            ClientShouldBeAwareOf(client, netId) && true;

        private EntityAwarenessMap GetOrCreateEntityAwarenessMap(ushort clientId) {
            if (m_entityAwarenessMaps.TryGetValue(clientId, out var entityAwarenessMap)) 
                return entityAwarenessMap;
            entityAwarenessMap = new EntityAwarenessMap();
            m_entityAwarenessMaps.Add(clientId, entityAwarenessMap);
            return entityAwarenessMap;
        }

        public abstract class BaseAwarenessMap<TId> {
            private readonly HashSet<TId> m_hashSet = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool ClientIsAwareOf(TId id) => m_hashSet.Contains(id);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool MakeClientAwareOf(TId id) => m_hashSet.Add(id);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool MakeClientUnawareOf(TId id) => m_hashSet.Remove(id);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void RemoveAllAwareness() => m_hashSet.Clear();
        }
        
        public sealed class EntityAwarenessMap : BaseAwarenessMap<ushort> {
            private readonly Dictionary<ushort, ComponentAwarenessMap> m_componentAwarenessMaps = new();

            public override bool MakeClientUnawareOf(ushort id) {
                if (m_componentAwarenessMaps.TryGetValue(id, out var componentAwarenessMap))
                    componentAwarenessMap.Clear();
                return base.MakeClientUnawareOf(id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComponentAwarenessMap GetComponentAwarenessMap(ushort netId) => 
                m_componentAwarenessMaps[netId];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetComponentAwarenessMap(ushort netId, out ComponentAwarenessMap componentAwarenessMap) =>
                m_componentAwarenessMaps.TryGetValue(netId, out componentAwarenessMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComponentAwarenessMap GetOrCreateComponentAwarenessMap(ushort netId) {
                if (m_componentAwarenessMaps.TryGetValue(netId, out var componentAwarenessMap)) 
                    return componentAwarenessMap;
                componentAwarenessMap = new ComponentAwarenessMap();
                m_componentAwarenessMaps.Add(netId, componentAwarenessMap);
                return componentAwarenessMap;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() {
                foreach (var (_, componentAwarenessMap) in m_componentAwarenessMaps) 
                    componentAwarenessMap.Clear();
                RemoveAllAwareness();
            }
        }

        public sealed class ComponentAwarenessMap : BaseAwarenessMap<ushort> {
            private readonly Dictionary<ushort, VarAwarenessMap> m_varAwarenessMaps = new();

            public override bool MakeClientUnawareOf(ushort id) {
                if (m_varAwarenessMaps.TryGetValue(id, out var varAwarenessMap))
                    varAwarenessMap.Clear();
                return base.MakeClientUnawareOf(id);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VarAwarenessMap GetVarAwarenessMap(ushort compId) => 
                m_varAwarenessMaps[compId];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetVarAwarenessMap(ushort compId, out VarAwarenessMap componentAwarenessMap) =>
                m_varAwarenessMaps.TryGetValue(compId, out componentAwarenessMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VarAwarenessMap GetOrCreateVarAwarenessMap(ushort compId) {
                if (m_varAwarenessMaps.TryGetValue(compId, out var componentAwarenessMap)) 
                    return componentAwarenessMap;
                componentAwarenessMap = new VarAwarenessMap();
                m_varAwarenessMaps.Add(compId, componentAwarenessMap);
                return componentAwarenessMap;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() {
                foreach (var (_, varAwarenessMap) in m_varAwarenessMaps)
                    varAwarenessMap.Clear();
                m_varAwarenessMaps.Clear();
            }
        }

        public sealed class VarAwarenessMap : BaseAwarenessMap<byte> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() => RemoveAllAwareness();
        }
    }
}