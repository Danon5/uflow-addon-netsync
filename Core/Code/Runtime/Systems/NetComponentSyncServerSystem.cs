using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    [ExecuteInGroup(typeof(PostTickSystemGroup))]
    [ExecuteAfter(typeof(NetEntitySyncServerSystem))]
    [ExecuteOnServer]
    public sealed class NetComponentSyncServerSystem : BaseSetIterationSystem {
        public NetComponentSyncServerSystem(in World world) : base(in world, world.BuildQuery()
            .With<NetSynchronize>()) { }

        protected override void IterateEntity(World world, in Entity entity) {
            
        }
    }
}