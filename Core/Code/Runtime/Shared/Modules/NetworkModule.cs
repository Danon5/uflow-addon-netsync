using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkModule : BaseAsyncBehaviourModule<NetworkModule> {
        private static NetworkModule s_internalSingleton;
        private readonly BaseTransport m_transport;
        private readonly Dictionary<ushort, NetClient> m_connectedClients = new();

        public NetworkModule() : this(new LiteNetTransport()) { }

        public NetworkModule(in BaseTransport transport) {
            m_transport = transport;
        }

        public override UniTask LoadDirectAsync() {
            s_internalSingleton = this;
            return base.LoadDirectAsync();
        }

        public override async UniTask UnloadDirectAsync() {
            if (m_transport.HostStartingOrStarted)
                await m_transport.StopHostAsync();
            else if (m_transport.ServerStartingOrStarted)
                await m_transport.StopServerAsync();
            else if (m_transport.ClientStartingOrStarted)
                await m_transport.StopClientAsync();
            m_transport.ForceStop();
            s_internalSingleton = null;
        }

        public override void FinalUpdate() => m_transport.PollEvents();
        
        public static class ServerAPI {
            public static UniTask StartServerAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartServerAsync();
            }

            public static UniTask StartServerAsync(ushort port) {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartServerAsync(port);
            }

            public static UniTask StopServerAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StopServerAsync();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SendToAll<T>(in T rpc, NetReliabilityType netReliabilityType = NetReliabilityType.ReliableOrdered) 
                where T : INetRpc {
                
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Send<T>(in T rpc, in NetClient client, NetReliabilityType netReliabilityType = NetReliabilityType.ReliableOrdered)
                where T : INetRpc {
                
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Send<T>(in T rpc, in ushort clientId, NetReliabilityType netReliabilityType = NetReliabilityType.ReliableOrdered)
                where T : INetRpc {
                
            }
        }
        
        public static class ClientAPI {
            public static UniTask StartClientAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartClientAsync();
            }

            public static UniTask StartClientAsync(string ip, ushort port) {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartClientAsync(ip, port);
            }

            public static UniTask StopClientAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StopClientAsync();
            }
        }

        public static class HostAPI {
            public static UniTask StartHostAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartHostAsync();
            }

            public static UniTask StartHostAsync(ushort port) {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StartHostAsync(port);
            }

            public static UniTask StopHostAsync() {
                ThrowIfNotLoaded();
                return s_internalSingleton.m_transport.StopHostAsync();
            }
        }
    }
}