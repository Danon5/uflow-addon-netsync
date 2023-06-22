using System;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;

namespace UFlow.Addon.NetSync.Runtime {
    public sealed class NetSyncTransport : BaseTransport {
        private readonly EventBasedNetListener m_serverListener;
        private readonly NetPacketProcessor m_serverPacketProcessor;
        private readonly NetManager m_server;
        private readonly EventBasedNetListener m_clientListener;
        private readonly NetPacketProcessor m_clientPacketProcessor;
        private readonly NetManager m_client;

        public NetSyncTransport() {
            m_serverListener = new EventBasedNetListener();
            m_serverPacketProcessor = new NetPacketProcessor();
            m_server = new NetManager(m_serverListener);
            m_clientListener = new EventBasedNetListener();
            m_clientPacketProcessor = new NetPacketProcessor();
            m_client = new NetManager(m_clientListener);
        }
        
        protected override UniTask<bool> SetupServer(ushort port) {
            return default;
        }

        protected override UniTask CleanupServer() {
            return default;
        }

        protected override UniTask<bool> SetupClient(string ip, ushort port) {
            return default;
        }

        protected override UniTask CleanupClient() {
            return default;
        }

        public override void ClientRegisterHandler<T>(Action<T> handler) {
            m_clientPacketProcessor.SubscribeNetSerializable(handler, Activator.CreateInstance<T>);
        }

        public override void ClientUnRegisterHandler<T>() {
            m_clientPacketProcessor.RemoveSubscription<T>();
        }

        public override void ServerRegisterHandler<T>(Action<T, NetPeer> handler) {
            m_serverPacketProcessor.SubscribeNetSerializable(handler, Activator.CreateInstance<T>);
        }

        public override void ServerUnRegisterHandler<T>() {
            m_serverPacketProcessor.RemoveSubscription<T>();
        }
    }
}