using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkEcsModule : BaseModule<NetworkEcsModule> {
        public World NetworkWorld { get; private set; }
        
        
    }
}