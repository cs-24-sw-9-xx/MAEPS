#if NET9_0_OR_GREATER
using System.Globalization;
#endif

using System.IO;

using Maes.Statistics.Csv;

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

#if NET9_0_OR_GREATER
        public void ReadRow(ReadOnlySpan<char> columns, ref MemoryExtensions.SpanSplitEnumerator<char> enumerator)
        {
            CommunicationSnapshot.ReadRow(columns, ref enumerator);

            enumerator.MoveNext();
            Explored = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            Covered = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            AverageAgentDistance = float.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            NumberOfRobots = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
        }
#endif
    }
}