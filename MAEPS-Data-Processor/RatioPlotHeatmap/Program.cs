using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using RatioPlotHeatmap;

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

var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "RatioHeatmap", regenerate: regenerate);
var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(dataFolder);
foreach (var algorithmFolder in algorithmFolders)
{
    var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, algorithmFolder);
    foreach (var groupedFolderPath in groupedFolderPaths)
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(groupedFolderPath, regenerate: regenerate);
    }
    var ratioPlotter = new RatioHeatmapComputer(dataFolder, groupBys, summery => summery.AverageIdleness, "averageIdleness");
    ratioPlotter.GenerateRatioData(algorithmFolder, groupedFolderPaths);
}