using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace UFlow.Addon.NetSync.Runtime {
    public sealed class NetSyncTransport : BaseTransport {
        private EventBasedNetListener m_netListener;
        
        protected override UniTask<bool> SetupServer(ushort port) {
            return default;
        }

        protected override UniTask CleanupServer() {
            return default;
        }

        protected override UniTask<bool> SetupClient(string ip, ushort port) {
            return default;
        }

        protected override UniTask CleanupClient() {
            return default;
        }
    }
}