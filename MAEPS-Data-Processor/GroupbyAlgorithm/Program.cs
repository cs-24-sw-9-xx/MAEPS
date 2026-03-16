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

var regenerate = argumentParser.GetArgument("--regenerate", bool.TryParse, true);

var dataFolder = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "Grouped", regenerate: regenerate);

var mapTypeFolders = DataPreProcessor.SplitMapTypesDataFolder(dataFolder, regenerate: regenerate);

foreach (var (_, folder) in mapTypeFolders)
{
    GroupingAlgorithm.GroupScenarioByAlgorithmName(folder);
}