using System.Globalization;
using CsvHelper;
using ScottPlot;

namespace AlgorithmSplitter.Boxplots;

public record PlotSettings(string Title, string XLabel, string YLabel);

public sealed class BoxPlotCreator : IDisposable
{
    private record BoxplotStat(string AlgorithmName, int X, double Min, double Q1, double Median, double Q3, double Max);

    public BoxPlotCreator(string experimentsFolderPath, string groupBy, PlotSettings plotSettings)
    {
        _experimentsFolderPath = experimentsFolderPath;
        _groupBy = groupBy;
        _plotSettings = plotSettings;
        _boxPlotFolderPath = Path.Join(experimentsFolderPath, "Plots", "Boxplot");
        Directory.CreateDirectory(_boxPlotFolderPath);
        
        var boxPlotCsvFilePath = Path.Join(_boxPlotFolderPath, "boxplot.csv");
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        _writer = new StreamWriter(boxPlotCsvFilePath);
        _csv = new CsvWriter(_writer, config);
    }
    
    private readonly string _experimentsFolderPath;
    private readonly string _groupBy;
    private readonly string _boxPlotFolderPath;
    private readonly PlotSettings _plotSettings;
    
    private readonly StreamWriter _writer;
    private readonly CsvWriter _csv;
    
    private readonly Dictionary<string, List<Box>> _boxPlotsByAlgorithmName = new();
    
    public void Dispose()
    {
        _csv.Dispose();
        _writer.Dispose();
    }
    
    public void CreateBoxPlotForAlgorithms()
    {
        var groupByFolders = Directory.GetDirectories(_experimentsFolderPath, _groupBy + "*", SearchOption.TopDirectoryOnly);
        
        var xValues = new List<int>();
        
        foreach (var groupByFolder in groupByFolders)
        {
            var x = int.Parse(Path.GetFileName(groupByFolder).Replace(_groupBy, ""));
            xValues.Add(x);

            var algorithmFolders = Directory.GetDirectories(groupByFolder);
            foreach (var algorithmFolder in algorithmFolders)
            {
                var algorithmName = Path.GetFileName(algorithmFolder);
                var summary = SummaryAlgorithmSeedsCreator.GetSummary(algorithmFolder);

                var (min, q1, median, q3, max) = BoxplotCalculator.GetBoxplotValues(summary.Select(s => s.AverageIdleness).ToList());
                
                var boxplotStats = new BoxplotStat(algorithmName, x, min, q1, median, q3, max);
                
                _csv.WriteRecord(boxplotStats);
                _csv.NextRecord();
                
                CreateAndAddScottPlotBoxPlot(boxplotStats);
            }
        }
        
        SaveBoxPlot(xValues);
    }

    private void CreateAndAddScottPlotBoxPlot(BoxplotStat boxplotStat)
    {
        if (!_boxPlotsByAlgorithmName.TryGetValue(boxplotStat.AlgorithmName, out var list))
        {
            list = [];
            _boxPlotsByAlgorithmName[boxplotStat.AlgorithmName] = list;
        }
        
        var boxPlot = new Box
        {
            Position = boxplotStat.X,
            WhiskerMin = boxplotStat.Min,
            BoxMin = boxplotStat.Q1,
            BoxMiddle = boxplotStat.Median,
            BoxMax = boxplotStat.Q3,
            WhiskerMax = boxplotStat.Max
        };
        list.Add(boxPlot);
    }
    
    private void SaveBoxPlot(IReadOnlyList<int> xValues)
    {
        Plot plot = new();
        
        AdjustPositionOfBoxPlots();
        
        foreach (var (algorithmName, boxes) in _boxPlotsByAlgorithmName)
        {
            var boxPlots = plot.Add.Boxes(boxes);
            boxPlots.LegendText = algorithmName;
        }

        plot.ShowLegend(Alignment.UpperRight);
        plot.Title(_plotSettings.Title);
        plot.XLabel(_plotSettings.XLabel);
        plot.YLabel(_plotSettings.YLabel);
        plot.Axes.Bottom.SetTicks(xValues.Select(x => (double)x).ToArray(), xValues.Select(x => x.ToString()).ToArray());

        var boxPlotPngFilePath = Path.Join(_boxPlotFolderPath, "boxplot.png");
        plot.SavePng(boxPlotPngFilePath, 1600, 900);
    }
    
    /// <summary>
    /// This method is used to adjust the position of the box plots that are on the same position on the x-axis.
    /// So instance of they are placed on each other, they are offset by a small value.
    /// </summary>
    /// <param name="boxPlots"></param>
    private void AdjustPositionOfBoxPlots()
    {
        var boxPlotsByGroup = _boxPlotsByAlgorithmName.Values
                                                                             .SelectMany(x => x)
                                                                             .GroupBy(boxPlot => boxPlot.Position, boxPlot => boxPlot);
        foreach (var group in boxPlotsByGroup)
        {
            var boxPlots = group.ToArray();
            var n = boxPlots.Length;
            const float offset = 1.5f;
            var lowest = n / 2;
            var index = 0;
            for (var i = -lowest; index < n; i++)
            {
                if (i == 0 && n % 2 == 0)
                {
                    // Skip the middle position if there is an even number of box plots
                    continue;
                }
                
                boxPlots[index++].Position += i * offset;
            }
        }
    }
}