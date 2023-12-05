using System;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class WorldExtensions {
        public static void AddNetRpcProcessor(this World world, in INetRpcProcessor processor) =>
            NetRpcProcessors.AddProcessor(world.id, processor);

        public static Entity CreateNetworkEntity(this World world, bool enabled = true) {
            if (!NetSyncAPI.IsServer)
                throw new Exception("Attempting to create network entity when server is not started.");
            var entity = world.CreateEntity(enabled);
            entity.Set(NetSyncAPI.ServerAPI.GetNextNetSynchronizeComponent());
            return entity;
        }
    }
}