using System.Reflection;

namespace MAEPS_Data_Processor;

internal class Program
{
    static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(@"..\..\..\..\..\data");
        foreach (var experimentDirectory in Directory.GetDirectories(Directory.GetCurrentDirectory()))
        {
            Console.WriteLine(experimentDirectory);
            foreach (var scenarioDirectory in Directory.GetDirectories(experimentDirectory))
            {
                Console.WriteLine(scenarioDirectory);
                if (File.Exists(Path.Combine(scenarioDirectory, "*.png")))
                {
                    Console.WriteLine("Graphs already created. Skipping");
                    continue;
                }
                var data = CsvDataReader.ReadPatrollingCsv(Path.Combine(scenarioDirectory, "patrolling.csv"));
                
                var dataDict = new Dictionary<string, List<dynamic>>()
                {
                    {"Tick", new List<dynamic>()},
                    {"WorstGraphIdleness", new List<dynamic>()},
                    {"AverageGraphIdleness", new List<dynamic>()}
                };
                
                foreach (var item in data)
                {
                    dataDict["Tick"].Add(item.Tick);
                    dataDict["WorstGraphIdleness"].Add(item.WorstGraphIdleness);
                    dataDict["AverageGraphIdleness"].Add(item.AverageGraphIdleness);
                }
                

                ScottPlot.Plot myPlot = new();
                myPlot.Add.Scatter(dataDict["Tick"], dataDict["WorstGraphIdleness"]);

                myPlot.Title("Worst Graph Idleness");
                myPlot.XLabel("Tick");
                myPlot.YLabel("Worst Graph Idleness");
                

                myPlot.Save(Path.Combine(scenarioDirectory, "test.pdf"), 1200, 600);
            }
        }
    }
}