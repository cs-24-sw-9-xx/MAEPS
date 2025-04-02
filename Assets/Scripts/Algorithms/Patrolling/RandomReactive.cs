using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using Random = System.Random;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Conscientious Reactive Algorithm of https://doi.org/10.1007/3-540-36483-8_11.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// </summary>
    public sealed class RandomReactive : PatrollingAlgorithm
    {
        private readonly Random _random;

        public RandomReactive(int seed)
        {
            _random = new Random(seed);
        }

        public override string AlgorithmName => "Random Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            var index = _random.Next(currentVertex.Neighbors.Count);
            return currentVertex.Neighbors.ElementAt(index);
        }
    }
}