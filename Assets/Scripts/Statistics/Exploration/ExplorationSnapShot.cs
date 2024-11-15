using CsvHelper.Configuration.Attributes;

using Maes.Statistics.Communication;

namespace Maes.Statistics.Exploration
{
    public class ExplorationSnapShot : CommunicationSnapShot
    {
        [Index(1)]
        public float Explored { get; }
        [Index(2)]
        public float Covered { get; }
        [Name("Average Agent Distance"), Index(3)]
        public float AverageAgentDistance { get; }

        public ExplorationSnapShot(int tick, float explored, float covered, float averageAgentDistance, bool? agentsInterconnected = null, float? biggestClusterPercentage = null) : base(tick, agentsInterconnected, biggestClusterPercentage)
        {
            Explored = explored;
            Covered = covered;
            AverageAgentDistance = averageAgentDistance;
        }
    }
}