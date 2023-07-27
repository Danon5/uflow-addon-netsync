using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkModule : BaseAsyncBehaviourModule<NetworkModule> {
        private readonly BaseTransport m_transport;

        public NetworkModule() {
            m_transport = new LiteNetTransport();
        }

        public NetworkModule(in BaseTransport transport) {
            m_transport = transport;
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

        public override void LateUpdate() => m_transport.PollEvents();

        public UniTask StartServerAsync() => m_transport.StartServerAsync();
        
        public UniTask StartServerAsync(ushort port) => m_transport.StartServerAsync(port);

        public UniTask StopServerAsync() => m_transport.StopServerAsync();

        public UniTask StartClientAsync() => m_transport.StartClientAsync();

        public UniTask StartClientAsync(string ip, ushort port) => m_transport.StartClientAsync(ip, port);

        public UniTask StartHostAsync() => m_transport.StartHostAsync();
        
        public UniTask StartHostAsync(ushort port) => m_transport.StartHostAsync(port);
        
    }
}