using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkEcsModule : BaseModule<NetworkEcsModule> {
        public World World { get; private set; }

        public override void LoadDirect() {
            
        }

        public static class ServerAPI {
            
        }
        
        public static class ClientAPI {
            
        }
    }
}