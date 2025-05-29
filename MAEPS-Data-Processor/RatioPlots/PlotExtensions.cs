using ScottPlot;

namespace RatioPlots;

public static class PlotExtensions
{
    public static void AddAlgorithm(this Plot plot, string algorithmName, double[] xValues, double[] yValues)
    {
        var algScatter = plot.Add.Scatter(
            xValues, 
            yValues
        );
        algScatter.LegendText = algorithmName;
    }
    
    public static void SavePlot(this Plot plot, string fileNameWithExtension)
    {
        plot.ShowLegend(Alignment.UpperLeft);
        plot.Axes.Margins(bottom: 0);
        plot.SaveSvg(fileNameWithExtension, 1600, 900);
    }
}