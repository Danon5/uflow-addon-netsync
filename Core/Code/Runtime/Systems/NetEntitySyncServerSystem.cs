using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    [ExecuteInGroup(typeof(PostTickSystemGroup))]
    [ExecuteOnServer]
    public sealed class NetEntitySyncServerSystem : BaseSetIterationCallbackSystem {
        public NetEntitySyncServerSystem(in World world) : base(in world, world.BuildQuery()
            .With<NetSynchronize>()) { }

        protected override void IterateEntity(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMap = netSyncModule.AwarenessMap;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (ClientShouldBeAwareOf(client, netSynchronize.netId)) {
                    if (awarenessMap.ClientIsAwareOf(client, netSynchronize.netId)) continue;
                    CreateEntityForClient(client, entity);
                    awarenessMap.MarkClientAware(client, netSynchronize.netId);
                }
                else {
                    if (!awarenessMap.ClientIsAwareOf(client, netSynchronize.netId)) continue;
                    DestroyEntityForClient(client, entity);
                    awarenessMap.MarkClientUnaware(client, netSynchronize.netId);
                }
            }
        }

        protected override void EntityRemoved(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMap = netSyncModule.AwarenessMap;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (!awarenessMap.ClientIsAwareOf(client, netSynchronize.netId)) continue;
                DestroyEntityForClient(client, entity);
                awarenessMap.MarkClientUnaware(client, netSynchronize.netId);
            }
        }

        private static void CreateEntityForClient(NetClient client, in Entity entity) {
            var netSyncModule = NetSyncModule.InternalSingleton;
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var isSceneEntity = entity.TryGet(out SceneEntityRef sceneEntityRef);
            if (isSceneEntity) {
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateSceneEntity);
                NetSerializer.SerializeCreateEntity(netSyncModule.Transport.Buffer, netSynchronize.netId);
                var prefabId = NetSyncPrefabCache.Get().GetNetworkIdFromGuid(sceneEntityRef.value.Guid);
                NetSerializer.SerializeCreateEntity(netSyncModule.Transport.Buffer, prefabId);
            }
            else {
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateEntity);
                NetSerializer.SerializeCreateEntity(netSyncModule.Transport.Buffer, netSynchronize.netId);
            }
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static void DestroyEntityForClient(NetClient client, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netSyncModule = NetSyncModule.InternalSingleton;
            netSyncModule.Transport.BeginWrite(NetPacketType.DestroyEntity);
            NetSerializer.SerializeDestroyEntity(netSyncModule.Transport.Buffer, netSynchronize.netId);
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static bool ClientShouldBeAwareOf(NetClient client, ushort netId) => true;
    }
}