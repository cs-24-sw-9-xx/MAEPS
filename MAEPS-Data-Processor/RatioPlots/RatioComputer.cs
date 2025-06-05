using MAEPS.Data.Processor.Preprocessors;

using ScottPlot;

namespace RatioPlots;

public class RatioComputer(string storeInFolderPath, IReadOnlyDictionary<string, IReadOnlyList<string>> folderStructure, string[] groupBys)
{
    private record ResultValue(double Ratio, AverageExperimentSummary AverageExperiment);
    private readonly Dictionary<string, List<ResultValue>> _resultValuesByAlgorithm = new();
    private readonly DirectoryInfo _plotsFolderPath = Directory.CreateDirectory(storeInFolderPath);

    public void GenerateRatioData()
    {
        foreach (var (algorithmFolderPath, groupedFolders) in folderStructure)
        {
            foreach (var folder in groupedFolders)
            {
                var values = folder.GroupingValues<double>(groupBys);
                var ratio = values[0] / values[1];
                
                var average = SummaryAlgorithmSeedsCreator.GetAverageOfExperimentSummary(folder);
                if (average == null)
                {
                    Console.WriteLine($"No average found for folder: {folder}");
                    continue;
                }
                
                var algorithmName = Path.GetFileName(algorithmFolderPath);
                if(!_resultValuesByAlgorithm.TryGetValue(algorithmName, out var resultValues))
                {
                    resultValues = [];
                    _resultValuesByAlgorithm[algorithmName] = resultValues;
                }
                
                resultValues.Add(new ResultValue(ratio, average));
            }
        }
    }
    
    public void CreatePlots()
    {
        var plot = new Plot();
        foreach (var (algorithm, resultValues) in _resultValuesByAlgorithm)
        {
            var sortedResultValues = resultValues.OrderBy(rv => rv.Ratio).ToList();
            var xValues = sortedResultValues.Select(rv => rv.Ratio).ToArray();
            var yValues = sortedResultValues.Select(rv => rv.AverageExperiment.AverageIdleness).ToArray();
            
            plot.AddAlgorithm(algorithm, xValues, yValues);
            PlotSeparate(algorithm, xValues, yValues);
        }
        
        var path = Path.Combine(_plotsFolderPath.FullName, "CombineRatioPlot.svg");
        plot.SavePlot(path);
    }
    
    private void PlotSeparate(string algorithmName, double[] xValues, double[] yValues)
    {
        var plot = new Plot();
        plot.AddAlgorithm(algorithmName, xValues, yValues);
        
        var path = Path.Combine(_plotsFolderPath.FullName, algorithmName + ".svg");
        plot.SavePlot(path);
    }
}