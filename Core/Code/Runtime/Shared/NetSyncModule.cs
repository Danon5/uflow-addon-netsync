using Cysharp.Threading.Tasks;
using UFlow.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetSyncModule : BaseAsyncBehaviourModule<NetSyncModule> {
        
        
        public NetSyncModule() => Transport = new LiteNetLibTransport();
        internal static NetSyncModule InternalSingleton { get; private set; }
        internal LiteNetLibTransport Transport { get; }
        
        public override UniTask LoadDirectAsync() {
            InternalSingleton = this;
            return base.LoadDirectAsync();
        }

        public override async UniTask UnloadDirectAsync() {
            if (Transport.HostStartingOrStarted)
                await Transport.StopHostAsync();
            else if (Transport.ServerStartingOrStarted)
                await Transport.StopServerAsync();
            else if (Transport.ClientStartingOrStarted)
                await Transport.StopClientAsync();
            Transport.ForceStop();
            InternalSingleton = null;
        }
        
        

        public override void FinalUpdate() {
            Transport.PollEvents();
        }
        
        
    }
}