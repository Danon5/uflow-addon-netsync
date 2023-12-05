using LiteNetLib;

namespace UFlow.Addon.NetSync.Core.Runtime {
    public readonly struct NetStatisticData {
        public readonly int bytesIncoming;
        public readonly int bytesOutgoing;
        public readonly ushort packetsIncoming;
        public readonly ushort packetsOutgoing;
        public readonly ushort packetsLost;
        public readonly byte packetLossPercent;

        public NetStatisticData(NetStatistics statistics) {
            bytesIncoming = (int)statistics.BytesReceived;
            bytesOutgoing = (int)statistics.BytesSent;
            packetsIncoming = (ushort)statistics.PacketsSent;
            packetsOutgoing = (ushort)statistics.PacketsReceived;
            packetsLost = (ushort)statistics.PacketLoss;
            packetLossPercent = (byte)statistics.PacketLossPercent;
        }
    }
}