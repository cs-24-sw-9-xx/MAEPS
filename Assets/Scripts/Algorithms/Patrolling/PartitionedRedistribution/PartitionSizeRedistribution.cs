using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.Components.Redistribution;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.PartitionedRedistribution
{
    public class PartitionSizeRedistribution : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Partition Size Redistribution";

        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private RandomRedistributionComponent _redistributionComponent = null!;

        private readonly int _seed;
        public PartitionSizeRedistribution(int seed)
        {
            _seed = seed;
        }

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _redistributionComponent = new RandomRedistributionComponent(
                controller,
                patrollingMap.Vertices,
                this,
                seed: _seed,
                patrollingMap,
                (p) => 1f / p.Vertices.Count);

            return new IComponent[] { _goToNextVertexComponent, _redistributionComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            return ConscientiousReactiveLogic.NextVertex(currentVertex, currentVertex.Neighbors.Where(v => v.Partition == Controller.AssignedPartition).ToList());
        }
    }
}