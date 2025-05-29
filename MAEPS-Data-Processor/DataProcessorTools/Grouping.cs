using System.Text.RegularExpressions;

namespace DataProcessorTools;

public static class Grouping
{
    public static string[] GetGroupingFolders(string experimentsFolderPath, string[] groupBys)
    {
        var folderByGroupValue = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);
        
        return folderByGroupValue.Where(folder => Path.GetFileName(folder).AllReadyGrouped(groupBys)).ToArray();
    }
    
    private static string GroupingName(string[] groupBys, string[] values)
    {
        if (groupBys.Length != values.Length)
        {
            throw new ArgumentException("The number of groupBys and values must match.");
        }

        return string.Join("-", groupBys.Zip(values, (groupBy, value) => $"{groupBy}{value}"));
    }
    
    private static string GroupingNamePattern(string[] groupBys)
    {
        var pattern = string.Join("-", groupBys.Select(groupBy=> $@"{groupBy}([^-\\s]+)"));
        return $@"^{pattern}$";
    }
    
    private static bool AllReadyGrouped(this string folderPath, string[] groupBys)
    {
        var scenarioName = Path.GetFileName(folderPath);
        return Regex.Match(scenarioName, $@"^{GroupingNamePattern(groupBys)}$").Success;
    }

    /// <summary>
    /// This method groups the scenarios by the grouping value in the folder structure of the experiment folder.
    /// It returns the list of paths to grouped folders.
    /// </summary>
    /// <param name="groupBys">The grouping values.</param>
    /// <param name="scenariosFolderPath">The path to the folder, that contains the scenarios as folders that need to be grouped</param>
    public static List<string> GroupScenariosByGroupingValue(string[] groupBys, string scenariosFolderPath)
    {
        var groupedFolderPaths = new List<string>();
        var folders = Directory.GetDirectories(scenariosFolderPath);
        foreach (var folder in folders)
        {
            if (folder.AllReadyGrouped(groupBys) || Path.GetFileName(folder).StartsWith("Plots"))
            {
                groupedFolderPaths.Add(folder);
                continue;
            }
    
            var scenarioName = Path.GetFileName(folder);
            var values = groupBys.Select(groupBy =>
            {
                var match = Regex.Match(scenarioName, $@"{groupBy}([^-\s]+)");

                if (!match.Success)
                {
                    throw new ArgumentException($"GroupBy '{groupBy}' not found in scenario name '{scenarioName}'. ");
                }

                return match.Groups[1].Value;
            }).ToArray();

            var valueFolder = Directory.CreateDirectory(Path.Combine(scenariosFolderPath, GroupingName(groupBys, values)));
            var newExperimentFolder = Path.Combine(valueFolder.FullName, scenarioName);
            Directory.Move(folder, newExperimentFolder);
        }

        return groupedFolderPaths;
    }

    /// <summary>
    /// This method groups the scenarios by algorithm name in the folder structure of the groupBy.
    /// </summary>
    /// <param name="experimentsFolderPath">The path to the experiment folder.</param>
    /// <param name="groupBys">The groupBy values.</param>
    public static void GroupScenariosByAlgorithmInGroups(string experimentsFolderPath, string[] groupBys)
    {
        var folderByGroupValue = GetGroupingFolders(experimentsFolderPath, groupBys);
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