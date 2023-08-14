using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class RpcMethodSubscriptionCache<T> where T : INetRpc {
        private static readonly Dictionary<RpcSendType, RpcMethodDelegate<T>> s_methods = new();

        static RpcMethodSubscriptionCache() => ExternalEngineEvents.clearStaticCachesEvent += ClearStaticCaches;

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterMethod(in RpcMethodDelegate<T> method, RpcSendType sendType) {
            if (!s_methods.ContainsKey(sendType))
                s_methods.Add(sendType, default);
            s_methods[sendType] += method;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeMethods(in T rpc, RpcSendType sendType) {
            if (s_methods.TryGetValue(sendType, out var methods))
                methods?.Invoke(rpc);
        }
        
        private static void ClearStaticCaches() => s_methods.Clear();
    }
}