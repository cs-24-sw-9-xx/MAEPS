namespace AlgorithmSplitter.Boxplots;

public class BoxplotCalculator
{
    public static (double Min, double Q1, double Median, double Q3, double Max) GetBoxplotValues(IReadOnlyList<float> values)
    {
        if (values == null || values.Count == 0)
        {
            throw new ArgumentException("Data set cannot be null or empty");
        }

        var sorted = values.OrderBy(v => v).ToList();
        var n = sorted.Count;

        var lowerHalf = sorted.Take(n / 2).ToList();
        var upperHalf = sorted.Skip((n + 1) / 2).ToList();

        return (
            sorted.First(),
            GetMedian(lowerHalf),
            GetMedian(sorted),
            GetMedian(upperHalf),
            sorted.Last());

        double GetMedian(List<float> list)
        {
            var mid = list.Count / 2;
            return (list.Count % 2 == 0)
                ? (list[mid - 1] + list[mid]) / 2.0
                : list[mid];
        }
    }
    
    
}