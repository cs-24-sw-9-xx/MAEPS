using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

var argumentParser = new ArgumentParser();
argumentParser.ParseArguments(args);

var experimentsFolderPath = argumentParser.GetArgument("--path");
if (!Path.Exists(experimentsFolderPath))
{
    Console.WriteLine("Invalid path provided");
    return;
}

var groupBys = argumentParser.GetArgumentList("--groupBy", []);
var regenerate = argumentParser.GetArgument("--regenerate", bool.TryParse, true);


var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "Variance", regenerate: regenerate);
var groupedFolderPaths = Grouping.GroupScenariosByGroupingValue(groupBys, dataFolder);
foreach (var groupedFolderPath in groupedFolderPaths)
{
    var mapTypeFolders = DataPreProcessor.SplitMapTypesDataFolder(groupedFolderPath, regenerate: regenerate);

    foreach (var (_, folder) in mapTypeFolders)
    {
        var algorithmFolders = GroupingAlgorithm.GroupScenarioByAlgorithmName(folder);
    
        Parallel.ForEach(algorithmFolders, algorithmFolder =>
        {
            VarianceAlgorithmCreator.CreateVarianceFromScenarios(algorithmFolder, regenerate: regenerate);
            NumberOfMeetingsAlgorithmCreator.CreateNumberOfRecievedNewFromToMyMeetingHMPFromScenarios(algorithmFolder, regenerate: regenerate);
        });
    }
}