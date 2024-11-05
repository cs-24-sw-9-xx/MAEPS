using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Maes.Map;

namespace Maes.PatrollingAlgorithms
{
    public class CognitiveCoordinated : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Cognitive Coordinated Algorithm";
        private bool _isPatrolling = false;
        private List<Vertex> _currentPath = new List<Vertex>();
        private Vertex _pathStart;
        private int i = 0;

        public override string GetDebugInfo()
        {
            return 
                base.GetDebugInfo() +
                $"Highest idle: {HighestIdle().Position}\n" +
                $"Init done:  {_isPatrolling}\n";
        }


        protected override void Preliminaries()
        {
            if (_isPatrolling) return;

            _pathStart = GetClosestVertex();
            MakePath();
            TargetVertex = _currentPath[i]; 
            _isPatrolling = true;
        }

        private void MakePath()
        {
            if (_currentPath != null && i < _currentPath.Count)
            {
                return;
            }

            i = 0;
            _currentPath = FindShortestPath(_pathStart, HighestIdle());
            _pathStart = _currentPath.Last();
        }

        protected override Vertex NextVertex()
        {
            MakePath();
            Vertex next = _currentPath[i];  
            i++;

            return next;
        }

        private Vertex HighestIdle()
        {
            return _vertices.OrderBy((x)=>x.LastTimeVisitedTick).First();
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
        public List<Vertex> FindShortestPath(Vertex start, Vertex target)
        {
            // Dictionary to store the parent of each visited vertex
            Dictionary<Vertex, Vertex> parents = new Dictionary<Vertex, Vertex>();
            // Queue for BFS
            Queue<Vertex> queue = new Queue<Vertex>();
            // Set to track visited vertices
            HashSet<Vertex> visited = new HashSet<Vertex>();

            queue.Enqueue(start);
            visited.Add(start);
            parents[start] = null;

            while (queue.Count > 0)
            {
                Vertex current = queue.Dequeue();
                               
                if (current.Equals(target))
                {
                    return ConstructPath(parents, target);
                }

                foreach (Vertex neighbor in current.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        parents[neighbor] = current;
                    }
                }
            }
            return null;
        }

        private List<Vertex> ConstructPath(Dictionary<Vertex, Vertex> parents, Vertex target)
        {
            List<Vertex> path = new List<Vertex>();
            for (Vertex current = target; current != null; current = parents[current])
            {
                path.Insert(0, current);
            }
            return path;
        }
    }
}
