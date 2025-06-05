using System.Globalization;

using MAEPS.Data.Processor.Preprocessors;

using ScottPlot;

namespace RatioPlotHeatmap;

public class RatioHeatmapComputer(
    string storeInFolderPath,
    string[] groupBys,
    Func<AverageExperimentSummary, double> propertyToShowFunc,
    string propertyToShowName)
{
    public void GenerateRatioData(string algorithmFolderPath, IEnumerable<string> groupedFolders)
    {
        
        
        var dataset = new List<(double mapSize, double robotCount, double value)>();
        
        var mapSizes = new HashSet<double>();
        var robotCounts = new HashSet<double>();
        
        var algorithmName = Path.GetFileName(algorithmFolderPath);
        
        foreach (var folder in groupedFolders)
        {
            var values = folder.GroupingValues<double>(groupBys);
            var mapSize = values[0];
            var robotCount = values[1];
            
            var average = SummaryAlgorithmSeedsCreator.GetAverageOfExperimentSummary(folder);
            if (average == null)
            {
                Console.WriteLine($"No average found for folder: {folder}");
                continue;
            }

            mapSizes.Add(mapSize);
            robotCounts.Add(robotCount);
            dataset.Add((mapSize, robotCount, (propertyToShowFunc(average))));
        }
        
        /*var cs = new Coordinates3d[(int)(robotCounts.Max()) + 1, (int)(mapSizes.Max()) + 1]; 

        
        foreach (var (groupByMapSize, index1) in dataset.GroupBy(data => data.mapSize).Select((groupByMapSize, index) => (groupByMapSize, index)))
        {
            var mapSize = groupByMapSize.Key;
            foreach (var (groupByRobotCount, index2) in groupByMapSize.GroupBy(data => (data.robotCount)).Select((groupByRobotCount, index) => (groupByRobotCount, index)))
            {
                var robotCount = groupByRobotCount.Key;
                var value = groupByRobotCount.Select(data => data.value).First();
                cs[(int)robotCount, (int)mapSize] = new(mapSize, robotCount, value);
            }
        }*/
        
        Plot myPlot = new();
        
        var cs = new double[robotCounts.Count, mapSizes.Count];
        foreach (var (groupByMapSize, index1) in dataset.GroupBy(data => data.mapSize).OrderBy(groupBy => groupBy.Key).Select((groupByMapSize, index) => (groupByMapSize, index)))
        {
            var mapSize = groupByMapSize.Key;
            foreach (var (groupByRobotCount, index2) in groupByMapSize.GroupBy(data => (data.robotCount)).OrderBy(groupBy => groupBy.Key).Select((groupByRobotCount, index) => (groupByRobotCount, index)))
            {
                var robotCount = groupByRobotCount.Key;
                var value = groupByRobotCount.Select(data => data.value).First();
                cs[index2, index1] = (int)value;
            }
        }
        
        var heatmap = myPlot.Add.Heatmap(cs);

        var mapSizesOrdered = mapSizes.OrderBy(x => x).Select((x, i) => (i, x)).ToArray();
        var robotCountsOrdered = robotCounts.OrderBy(x => x).Select((x, i) => (i, x)).ToArray();
        myPlot.Axes.Bottom.SetTicks(mapSizesOrdered.Select(v => (double)v.i).ToArray(), mapSizesOrdered.Select(v => v.x.ToString(CultureInfo.InvariantCulture)).ToArray());
        myPlot.Axes.Left.SetTicks(robotCountsOrdered.Select(v => (double)v.i).ToArray(), robotCountsOrdered.Select(v => v.x.ToString(CultureInfo.InvariantCulture)).ToArray());
        
        myPlot.XLabel("Map size");
        myPlot.YLabel("Robot count");
        myPlot.Title($"{algorithmName} - {propertyToShowName}", 10);
        
        for (int y = 0; y < cs.GetLength(0); y++)
        {
            for (int x = 0; x < cs.GetLength(1); x++)
            {
                Coordinates coordinates = new(x, y);
                string cellLabel = cs[y, x].ToString("0.0");
                var text = myPlot.Add.Text(cellLabel, coordinates);
                text.Alignment = Alignment.MiddleCenter;
                text.LabelFontSize = 10;
                text.LabelFontColor = Colors.White;
            }
        }

        myPlot.Add.ColorBar(heatmap);

        var fileName = $"{algorithmName}_{propertyToShowName}_heatmap.svg";
        var filePath = Path.Combine(storeInFolderPath, fileName);
        
        myPlot.SaveSvg(filePath, 400, 300);
    }
}