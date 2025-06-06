using BoxPlotProcessor.Boxplots;

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

var groupBy = argumentParser.GetArgument("--groupBy");
var groupBys = new[] { groupBy };

var regenerate = argumentParser.GetArgument("--regenerate", bool.TryParse, false);
var title = argumentParser.GetArgument("--title");
var xLabel = argumentParser.GetArgument("--xLabel");
var yLabel = argumentParser.GetArgument("--yLabel");

var plotSettings = new PlotSettings(title, xLabel, yLabel);

var experimentsFolderCopyPath = DataPreProcessor.CopyDataFolder(experimentsFolderPath, folderName: "BoxPlot", regenerate: regenerate);

var groupingFolders = Grouping.GroupScenariosByGroupingValue(groupBys, experimentsFolderCopyPath);
foreach (var groupFolder in groupingFolders)
{
    var algorithmFolders = Grouping.GroupScenariosByAlgorithm(groupFolder);
    foreach (var algorithmFolder in algorithmFolders)
    {
        SummaryAlgorithmSeedsCreator.CreateSummaryFromScenarios(algorithmFolder, regenerate: regenerate);
    }
}

using var boxPlotCreator = new BoxPlotCreator(experimentsFolderCopyPath, groupBys[0], plotSettings);
boxPlotCreator.CreateBoxPlotForAlgorithms(groupingFolders);
