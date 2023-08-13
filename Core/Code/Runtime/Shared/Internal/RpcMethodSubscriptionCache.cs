using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class RpcMethodSubscriptionCache<T> where T : INetRpc {
        private static RpcMethodDelegate<T> s_methods;

        static RpcMethodSubscriptionCache() => ExternalEngineEvents.clearStaticCachesEvent += ClearStaticCaches;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterMethod(in RpcMethodDelegate<T> method) => s_methods += method;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeMethods(in T rpc) => s_methods.Invoke(rpc);
        
        private static void ClearStaticCaches() => s_methods = null;
    }
}