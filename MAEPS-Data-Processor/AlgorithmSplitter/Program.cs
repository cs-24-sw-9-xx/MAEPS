using AlgorithmSplitter;

var argumentParser = new ArgumentParser();
argumentParser.ParseArguments(args);

var experimentsFolderPath = argumentParser.GetArgument("--path");
if (!Path.Exists(experimentsFolderPath))
{
    Console.WriteLine("Invalid path provided");
    return;
}

var groupBy = argumentParser.GetArgument("--groupBy");
var regenerateExistingSummaries =
    argumentParser.GetArgument("--regenerateExistingSummaries", bool.TryParse, false);

if (!Path.Exists(experimentsFolderPath + ".zip"))
{
    Console.WriteLine("Zip before running the program because of moving files");
    return;
}

DataPreProcessor.FlattenDirectoryStructure(experimentsFolderPath);
Grouping.GroupScenariosByGroupingValue(groupBy, experimentsFolderPath);
Grouping.GroupScenariosByAlgorithmInGroups(experimentsFolderPath);
SummaryAlgorithmSeedsCreator.CreateSummaryForAlgorithms(experimentsFolderPath, regenerateExistingSummaries);