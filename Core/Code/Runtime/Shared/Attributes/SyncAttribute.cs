using System;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SyncAttribute : Attribute { }
}