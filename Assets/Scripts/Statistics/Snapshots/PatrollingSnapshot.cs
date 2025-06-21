#if NET9_0_OR_GREATER
using System.Globalization;
#endif

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

#if NET9_0_OR_GREATER
        public void ReadRow(ReadOnlySpan<char> columns, ref MemoryExtensions.SpanSplitEnumerator<char> enumerator)
        {
            CommunicationSnapshot.ReadRow(columns, ref enumerator);

            enumerator.MoveNext();
            GraphIdleness = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            WorstGraphIdleness = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            TotalDistanceTraveled = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);

            enumerator.MoveNext();
            CompletedCycles = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            AverageGraphIdleness = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            NumberOfRobots = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
        }
#endif
    }
}