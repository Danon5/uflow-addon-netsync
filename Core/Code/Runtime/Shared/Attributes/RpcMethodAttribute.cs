using System;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RpcMethodAttribute : Attribute {
        public readonly Type rpcType;
        public readonly RpcSendType sendType;

        public RpcMethodAttribute(Type rpcType, RpcSendType sendType) {
            this.rpcType = rpcType;
            this.sendType = sendType;
        }
    }
}