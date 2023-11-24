using System;
using System.Collections.Generic;
using UFlow.Core.Runtime;

// ReSharper disable SuspiciousTypeConversion.Global

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetRpcProcessors {
        private static Dictionary<Type, List<INetRpcProcessor>>[] s_processors;

        static NetRpcProcessors() {
            s_processors = Array.Empty<Dictionary<Type, List<INetRpcProcessor>>>();
            UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;
        }

        public static void AddProcessor(short worldId, INetRpcProcessor processor) {
            UFlowUtils.Collections.EnsureIndex(ref s_processors, worldId);
            s_processors[worldId] ??= new Dictionary<Type, List<INetRpcProcessor>>();
            var processorTypeMap = s_processors[worldId];
            var processorType = processor.GetType();
            if (!processorTypeMap.TryGetValue(processorType, out var processors)) {
                processors = new List<INetRpcProcessor>();
                s_processors[worldId].Add(processorType, processors);
            }
            processors.Add(processor);
        }

        public static void RemoveProcessorsForWorld(short worldId) {
            if (s_processors == null) return;
            if (worldId >= s_processors.Length) return;
            if (s_processors[worldId] == null) return;
            foreach (var (type, processors) in s_processors[worldId]) {
                if (processors == null) continue;
                foreach (var processor in processors) {
                    if (processor is IDisposable disposable)
                        disposable.Dispose();
                }
            }
            s_processors[worldId].Clear();
        }

        public static void RunProcessors(short worldId) {
            if (s_processors == null) return;
            if (worldId >= s_processors.Length) return;
            if (s_processors[worldId] == null) return;
            foreach (var (type, processors) in s_processors[worldId]) {
                if (processors == null) continue;
                foreach (var processor in processors)
                    processor.ProcessAll();
            }
        }

        private static void ClearStaticCache() => s_processors = Array.Empty<Dictionary<Type, List<INetRpcProcessor>>>();
    }
}