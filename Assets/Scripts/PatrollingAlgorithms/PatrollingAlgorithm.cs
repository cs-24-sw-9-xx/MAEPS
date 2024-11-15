using System;

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
        private Vertex[] _vertices = null!;

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
                AlgorithmName + "\n" +
                $"Target vertex position: {TargetVertex.Position}\n";
        }

        protected Vertex GetClosestVertex()
        {
            Vertex? closestVertex = null;
            var closestDistance = float.MaxValue;
            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
            foreach (var vertex in _vertices)
            {
                var distance = Vector2Int.Distance(position, vertex.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestVertex = vertex;
                }
            }
            return closestVertex ?? throw new InvalidOperationException("There are no vertices!");
        }
    }
}