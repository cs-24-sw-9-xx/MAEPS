// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Mads Beyer Mogensen

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Tasks;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// This component moves the robot to the vertex specified by the algorithm.
    /// When the vertex has been reached it gets the next vertex from the algorithm.
    /// </summary>
    public sealed class GoToNextVertexComponent : IComponent
    {
        // How close the robot has to get to a point before it has arrived.
        private const float MinDistance = 0.25f;

        // How small the angle to the target has to be before it is pointing in the direction.
        private const float MinAngle = 1.5f;

        private readonly NextVertexDelegate _nextVertexDelegate;
        private readonly PatrollingAlgorithm _patrollingAlgorithm;
        private readonly Robot2DController _controller;
        private readonly PatrollingMap _patrollingMap;

        /// <summary>
        /// Delegate that specifies the next vertex to travel to.
        /// </summary>
        public delegate Vertex NextVertexDelegate(Vertex currentVertex);

        /// <inheritdoc />
        public int PreUpdateOrder { get; } = 0;

        /// <inheritdoc />
        public int PostUpdateOrder { get; } = 0;

        /// <summary>
        /// Creates a new instance of <see cref="GoToNextVertexComponent"/>.
        /// </summary>
        /// <param name="nextVertexDelegate">The delegate that gets called to get the next vertex.</param>
        /// <param name="patrollingAlgorithm">The patrolling algorithm.</param>
        /// <param name="controller">The robot controller.</param>
        /// <param name="patrollingMap">The patrolling map.</param>
        public GoToNextVertexComponent(NextVertexDelegate nextVertexDelegate, PatrollingAlgorithm patrollingAlgorithm, Robot2DController controller, PatrollingMap patrollingMap)
        {
            _nextVertexDelegate = nextVertexDelegate;
            _patrollingAlgorithm = patrollingAlgorithm;
            _controller = controller;
            _patrollingMap = patrollingMap;
        }

        /// <inheritdoc />
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            // Go to the initial vertex.
            var vertex = GetClosestVertex();
            while (GetRelativePositionTo(vertex.Position).Distance > MinDistance)
            {
                _controller.PathAndMoveTo(vertex.Position, dependOnBrokenBehaviour: false);
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
            }

            while (true)
            {

                // Follow the path.
                // Go to the next vertex
                var targetVertex = _nextVertexDelegate(vertex);

                // Tell PatrollingAlgorithm that we reached the vertex
                _patrollingAlgorithm.OnReachTargetVertex(vertex, targetVertex);

                var path = GetPathStepsToVertex(vertex, targetVertex);
                vertex = targetVertex;


                // Move to the start of the path
                foreach (var condition in MoveToPosition(path.Peek().Start))
                {
                    yield return condition;
                }

                // Move to the end of each path
                while (path.TryDequeue(out var pathStep))
                {
                    foreach (var condition in MoveToPosition(pathStep.End))
                    {
                        yield return condition;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<ComponentWaitForCondition> PostUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        /// <summary>
        /// Gets the vertex closest to the robot.
        /// </summary>
        /// <remarks>
        /// TODO: This does not take walls into account so it might not pick the best waypoint.
        /// </remarks>
        /// <returns>The closest vertex.</returns>
        private Vertex GetClosestVertex()
        {
            var robotPartition = _controller.GetRobot().AssignedPartition;
            var vertices = _patrollingMap.Vertices.Where(x => x.Partition == robotPartition).ToArray();

            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition(dependOnBrokenBehavior: false);
            var closestVertex = vertices[0];
            var closestDistance = Vector2Int.Distance(position, closestVertex.Position);
            foreach (var vertex in vertices.AsSpan(1))
            {
                var distance = Vector2Int.Distance(position, vertex.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestVertex = vertex;
                }
            }

            return closestVertex;
        }


        private IEnumerable<ComponentWaitForCondition> MoveToPosition(Vector2Int target)
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, shouldContinue: false);

                var relativePosition = GetRelativePositionTo(target);
                if (relativePosition.Distance <= MinDistance)
                {
                    yield break;
                }


                if (Math.Abs(relativePosition.RelativeAngle) > MinAngle)
                {
                    _controller.Rotate(relativePosition.RelativeAngle);
                }
                else
                {
                    _controller.Move(relativePosition.Distance);
                }
            }
        }

        private RelativePosition GetRelativePositionTo(Vector2Int position)
        {
            return _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(position, dependOnBrokenBehaviour: false);
        }

        private Queue<PathStep> GetPathStepsToVertex(Vertex currentVertex, Vertex targetVertex)
        {
            return new Queue<PathStep>(_patrollingMap.Paths[(currentVertex.Id, targetVertex.Id)]);
        }
    }
}