using System;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSceneEntity : SceneEntity {
        public override World GetWorld() => EcsModule<NetWorld>.Get().World;

        protected override void AddSpecialComponentsBeforeBaking() {
            base.AddSpecialComponentsBeforeBaking();
            if (!NetSyncAPI.IsServer)
                throw new Exception("Attempting to create network entity when server is not started.");
            Entity.Set(NetSyncAPI.ServerAPI.GetNextValidNetSynchronizeComponent());
        }
    }
}