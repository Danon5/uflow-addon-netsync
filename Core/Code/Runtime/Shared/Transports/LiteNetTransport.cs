using System;
using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class LiteNetTransport : BaseTransport {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private readonly EventBasedNetListener m_serverListener;
        private readonly NetManager m_server;
        private readonly EventBasedNetListener m_clientListener;
        private readonly NetManager m_client;

        public LiteNetTransport() {
            m_serverListener = new EventBasedNetListener();
            m_server = new NetManager(m_serverListener) {
                AutoRecycle = true
            };
            m_clientListener = new EventBasedNetListener();
            m_client = new NetManager(m_clientListener) {
                AutoRecycle = true
            };
        }

        public override void PollEvents() {
            if (m_server.IsRunning)
                m_server.PollEvents();
            if (m_client.IsRunning)
                m_client.PollEvents();
        }
        
        protected override async UniTask<bool> SetupServer(ushort port) {
            var result = m_server.Start(port);
            await UniTask.WaitUntil(() => m_server.IsRunning).Timeout(s_timeout);
            return result;
        }

        protected override async UniTask CleanupServer() {
            m_server.Stop();
            await UniTask.WaitUntil(() => !m_server.IsRunning);
        }

        protected override async UniTask<bool> SetupClient(string ip, ushort port) {
            var result = m_client.Start(port);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
            return result;
        }

        protected override async UniTask CleanupClient() {
            m_client.Stop();
            await UniTask.WaitUntil(() => !m_client.IsRunning);
        }
    }
}