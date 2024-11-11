using System;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public class ConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Conscientious Reactive Algorithm";
        private bool _isPatrolling;

        public override string GetDebugInfo()
        {
            return
                base.GetDebugInfo() +
                $"Init done:  {_isPatrolling}\n";
        }


        protected override void Preliminaries()
        {
            if (_isPatrolling)
            {
                return;
            }

            var vertex = GetClosestVertex();
            TargetVertex = vertex;
            _isPatrolling = true;
        }

        protected override Vertex NextVertex()
        {
            return TargetVertex.Neighbors.OrderBy((x) => x.LastTimeVisitedTick).First();
        }

        private Vertex GetClosestVertex()
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