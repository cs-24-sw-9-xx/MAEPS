using System.Collections.Concurrent;
using System.Globalization;

using MAEPS.Data.Processor.Utilities;

using Maes.Statistics.Snapshots;

using ScottPlot;

namespace MAEPS.Data.Processor;

internal class Program
{
    private static void Main(string[] args)
    {
        // Defaults to using the data folder in the MAEPS project.
        DirectoryUtils.SetDefaultDataDirectory();
        
        var argumentParser = new ArgumentParser();
        argumentParser.ParseArguments(args);

        var experimentsFolderPath = argumentParser.GetArgument("--path");
        
        // Override used to specify another location.
        if (Directory.Exists(experimentsFolderPath))
        {
            Directory.SetCurrentDirectory(experimentsFolderPath);
        }
        
        var groupBy = argumentParser.GetArgument("--groupBy");

        foreach (var experimentDirectory in Directory.GetDirectories(experimentsFolderPath))
        {
            DirectoryUtils.GroupScenarios(groupBy, experimentDirectory);
            DirectoryUtils.GroupScenariosByAlgorithm(experimentDirectory, groupBy);

            foreach (var groupedDirectory in Directory.GetDirectories(experimentDirectory, groupBy + "*", SearchOption.TopDirectoryOnly))
            {
                if (File.Exists(Path.Combine(groupedDirectory, "summary.csv")))
                {
                    return;
                }
                
                var summaries = new List<ExperimentSummary>();
                var patrollingData = new ConcurrentDictionary<string, ConcurrentBag<PatrollingSnapshot>>();
                Console.WriteLine(groupedDirectory);
                foreach (var algorithmDirectory in Directory.GetDirectories(groupedDirectory))
                {
                    Parallel.ForEach(Directory.GetDirectories(algorithmDirectory),
                        scenarioDirectory =>
                        {
                            var name = scenarioDirectory.Replace(groupedDirectory,
                                    string.Empty)
                                .Split('-')[0];
                            var bag = patrollingData.GetOrAdd(name,
                                _ => new ConcurrentBag<PatrollingSnapshot>());
                            if (!File.Exists(Path.Combine(scenarioDirectory,
                                    "patrolling.csv")))
                            {
                                Console.WriteLine(
                                    $"Skipping {scenarioDirectory} because patrolling.csv is missing or incomplete.");
                                return;
                            }

                            var data = CsvDataReader.ReadPatrollingCsv(Path.Combine(scenarioDirectory,
                                "patrolling.csv"));
                            PlotWorstIdleness(scenarioDirectory,
                                data);
                            PlotAverageIdleness(scenarioDirectory,
                                data);

                            var summary = new ExperimentSummary
                            {
                                Algorithm = Path.GetFileName(scenarioDirectory),
                                AverageIdleness = data.Last()
                                    .AverageGraphIdleness,
                                WorstIdleness = data.Max(ps => ps.WorstGraphIdleness),
                                TotalDistanceTraveled = data.Last()
                                    .TotalDistanceTraveled,
                                TotalCycles = data.Last()
                                    .CompletedCycles,
                                NumberOfRobotsStart = data.First()
                                    .NumberOfRobots,
                                NumberOfRobotsEnd = data.Last()
                                    .NumberOfRobots
                            };
                            lock (summaries)
                            {
                                summaries.Add(summary);
                            }

                            foreach (var item in data)
                            {
                                bag.Add(item);
                            }
                        });
                    
                    Plot plot = new();

                    foreach (var (name, algoData) in patrollingData)
                    {
                        
                        var averageWorstIdlenessList = algoData
                            .AsParallel()
                            .GroupBy(d => d.CommunicationSnapshot.Tick)
                            .OrderBy(g => g.Key)
                            .Select(g => (g.Key, g.Average(d => d.WorstGraphIdleness)))
                            .ToList();
                        
                        SaveAggretatedData(algorithmDirectory, algoData.ToList());

                        PlotWorstIdlenessAll(
                            algorithmDirectory,
                            plot, 
                            name,
                            averageWorstIdlenessList);
                    }
                    
                    
                    var graphPath = Path.Combine(groupedDirectory, "WorstGraphIdleness.png");
                    plot.Save(graphPath, 1200, 600);
                    Console.WriteLine("Saving to {0}", graphPath);
                    GenerateSummary(groupedDirectory, summaries);
                }
            }
        }

        void GenerateSummary(string path, IEnumerable
            <ExperimentSummary> summaries)
        {
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };
            
            using var writer = new StreamWriter(Path.Combine(path, "summary.csv"));
            using var csv = new CsvHelper.CsvWriter(writer, config);
            // Write header and record
            csv.WriteRecords(summaries);
        }

        void SaveAggretatedData(string path, List<PatrollingSnapshot> data)
        {
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };
            
            using var writer = new StreamWriter(Path.Combine(path, "AggregatedData.csv"));
            using var csv = new CsvHelper.CsvWriter(writer, config);
            // Write header and record
            csv.WriteRecords(data);
        }

        void PlotWorstIdlenessAll(string path, Plot plot, string name, List<(int tick, double idleness)> data)
        {
            var scatterPlot = plot.Add.ScatterPoints(data.Select(ps => ps.tick).ToList(), data.Select(ps => ps.idleness).ToList());
            
            scatterPlot.LegendText = name;
        }

        
        void PlotWorstIdleness(string path, List<PatrollingSnapshot> data)
        {
            Plot plot = new();
            plot.Add.Signal(data.Select(ps => ps.WorstGraphIdleness).ToList());

            AddDeadRobotsVerticalLines(data, plot);

            plot.Title("Worst Idleness");
            plot.XLabel("Tick");
            plot.YLabel("Worst Idleness");

            var graphPath = Path.Combine(path, "WorstGraphIdleness.png");
            plot.Save(graphPath, 1200, 600);
            Console.WriteLine("Saving to {0}", graphPath);
        }
        
        void PlotAverageIdleness(string path, List<PatrollingSnapshot> data)
        {
            Plot plot = new();
            plot.Add.Signal(data.Select(ps => ps.AverageGraphIdleness).ToList());
            
            AddDeadRobotsVerticalLines(data, plot);
            
            plot.Title("Average Idleness");
            plot.XLabel("Tick");
            plot.YLabel("Average Idleness");

            var graphPath = Path.Combine(path, "AverageGraphIdleness.png");
            Console.WriteLine("Saving to {0}", graphPath);
            plot.Save(graphPath, 1200, 600);
        }
    }
    
    private static void AddDeadRobotsVerticalLines(List<PatrollingSnapshot> data, Plot plot)
    {
        var deadRobots = data.GroupBy(ps => ps.NumberOfRobots).ToList();
        foreach (var group in deadRobots.Skip(1))
        {
            var line = plot.Add.VerticalLine(group.First().CommunicationSnapshot.Tick, 1, Color.FromColor(System.Drawing.Color.Red), LinePattern.Dashed);
            if (group == deadRobots.Last())
            {
                line.LegendText = "Dead Robots";
            }
        }
    }
}