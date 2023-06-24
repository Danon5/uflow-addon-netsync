using UFlow.Addon.Ecs.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Runtime.Modules {
    public sealed class BaseNetworkEcsModule : BaseModule {
        public World LocalWorld { get; private set; }
    }
}