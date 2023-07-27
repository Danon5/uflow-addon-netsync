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
            serverListener.ConnectionRequestEvent += On;
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
            m_server.PollEvents();
            m_client.PollEvents();
        }
        public override void ForceStop() {
            if (m_server.IsRunning) {
                m_server.Stop();
#if UFLOW_DEBUG_ENABLED
                Debug.Log("Server stopped.");
#endif
            }
            if (m_client.IsRunning) {
                m_client.Stop();
#if UFLOW_DEBUG_ENABLED
                Debug.Log("Client stopped");
#endif
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
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Server stopped.");
#endif
        }

        protected override async UniTask<bool> SetupClient(string ip, ushort port) {
            m_client.Start();
            m_clientPeer = m_client.Connect(ip, port, string.Empty);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
#if UFLOW_DEBUG_ENABLED
            if (m_clientPeer == null)
                Debug.LogWarning($"Connection failed to ip {ip} on port {port}.");
#endif
            return m_clientPeer != null;
        }

        protected override async UniTask CleanupClient() {
            m_client.Stop();
            await UniTask.WaitUntil(() => !m_client.IsRunning);
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client stopped");
#endif
        }

        private static void On(ConnectionRequest request) {
            request.Accept();
        }
    }
}