using ScottPlot;

namespace MAEPS.Data.Processor.Utilities;

public static class PlotExtensions
{
    public static void AddAlgorithmLinePlot(this Plot plot, string algorithmName, double[] xValues, double[] yValues, LinePattern? linePattern = null)
    {
        var algScatter = plot.Add.Scatter(
            xValues, 
            yValues
        );
        algScatter.LegendText = algorithmName;
        algScatter.LinePattern = linePattern ?? LinePattern.Solid;
    }
    
    public static void AddAlgorithmPointPlot(this Plot plot, string algorithmName, double[] xValues, double[] yValues)
    {
        var algScatter = plot.Add.Scatter(
            xValues, 
            yValues
        );
        algScatter.LineWidth = 0;
        algScatter.MarkerSize = 10;
        algScatter.LegendText = algorithmName;
    }
    
    public static void SavePlot(this Plot plot, string fileNameWithExtension)
    {
        plot.ShowLegend(Alignment.UpperLeft);
        plot.Axes.Margins(bottom: 0);
        plot.SaveSvg(fileNameWithExtension, 1600, 900);
    }
}