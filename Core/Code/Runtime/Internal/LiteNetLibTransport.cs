using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using UFlow.Addon.Serialization.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class LiteNetLibTransport {
        public event Action<ConnectionState> ServerStateChangedEvent;
        public event Action<ConnectionState> ClientStateChangedEvent;
        public event Action<ConnectionState> HostStateChangedEvent;
        public event Action<NetClient> ServerClientAuthorizedEvent;
        public event Action<NetClient> ServerClientDisconnectedEvent; 
        private const string c_default_ip = "localhost";
        private const ushort c_default_port = 7777;
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);
        private static readonly int s_timeoutMS = (int)s_timeout.TotalMilliseconds;
        private readonly Dictionary<ushort, NetPeer> m_peers = new();
        private readonly Dictionary<ushort, NetClient> m_clients = new();
        private readonly NetManager m_server;
        private readonly NetManager m_client;
        private readonly NetDataWriter m_writer;
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
        public bool OfflineMode { get; private set; }
        internal ByteBuffer Buffer { get; }

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
            Buffer = new ByteBuffer(true);
            m_writer = new NetDataWriter(true);
            NetTypeIdMaps.RpcMap.InitializeLocallyIfRequired();
            NetTypeIdMaps.ComponentMap.InitializeLocallyIfRequired();
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
            Debug.Log($"Server started. Port: {port}");
#endif
        }

        public async UniTask StopServerAsync() {
            if (ServerStoppingOrStopped) throw new Exception("Server not yet started.");
            ServerState = ConnectionState.Stopping;
            await ServerCleanupAsync();
            m_clients.Clear();
            ServerState = ConnectionState.Stopped;
            OfflineMode = false;
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
            Debug.Log($"Client started. IP: {ip}, Port: {port}");
#endif
        }

        public async UniTask StopClientAsync() {
            if (ClientStoppingOrStopped) throw new Exception("Client not yet started.");
            ClientState = ConnectionState.Stopping;
            await ClientCleanupAsync();
            ClientState = ConnectionState.Stopped;
            OfflineMode = false;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client stopped.");
#endif
        }

        public async UniTask StartHostAsync(bool offlineMode = false, ushort port = c_default_port) {
            if (HostStartingOrStarted) throw new Exception("Host already started.");
            OfflineMode = offlineMode;
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
            m_clients.Add(0, new NetClient(0));
            HostState = ConnectionState.Started;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host started.");
            Debug.Log("Host connected.");
            Debug.Log("Connected.");
#endif
            ServerClientAuthorizedEvent?.Invoke(m_clients[0]);
        }

        public async UniTask StopHostAsync() {
            if (HostStoppingOrStopped) throw new Exception("Host not yet started.");
            OfflineMode = false;
            HostState = ConnectionState.Stopping;
            await ServerCleanupAsync();
            await ClientCleanupAsync();
            HostState = ConnectionState.Stopped;
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host stopped.");
#endif
        }
        
        public void PollEvents() {
            if (m_server.IsRunning)
                m_server.PollEvents();
            if (m_client.IsRunning)
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
        public bool TryGetHostClient(out NetClient hostClient) {
            if (!HostStartingOrStarted) {
                hostClient = null;
                return false;
            }
            hostClient = m_clients[0];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetClient(ushort id, out NetClient client) => m_clients.TryGetValue(id, out client);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHost(ushort clientId) => HostStartingOrStarted && clientId == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHost(NetPeer peer) => HostStartingOrStarted && peer.Id == 0;

        public void ServerSend<T>(in T rpc,
                                  in NetClient client,
                                  NetReliability netReliability = NetReliability.ReliableOrdered) 
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            if (TryGetHostClient(out var hostClient) && client.id == hostClient.id) {
#if UFLOW_DEBUG_ENABLED
                Debug.Log($"Host emulating packet. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}");
#endif
                NetDeserializer.RpcDeserializer<T>.InvokeClientRpcDeserializedDirect(rpc);
                return;
            }
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(Buffer, rpc);
            EndWrite();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server sending packet. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}, ClientID: {client.id}");
#endif
            SendBufferPayloadToClient(client, netReliability);
        }

        public void ServerSendToAll<T>(in T rpc, 
                                       NetReliability netReliability = NetReliability.ReliableOrdered)
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            if (HostStartingOrStarted) {
#if UFLOW_DEBUG_ENABLED
                Debug.Log($"Host emulating packet. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}");
#endif
                NetDeserializer.RpcDeserializer<T>.InvokeClientRpcDeserializedDirect(rpc);
            }
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(Buffer, rpc);
            EndWrite();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server sending packet to all. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}");
