using System;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSceneEntity : SceneEntity {
        private bool m_shouldEnableAfterSpawn;
        
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
            if (NetSyncAPI.IsServer)
                NetSyncModule.InternalSingleton.serverSpawnQueue.Enqueue(this);
        }

        public override Entity CreateEntity() {
            if (!NetSyncAPI.NetworkInitialized) return default;
            if (NetSyncAPI.IsServer) {
                if (World == null)
                    throw new Exception("Attempting to create a SceneEntity with no valid world.");
                if (Entity.IsAlive())
                    throw new Exception("Attempting to create a SceneEntity multiple times.");
                m_shouldEnableAfterSpawn = EnabledInInspector;
                Entity = World.CreateEntity(false);
                AddSpecialComponentsBeforeBaking();
                gameObject.SetActive(false);
                return Entity;
            }
            return base.CreateEntity();
        }

        internal void ServerSpawn() {
            BakeAuthoringComponents();
            Entity.SetEnabled(m_shouldEnableAfterSpawn);
            gameObject.SetActive(m_shouldEnableAfterSpawn);
            if (!IsValidPrefab) return;
            LogicHook<PrefabSceneEntityCreatedHook>.Execute(new PrefabSceneEntityCreatedHook(this));
        }
        
        protected override void AddSpecialComponentsBeforeBaking() {
            base.AddSpecialComponentsBeforeBaking();
            if (!NetSyncAPI.IsServer) return;
            Entity.Set(NetSyncAPI.ServerAPI.GetNextNetSynchronizeComponent());
        }
    }
}