using System;
using System.Linq;

using Maes.Map;

namespace Maes.Utilities
{
    public static class PatrollingMapExtensions
    {
        public static double SquaredDistanceBetweenVertices(this PatrollingMap patrollingMap, int vertex1Id, int vertex2Id)
        {
            return patrollingMap.Paths.TryGetValue((vertex1Id, vertex2Id), out var path)
                ? path.Sum(pathStep => Math.Pow(pathStep.End.x - pathStep.Start.x, 2) +
                                       Math.Pow(pathStep.End.y - pathStep.Start.y, 2))
                : double.MaxValue;
        }
    }
}