#endif
            SendBufferPayloadToAllClients(netReliability);
        }
        
        public void ServerSendToAllExcept<T>(in T rpc, 
                                             in NetClient excludedClient,
                                             NetReliability netReliability = NetReliability.ReliableOrdered)
            where T : INetRpc {
            if (m_clients.Count == 0) return;
            if (TryGetHostClient(out var hostClient) && excludedClient.id != hostClient.id) {
#if UFLOW_DEBUG_ENABLED
                Debug.Log($"Host emulating packet. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}");
#endif
                NetDeserializer.RpcDeserializer<T>.InvokeClientRpcDeserializedDirect(rpc);
            }
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(Buffer, rpc);
            EndWrite();
#if UFLOW_DEBUG_ENABLED
            Debug.Log(
                $"Server sending packet to all except. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}, ClientID: {excludedClient.id}");
#endif
            SendBufferPayloadToAllClientsExcept(excludedClient, netReliability);
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
            if (TryGetHostClient(out var hostClient)) {
                NetDeserializer.RpcDeserializer<T>.InvokeServerRpcDeserializedDirect(rpc, hostClient);
                return;
            }
            if (ServerPeer.ConnectionState != LiteNetLib.ConnectionState.Connected)
                throw new Exception("Cannot send RPC when not connected.");
            BeginWrite(NetPacketType.RPC);
            NetSerializer.SerializeRpc(Buffer, rpc);
            EndWrite();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Client sending packet. Type: {NetPacketType.RPC}, RPC: {typeof(T).Name}");
#endif
            SendBufferPayloadToServer(netReliability);
        }

        public void ClientPeerHandshakeResponse() {
            BeginWrite(NetPacketType.HandshakeResponse);
            NetSerializer.SerializeHandshakeResponse(Buffer);
            EndWrite();
            SendBufferPayloadToServer();
        }

        public void ServerAuthorizePeer(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Authorized peer. ID: {peer.Id}");
#endif
            var client = new NetClient((ushort)peer.Id);
            m_clients.Add((ushort)peer.Id, client);
            ServerClientAuthorizedEvent?.Invoke(client);
        }

        public IEnumerable<NetClient> GetClientsEnumerable() => m_clients.Values;

        public void ServerSetStatisticsEnabled(bool state) => m_server.EnableStatistics = state;
        
        public void ClientSetStatisticsEnabled(bool state) => m_client.EnableStatistics = state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetStatisticData ServerGetStatistics() => new(m_server.Statistics);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetStatisticData ClientGetStatistics() => new(m_client.Statistics);

        public void ServerResetStatistics() => m_server.Statistics.Reset();
        
        public void ClientResetStatistics() => m_client.Statistics.Reset();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BeginWrite(NetPacketType type) {
            m_writer.Reset();
            Buffer.Reset();
            Buffer.Write((byte)type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EndWrite() => m_writer.Put(Buffer.GetBytesToCursor());

        internal void Write<T>(in T value) where T : unmanaged => Buffer.WriteUnsafe(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendBufferPayloadToServer(NetReliability netReliability = NetReliability.ReliableOrdered) =>
            ServerPeer.Send(m_writer, (DeliveryMethod)netReliability);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendBufferPayloadToClient(NetClient client, 
                                                NetReliability netReliability = NetReliability.ReliableOrdered) =>
            m_peers[client.id].Send(m_writer, (DeliveryMethod)netReliability);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendBufferPayloadToAllClients(NetReliability netReliability = NetReliability.ReliableOrdered) {
            foreach (var client in m_clients) {
                if (TryGetHostClient(out var hostClient) && client.Key == hostClient.id) continue;
                m_peers[client.Key].Send(m_writer, (DeliveryMethod)netReliability);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendBufferPayloadToAllClientsExcept(NetClient excludedClient, 
                                                          NetReliability netReliability = NetReliability.ReliableOrdered) {
            foreach (var client in m_clients) {
                if (client.Key == excludedClient.id) continue;
                if (TryGetHostClient(out var hostClient) && client.Key == hostClient.id) continue;
                m_peers[client.Key].Send(m_writer, (DeliveryMethod)netReliability);
            }
        }

        private static void ServerOnConnectionRequest(ConnectionRequest request) => request.Accept();
        
        private async UniTask<bool> ServerSetupAsync(ushort port) {
            var result = true;
            if (!OfflineMode) {
                result = m_server.Start(port);
                await UniTask.WaitUntil(() => m_server.IsRunning).Timeout(s_timeout);
            }
            NetTypeIdMaps.RpcMap.InitializeServer();
            NetTypeIdMaps.ComponentMap.InitializeServer();
            var prefabCache = NetSyncPrefabCache.Get();
            if (prefabCache == null) return result;
            prefabCache.ServerRegisterNetworkIds();
            return result;
        }

        private async UniTask ServerCleanupAsync() {
            if (!OfflineMode) {
                m_server.Stop(true);
                await UniTask.WaitUntil(() => !m_server.IsRunning);
            }
#if UFLOW_DEBUG_ENABLED
            if (HostState == ConnectionState.Stopping)
                Debug.Log("Server stopped.");
#endif
            m_peers.Clear();
            m_clients.Clear();
            NetTypeIdMaps.RpcMap.ClearNetworkCaches();
            NetTypeIdMaps.ComponentMap.ClearNetworkCaches();
            var prefabCache = NetSyncPrefabCache.Get();
            if (prefabCache == null) return;
            prefabCache.ClearNetworkIdMaps();
        }

        private async UniTask<bool> ClientSetupAsync(string ip, ushort port) {
            if (OfflineMode) return true;
            m_client.Start();
            ServerPeer = m_client.Connect(ip, port, string.Empty);
            await UniTask.WaitUntil(() => m_client.IsRunning).Timeout(s_timeout);
#if UFLOW_DEBUG_ENABLED
            if (ServerPeer == null)
                Debug.LogWarning($"Connection failed. IP: {ip}, Port: {port}");
#endif
            return ServerPeer != null;
        }

        private async UniTask ClientCleanupAsync() {
            if (!OfflineMode) {
                m_client.Stop();
                await UniTask.WaitUntil(() => !m_client.IsRunning);
            }
#if UFLOW_DEBUG_ENABLED
            if (HostState == ConnectionState.Stopping)
                Debug.Log("Client stopped.");
#endif
            NetTypeIdMaps.RpcMap.ClearNetworkCaches();
            NetTypeIdMaps.ComponentMap.ClearNetworkCaches();
            var prefabCache = NetSyncPrefabCache.Get();
            if (prefabCache == null) return;
            prefabCache.ClearNetworkIdMaps();
        }

        private void ServerOnPeerConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer connected. ID: {peer.Id}");
#endif
            if (IsHost((ushort)peer.Id)) return;
            m_peers.Add((ushort)peer.Id, peer);
            BeginWrite(NetPacketType.Handshake);
            NetSerializer.SerializeHandshake(Buffer);
            EndWrite();
            peer.Send(m_writer, DeliveryMethod.ReliableOrdered);
        }
        
        private void ServerOnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Peer disconnected. ID: {peer.Id}, Reason: {info.Reason}");
#endif
            var peerId = (ushort)peer.Id;
            m_peers.Remove(peerId);
            if (!m_clients.TryGetValue(peerId, out var client)) return;
            ServerClientDisconnectedEvent?.Invoke(client);
            m_clients.Remove(peerId);
        }

        private void ServerOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
            BeginRead(reader, out var packetType);
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server received packet. Type: {packetType}");
#endif
            NetDeserializer.Deserialize(Buffer, packetType, peer);
            EndRead(reader);
        }

        private void ClientOnConnected(NetPeer peer) {
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Connected.");
#endif
        }
        
        private void ClientOnDisconnected(NetPeer peer, DisconnectInfo info) {
            NetTypeIdMaps.RpcMap.ClearNetworkCaches();
            NetTypeIdMaps.ComponentMap.ClearNetworkCaches();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Disconnected. Reason: {info.Reason}");
#endif
        }

        private void ClientOnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
            BeginRead(reader, out var packetType);
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Client received packet. Type: {packetType}");
#endif
            NetDeserializer.Deserialize(Buffer, packetType, peer);
            EndRead(reader);
        }

        private void BeginRead(NetDataReader reader, out NetPacketType packetType) {
            Buffer.Reset();
            Buffer.TransferBytesToBuffer(reader.RawData, 4, reader.AvailableBytes);
            packetType = (NetPacketType)Buffer.ReadByte();
        }

        private void EndRead(NetPacketReader reader) => reader.Recycle();
    }
}