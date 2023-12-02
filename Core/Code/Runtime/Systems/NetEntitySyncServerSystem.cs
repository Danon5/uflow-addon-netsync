using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine;
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
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (ClientShouldBeAwareOf(client, netSynchronize.netId)) {
                    if (!awarenessMaps.ClientIsAwareOf(client, netSynchronize.netId)) {
                        SendCreateEntityPacketToClient(client, entity);
                        awarenessMaps.MakeClientAwareOf(client, netSynchronize.netId);
                    }
                    SyncComponentsToClient(client, entity);
                }
                else {
                    if (!awarenessMaps.ClientIsAwareOf(client, netSynchronize.netId)) continue;
                    SendDestroyEntityPacketToClient(client, entity);
                    awarenessMaps.MakeClientUnawareOf(client, netSynchronize.netId);
                }
            }
        }

        protected override void EntityRemoved(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (!awarenessMaps.ClientIsAwareOf(client, netSynchronize.netId)) continue;
                SendDestroyEntityPacketToClient(client, entity);
                awarenessMaps.MakeClientUnawareOf(client, netSynchronize.netId);
            }
        }

        private static void SendCreateEntityPacketToClient(NetClient client, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netId = netSynchronize.netId;
            var isSceneEntity = entity.TryGet(out SceneEntityRef sceneEntityRef);
            var netSyncModule = NetSyncModule.InternalSingleton;
            if (isSceneEntity) {
                var prefabId = NetSyncPrefabCache.Get().GetNetworkIdFromGuid(sceneEntityRef.value.Guid);
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateSceneEntity);
                NetSerializer.SerializeCreateSceneEntity(netSyncModule.Transport.Buffer, netId, prefabId);
#if UFLOW_DEBUG_ENABLED
                Debug.Log(
                    $"Server sending packet. Type: {NetPacketType.CreateSceneEntity}, ClientID: {client.id}, NetID: {netId}, PrefabID: {prefabId}");
#endif
            }
            else {
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateEntity);
                NetSerializer.SerializeCreateEntity(netSyncModule.Transport.Buffer, netId);
#if UFLOW_DEBUG_ENABLED
                Debug.Log($"Server sending packet. Type: {NetPacketType.CreateEntity}, ClientID: {client.id}, NetID: {netId}");
#endif
            }
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static void SendDestroyEntityPacketToClient(NetClient client, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netId = netSynchronize.netId;
            var netSyncModule = NetSyncModule.InternalSingleton;
            netSyncModule.Transport.BeginWrite(NetPacketType.DestroyEntity);
            NetSerializer.SerializeDestroyEntity(netSyncModule.Transport.Buffer, netId);
#if UFLOW_DEBUG_ENABLED
            Debug.Log($"Server sending packet. Type: {NetPacketType.DestroyEntity}, ClientID: {client.id}, NetID: {netId}");
#endif
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static void SyncComponentsToClient(NetClient client, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netId = netSynchronize.netId;
            if (!NetSyncModule.InternalSingleton.StateMaps.TryGetComponentStateMap(netId, out var componentStateMap)) return;
            foreach (var (compId, varStateMap) in componentStateMap.AsEnumerable()) {
                foreach (var (varId, netVar) in varStateMap.AsEnumerable()) {
                    
                }
            }
        }

        private static bool ClientShouldBeAwareOf(NetClient client, ushort netId) => true;
    }
}