using MAEPS.Data.Processor;
using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using ScottPlot;

namespace RatioPlotsDebugger;

public class RatioDebuggerComputer(
    string storeInFolderPath,
    IReadOnlyDictionary<string, IReadOnlyList<string>> folderStructure,
    string[] groupBys,
    Func<ExperimentSummary, double> propertyToShowFunc,
    string propertyToShowName)
{
    private record ResultValue(double Ratio, double Value);
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

                ExperimentSummary[] summaries;
                try
                {
                    summaries = SummaryAlgorithmSeedsCreator.GetSummary(folder);
                }
                catch (Exception _)
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

                foreach (var summary in summaries)
                {
                    resultValues.Add(new ResultValue(ratio, propertyToShowFunc(summary)));
                }
            }
        }
    }
    
    public void CreatePlots()
    {
        var plot = new Plot();
        foreach (var (algorithm, resultValues) in _resultValuesByAlgorithm)
        {
            var xValues = resultValues.Select(rv => rv.Ratio).ToArray();
            var yValues = resultValues.Select(rv => rv.Value).ToArray();
            
            plot.AddAlgorithmPointPlot(algorithm, xValues, yValues);
            PlotSeparate(algorithm, xValues, yValues);
        }
        
        var path = Path.Combine(_plotsFolderPath.FullName, propertyToShowName + "CombineRatioDebuggerPlot.svg");
        plot.SavePlot(path);
    }
    
    private void PlotSeparate(string algorithmName, double[] xValues, double[] yValues)
    {
        var plot = new Plot();
        plot.AddAlgorithmPointPlot(algorithmName, xValues, yValues);
        
        var path = Path.Combine(_plotsFolderPath.FullName, propertyToShowName + algorithmName + ".svg");
        plot.SavePlot(path);
    }
}