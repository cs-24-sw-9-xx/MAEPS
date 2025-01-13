using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public class CognitiveCoordinated : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Cognitive Coordinated Algorithm";
        private readonly Dictionary<int, Vertex> _unavailableVertices = new();
        private List<Vertex> _currentPath = new();
        private int _iterator = 0;

        private readonly List<KeyValuePair<int, Vertex>> _messages = new();

        public override string GetDebugInfo()
        {
            return
                new StringBuilder()
                    .Append(base.GetDebugInfo())
                    .Append("Highest idle:")
                    .Append(HighestIdle().Position.ToString())
                    .ToString();
        }

        protected override void EveryTick()
        {
            _messages.AddRange(_controller.ReceiveBroadcastWithId().Select(m => new KeyValuePair<int, Vertex>(m.Key, (Vertex)m.Value)));
        }

        protected override Vertex NextVertex()
        {
            if (_messages.Count > 1)
            {
                foreach (var message in _messages)
                {
                    _unavailableVertices[message.Key] = message.Value;
                }

                _messages.Clear();
            }

            ConstructPath();
            return _currentPath[_iterator++];
        }

        private void ConstructPath()
        {
            // calculates a new path if another agent is going towards same end vertex
            if (_unavailableVertices.Values.Any(value => value == _currentPath.Last()))
            {
                _iterator = 0;
                _currentPath = AStar(GetClosestVertex(), HighestIdle());
                _currentPath.Remove(_currentPath.First());
                _controller.Broadcast(_currentPath.Last());

                return;
            }

            if (_iterator < _currentPath.Count)
            {
                //_controller.Broadcast(_iterator);
                return;
            }

            _iterator = 0;

            _currentPath = AStar(TargetVertex, HighestIdle());
            _currentPath.Remove(_currentPath.First());
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