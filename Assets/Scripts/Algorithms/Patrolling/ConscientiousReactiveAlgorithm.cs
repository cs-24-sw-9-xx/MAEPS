using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Original implementation of the Conscientious Reactive Algorithm of https://doi.org/10.1007/3-540-36483-8_11.
    /// Pseudocode can be found in another paper: https://doi.org/10.1080/01691864.2013.763722
    /// </summary>
    public sealed class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        private readonly bool _useBuiltinCollisionAvoidance;
        public override string AlgorithmName => "Conscientious Reactive Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryTradeInfoComponent _collisionRecoveryComponent = null!;

        public ConscientiousReactiveAlgorithm(bool useBuiltinCollisionAvoidance = false)
        {
            _useBuiltinCollisionAvoidance = useBuiltinCollisionAvoidance;
        }

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            if (!_useBuiltinCollisionAvoidance)
            {
                _collisionRecoveryComponent = new CollisionRecoveryTradeInfoComponent(controller, this, patrollingMap, _goToNextVertexComponent);
                return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
            }

            return new IComponent[] { _goToNextVertexComponent };
        }

        private static Vertex NextVertex(Vertex currentVertex)
        {
            return ConscientiousReactiveLogic.NextVertex(currentVertex);
        }
    }
}