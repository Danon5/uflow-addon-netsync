using System;
using System.Linq;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    internal sealed class WorldCreatedFromTypeLogic : ILogic<WorldCreatedFromTypeHook> {
        public void Execute(in WorldCreatedFromTypeHook hook) {
            var netRpcProcessorType = typeof(INetRpcProcessor);
            foreach (var (type, attribute) in UFlowUtils.Reflection.GetAllTypesWithAttribute<ExecuteInWorldAttribute>(
                UFlowUtils.Reflection.CommonExclusionNamespaces)) {
                if (!netRpcProcessorType.IsAssignableFrom(type)) continue;
                if (!attribute.WorldTypes.Contains(hook.worldType)) continue;
                hook.world.AddNetRpcProcessor(Activator.CreateInstance(type) as INetRpcProcessor);
            }
        }
    }
}