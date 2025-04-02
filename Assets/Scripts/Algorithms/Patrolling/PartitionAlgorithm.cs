using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    public class PartitionAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Test Partitioning Algorithm";
        
        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private RedistributionComponent _redistributionComponent = null!;

        protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            _redistributionComponent = new RedistributionComponent(patrollingMap.Partitions, controller);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent, _redistributionComponent };
        }
        
        private static Vertex NextVertex(Vertex currentVertex)
        {
            return currentVertex.Neighbors.OrderBy(x => x.LastTimeVisitedTick).First();
        }
    }

}