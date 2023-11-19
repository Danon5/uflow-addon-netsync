using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Addon.NetSync.Core.Runtime.SystemGroups;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetworkWorld))]
    [ExecuteInGroup(typeof(PreTickSystemGroup))]
    public sealed class NetRpcProcessorSystem : BaseRunSystem {
        public NetRpcProcessorSystem(in World world) : base(in world) { }

        protected override void Run(World world) {
            
        }
    }
}