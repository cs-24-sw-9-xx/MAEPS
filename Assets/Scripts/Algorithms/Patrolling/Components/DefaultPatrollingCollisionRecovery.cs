using System.Collections.Generic;

using Maes.Algorithms.Components;
using Maes.Robot;
using Maes.Robot.Task;

namespace Maes.Algorithms.Patrolling.Components
{
    public class DefaultPatrollingCollisionRecovery : ICollisionRecovery<PatrollingAlgorithm>
    {
        private Robot2DController _controller = null!;
        private PatrollingAlgorithm _algorithm = null!;

        public void SetAlgorithm(PatrollingAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public IEnumerable<WaitForCondition> CheckAndRecoverFromCollision()
        {
            if (!_controller.IsCurrentlyColliding)
            {
                yield break;
            }

            _controller.StopCurrentTask();
            yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

            _controller.Move(1.0f, reverse: true);
            yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

            if (_controller.IsCurrentlyColliding)
            {
                _controller.Move(1.0f, reverse: false);
                yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);
            }

            while (!_algorithm.HasReachedTarget())
            {
                _controller.PathAndMoveTo(_algorithm.TargetVertex.Position, dependOnBrokenBehaviour: false);
                yield return WaitForCondition.WaitForLogicTicks(1);
            }

            // Invalidate the old path.
            _algorithm.CurrentPath.Clear();
        }
    }
}