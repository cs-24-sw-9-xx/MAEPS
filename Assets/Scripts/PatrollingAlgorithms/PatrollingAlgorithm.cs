using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms;
using Maes.Map;
using Maes.Robot;

namespace MAES.PatrollingAlgorithms
{
    public abstract class PatrollingAlgorithm : IPatrollingAlgorithm
    {
        public abstract string AlgorithmName { get; }
        public Vertex TargetVertex { get; protected set; }
        protected IReadOnlyList<Vertex> _vertices;
        protected Robot2DController _controller;
        protected event OnReachVertex OnReachVertexHandler;
        
        public void SetController(Robot2DController controller)
        {
            _controller = controller;
        }

        public void SetPatrollingMap(PatrollingMap map)
        {
            _vertices = map.Verticies;
        }
        
        public void SubscribeOnReachVertex(OnReachVertex onReachVertex)
        {
            OnReachVertexHandler += onReachVertex;
        }

        protected void OnReachTargetVertex()
        {
            var atTick = _controller.GetRobot().Simulation.SimulatedLogicTicks;
            OnReachVertexHandler?.Invoke(TargetVertex, atTick);
            TargetVertex.VisitedAtTick(atTick);
        }

        public virtual void UpdateLogic()
        {
            Preliminaries();
            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
            if(currentPosition != TargetVertex.Position){
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