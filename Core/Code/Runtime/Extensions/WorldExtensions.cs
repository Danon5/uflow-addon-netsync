using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class WorldExtensions {
        public static void AddNetRpcProcessor(this World world, in INetRpcProcessor processor) =>
            NetRpcProcessors.AddProcessor(world.id, processor);

        public static Entity CreateNetworkEntity(this World world) {
            var entity = world.CreateEntity();
            entity.Set(NetSyncAPI.ServerAPI.GetNextValidNetSynchronizeComponent());
            return entity;
        }
    }
}