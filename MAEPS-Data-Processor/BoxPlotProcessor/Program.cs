using BoxPlotProcessor;
using BoxPlotProcessor.Boxplots;

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

var groupBy = argumentParser.GetArgument("--groupBy");
var groupBys = new[] { groupBy };

var regenerateExistingSummaries = argumentParser.GetArgument("--regenerate", bool.TryParse, false);
var title = argumentParser.GetArgument("--title");
var xLabel = argumentParser.GetArgument("--xLabel");
var yLabel = argumentParser.GetArgument("--yLabel");

var plotSettings = new PlotSettings(title, xLabel, yLabel);

var experimentsFolderCopyPath = DataPreProcessor.CopyDataFolder(experimentsFolderPath);


Grouping.GroupScenariosByGroupingValue(groupBys, experimentsFolderCopyPath);
Grouping.GroupScenariosByAlgorithmInGroups(experimentsFolderCopyPath, groupBys);
SummaryAlgorithmSeedsCreator.CreateSummaryForAlgorithms(experimentsFolderCopyPath, regenerateExistingSummaries, groupBys);

if (groupBys.Length == 1)
{
    using var boxPlotCreator = new BoxPlotCreator(experimentsFolderPath, groupBys[0], plotSettings);
    boxPlotCreator.CreateBoxPlotForAlgorithms();
}


