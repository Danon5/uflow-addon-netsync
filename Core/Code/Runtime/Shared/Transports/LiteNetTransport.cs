
using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class LiteNetTransport : BaseTransport {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private static readonly int s_timeoutMS = (int)s_timeout.TotalMilliseconds;
        private readonly NetManager m_server;
        private readonly NetManager m_client;
        private readonly ByteBuffer m_buffer;
        private readonly NetDataWriter m_writer;
        private readonly NetDataReader m_reader;
        private NetPeer m_clientPeer;

        public LiteNetTransport() {
            var serverListener = new EventBasedNetListener();
            serverListener.ConnectionRequestEvent += ServerOnConnectionRequest;
            serverListener.PeerConnectedEvent += ServerOnPeerConnected;
            serverListener.PeerDisconnectedEvent += ServerOnPeerDisconnected;
            m_server = new NetManager(serverListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeoutMS
            };
            var clientListener = new EventBasedNetListener();
            clientListener.PeerConnectedEvent += ClientOnPeerConnected;
            clientListener.PeerDisconnectedEvent += ClientOnPeerDisconnected;
            m_client = new NetManager(clientListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeoutMS
            };
            m_buffer = new ByteBuffer(true);
            m_writer = new NetDataWriter(true);
            m_reader = new NetDataReader(m_writer);
        }

        public override void SendServerRpc<T>(in T rpc, DeliveryMethod method) {
            m_buffer.WriteUnsafe(rpc); // replace
            m_writer.Put(m_buffer.GetBytesToCursor());
            m_client.SendBroadcast(m_writer, m_client.LocalPort);
            ResetCursors();
        }
        
        public override void SendClientRpc<T>(in T rpc, DeliveryMethod method) {
            m_buffer.WriteUnsafe(rpc); // replace
            m_writer.Put(m_buffer.GetBytesToCursor());
            m_server.SendToAll(m_writer, method);
            ResetCursors();
        }

        public override void SendClientRpc<T>(in T rpc, in NetPeer excludedClient, DeliveryMethod method) {
            m_buffer.WriteUnsafe(rpc); // replace
            m_writer.Put(m_buffer.GetBytesToCursor());
            m_server.SendToAll(m_writer, method, excludedClient);
            ResetCursors();
        }
        public override void SendTargetRpc<T>(in T rpc, in NetPeer target, DeliveryMethod method) {
            m_buffer.WriteUnsafe(rpc); // replace
            m_writer.Put(m_buffer.GetBytesToCursor());
            target.Send(m_writer, method);
            ResetCursors();
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
                Debug.Log("Client stopped.");
#endif
            }
        }

        protected override async UniTask<bool> SetupServer(ushort port) {
            var result = m_server.Start(port);
            await UniTask.WaitUntil(() => m_server.IsRunning).Timeout(s_timeout);
            return result;
        }

        protected override async UniTask CleanupServer() {
            m_server.Stop(true);
            await UniTask.WaitUntil(() => !m_server.IsRunning);
#if UFLOW_DEBUG_ENABLED
            if (HostState == ConnectionState.Stopping)
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
            if (HostState == ConnectionState.Stopping)
                Debug.Log("Client stopped.");
#endif
        }

        private static void ServerOnConnectionRequest(ConnectionRequest request) {
            request.Accept();
        }

        private void ServerOnPeerConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} connected.");
#endif
        }
        
        private void ServerOnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} disconnected: {info.Reason}.");
#endif
        }

        private void ClientOnPeerConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Connected.");
#endif
        }
        
        private void ClientOnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Disconnected: {info.Reason}.");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetCursors() {
            m_buffer.ResetCursor();
            m_writer.Reset();
        }
    }
}