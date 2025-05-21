using AlgorithmSplitter;

using MAEPS_Data_Processor;



switch (args.Length)
{
    case < 1:
        Console.WriteLine("Please provide a path to the data folder");
        return;
    case < 2:
        Console.WriteLine("Please provide a groupBy value");
        return;
}

var experimentsFolderPath = args[0];
var groupBy = args[1];
var regenerateExistingSummaries = args.Length > 2 && bool.Parse(args[2]);

if (!Path.Exists(experimentsFolderPath + ".zip"))
{
    Console.WriteLine("Zip before running the program because of moving files");
    return;
}

var arguments = ArgumentParser.ParseArguments(args);
DataPreProcessor.FlattenDirectoryStructure(experimentsFolderPath);
Grouping.GroupScenariosByGroupingValue(groupBy, experimentsFolderPath);
Grouping.GroupScenariosByAlgorithmInGroups(experimentsFolderPath);
SummaryAlgorithmSeedsCreator.CreateSummaryForAlgorithms(experimentsFolderPath, regenerateExistingSummaries);

public static class ArgumentParser
{
    public static Dictionary<string, string> ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No arguments provided.");
        }
        
        var arguments = new Dictionary<string, string>();
        
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i].TrimStart('-');
                
            }
        }
    }
}

