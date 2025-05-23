using AlgorithmSplitter;
using AlgorithmSplitter.Boxplots;

var argumentParser = new ArgumentParser();
argumentParser.ParseArguments(args);

var experimentsFolderPath = argumentParser.GetArgument("--path");
if (!Path.Exists(experimentsFolderPath))
{
    Console.WriteLine("Invalid path provided");
    return;
}

var groupBy = argumentParser.GetArgument("--groupBy");
var regenerateExistingSummaries = argumentParser.GetArgument("--regenerateExistingSummaries", bool.TryParse, false);
//var title = argumentParser.GetArgument("--title");
//var xLabel = argumentParser.GetArgument("--xLabel");
//var yLabel = argumentParser.GetArgument("--yLabel");

//var plotSettings = new PlotSettings(title, xLabel, yLabel);

if (!Path.Exists(experimentsFolderPath + ".zip"))
{
    Console.WriteLine("Zip before running the program because of moving files");
    return;
}

DataPreProcessor.FlattenDirectoryStructure(experimentsFolderPath);
Grouping.GroupScenariosByGroupingValue(groupBy, experimentsFolderPath);
Grouping.GroupScenariosByAlgorithmInGroups(experimentsFolderPath, groupBy);
SummaryAlgorithmSeedsCreator.CreateSummaryForAlgorithms(experimentsFolderPath, regenerateExistingSummaries, groupBy);

//using var boxPlotCreator = new BoxPlotCreator(experimentsFolderPath, groupBy, plotSettings);
//boxPlotCreator.CreateBoxPlotForAlgorithms();