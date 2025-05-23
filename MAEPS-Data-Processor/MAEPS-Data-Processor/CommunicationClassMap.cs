using CsvHelper.Configuration;

using Maes.Statistics.Snapshots;

namespace MAEPS_Data_Processor;

public sealed class CommunicationClassMap : ClassMap<CommunicationSnapshot>
{
    public CommunicationClassMap()
    {
        Map(m => m.Tick);
        Map(m => m.AgentsInterconnected);
        Map(m => m.BiggestClusterPercentage);
        Map(m => m.ReceivedMessageCount);
        Map(m => m.SentMessageCount);
    }
}