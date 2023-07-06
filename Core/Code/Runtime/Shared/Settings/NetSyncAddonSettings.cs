using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [CreateAssetMenu(
        fileName = nameof(NetSyncAddonSettings),
        menuName = MENU_NAME + nameof(NetSyncAddonSettings))]
    public sealed class NetSyncAddonSettings : BaseAddonSettings {
        public override string AddonName => "NetSync";
    }
}