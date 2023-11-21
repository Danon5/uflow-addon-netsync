namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkClient {
        public readonly ushort id;

        public NetworkClient(ushort id) {
            this.id = id;
        }

        public override int GetHashCode() => id;
    }
}