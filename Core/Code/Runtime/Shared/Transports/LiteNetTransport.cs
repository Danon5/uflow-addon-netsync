
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UFlow.Addon.Serialization.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class LiteNetTransport : BaseTransport {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private static readonly int s_timeoutMS = (int)s_timeout.TotalMilliseconds;
        private readonly Dictionary<ushort, NetPeer> m_peers = new();
        private readonly NetManager m_server;
        private readonly NetManager m_client;
        private readonly ByteBuffer m_buffer;
        private readonly NetDataWriter m_writer;
        private readonly NetDataReader m_reader;
        private NetPeer m_clientConnectionToServer;

        public LiteNetTransport() {
            var serverListener = new EventBasedNetListener();
            serverListener.ConnectionRequestEvent += ServerOnConnectionRequest;
            serverListener.PeerConnectedEvent += ServerOnPeerConnected;
            serverListener.PeerDisconnectedEvent += ServerOnPeerDisconnected;
            serverListener.NetworkReceiveEvent += ServerOnReceive;
            m_server = new NetManager(serverListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeoutMS
            };
            var clientListener = new EventBasedNetListener();
            clientListener.PeerConnectedEvent += ClientOnConnected;
            clientListener.PeerDisconnectedEvent += ClientOnDisconnected;
            clientListener.NetworkReceiveEvent += ClientOnReceive;
            m_client = new NetManager(clientListener) {
                AutoRecycle = true,
                DisconnectTimeout = s_timeoutMS
            };
            m_buffer = new ByteBuffer(true);
            m_writer = new NetDataWriter(true);
            m_reader = new NetDataReader(m_writer);
        }

        public override void SendToServer<T>(in T rpc, NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            var rpcCopy = rpc;
            SerializationAPI.Serialize(m_buffer, ref rpcCopy);
            m_writer.Put(m_buffer.GetBytesToCursor());
            m_clientConnectionToServer.Send(m_writer, (DeliveryMethod)reliabilityType);
            ResetCursors();
        }

        public override void SendToClient<T>(in T rpc, in NetClient target, 
                                             NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            var rpcCopy = rpc;
            SerializationAPI.Serialize(m_buffer, ref rpcCopy);
            m_writer.Put(m_buffer.GetBytesToCursor());
            m_peers[target.id].Send(m_writer, (DeliveryMethod)reliabilityType);
            ResetCursors();
        }
        public override void SendToClient<T>(in T rpc, ushort targetId, 
                                             NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            SendToClient(rpc, GetClient(targetId), reliabilityType);
        }

        public override void SendToAllClients<T>(in T rpc, NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            foreach (var (id, _) in m_peers)
                SendToClient(rpc, GetClient(id), reliabilityType);
        }

        public override void SendToAllClientsExcept<T>(in T rpc, in NetClient except, 
                                                       NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            var exceptId = except.id;
            foreach (var (id, _) in m_peers) {
                if (id == exceptId) continue;
                SendToClient(rpc, GetClient(id), reliabilityType);
            }
        }
        
        public override void SendToAllClientsExcept<T>(in T rpc, ushort exceptId, 
                                                       NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) {
            foreach (var (id, _) in m_peers) {
                if (id == exceptId) continue;
                SendToClient(rpc, GetClient(id), reliabilityType);
            }
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
            m_peers.Clear();
        }

        protected override async UniTask<bool> SetupClient(string ip, ushort port) {
            m_client.Start();
            m_clientConnectionToServer = m_client.Connect(ip, port, string.Empty);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
#if UFLOW_DEBUG_ENABLED
            if (m_clientConnectionToServer == null)
                Debug.LogWarning($"Connection failed to ip {ip} on port {port}.");
#endif
            return m_clientConnectionToServer != null;
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
            /*
            if (HostStartingOrStarted)
                SendToAllClientsExcept(new ClientConnectedRpc((ushort)peer.Id), (ushort)m_clientConnectionToServer.Id); 
            else
                SendToAllClients(new ClientConnectedRpc((ushort)peer.Id));
                */
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} connected.");
#endif
        }
        
        private void ServerOnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} disconnected: {info.Reason}.");
#endif
        }

        private void ServerOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Server received.");
#endif
        }

        private void ClientOnConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Connected.");
#endif
        }
        
        private void ClientOnDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Disconnected: {info.Reason}.");
#endif
        }

        private void ClientOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client received.");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetCursors() {
            m_buffer.ResetCursor();
            m_writer.Reset();
        }
    }
}