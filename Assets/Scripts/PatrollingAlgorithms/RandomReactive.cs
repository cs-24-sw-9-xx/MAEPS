using System.Linq;

using Maes.Map;

namespace Maes.PatrollingAlgorithms
{
    /// <summary>
    /// The random reactive patrolling algorithm from "Multi-Agent Movement Coordination in Patrolling", 2002
    /// </summary>
    public class RandomReactive : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Random Reactive Algorithm";

        protected override Vertex NextVertex(Vertex currentVertex)
        {
            var index = UnityEngine.Random.Range(0, currentVertex.Neighbors.Count);
            return currentVertex.Neighbors.ElementAt(index);
        }
    }
}