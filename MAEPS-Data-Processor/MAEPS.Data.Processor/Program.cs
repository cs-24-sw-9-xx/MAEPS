using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MAEPS.Data.Processor.Utilities;

using Maes.Statistics.Csv;
using Maes.Statistics.Snapshots;
using ScottPlot;

namespace MAEPS.Data.Processor;

internal static class Program
{
    private const int PlotWidth = 1200;
    private const int PlotHeight = 800;

    private static bool s_plotFailedRobots;
    private static bool s_plotIndividual;
    private static bool s_recompute;

    public static void Main(string[] args)
    {
        var argumentParser = new ArgumentParser();
        argumentParser.ParseArguments(args);

        var experimentsFolderPath = argumentParser.GetArgument("--path");
        var groupBy = argumentParser.GetArgument("--groupBy");
        s_plotFailedRobots = argumentParser.GetArgument("--showFailed", bool.TryParse, false);
        s_plotIndividual = argumentParser.GetArgument("--plotIndividual", bool.TryParse, false);
        s_recompute = argumentParser.GetArgument("--recompute", bool.TryParse, false);
        
        ProcessExperimentDirectories(experimentsFolderPath, groupBy);
    }

    private static void ProcessExperimentDirectories(string experimentsFolderPath, string groupBy)
    {
        foreach (var experimentDirectory in Directory.GetDirectories(experimentsFolderPath))
        {
            ProcessSingleExperiment(experimentDirectory, groupBy);
        }
    }

    private static void ProcessSingleExperiment(string experimentDirectory, string groupBy)
    {
        DirectoryUtils.GroupScenarios(groupBy, experimentDirectory);
        DirectoryUtils.GroupScenariosByAlgorithm(experimentDirectory, groupBy);

        foreach (var groupedDirectory in Directory.GetDirectories(experimentDirectory, $"{groupBy}*", SearchOption.TopDirectoryOnly))
        {
            if (!s_recompute && File.Exists(Path.Combine(groupedDirectory, "summary.csv")))
            {
                Console.WriteLine("Data already processed. Skipping for {0}", groupedDirectory);
                continue;
            }

            ProcessGroupedDirectory(groupedDirectory);
        }
    }

    private static void ProcessGroupedDirectory(string groupedDirectory)
    {
        Console.WriteLine(groupedDirectory);
        var summaries = new ConcurrentBag<ExperimentSummary>();
        var patrollingData = new ConcurrentDictionary<string, ConcurrentBag<PatrollingSnapshot>>();

        foreach (var algorithmDirectory in Directory.GetDirectories(groupedDirectory))
        {
            ProcessAlgorithmDirectory(algorithmDirectory, groupedDirectory, summaries, patrollingData);
            GenerateSummary(groupedDirectory, summaries);
        }
        GenerateAggregatedPlots(groupedDirectory, patrollingData);
    }

    private static void ProcessAlgorithmDirectory(
        string algorithmDirectory,
        string groupedDirectory,
        ConcurrentBag<ExperimentSummary> summaries,
        ConcurrentDictionary<string, ConcurrentBag<PatrollingSnapshot>> patrollingData)
    {
        Parallel.ForEach(Directory.GetDirectories(algorithmDirectory), scenarioDirectory =>
        {
            var name = Path.GetFileName(algorithmDirectory);
            var bag = patrollingData.GetOrAdd(name, _ => new ConcurrentBag<PatrollingSnapshot>());
        
            var patrollingFilePath = Path.Combine(scenarioDirectory, "patrolling.csv");
            if (!File.Exists(patrollingFilePath))
            {
                Console.WriteLine($"Skipping {scenarioDirectory} because patrolling.csv is missing or incomplete.");
                return;
            }

            var data = CsvDataReader.ReadPatrollingCsv(patrollingFilePath);

            if (s_plotIndividual)
            {
                GenerateIndividualPlots(name, scenarioDirectory, data);
            }
            
            summaries.Add(CreateExperimentSummary(scenarioDirectory, data));

            foreach (var item in data)
            {
                bag.Add(item);
            }
        });

        var data = patrollingData[Path.GetFileName(algorithmDirectory)];
        
        var worstIdleness = CalculateAverageIdleness(data, ps => ps.WorstGraphIdleness);
        var averageIdleness = CalculateAverageIdleness(data, ps => ps.AverageGraphIdleness);
        
        var worstIdlenessPlot = GeneratePlot(
            "Aggregated - Worst Idleness",
            "Worst Idleness", worstIdleness);
        
        var averageIdlenessPlot = GeneratePlot(
            "Aggregated - Average Idleness",
            "Average Idleness",
            averageIdleness);
        
        if (s_plotFailedRobots)
        {
            worstIdlenessPlot.AddDeadRobotsVerticalLines(data);
            averageIdlenessPlot.AddDeadRobotsVerticalLines(data);
        }
        
        
        
        SavePlot(worstIdlenessPlot, algorithmDirectory, "WorstGraphIdleness.png");
        SavePlot(averageIdlenessPlot, algorithmDirectory, "AverageGraphIdleness.png");
    }

