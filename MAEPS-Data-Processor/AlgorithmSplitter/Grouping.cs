using System.Text.RegularExpressions;

namespace AlgorithmSplitter;

internal static class Grouping
{
    /// <summary>
    /// This method groups the scenarios by the grouping value in the folder structure of the experiment folder.
    /// </summary>
    /// <param name="groupBy">The grouping value.</param>
    /// <param name="experimentsFolderPath">The path to the experiment folder.</param>
    public static void GroupScenariosByGroupingValue(string groupBy, string experimentsFolderPath)
    {
        var scenariosByGroupValue = Directory.GetDirectories(experimentsFolderPath);
        foreach (var scenarioFolder in scenariosByGroupValue)
        {
            if (Path.GetFileName(scenarioFolder).StartsWith(groupBy) || Path.GetFileName(scenarioFolder).StartsWith("Plots"))
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

    /// <summary>
    /// This method groups the scenarios by algorithm name in the folder structure of the groupBy.
    /// </summary>
    /// <param name="experimentsFolderPath">The path to the experiment folder.</param>
    /// <param name="groupBy">The groupBy value.</param>
    public static void GroupScenariosByAlgorithmInGroups(string experimentsFolderPath, string groupBy)
    {
        var folderByGroupValue = Directory.GetDirectories(experimentsFolderPath, groupBy + "*", SearchOption.TopDirectoryOnly);
        foreach (var folderGroupValue in folderByGroupValue)
        {
            var scenarioFolders = Directory.GetDirectories(folderGroupValue);
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