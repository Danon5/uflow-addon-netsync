using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UFlow.Core.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetSyncPrefabCache : BaseResourceCache<NetSyncPrefabCache> {
        private readonly Dictionary<string, GameObject> m_guidToPrefabMap = new();
        private readonly Dictionary<ushort, GameObject> m_idToPrefabMap = new();
        [SerializeField, ListDrawerSettings(IsReadOnly = true)] 
        private List<GameObject> m_prefabs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad() {
            var cache = Get();
            if (cache == null) return;
            cache.m_idToPrefabMap.Clear();
#if UNITY_EDITOR
            FullRefreshEditorOnly();
#endif
        }

        public void RefreshGuidMap() {
            m_guidToPrefabMap.Clear();
            foreach (var prefab in m_prefabs) {
                if (!prefab.TryGetComponent(out NetSceneEntity netSceneEntity)) continue;
                m_guidToPrefabMap.Add(netSceneEntity.Guid, prefab);
            }
        }

#if UNITY_EDITOR
        public static void FullRefreshEditorOnly() {
            try {
                AssetDatabase.Refresh();
                var guids = AssetDatabase.FindAssets("t:Prefab");
                List<GameObject> netPrefabs = null;
                foreach (var guid in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!asset.TryGetComponent(out NetSceneEntity netSceneEntity)) continue;
                    netPrefabs ??= new List<GameObject>();
                    netPrefabs.Add(asset);
                }
                if (netPrefabs == null || netPrefabs.Count == 0) {
                    var existingCache = Get();
                    if (existingCache != null)
                        DestroyEditorOnly();
                    return;
                }
                var cache = GetOrCreateEditorOnly();
                cache.m_prefabs ??= new List<GameObject>();
                cache.m_prefabs.Clear();
                cache.m_prefabs.AddRange(netPrefabs);
                cache.RefreshGuidMap();
            }
            catch (Exception e){
                Debug.LogWarning(e);
            }
        }
#endif

        private void OnEnable() {
#if UNITY_EDITOR
            EditorApplication.delayCall += FullRefreshEditorOnly;
#else
            RefreshGuidMap();
#endif
        }
    }
}