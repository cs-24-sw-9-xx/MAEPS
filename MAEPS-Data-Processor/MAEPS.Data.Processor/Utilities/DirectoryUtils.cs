using System.Text.RegularExpressions;

namespace MAEPS.Data.Processor.Utilities;

public static class DirectoryUtils
{
    public static void SetDefaultDataDirectory()
    {
        // Change directory to the data folder
        var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
        do
        {
            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null)
            {
                throw new Exception("Could not find data folder");
            }
        } while (directoryInfo.Name != "MAEPS");
        
        Console.WriteLine("Found data directory {0}", directoryInfo.FullName + "/data");
        Directory.SetCurrentDirectory(directoryInfo.FullName + "/data");
    }
    
    public static void GroupScenarios(string groupBy, string experimentsFolderPath)
    {
        var scenariosByGroupValue = Directory.GetDirectories(experimentsFolderPath);
        foreach (var scenarioFolder in scenariosByGroupValue)
        {
            if (Path.GetFileName(scenarioFolder).StartsWith(groupBy))
            {
                Console.WriteLine($"Experiment: {experimentsFolderPath} has already been grouped by {groupBy}. Skipping.");
                break;
            }
    
            var scenarioName = Path.GetFileName(scenarioFolder);
            var match = Regex.Match(scenarioName, $@"(?:^|-){groupBy}-([^-\s]+)");
    
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
    public static void GroupScenariosByAlgorithm(string experimentsFolderPath, string groupBy)
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
                    Console.WriteLine($"Experiment: {scenarioFolder} has already been grouped by Algorithm. Skipping.");
                    break;
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