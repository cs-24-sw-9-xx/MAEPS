using System.Globalization;
using System.Reflection;

using Maes.Statistics.Snapshots;

using ScottPlot;

namespace MAEPS_Data_Processor;

internal class Program
{
    private static void Main(string[] args)
    {
        // Change directory to the data folder
        var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
        do
        {
            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null)
            {
                throw new Exception("Could not find data folder");
            }
        } while (directoryInfo.Name != "MAEPS");
        
        Console.WriteLine("Found data directory {0}", directoryInfo.FullName + "/data");
        Directory.SetCurrentDirectory(directoryInfo.FullName + "/data");
        
        if (Directory.Exists(args[0]))
        {
            Directory.SetCurrentDirectory(args[0]);
        }
        
        foreach (var experimentDirectory in Directory.GetDirectories(Directory.GetCurrentDirectory()))
        {
            var summaries = new List<ExperimentSummary>();
            var patrollingData = new Dictionary<string, List<PatrollingSnapshot>>();
            Console.WriteLine(experimentDirectory);
            Parallel.ForEach(Directory.GetDirectories(experimentDirectory), scenarioDirectory =>
            {
                var name = scenarioDirectory.Replace(experimentDirectory, string.Empty).Split('-')[0];
                Console.WriteLine(scenarioDirectory);

                var data = CsvDataReader.ReadPatrollingCsv(Path.Combine(scenarioDirectory, "patrolling.csv"));
                PlotWorstIdleness(scenarioDirectory, data);
                PlotAverageIdleness(scenarioDirectory, data);

                var summary = new ExperimentSummary
                {
                    Algorithm = Path.GetFileName(scenarioDirectory),
                    AverageIdleness = data.Last().AverageGraphIdleness,
                    WorstIdleness = data.Max(ps => ps.WorstGraphIdleness),
                    TotalDistanceTraveled = data.Last().TotalDistanceTraveled,
                    TotalCycles = data.Last().CompletedCycles,
                    NumberOfRobotsStart = data.First().NumberOfRobots,
                    NumberOfRobotsEnd = data.Last().NumberOfRobots
                };
                lock (summaries)
                {
                    summaries.Add(summary);
                }

                lock (patrollingData)
                {
                    if (!patrollingData.ContainsKey(name))
                    {
                        patrollingData.Add(name, new List<PatrollingSnapshot>());
                    }
                    patrollingData[name].AddRange(data);
                }
            });
            
            Plot plot = new();
            foreach (var (name, algoData) in patrollingData)
            {
                var averageWorstIdlenessList = algoData
                    .GroupBy(d => d.CommunicationSnapshot.Tick)
                    .Select(group => (
                        group.Key,
                        group.Average(d => d.WorstGraphIdleness)
                    ))
                    .ToList();

                PlotWorstIdlenessAll(
                    experimentDirectory,
                    plot, 
                    name,
                    averageWorstIdlenessList);
            }
            
            var graphPath = Path.Combine(experimentDirectory, "WorstGraphIdleness.png");
            plot.Save(graphPath, 1200, 600);
            Console.WriteLine("Saving to {0}", graphPath);
            GenerateSummary(experimentDirectory, summaries);
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
            csv.WriteHeader<ExperimentSummary>();
            csv.NextRecord();
            foreach (var summary in summaries)
            {
                csv.WriteRecord(summary);
                csv.NextRecord();
            }
        }

        void PlotWorstIdlenessAll(string path, Plot plot, string name, List<(int tick, double idleness)> data)
        {
            var scatterPlot = plot.Add.ScatterPoints(data.Select(ps => ps.tick).ToList(), data.Select(ps => ps.idleness).ToList());
            
            scatterPlot.LegendText = name;
        }

        
        void PlotWorstIdleness(string path, List<PatrollingSnapshot> data)
        {
            Plot plot = new();
            plot.Add.ScatterPoints(data.Select(ps => ps.CommunicationSnapshot.Tick).ToList(), data.Select(ps => ps.WorstGraphIdleness).ToList());

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
            plot.Add.ScatterPoints(data.Select(ps => ps.CommunicationSnapshot.Tick).ToList(), data.Select(ps => ps.AverageGraphIdleness).ToList());
            
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