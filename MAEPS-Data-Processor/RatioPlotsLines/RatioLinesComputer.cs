using MAEPS.Data.Processor.Preprocessors;
using MAEPS.Data.Processor.Utilities;

using ScottPlot;
using ScottPlot.Plottables;

public class RatioLinesComputer(
    string storeInFolderPath,
    string[] groupBys,
    string mapType)
{

    private readonly Dictionary<int, Plot> _globalWorstIdlenessPlots = new();
    private double _maxGlobalWorstIdleness = 0;
    private int _maxGlobalRobotCount = 0;

    public void GenerateRatioData(string algorithmFolderPath, IEnumerable<string> groupedFolders)
    {
        var algorithmName = Path.GetFileName(algorithmFolderPath);

        var dataByMapSize = new Dictionary<int, (List<(int, double)> average, List<(int, double)> worst)>();

        foreach (var folder in groupedFolders)
        {
            var values = folder.GroupingValues<int>(groupBys);
            var mapSize = values[0];
            var robotCount = values[1];

            var average = SummaryAlgorithmSeedsCreator.GetAverageOfExperimentSummary(folder);
            if (average == null)
            {
                Console.WriteLine($"No average found for folder: {folder}");
                continue;
            }

            if (!dataByMapSize.ContainsKey(mapSize))
            {
                dataByMapSize[mapSize] = (
                    [],
                    []
                );
            }

            dataByMapSize[mapSize].average.Add((robotCount, average.AverageIdleness));
            dataByMapSize[mapSize].worst.Add((robotCount, average.WorstIdleness));
        }

        Multiplot multiplot = new();
        multiplot.AddPlots(dataByMapSize.Keys.Count);

        var maxIdleness = dataByMapSize.Values
            .SelectMany(d => d.average.Select(x => x.Item2).Concat(d.worst.Select(x => x.Item2)))
            .Max();

        var maxRobotCount = dataByMapSize.Values
            .SelectMany(d => d.average.Select(x => x.Item1).Concat(d.worst.Select(x => x.Item1)))
            .Max();
        var i = 0;

        foreach (var (mapSize, (average, worst)) in dataByMapSize.OrderBy(d => d.Key))
        {
            var plot = multiplot.Subplots.GetPlot(i);
            i++;

            var sortedAverage = average.OrderBy(x => x.Item1).ToList();
            plot.AddAlgorithmLinePlot("Average idleness", sortedAverage.Select(x => (double)x.Item1).ToArray(),
                sortedAverage.Select(x => x.Item2).ToArray(), LinePattern.Solid);

            var sortedWorst = worst.OrderBy(x => x.Item1).ToList();
            plot.AddAlgorithmLinePlot("Worst idleness", sortedWorst.Select(x => (double)x.Item1).ToArray(),
                sortedWorst.Select(x => x.Item2).ToArray(), LinePattern.Dashed);

            if (!_globalWorstIdlenessPlots.TryGetValue(mapSize, out var globalWorstIdlenessPlot))
            {
                globalWorstIdlenessPlot = new Plot();
                _globalWorstIdlenessPlots[mapSize] = globalWorstIdlenessPlot;
            }

            globalWorstIdlenessPlot.AddAlgorithmLinePlot(algorithmName,
                sortedWorst.Select(x => (double)x.Item1).ToArray(), sortedWorst.Select(x => x.Item2).ToArray(),
                LinePattern.Solid);

            _maxGlobalWorstIdleness = Math.Max(_maxGlobalWorstIdleness, sortedWorst.Select(x => x.Item2).Max());
            _maxGlobalRobotCount = Math.Max(_maxGlobalRobotCount, maxRobotCount);

            plot.Axes.SetLimits(0, maxRobotCount + 1, 0, maxIdleness + 1000);
            plot.YLabel("Ticks");
            plot.XLabel("Robot count");
            plot.Title($"{algorithmName} - {mapType} - {mapSize}");
            plot.Legend.IsVisible = false;
            
            var dataList = new Dictionary<double, List<(string, double)>>();
            foreach (var (robotCount, idleness) in sortedWorst)
            {
                if (!dataList.TryGetValue(robotCount, out var yValuesAlg))
                {
                    yValuesAlg = new List<(string, double)>();
                    dataList[robotCount] = yValuesAlg;
                }

                yValuesAlg.Add((algorithmName, idleness));
            }
            WriteData(dataList, $"{algorithmName}_{mapType}_{mapSize}");
        }

        multiplot.Layout = new ScottPlot.MultiplotLayouts.Columns();
        var fileName = $"{algorithmName}_{mapType}.png";
        var filePath = Path.Combine(storeInFolderPath, fileName);
        multiplot.SavePng(filePath, 700 * dataByMapSize.Keys.Count, 600);
    }

    public void SaveGlobalWorstIdlenessPlots()
    {
        foreach (var (mapSize, plot) in _globalWorstIdlenessPlots)
        {
            var dataList = new Dictionary<double, List<(string, double)>>();
            double[]? xValues = null;
            var algs = plot.PlottableList.OfType<Scatter>().ToArray();
            var maxWorstIdleness = algs.Max(x => x.Data.GetScatterPoints().Max(p => p.Y));

            foreach (var alg in algs)
            {
                var source = alg.Data.GetScatterPoints();
                foreach (var coordinate in source)
                {
                    if (!dataList.TryGetValue(coordinate.X, out var yValuesAlg))
                    {
                        yValuesAlg = new List<(string, double)>();
                        dataList[coordinate.X] = yValuesAlg;
                    }

                    yValuesAlg.Add((alg.LegendText, coordinate.Y));
                }

                xValues ??= source.Select(p => p.X).ToArray();
            }

            plot.Axes.SetLimits(0, _maxGlobalRobotCount + 1, 0, maxWorstIdleness + 1000);
            plot.YLabel("Ticks");
            plot.XLabel("Robot count");
            plot.Title($"Worst Idleness - {mapSize}");
            plot.ShowLegend(Alignment.UpperRight);
            var fileName = $"AllWorstIdleness_{mapType}_{mapSize}.png";
            var filePath = Path.Combine(storeInFolderPath, fileName);
            plot.SavePng(filePath, 1600, 900);
            Console.WriteLine($"Saved global worst idleness plot to {filePath}");
            WriteData(dataList, $"AllWorstIdleness_{mapType}_{mapSize}");
        }
    }

    private void WriteData(Dictionary<double, List<(string, double)>> dataList, string name, char delimiter = ',')
    {
        var filePath = Path.Combine(storeInFolderPath, name + ".csv");
        using var writer = new StreamWriter(filePath);
        var algorithmNames = dataList.Values
            .SelectMany(v => v.Select(x => x.Item1))
            .Distinct()
            .OrderBy(algName => algName);

        writer.WriteLine("RobotCount" + delimiter + string.Join(delimiter.ToString(), algorithmNames));

        foreach (var (robotCount, yValuesAlg) in dataList.OrderBy(v => v.Key))
        {
            writer.Write(robotCount);
            writer.Write(delimiter);

            var yValues = yValuesAlg.OrderBy(x => x.Item1).Select(x => x.Item2.ToString());
            writer.Write(string.Join(delimiter.ToString(), yValues));

            writer.WriteLine("");
        }
    }
}