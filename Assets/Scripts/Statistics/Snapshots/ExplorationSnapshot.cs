using System;
using System.Globalization;
using System.IO;

namespace Maes.Statistics.Snapshots
{
    public struct ExplorationSnapshot : ICsvData
    {
        public CommunicationSnapshot CommunicationSnapshot { get; private set; }

        public float Explored { get; private set; }
        public float Covered { get; private set; }
        public float AverageAgentDistance { get; private set; }
        public int NumberOfRobots { get; private set; }

        public ExplorationSnapshot(CommunicationSnapshot communicationSnapshot, float explored, float covered, float averageAgentDistance, int numberofRobots)
        {
            CommunicationSnapshot = communicationSnapshot;
            Explored = explored;
            Covered = covered;
            AverageAgentDistance = averageAgentDistance;
            NumberOfRobots = numberofRobots;
        }

        public void WriteHeader(StreamWriter streamWriter, char delimiter)
        {
            CommunicationSnapshot.WriteHeader(streamWriter, delimiter);
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(Explored));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(Covered));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(AverageAgentDistance));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(NumberOfRobots));
        }

        public void WriteRow(StreamWriter streamWriter, char delimiter)
        {
            CommunicationSnapshot.WriteRow(streamWriter, delimiter);
            streamWriter.Write(delimiter);
            streamWriter.Write(Explored);
            streamWriter.Write(delimiter);
            streamWriter.Write(Covered);
            streamWriter.Write(delimiter);
            streamWriter.Write(AverageAgentDistance);
            streamWriter.Write(delimiter);
            streamWriter.Write(NumberOfRobots);
        }

        public ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns)
        {
            columns = CommunicationSnapshot.ReadRow(columns);
            Explored = Convert.ToSingle(columns[0], CultureInfo.InvariantCulture);
            Covered = Convert.ToSingle(columns[1], CultureInfo.InvariantCulture);
            AverageAgentDistance = Convert.ToSingle(columns[2], CultureInfo.InvariantCulture);
            NumberOfRobots = Convert.ToInt32(columns[3], CultureInfo.InvariantCulture);

            return columns[4..];
        }
    }
}