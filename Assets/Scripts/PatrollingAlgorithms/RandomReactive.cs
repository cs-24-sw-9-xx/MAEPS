using System;
using System.Linq;

using Maes.Map;

using UnityEngine;

/// <summary>
/// The random reactive patrolling algorithm from "Multi-Agent Movement Coordination in Patrolling", 2002
/// </summary>
namespace Maes.PatrollingAlgorithms
{
    public class RandomReactive : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Random Reactive Algorithm";
        private bool _isPatrolling;

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
            return TargetVertex.Neighbors.ElementAt(UnityEngine.Random.Range(0, TargetVertex.Neighbors.Count));
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