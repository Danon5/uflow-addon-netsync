using System;
using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
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

        // Delta state
        protected override void IterateEntity(World world, in Entity entity) {
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                ref var netSynchronize = ref entity.Get<NetSynchronize>();
                var netId = netSynchronize.netId;
                if (!awarenessMaps.ClientShouldBeAwareOf(client, netId)) continue;
                if (awarenessMaps.ClientIsAwareOf(client, netId))
                    SendEntityDeltaPacketToClientIfRequired(client, entity, netId);
                else {
                    SendCreateEntityPacketToClient(client, entity, netId);
                    awarenessMaps.MakeClientAwareOf(client, netId);
                }
            }
            netSyncModule.StateMaps.ResetDeltas();
        }

        // Initial state
        private void OnClientAuthorized(NetClient client) {
            if (NetSyncAPI.ServerAPI.IsHostClient(client)) return;
            foreach (var entity in Query) {
                ref var netSynchronize = ref entity.Get<NetSynchronize>();
                var netId = netSynchronize.netId;
                var netSyncModule = NetSyncModule.InternalSingleton;
                var awarenessMaps = netSyncModule.ServerAwarenessMaps;
                if (awarenessMaps.ClientShouldBeAwareOf(client, netId)) {
                    if (awarenessMaps.ClientIsAwareOf(client, netId)) continue;
                    SendCreateEntityPacketToClient(client, entity, netId);
                    awarenessMaps.MakeClientAwareOf(client, netId);
                }
                else {
                    if (!awarenessMaps.ClientIsAwareOf(client, netId)) continue;
                    SendDestroyEntityPacketToClient(client, entity, netId);
                    awarenessMaps.MakeClientUnawareOf(client, netId);
                }
            }
        }

        // State removal
        private static void OnNetSynchronizeRemoved(in Entity entity, in NetSynchronize netSynchronize) {
            var netId = netSynchronize.netId;
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (!awarenessMaps.ClientIsAwareOf(client, netId)) continue;
                SendDestroyEntityPacketToClient(client, entity, netId);
                awarenessMaps.MakeClientUnawareOf(client, netId);
            }
        }

        private static void SendCreateEntityPacketToClient(NetClient client, in Entity entity, ushort netId) {
            var isSceneEntity = entity.TryGet(out SceneEntityRef sceneEntityRef);
            var netSyncModule = NetSyncModule.InternalSingleton;
            if (isSceneEntity) {
                var prefabId = NetSyncPrefabCache.Get().GetNetworkIdFromGuid(sceneEntityRef.value.Guid);
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateSceneEntity);
                NetSerializer.SerializeCreateSceneEntity(netSyncModule.Transport.Buffer, entity, netId, prefabId);
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogMessage($"Server sending packet. Type: {NetPacketType.CreateSceneEntity}, " +
                    $"ClientID: {client.id}, NetID: {netId}, PrefabID: {prefabId}");
#endif
            }
            else {
                netSyncModule.Transport.BeginWrite(NetPacketType.CreateEntity);
                NetSerializer.SerializeCreateEntity(netSyncModule.Transport.Buffer, entity, netId);
#if UFLOW_DEBUG_ENABLED
                DebugAPI.LogMessage($"Server sending packet. Type: {NetPacketType.CreateEntity}, ClientID: {client.id}, NetID: {netId}");
#endif
            }
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static void SendDestroyEntityPacketToClient(NetClient client, in Entity entity, ushort netId) {
            var netSyncModule = NetSyncModule.InternalSingleton;
            netSyncModule.Transport.BeginWrite(NetPacketType.DestroyEntity);
            NetSerializer.SerializeDestroyEntity(netSyncModule.Transport.Buffer, netId);
            netSyncModule.Transport.EndWrite();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Server sending packet. Type: {NetPacketType.DestroyEntity}, ClientID: {client.id}, NetID: {netId}");
#endif
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        private static void SendEntityDeltaPacketToClientIfRequired(NetClient client, in Entity entity, ushort netId) {
            if (!NetSerializer.TrySerializeDeltaEntityStateIntoTempBuffer(client, entity, netId)) return;
            var netSyncModule = NetSyncModule.InternalSingleton;
            netSyncModule.Transport.BeginWrite(NetPacketType.EntityDelta);
            NetSerializer.SerializeDeltaEntityState(netSyncModule.Transport.Buffer);
            netSyncModule.Transport.EndWrite();
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Server sending packet. Type: {NetPacketType.EntityDelta}, ClientID: {client.id}, NetID: {netId}");
#endif
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }
    }
}