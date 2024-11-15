using CsvHelper.Configuration.Attributes;

using Maes.Statistics.Writer.TypeConverter;

namespace Maes.Statistics.Communication
{
    public class CommunicationSnapShot
    {
        [Index(0)]
        public int Tick { get; }

        [Name("Agents Interconnected"), TypeConverter(typeof(BooleanNullableToBinaryConverter))]
        public bool? AgentsInterconnected { get; set; }
        [Name("Biggest Cluster %"), TypeConverter(typeof(NullableFloatToStringConverter))]
        public float? BiggestClusterPercentage { get; set; }

        public CommunicationSnapShot(int tick, bool? agentsInterconnected = null, float? biggestClusterPercentage = null)
        {
            Tick = tick;
            AgentsInterconnected = agentsInterconnected;
            BiggestClusterPercentage = biggestClusterPercentage;
        }
    }
}