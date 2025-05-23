using System.Globalization;

using CsvHelper.Configuration;

using Maes.Statistics.Snapshots;

namespace MAEPS_Data_Processor;

public sealed class PatrollingClassMap : ClassMap<PatrollingSnapshot>
{
    public PatrollingClassMap()
    {
        //References<CommunicationClassMap>(p => p.CommunicationSnapshot);
        Map(p => p.AverageGraphIdleness);
        Map(p => p.WorstGraphIdleness);
        Map(p => p.CompletedCycles);
        Map(p => p.GraphIdleness);
        Map(p => p.TotalDistanceTraveled);
        Map(p => p.NumberOfRobots);
    }
}