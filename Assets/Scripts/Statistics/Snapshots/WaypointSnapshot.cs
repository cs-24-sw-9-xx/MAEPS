#if NET9_0_OR_GREATER
using System.Globalization;
#endif

using System.IO;

using Maes.Statistics.Csv;

namespace Maes.Statistics.Snapshots
{
    public struct WaypointSnapshot : ICsvData
    {
        public int Tick { get; private set; }
        public int Idleness { get; private set; }
        public int NumberOfVisit { get; private set; }

        public WaypointSnapshot(int tick, int idleness, int numberOfVisit)
        {
            Tick = tick;
            Idleness = idleness;
            NumberOfVisit = numberOfVisit;
        }

        public void WriteHeader(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(nameof(Tick));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(Idleness));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(NumberOfVisit));
        }

        public void WriteRow(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(Tick);
            streamWriter.Write(delimiter);
            streamWriter.Write(Idleness);
            streamWriter.Write(delimiter);
            streamWriter.Write(NumberOfVisit);
        }

#if NET9_0_OR_GREATER
        public void ReadRow(ReadOnlySpan<char> columns, ref MemoryExtensions.SpanSplitEnumerator<char> enumerator)
        {
            enumerator.MoveNext();
            Tick = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            Idleness = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
            
            enumerator.MoveNext();
            NumberOfVisit = int.Parse(columns[enumerator.Current], CultureInfo.InvariantCulture);
        }
#endif
    }
}