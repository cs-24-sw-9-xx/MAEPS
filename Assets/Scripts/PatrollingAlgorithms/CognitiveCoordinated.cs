using System.Text;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public class CognitiveCoordinated : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Cognitive Coordinated Algorithm";
        private readonly Dictionary<int, Vertex> _unavailableVertices = new Dictionary<int, Vertex>();
        private List<Vertex> _currentPath = new List<Vertex>();
        private int _iterator = 0;

        public override string GetDebugInfo()
        {
            return
                base.GetDebugInfo() +
                new StringBuilder()
                    .Append("Highest idle:")
                    .Append(HighestIdle().Position.ToString())
                    .ToString();
        }

        protected override Vertex NextVertex()
        {
            var broadcasts = _controller.ReceiveBroadcastWithId().Where(broadcast => broadcast.Value is Vertex).ToList();

            if (broadcasts.Count > 1)
            {
                foreach (var message in broadcasts)
                {
                    if (message.Value is Vertex value)
                    {
                        _unavailableVertices[message.Key] = value;    
                    }
                }
            }

            ConstructPath();
            var next = _currentPath[_iterator];
            _iterator++;

            return next;
        }

        private void ConstructPath()
        {
            if (_iterator < _currentPath.Count)
            {
                return;
            }

            _iterator = 0;

            _currentPath = AStar(TargetVertex, HighestIdle());
            _controller.Broadcast(_currentPath.Last());
        }

        private Vertex HighestIdle()
        {
            // excluding the vertices other agents are pathing towards
           var availableVertices = _vertices.Except(_unavailableVertices.Values).OrderBy((x) => x.LastTimeVisitedTick).ToList();

            var position = TargetVertex.Position;
            var first = availableVertices.First();
            var closestVertex = first;

            //maybe this extra computation shouldn't exist for CC.
            foreach (var vertex in availableVertices.Where(vertex => vertex.LastTimeVisitedTick == first.LastTimeVisitedTick))
            {
                //would be better if it wasn't euclidean distance
                if (Vector2Int.Distance(position, vertex.Position) < Vector2Int.Distance(position, closestVertex.Position))
                {
                   closestVertex = vertex; 
                }
            }

            return closestVertex;
           //return _vertices.OrderBy((x) => x.LastTimeVisitedTick).First();
        }

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
                if (current.Position == target.Position)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                // Explore neighbors
                foreach (var neighbor in current.Neighbors)
                {
                    var tentativeGCost = gCost[current] + Vector2Int.Distance(current.Position, neighbor.Position);

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