    private static ExperimentSummary CreateExperimentSummary(string scenarioDirectory, List<PatrollingSnapshot> data)
    {
        return new ExperimentSummary
        {
            Algorithm = Path.GetFileName(scenarioDirectory),
            AverageIdleness = data.Last().AverageGraphIdleness,
            WorstIdleness = data.Max(ps => ps.WorstGraphIdleness),
            TotalDistanceTraveled = data.Last().TotalDistanceTraveled,
            TotalCycles = data.Last().CompletedCycles,
            NumberOfRobotsStart = data.First().NumberOfRobots,
            NumberOfRobotsEnd = data.Last().NumberOfRobots
        };
    }

    struct AggregatedSnapshot : ICsvData
    {
        public int Tick { get; set; }
        public double Idleness { get; set; }
        
        public AggregatedSnapshot(int tick, double idleness) 
        {
            Tick = tick;
            Idleness = idleness;
        }

        public void WriteHeader(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(nameof(Tick));
            streamWriter.Write(delimiter);
            streamWriter.Write(nameof(Idleness));
        }

        public void WriteRow(StreamWriter streamWriter, char delimiter)
        {
            streamWriter.Write(Tick);
            streamWriter.Write(delimiter);
            streamWriter.Write(Idleness);
        }

        public ReadOnlySpan<string> ReadRow(ReadOnlySpan<string> columns)
        {
            throw new NotImplementedException();
        }
    }

    private static void GenerateIndividualPlots(string algoName, string path, List<PatrollingSnapshot> data)
    {
        var worstIdlenessPlot = GeneratePlot(algoName, $"Worst Idleness", 
            data.Select(ps => ps.WorstGraphIdleness).ToList());
        
        var averageIdlenessPlot = GeneratePlot(algoName, $"Average Idleness", 
            data.Select(ps => ps.AverageGraphIdleness).ToList());

        if (s_plotFailedRobots)
        {
            worstIdlenessPlot.AddDeadRobotsVerticalLines(data);
            averageIdlenessPlot.AddDeadRobotsVerticalLines(data);
        }
        
        SavePlot(worstIdlenessPlot, path, "WorstGraphIdleness.png");
        SavePlot(averageIdlenessPlot, path, "AverageGraphIdleness.png");
    }

    private static Plot GeneratePlot<T>(string title, string yLabel, List<T> values)
    {
        var plot = new Plot();
        plot.Add.Signal(values);

        plot.Title(title);
        plot.XLabel("Tick");
        plot.YLabel(yLabel);
        
        return plot;
    }

