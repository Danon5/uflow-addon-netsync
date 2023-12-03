using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSceneEntity : SceneEntity {
        public override World GetWorld() => EcsModule<NetWorld>.Get().World;

        protected override void Awake() => Initialize(NetSyncAPI.IsServer);

        protected override void AddSpecialComponentsBeforeBaking() {
            base.AddSpecialComponentsBeforeBaking();
            if (!NetSyncAPI.IsServer) return;
            Entity.Set(NetSyncAPI.ServerAPI.GetNextNetSynchronizeComponent());
        }
    }
}