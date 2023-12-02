using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class GenericNetTypeIdMap<T> {
        private readonly Dictionary<ulong, Type> m_hashToTypeMap = new();
        private readonly Dictionary<Type, ulong> m_typeToHashMap = new();
        private readonly Dictionary<ushort, Type> m_idToTypeMap = new();
        private readonly Dictionary<Type, ushort> m_typeToIdMap = new();
        private bool m_locallyInitialized;

        public void InitializeLocallyIfRequired() {
            if (m_locallyInitialized) return;
            foreach (var type in UFlowUtils.Reflection.GetInheritors<T>(false, UFlowUtils.Reflection.CommonExclusionNamespaces))
                RegisterLocalType(type);
            m_locallyInitialized = true;
        }

        public void InitializeServer() {
            ushort nextId = 1;
            foreach (var (hash, type) in m_hashToTypeMap)
                RegisterNetworkHash(hash, nextId++);
        }

        public int GetNetworkRegisteredCount() => m_typeToIdMap.Count;

        public IEnumerable<(ulong, ushort)> GetNetworkHashToIdEnumerable() {
            foreach (var (type, id) in m_typeToIdMap)
                yield return (m_typeToHashMap[type], id);
        }
        
        public void ClearAllCaches() {
            m_hashToTypeMap.Clear();
            m_typeToHashMap.Clear();
            m_idToTypeMap.Clear();
            m_typeToIdMap.Clear();
            m_locallyInitialized = false;
        }

        public void ClearNetworkCaches() {
            m_idToTypeMap.Clear();
            m_typeToIdMap.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetTypeFromNetworkId(ushort id) => m_idToTypeMap[id];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetNetworkIdFromType(Type type) => m_typeToIdMap[type];

        public void RegisterNetworkHash(ulong hash, ushort id) {
            var type = m_hashToTypeMap[hash];
            m_idToTypeMap[id] = type;
            m_typeToIdMap[type] = id;
            Debug.Log($"{typeof(T).Name}: Registering {GetTypeFromNetworkId(id).Name} as {hash} - {id}");
        }
        
        private void RegisterLocalType(Type type) {
            var hash = SerializationAPI.CalculateHash(type.Name);
            m_hashToTypeMap[hash] = type;
            m_typeToHashMap[type] = hash;
        }
    }
}