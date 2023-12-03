using System;
using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    internal sealed class NetEntityCacheSystem : BaseSystem {
        private static readonly Type s_netComponentType = typeof(IEcsNetComponent);
        private IDisposable m_addedSubscription;
        private IDisposable m_removedSubscription;
        public NetEntityCacheSystem(in World world) : base(in world) { }

        protected override void Setup(World world) {
            m_addedSubscription = world.SubscribeEntityComponentAdded<NetSynchronize>(OnNetSynchronizeAdded);
            m_removedSubscription = world.SubscribeEntityComponentRemoved<NetSynchronize>(OnNetSynchronizeRemoved);
        }

        protected override void Cleanup(World world) {
            m_addedSubscription?.Dispose();
            m_removedSubscription?.Dispose();
        }

        private static void OnNetSynchronizeAdded(in Entity entity, ref NetSynchronize component) {
            NetSyncModule.InternalSingleton.StateMaps.GetEntityMap().Add(component.netId, entity);
        }

        private static void OnNetSynchronizeRemoved(in Entity entity, in NetSynchronize component) {
            NetSyncModule.InternalSingleton.StateMaps.GetEntityMap().Remove(component.netId);
            NetSyncModule.InternalSingleton.StateMaps.GetEntityStateMap().Remove(component.netId);
            if (NetSyncAPI.IsServer)
                NetSyncAPI.ServerAPI.RecycleNetSynchronizeComponent(component);
        }
    }
}