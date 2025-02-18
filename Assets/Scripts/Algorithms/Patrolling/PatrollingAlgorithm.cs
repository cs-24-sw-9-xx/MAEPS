using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Algorithms.Components;
using Maes.Algorithms.Patrolling.Components;
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

        protected PatrollingAlgorithm(ICollisionRecovery<PatrollingAlgorithm>? collisionRecovery = null)
        {
            _collisionRecovery = collisionRecovery ?? new DefaultPatrollingCollisionRecovery();
            _collisionRecovery.SetAlgorithm(this);
        }

        private readonly ICollisionRecovery<PatrollingAlgorithm> _collisionRecovery;

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
        private Vertex[] _vertices = null!;
        private IReadOnlyDictionary<(int, int), PathStep[]> _paths = null!;

        public Queue<PathStep> CurrentPath { get; private set; } = new();

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
            _collisionRecovery.SetController(_controller);
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

        public bool HasReachedTarget()
        {
            var relativePosition = GetRelativePositionTo(TargetVertex.Position);
            return relativePosition.Distance <= Closeness;
        }

        public IEnumerable<WaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_targetVertex != null)
                {
                    foreach (var condition in _collisionRecovery.CheckAndRecoverFromCollision())
                    {
                        yield return condition;
                    }
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
                var initialPathStep = CurrentPath.Peek();
                foreach (var condition in MoveToPosition(initialPathStep.Start))
                {
                    yield return condition;
                }

                // Go to the end of each path step.
                while (CurrentPath.Count > 0)
                {
                    var target = CurrentPath.Dequeue();
                    foreach (var condition in MoveToPosition(target.End))
                    {
                        yield return condition;
                    }
                }
            }
        }

        private const float Closeness = 0.25f;

        private IEnumerable<WaitForCondition> MoveToPosition(Vector2Int target)
        {
            while (true)
            {
                yield return WaitForCondition.WaitForRobotStatus(RobotStatus.Idle);

                var relativePosition = GetRelativePositionTo(target);
                if (relativePosition.Distance <= Closeness)
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
            CurrentPath = new Queue<PathStep>(_paths[(currentVertex.Id, TargetVertex.Id)]);
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

        private Vertex GetClosestVertex()
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