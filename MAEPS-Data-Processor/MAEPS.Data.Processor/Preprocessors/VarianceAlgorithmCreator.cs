using System.Globalization;

using Maes.Statistics.Snapshots;
using MathNet.Numerics.Statistics;


namespace MAEPS.Data.Processor.Preprocessors;

public static class VarianceAlgorithmCreator
{
    private const string VarianceFileName = "Variance.csv";

    /// <summary>
    /// </summary>
    /// <param name="scenariosFolderPath">The path to the folder that contains the scenarios as folders</param>
    /// <param name="regenerate">If true, it regenerate the summary file </param>
    public static void CreateVarianceFromScenarios(string scenariosFolderPath, bool regenerate)
    {
        if (SummaryExists(scenariosFolderPath) && !regenerate)
        {
            Console.WriteLine("Variance already exists. Skipping for {0}", scenariosFolderPath);
            return;
        }
        
        Console.WriteLine("Create summary for {0}", scenariosFolderPath);
        var snapshots = new List<PatrollingSnapshot>();
        
        foreach (var scenarioFolderPath in Directory.GetDirectories(scenariosFolderPath))
        {
            var patrollingCsvPath = Path.Combine(scenarioFolderPath, "patrolling.csv");
            if (!File.Exists(patrollingCsvPath))
            {
                Console.WriteLine("No patrolling.csv found in {0}. Skipping.", scenarioFolderPath);
                continue;
            }
                
            var data = CsvDataReader.ReadPatrollingCsv(patrollingCsvPath);
            snapshots.AddRange(data);
        }
        GenerateVariance(scenariosFolderPath, snapshots);
        Console.WriteLine("Summary created for {0}", scenariosFolderPath);
    }

    private static void GenerateVariance(string scenariosFolderPath, List<PatrollingSnapshot> snapshots)
    {
        var datas = snapshots.GroupBy(snapshot => snapshot.CommunicationSnapshot.Tick)
            .AsParallel()
            .Select(group =>
            {
                var tick = group.Key;
                var worstGraphIdleness = group.Average(s => s.WorstGraphIdleness);
                var worstGraphIdlenessStd = group.Select(s => (double)s.WorstGraphIdleness).StandardDeviation();
                var averageGraphIdleness = group.Average(s => s.GraphIdleness);
                var averageGraphIdlenessStd = group.Select(s => (double)s.GraphIdleness).StandardDeviation();

                return new VarianceData(tick, worstGraphIdleness, worstGraphIdlenessStd,
                    averageGraphIdleness, averageGraphIdlenessStd);
            }).OrderBy(v => v.Tick);
        
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
            
        using var writer = new StreamWriter(GetSummaryPath(scenariosFolderPath));
        using var csv = new CsvHelper.CsvWriter(writer, config);
    
        csv.WriteRecords(datas);
    }
    


    private static string GetSummaryPath(string folderPath)
    {
        return Path.Combine(folderPath, VarianceFileName);
    }

    private static bool SummaryExists(string folderPath)
    {
        return File.Exists(GetSummaryPath(folderPath));
    }


    private record VarianceData
    (
        int Tick,
        double WorstGraphIdleness,
        double WorstGraphIdlenessStd,
        float AverageGraphIdleness,
        double AverageGraphIdlenessStd
    );
}

public static class NumberOfMeetingsAlgorithmCreator
{
    private const string NumberOfRecievedNewFromToMyMeetingHMPFileName = "NumberOfRecievedNewFromToMyMeetingHMP.csv";

    /// <summary>
    /// </summary>
    /// <param name="scenariosFolderPath">The path to the folder that contains the scenarios as folders</param>
    /// <param name="regenerate">If true, it regenerate the summary file </param>
    public static void CreateNumberOfRecievedNewFromToMyMeetingHMPFromScenarios(string scenariosFolderPath, bool regenerate)
    {
        if (SummaryExists(scenariosFolderPath) && !regenerate)
        {
            Console.WriteLine("NumberOfRecievedNewFromToMyMeetingHMP already exists. Skipping for {0}", scenariosFolderPath);
            return;
        }
        
        Console.WriteLine("Create NumberOfRecievedNewFromToMyMeetingHMP for {0}", scenariosFolderPath);
        var snapshots = new List<DataProcessed>();
        
        foreach (var scenarioFolderPath in Directory.GetDirectories(scenariosFolderPath))
        {
            var patrollingCsvPath = Path.Combine(scenarioFolderPath, "ReceivedNewMeetingtimeAtTicks.csv");
            if (!File.Exists(patrollingCsvPath))
            {
                Console.WriteLine("No ReceivedNewMeetingtimeAtTicks.csv found in {0}. Skipping.", scenarioFolderPath);
                continue;
            }
            
            // Read the CSV file using CsvHelper
            using var reader = new StreamReader(patrollingCsvPath);
            using var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture);
            var data = csv.GetRecords<DataRaw>().ToList();
            
            snapshots.AddRange(data.GroupBy(s => s.Tick)
                .Select(group => new DataProcessed
                {
                    Tick = group.Key,
                    receivedNewFromToMyMeetingHmp = group.Count()
                }));
        }
        GenerateVariance(scenariosFolderPath, snapshots);
        Console.WriteLine("Summary created for {0}", scenariosFolderPath);
    }

    private static void GenerateVariance(string scenariosFolderPath, List<DataProcessed> snapshots)
    {
        if (snapshots.Count == 0)
        {
            Console.WriteLine("No data found for {0}. Skipping.", scenariosFolderPath);
            return;
        }
        
        var datas = snapshots.GroupBy(snapshot => snapshot.Tick)
            .Select(group =>
            {
                var tick = group.Key;
                var receivedNewFromToMyMeetingHmp = group.Average(s => s.receivedNewFromToMyMeetingHmp);
                var receivedNewFromToMyMeetingHmpStd = group.Select(s => (double)s.receivedNewFromToMyMeetingHmp).StandardDeviation();
                receivedNewFromToMyMeetingHmpStd = double.IsNaN(receivedNewFromToMyMeetingHmpStd) ? 0 : receivedNewFromToMyMeetingHmpStd;

                return new NumberOfRecievedNewFromToMyMeetingHMPData(tick, receivedNewFromToMyMeetingHmp, receivedNewFromToMyMeetingHmpStd);
            }).OrderBy(v => v.Tick);
        
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
            
        using var writer = new StreamWriter(GetSummaryPath(scenariosFolderPath));
        using var csv = new CsvHelper.CsvWriter(writer, config);
    
        csv.WriteRecords(datas);
    }
    


    private static string GetSummaryPath(string folderPath)
    {
        return Path.Combine(folderPath, NumberOfRecievedNewFromToMyMeetingHMPFileName);
    }

    private static bool SummaryExists(string folderPath)
    {
        return File.Exists(GetSummaryPath(folderPath));
    }


    private record NumberOfRecievedNewFromToMyMeetingHMPData(
        int Tick,
        double ReceivedNewFromToMyMeetingHmp,
        double ReceivedNewFromToMyMeetingHmpStd
    );

    private class DataRaw
    {
        public int Tick { get; set; }
    }
    
    private class DataProcessed
    {
        public int Tick { get; set; }
        public int receivedNewFromToMyMeetingHmp { get; set; }
    }
}