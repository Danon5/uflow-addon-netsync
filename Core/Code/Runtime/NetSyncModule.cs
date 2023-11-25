using Cysharp.Threading.Tasks;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSyncModule : BaseAsyncBehaviourModule<NetSyncModule> {
        private float m_tickRolloverDelta; 
        
        public int TickRate { get; set; } = 60;
        public float TickDelta => 1f / TickRate;
        public int MaxRolloverTicks { get; set; } = 3;
        public int Tick { get; private set; }
        internal static NetSyncModule InternalSingleton { get; private set; }
        internal World World { get; private set; }
        internal LiteNetLibTransport Transport { get; }
        
        public NetSyncModule() => Transport = new LiteNetLibTransport();
        
        public override UniTask LoadDirectAsync() {
            InternalSingleton = this;
            EcsModule<NetWorld>.Load();
            World = EcsModule<NetWorld>.Get().World;
            return base.LoadDirectAsync();
        }

        public override async UniTask UnloadDirectAsync() {
            if (Transport.HostStartingOrStarted)
                await Transport.StopHostAsync();
            else if (Transport.ServerStartingOrStarted)
                await Transport.StopServerAsync();
            else if (Transport.ClientStartingOrStarted)
                await Transport.StopClientAsync();
            Transport.ForceStop();
            EcsModule<NetWorld>.Unload();
            World = null;
            InternalSingleton = null;
        }

        public override void Update() {
            if (!Transport.ServerStartingOrStarted && !Transport.ClientStartingOrStarted) return;
           EnsurePhysicsSimulationSettings();
            m_tickRolloverDelta += Time.deltaTime;
            var ticksExecuted = 0;
            while (m_tickRolloverDelta >= TickDelta) {
                m_tickRolloverDelta -= TickDelta;
                if (ticksExecuted > MaxRolloverTicks) continue;
                World.RunSystemGroup<PreTickSystemGroup>();
                World.RunSystemGroup<TickSystemGroup>();
                Physics.Simulate(TickDelta);
                World.RunSystemGroup<PostTickSystemGroup>();
                ticksExecuted++;
                Tick++;
            }
        }

        private void EnsurePhysicsSimulationSettings() {
            if (Physics.simulationMode != SimulationMode.Script) {
                Physics.simulationMode = SimulationMode.Script;
#if UFLOW_DEBUG_ENABLED
                Debug.LogWarning("NetSync overriding Physics.SimulationMode.");
#endif
            }
            if (Physics2D.simulationMode != SimulationMode2D.Script) {
                Physics2D.simulationMode = SimulationMode2D.Script;
#if UFLOW_DEBUG_ENABLED
                Debug.LogWarning("NetSync overriding Physics2D.SimulationMode.");
#endif
            }
            if (!Mathf.Approximately(Time.fixedDeltaTime - TickDelta, 0f)) {
                Time.fixedDeltaTime = TickDelta;
#if UFLOW_DEBUG_ENABLED
                Debug.LogWarning("NetSync overriding Time.fixedDeltaTime.");
#endif
            }
        }
    }
}