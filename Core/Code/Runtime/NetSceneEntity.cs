using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSceneEntity : SceneEntity {
        public override World GetWorld() => EcsModule<NetWorld>.Get().World;

        protected override void Awake() {
            if (!NetSyncAPI.NetworkInitialized) {
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogError($"Attempting to instantiate NetSceneEntity when network is not initialized. Name: {name}");
#endif
                Destroy(gameObject);
                return;
            }
            Initialize(NetSyncAPI.IsServer);
        }

        public override Entity CreateEntity() => NetSyncAPI.NetworkInitialized ? base.CreateEntity() : default;

        protected override void AddSpecialComponentsBeforeBaking() {
            base.AddSpecialComponentsBeforeBaking();
            if (!NetSyncAPI.IsServer) return;
            Entity.Set(NetSyncAPI.ServerAPI.GetNextNetSynchronizeComponent());
        }
    }
}