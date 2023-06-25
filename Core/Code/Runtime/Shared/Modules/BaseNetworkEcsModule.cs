using UFlow.Addon.Ecs.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class BaseNetworkEcsModule : BaseModule {
        public World LocalWorld { get; private set; }
    }
}