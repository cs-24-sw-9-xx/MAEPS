using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    public abstract class ConscientiousReactiveLogic
    {
        public static Vertex NextVertex(Vertex currentVertex)
        {
            // If the current vertex has no neighbors, return it
            return currentVertex.Neighbors.Count == 0 ? currentVertex : currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}