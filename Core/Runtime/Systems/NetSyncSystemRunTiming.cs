namespace UFlow.Addon.NetSync.Runtime {
    public enum NetSyncSystemRunTiming : byte {
        Update,
        PreTick,
        Tick,
        PostTick,
        LateUpdate,
        OnDrawGizmos,
        OnDrawGUI
    }
}