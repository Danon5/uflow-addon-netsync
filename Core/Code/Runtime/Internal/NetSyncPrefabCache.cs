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
        private readonly Dictionary<ulong, string> m_localHashToGuidMap = new();
        private readonly Dictionary<string, ushort> m_networkGuidToIdMap = new();
        private readonly Dictionary<ushort, ulong> m_networkIdToHashMap = new();
        private readonly Dictionary<ushort, GameObject> m_networkIdToPrefabMap = new();
        [SerializeField, ListDrawerSettings(IsReadOnly = true)] 
        private List<GameObject> m_prefabs;

        public int NetworkPrefabCount => m_networkIdToPrefabMap.Count;

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
        
        public IEnumerable<(ulong, ushort)> GetNetworkPrefabHashToIdEnumerable() {
            foreach (var (id, hash) in m_networkIdToHashMap)
                yield return (hash, id);
        }

        public void ClearNetworkIdMaps() {
            m_networkGuidToIdMap.Clear();
            m_networkIdToHashMap.Clear();
            m_networkIdToPrefabMap.Clear();
        }

        public void ServerRegisterNetworkIds() {
            ushort nextId = 1;
            foreach (var (hash, _) in m_localHashToPrefabMap)
                RegisterNetworkPrefab(hash, nextId++);
        }

        public bool HasLocalHash(ulong hash) => m_localHashToPrefabMap.ContainsKey(hash);
        
        public void RegisterNetworkPrefab(ulong hash, ushort id) {
            var prefab = m_localHashToPrefabMap[hash];
            m_networkIdToHashMap[id] = hash;
            m_networkIdToPrefabMap[id] = prefab;
            m_networkGuidToIdMap[m_localHashToGuidMap[hash]] = id;
        }

        public GameObject GetPrefabFromNetworkId(ushort id) => m_networkIdToPrefabMap[id];

        public ushort GetNetworkIdFromGuid(in string guid) => m_networkGuidToIdMap[guid];

#if UNITY_EDITOR
        public static void FullRefreshEditorOnly() {
            try {
                AssetDatabase.Refresh();
                var guids = AssetDatabase.FindAssets("t:Prefab");
                List<GameObject> netPrefabs = null;
                foreach (var guid in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!asset.TryGetComponent(out NetSceneEntity _)) continue;
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
                cache.RefreshLocalHashMap();
            }
            catch (Exception e) {
                Debug.LogWarning(e);
            }
        }
#endif

        private void RefreshLocalHashMap() {
            m_localHashToPrefabMap.Clear();
            m_localHashToGuidMap.Clear();
            foreach (var prefab in m_prefabs)
                RegisterLocalPrefab(prefab);
        }
        
        private void RegisterLocalPrefab(GameObject prefab) {
            if (!prefab.TryGetComponent(out NetSceneEntity netSceneEntity)) return;
            var hash = SerializationAPI.CalculateHash(netSceneEntity.Guid);
            m_localHashToPrefabMap.Add(hash, prefab);
            m_localHashToGuidMap.Add(hash, netSceneEntity.Guid);
        }
    }
}