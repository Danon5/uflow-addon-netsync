namespace UFlow.Addon.NetSync.Core.Runtime {
    internal struct InitialHandshakeRpc : INetRpc {
        public RpcHash[] hashes;
        
        public struct RpcHash {
            public string typeName;
            public ushort typeHash;
        }
    }
}