using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;
using UnityEngine.Scripting;
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
        public bool HasComponentStateMap(ushort netId) => m_entityStateMap.ContainsKey(netId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStateMap GetComponentStateMap(ushort netId) => m_entityStateMap.Get(netId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentStateMap GetOrCreateComponentStateMap(ushort netId) => m_entityStateMap.GetOrCreate(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponentStateMap(ushort netId, out ComponentStateMap componentStateMap) {
            if (!HasComponentStateMap(netId)) {
                componentStateMap = default;
                return false;
            }
            componentStateMap = GetComponentStateMap(netId);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasVarStateMap(ushort netId, ushort compId) => 
            m_entityStateMap.TryGet(netId, out var componentStateMap) && 
            componentStateMap.TryGet(compId, out var varStateMap) && varStateMap != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VarStateMap GetVarStateMap(ushort netId, ushort compId) => 
            GetComponentStateMap(netId).Get(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VarStateMap GetOrCreateVarStateMap(ushort netId, ushort compId) => 
            GetOrCreateComponentStateMap(netId).GetOrCreate(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVarStateMap(ushort netId, ushort compId, out VarStateMap varStateMap) {
            if (!HasVarStateMap(netId, compId)) {
                varStateMap = default;
                return false;
            }
            varStateMap = GetVarStateMap(netId, compId);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public INetVar GetVar(ushort netId, ushort compId, byte varId) => 
            GetVarStateMap(netId, compId).Get(varId);

        public void Clear() {
            m_entityMap.Clear();
            m_entityStateMap.Clear();
        }

        private static class NetVarSubscriptions<T> where T : IEcsNetComponent {
            [Preserve]
            public static void OnEntityComponentAdded(in Entity entity, ref T component) {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncAPI.NetworkInitialized) return;
                if (!entity.TryGet(out NetSynchronize netSynchronize)) return;
                var netId = netSynchronize.netId;
                var compId = NetTypeIdMaps.ComponentMap.GetNetworkIdFromType(typeof(T));
                NetSyncModule.InternalSingleton.StateMaps.GetOrCreateComponentStateMap(netId).Add(compId, default);
                component.InitializeNetVars(netId, compId);
            }
            
            [Preserve]
            public static void OnEntityComponentRemoved(in Entity entity, in T component) {
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncAPI.NetworkInitialized) return;
                if (!entity.TryGet(out NetSynchronize netSynchronize)) return;
                var netId = netSynchronize.netId;
                var compId = NetTypeIdMaps.ComponentMap.GetNetworkIdFromType(typeof(T));
                NetSyncModule.InternalSingleton.StateMaps.GetComponentStateMap(netId).Remove(compId);
            }
        }

        public abstract class BaseStateMap<TKey, TValue> {
            private readonly Dictionary<TKey, TValue> m_map = new();

            public int Count => m_map.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Add(TKey key, TValue value) => m_map.Add(key, value);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool TryAdd(TKey key, TValue value) => m_map.TryAdd(key, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual TValue Get(TKey key) => m_map[key];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool TryGet(TKey key, out TValue value) => m_map.TryGetValue(key, out value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool Remove(TKey key) => m_map.Remove(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual bool ContainsKey(TKey key) => m_map.ContainsKey(key);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Clear() => m_map.Clear();

            public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable() => m_map;
        }

        public sealed class EntityMap : BaseStateMap<ushort, Entity> { }

        public sealed class EntityStateMap : BaseStateMap<ushort, ComponentStateMap> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComponentStateMap GetOrCreate(ushort key) {
                if (TryGet(key, out var value)) 
                    return value;
                value = new ComponentStateMap();
                Add(key, value);
                return value;
            }
        }

        public sealed class ComponentStateMap : BaseStateMap<ushort, VarStateMap> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public VarStateMap GetOrCreate(ushort key) {
                if (TryGet(key, out var value)) {
                    value ??= new VarStateMap();
                    return value;
                }
                value = new VarStateMap();
                Add(key, value);
                return value;
            }
        }
        
        public sealed class VarStateMap : BaseStateMap<byte, INetVar> { }
    }
}