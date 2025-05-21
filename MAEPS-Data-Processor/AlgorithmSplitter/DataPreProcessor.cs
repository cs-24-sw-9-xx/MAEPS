namespace AlgorithmSplitter;

internal static class DataPreProcessor
{
    public static void FlattenDirectoryStructure(string experimentsFolderPath)
    {
        var directoryInfo = Directory.GetDirectories(experimentsFolderPath, "experiment-*", SearchOption.TopDirectoryOnly);

        // flatten the directory structure, so that all scenarios are in the same folder
        foreach (var scenariosFolder in directoryInfo)
        {
            var pathScenariosFolder = Path.Combine(experimentsFolderPath, scenariosFolder);
            var scenarios = Directory.GetDirectories(pathScenariosFolder)
                .Select(Path.GetFileName).ToArray();
            foreach (var scenario in scenarios)
            {
                var pathScenarioFolder = Path.Combine(pathScenariosFolder, scenario!);
                var newPathScenarioFolder = Path.Combine(experimentsFolderPath, scenario!);
                Directory.Move(pathScenarioFolder, newPathScenarioFolder);
            }
        }

        // Remove the experiment folders
        foreach (var experimentFolder in Directory.GetDirectories(experimentsFolderPath, "experiment-*", SearchOption.TopDirectoryOnly))
        {
            Directory.Delete(experimentFolder, true);
        }
    }
}