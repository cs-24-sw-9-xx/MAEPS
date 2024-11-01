using System.Linq;
using UnityEngine;

using Maes.Map;

namespace Maes.PatrollingAlgorithms
{
    public class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Conscientious Reactive Algorithm";
        private bool _isPatrolling = false;

        public override string GetDebugInfo()
        {
            return 
                base.GetDebugInfo() +
                $"Init done:  {_isPatrolling}\n";
        }


        protected override void Preliminaries()
        {
            if (_isPatrolling) return;

            var vertex = GetClosestVertex();
            TargetVertex = vertex; 
            _isPatrolling = true;
        }

        protected override Vertex NextVertex()
        {
            return TargetVertex.Neighbors.OrderBy((x)=>x.LastTimeVisitedTick).First();
        }

        private Vertex GetClosestVertex(){
            Vertex closestVertex = null;
            float closestDistance = float.MaxValue;
            Vector2Int myPossition = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
            foreach (var vertex in _vertices){
                float distance = Vector2Int.Distance(myPossition, vertex.Position);
                if (distance < closestDistance){
                    closestDistance = distance;
                    closestVertex = vertex;
                }
            }
            return closestVertex;
        }
    }
}
