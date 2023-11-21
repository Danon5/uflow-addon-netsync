using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class RpcTypeIdMap {
        private static readonly Dictionary<Type, ulong> s_localTypeToHashMap = new();
        private static readonly Dictionary<ulong, Type> s_localHashToTypeMap = new();
        private static readonly Dictionary<Type, ushort> s_networkTypeToIdMap = new();
        private static readonly Dictionary<ushort, Type> s_networkIdToTypeMap = new();
        private static ushort s_networkNextId = 1;
        private static bool s_initialized;

        static RpcTypeIdMap() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        public static void RegisterAllLocalRpcsIfRequired() {
            if (s_initialized) return;
            foreach (var type in UFlowUtils.Reflection.GetInheritors<INetRpc>(false, UFlowUtils.Reflection.CommonExclusionNamespaces))
                RegisterLocalRpc(type);
            s_initialized = true;
        }

        public static void ServerRegisterNetworkRpcs() {
            foreach (var (hash, type) in s_localHashToTypeMap)
                RegisterNetworkRpc(hash, s_networkNextId++);
        }

        public static int GetNetworkRpcCount() => s_networkTypeToIdMap.Count;

        public static IEnumerable<(ulong, ushort)> GetNetworkRpcsEnumerable() {
            foreach (var (type, id) in s_networkTypeToIdMap)
                yield return (s_localTypeToHashMap[type], id);
        }

        public static void ClearNetworkRpcs() {
            s_localTypeToHashMap.Clear();
            s_localHashToTypeMap.Clear();
            s_networkNextId = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetTypeFromNetworkId(ushort id) => s_networkIdToTypeMap[id];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetNetworkIdFromType(Type type) => s_networkTypeToIdMap[type];

        public static void RegisterNetworkRpc(ulong hash, ushort id) {
            var type = s_localHashToTypeMap[hash];
            s_networkTypeToIdMap[type] = id;
            s_networkIdToTypeMap[id] = type;
        }
        
        private static void RegisterLocalRpc(Type type) {
            var hash = SerializationAPI.CalculateHash(type.Name);
            s_localTypeToHashMap[type] = hash;
            s_localHashToTypeMap[hash] = type;
        }

        private static void ClearStaticCache() {
            s_localTypeToHashMap.Clear();
            s_localHashToTypeMap.Clear();
            s_networkTypeToIdMap.Clear();
            s_networkIdToTypeMap.Clear();
            s_networkNextId = 1;
        }
    }
}