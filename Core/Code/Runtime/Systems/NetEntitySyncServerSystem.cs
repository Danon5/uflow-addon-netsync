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

        protected override void Setup(World world) => m_netSynchronizeRemovedSubscription =
            world.SubscribeEntityComponentRemoved<NetSynchronize>(OnNetSynchronizeRemoved);

        protected override void Cleanup(World world) => m_netSynchronizeRemovedSubscription?.Dispose();

        protected override void IterateEntity(World world, in Entity entity) {
            ref var netSynchronize = ref entity.Get<NetSynchronize>();
            var netId = netSynchronize.netId;
            var netSyncModule = NetSyncModule.InternalSingleton;
            var awarenessMaps = netSyncModule.ServerAwarenessMaps;
            foreach (var client in NetSyncAPI.ServerAPI.GetClientsEnumerable()) {
                if (NetSyncAPI.ServerAPI.IsHostClient(client)) continue;
                if (ClientShouldBeAwareOf(client, netId)) {
                    if (awarenessMaps.ClientIsAwareOf(client, netId)) {
                        // send state deltas since client is already aware of entity
                        // TrySendStateDelta(client, netId);
                        // also needs to check if the client should be aware of each component and NetVar
                        // not being aware of a component means that it should be removed, but not being aware of a NetVar just means
                        // no data should be sent
                        continue;
                    }
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
#if UFLOW_DEBUG_ENABLED
            DebugAPI.LogMessage($"Server sending packet. Type: {NetPacketType.DestroyEntity}, ClientID: {client.id}, NetID: {netId}");
#endif
            netSyncModule.Transport.EndWrite();
            netSyncModule.Transport.SendBufferPayloadToClient(client);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ClientShouldBeAwareOf(NetClient client, ushort netId) => true;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ClientShouldBeAwareOf(NetClient client, ushort netId, ushort compId) => 
            ClientShouldBeAwareOf(client, netId) && true;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ClientShouldBeAwareOf(NetClient client, ushort netId, ushort compId, byte varId) => 
            ClientShouldBeAwareOf(client, netId, compId) && true;
    }
}