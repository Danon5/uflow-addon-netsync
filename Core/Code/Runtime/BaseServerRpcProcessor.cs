using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public abstract class BaseServerRpcProcessor<T> : BaseNetRpcProcessor<T> where T : INetRpc {
        public sealed override void ProcessAll(World world) {
            foreach (var bufferElement in NetRpcBuffers<T>.GetBufferElementsEnumerable(world.id))
                Process(bufferElement.rpc, bufferElement.client);
        }

        protected abstract void Process(in T rpc, NetworkClient client);
    }
}