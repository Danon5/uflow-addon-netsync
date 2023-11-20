using System;
using System.Collections.Generic;
using UFlow.Core.Runtime;
using UnityEngine;

namespace UFlow.Addon.NetSync.Core.Runtime {
    internal static class RpcTypeIdMap {
        private static readonly Dictionary<Type, ushort> s_serverTypeToIdMap = new();
        private static readonly Dictionary<ushort, Type> s_serverIdToTypeMap = new();
        private static readonly Dictionary<Type, ushort> s_clientTypeToIdMap = new();
        private static readonly Dictionary<ushort, Type> s_clientIdToTypeMap = new();
        private static ushort s_serverNextId;

        static RpcTypeIdMap() => UnityGlobalEventHelper.RuntimeInitializeOnLoad += ClearStaticCache;

        public static void ServerRegisterAllTypes() {
            foreach (var type in UFlowUtils.Reflection.GetInheritors<INetRpc>(false, UFlowUtils.Reflection.CommonExclusionNamespaces))
                ServerRegisterType(type);
        }

        public static void ServerRegisterType(Type type) {
            var id = s_serverNextId++;
            s_serverTypeToIdMap.Add(type, id);
            s_serverIdToTypeMap.Add(id, type);
            Debug.Log($"Server registering type {type.Name} with id {id}");
        }
        
        public static void ClientRegisterType(Type type, ushort id) {
            s_clientTypeToIdMap.Add(type, id);
            s_clientIdToTypeMap.Add(id, type);
            Debug.Log($"Client registering type {type.Name} with id {id}");
        }
        
        public static ushort ServerGetId(Type type) => s_serverTypeToIdMap[type];

        public static Type ServerGetType(ushort id) => s_serverIdToTypeMap[id];
        
        public static ushort ClientGetId(Type type) => s_clientTypeToIdMap[type];

        public static Type ClientGetType(ushort id) => s_clientIdToTypeMap[id];

        public static ushort GetIdAuto(Type type) => NetSyncAPI.ServerAPI.StartingOrStarted ? ServerGetId(type) : ClientGetId(type);
        
        public static Type GetTypeAuto(ushort id) => NetSyncAPI.ServerAPI.StartingOrStarted ? ServerGetType(id) : ClientGetType(id);

        public static void ClearClientMaps() {
            s_clientTypeToIdMap.Clear();
            s_clientIdToTypeMap.Clear();
        }

        private static void ClearStaticCache() {
            s_serverNextId = 1;
            s_serverTypeToIdMap.Clear();
            s_serverIdToTypeMap.Clear();
            ClearClientMaps();
        }
    }
}