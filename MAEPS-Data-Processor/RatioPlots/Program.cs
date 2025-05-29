using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using RatioPlots;

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

var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "Ratio", regenerate: regenerate);
var folderStructure = new Dictionary<string, IReadOnlyList<string>>();
var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(dataFolder);
foreach (var algorithmFolder in algorithmFolders)
{
    var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, algorithmFolder);
    foreach (var groupedFolderPath in groupedFolderPaths)
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(groupedFolderPath, regenerate: regenerate);
    }
    folderStructure[algorithmFolder] = groupedFolderPaths;
}

var ratioPlotter = new RatioComputer(dataFolder, folderStructure, groupBys);
ratioPlotter.GenerateRatioData();
ratioPlotter.CreatePlots();