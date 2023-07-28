using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkModule : BaseAsyncBehaviourModule<NetworkModule> {
        private static NetworkModule s_internalSingleton;
        private readonly BaseTransport m_transport;

        public NetworkModule() {
            m_transport = new LiteNetTransport();
        }

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
        }

        public override void FinalUpdate() => m_transport.PollEvents();

        public static class ServerAPI {
            public static UniTask StartServerAsync() => s_internalSingleton.m_transport.StartServerAsync();
        
            public static UniTask StartServerAsync(ushort port) => s_internalSingleton.m_transport.StartServerAsync(port);

            public static UniTask StopServerAsync() => s_internalSingleton.m_transport.StopServerAsync();
        }
        
        public static class ClientAPI {
            public static UniTask StartClientAsync() => s_internalSingleton.m_transport.StartClientAsync();

            public static UniTask StartClientAsync(string ip, ushort port) => s_internalSingleton.m_transport.StartClientAsync(ip, port);

            public static UniTask StopClientAsync() => s_internalSingleton.m_transport.StopClientAsync();
        }

        public static class HostAPI {
            public static UniTask StartHostAsync() => s_internalSingleton.m_transport.StartHostAsync();
            
            public static UniTask StartHostAsync(ushort port) => s_internalSingleton.m_transport.StartHostAsync(port);
            
            public static UniTask StopHostAsync() => s_internalSingleton.m_transport.StopHostAsync();
        }
    }
}