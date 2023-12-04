using UFlow.Addon.ECS.Core.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace UFlow.Addon.NetSync.Core.Runtime {
    [Preserve]
    [ExecuteInWorld(typeof(NetWorld))]
    [ExecuteInGroup(typeof(GUISystemGroup))]
    public sealed class NetStatisticsSystem : BaseRunSystem {
        private float m_lastUpdateTime;
        private NetStatisticData m_serverStatistics;
        private NetStatisticData m_clientStatistics;

        public NetStatisticsSystem(in World world) : base(in world) { }

        protected override void Run(World world) {
            NetSyncModule.InternalSingleton.Transport.ServerSetStatisticsEnabled(NetSyncModule.InternalSingleton.EnableStatistics);
            NetSyncModule.InternalSingleton.Transport.ClientSetStatisticsEnabled(NetSyncModule.InternalSingleton.EnableStatistics);
            if (!NetSyncModule.InternalSingleton.EnableStatistics) return;
            var yPos = 50f;
            const float offset = 125f;
            if (NetSyncAPI.IsServer) {
                GUI.color = Color.cyan;
                GUI.Label(GetNextRect(ref yPos, offset), $"- Server Statistics - \n{GetStatisticsString(m_serverStatistics)}");
            }
            if (NetSyncAPI.IsClient) {
                GUI.color = Color.green;
                GUI.Label(GetNextRect(ref yPos, offset), $"- Client Statistics - \n{GetStatisticsString(m_clientStatistics)}");
            }
            if (Time.time - m_lastUpdateTime < 1f) return;
            m_lastUpdateTime = Time.time;
            if (NetSyncAPI.IsServer) {
                m_serverStatistics = NetSyncModule.InternalSingleton.Transport.ServerGetStatistics();
                NetSyncModule.InternalSingleton.Transport.ServerResetStatistics();
            }
            if (NetSyncAPI.IsClient) {
                m_clientStatistics = NetSyncModule.InternalSingleton.Transport.ClientGetStatistics();
                NetSyncModule.InternalSingleton.Transport.ClientResetStatistics();
            }
        }

        private static Rect GetNextRect(ref float yPos, float offset) {
            var rect = new Rect(50f, yPos, 200f, 200f);
            yPos += offset;
            return rect;
        }

        private static string GetStatisticsString(in NetStatisticData statistics) =>
            $"Bytes Incoming: {ParseBytes(statistics.bytesIncoming)}/s\n" +
            $"Bytes Outgoing: {ParseBytes(statistics.bytesOutgoing)}/s\n" +
            $"Packets Incoming: {statistics.packetsIncoming}\n" +
            $"Packets Outgoing: {statistics.packetsOutgoing}\n" +
            $"Packets Lost: {statistics.packetsLost}\n" +
            $"Packet Loss: {statistics.packetLossPercent}%";

        private static string ParseBytes(int bytes) {
            return bytes switch {
                > 1000000 => $"{bytes / 1000000} MB",
                > 1000 => $"{bytes / 1000} KB",
                _ => $"{bytes} B"
            };
        }
    }
}