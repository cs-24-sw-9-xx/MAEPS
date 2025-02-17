using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    public class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Conscientious Reactive Algorithm";

        protected override Vertex NextVertex(Vertex currentVertex)
        {
            return currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}