using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;
using DisposableExtensions = UFlow.Addon.ECS.Core.Runtime.DisposableExtensions;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetStateMaps {
        private readonly EntityMap m_entityMap = new();
        private readonly EntityStateMap m_entityStateMap = new();
        private readonly object[] m_objectBuffer = new object[1];
        private IDisposable m_netVarSubscriptions;
        private bool m_initialized;

        public void RegisterSubscriptionsIfRequired() {
            if (m_initialized) return;
            var world = NetSyncModule.InternalSingleton.World;
            var subscriptionType = typeof(NetVarSubscriptions<>);
            var addedDelegateType = typeof(EntityComponentAddedHandler<>);
            var removedDelegateType = typeof(EntityComponentRemovedHandler<>);
            var worldType = world.GetType();
            var subscriptions = new List<IDisposable>();
            foreach (var componentType in UFlowUtils.Reflection.GetInheritors<IEcsNetComponent>(false,
                UFlowUtils.Reflection.CommonExclusionNamespaces)) {
                // Component added subscription
                var genericAddedDelegateType = addedDelegateType.MakeGenericType(componentType);
                m_objectBuffer[0] = subscriptionType!
                    .MakeGenericType(componentType)!
                    .GetMethod("OnEntityComponentAdded", BindingFlags.Public | BindingFlags.Static)!
                    .CreateDelegate(genericAddedDelegateType);
                var addedSubscription = worldType!
                    .GetMethod("SubscribeEntityComponentAdded", BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(componentType)!
                    .Invoke(world, m_objectBuffer) as IDisposable;
                // Component removed subscription
                var genericRemovedDelegateType = removedDelegateType.MakeGenericType(componentType);
                m_objectBuffer[0] = subscriptionType!
                    .MakeGenericType(componentType)!
                    .GetMethod("OnEntityComponentRemoved", BindingFlags.Public | BindingFlags.Static)!
                    .CreateDelegate(genericRemovedDelegateType);
                var removedSubscription = worldType!
                    .GetMethod("SubscribeEntityComponentRemoved", BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(componentType)!
                    .Invoke(world, m_objectBuffer) as IDisposable;
                subscriptions.Add(addedSubscription);
                subscriptions.Add(removedSubscription);
            }
            m_netVarSubscriptions = DisposableExtensions.MergeIntoGroup(subscriptions);
            m_initialized = true;
        }

        public void DisposeSubscriptions() {
            m_netVarSubscriptions?.Dispose();
            m_initialized = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityMap GetEntityMap() => m_entityMap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityStateMap GetEntityStateMap() => m_entityStateMap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStateMap GetComponentStateMap(ushort netId) => m_entityStateMap.Get(netId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStateMap GetOrCreateComponentStateMap(ushort netId) => m_entityStateMap.GetOrCreate(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VarStateMap GetVarStateMap(ushort netId, ushort compId) => GetComponentStateMap(netId).Get(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VarStateMap GetOrCreateVarStateMap(ushort netId, ushort compId) => GetOrCreateComponentStateMap(netId).GetOrCreate(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public INetVar GetVar(ushort netId, ushort compId, byte varId) => GetVarStateMap(netId, compId).Get(varId);

        public void Clear() {
            m_entityMap.Clear();
            m_entityStateMap.Clear();
        }

        private static class NetVarSubscriptions<T> where T : IEcsNetComponent {
            public static void OnEntityComponentAdded(in Entity entity, ref T component) {
                NetSyncModule.ThrowIfNotLoaded();
                if (!entity.TryGet(out NetSynchronize netSynchronize)) return;
                var netId = netSynchronize.netId;
                var compId = NetTypeIdMaps.ComponentMap.GetNetworkIdFromType(typeof(T));
                component.InitializeNetVars(netId, compId);
                Debug.Log($"Initializing NetVars on entity: NetId {netId}, CompId {compId}");
            }
            
            public static void OnEntityComponentRemoved(in Entity entity, in T component) {
                NetSyncModule.ThrowIfNotLoaded();
                if (!entity.TryGet(out NetSynchronize netSynchronize)) return;
                var netId = netSynchronize.netId;
                var compId = NetTypeIdMaps.ComponentMap.GetNetworkIdFromType(typeof(T));
                NetSyncModule.InternalSingleton.StateMaps.GetComponentStateMap(netId).Remove(compId);
                Debug.Log($"Removing NetVars on entity: NetId {netId}, CompId {compId}");
            }
        }

        public abstract class BaseMap<TKey, TValue> {
            private readonly Dictionary<TKey, TValue> m_map = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(TKey key, TValue value) => m_map.Add(key, value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryAdd(TKey key, TValue value) => m_map.TryAdd(key, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get(TKey key) => m_map[key];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(TKey key, out TValue value) => m_map.TryGetValue(key, out value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Remove(TKey key) => m_map.Remove(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ContainsKey(TKey key) => m_map.ContainsKey(key);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() => m_map.Clear();
        }

        public sealed class EntityMap : BaseMap<ushort, Entity> {
            
        }

        public sealed class EntityStateMap : BaseMap<ushort, ComponentStateMap> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComponentStateMap GetOrCreate(ushort key) {
                if (TryGet(key, out var value)) return value;
                value = new ComponentStateMap();
                Add(key, value);
                return value;
            }
        }

        public sealed class ComponentStateMap : BaseMap<ushort, VarStateMap> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VarStateMap GetOrCreate(ushort key) {
                if (TryGet(key, out var value)) return value;
                value = new VarStateMap();
                Add(key, value);
                return value;
            }
        }
        
        public sealed class VarStateMap : BaseMap<byte, INetVar> {
            
        }
    }
}