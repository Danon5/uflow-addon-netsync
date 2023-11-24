using System;
using System.Collections.Generic;
using UFlow.Core.Runtime;

// ReSharper disable StaticMemberInGenericType

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class NetRpcBuffers<T> where T : INetRpc {
        private static List<BufferElement>[] s_buffers = Array.Empty<List<BufferElement>>();
        private static bool s_initialized;

        static NetRpcBuffers() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        public static void AllocateBuffer(short worldId) {
            if (!s_initialized)
                Initialize();
            UFlowUtils.Collections.EnsureIndex(ref s_buffers, worldId);
            s_buffers[worldId] = new List<BufferElement>();
        }

        public static bool HasBuffer(short worldId) {
            if (worldId < 0 || worldId >= s_buffers.Length) return false;
            return s_buffers[worldId] != null;
        }

        public static IEnumerable<BufferElement> GetBufferElementsEnumerable(short worldId) => s_buffers[worldId];

        public static void ClearBuffer(short worldId) => s_buffers[worldId].Clear();

        public static void DisposeBuffer(short worldId) => s_buffers[worldId] = null;

        private static void AppendToBuffers(in T rpc, NetworkClient client = null) {
            foreach (var buffer in s_buffers)
                buffer?.Add(new BufferElement(rpc, client));
        }
        
        private static void ClientOnRpcDeserialized(in T rpc) => AppendToBuffers(rpc);

        private static void ServerOnRpcDeserialized(in T rpc, in NetworkClient client) => AppendToBuffers(rpc, client);

        private static void Initialize() {
            NetDeserializer.RpcDeserializer<T>.ClientRpcDeserializedEvent += ClientOnRpcDeserialized;
            NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent += ServerOnRpcDeserialized;
            s_initialized = true;
        }
        
        private static void ClearStaticCache() {
            s_buffers = Array.Empty<List<BufferElement>>();
            s_initialized = false;
            NetDeserializer.RpcDeserializer<T>.ClientRpcDeserializedEvent -= ClientOnRpcDeserialized;
            NetDeserializer.RpcDeserializer<T>.ServerRpcDeserializedEvent -= ServerOnRpcDeserialized;
        }

        public readonly struct BufferElement {
            public readonly T rpc;
            public readonly NetworkClient client;

            public BufferElement(in T rpc, NetworkClient client) {
                this.rpc = rpc;
                this.client = client;
            }
        }
    }
}