using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

using Maes.Statistics.Writer.TypeConverter;

namespace Maes.Statistics.Exploration
{
    public class ExplorationSnapShot
    {
        public int Tick { get; }
        public float Explored { get; }
        public float Covered { get; }
        [Name("Average Agent Distance")]
        public float AverageAgentDistance { get; }
        [Name("Agents Interconnected"), TypeConverter(typeof(BooleanNullableToBinaryConverter))]
        public bool? AgentsInterconnected { get; set; }
        [Name("Biggest Cluster %"), TypeConverter(typeof(NullableFloatToStringConverter))]
        public float? BiggestClusterPercentage { get; set; }

        public ExplorationSnapShot(int tick, float explored, float covered, float averageAgentDistance, bool? agentsInterconnected = null, float? biggestClusterPercentage = null)
        {
            Tick = tick;
            Explored = explored;
            Covered = covered;
            AverageAgentDistance = averageAgentDistance;
            AgentsInterconnected = agentsInterconnected;
            BiggestClusterPercentage = biggestClusterPercentage;
        }
    }

    public sealed class ExplorationSnapShotMap : ClassMap<ExplorationSnapShot>
    {
        public ExplorationSnapShotMap()
        {
            Map(e => e.Tick).Index(0).Name("Tick");
            Map(e => e.Explored).Index(1).Name("Explored");
            Map(e => e.Covered).Index(2).Name("Covered");
            Map(e => e.AverageAgentDistance).Index(3).Name("Average Agent Distance");

            Map(e => e.AgentsInterconnected)
                .Index(4)
                .TypeConverter<BooleanNullableToBinaryConverter>()
                .Name("Agents Interconnected");

            Map(e => e.BiggestClusterPercentage)
                .Index(5)
                .TypeConverter<NullableFloatToStringConverter>()
                .Name("Biggest Cluster %");
        }
    }
}