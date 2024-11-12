#nullable enable
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
        //private Vertex? _pathStart; // = new Vertex(0, Vector2Int.zero);
        private int _iterator = 0;

        public override string GetDebugInfo()
        {
            return 
                base.GetDebugInfo() +
                $"Highest idle: {HighestIdle().Position}\n" +
                $"Init done: {_isPatrolling}\n";
        }

        protected override void Preliminaries()
        {
            if (_isPatrolling)
            {
                return;
            }

            //_pathStart = GetClosestVertex();
            MakePath();
            TargetVertex = _currentPath[_iterator]; 
            _isPatrolling = true;
        }
        
        protected override Vertex NextVertex()
        {
            MakePath();
            var next = _currentPath[_iterator];  
            _iterator++;

            return next;
        }

        private void MakePath()
        {
            if (_currentPath != null && _iterator < _currentPath.Count)
            {
                return;
            }

            _iterator = 0;
            _currentPath = AStar(GetClosestVertex(), HighestIdle());
            //_pathStart = _currentPath.Last();
        }
        
        private Vertex HighestIdle()
        {
            return _vertices.OrderBy((x)=>x.LastTimeVisitedTick).First();
        }

        private Vertex GetClosestVertex()
        {
            Vertex? closestVertex = null;
            var closestDistance = float.MaxValue;
            var position = _controller.GetSlamMap().GetCoarseMap().GetCurrentPosition();
            foreach (var vertex in _vertices)
            {
                var distance = Vector2Int.Distance(position, vertex.Position);
                if (!(distance < closestDistance))
                {
                    continue;
                }

                closestDistance = distance;
                closestVertex = vertex;
            }
            return closestVertex ?? throw new InvalidOperationException("There are no vertices!");
        }
       
        /*private static List<Vertex> FindShortestPath(Vertex start, Vertex target)
        { //BFS search
            var parents = new Dictionary<Vertex, Vertex>();
            var queue = new Queue<Vertex>();
            var visited = new HashSet<Vertex>();

            queue.Enqueue(start);
            visited.Add(start);
            parents[start] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                               
                if (current.Equals(target))
                {
                    var path = new List<Vertex>();
                    for (var curr = target; curr != null; curr = parents[curr])
                    {
                        path.Insert(0, curr);
                    }
                    return path;
                }

                foreach (var neighbor in current.Neighbors)
                {
                    if (visited.Contains(neighbor))
                    {
                        continue;
                    }

                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    parents[neighbor] = current;
                }
            }
            throw new InvalidOperationException("There are no path");
        }*/
        
        private static List<Vertex> AStar(Vertex start, Vertex target)
        {
            // Dictionary to store the cost of the path from the start to each vertex (g-cost)
            var gCost = new Dictionary<Vertex, float> { [start] = 0 };

            // Dictionary to store estimated total cost from start to target (f-cost)
            var fCost = new Dictionary<Vertex, float> { [start] = Heuristic(start, target) };

            // Dictionary to store the path (i.e., the vertex that leads to each vertex)
            var cameFrom = new Dictionary<Vertex, Vertex>();

            // Open set initialized with the starting vertex
            var openSet = new HashSet<Vertex> { start };

            while (openSet.Count > 0)
            {
                // Select vertex in openSet with lowest f-cost
                var current = openSet.OrderBy(v => fCost.ContainsKey(v) ? fCost[v] : float.MaxValue).First();

                // If reached target, reconstruct path
                if (current.Equals(target))
                {
                    Debug.Log("current == target");
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                // Explore neighbors
                foreach (var neighbor in current.Neighbors)
                {
                    float tentativeGCost = gCost[current] + Vector2Int.Distance(current.Position, neighbor.Position) * neighbor.Weight;

                    // If a cheaper path to neighbor is found
                    if (!gCost.ContainsKey(neighbor) || tentativeGCost < gCost[neighbor])
                    {
                        // Record the best path to neighbor and its costs
                        cameFrom[neighbor] = current;
                        gCost[neighbor] = tentativeGCost;
                        fCost[neighbor] = tentativeGCost + Heuristic(neighbor, target);

                        // Add neighbor to open set if not present
                        openSet.Add(neighbor);
                    }
                }
            }

            Debug.Log("empty list");
            // Return empty path if target is unreachable
            return new List<Vertex>();
        }

        private static List<Vertex> ReconstructPath(Dictionary<Vertex, Vertex> cameFrom, Vertex current)
        {
            var path = new List<Vertex> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private static float Heuristic(Vertex a, Vertex b)
        {
            // Use Manhattan distance as the heuristic for grid-based graphs
            return Vector2Int.Distance(a.Position, b.Position);
        }
    }
}
