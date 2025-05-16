using CsvHelper.Configuration.Attributes;

namespace Maes.Statistics.Snapshots
{
    public readonly struct WaypointSnapshot
    {
        public int Tick { get; }
        public int Idleness { get; }
        [Name("Number of visit")]
        public int NumberOfVisit { get; }

        public WaypointSnapshot(int tick, int idleness, int numberOfVisit)
        {
            Tick = tick;
            Idleness = idleness;
            NumberOfVisit = numberOfVisit;
        }
    }
}