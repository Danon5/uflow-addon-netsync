using System;
using UFlow.Addon.ECS.Core.Runtime;

// ReSharper disable StaticMemberInGenericType

namespace UFlow.Addon.NetSync.Core.Runtime {
    public abstract class BaseNetRpcProcessor<T> : INetRpcProcessor where T : INetRpc {
        public Type GetRpcType() => typeof(T);

        public void EnsureBufferAllocated(World world) {
            if (NetRpcBuffers<T>.HasBuffer(world.id)) return;
            NetRpcBuffers<T>.AllocateBuffer(world.id);
        }
        
        public void EnsureBufferDisposed(World world) {
            if (!NetRpcBuffers<T>.HasBuffer(world.id)) return;
            NetRpcBuffers<T>.DisposeBuffer(world.id);
        }

        public abstract void ProcessAll(World world);
        
        public void ClearAll(World world) => NetRpcBuffers<T>.ClearBuffer(world.id);
    }
}