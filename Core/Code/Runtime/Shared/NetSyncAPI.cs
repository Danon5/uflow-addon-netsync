using Cysharp.Threading.Tasks;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class NetSyncAPI {
        public static class ServerAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ServerState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton.Transport.ServerStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton.Transport.ServerStoppingOrStopped;
            
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

            public static void Send<T>(in NetworkClient client, in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSend(client, rpc);
            }
            
            public static void SendToAll<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAll(rpc);
            }
        }

        public static class ClientAPI {
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
        }

        public static class HostAPI {
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