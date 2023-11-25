using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UFlow.Addon.Serialization.Core.Runtime;
using UFlow.Core.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal sealed class NetSyncPrefabCache : BaseResourceCache<NetSyncPrefabCache> {
        private readonly Dictionary<ulong, GameObject> m_localHashToPrefabMap = new();
        private readonly Dictionary<GameObject, ulong> m_localPrefabToHashMap = new();
        private readonly Dictionary<ushort, GameObject> m_networkIdToPrefabMap = new();
        private readonly Dictionary<GameObject, ushort> m_networkPrefabToIdMap = new();
        [SerializeField, ListDrawerSettings(IsReadOnly = true)] 
        private List<GameObject> m_prefabs;

        public int LocalPrefabCount => m_localHashToPrefabMap.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad() {
            var cache = Get();
            if (cache == null) return;
            cache.ClearNetworkIdMaps();
#if UNITY_EDITOR
            FullRefreshEditorOnly();
#endif
        }

        private void OnEnable() {
#if UNITY_EDITOR
            EditorApplication.delayCall += FullRefreshEditorOnly;
#else
            RefreshGuidMap();
#endif
        }
        
        public void RefreshLocalHashMaps() {
            m_localHashToPrefabMap.Clear();
            m_localPrefabToHashMap.Clear();
            foreach (var prefab in m_prefabs)
                RegisterLocalPrefab(prefab);
        }

        public IEnumerable<(ulong, ushort)> GetNetworkPrefabHashToIdEnumerable() {
            foreach (var (id, prefab) in m_networkIdToPrefabMap)
                yield return (m_localPrefabToHashMap[prefab], id);
        }

        public void ClearNetworkIdMaps() {
            m_networkIdToPrefabMap.Clear();
            m_networkPrefabToIdMap.Clear();
        }

        public void ServerRegisterNetworkIds() {
            ushort nextId = 1;
            foreach (var (hash, prefab) in m_localHashToPrefabMap)
                RegisterNetworkPrefab(hash, nextId++);
        }

        public void RegisterNetworkPrefab(ulong hash, ushort id) {
            var prefab = m_localHashToPrefabMap[hash];
            m_networkIdToPrefabMap[id] = prefab;
            m_networkPrefabToIdMap[prefab] = id;
        }

        public GameObject GetPrefabFromNetworkId(ushort id) => m_networkIdToPrefabMap[id];

        public ushort GetNetworkIdFromPrefab(GameObject prefab) => m_networkPrefabToIdMap[prefab];
        
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
                cache.RefreshLocalHashMaps();
            }
            catch (Exception e){
                Debug.LogWarning(e);
            }
        }
#endif

        private void RegisterLocalPrefab(GameObject prefab) {
            if (!prefab.TryGetComponent(out NetSceneEntity netSceneEntity)) return;
            var hash = SerializationAPI.CalculateHash(netSceneEntity.Guid);
            m_localHashToPrefabMap.Add(hash, prefab);
            m_localPrefabToHashMap.Add(prefab, hash);
        }
    }
}