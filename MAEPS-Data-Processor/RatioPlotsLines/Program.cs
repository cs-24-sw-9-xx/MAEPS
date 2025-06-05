using System.Globalization;

using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using ScottPlot.DataSources;

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

var regenerate = argumentParser.GetArgument("--regenerate", bool.TryParse, true);

var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "RatioLines", regenerate: regenerate);
var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(dataFolder);

var ratioPlotter = new RatioLinesComputer(dataFolder, groupBys);
foreach (var algorithmFolder in algorithmFolders)
{
    var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, algorithmFolder);
    Parallel.ForEach(groupedFolderPaths, groupedFolderPath =>
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(groupedFolderPath, regenerate: regenerate);
    });
    ratioPlotter.GenerateRatioData(algorithmFolder, groupedFolderPaths);
}

ratioPlotter.SaveGlobalWorstIdlenessPlots();