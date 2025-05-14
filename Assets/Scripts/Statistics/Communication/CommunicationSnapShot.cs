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

        [TypeConverter(typeof(NullableIntToStringConverter))]
        public int? ReceivedMessageCount { get; set; }

        [TypeConverter(typeof(NullableIntToStringConverter))]
        public int? SentMessageCount { get; set; }

        protected CommunicationSnapShot(int tick, bool? agentsInterconnected = null, float? biggestClusterPercentage = null, int? receivedMessageCount = null, int? sentMessageCount = null)
        {
            Tick = tick;
            AgentsInterconnected = agentsInterconnected;
            BiggestClusterPercentage = biggestClusterPercentage;
            ReceivedMessageCount = receivedMessageCount;
            SentMessageCount = sentMessageCount;
        }
    }
}