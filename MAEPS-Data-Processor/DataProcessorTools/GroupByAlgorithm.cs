using System.Diagnostics.CodeAnalysis;

namespace DataProcessorTools;

public static class GroupingAlgorithm
{
    /// <summary>
    /// Assuming that the first part from begin to the first dash of the folder name is the algorithm name.
    /// </summary>
    /// <param name="experimentsFolderPath"></param>
    /// <param name="regenerate"></param>
    public static string[] GroupScenarioByAlgorithmName(string experimentsFolderPath, bool regenerate = true)
    {
        var scenarios = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);

        if (!regenerate && scenarios.AlreadyGrouped(out var algorithmDirectories))
        {
            return algorithmDirectories.ToArray();
        }
        
        algorithmDirectories = [];
        foreach (var scenario in scenarios)
        {
            var scenarioName = Path.GetFileName(scenario);
            var algorithmName = scenarioName.Split('-')[0];
            var algorithmDirectory = Path.Combine(experimentsFolderPath, algorithmName);

            if (!Directory.Exists(algorithmDirectory))
            {
                Directory.CreateDirectory(algorithmDirectory);
                algorithmDirectories.Add(algorithmDirectory);
            }

            var destinationPath = Path.Combine(algorithmDirectory, scenarioName);
            if (!Directory.Exists(destinationPath))
            {
                Directory.Move(scenario, destinationPath);
            }
        }
        
        return algorithmDirectories.ToArray();
    }

    private static bool AlreadyGrouped(this string[] folderPaths, [NotNullWhen(true)] out List<string>? algorithmDirectories)
    {
        algorithmDirectories = []; 
        foreach (var folderPath in folderPaths)
        {
            var folderName = Path.GetFileName(folderPath);
            if (folderName.StartsWith("Plots"))
            {
                continue;
            }

            if (folderName.Contains('-'))
            {
                algorithmDirectories.Add(folderPath);
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}