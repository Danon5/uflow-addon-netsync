using JetBrains.Annotations;
using UFlow.Addon.Ecs.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Runtime {
    public sealed class NetSyncSystemRunner : BaseSystemRunner<NetSyncSystemRunTiming> {
        [UsedImplicitly]
        private void Awake() {
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        [UsedImplicitly]
        private void Update() {
            RunGroup(NetSyncSystemRunTiming.Update);
        }

        [UsedImplicitly]
        private void LateUpdate() {
            RunGroup(NetSyncSystemRunTiming.LateUpdate);
        }

        [UsedImplicitly]
        private void OnDrawGizmos() {
            RunGroup(NetSyncSystemRunTiming.OnDrawGizmos);
        }

        [UsedImplicitly]
        private void OnGUI() {
            RunGroup(NetSyncSystemRunTiming.OnDrawGUI);
        }
    }
}