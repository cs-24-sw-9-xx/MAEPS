using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Heuristic Conscientious Reactive Algorithm of https://repositorio.ufpe.br/handle/123456789/2474.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// </summary>
    public class HeuristicConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Heuristic Conscientious Reactive Algorithm";

        protected override Vertex NextVertex(Vertex currentVertex)
        {
            var idleness = currentVertex.Neighbors.Select(x => x.LastTimeVisitedTick).ToArray();
            var minIdleness = idleness.Min();
            var maxIdleness = idleness.Max();
            var normalizedIdleness = idleness.Select(x => (x - minIdleness) / (maxIdleness - minIdleness)).ToArray();

            var distanceEstimation = currentVertex.Neighbors.Select(x => _controller.EstimateDistanceToTarget(x.Position)).ToArray();
            var maxDistanceEstimation = distanceEstimation.Max();
            return currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }
}