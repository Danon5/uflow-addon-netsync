using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class RpcTypeMap {
        private static readonly Dictionary<Type, ulong> s_typeToHash = new();
        private static readonly Dictionary<ulong, Type> s_hashToType = new();
        private static bool s_initialized;

        static RpcTypeMap() {
            foreach (var type in UFlowUtils.Reflection.GetInheritors<INetRpc>()) {
                var hash = SerializationAPI.CalculateHash(type.ToString());
                s_typeToHash[type] = hash;
                s_hashToType[hash] = type;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetHash(in Type type) => s_typeToHash[type];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(ulong hash) => s_hashToType[hash];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Type> GetRegisteredTypesEnumerable() => s_typeToHash.Keys;
    }
}