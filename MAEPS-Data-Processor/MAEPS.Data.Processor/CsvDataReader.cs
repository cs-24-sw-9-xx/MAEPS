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
            var patrollingSnapshot = new PatrollingSnapshot();
            var lastBits = patrollingSnapshot.ReadRow(columnValues);
            Debug.Assert(lastBits.Length == 0);
            patrollingSnapshots.Add(patrollingSnapshot);
        }
    }
}