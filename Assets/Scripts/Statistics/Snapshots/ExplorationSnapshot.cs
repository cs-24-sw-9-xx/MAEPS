using CsvHelper.Configuration.Attributes;

namespace Maes.Statistics.Snapshots
{
    public readonly struct ExplorationSnapshot
    {
        public CommunicationSnapshot CommunicationSnapshot { get; }

        [Index(1)]
        public float Explored { get; }
        [Index(2)]
        public float Covered { get; }
        [Name("Average Agent Distance"), Index(3)]
        public float AverageAgentDistance { get; }
        [Index(4)]
        public int NumberOfRobots { get; }

        public ExplorationSnapshot(CommunicationSnapshot communicationSnapshot, float explored, float covered, float averageAgentDistance, int numberofRobots)
        {
            CommunicationSnapshot = communicationSnapshot;
            Explored = explored;
            Covered = covered;
            AverageAgentDistance = averageAgentDistance;
            NumberOfRobots = numberofRobots;
        }
    }
}