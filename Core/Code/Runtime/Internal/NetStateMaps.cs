using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UFlow.Addon.ECS.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine.Scripting;
using DisposableExtensions = UFlow.Addon.ECS.Core.Runtime.DisposableExtensions;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetStateMaps {
        private readonly EntityMap m_entityMap = new();
        private readonly EntityStateMap m_entityStateMap = new();
        private readonly object[] m_objectBuffer = new object[1];
        private IDisposable m_subscriptions;
        private bool m_initialized;

        public void RegisterSubscriptionsIfRequired() {
            if (m_initialized) return;
            var subscriptions = new List<IDisposable>();
            var world = NetSyncModule.InternalSingleton.World;
            subscriptions.Add(world.SubscribeEntityEnabled(OnEntityEnabled));
            subscriptions.Add(world.SubscribeEntityDisabled(OnEntityDisabled));
            var addedDelegateType = typeof(EntityComponentAddedHandler<>);
            var enabledDelegateType = typeof(EntityComponentEnabledHandler<>);
            var disabledDelegateType = typeof(EntityComponentDisabledHandler<>);
            var removedDelegateType = typeof(EntityComponentRemovedHandler<>);
            foreach (var componentType in UFlowUtils.Reflection.GetInheritors<IEcsNetComponent>(false,
                UFlowUtils.Reflection.CommonExclusionNamespaces)) {
                // Added
                subscriptions.Add(AddGenericSubscription(world, componentType, 
                    addedDelegateType, "OnEntityComponentAdded", "SubscribeEntityComponentAdded"));
                // Enabled
                subscriptions.Add(AddGenericSubscription(world, componentType, 
                    enabledDelegateType, "OnEntityComponentEnabled", "SubscribeEntityComponentEnabled"));
                // Disabled
                subscriptions.Add(AddGenericSubscription(world, componentType, 
                    disabledDelegateType, "OnEntityComponentDisabled", "SubscribeEntityComponentDisabled"));
                // Removed
                subscriptions.Add(AddGenericSubscription(world, componentType, 
                    removedDelegateType, "OnEntityComponentRemoved", "SubscribeEntityComponentRemoved"));
            }
            m_subscriptions = DisposableExtensions.MergeIntoGroup(subscriptions);
            m_initialized = true;
        }

        public void DisposeSubscriptions() {
            m_subscriptions?.Dispose();
            m_initialized = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityMap GetEntityMap() => m_entityMap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasEntity(ushort netId) => m_entityMap.ContainsKey(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(ushort netId) => m_entityMap.Get(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEntity(ushort netId, out Entity entity) => m_entityMap.TryGet(netId, out entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityStateMap GetEntityStateMap() => m_entityStateMap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasEntityState(ushort netId) => m_entityStateMap.ContainsKey(netId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityState GetEntityState(ushort netId) => m_entityStateMap.Get(netId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityState GetOrCreateEntityState(ushort netId) => m_entityStateMap.GetOrCreate(netId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEntityState(ushort netId, out EntityState entityState) {
            if (!HasEntityState(netId)) {
                entityState = default;
                return false;
            }
            entityState = GetEntityState(netId);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponentState(ushort netId, ushort compId) => 
            m_entityStateMap.TryGet(netId, out var entityState) && entityState.ContainsKey(compId);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentState GetComponentState(ushort netId, ushort compId) => 
            GetEntityState(netId).Get(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentState GetOrCreateComponentState(ushort netId, ushort compId) => 
            GetOrCreateEntityState(netId).GetOrCreate(compId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponentState(ushort netId, ushort compId, out ComponentState componentState) {
            if (!HasComponentState(netId, compId)) {
                componentState = default;
                return false;
            }
            componentState = GetComponentState(netId, compId);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public INetVar GetVar(ushort netId, ushort compId, byte varId) => 
            GetComponentState(netId, compId).Get(varId);

        public void Clear() {
            m_entityMap.Clear();
            m_entityStateMap.Clear();
        }

        public void ResetDeltas() {
            foreach (var (netId, entityState) in m_entityStateMap.AsEnumerable()) {
                entityState.EnabledStateDirty = false;
                foreach (var (compId, componentState) in entityState.AsEnumerable()) {
                    componentState.EnabledStateDirty = false;
                    componentState.NumVarsDirty = 0;
                    foreach (var (varId, netVar) in componentState.AsEnumerable())
                        netVar.ResetIsDirty();
                }
            }
        }
        
        private static void OnEntityEnabled(in Entity entity) {
            if (!TryGetEntityStateInfo(entity, out var netId, out var entityState)) return;
            entityState.EnabledStateDirty = true;
        }
        
        private static void OnEntityDisabled(in Entity entity) {
            if (!TryGetEntityStateInfo(entity, out var netId, out var entityState)) return;
            entityState.EnabledStateDirty = true;
        }

        private static bool TryGetEntityStateInfo(in Entity entity,
                                            out ushort netId, out EntityState entityState) {
            netId = default;
            entityState = default;
            NetSyncModule.ThrowIfNotLoaded();
            if (!NetSyncAPI.NetworkInitialized) return false;
            if (!entity.TryGet(out NetSynchronize netSynchronize)) return false;
            netId = netSynchronize.netId;
            return NetSyncModule.InternalSingleton.StateMaps.TryGetEntityState(netId, out entityState);
        }
        
        private IDisposable AddGenericSubscription(World world, Type componentType, Type delegateType,
                                                       in string handlerMethod, in string eventMethod) {
            var worldType = world.GetType();
            var subscriptionType = typeof(NetVarSubscriptions<>);
            var genericDelegateType = delegateType.MakeGenericType(componentType);
            m_objectBuffer[0] = subscriptionType!
                .MakeGenericType(componentType)!
                .GetMethod(handlerMethod, BindingFlags.Public | BindingFlags.Static)!
                .CreateDelegate(genericDelegateType);
            return worldType!
                .GetMethod(eventMethod, BindingFlags.Public | BindingFlags.Instance)!
                .MakeGenericMethod(componentType)!
                .Invoke(world, m_objectBuffer) as IDisposable;
        }

        private static class NetVarSubscriptions<T> where T : IEcsNetComponent {
            [Preserve]
            public static void OnEntityComponentAdded(in Entity entity, ref T component) {
                if (!TryGetIds(entity, out var netId, out var compId)) return;
                NetSyncModule.InternalSingleton.StateMaps.GetOrCreateComponentState(netId, compId);
                component.InitializeNetVars(netId, compId);
            }

            [Preserve]
            public static void OnEntityComponentEnabled(in Entity entity, ref T component) {
                if (!TryGetAllStateInfo(entity, out var netId, out var compId, out var entityState, out var componentState)) return;
                componentState.EnabledStateDirty = true;
            }
            
            [Preserve]
            public static void OnEntityComponentDisabled(in Entity entity, ref T component) {
                if (!TryGetAllStateInfo(entity, out var netId, out var compId, out var entityState, out var componentState)) return;
                componentState.EnabledStateDirty = true;
            }
            
            [Preserve]
            public static void OnEntityComponentRemoved(in Entity entity, in T component) {
                if (!TryGetIds(entity, out var netId, out var compId)) return;
                NetSyncModule.InternalSingleton.StateMaps.GetEntityState(netId).Remove(compId);
            }

            private static bool TryGetIds(in Entity entity, 
                                          out ushort netId, out ushort compId) {
                netId = default;
                compId = default;
                NetSyncModule.ThrowIfNotLoaded();
                if (!NetSyncAPI.NetworkInitialized) return false;
                if (!entity.TryGet(out NetSynchronize netSynchronize)) return false;
                netId = netSynchronize.netId;
                compId = NetTypeIdMaps.ComponentMap.GetNetworkIdFromType(typeof(T));
                return true;
            }

            private static bool TryGetAllStateInfo(in Entity entity, 
                                                out ushort netId, out ushort compId,
                                                out EntityState entityState, out ComponentState componentState) {
                entityState = default;
                componentState = default;
                if (!TryGetIds(entity, out netId, out compId)) return false;
                var stateMaps = NetSyncModule.InternalSingleton.StateMaps;
                return stateMaps.TryGetEntityState(netId, out entityState) && 
                    stateMaps.TryGetComponentState(netId, compId, out componentState);
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

        public sealed class EntityStateMap : BaseStateMap<ushort, EntityState> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntityState GetOrCreate(ushort key) {
                if (TryGet(key, out var value)) 
                    return value;
                value = new EntityState();
                Add(key, value);
                return value;
            }
        }

        public sealed class EntityState : BaseStateMap<ushort, ComponentState> {
            public bool EnabledStateDirty { get; set; }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ComponentState GetOrCreate(ushort key) {
                if (TryGet(key, out var value))
                    return value;
                value = new ComponentState();
                Add(key, value);
                return value;
            }
        }

        public sealed class ComponentState : BaseStateMap<byte, INetVar> {
            public bool EnabledStateDirty { get; set; }
            public byte NumVarsDirty { get; set; }
        }
    }
}