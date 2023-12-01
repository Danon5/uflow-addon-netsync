using System;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NetVarIdAttribute : Attribute {
        internal byte Id { get; }
        
        public NetVarIdAttribute(byte id) => Id = id;
    }
}