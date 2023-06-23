namespace UFlow.Addon.NetSync.Runtime {
    public struct SyncVar<T> {
        internal T value;

        public SyncVar(in T value) {
            this.value = value;
        }

        public static implicit operator T(in SyncVar<T> sv) => sv.value;
        public static implicit operator SyncVar<T>(in T v) => new(v);
        public static bool operator ==(in SyncVar<T> a, in SyncVar<T> b) => a.Equals(b);
        public static bool operator !=(in SyncVar<T> a, in SyncVar<T> b) => !a.Equals(b);
        public static bool operator ==(in T a, in SyncVar<T> b) => a != null && a.Equals(b.value);
        public static bool operator !=(in T a, in SyncVar<T> b) => a != null && !a.Equals(b.value);
        public override string ToString() => value.ToString();
        public override int GetHashCode() => value.GetHashCode();
        public override bool Equals(object obj) => obj != null && this == (SyncVar<T>)obj;
    }
}