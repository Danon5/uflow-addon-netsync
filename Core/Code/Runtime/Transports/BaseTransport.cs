using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UFlow.Addon.NetSync.Runtime {
    public abstract class BaseTransport : ScriptableObject {
        public const string DEFAULT_IP = "localhost";
        public const ushort DEFAULT_PORT = 7777;

        public NetworkState ServerState { get; private set; }
        public NetworkState ClientState { get; private set; }
        public NetworkState HostState { get; private set; }

        public bool ServerStartingOrStarted => ServerState is NetworkState.Starting or NetworkState.Started;
        public bool ServerStoppingOrStopped => ServerState is NetworkState.Stopping or NetworkState.Stopped;
        public bool ClientStartingOrStarted => ClientState is NetworkState.Starting or NetworkState.Started;
        public bool ClientStoppingOrStopped => ClientState is NetworkState.Stopping or NetworkState.Stopped;
        public bool HostStartingOrStarted => HostState is NetworkState.Starting or NetworkState.Started;
        public bool HostStoppingOrStopped => HostState is NetworkState.Stopping or NetworkState.Stopped;

        public async UniTask StartServerAsync(ushort port = DEFAULT_PORT) {
            if (ServerStartingOrStarted) throw new Exception("Server already started.");
            ServerState = NetworkState.Starting;
            if (!await SetupServer(port)) {
                ServerState = NetworkState.Stopped;
                return;
            }

            ServerState = NetworkState.Started;
        }

        public async UniTask StopServerAsync() {
            if (ServerStoppingOrStopped) throw new Exception("Server not yet started.");
            ServerState = NetworkState.Stopping;
            await CleanupServer();
            ServerState = NetworkState.Stopped;
        }

        public async UniTask StartClientAsync(string ip = DEFAULT_IP, ushort port = DEFAULT_PORT) {
            if (ClientStartingOrStarted) throw new Exception("Client already started.");
            ClientState = NetworkState.Starting;
            if (!await SetupClient(ip, port)) {
                ClientState = NetworkState.Stopped;
                return;
            }

            ClientState = NetworkState.Started;
        }

        public async UniTask StopClientAsync() {
            if (ClientStoppingOrStopped) throw new Exception("Client not yet started.");
            ClientState = NetworkState.Stopping;
            await CleanupClient();
            ClientState = NetworkState.Stopped;
        }

        public async UniTask StartHostAsync(ushort port = DEFAULT_PORT) {
            if (HostStartingOrStarted) throw new Exception("Host already started.");
            HostState = NetworkState.Starting;
            await StartServerAsync(port);
            if (!ServerStartingOrStarted) {
                HostState = NetworkState.Stopped;
                return;
            }
            
            await StartClientAsync("localhost", port);
            if (!ClientStartingOrStarted) {
                HostState = NetworkState.Stopped;
                return;
            }
            
            HostState = NetworkState.Started;
        }

        public async UniTask StopHostAsync() {
            if (!HostStoppingOrStopped) throw new Exception("Host not yet started.");
            HostState = NetworkState.Stopping;
            await CleanupServer();
            await CleanupClient();
            HostState = NetworkState.Stopped;
        }
        
        protected abstract UniTask<bool> SetupServer(ushort port);
        protected abstract UniTask CleanupServer();
        protected abstract UniTask<bool> SetupClient(string ip, ushort port);
        protected abstract UniTask CleanupClient();
    }
}