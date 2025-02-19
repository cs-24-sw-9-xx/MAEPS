using System.Linq;

using Maes.Map;

using Random = System.Random;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Conscientious Reactive Algorithm of https://doi.org/10.1007/3-540-36483-8_11.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
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