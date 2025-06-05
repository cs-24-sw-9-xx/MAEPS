using System.Globalization;

using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using ScottPlot;

var argumentParser = new ArgumentParser();
argumentParser.ParseArguments(args);

var experimentsFolderPath = argumentParser.GetArgument("--path");
if (!Path.Exists(experimentsFolderPath))
{
    Console.WriteLine("Invalid path provided");
    return;
}

var groupBys = argumentParser.GetArgumentList("--groupBy");
if (groupBys.Length != 2)
{
    Console.WriteLine("Please provide exactly two groupBy arguments.");
    return;
}

var regenerate = argumentParser.GetArgument("--regenerate", bool.TryParse, false);

var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "RatioLines", regenerate: regenerate);
var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(dataFolder);
foreach (var algorithmFolder in algorithmFolders)
{
    var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, algorithmFolder);
    foreach (var groupedFolderPath in groupedFolderPaths)
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(groupedFolderPath, regenerate: regenerate);
    }
    var ratioPlotter = new RatioLinesComputer(dataFolder, groupBys);
    ratioPlotter.GenerateRatioData(algorithmFolder, groupedFolderPaths);
}


public class RatioLinesComputer(
    string storeInFolderPath,
    string[] groupBys)
{
    public void GenerateRatioData(string algorithmFolderPath, IEnumerable<string> groupedFolders)
    {
        var mapSizes = new HashSet<double>();
        var robotCounts = new HashSet<double>();
        
        var algorithmName = Path.GetFileName(algorithmFolderPath);

        var dataByMapSize = new Dictionary<int, (List<(int, double)> average, List<(int, double)> worst)>();
        
        foreach (var folder in groupedFolders)
        {
            var values = folder.GroupingValues<int>(groupBys);
            var mapSize = values[0];
            var robotCount = values[1];
            
            var average = SummaryAlgorithmSeedsCreator.GetAverageOfExperimentSummary(folder);
            if (average == null)
            {
                Console.WriteLine($"No average found for folder: {folder}");
                continue;
            }
            
            if (!dataByMapSize.ContainsKey(mapSize))
            {
                dataByMapSize[mapSize] = (
                    [],
                    []
                );
            }
            
            dataByMapSize[mapSize].average.Add((robotCount, average.AverageIdleness));
            dataByMapSize[mapSize].worst.Add((robotCount, average.WorstIdleness));
        }
        
        Multiplot multiplot = new();

        multiplot.AddPlots(dataByMapSize.Keys.Count);

        var i = 0;
        foreach (var (mapSize, (average, worst)) in dataByMapSize.OrderBy(d => d.Key))
        {
            var plot = multiplot.Subplots.GetPlot(i);
            i++;
            
            var sortedAverage = average.OrderBy(x => x.Item1).ToList();
            plot.AddAlgorithmLinePlot("Average idleness", sortedAverage.Select(x => (double)x.Item1).ToArray(), sortedAverage.Select(x => x.Item2).ToArray(), LinePattern.Solid);
            
            var sortedWorst = worst.OrderBy(x => x.Item1).ToList();
            plot.AddAlgorithmLinePlot("Worst idleness", sortedWorst.Select(x => (double)x.Item1).ToArray(), sortedWorst.Select(x => x.Item2).ToArray(), LinePattern.Dashed);
            
            plot.YLabel("Ticks");
            plot.XLabel("Robot count");
            plot.Title($"{algorithmName} - {mapSize}", 10);
            plot.Legend.IsVisible = false;
        }
        
        multiplot.Layout = new ScottPlot.MultiplotLayouts.Columns();
        var fileName = $"{algorithmName}.png";
        var filePath = Path.Combine(storeInFolderPath, fileName);
        multiplot.SavePng(filePath, 400 * dataByMapSize.Keys.Count, 300);
    }
}