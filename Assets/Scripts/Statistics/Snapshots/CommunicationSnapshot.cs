using CsvHelper.Configuration.Attributes;

namespace Maes.Statistics.Snapshots
{
    public
#if UNITY
        readonly struct
#else
        sealed class
#endif 
        CommunicationSnapshot
    {
        [Index(0)]
        public int Tick { get; }

        public bool AgentsInterconnected { get; }

        public float BiggestClusterPercentage { get; }

        public int ReceivedMessageCount { get; }

        public int SentMessageCount { get; }

        public CommunicationSnapshot(int tick, bool agentsInterconnected, float biggestClusterPercentage, int receivedMessageCount, int sentMessageCount)
        {
            Tick = tick;
            AgentsInterconnected = agentsInterconnected;
            BiggestClusterPercentage = biggestClusterPercentage;
            ReceivedMessageCount = receivedMessageCount;
            SentMessageCount = sentMessageCount;
        }
    }
}