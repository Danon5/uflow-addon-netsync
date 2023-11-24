using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class LiteNetLibTransport {
        public static event Action<ConnectionState> ServerStateChangedEvent;
        public static event Action<ConnectionState> ClientStateChangedEvent;
        public static event Action<ConnectionState> HostStateChangedEvent;
        public static event Action<NetClient> ServerClientAuthorizedEvent;
        private const string c_default_ip = "localhost";
        private const ushort c_default_port = 7777;
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private static readonly int s_timeoutMS = (int)s_timeout.TotalMilliseconds;
        private readonly Dictionary<ushort, NetPeer> m_peers = new();
        private readonly Dictionary<ushort, NetClient> m_clients = new();
        private readonly NetManager m_server;
        private readonly NetManager m_client;
        private readonly ByteBuffer m_buffer;
        private readonly NetDataWriter m_writer;
        private readonly NetDataReader m_reader;
        private ConnectionState m_serverState;
        private ConnectionState m_clientState;
        private ConnectionState m_hostState;

        public ConnectionState ServerState {
            get => m_serverState;
            private set {
                if (value == m_serverState) return;
                m_serverState = value;
                ServerStateChangedEvent?.Invoke(value);
            }
        }
        public ConnectionState ClientState {
            get => m_clientState;
            private set {
                if (value == m_clientState) return;
                m_clientState = value;
                ClientStateChangedEvent?.Invoke(value);
            }
        }
        public ConnectionState HostState {
            get => m_hostState;
            private set {
                if (value == m_hostState) return;
                m_hostState = value;
                HostStateChangedEvent?.Invoke(value);
            }
        }
        public bool ServerStartingOrStarted => ServerState is ConnectionState.Starting or ConnectionState.Started;
        public bool ServerStoppingOrStopped => ServerState is ConnectionState.Stopping or ConnectionState.Stopped;
        public bool ClientStartingOrStarted => ClientState is ConnectionState.Starting or ConnectionState.Started;
        public bool ClientStoppingOrStopped => ClientState is ConnectionState.Stopping or ConnectionState.Stopped;
        public bool HostStartingOrStarted => HostState is ConnectionState.Starting or ConnectionState.Started;
        public bool HostStoppingOrStopped => HostState is ConnectionState.Stopping or ConnectionState.Stopped;
        public NetPeer ServerPeer { get; private set; }

        static LiteNetLibTransport() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        public LiteNetLibTransport() {
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
            RpcTypeIdMap.RegisterAllLocalRpcsIfRequired();
        }

        public async UniTask StartServerAsync(ushort port = c_default_port) {
            if (ServerStartingOrStarted) throw new Exception("Server already started.");
            ServerState = ConnectionState.Starting;
            if (!await ServerSetupAsync(port)) {
                ServerState = ConnectionState.Stopped;
                return;
            }
            ServerState = ConnectionState.Started;
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server started on port {port}.");
#endif
        }

        public async UniTask StopServerAsync() {
            if (ServerStoppingOrStopped) throw new Exception("Server not yet started.");
            ServerState = ConnectionState.Stopping;
            await ServerCleanupAsync();
            m_clients.Clear();
            ServerState = ConnectionState.Stopped;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Server stopped.");
#endif
        }

        public async UniTask StartClientAsync(string ip = c_default_ip, ushort port = c_default_port) {
            if (ClientStartingOrStarted) throw new Exception("Client already started.");
            if (ip == "localhost")
                ip = "127.0.0.1";
            ClientState = ConnectionState.Starting;
            if (!await ClientSetupAsync(ip, port)) {
                ClientState = ConnectionState.Stopped;
                return;
            }
            ClientState = ConnectionState.Started;
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Client started with ip {ip} on port {port}.");
#endif
        }

        public async UniTask StopClientAsync() {
            if (ClientStoppingOrStopped) throw new Exception("Client not yet started.");
            ClientState = ConnectionState.Stopping;
            await ClientCleanupAsync();
            ClientState = ConnectionState.Stopped;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client stopped.");
#endif
        }

        public async UniTask StartHostAsync(ushort port = c_default_port) {
            if (HostStartingOrStarted) throw new Exception("Host already started.");
            HostState = ConnectionState.Starting;
            await StartServerAsync(port);
            if (!ServerStartingOrStarted) {
                HostState = ConnectionState.Stopped;
                return;
            }
            await StartClientAsync("localhost", port);
            if (!ClientStartingOrStarted) {
                HostState = ConnectionState.Stopped;
                return;
            }
            HostState = ConnectionState.Started;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host started.");
#endif
        }

        public async UniTask StopHostAsync() {
            if (HostStoppingOrStopped) throw new Exception("Host not yet started.");
            HostState = ConnectionState.Stopping;
            await ServerCleanupAsync();
            await ClientCleanupAsync();
            HostState = ConnectionState.Stopped;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host stopped.");
#endif
        }
        
        public void PollEvents() {
            m_server.PollEvents();
            m_client.PollEvents();
        }
        
        public void ForceStop() {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetClient GetClient(ushort id) => m_clients[id];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetClient GetClient(NetPeer peer) => m_clients[(ushort)peer.Id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetClient(ushort id, out NetClient client) => m_clients.TryGetValue(id, out client);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ServerIsHostClient(in NetClient client) => HostStartingOrStarted && client.id == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ServerIsHostClient(ushort clientId) => HostStartingOrStarted && clientId == 0;

        public void ServerSend<T>(in T rpc,
                                  in NetClient client,
                                  NetReliability netReliability = NetReliability.ReliableOrdered) 
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(m_buffer, rpc);
            EndWrite();
            m_peers[client.id].Send(m_writer, (DeliveryMethod)netReliability);
        }

        public void ServerSendToAll<T>(in T rpc, 
                                       NetReliability netReliability = NetReliability.ReliableOrdered)
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(m_buffer, rpc);
            EndWrite();
            foreach (var (id, client) in m_clients)
                m_peers[id].Send(m_writer, (DeliveryMethod)netReliability);
        }
        
        public void ServerSendToAllExcept<T>(in T rpc, 
                                             in NetClient excludedClient,
                                             NetReliability netReliability = NetReliability.ReliableOrdered)
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(m_buffer, rpc);
            EndWrite();
            foreach (var (id, client) in m_clients) {
                if (id == excludedClient.id) continue;
                m_peers[id].Send(m_writer, (DeliveryMethod)netReliability);
            }
        }

        public void ServerSendToAllExceptHost<T>(in T rpc,
                                                 NetReliability netReliability = NetReliability.ReliableOrdered)
            where T : INetRpc {
            if (!HostStartingOrStarted) return;
            ServerSendToAllExcept(rpc, m_clients[0], netReliability);
        }
        
        public void ClientSend<T>(in T rpc,
                                  NetReliability netReliability = NetReliability.ReliableOrdered) 
            where T : INetRpc {
            if (ServerPeer.ConnectionState != LiteNetLib.ConnectionState.Connected)
                throw new Exception("Cannot send rpc when not connected.");
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(m_buffer, rpc);
            EndWrite();
            ServerPeer.Send(m_writer, (DeliveryMethod)netReliability);
        }

        public void ClientPeerHandshakeResponse() {
            BeginWrite(NetPacketType.HandshakeResponse);
            NetSerializer.SerializeHandshakeResponse(m_buffer);
            EndWrite();
            ServerPeer.Send(m_writer, DeliveryMethod.ReliableOrdered);
        }

        public void ServerAuthorizePeer(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Authorized peer {peer.Id}.");
#endif
            var client = new NetClient((ushort)peer.Id);
            m_clients.Add((ushort)peer.Id, client);
            ServerClientAuthorizedEvent?.Invoke(client);
        }

        private static void ClearStaticCache() {
            ServerStateChangedEvent = default;
            ClientStateChangedEvent = default;
            HostStateChangedEvent = default;
            ServerClientAuthorizedEvent = default;
        }
        
        private static void ServerOnConnectionRequest(ConnectionRequest request) => request.Accept();
        
        private async UniTask<bool> ServerSetupAsync(ushort port) {
            var result = m_server.Start(port);
            await UniTask.WaitUntil(() => m_server.IsRunning).Timeout(s_timeout);
            RpcTypeIdMap.ServerRegisterNetworkRpcs();
            return result;
        }

        private async UniTask ServerCleanupAsync() {
            m_server.Stop(true);
            await UniTask.WaitUntil(() => !m_server.IsRunning);
#if UFLOW_DEBUG_ENABLED
            if (HostState == ConnectionState.Stopping)
                Debug.Log("Server stopped.");
#endif
            m_peers.Clear();
            RpcTypeIdMap.ClearNetworkRpcs();
        }

        private async UniTask<bool> ClientSetupAsync(string ip, ushort port) {
            m_client.Start();
            ServerPeer = m_client.Connect(ip, port, string.Empty);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
#if UFLOW_DEBUG_ENABLED
            if (ServerPeer == null)
                Debug.LogWarning($"Connection failed to ip {ip} on port {port}.");
#endif
            return ServerPeer != null;
        }

        private async UniTask ClientCleanupAsync() {
            m_client.Stop();
            await UniTask.WaitUntil(() => !m_client.IsRunning);
#if UFLOW_DEBUG_ENABLED
            if (HostState == ConnectionState.Stopping)
                Debug.Log("Client stopped.");
#endif
            RpcTypeIdMap.ClearNetworkRpcs();
        }

        private void ServerOnPeerConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} connected.");
