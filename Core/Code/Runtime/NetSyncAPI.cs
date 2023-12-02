using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class NetSyncAPI {
        public static bool IsServer => ServerAPI.StartingOrStarted;
        public static bool IsClient => ClientAPI.StartingOrStarted;
        public static bool IsHost => HostAPI.StartingOrStarted;
        public static bool OfflineMode => NetSyncModule.InternalSingleton != null &&
            NetSyncModule.InternalSingleton.Transport.OfflineMode;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetEntityFromNetId(ushort netId) {
            NetSyncModule.ThrowIfNotLoaded();
            return NetSyncModule.InternalSingleton.StateMaps.GetEntityMap().Get(netId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetEntityFromNetId(ushort netId, out Entity entity) {
            NetSyncModule.ThrowIfNotLoaded();
            return NetSyncModule.InternalSingleton.StateMaps.GetEntityMap().TryGet(netId, out entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetStatisticsEnabled(bool state) {
            NetSyncModule.ThrowIfNotLoaded();
            NetSyncModule.InternalSingleton.EnableStatistics = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitializeNetVar<T>(ref NetVar<T> netVar, ushort netId, ushort compId, byte varId, bool interpolate = false) {
            NetSyncModule.ThrowIfNotLoaded();
            netVar ??= new NetVar<T>();
            netVar.Initialize(netId, compId, varId, interpolate);
        }

        public static class ServerAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton == null
                ? default
                : NetSyncModule.InternalSingleton.Transport.ServerState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.ServerStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.ServerStoppingOrStopped;
            
            public static void SubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerStateChangedEvent += action;
            }

            public static void UnsubscribeStateChanged(Action<ConnectionState> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerStateChangedEvent -= action;
            }

            public static void SubscribeClientAuthorized(Action<NetClient> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerClientAuthorizedEvent += action;
            }

            public static void UnsubscribeClientAuthorized(Action<NetClient> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerClientAuthorizedEvent -= action;
            }

            public static void SubscribeClientDisconnected(Action<NetClient> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerClientDisconnectedEvent += action;
            }

            public static void UnsubscribeClientDisconnected(Action<NetClient> action) {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerClientDisconnectedEvent -= action;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Send<T>(in T rpc, NetClient client) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSend(rpc, client);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Send<T>(in T rpc, ushort clientId) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncModule.InternalSingleton.Transport.TryGetClient(clientId, out var client))
                    throw new Exception($"No client with id {clientId}.");
                NetSyncModule.InternalSingleton.Transport.ServerSend(rpc, client);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendToAll<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAll(rpc);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendToAllExcept<T>(in T rpc, NetClient excludedClient) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExcept(rpc, excludedClient);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendToAllExcept<T>(in T rpc, ushort excludedClientId) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncModule.InternalSingleton.Transport.TryGetClient(excludedClientId, out var client))
                    throw new Exception($"No client with id {excludedClientId}.");
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExcept(rpc, client);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendToAllExceptHost<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ServerSendToAllExceptHost(rpc);
            }

            public static void RegisterHandler<T>(in ServerRpcHandlerDelegate<T> handler) where T : INetRpc =>
                NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent += handler;

            public static void UnregisterHandler<T>(in ServerRpcHandlerDelegate<T> handler) where T : INetRpc =>
                NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent -= handler;

            public static NetSynchronize GetNextNetSynchronizeComponent() => new() {
                netId = NetSyncModule.InternalSingleton.NetServerIdStack.GetNextId()
            };

            public static void RecycleNetSynchronizeComponent(in NetSynchronize netSynchronize) =>
                NetSyncModule.InternalSingleton.NetServerIdStack.RecycleId(netSynchronize.netId);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsHostClient(NetClient client) => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.IsHost(client);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsHostClient(ushort clientId) => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.IsHost(clientId);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static IEnumerable<NetClient> GetClientsEnumerable() {
                NetSyncModule.ThrowIfNotLoaded();
                return NetSyncModule.InternalSingleton.Transport.GetClientsEnumerable();
            }
        }

        public static class ClientAPI {
            public static ConnectionState State => NetSyncModule.InternalSingleton.Transport.ClientState;

            public static bool StartingOrStarted => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.ClientStartingOrStarted;

            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.ClientStoppingOrStopped;

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Send<T>(in T rpc) where T : INetRpc {
                NetSyncModule.ThrowIfNotLoaded();
                NetSyncModule.InternalSingleton.Transport.ClientSend(rpc);
            }
        }

        public static class HostAPI {
            public static ConnectionState State =>
                NetSyncModule.InternalSingleton == null ? default : NetSyncModule.InternalSingleton.Transport.HostState;
            public static bool StartingOrStarted => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.HostStartingOrStarted;
            public static bool StoppingOrStopped => NetSyncModule.InternalSingleton != null &&
                NetSyncModule.InternalSingleton.Transport.HostStoppingOrStopped;

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