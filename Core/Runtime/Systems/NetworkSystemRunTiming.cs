namespace UFlow.Addon.Networking.Runtime {
    public enum NetworkSystemRunTiming : byte {
        Update,
        PreTick,
        Tick,
        PostTick,
        LateUpdate,
        OnDrawGizmos,
        OnDrawGUI
    }
}