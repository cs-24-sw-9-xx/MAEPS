namespace MAEPS.Data.Processor;

public class ExperimentSummary
{
    public string Algorithm { get; set; }
    
    public float AverageIdleness { get; set; }
    
    public int WorstIdleness { get; set; }
    
    public float TotalDistanceTraveled { get; set; }
    
    public int TotalCycles { get; set; }
    
    public int NumberOfRobotsStart { get; set; }

    public int NumberOfRobotsEnd { get; set; }

}