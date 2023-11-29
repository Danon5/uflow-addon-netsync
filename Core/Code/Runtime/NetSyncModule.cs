using System;
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
        public World World { get; private set; }
        internal static NetSyncModule InternalSingleton { get; private set; }
        internal LiteNetLibTransport Transport { get; }
        internal NetClientAwarenessMap AwarenessMap { get; }
        internal NetIdEntityMap EntityMap { get; }
        internal ushort NextNetworkId { get; set; }
        
        public NetSyncModule() {
            Transport = new LiteNetLibTransport();
            AwarenessMap = new NetClientAwarenessMap();
            EntityMap = new NetIdEntityMap();
        }

        public override UniTask LoadDirectAsync() {
            InternalSingleton = this;
            Transport.ServerStateChangedEvent += OnServerStateChanged;
            Transport.ServerClientAuthorizedEvent += OnServerAuthorizedClient;
            Transport.ServerClientDisconnectedEvent += OnServerDisconnectedClient;
            Transport.ClientStateChangedEvent += OnClientStateChanged;
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
            Transport.ServerStateChangedEvent -= OnServerStateChanged;
            Transport.ServerClientAuthorizedEvent -= OnServerAuthorizedClient;
            Transport.ServerClientDisconnectedEvent -= OnServerDisconnectedClient;
            Transport.ClientStateChangedEvent -= OnClientStateChanged;
            EnsureNetWorldDestroyed();
            InternalSingleton = null;
        }

        public override void Update() {
            if (!Transport.ServerStartingOrStarted && !Transport.ClientStartingOrStarted) return;
            if (World == null) return;
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

        private void EnsureNetWorldCreated() {
            EcsModule<NetWorld>.EnsureLoaded();
            World = EcsModule<NetWorld>.Get().World;
        }
        
        private void EnsureNetWorldDestroyed() {
            EcsModule<NetWorld>.EnsureUnloaded();
            World = null;
        }

        private void OnServerStateChanged(ConnectionState state) {
            switch (state) {
                case ConnectionState.Starting:
                    break;
                case ConnectionState.Started:
                    AwarenessMap.Clear();
                    EntityMap.Clear();
                    EnsureNetWorldCreated();
                    break;
                case ConnectionState.Stopping:
                    break;
                case ConnectionState.Stopped:
                    EnsureNetWorldDestroyed();
                    AwarenessMap.Clear();
                    EntityMap.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
        
        private void OnServerAuthorizedClient(NetClient client) => AwarenessMap.AddClientCache(client);
        
        private void OnServerDisconnectedClient(NetClient client) => AwarenessMap.RemoveClientCache(client);
        
        private void OnClientStateChanged(ConnectionState state) {
            switch (state) {
                case ConnectionState.Starting:
                    break;
                case ConnectionState.Started:
                    AwarenessMap.Clear();
                    EntityMap.Clear();
                    EnsureNetWorldCreated();
                    break;
                case ConnectionState.Stopping:
                    break;
                case ConnectionState.Stopped:
                    EnsureNetWorldDestroyed();
                    AwarenessMap.Clear();
                    EntityMap.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}