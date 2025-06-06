﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MAEPS.Data.Processor.Utilities;
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
        DirectoryUtils.SetDefaultDataDirectory();
        
        var argumentParser = new ArgumentParser();
        argumentParser.ParseArguments(args);

        var experimentsFolderPath = argumentParser.GetArgument("--path");
        var groupBy = argumentParser.GetArgument("--groupBy");
        s_plotFailedRobots = argumentParser.GetArgument("--showFailed", bool.TryParse, false);
        s_plotIndividual = argumentParser.GetArgument("--plotIndividual", bool.TryParse, false);
        s_recompute = argumentParser.GetArgument("--recompute", bool.TryParse, false);


        if (Directory.Exists(experimentsFolderPath))
        {
            Directory.SetCurrentDirectory(experimentsFolderPath);
        }

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
        
        var worstIdlenessPlot = GeneratePlot(
            "Aggregated - Worst Idleness",
            "Worst Idleness",
            CalculateAverageIdleness(data, ps => ps.WorstGraphIdleness));
        
        var averageIdlenessPlot = GeneratePlot(
            "Aggregated - Average Idleness",
            "Average Idleness",
            CalculateAverageIdleness(data, ps => ps.AverageGraphIdleness));
        
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
            
            SaveAggregatedData(outputDirectory, algoData.ToList());
            
            worstIdlenessPlot.AddPlotLine(name, averageWorstIdlenessList);
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

    private static void SaveAggregatedData(string path, List<PatrollingSnapshot> data)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        
        using var writer = new StreamWriter(Path.Combine(path, "AggregatedData.csv"));
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(data);
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