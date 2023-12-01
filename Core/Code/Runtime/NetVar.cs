using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UFlow.Addon.Serialization.Core.Runtime;
using UnityEngine;

// ReSharper disable StaticMemberInGenericType

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Serializable]
    public sealed class NetVar<T> : INetVar {
        private static readonly HashSet<Type> s_validInterpolateTypes = new() {
            typeof(float)
        };
        [ShowInInspector, ReadOnly] private byte m_id;
        [SerializeField, PropertyOrder(-1)] private T m_value;
        private T m_lastSentValue;
        private bool m_isDirty;

        public T Value {
            get => m_value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                m_value = value;
                m_isDirty = !EqualityComparer<T>.Default.Equals(m_value, m_lastSentValue);
            }
        }
        [field: ShowInInspector, ReadOnly] internal bool Interpolate { get; private set; }
        bool INetVar.IsDirty => m_isDirty;
        private bool IsValidInterpolateType => s_validInterpolateTypes.Contains(typeof(T));

        internal void Initialize(byte id, bool interpolate) {
            m_id = id;
            Interpolate = interpolate && IsValidInterpolateType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(in T value) => Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => m_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INetVar.Serialize(ByteBuffer buffer) => SerializationAPI.Serialize(buffer, ref m_value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void INetVar.Deserialize(ByteBuffer buffer) => SerializationAPI.DeserializeInto(buffer, ref m_value); 
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLastSentValue() => m_lastSentValue = m_value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InterpolationTick(float delta) {
            
        }
    }
}