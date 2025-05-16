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
// Contributors:
// Mads Beyer Mogensen
// Puvikaran Santhirasegaram
// Henrik van Peet

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Tasks;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// This component moves the robot to the vertex specified by the algorithm.
    /// When the vertex has been reached it gets the next vertex from the algorithm.
    /// </summary>
    public sealed class GoToNextVertexComponent : IMovementComponent
    {
        // How close the robot has to get to a point before it has arrived.
        private const float MinDistance = 0.25f;

        // How small the angle to the target has to be before it is pointing in the direction.
        private const float MinAngle = 0.5f;

        private readonly NextVertexDelegate _nextVertexDelegate;
        private readonly PatrollingAlgorithm _patrollingAlgorithm;
        private readonly IRobotController _controller;
        private readonly PatrollingMap _patrollingMap;
        private readonly InitialVertexToPatrolDelegate _initialVertexToPatrolDelegate;
        public Vector2Int TargetPosition { get; private set; }
        public Vertex ApproachingVertex { get; private set; } = null!;
        private AbortingTask? _abortingTask;

        /// <summary>
        /// Delegate that specifies the next vertex to travel to.
        /// </summary>
        public delegate Vertex NextVertexDelegate(Vertex currentVertex);
        public delegate Vertex InitialVertexToPatrolDelegate();

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
        /// <param name="initialVertexToPatrolDelegate">The delegate that gets called to get the initial vertex to patrol.</param>
        public GoToNextVertexComponent(NextVertexDelegate nextVertexDelegate, PatrollingAlgorithm patrollingAlgorithm, IRobotController controller, PatrollingMap patrollingMap, InitialVertexToPatrolDelegate? initialVertexToPatrolDelegate = null)
        {
            _nextVertexDelegate = nextVertexDelegate;
            _patrollingAlgorithm = patrollingAlgorithm;
            _controller = controller;
            _patrollingMap = patrollingMap;
            _initialVertexToPatrolDelegate = initialVertexToPatrolDelegate ?? GetClosestVertexDefault;
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            // Go to the initial vertex.
            ApproachingVertex = _initialVertexToPatrolDelegate();
            TargetPosition = ApproachingVertex.Position;
            while (GetRelativePositionTo(ApproachingVertex.Position).Distance > MinDistance)
            {
                _controller.PathAndMoveTo(ApproachingVertex.Position, dependOnBrokenBehaviour: false);
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
            }

            while (true)
            {
                Vertex targetVertex;
                if (_controller.AssignedPartition != ApproachingVertex.Partition)
                {
                    // The robot has been assigned to another partition. We need to find the closest vertex in the new partition.
                    targetVertex = _nextVertexDelegate(_initialVertexToPatrolDelegate());
                    TargetPosition = targetVertex.Position;
                    while (GetRelativePositionTo(targetVertex.Position).Distance > MinDistance)
                    {
                        _controller.PathAndMoveTo(targetVertex.Position, dependOnBrokenBehaviour: false);
                        yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
                    }
                    _patrollingAlgorithm.OnReachTargetVertex(targetVertex, _nextVertexDelegate(targetVertex));
                    continue;
                }

                if (_abortingTask != null)
                {
                    ApproachingVertex = _abortingTask.Value.TargetVertex;
                    _abortingTask = null;
                }

                // Follow the path.
                // Go to the next vertex
                targetVertex = _nextVertexDelegate(ApproachingVertex);

                // Tell PatrollingAlgorithm that we reached the vertex
                _patrollingAlgorithm.OnReachTargetVertex(ApproachingVertex, targetVertex);
                var path = GetPathStepsToVertex(ApproachingVertex, targetVertex);
                ApproachingVertex = targetVertex;

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

        private Vertex GetClosestVertexDefault()
        {
            var robotPartition = _controller.AssignedPartition;
            var vertices = _patrollingMap.Vertices.Where(x => x.Partition == robotPartition).ToArray();
            return vertices.GetClosestVertex(target => _controller.EstimateDistanceToTarget(target) ?? int.MaxValue);
        }


        private IEnumerable<ComponentWaitForCondition> MoveToPosition(Vector2Int target)
        {
            TargetPosition = target;
            while (true)
            {
                if (_abortingTask != null)
                {
                    yield break;
                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

                // Do our own collision recovery when colliding with walls
                if (_controller.Status != RobotStatus.Idle && !_controller.IsCurrentlyColliding)
                {
                    continue;
                }

                if (_controller.IsCurrentlyColliding)
                {
                    _controller.StopCurrentTask();
                    yield return ComponentWaitForCondition.WaitForRobotStatus(RobotStatus.Idle, false);
                }

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
            if (currentVertex == targetVertex)
            {
                var path = new Queue<PathStep>();
                path.Enqueue(new PathStep(currentVertex.Position, currentVertex.Position, new HashSet<Vector2Int>() { currentVertex.Position }));
                return path;
            }
            return new Queue<PathStep>(_patrollingMap.Paths[(currentVertex.Id, targetVertex.Id)]);
        }

        public void AbortCurrentTask(AbortingTask abortingTask)
        {
            _abortingTask = abortingTask;
            TargetPosition = abortingTask.TargetVertex.Position;
        }
    }
}