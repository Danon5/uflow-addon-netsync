namespace UFlow.Addon.NetSync.Core.Runtime {
    public enum NetPacketType : byte {
        Handshake,
        HandshakeResponse,
        RPC
    }
}