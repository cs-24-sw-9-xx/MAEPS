using System;
using System.Text;

using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }

        public Vertex TargetVertex => _targetVertex ?? throw new InvalidOperationException("TargetVertex is null");

        private Vertex? _targetVertex;

        // Set by SetPatrollingMap
        protected Vertex[] _vertices = null!;

        // Set by SetController
        protected Robot2DController _controller = null!;

        protected event OnReachVertex? OnReachVertexHandler;

        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void SetPatrollingMap(PatrollingMap map)
        {
            _vertices = map.Vertices;
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
            _targetVertex ??= GetClosestVertex();

            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
            if (currentPosition != TargetVertex.Position)
            {
                _controller.PathAndMoveTo(TargetVertex.Position);
                return;
            }

            OnReachTargetVertex(TargetVertex);
            _targetVertex = NextVertex();
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