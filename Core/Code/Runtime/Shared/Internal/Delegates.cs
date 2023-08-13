namespace UFlow.Addon.NetSync.Core.Runtime {
    public delegate void RpcMethodDelegate<T>(in T rpc) where T : INetRpc;
}