using System;
using System.Runtime.CompilerServices;

namespace UFlow.Addon.NetSync.Core.Runtime.Attributes {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NetMemberAttribute : Attribute {
        internal readonly int order;

        public NetMemberAttribute([CallerLineNumber] int order = default) {
            this.order = order;
        }
    }
}