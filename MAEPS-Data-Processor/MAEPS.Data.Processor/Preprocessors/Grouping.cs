using System.Text.RegularExpressions;

namespace MAEPS.Data.Processor.Preprocessors;

public static class Grouping
{
    private static string[] GetGroupingFolders(string experimentsFolderPath, string[] groupBys)
    {
        var folderByGroupValue = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);
        
        return folderByGroupValue.Where(folder => Path.GetFileName(folder).AllReadyGrouped(groupBys)).ToArray();
    }
    
    private static string GroupingName<T>(string[] groupBys, T[] values)
    {
        if (groupBys.Length != values.Length)
        {
            throw new ArgumentException("The number of groupBys and values must match.");
        }

        return string.Join("-", groupBys.Zip(values, (groupBy, value) => $"{groupBy}{value}"));
    }

    public static T[] GroupingValues<T>(this string name, IEnumerable<string> groupBys)
    {
        var values = groupBys.Select(groupBy =>
        {
            var match = Regex.Match(name, $@"{groupBy}([^-\s]+)");

            if (!match.Success)
            {
                throw new ArgumentException($"GroupBy '{groupBy}' not found in '{name}'.");
            }

            return match.Groups[1].Value;
        }).ToArray();
        
        return values.Select(value => (T)Convert.ChangeType(value, typeof(T))).ToArray();
    }
    
    private static string GroupingNamePattern(IEnumerable<string> groupBys)
    {
        var pattern = string.Join("-", groupBys.Select(groupBy=> $@"{groupBy}([^-\\s]+)"));
        return $@"^{pattern}$";
    }
    
    private static bool AllReadyGrouped(this string folderPath, IEnumerable<string> groupBys)
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
            if (Path.GetFileName(folder).StartsWith("Plots"))
            {
                continue;
            }
            
            if (folder.AllReadyGrouped(groupBys))
            {
                groupedFolderPaths.Add(folder);
                continue;
            }
    
            var scenarioName = Path.GetFileName(folder);
            var values = scenarioName.GroupingValues<string>(groupBys);

            var groupedFolderPath = Path.Combine(scenariosFolderPath, GroupingName(groupBys, values));
            if (!Directory.Exists(groupedFolderPath))
            {
                Directory.CreateDirectory(groupedFolderPath);
                groupedFolderPaths.Add(groupedFolderPath);
            }
            var newExperimentFolder = Path.Combine(groupedFolderPath, scenarioName);
            Directory.Move(folder, newExperimentFolder);
        }
        return groupedFolderPaths;
    }

    /// <summary>
    /// This method groups the scenarios by algorithm name.
    /// </summary>
    /// <param name="folderGroupValue">The path to the experiment folder.</param>
    public static List<string> GroupScenariosByAlgorithm(string folderGroupValue)
    {
        var scenarioFolders = Directory.GetDirectories(folderGroupValue);
        var algorithms = new List<string>();
        var algorithmFolders = new List<string>();
        
        foreach (var scenarioFolder in scenarioFolders)
        {
            var algorithmName = Path.GetFileName(scenarioFolder).Split('-')[0];
            if (algorithmName == Path.GetFileName(scenarioFolder))
            {
                // The folder is already grouped by algorithm. Assuming that the algorithm name does not contains any hyphen.
                continue;
            }

            var algorithmFolder = Path.Combine(folderGroupValue, algorithmName);
            
            if (!algorithms.Contains(algorithmName))
            {
                Directory.CreateDirectory(algorithmFolder);
                algorithms.Add(algorithmName);
                algorithmFolders.Add(algorithmFolder);
            }

            var newScenarioFolder = Path.Combine(algorithmFolder, Path.GetFileName(scenarioFolder));
            Directory.Move(scenarioFolder, newScenarioFolder);
        }

        return algorithmFolders;
    }
}