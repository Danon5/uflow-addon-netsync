namespace UFlow.Addon.NetSync.Core.Runtime {
    internal readonly struct ClientConnectedRpc : INetRpc {
        public readonly ushort clientId;

        public ClientConnectedRpc(ushort clientId) {
            this.clientId = clientId;
        }
    }
}