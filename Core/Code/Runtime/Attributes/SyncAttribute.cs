using System;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SyncAttribute : Attribute { }
}