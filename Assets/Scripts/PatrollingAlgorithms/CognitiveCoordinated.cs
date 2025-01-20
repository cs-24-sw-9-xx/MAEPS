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
        private readonly Dictionary<int, int> _unavailableVertices = new();
        private List<Vertex> _currentPath = new();
        private int _pathStep = 0;

        private readonly List<(int robotId, OccupiedVertexMessage message)> _messages = new();

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
            _messages.AddRange(_controller.ReceiveBroadcastWithId().Select(m => (m.Key, (OccupiedVertexMessage)m.Value)));
        }

        protected override Vertex NextVertex()
        {
            foreach (var (robotId, occupiedVertexMessage) in _messages)
            {
                var vertex = _vertices[occupiedVertexMessage.VertexId];
                if (vertex.LastTimeVisitedTick < occupiedVertexMessage.LastTimeVisitedTick)
                {
                    vertex.VisitedAtTick(occupiedVertexMessage.LastTimeVisitedTick);
                }
                _unavailableVertices[robotId] = occupiedVertexMessage.VertexId;
            }

            _messages.Clear();

            CreatePathIfNeeded();
            return _currentPath[_pathStep++];
        }

        private void CreatePathIfNeeded()
        {
            // If our target is not unavailable, and we have steps left, keep it.
            if (_unavailableVertices.Values.All(unavailableVertexId => unavailableVertexId != _currentPath.Last().Id) && _pathStep < _currentPath.Count)
            {
                return;
            }

            // Create a path to the vertex with the highest idleness
            _pathStep = 0;
            _currentPath = AStar(TargetVertex, HighestIdle());
            _currentPath.Remove(_currentPath.First());
            var lastVertex = _currentPath.Last();
            _controller.Broadcast(new OccupiedVertexMessage(lastVertex.Id, lastVertex.LastTimeVisitedTick));
        }

        private Vertex HighestIdle()
        {
            // excluding the vertices other agents are pathing towards
            var availableVertices = _vertices
                .Where(vertex =>
                    _unavailableVertices.Values.All(unavailableVertexId => unavailableVertexId != vertex.Id))
                .OrderBy(vertex => vertex.LastTimeVisitedTick)
                .ToList();

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

        private sealed class OccupiedVertexMessage
        {
            public int VertexId { get; }

            public int LastTimeVisitedTick { get; }

            public OccupiedVertexMessage(int vertexId, int lastTimeVisitedTick)
            {
                VertexId = vertexId;
                LastTimeVisitedTick = lastTimeVisitedTick;
            }
        }
    }

}