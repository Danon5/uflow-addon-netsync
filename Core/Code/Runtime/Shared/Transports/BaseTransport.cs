using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public abstract class BaseTransport {
        public const string DEFAULT_IP = "localhost";
        public const ushort DEFAULT_PORT = 7777;
        public event Action ServerStartedEvent;
        public event Action ServerStoppedEvent;
        public event Action ClientStartedEvent;
        public event Action ClientStoppedEvent;
        public event Action HostStartedEvent;
        public event Action HostStoppedEvent;
        public event Action<NetClient> ClientConnectedEvent; 
        public event Action<NetClient> ClientDisconnectedEvent; 
        private readonly Dictionary<ushort, NetClient> m_clients = new();
        public ConnectionState ServerState { get; private set; }
        public ConnectionState ClientState { get; private set; }
        public ConnectionState HostState { get; private set; }
        public bool ServerStartingOrStarted => ServerState is ConnectionState.Starting or ConnectionState.Started;
        public bool ServerStoppingOrStopped => ServerState is ConnectionState.Stopping or ConnectionState.Stopped;
        public bool ClientStartingOrStarted => ClientState is ConnectionState.Starting or ConnectionState.Started;
        public bool ClientStoppingOrStopped => ClientState is ConnectionState.Stopping or ConnectionState.Stopped;
        public bool HostStartingOrStarted => HostState is ConnectionState.Starting or ConnectionState.Started;
        public bool HostStoppingOrStopped => HostState is ConnectionState.Stopping or ConnectionState.Stopped;

        public async UniTask StartServerAsync(ushort port = DEFAULT_PORT) {
            if (ServerStartingOrStarted) throw new Exception("Server already started.");
            ServerState = ConnectionState.Starting;
            if (!await SetupServer(port)) {
                ServerState = ConnectionState.Stopped;
                return;
            }
            ServerState = ConnectionState.Started;
            ServerStartedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server started on port {port}.");
#endif
        }

        public async UniTask StopServerAsync() {
            if (ServerStoppingOrStopped) throw new Exception("Server not yet started.");
            ServerState = ConnectionState.Stopping;
            await CleanupServer();
            m_clients.Clear();
            ServerState = ConnectionState.Stopped;
            ServerStoppedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Server stopped.");
#endif
        }

        public async UniTask StartClientAsync(string ip = DEFAULT_IP, ushort port = DEFAULT_PORT) {
            if (ClientStartingOrStarted) throw new Exception("Client already started.");
            if (ip == "localhost")
                ip = "127.0.0.1";
            ClientState = ConnectionState.Starting;
            if (!await SetupClient(ip, port)) {
                ClientState = ConnectionState.Stopped;
                return;
            }
            ClientState = ConnectionState.Started;
            ClientStartedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Client started with ip {ip} on port {port}.");
#endif
        }

        public async UniTask StopClientAsync() {
            if (ClientStoppingOrStopped) throw new Exception("Client not yet started.");
            ClientState = ConnectionState.Stopping;
            await CleanupClient();
            ClientState = ConnectionState.Stopped;
            ClientStoppedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Client stopped.");
#endif
        }

        public async UniTask StartHostAsync(ushort port = DEFAULT_PORT) {
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
            HostStartedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host started.");
#endif
        }

        public async UniTask StopHostAsync() {
            if (HostStoppingOrStopped) throw new Exception("Host not yet started.");
            HostState = ConnectionState.Stopping;
            await CleanupServer();
            await CleanupClient();
            HostState = ConnectionState.Stopped;
            HostStoppedEvent?.Invoke();
#if UFLOW_DEBUG_ENABLED
            Debug.Log("Host stopped.");
#endif
        }

        public abstract void SendToServer<T>(in T rpc, 
                                             NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) 
            where T : INetRpc;

        public abstract void SendToClient<T>(in T rpc, in NetClient target, 
                                             NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered)
            where T : INetRpc;
        
        public abstract void SendToClient<T>(in T rpc, ushort targetId, 
                                             NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered)
            where T : INetRpc;
        
        public abstract void SendToAllClients<T>(in T rpc, 
                                                 NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) 
            where T : INetRpc;

        public abstract void SendToAllClientsExcept<T>(in T rpc, in NetClient except,
                                                       NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) 
            where T : INetRpc;
        
        public abstract void SendToAllClientsExcept<T>(in T rpc, ushort exceptId, 
                                                       NetReliabilityType reliabilityType = NetReliabilityType.ReliableOrdered) 
            where T : INetRpc;

        public abstract void PollEvents();

        public abstract void ForceStop();

        public NetClient GetClient(ushort id) => m_clients[id]; 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeServerStarted() => ServerStartedEvent?.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeServerStopped() => ServerStoppedEvent?.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeClientStarted() => ClientStartedEvent?.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeClientStopped() => ClientStoppedEvent?.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeHostStarted() => HostStartedEvent?.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeHostStopped() => HostStoppedEvent?.Invoke();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeClientConnected(in NetClient client) => ClientConnectedEvent?.Invoke(client);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InvokeClientDisconnected(in NetClient client) => ClientDisconnectedEvent?.Invoke(client);

        protected abstract UniTask<bool> SetupServer(ushort port);

        protected abstract UniTask CleanupServer();

        protected abstract UniTask<bool> SetupClient(string ip, ushort port);

        protected abstract UniTask CleanupClient();
    }
}