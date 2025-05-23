using CsvHelper.Configuration.Attributes;

namespace Maes.Statistics.Snapshots
{
    public sealed class PatrollingSnapshot
    {
        public CommunicationSnapshot CommunicationSnapshot { get; }

        [Index(1)]
        public float GraphIdleness { get; }
        [Index(2)]
        public int WorstGraphIdleness { get; }
        [Index(3)]
        public float TotalDistanceTraveled { get; }
        [Index(4)]
        public int CompletedCycles { get; }
        [Index(5)]
        public float AverageGraphIdleness { get; }
        [Index(6)]
        public int NumberOfRobots { get; }

        public PatrollingSnapshot(CommunicationSnapshot communicationSnapshot,
            float graphIdleness, int worstGraphIdleness, float totalDistanceTraveled,
            float averageGraphIdleness, int completedCycles, int numberOfRobots)
        {
            CommunicationSnapshot = communicationSnapshot;
            GraphIdleness = graphIdleness;
            WorstGraphIdleness = worstGraphIdleness;
            TotalDistanceTraveled = totalDistanceTraveled;
            CompletedCycles = completedCycles;
            AverageGraphIdleness = averageGraphIdleness;
            NumberOfRobots = numberOfRobots;
        }
    }
}