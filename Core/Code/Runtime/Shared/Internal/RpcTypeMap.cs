using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class RpcTypeMap {
        private static readonly Dictionary<Type, ushort> s_typeToHash = new();
        private static readonly Dictionary<ushort, Type> s_hashToType = new();

        static RpcTypeMap() {
            var initialHandshakeRpcType = typeof(InitialHandshakeRpc);
            s_typeToHash.Add(initialHandshakeRpcType, 0);
            s_hashToType.Add(0, initialHandshakeRpcType);
            ushort nextId = 1;
            foreach (var type in UFlowUtils.Reflection.GetInheritors<INetRpc>()) {
                if (type == initialHandshakeRpcType) continue;
                var hash = nextId++;
                s_typeToHash[type] = hash;
                s_hashToType[hash] = type;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetHash(in Type type) => s_typeToHash[type];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(ushort hash) => s_hashToType[hash];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Type> GetRegisteredTypesEnumerable() => s_typeToHash.Keys;

        internal static IEnumerable<(Type, ushort)> GetTypeHashPairsEnumerable() {
            foreach (var (type, hash) in s_typeToHash)
                yield return (type, hash);
        }
    }
}