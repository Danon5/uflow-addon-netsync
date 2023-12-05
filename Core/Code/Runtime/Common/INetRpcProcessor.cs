using System;
using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public interface INetRpcProcessor {
        Type GetRpcType();
        void EnsureBufferAllocated(World world);
        void EnsureBufferDisposed(World world);
        void ProcessAll(World world);
        void ClearAll(World world);
    }
}