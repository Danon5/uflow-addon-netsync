namespace UFlow.Addon.NetSync.Core.Runtime {
    public delegate void RpcHandlerDelegate<T>(in T rpc) where T : INetRpc;
}