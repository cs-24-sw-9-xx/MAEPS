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

        protected override Vertex NextVertex()
        {
            return TargetVertex.Neighbors.ElementAt(UnityEngine.Random.Range(0, TargetVertex.Neighbors.Count));
        }
    }
}