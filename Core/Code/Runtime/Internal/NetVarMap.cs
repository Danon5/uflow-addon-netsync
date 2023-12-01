using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetVarMap {
        private readonly Dictionary<ushort, NetVarBag> m_map = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ushort netId, INetVar netVar) {
            if (!m_map.TryGetValue(netId, out var bag)) {
                bag = new NetVarBag();
                m_map.Add(netId, bag);
            }
            bag.Add(netVar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ushort netId, INetVar netVar) => m_map[netId].Remove(netVar);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ushort netId, byte varId) => m_map[netId].Remove(varId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAll(ushort netId) => m_map[netId].Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public INetVar Get(ushort netId, byte varId) => m_map[netId].Get(varId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(ushort netId, byte varId, out INetVar netVar) {
            netVar = default;
            return m_map.TryGetValue(netId, out var bag) && bag.TryGet(varId, out netVar);
        }

        public void Clear() => m_map.Clear();

        private sealed class NetVarBag {
            private readonly Dictionary<byte, INetVar> m_bag = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(INetVar netVar) => m_bag.Add(netVar.VarId, netVar);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Remove(INetVar netVar) => m_bag.Remove(netVar.VarId);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Remove(byte varId) => m_bag.Remove(varId);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public INetVar Get(byte varId) => m_bag[varId];
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(byte varId, out INetVar netVar) => m_bag.TryGetValue(varId, out netVar);

            public void Clear() => m_bag.Clear();
        }
    }
}