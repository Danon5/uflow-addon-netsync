namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetClient {
        public ushort Id { get; }

        public NetClient(ushort id) {
            Id = id;
        }
    }
}