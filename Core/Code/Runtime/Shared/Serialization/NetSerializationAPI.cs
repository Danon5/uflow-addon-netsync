using System;
using System.Collections.Generic;
using System.Reflection;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public static class NetSerializationAPI {
        private static readonly Dictionary<Type, MethodInfo> s_serializeRpcCache = new();

        private delegate void SerializeRpcDelegate();
    }
}