    private static void GenerateAggregatedPlots(string outputDirectory, ConcurrentDictionary<string, ConcurrentBag<PatrollingSnapshot>> patrollingData)
    {
        var worstIdlenessPlot = new Plot();
        worstIdlenessPlot.XLabel("Tick");
        worstIdlenessPlot.YLabel("Worst Idleness");

        var averageIdlenessPlot = new Plot();
        averageIdlenessPlot.XLabel("Tick");
        averageIdlenessPlot.YLabel("Average Idleness");

        if (s_plotFailedRobots)
        {
            var largestDataSet = patrollingData
                .OrderByDescending(kvp => kvp.Value.Count)
                .First()
                .Value;

            worstIdlenessPlot.AddDeadRobotsVerticalLines(largestDataSet);
            averageIdlenessPlot.AddDeadRobotsVerticalLines(largestDataSet);
        }
        
        foreach (var (name, algoData) in patrollingData)
        {
            var averageWorstIdlenessList = CalculateAverageIdleness(algoData, ps => ps.WorstGraphIdleness);
            var averageAvgIdlenessList = CalculateAverageIdleness(algoData, ps => ps.AverageGraphIdleness);
            
            //SaveAggregatedData(outputDirectory, name, algoData.ToList());
            
            using (var csv = new CsvDataWriter<AggregatedSnapshot>(Path.Combine(outputDirectory, name+ "WorstGraphIdleness.csv")))
            {
                for (var i = 0; i < averageWorstIdlenessList.Count; i++)
                {
                    csv.AddRecord(new AggregatedSnapshot(i, averageWorstIdlenessList[i]));
                }
                
                csv.Finish();
            }
            
            using (var csv = new CsvDataWriter<AggregatedSnapshot>(Path.Combine(outputDirectory, name+ "AverageGraphIdleness.csv")))
            {
                for (var i = 0; i < averageAvgIdlenessList.Count; i++)
                {
                    csv.AddRecord(new AggregatedSnapshot(i, averageAvgIdlenessList[i]));
                }
                
                csv.Finish();
            }
            
            worstIdlenessPlot.AddPlotLine(name,  averageWorstIdlenessList);
            averageIdlenessPlot.AddPlotLine(name, averageAvgIdlenessList);
        }
        
        SavePlot(averageIdlenessPlot, outputDirectory, "AverageGraphIdleness.png");
        SavePlot(worstIdlenessPlot, outputDirectory, "WorstGraphIdleness.png");
        Console.WriteLine("Saving aggregated graphs");
    }

    private static List<double> CalculateAverageIdleness(
        IEnumerable<PatrollingSnapshot> snapshots,
        Func<PatrollingSnapshot, double> idlenessSelector)
    {
        return snapshots
            .AsParallel()
            .GroupBy(d => d.CommunicationSnapshot.Tick)
            .OrderBy(g => g.Key)
            .Select(g => g.Average(idlenessSelector))
            .ToList();
    }

    private static void SavePlot(Plot plot, string directory, string fileName)
    {
        plot.Legend.Alignment = Alignment.UpperLeft;
        var path = Path.Combine(directory, fileName);
        plot.Save(path, PlotWidth, PlotHeight);

        Console.WriteLine("Saved {0} to {1}",fileName, path);
    }

    private static void GenerateSummary(string path, IEnumerable<ExperimentSummary> summaries)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        
        using var writer = new StreamWriter(Path.Combine(path, "summary.csv"));
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(summaries);
    }

    private static void SaveAggregatedData(string path, string name, List<PatrollingSnapshot> data)
    {
        using var csv1 = new CsvDataWriter<PatrollingSnapshot>(Path.Combine(path, $"{name}AggregatedData.csv"));
        foreach (var snapshot in data)
        {
            csv1.AddRecord(snapshot);
        }

        csv1.Finish();
    }
    
    private static void AddPlotLine(this Plot plot, string name, List<double> data)
    {
        var addedPlot = plot.Add.Signal(data);
        addedPlot.LegendText = name;
    }

    private static void AddDeadRobotsVerticalLines(this Plot plot, IEnumerable<PatrollingSnapshot> data)
    {
        var failedRobotTicks = data.GroupBy(ps => ps.NumberOfRobots)
            .Skip(1)
            .Select(g => g.First().CommunicationSnapshot.Tick).ToArray();
        
        foreach (var tick in failedRobotTicks)
        {
            var line = plot.Add.VerticalLine(tick, 1, Color.FromColor(System.Drawing.Color.Red), LinePattern.Dashed);
            if (tick == failedRobotTicks.Last())
            {
                line.LegendText = "Dead Robots";
            }
        }
    }
}