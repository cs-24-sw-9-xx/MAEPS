using System.Collections.Generic;
using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    public abstract class ConscientiousReactiveLogic
    {
        public static Vertex NextVertex(Vertex currentVertex,  IReadOnlyCollection<Vertex> neighbors)
        {
            // If the current vertex has no neighbors, return it
            return currentVertex.Neighbors.Count == 0 ? currentVertex : neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}