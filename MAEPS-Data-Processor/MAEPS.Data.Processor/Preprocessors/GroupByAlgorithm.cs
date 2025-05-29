namespace MAEPS.Data.Processor.Preprocessors;

public static class GroupingAlgorithm
{
    /// <summary>
    /// Assuming that the first part from begin to the first dash of the folder name is the algorithm name and that the algorithm name does not include any dash (-).
    /// </summary>
    /// <param name="experimentsFolderPath">The path to the folder that contains the ungrouped scenarios by algorithm name or some grouped scenarios with ungrouped scenarios</param>
    public static string[] GroupScenarioByAlgorithmName(string experimentsFolderPath)
    {
        var scenarios = Directory.GetDirectories(experimentsFolderPath, "*", SearchOption.TopDirectoryOnly);

        if (scenarios.AlreadyGrouped(out var algorithmDirectories))
        {
            return algorithmDirectories.ToArray();
        }
        
        scenarios = scenarios
            .Where(scenario => !Path.GetFileName(scenario).StartsWith("Plots") && !algorithmDirectories.Contains(scenario))
            .ToArray();
        
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

    private static bool AlreadyGrouped(this string[] folderPaths, out List<string> algorithmDirectories)
    {
        algorithmDirectories = [];
        var alreadyGrouped = true;
        foreach (var folderPath in folderPaths)
        {
            var folderName = Path.GetFileName(folderPath);
            if (folderName.StartsWith("Plots"))
            {
                continue;
            }

            if (folderName.Contains('-'))
            {
                alreadyGrouped = false;
            }
            else
            {
                algorithmDirectories.Add(folderPath);
            }
        }

        return alreadyGrouped;
    }
}