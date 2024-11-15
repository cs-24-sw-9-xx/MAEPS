using CsvHelper.Configuration.Attributes;

namespace Maes.Statistics.Patrolling
{
    public class WaypointSnapShot
    {
        public int Tick { get; }
        public int Idleness { get; }
        [Name("Number of visit")]
        public int NumberOfVisit { get; }

        public WaypointSnapShot(int tick, int idleness, int numberOfVisit)
        {
            Tick = tick;
            Idleness = idleness;
            NumberOfVisit = numberOfVisit;
        }
    }
}