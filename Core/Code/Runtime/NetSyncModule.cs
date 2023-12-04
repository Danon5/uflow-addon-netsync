using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSyncModule : BaseAsyncBehaviourModule<NetSyncModule> {
        internal readonly Queue<NetSceneEntity> serverSpawnQueue = new();
        private float m_tickRolloverDelta;

        public int TickRate { get; set; } = 60;
        public float TickDelta => 1f / TickRate;
        public int MaxRolloverTicks { get; set; } = 3;
        public bool EnableStatistics { get; set; }
        public int Tick { get; private set; }
        public float NetworkTime { get; private set; }
        public World World { get; private set; }
        internal static NetSyncModule InternalSingleton { get; private set; }
        internal LiteNetLibTransport Transport { get; }
        internal NetAwarenessMaps ServerAwarenessMaps { get; }
        internal NetStateMaps StateMaps { get; }
        internal UShortIdStack NetServerIdStack { get; }
        
        public NetSyncModule() {
            Transport = new LiteNetLibTransport();
            ServerAwarenessMaps = new NetAwarenessMaps();
            StateMaps = new NetStateMaps();
            NetServerIdStack = new UShortIdStack(1);
            Application.runInBackground = true;
        }

        public override UniTask LoadDirectAsync() {
            InternalSingleton = this;
            Transport.ServerStateChangedEvent += ServerOnStateChanged;
            Transport.ServerClientDisconnectedEvent += ServerOnDisconnectedClient;
            Transport.ClientStateChangedEvent += ClientOnStateChanged;
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
            Transport.ServerStateChangedEvent -= ServerOnStateChanged;
            Transport.ServerClientDisconnectedEvent -= ServerOnDisconnectedClient;
            Transport.ClientStateChangedEvent -= ClientOnStateChanged;
            StateMaps.DisposeSubscriptions();
            EnsureNetWorldDestroyed();
            InternalSingleton = null;
        }

        public override void Update() {
            if (!NetSyncAPI.NetworkInitialized) return;
            if (World == null) return;
            EnsurePhysicsSimulationSettings();
            m_tickRolloverDelta += Time.deltaTime;
            var ticksExecuted = 0;
            while (m_tickRolloverDelta >= TickDelta) {
                m_tickRolloverDelta -= TickDelta;
                if (ticksExecuted > MaxRolloverTicks) continue;
                NetworkTime += TickDelta;
                World?.RunSystemGroup<PreTickSystemGroup>(TickDelta);
                World?.RunSystemGroup<TickSystemGroup>(TickDelta);
                Physics.Simulate(TickDelta);
                World?.RunSystemGroup<PostTickSystemGroup>(TickDelta);
                ticksExecuted++;
                Tick++;
            }
        }

        private void EnsurePhysicsSimulationSettings() {
            if (Physics.simulationMode != SimulationMode.Script) {
                Physics.simulationMode = SimulationMode.Script;
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogWarning("NetSync overriding Physics.SimulationMode.");
#endif
            }
            if (Physics2D.simulationMode != SimulationMode2D.Script) {
                Physics2D.simulationMode = SimulationMode2D.Script;
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogWarning("NetSync overriding Physics2D.SimulationMode.");
#endif
            }
            if (!Mathf.Approximately(Time.fixedDeltaTime - TickDelta, 0f)) {
                Time.fixedDeltaTime = TickDelta;
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogWarning("NetSync overriding Time.fixedDeltaTime.");
#endif
            }
        }

        private void EnsureNetWorldCreated() {
            EcsModule<NetWorld>.EnsureLoaded();
            World = EcsModule<NetWorld>.Get().World;
            StateMaps.RegisterSubscriptionsIfRequired();
        }
        
        private void EnsureNetWorldDestroyed() {
            StateMaps.DisposeSubscriptions();
            EcsModule<NetWorld>.EnqueueEnsureUnloaded();
            World = null;
        }

        private void ResetState() {
            ServerAwarenessMaps.Clear();
            StateMaps.Clear();
            NetServerIdStack.Reset();
            NetworkTime = 0f;
            m_tickRolloverDelta = 0f;
            serverSpawnQueue.Clear();
        }

        private void ServerOnStateChanged(ConnectionState state) {
            switch (state) {
                case ConnectionState.Starting:
                    ResetState();
                    break;
                case ConnectionState.Started:
                    EnsureNetWorldCreated();
                    break;
                case ConnectionState.Stopping:
                    EnsureNetWorldDestroyed();
                    break;
                case ConnectionState.Stopped:
                    ResetState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ServerOnDisconnectedClient(NetClient client) => ServerAwarenessMaps.RemoveClientMaps(client);

        private void ClientOnStateChanged(ConnectionState state) {
            switch (state) {
                case ConnectionState.Starting:
                    ResetState();
                    break;
                case ConnectionState.Started:
                    EnsureNetWorldCreated();
                    break;
                case ConnectionState.Stopping:
                    EnsureNetWorldDestroyed();
                    break;
                case ConnectionState.Stopped:
                    ResetState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}