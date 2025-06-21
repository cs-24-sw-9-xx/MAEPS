using System.Diagnostics;

using Maes.Statistics.Snapshots;

namespace MAEPS.Data.Processor;

public static class CsvDataReader
{
    public static List<PatrollingSnapshot> ReadPatrollingCsv(string path)
    {
        var patrollingSnapshots = new List<PatrollingSnapshot>();
        using var reader = new StreamReader(path);
        reader.ReadLine(); // Ignore the header
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
            {
                return patrollingSnapshots;
            }

            var columnValues = line.Split(';');
            
            if (columnValues.Length == 11)
            {
                columnValues = [columnValues[0], ..columnValues.Skip(3)]; // Skip the first 3 columns
            }
            
            var patrollingSnapshot = new PatrollingSnapshot();
            var lastBits = patrollingSnapshot.ReadRow(columnValues);
            Debug.Assert(lastBits.Length == 0);
            patrollingSnapshots.Add(patrollingSnapshot);
        }
    }
}