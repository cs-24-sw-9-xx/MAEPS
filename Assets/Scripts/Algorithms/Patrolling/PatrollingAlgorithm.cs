using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Map.PathFinding;
using Maes.Robot;
using Maes.Robot.Task;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }


        // Do not change visibility of this!
        private PatrollingMap _globalMap;

        public Vertex TargetVertex
        {
            get => _targetVertex ?? throw new InvalidOperationException("TargetVertex is null");
            private set
            {
#if DEBUG
                if (!AllowForeignVertices && !_vertices.Contains(value))
                {
                    throw new ArgumentException("TargetVertex is not from our patrolling map", nameof(value));
                }
#endif

                _targetVertex = value;
            }
        }

        private Vertex? _targetVertex;

        // Set by SetPatrollingMap
        protected Vertex[] _vertices = null!;
        private IReadOnlyDictionary<(int, int), PathStep[]> _paths = null!;

        private Queue<PathStep> _currentPath = new();

        private PathStep? _initialPathStep;
        private Vector2Int? _currentTarget;

        // Set by SetController
        protected Robot2DController _controller = null!;

        /// <summary>
        /// Allow NextVertex to return a vertex that is not from _vertices.
        /// You must know what you are doing when setting this to true.
        /// </summary>
        /// <remarks>
        /// This allows for using a global map such that they can share idleness knowledge globally.
        /// This is mostly useful for algorithms with a central planner / coordinator.
        /// </remarks>
        protected virtual bool AllowForeignVertices => false;

        private readonly StringBuilder _stringBuilder = new();

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

        /// <inheritdoc/>
        public virtual void SetGlobalPatrollingMap(PatrollingMap globalMap)
        {
            _globalMap = globalMap;
        }

        public void SubscribeOnReachVertex(OnReachVertex onReachVertex)
        {
            OnReachVertexHandler += onReachVertex;
        }

        private void OnReachTargetVertex(Vertex vertex)
        {
            var atTick = _controller.GetRobot().Simulation.SimulatedLogicTicks;
            OnReachVertexHandler?.Invoke(vertex.Id, atTick);

            if (!AllowForeignVertices || (AllowForeignVertices && !_globalMap.Vertices.Contains(vertex)))
            {
                vertex.VisitedAtTick(atTick);
            }
        }

        private bool HasReachedTarget()
        {
            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition(dependOnBrokenBehavior: false);
            return currentPosition == TargetVertex.Position;
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_targetVertex != null && _controller.IsCurrentlyColliding)
                {
                    _controller.StopCurrentTask();
                    yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

                    _controller.Move(1.0f, reverse: true);
                    yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

                    while (!HasReachedTarget())
                    {
                        _controller.PathAndMoveTo(TargetVertex.Position, dependOnBrokenBehaviour: false);
                        yield return WaitForCondition.WaitForLogicTicks(1);
                    }

                    // Invalidate the old path.
                    _currentPath.Clear();
                }

                yield return WaitForCondition.ContinueUpdateLogic();
            }
        }

        public virtual IEnumerable<WaitForCondition> UpdateLogic()
        {
            TargetVertex = GetClosestVertex();
            while (!HasReachedTarget())
            {
                // Do normal astar pathing
                _controller.PathAndMoveTo(TargetVertex.Position, dependOnBrokenBehaviour: false);

                yield return WaitForCondition.WaitForLogicTicks(1);
            }

            SetNextVertex();

            while (true)
            {
                if (_currentPath.Count != 0 || !HasReachedTarget())
                {
                    PathAndMoveToTarget();
                    yield return WaitForCondition.WaitForLogicTicks(1);
                }
                else
                {
                    SetNextVertex();
                }
            }
        }

        private void SetNextVertex()
        {
            var currentVertex = TargetVertex;
            OnReachTargetVertex(currentVertex);
            TargetVertex = NextVertex(currentVertex);
            _currentPath = new Queue<PathStep>(_paths[(currentVertex.Id, TargetVertex.Id)]);
            _currentTarget = null;
        }

        protected abstract Vertex NextVertex(Vertex currentVertex);

        public string GetDebugInfo()
        {
            _stringBuilder.Clear();
            _stringBuilder
                .AppendLine(AlgorithmName)
                .AppendFormat("Target vertex: {0}\n", TargetVertex);
            GetDebugInfo(_stringBuilder);
            return _stringBuilder.ToString();
        }

        protected virtual void GetDebugInfo(StringBuilder stringBuilder) { }

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

        protected Vertex GetClosestVertex()
        {
            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition(dependOnBrokenBehavior: false);
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