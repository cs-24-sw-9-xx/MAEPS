using CsvHelper.Configuration.Attributes;

using Maes.Statistics.Communication;

namespace Maes.Statistics.Patrolling
{
    public class PatrollingSnapShot : CommunicationSnapShot
    {
        [Index(1)]
        public float GraphIdleness { get; }
        [Index(2)]
        public int WorstGraphIdleness { get; }
        [Index(3)]
        public float TotalDistanceTraveled { get; }
        [Index(4)]
        public int CompletedCycles { get; }

        public PatrollingSnapShot(int tick, float graphIdleness, int worstGraphIdleness, float totalDistanceTraveled,
            int completedCycles, bool? agentsInterconnected = null, float? biggestClusterPercentage = null) : base(tick,
            agentsInterconnected, biggestClusterPercentage)
        {
            GraphIdleness = graphIdleness;
            WorstGraphIdleness = worstGraphIdleness;
            TotalDistanceTraveled = totalDistanceTraveled;
            CompletedCycles = completedCycles;
        }
    }
}