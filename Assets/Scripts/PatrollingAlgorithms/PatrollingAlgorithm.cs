using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;

namespace Maes.PatrollingAlgorithms
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }

        public Vertex TargetVertex { get; protected set; } = null!; // HACK!

        // Set by SetPatrollingMap
        protected IReadOnlyList<Vertex> _vertices = null!;

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

        private void OnReachTargetVertex()
        {
            var atTick = _controller.GetRobot().Simulation.SimulatedLogicTicks;
            OnReachVertexHandler?.Invoke(TargetVertex, atTick);
            TargetVertex.VisitedAtTick(atTick);
        }

        public virtual void UpdateLogic()
        {
            Preliminaries();
            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
            if (currentPosition != TargetVertex.Position)
            {
                _controller.PathAndMoveTo(TargetVertex.Position);
                return;
            }
            OnReachTargetVertex();
            TargetVertex = NextVertex();
        }
        protected virtual void Preliminaries() { }
        protected abstract Vertex NextVertex();

        public virtual string GetDebugInfo()
        {
            return
                AlgorithmName + "\n" +
                $"Target vertex position: {TargetVertex.Position}\n";
        }
    }
}