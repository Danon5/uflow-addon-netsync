using UFlow.Addon.ECS.Core.Runtime;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public sealed class NetworkSceneEntity : SceneEntity {
        public override World GetWorld() => EcsModule<NetworkWorld>.Get().World;
    }
}