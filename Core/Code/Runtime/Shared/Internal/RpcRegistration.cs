using System;
using System.Reflection;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class RpcRegistration {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad() {
            var subscriptionCacheType = typeof(RpcMethodSubscriptionCache<>);
            var delegateType = typeof(RpcMethodDelegate<>);
            var registerMethod = typeof(RpcRegistration).GetMethod(nameof(RegisterDelegate), BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var (method, att) in UFlowUtils.Reflection.GetAllMethodsWithAttribute<RpcMethodAttribute>(
                UFlowUtils.Reflection.CommonExclusionNamespaces)) {
                var delegateGenericType = delegateType.MakeGenericType(att.rpcType);
                registerMethod!.MakeGenericMethod(att.rpcType)
                    .Invoke(null, new object[] { subscriptionCacheType, delegateGenericType, method, att.sendType });
            }
        }

        private static void RegisterDelegate<T>(in Type subscriptionCacheType, in Type delegateGenericType, 
                                                in MethodInfo methodInfo, in RpcSendType sendType) where T : INetRpc {
            subscriptionCacheType.MakeGenericType(typeof(T)).GetMethod(nameof(RpcMethodSubscriptionCache<T>.RegisterMethod))!.Invoke(null, 
                new object[] { Delegate.CreateDelegate(delegateGenericType, methodInfo) as RpcMethodDelegate<T>, sendType });
        }
    }
}