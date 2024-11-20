using System;
using System.Collections.Generic;
using System.Text;

using Maes.Algorithms;
using Maes.Map;
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
        private IReadOnlyDictionary<(int, int), Vector2Int[]> _paths = null!;

        private Queue<Vector2Int> _currentPath = new();

        private Vector2Int? _currentTarget;

        private bool _goingToInitialVertex = true;

        // Set by SetController
        private Robot2DController _controller = null!;

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
            _currentPath = new Queue<Vector2Int>(_paths[(currentVertex.Id, _targetVertex.Id)]);
        }

        protected abstract Vertex NextVertex();

        public virtual string GetDebugInfo()
        {
            return
                new StringBuilder()
                    .AppendLine(AlgorithmName)
                    .Append("Target vertex position: ")
                    .AppendLine(TargetVertex.Position.ToString())
                    .ToString();
        }

        private void PathAndMoveToTarget()
        {
            const float closeness = 0.25f;

            if (_controller.GetStatus() != RobotStatus.Idle)
            {
                return;
            }

            _currentTarget ??= _currentPath.Dequeue();

            var relativePosition = _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget.Value);
            if (relativePosition.Distance < closeness)
            {
                if (_currentPath.Count == 0)
                {
                    _currentTarget = null;
                    return;
                }

                _currentTarget = _currentPath.Dequeue();
                relativePosition = _controller.SlamMap.CoarseMap.GetTileCenterRelativePosition(_currentTarget.Value);
            }
            #region DrawPath
#if DEBUG
            Debug.DrawLine(_controller.SlamMap.CoarseMap.TileToWorld(_controller.SlamMap.CoarseMap.GetApproximatePosition()), _controller.SlamMap.CoarseMap.TileToWorld(_currentTarget.Value), Color.cyan, 2);
            for (var i = 0; i < _currentPath.Count - 1; i++)
            {
                var pathSteps = _currentPath.ToArray();
                if (i == 0)
                {
                    Debug.DrawLine(_controller.SlamMap.CoarseMap.TileToWorld(_currentTarget.Value), _controller.SlamMap.CoarseMap.TileToWorld(pathSteps[i]), Color.cyan, 2);
                }

                Debug.DrawLine(_controller.SlamMap.CoarseMap.TileToWorld(pathSteps[i]), _controller.SlamMap.CoarseMap.TileToWorld(pathSteps[i + 1]), Color.cyan, 2);
            }
#endif
            #endregion
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