using System;

namespace UFlow.Addon.NetSync.Core.Runtime.Attributes {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NetStateAttribute : Attribute { }
}