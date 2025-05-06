using System.Collections.Generic;
using System.Linq;

using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public class CollisionAvoidanceTargetSameVertexComponent : IComponent
    {
        public CollisionAvoidanceTargetSameVertexComponent(int preUpdateOrder, int postUpdateOrder, IMovementComponent component,
            IRobotController controller)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _movementComponent = component;
            _controller = controller;
        }

        private readonly IMovementComponent _movementComponent;
        private readonly IRobotController _controller;

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                CollisionAvoidanceHandling();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, true);
            }
        }

        /// <summary>
        /// Resolves potential movement conflicts when multiple robots target the same vertex.
        /// </summary>
        private void CollisionAvoidanceHandling()
        {
            var targetVertex = _movementComponent.ApproachingVertex;
            if (_movementComponent.TargetPosition != targetVertex.Position)
            {
                // The robot is not moving towards the target vertex, so no need to resolve possible collision.
                return;
            }

            var distanceToVertex = _controller.EstimateDistanceToTarget(targetVertex.Position) ?? float.MaxValue;

            // Check if other robots are already going to the target vertex and if they are closer than this robot.
            var receivedMessages = _controller.ReceiveBroadcast().OfType<GoingToVertexMessage>();
            if (receivedMessages.Any(otherRobot => otherRobot.TargetPosition == targetVertex.Position &&
                                                          otherRobot.DistanceToVertex < distanceToVertex))
            {
                _movementComponent.AbortCurrentTask(new AbortingTask(targetVertex, true));
                return;
            }
            
            // Broadcast the message to other robots that this robot is going to the target vertex and the distance to the vertex.
            _controller.Broadcast(new GoingToVertexMessage(targetVertex.Position, distanceToVertex, _controller.Id));
        }

        /// <summary>
        /// Encapsulates the message that is sent to other robots when this robot is going to a vertex and the distance to the vertex.
        /// </summary>
        private readonly struct GoingToVertexMessage
        {
            public GoingToVertexMessage(Vector2Int targetPosition, float distanceToVertex, int fromRobotId)
            {
                TargetPosition = targetPosition;
                DistanceToVertex = distanceToVertex;
                FromRobotId = fromRobotId;
            }

            public Vector2Int TargetPosition { get; }
            public float DistanceToVertex { get; }
            public int FromRobotId { get; }
        }
    }
}