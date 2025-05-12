namespace MAEPS_Data_Processor;

public class ExperimentSummary
{
    public string Algorithm { get; set; }
    
    [CsvHelper.Configuration.Attributes.Format("F2")]
    public float AverageIdleness { get; set; }
    
    public int WorstIdleness { get; set; }
    
    [CsvHelper.Configuration.Attributes.Format("F2")]
    public float TotalDistanceTraveled { get; set; }

}