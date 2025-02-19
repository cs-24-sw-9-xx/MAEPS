using System.Linq;

using Maes.Map;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Heuristic Conscientious Reactive Algorithm of https://repositorio.ufpe.br/handle/123456789/2474.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    // Heuristic: The vertex with the lowest idleness and distance estimation
    /// </summary>
    public class HeuristicConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Heuristic Conscientious Reactive Algorithm";

        protected override Vertex NextVertex(Vertex currentVertex)
        {
            // Calculate the normalized idleness of the neighbors
            var idleness = currentVertex.Neighbors.Select(x => (x.Id, x.LastTimeVisitedTick));
            var minIdleness = idleness.Min(x => x.LastTimeVisitedTick);
            var maxIdleness = idleness.Max(x => x.LastTimeVisitedTick);
            var normalizedIdleness = idleness.Select(x => (id: x.Id, normalizedIdleness: (x.LastTimeVisitedTick - minIdleness) / (maxIdleness - minIdleness)));

            // Calculate the normalized distance estimation of the neighbors
            var distanceEstimation = currentVertex.Neighbors.Select(x => (id: x.Id, dist: _controller.EstimateDistanceToTarget(x.Position)));
            var maxDistanceEstimation = distanceEstimation.Max(x => x.dist);
            distanceEstimation = distanceEstimation.Select(x => (id: x.id, dist: x.dist / maxDistanceEstimation));

            if (normalizedIdleness.Count() != distanceEstimation.Count())
            {
                throw new System.Exception("Length of normalizedIdleness and distanceEstimation must be equal");
            }
            var result = from idlenessValue in normalizedIdleness
                         join distanceValue in distanceEstimation on idlenessValue.id equals distanceValue.id
                         orderby idlenessValue.normalizedIdleness + distanceValue.dist ascending
                         select (id: idlenessValue.id, value: idlenessValue.normalizedIdleness + distanceValue.dist);
            return currentVertex.Neighbors.Single(x => x.Id == result.First().id);
        }
    }
}