#endif
            m_peers.Add((ushort)peer.Id, peer);
            BeginWrite(NetPacketType.Handshake);
            NetSerializer.SerializeHandshake(m_buffer);
            EndWrite();
            peer.Send(m_writer, DeliveryMethod.ReliableOrdered);
        }
        
        private void ServerOnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer {peer.Id} disconnected: {info.Reason}.");
#endif
        }

        private void ServerOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Server received packet.");
#endif
            BeginRead(reader, out var packetType);
            NetDeserializer.Deserialize(m_buffer, packetType, peer);
            EndRead(reader);
        }

        private void ClientOnConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Connected.");
#endif
        }
        
        private void ClientOnDisconnected(NetPeer peer, DisconnectInfo info) {
            RpcTypeIdMap.ClearNetworkRpcs();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Disconnected: {info.Reason}.");
#endif
        }

        private void ClientOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client received packet.");
#endif
            BeginRead(reader, out var packetType);
            NetDeserializer.Deserialize(m_buffer, packetType, peer);
            EndRead(reader);
        }

        private void BeginWrite(NetPacketType type) {
            m_writer.Reset();
            m_buffer.Reset();
            m_buffer.Write((byte)type);
        }

        private void EndWrite() {
            m_writer.Put(m_buffer.GetBytesToCursor());
        }

        private void BeginRead(NetDataReader reader, out NetPacketType packetType) {
            m_buffer.Reset();
            m_buffer.TransferBytesToBuffer(reader.RawData, 4, reader.AvailableBytes);
            packetType = (NetPacketType)m_buffer.ReadByte();
        }

        private void EndRead(NetPacketReader reader) {
            reader.Recycle();
        }
    }
}