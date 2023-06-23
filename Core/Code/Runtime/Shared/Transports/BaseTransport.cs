using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UFlow.Addon.NetSync.Runtime {
    public abstract class BaseTransport : ScriptableObject {
        public const string DEFAULT_IP = "localhost";
        public const ushort DEFAULT_PORT = 7777;

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
        }

        public async UniTask StopServerAsync() {
            if (ServerStoppingOrStopped) throw new Exception("Server not yet started.");
            ServerState = ConnectionState.Stopping;
            await CleanupServer();
            ServerState = ConnectionState.Stopped;
        }

        public async UniTask StartClientAsync(string ip = DEFAULT_IP, ushort port = DEFAULT_PORT) {
            if (ClientStartingOrStarted) throw new Exception("Client already started.");
            ClientState = ConnectionState.Starting;
            if (!await SetupClient(ip, port)) {
                ClientState = ConnectionState.Stopped;
                return;
            }

            ClientState = ConnectionState.Started;
        }

        public async UniTask StopClientAsync() {
            if (ClientStoppingOrStopped) throw new Exception("Client not yet started.");
            ClientState = ConnectionState.Stopping;
            await CleanupClient();
            ClientState = ConnectionState.Stopped;
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
        }

        public async UniTask StopHostAsync() {
            if (!HostStoppingOrStopped) throw new Exception("Host not yet started.");
            HostState = ConnectionState.Stopping;
            await CleanupServer();
            await CleanupClient();
            HostState = ConnectionState.Stopped;
        }
        
        protected abstract UniTask<bool> SetupServer(ushort port);
        protected abstract UniTask CleanupServer();
        protected abstract UniTask<bool> SetupClient(string ip, ushort port);
        protected abstract UniTask CleanupClient();
    }
}