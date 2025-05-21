using System.Text.RegularExpressions;

namespace AlgorithmSplitter;

internal static class Grouping
{
    public static void GroupScenariosByGroupingValue(string groupBy, string experimentsFolderPath)
    {
        var scenariosByGroupValue = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var scenarioFolder in scenariosByGroupValue)
        {
            if (Path.GetFileName(scenarioFolder).StartsWith(groupBy))
            {
                continue;
            }
    
            var scenarioName = Path.GetFileName(scenarioFolder);
            var match = Regex.Match(scenarioName, $@"{groupBy}([^-\s]+)");
    
            if (!match.Success)
            {
                Console.WriteLine($"No match for {scenarioName}");
                continue;
            }

            var value = match.Groups[1].Value;

            var valueFolder = Directory.CreateDirectory(Path.Combine(experimentsFolderPath, groupBy + value));
            var newExperimentFolder = Path.Combine(valueFolder.FullName, scenarioName);
            Directory.Move(scenarioFolder, newExperimentFolder);
        }
    }

    public static void GroupScenariosByAlgorithmInGroups(string experimentsFolderPath)
    {
        var folderByGroupValue = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var folderGroupValue in folderByGroupValue)
        {
            var scenarioFolders = Directory.GetDirectories(folderGroupValue, "*", SearchOption.TopDirectoryOnly);
            var algorithms = new HashSet<string>();

            foreach (var scenarioFolder in scenarioFolders)
            {
                var algorithmName = Path.GetFileName(scenarioFolder).Split('-')[0];
                if (algorithmName == Path.GetFileName(scenarioFolder))
                {
                    // The folder is already grouped by algorithm
                    continue;
                }
                
                if (!algorithms.Contains(algorithmName))
                {
                    Directory.CreateDirectory(Path.Combine(folderGroupValue, algorithmName));
                    algorithms.Add(algorithmName);
                }

                var newScenarioFolder = Path.Combine(folderGroupValue, algorithmName, Path.GetFileName(scenarioFolder));
                Directory.Move(scenarioFolder, newScenarioFolder);
            }
        }
    }
}