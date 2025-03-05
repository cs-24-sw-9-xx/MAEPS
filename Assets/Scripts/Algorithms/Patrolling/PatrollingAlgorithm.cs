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
        private PatrollingMap _globalMap = null!;

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

                    if (_controller.IsCurrentlyColliding)
                    {
                        _controller.Move(1.0f, reverse: false);
                        yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);
                    }

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

            while (true)
            {
                SetNextVertex();

                // Go to the start of the first step.
                var initialPathStep = _currentPath.Peek();
                foreach (var condition in MoveToPosition(initialPathStep.Start))
                {
                    yield return condition;
                }

                // Go to the end of each path step.
                while (_currentPath.Count > 0)
                {
                    var target = _currentPath.Dequeue();
                    foreach (var condition in MoveToPosition(target.End))
                    {
                        yield return condition;
                    }
                }
            }
        }

        private const float _closeness = 0.25f;

        private IEnumerable<WaitForCondition> MoveToPosition(Vector2Int target)
        {
            while (true)
            {
                yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

                var relativePosition = GetRelativePositionTo(target);
                if (relativePosition.Distance <= _closeness)
                {
                    yield break;
                }

                if (Math.Abs(relativePosition.RelativeAngle) > 1.5f)
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

        private void SetNextVertex()
        {
            var currentVertex = TargetVertex;
            OnReachTargetVertex(currentVertex);
            TargetVertex = NextVertex(currentVertex);
            _currentPath = new Queue<PathStep>(_paths[(currentVertex.Id, TargetVertex.Id)]);
        }

        protected abstract Vertex NextVertex(Vertex currentVertex);

        /// <summary>
        /// Compiles and returns debug information for the patrolling algorithm.
        /// </summary>
        /// <remarks>
        /// This method clears an internal StringBuilder and appends the algorithm’s name,
        /// its current target vertex, and a comma-separated list of the target vertex's
        /// neighbors. Additional debug details are appended by calling an overload of GetDebugInfo.
        /// </remarks>
        public string GetDebugInfo()
        {
            _stringBuilder.Clear();
            _stringBuilder
                .AppendLine(AlgorithmName)
                .AppendFormat("Target vertex: {0}\n", TargetVertex)
                .AppendFormat("Neighbours: {0}\n", TargetVertex.Neighbors.Select(x => x.ToString()).Aggregate((x, y) => $"{x}, {y}"));
            GetDebugInfo(_stringBuilder);
            return _stringBuilder.ToString();
        }

        protected virtual void GetDebugInfo(StringBuilder stringBuilder) { }

        /// <summary>
        /// Finds the vertex within the robot's assigned partition that is closest to its current coarse position.
        /// </summary>
        /// <returns>
        /// The vertex from the assigned partition with the smallest Euclidean distance to the robot's current position.
        /// </returns>
        private Vertex GetClosestVertex()
        {
            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition(dependOnBrokenBehavior: false);
            var robotPartition = _controller.GetRobot().AssignedPartition;
            var verticesInPartition = _vertices.Where(x => x.Partition == robotPartition).ToArray();
            var closestVertex = verticesInPartition[0];
            var closestDistance = Vector2Int.Distance(position, closestVertex.Position);
            foreach (var vertex in verticesInPartition.AsSpan(1))
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