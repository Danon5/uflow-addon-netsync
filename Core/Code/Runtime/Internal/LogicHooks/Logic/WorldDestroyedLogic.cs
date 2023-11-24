using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    internal sealed class WorldDestroyedLogic : ILogic<WorldDestroyedHook> {
        public void Execute(in WorldDestroyedHook hook) => NetRpcProcessors.RemoveProcessorsForWorld(hook.worldId);
    }
}