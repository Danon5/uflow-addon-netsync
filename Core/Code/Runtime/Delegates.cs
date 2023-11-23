namespace UFlow.Addon.NetSync.Core.Runtime {
    public delegate void ServerRpcHandlerDelegate<T>(in T rpc, in NetworkClient client) where T : INetRpc;
    
    public delegate void ClientRpcHandlerDelegate<T>(in T rpc) where T : INetRpc;
    
    public delegate void ConnectionStateHandlerDelegate(ConnectionState state);
}