using Sirenix.OdinInspector;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public struct NetSynchronize : IEcsComponent {
        [ReadOnly] public ushort netId;
    }
}