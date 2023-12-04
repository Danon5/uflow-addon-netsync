using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;

// ReSharper disable StaticMemberInGenericType

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Serializable]
    public sealed class NetVar<T> : INetVar {
        private const string c_internal = "Internal";
        private static readonly HashSet<Type> s_validInterpolateTypes = new() {
            typeof(float)
        };
        [SerializeField, OnValueChanged(nameof(UpdateDirtyState))] private T m_value;
        [ShowInInspector, ReadOnly, FoldoutGroup(c_internal)] private ushort m_netId;
        [ShowInInspector, ReadOnly, FoldoutGroup(c_internal)] private byte m_varId;
        [ShowInInspector, ReadOnly, FoldoutGroup(c_internal)] private ushort m_compId;
        [ShowInInspector, ReadOnly, FoldoutGroup(c_internal)] private bool m_interpolate;
        private T m_lastSentValue;
        private bool m_isDirty;

        public T Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                m_value = value;
                UpdateDirtyState();
            }
        }
        ushort INetVar.NetId => m_netId;
        ushort INetVar.CompId => m_compId;
        byte INetVar.VarId => m_varId;
        bool INetVar.IsDirty => m_isDirty;
        bool INetVar.Interpolate => m_interpolate;
        private bool IsValidInterpolateType => s_validInterpolateTypes.Contains(typeof(T));

        public static implicit operator T(NetVar<T> netVar) => netVar.Value; 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(in T value) => Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => m_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INetVar.Serialize(ByteBuffer buffer) => SerializationAPI.Serialize(buffer, ref m_value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INetVar.Deserialize(ByteBuffer buffer) => SerializationAPI.DeserializeInto(buffer, ref m_value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INetVar.ResetIsDirty() {
            m_isDirty = false;
            m_lastSentValue = m_value;
        }

        internal void Initialize(ushort netId, ushort compId, byte varId, bool interpolate) {
            m_netId = netId;
            m_compId = compId;
            m_varId = varId;
            m_interpolate = interpolate && IsValidInterpolateType;
            NetSyncModule.InternalSingleton.StateMaps.GetOrCreateComponentState(netId, compId).Add(varId, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InterpolationTick(float delta) {
            
        }

        private void UpdateDirtyState() {
            if (!NetSyncAPI.IsServer) return;
            var wasDirty = m_isDirty;
            m_isDirty = !Unsafe.AreSame(ref m_value, ref m_lastSentValue);
            if (wasDirty) return;
            if (!NetSyncModule.InternalSingleton.StateMaps.TryGetComponentState(m_netId, m_compId, out var componentState)) return;
            componentState.NumVarsDirty++;
        }
    }
}