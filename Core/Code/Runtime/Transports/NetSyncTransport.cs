using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace UFlow.Addon.NetSync.Runtime {
    public sealed class NetSyncTransport : BaseTransport {
        private readonly EventBasedNetListener m_serverListener;
        private readonly NetManager m_server;
        private readonly EventBasedNetListener m_clientListener;
        private readonly NetManager m_client;

        public NetSyncTransport() {
            m_serverListener = new EventBasedNetListener();
            m_server = new NetManager(m_serverListener);
            m_clientListener = new EventBasedNetListener();
            m_client = new NetManager(m_clientListener);
        }
        
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