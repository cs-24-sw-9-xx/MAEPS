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
        [Index(4)]
        public int NumberOfRobots { get; }

        public ExplorationSnapShot(int tick, float explored, float covered, float averageAgentDistance, int numberofRobots, bool? agentsInterconnected = null, float? biggestClusterPercentage = null) : base(tick, agentsInterconnected, biggestClusterPercentage)
        {
            Explored = explored;
            Covered = covered;
            AverageAgentDistance = averageAgentDistance;
            NumberOfRobots = numberofRobots;
        }
    }
}