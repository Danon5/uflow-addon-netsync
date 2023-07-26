using System;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class LiteNetTransport : BaseTransport {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private readonly NetManager m_server;
        private readonly NetManager m_client;
        private NetPeer m_clientPeer;

        public LiteNetTransport() {
            var serverListener = new EventBasedNetListener();
            m_server = new NetManager(serverListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeout.Milliseconds
            };

            var clientListener = new EventBasedNetListener();
            m_client = new NetManager(clientListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeout.Milliseconds
            };
        }

        public override void PollEvents() {
            if (m_server.IsRunning)
                m_server.PollEvents();
            if (m_client.IsRunning)
                m_client.PollEvents();
        }
        public override void Dispose() {
            if (m_server.IsRunning) {
                m_server.Stop();
                Debug.Log("Server stopped.");
            }
            if (m_client.IsRunning) {
                m_client.Stop();
                Debug.Log("Server stopped");
            }
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
            m_clientPeer = m_client.Connect(ip, port, string.Empty);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
            return m_clientPeer != null;
        }

        protected override async UniTask CleanupClient() {
            m_client.Stop();
            await UniTask.WaitUntil(() => !m_client.IsRunning);
        }
    }
}