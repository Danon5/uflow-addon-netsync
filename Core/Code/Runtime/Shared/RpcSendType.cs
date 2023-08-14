namespace UFlow.Addon.NetSync.Core.Runtime {
    public enum RpcSendType : byte {
        ClientToServer,
        ServerToClient,
        ServerToAll,
        ServerToAllExcept
    }
}