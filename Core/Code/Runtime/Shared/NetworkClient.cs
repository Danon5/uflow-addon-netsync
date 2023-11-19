using System;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public readonly struct NetworkClient : IEquatable<NetworkClient>, IComparable<NetworkClient> {
        public readonly ushort id;

        public NetworkClient(ushort id) {
            this.id = id;
        }

        public bool Equals(NetworkClient other) => id == other.id;

        public override bool Equals(object obj) => obj is NetworkClient other && Equals(other);

        public override int GetHashCode() => id;
        
        public int CompareTo(NetworkClient other) => id.CompareTo(other.id);
    }
}