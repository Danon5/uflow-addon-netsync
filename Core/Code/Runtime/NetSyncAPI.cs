using System;
using Cysharp.Threading.Tasks;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class NetSyncAPI {
        public static bool IsServer => ServerAPI.StartingOrStarted;
        public static bool IsClient => ClientAPI.StartingOrStarted;
        public static bool IsHost => HostAPI.StartingOrStarted;
        public static bool OfflineMode => NetSyncModule.InternalSingleton.Transport.OfflineMode; 
        
        public static class ServerAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ServerState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.ServerStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.ServerStoppingOrStopped;
            
            public static void SubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerStateChangedEvent += action;
            }
            
            public static void UnsubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerStateChangedEvent -= action;
            }

            public static UniTask StartServerAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartServerAsync();
            }

            public static UniTask StartServerAsync(ushort port) {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartServerAsync(port);
            }

            public static UniTask StopServerAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StopServerAsync();
            }

            public static void Send<T>(in T rpc, NetClient client) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSend(rpc, client);
            }
            
            public static void Send<T>(in T rpc, ushort clientId) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncModule.InternalSingleton.Transport.TryGetClient(clientId, out var client))
                    throw new Exception($"No client with id {clientId}.");
                NetSyncModule.InternalSingleton.Transport.ServerSend(rpc, client);
            }
            
            public static void SendToAll<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAll(rpc);
            }
            
            public static void SendToAllExcept<T>(in T rpc, NetClient excludedClient) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExcept(rpc, excludedClient);
            }
            
            public static void SendToAllExcept<T>(in T rpc, ushort excludedClientId) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncModule.InternalSingleton.Transport.TryGetClient(excludedClientId, out var client))
                    throw new Exception($"No client with id {excludedClientId}.");
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExcept(rpc, client);
            }
            
            public static void SendToAllExceptHost<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExceptHost(rpc);
            }
            
            public static void RegisterHandler<T>(in ServerRpcHandlerDelegate<T> handler) where T : INetRpc => 
                NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent += handler;

            public static void UnregisterHandler<T>(in ServerRpcHandlerDelegate<T> handler) where T : INetRpc => 
                NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent -= handler;

            public static NetSynchronize GetNextValidNetSynchronizeComponent() => new() {
                id = NetSyncModule.InternalSingleton.NextNetworkId++
            };
        }

        public static class ClientAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ClientState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.ClientStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.ClientStoppingOrStopped;
            
            public static void SubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ClientStateChangedEvent += action;
            }
            
            public static void UnsubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ClientStateChangedEvent -= action;
            }
            
            public static UniTask StartClientAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartClientAsync();
            }

            public static UniTask StartClientAsync(string ip, ushort port) {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartClientAsync(ip, port);
            }

            public static UniTask StopClientAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StopClientAsync();
            }

            public static void RegisterHandler<T>(in ClientRpcHandlerDelegate<T> handler) where T : INetRpc => 
                NetDeserializer.RpcDeserializer<T>.ClientRpcDeserializedEvent += handler;

            public static void UnregisterHandler<T>(in ClientRpcHandlerDelegate<T> handler) where T : INetRpc => 
                NetDeserializer.RpcDeserializer<T>.ClientRpcDeserializedEvent -= handler;

            public static void Send<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ClientSend(rpc);
            }
        }

        public static class HostAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.HostState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.HostStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.HostStoppingOrStopped;
            
            public static void SubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.HostStateChangedEvent += action;
            }
            
            public static void UnsubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.HostStateChangedEvent -= action;
            }

            public static UniTask StartHostOfflineAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartHostAsync(true);
            }
            
            public static UniTask StartHostOnlineAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartHostAsync();
            }

            public static UniTask StartHostOnlineAsync(ushort port) {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartHostAsync(port: port);
            }

            public static UniTask StopHostAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StopHostAsync();
            }
        }
    }
}