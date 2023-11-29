using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    internal sealed class NetEntityCacheSystem : BaseSetCallbackSystem {
        public NetEntityCacheSystem(in World world) : base(in world, world.BuildQuery()
            .With<NetSynchronize>()) { }

        protected override void EntityAdded(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            NetSyncModule.InternalSingleton.EntityMap.Add(netSynchronize.netId, entity);
        }

        protected override void EntityRemoved(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            NetSyncModule.InternalSingleton.EntityMap.Remove(netSynchronize.netId);
        }
    }
}