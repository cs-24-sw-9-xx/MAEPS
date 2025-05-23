using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Maes.Statistics.Snapshots;

namespace MAEPS_Data_Processor;



public static class CsvDataReader
{
    public static List<PatrollingSnapshot> ReadPatrollingCsv(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            HasHeaderRecord = true,
            HeaderValidated = null // Disable header validation

        };
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<PatrollingSnapshot>().ToList();
    }
}