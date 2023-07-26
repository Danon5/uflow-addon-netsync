using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkModule : BaseModule<NetworkModule> {
        private readonly BaseTransport m_transport;
        
        public NetworkModule() { }

        public NetworkModule(in BaseTransport transport) {
            m_transport = transport;
        }

        public UniTask StartServerAsync() => m_transport.StartServerAsync();
        
        public UniTask StartServerAsync(ushort port) => m_transport.StartServerAsync(port);
    }
}