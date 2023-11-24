using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public abstract class BaseClientNetRpcProcessor<T> : INetRpcProcessor where T : INetRpc {
        public void ProcessAll() {
            Debug.Log("Processing");
        }

        protected abstract void Process(in T rpc);
    }
}