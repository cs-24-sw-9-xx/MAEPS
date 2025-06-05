using System;
using System.Globalization;
using System.IO;

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

        public ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns)
        {
            Tick = Convert.ToInt32(columns[0], CultureInfo.InvariantCulture);
            Idleness = Convert.ToInt32(columns[1], CultureInfo.InvariantCulture);
            NumberOfVisit = Convert.ToInt32(columns[2], CultureInfo.InvariantCulture);
            return columns[3..];
        }
    }
}