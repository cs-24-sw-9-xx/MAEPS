using System;
using System.Collections.Generic;
using System.Text;

using Maes.Algorithms;
using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Task;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }

        public Vertex TargetVertex => _targetVertex ?? throw new InvalidOperationException("TargetVertex is null");

        private Vertex? _targetVertex;

        // Set by SetPatrollingMap
        private Vertex[] _vertices = null!;
        private IReadOnlyDictionary<(int, int), PathStep[]> _paths = null!;

        private Queue<PathStep> _currentPath = new();

        private PathStep? _initialPathStep;
        private Vector2Int? _currentTarget;

        private bool _goingToInitialVertex = true;

        // Set by SetController
        private Robot2DController _controller = null!;
        private bool _hasCollided;
        private bool _firstCollision;

        protected event OnReachVertex? OnReachVertexHandler;

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void SetPatrollingMap(PatrollingMap map)
        {
            _vertices = map.Vertices;
            _paths = map.Paths;
        }

        public void SubscribeOnReachVertex(OnReachVertex onReachVertex)
        {
            OnReachVertexHandler += onReachVertex;
        }

        private void OnReachTargetVertex(Vertex vertex)
        {
            var atTick = _controller.GetRobot().Simulation.SimulatedLogicTicks;
            OnReachVertexHandler?.Invoke(vertex.Id, atTick);
            vertex.VisitedAtTick(atTick);
        }

        public virtual void UpdateLogic()
        {
            if (_goingToInitialVertex)
            {
                _targetVertex ??= GetClosestVertex();
                var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
                if (currentPosition != TargetVertex.Position)
                {
                    // Do normal astar pathing
                    _controller.PathAndMoveTo(TargetVertex.Position);
                }
                else
                {
                    SetNextVertex();
                    _goingToInitialVertex = false;
                }
                return;
            }

            if (_controller.IsCurrentlyColliding)
            {
                _firstCollision = !_hasCollided;
                _hasCollided = true;
            }

            if (_hasCollided)
            {
                // Do default AStar
                _currentPath.Clear();
                var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
                if (currentPosition != TargetVertex.Position)
                {
                    if (_firstCollision)
                    {
                        _controller.StopCurrentTask();
                    }

                    _controller.PathAndMoveTo(TargetVertex.Position, dependOnBrokenBehaviour: false);
                }
                else
                {
                    _hasCollided = false;
                    SetNextVertex();
                }

                _firstCollision = false;

                return;
            }

            if (_currentPath.Count != 0)
            {
                PathAndMoveToTarget();
                return;
            }

            SetNextVertex();
        }

        private void SetNextVertex()
        {
            var currentVertex = TargetVertex;
            OnReachTargetVertex(currentVertex);
            _targetVertex = NextVertex();
            _currentPath = new Queue<PathStep>(_paths[(currentVertex.Id, _targetVertex.Id)]);
        }

        protected abstract Vertex NextVertex();

        public virtual string GetDebugInfo()
        {
            return
                new StringBuilder()
                    .AppendLine(AlgorithmName)
                    .Append("Target vertex position: ")
                    .AppendLine(TargetVertex.Position.ToString())
                    .Append("Has Collided: ")
                    .Append(_hasCollided)
                    .Append(" First Collision: ")
                    .AppendLine(_firstCollision.ToString())
                    .ToString();
        }

        private void PathAndMoveToTarget()
        {
            const float closeness = 0.25f;

            if (_controller.GetStatus() != RobotStatus.Idle)
            {
                return;
            }

            if (_currentTarget == null)
            {
                _initialPathStep = _currentPath.Dequeue();
                _currentTarget = _initialPathStep.Value.Start;
            }

            var relativePosition = _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget.Value, dependOnBrokenBehaviour: false);
            if (relativePosition.Distance < closeness)
            {
                if (_currentPath.Count == 0)
                {
                    _currentTarget = null;
                    return;
                }

                if (_initialPathStep != null)
                {
                    _currentTarget = _initialPathStep.Value.End;
                    _initialPathStep = null;
                }
                else
                {
                    _currentTarget = _currentPath.Dequeue().End;
                }
                relativePosition = _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget.Value, dependOnBrokenBehaviour: false);
            }

            if (Math.Abs(relativePosition.RelativeAngle) > 1.5f)
            {
                _controller.Rotate(relativePosition.RelativeAngle);
            }
            else if (relativePosition.Distance > closeness)
            {
                _controller.Move(relativePosition.Distance);
            }
        }

        private Vertex GetClosestVertex()
        {
            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
            var closestVertex = _vertices[0];
            var closestDistance = Vector2Int.Distance(position, closestVertex.Position);
            foreach (var vertex in _vertices.AsSpan(1))
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
    }
}