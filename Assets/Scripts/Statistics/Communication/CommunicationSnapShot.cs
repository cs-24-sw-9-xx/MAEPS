using CsvHelper.Configuration.Attributes;

using Maes.Statistics.Writer.TypeConverter;

namespace Maes.Statistics.Communication
{
    public abstract class CommunicationSnapShot
    {
        [Index(0)]
        public int Tick { get; }

        [TypeConverter(typeof(BooleanNullableToBinaryConverter))]
        public bool? AgentsInterconnected { get; set; }
        [TypeConverter(typeof(NullableFloatToStringConverter))]
        public float? BiggestClusterPercentage { get; set; }

        protected CommunicationSnapShot(int tick, bool? agentsInterconnected = null, float? biggestClusterPercentage = null)
        {
            Tick = tick;
            AgentsInterconnected = agentsInterconnected;
            BiggestClusterPercentage = biggestClusterPercentage;
        }
    }
}