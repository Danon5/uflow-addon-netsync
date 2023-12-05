using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    internal sealed class WorldCreatedFromTypeSystemsGatheredLogic : ILogic<WorldCreatedFromTypeSystemsGatheredHook> {
        public void Execute(in WorldCreatedFromTypeSystemsGatheredHook hook) {
            if (hook.worldType != typeof(NetWorld)) return;
            var isServer = NetSyncAPI.IsServer || NetSyncAPI.IsHost;
            var isClient = NetSyncAPI.IsClient || NetSyncAPI.IsHost;
            for (var i = hook.systemInfos.Count - 1; i >= 0; i--) {
                var info = hook.systemInfos[i];
                var hasServerAttribute = UFlowUtils.Reflection.HasAttribute<ExecuteOnServerAttribute>(info.systemType);
                var hasClientAttribute = UFlowUtils.Reflection.HasAttribute<ExecuteOnClientAttribute>(info.systemType);
                if (hasServerAttribute == hasClientAttribute) return;
                if (hasServerAttribute && isServer) return;
                if (hasClientAttribute && isClient) return;
                hook.systemInfos.RemoveAt(i);
            }
        }
    }
}