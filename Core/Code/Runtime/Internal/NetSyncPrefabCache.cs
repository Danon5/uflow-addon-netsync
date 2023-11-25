using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetSyncPrefabCache : ScriptableObject, ISerializationCallbackReceiver {
        private readonly Dictionary<string, GameObject> m_guidToPrefabMap;
        private readonly Dictionary<ushort, GameObject> m_idToPrefabMap;
        [SerializeField, InlineEditor, ListDrawerSettings(IsReadOnly = true)] private List<GameObject> m_prefabs;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() {
            m_guidToPrefabMap.Clear();
            m_idToPrefabMap.Clear();
            foreach (var prefab in m_prefabs) {
                if (!prefab.TryGetComponent(out NetSceneEntity netSceneEntity)) continue;
                m_guidToPrefabMap.Add(netSceneEntity.Guid, prefab);
            }
        }
    }
}