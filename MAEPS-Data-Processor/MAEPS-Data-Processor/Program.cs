﻿using System.Globalization;
using System.Reflection;

using Maes.Statistics.Patrolling;

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
        
        foreach (var experimentDirectory in Directory.GetDirectories(Directory.GetCurrentDirectory()))
        {
            var summaries = new List<ExperimentSummary>();
            Console.WriteLine(experimentDirectory);
            foreach (var scenarioDirectory in Directory.GetDirectories(experimentDirectory))
            {
                Console.WriteLine(scenarioDirectory);
                if (File.Exists(Path.Combine(scenarioDirectory, "*.png")))
                {
                    Console.WriteLine("Graphs already created. Skipping");
                    continue;
                }
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
                summaries.Add(summary);
                GenerateSummary(scenarioDirectory, summaries.TakeLast(1));
            }
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

        void PlotWorstIdleness(string path, List<PatrollingSnapShot> data)
        {
            Plot plot = new();
            plot.Add.ScatterPoints(data.Select(ps => ps.Tick).ToList(), data.Select(ps => ps.WorstGraphIdleness).ToList());

            AddDeadRobotsVerticalLines(data, plot);

            plot.Title("Worst Idleness");
            plot.XLabel("Tick");
            plot.YLabel("Worst Idleness");

            var graphPath = Path.Combine(path, "WorstGraphIdleness.png");
            plot.Save(graphPath, 1200, 600);
            Console.WriteLine("Saving to {0}", graphPath);
        }
        
        void PlotAverageIdleness(string path, List<PatrollingSnapShot> data)
        {
            Plot plot = new();
            plot.Add.ScatterPoints(data.Select(ps => ps.Tick).ToList(), data.Select(ps => ps.AverageGraphIdleness).ToList());
            
            AddDeadRobotsVerticalLines(data, plot);
            
            plot.Title("Average Idleness");
            plot.XLabel("Tick");
            plot.YLabel("Average Idleness");

            var graphPath = Path.Combine(path, "AverageGraphIdleness.png");
            Console.WriteLine("Saving to {0}", graphPath);
            plot.Save(graphPath, 1200, 600);
        }
    }

    private static void AddDeadRobotsVerticalLines(List<PatrollingSnapShot> data, Plot plot)
    {
        var deadRobots = data.GroupBy(ps => ps.NumberOfRobots).ToList();
        foreach (var group in deadRobots.Skip(1))
        {
            var line = plot.Add.VerticalLine(group.First().Tick, 1, Color.FromColor(System.Drawing.Color.Red), LinePattern.Dashed);
            if (group == deadRobots.Last())
            {
                line.LegendText = "Dead Robots";
            }
        }
    }
}