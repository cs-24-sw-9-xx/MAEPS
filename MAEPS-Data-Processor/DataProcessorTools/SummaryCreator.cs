using System.Globalization;

using MAEPS.Data.Processor;

namespace DataProcessorTools;

public static class SummaryAlgorithmSeedsCreator
{
    private const string SummaryFileName = "summary.csv";

    /// <summary>
    /// This method creates a summary for each algorithm in the given scenarios folder.
    /// </summary>
    /// <param name="scenariosFolderPath">The path to the folder that contains the scenarios as folders</param>
    /// <param name="regenerateSummary">If true, it regenerate the summary file </param>
    public static void CreateSummaryFromScenarios(string scenariosFolderPath, bool regenerateSummary)
    {
        foreach (var scenarioFolderPath in Directory.GetDirectories(scenariosFolderPath))
        {
            if (SummaryExists(scenarioFolderPath) && !regenerateSummary)
            {
                Console.WriteLine("Summary already exists. Skipping for {0}", scenarioFolderPath);
                continue;
            }
                
            Console.WriteLine("Create summary for {0}", scenarioFolderPath);
                
            var summaries = new List<ExperimentSummary>();
            foreach (var scenarioDirectory in Directory.GetDirectories(scenarioFolderPath))
            {
                var patrollingCsvPath = Path.Combine(scenarioDirectory, "patrolling.csv");
                if (!File.Exists(patrollingCsvPath))
                {
                    Console.WriteLine("No patrolling.csv found in {0}. Skipping.", scenarioDirectory);
                    continue;
                }
                    
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

            GenerateSummary(scenariosFolderPath, summaries);
            Console.WriteLine("Summary created for {0}", scenariosFolderPath);
        }
    }
    
    
    private static string GetSummaryPath(string folderPath)
    {
        return Path.Combine(folderPath, SummaryFileName);
    }

    private static bool SummaryExists(string folderPath)
    {
        return File.Exists(GetSummaryPath(folderPath));
    }

    private static void GenerateSummary(string folderPath, IEnumerable
        <ExperimentSummary> summaries)
    {
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
            
        using var writer = new StreamWriter(GetSummaryPath(folderPath));
        using var csv = new CsvHelper.CsvWriter(writer, config);
    
        csv.WriteRecords(summaries);
    }
    
    public static ExperimentSummary[] GetSummary(string folderPath)
    {
        if (!SummaryExists(folderPath))
        {
            throw new FileNotFoundException("Summary file not found", GetSummaryPath(folderPath));
        }
        
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
            
        using var reader = new StreamReader(GetSummaryPath(folderPath));
        using var csv = new CsvHelper.CsvReader(reader, config);
    
        return csv.GetRecords<ExperimentSummary>().ToArray();
    }

    public static AverageExperimentSummary GetAverageOfExperimentSummary(string folderPath)
    {
        if (!SummaryExists(folderPath))
        {
            throw new FileNotFoundException("Summary file not found", GetSummaryPath(folderPath));
        }
        
        var summaries = GetSummary(folderPath);
        if (summaries.Length == 0)
        {
            throw new InvalidOperationException("No summaries found in the folder.");
        }

        return new AverageExperimentSummary
        (
            summaries.Average(s => s.AverageIdleness),
            summaries.Average(s => s.WorstIdleness),
            summaries.Sum(s => s.TotalDistanceTraveled),
            summaries.Sum(s => s.TotalCycles),
            summaries.First().NumberOfRobotsStart,
            summaries.Last().NumberOfRobotsEnd
        );
    }
    
    public record AverageExperimentSummary(double AverageIdleness,
        double WorstIdleness, 
        double TotalDistanceTraveled, 
        int TotalCycles, 
        int NumberOfRobotsStart, 
        int NumberOfRobotsEnd);
}