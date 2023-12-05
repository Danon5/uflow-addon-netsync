using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    [ExecuteInGroup(typeof(PreTickSystemGroup))]
    [ExecuteAfter(typeof(NetEntitySpawnerServerSystem))]
    public sealed class NetEntitySpawnerServerSystem : BaseRunSystem {
        public NetEntitySpawnerServerSystem(in World world) : base(in world) { }

        protected override void Run(World world) {
            while (NetSyncModule.InternalSingleton.serverSpawnQueue.TryDequeue(out var netSceneEntity))
                netSceneEntity.ServerSpawn();
        }
    }
}