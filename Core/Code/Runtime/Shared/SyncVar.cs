using System;
using MemoryPack;

namespace UFlow.Addon.NetSync.Runtime {
    [MemoryPackable]
    public unsafe partial struct SyncVar<T> where T : unmanaged {
        internal T value;
        private static readonly int s_size = sizeof(T);
        
        public SyncVar(in T value) {
            this.value = value;
        }

        public static implicit operator T(in SyncVar<T> sv) => sv.value;
        public static implicit operator SyncVar<T>(in T v) => new(v);
        public static bool operator ==(SyncVar<T> a, SyncVar<T> b) =>
            new ReadOnlySpan<byte>(&a.value, s_size).SequenceEqual(new ReadOnlySpan<byte>(&b.value, s_size));
        public static bool operator !=(SyncVar<T> a, SyncVar<T> b) =>
            new ReadOnlySpan<byte>(&a.value, s_size).SequenceEqual(new ReadOnlySpan<byte>(&b.value, s_size)) == false;
        public static bool operator ==(T a, SyncVar<T> b) =>
            new ReadOnlySpan<byte>(&a, s_size).SequenceEqual(new ReadOnlySpan<byte>(&b.value, s_size));
        public static bool operator !=(T a, SyncVar<T> b) =>
            new ReadOnlySpan<byte>(&a, s_size).SequenceEqual(new ReadOnlySpan<byte>(&b.value, s_size)) == false;
        public override string ToString() => value.ToString();
        public override int GetHashCode() => value.GetHashCode();
        public override bool Equals(object obj) => obj != null && this == (SyncVar<T>)obj;
    }
}