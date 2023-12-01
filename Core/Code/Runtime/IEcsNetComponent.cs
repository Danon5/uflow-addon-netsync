using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public interface IEcsNetComponent : IEcsComponent {
        void InitializeNetVars();
    }
}