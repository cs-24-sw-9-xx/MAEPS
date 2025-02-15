using System.Linq;

using Maes.Map;

using Random = System.Random;

namespace Maes.PatrollingAlgorithms
{
    /// <summary>
    /// The random reactive patrolling algorithm from "Multi-Agent Movement Coordination in Patrolling", 2002
    /// </summary>
    public class RandomReactive : PatrollingAlgorithm
    {
        private readonly Random _random;

        public RandomReactive(int seed)
        {
            _random = new Random(seed);
        }

        public override string AlgorithmName => "Random Reactive Algorithm";

        protected override Vertex NextVertex(Vertex currentVertex)
        {
            var index = _random.Next(currentVertex.Neighbors.Count);
            return currentVertex.Neighbors.ElementAt(index);
        }
    }
}