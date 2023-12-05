namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetClient {
        public readonly ushort id;

        public NetClient(ushort id) {
            this.id = id;
        }

        public override int GetHashCode() => id;

        public static implicit operator ushort(NetClient client) => client.id;
    }
}