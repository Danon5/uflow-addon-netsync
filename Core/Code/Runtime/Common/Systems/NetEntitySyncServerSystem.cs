using System;
using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    [ExecuteInGroup(typeof(PostTickSystemGroup))]
    [ExecuteOnServer]
    public sealed class NetEntitySyncServerSystem : BaseSetIterationSystem {
        private IDisposable m_netSynchronizeRemovedSubscription;

        public NetEntitySyncServerSystem(in World world) : base(in world, world.BuildQuery(
                QueryEnabledFlags.Enabled | QueryEnabledFlags.Disabled)
            .With<NetSynchronize>()) { }

        protected override void Setup(World world) {
            NetSyncAPI.ServerAPI.SubscribeClientAuthorized(OnClientAuthorized);
            m_netSynchronizeRemovedSubscription = world.SubscribeEntityComponentRemoved<NetSynchronize>(OnNetSynchronizeRemoved);
        }

        protected override void Cleanup(World world) {
            NetSyncAPI.ServerAPI.UnsubscribeClientAuthorized(OnClientAuthorized);
            m_netSynchronizeRemovedSubscription?.Dispose();
        }

        private void OnClientAuthorized(NetClient client) {
        }

        private static void OnNetSynchronizeRemoved(in Entity entity, in NetSynchronize netSynchronize) {
            var netId = netSynchronize.netId;
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (!awarenessMaps.ClientIsAwareOf(client, netId)) continue;
                awarenessMaps.MakeClientUnawareOf(client, netId);
            }
        }
    }
}