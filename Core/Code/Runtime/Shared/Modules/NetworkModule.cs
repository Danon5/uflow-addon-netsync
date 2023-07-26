using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkModule : BaseModule<NetworkModule> {
        private readonly BaseTransport m_transport;
        
        public NetworkModule() { }

        public NetworkModule(in BaseTransport transport) {
            m_transport = transport;
        }

        public override void UnloadDirect() {
            if (m_transport.HostStartingOrStarted)
                m_transport.StopHostAsync().Forget();
            else if (m_transport.ServerStartingOrStarted)
                m_transport.StopServerAsync().Forget();
            else if (m_transport.ClientStartingOrStarted)
                m_transport.StopClientAsync().Forget();
            m_transport?.Dispose();
        }

        public UniTask StartServerAsync() => m_transport.StartServerAsync();
        
        public UniTask StartServerAsync(ushort port) => m_transport.StartServerAsync(port);
    }
}