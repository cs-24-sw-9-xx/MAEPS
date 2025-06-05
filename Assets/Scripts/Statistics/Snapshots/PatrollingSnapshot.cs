using System;
using System.Globalization;
using System.IO;

using Maes.Statistics.Csv;

namespace Maes.Statistics.Snapshots
{
    public struct PatrollingSnapshot : ICsvData
    {
        public CommunicationSnapshot CommunicationSnapshot;

        public float GraphIdleness { get; private set; }
        public int WorstGraphIdleness { get; private set; }
        public float TotalDistanceTraveled { get; private set; }
        public int CompletedCycles { get; private set; }
        public float AverageGraphIdleness { get; private set; }
        public int NumberOfRobots { get; private set; }

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

        public void WriteHeader(StreamWriter streamWriter, char delimiter)
        {
            CommunicationSnapshot.WriteHeader(streamWriter, delimiter);
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(GraphIdleness));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(WorstGraphIdleness));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(TotalDistanceTraveled));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(CompletedCycles));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(AverageGraphIdleness));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(NumberOfRobots));
        }

        public void WriteRow(StreamWriter streamWriter, char delimiter)
        {
            CommunicationSnapshot.WriteRow(streamWriter, delimiter);
            streamWriter.Write(delimiter);
            streamWriter.Write(GraphIdleness);
            streamWriter.Write(delimiter);
            streamWriter.Write(WorstGraphIdleness);
            streamWriter.Write(delimiter);
            streamWriter.Write(TotalDistanceTraveled);
            streamWriter.Write(delimiter);
            streamWriter.Write(CompletedCycles);
            streamWriter.Write(delimiter);
            streamWriter.Write(AverageGraphIdleness);
            streamWriter.Write(delimiter);
            streamWriter.Write(NumberOfRobots);
        }

        public ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns)
        {
            columns = CommunicationSnapshot.ReadRow(columns);
            GraphIdleness = Convert.ToSingle(columns[0], CultureInfo.InvariantCulture);
            WorstGraphIdleness = Convert.ToInt32(columns[1], CultureInfo.InvariantCulture);
            TotalDistanceTraveled = Convert.ToSingle(columns[2], CultureInfo.InvariantCulture);
            CompletedCycles = Convert.ToInt32(columns[3], CultureInfo.InvariantCulture);
            AverageGraphIdleness = Convert.ToSingle(columns[4], CultureInfo.InvariantCulture);
            NumberOfRobots = Convert.ToInt32(columns[5], CultureInfo.InvariantCulture);
            return columns[6..];
        }
    }
}
