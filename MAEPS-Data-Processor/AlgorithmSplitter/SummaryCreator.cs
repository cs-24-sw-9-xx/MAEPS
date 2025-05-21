using System.Globalization;

using MAEPS_Data_Processor;

namespace AlgorithmSplitter;

internal static class SummaryAlgorithmSeedsCreator
{
    private const string SummaryFileName = "summary.csv";

    public static void CreateSummaryForAlgorithms(string experimentsFolderPath, bool regenerateExistingSummaries)
    {
        foreach (var folderByGroupValue in Directory.GetDirectories(experimentsFolderPath))
        {
            foreach (var algorithmDirectory in Directory.GetDirectories(folderByGroupValue))
            {
                if (SummaryExists(algorithmDirectory) && !regenerateExistingSummaries)
                {
                    Console.WriteLine("Summary already exists. Skipping for {0}", algorithmDirectory);
                    continue;
                }
                
                Console.WriteLine("Create summary for {0}", algorithmDirectory);
                
                var summaries = new List<ExperimentSummary>();
                foreach (var scenarioDirectory in Directory.GetDirectories(algorithmDirectory))
                {
                    var data = CsvDataReader.ReadPatrollingCsv(Path.Combine(scenarioDirectory, "patrolling.csv"));
                    var summary = new ExperimentSummary
                    {
                        Algorithm = Path.GetFileName(scenarioDirectory),
                        AverageIdleness = data.Average(ps => ps.AverageGraphIdleness),
                        WorstIdleness = data.Max(ps => ps.WorstGraphIdleness),
                        TotalDistanceTraveled = data.Last().TotalDistanceTraveled,
                        TotalCycles = data.Last().CompletedCycles,
                        NumberOfRobotsStart = data.First().NumberOfRobots,
                        NumberOfRobotsEnd = data.Last().NumberOfRobots
                    };
                    summaries.Add(summary);
                }

                GenerateSummary(algorithmDirectory, summaries);
                Console.WriteLine("Summary created for {0}", algorithmDirectory);
            }
        }
    }
    
    
    private static string GetSummaryPath(string path)
    {
        return Path.Combine(path, SummaryFileName);
    }
    
    public static bool SummaryExists(string path)
    {
        return File.Exists(GetSummaryPath(path));
    }

    private static void GenerateSummary(string path, IEnumerable
        <ExperimentSummary> summaries)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
            
        using var writer = new StreamWriter(GetSummaryPath(path));
        using var csv = new CsvHelper.CsvWriter(writer, config);
    
        csv.WriteRecords(summaries);
    }
}