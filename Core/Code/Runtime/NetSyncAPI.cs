using System;
using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class NetSyncAPI {
        public static class ServerAPI {
            public static event Action<ConnectionState> StateChangedEvent;
            public static event Action<NetClient> ClientAuthorizedEvent;
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ServerState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.ServerStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.ServerStoppingOrStopped;

            static ServerAPI() {
                UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;
                LiteNetLibTransport.ServerStateChangedEvent += state => StateChangedEvent?.Invoke(state);
                LiteNetLibTransport.ServerClientAuthorizedEvent += client => ClientAuthorizedEvent?.Invoke(client);
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

            public static void Send<T>(in T rpc, in NetClient client) where T : INetRpc {
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
            
            public static void SendToAllExcept<T>(in T rpc, in NetClient excludedClient) where T : INetRpc {
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
            
            private static void ClearStaticCache() {
                StateChangedEvent = default;
                ClientAuthorizedEvent = default;
            }
        }

        public static class ClientAPI {
            public static event Action<ConnectionState> StateChangedEvent;
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ClientState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.ClientStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.ClientStoppingOrStopped;
            
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
            public static event Action<ConnectionState> StateChangedEvent;
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.HostState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.HostStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.HostStoppingOrStopped;
            
            public static UniTask StartHostAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartHostAsync();
            }

            public static UniTask StartHostAsync(ushort port) {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StartHostAsync(port);
            }

            public static UniTask StopHostAsync() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.StopHostAsync();
            }
        }
    }
}