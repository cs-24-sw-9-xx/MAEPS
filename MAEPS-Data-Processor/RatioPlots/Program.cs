using DataProcessorTools;

using MAEPS.Data.Processor.Utilities;

var argumentParser = new ArgumentParser();
argumentParser.ParseArguments(args);

var experimentsFolderPath = argumentParser.GetArgument("--path");
if (!Path.Exists(experimentsFolderPath))
{
    Console.WriteLine("Invalid path provided");
    return;
}

var groupBys = argumentParser.GetArgumentList("--groupBy");

var experimentsFolderCopyPath = DataPreProcessor.CopyDataFolder(experimentsFolderPath);

var folderStructure = new Dictionary<string, IReadOnlyList<string>>();

var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(experimentsFolderCopyPath);
foreach (var algorithmFolder in algorithmFolders)
{
    var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, algorithmFolder);
    foreach (var groupedFolderPath in groupedFolderPaths)
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(groupedFolderPath, false);
    }
    folderStructure[algorithmFolder] = groupedFolderPaths;
}

var ratioPlotter = new RatioPlotter(folderStructure, groupBys);

public class RatioPlotter
{
    public RatioPlotter(Dictionary<string, IReadOnlyList<string>> folderStructure, string[] groupBys)
    {
        
    }
}




