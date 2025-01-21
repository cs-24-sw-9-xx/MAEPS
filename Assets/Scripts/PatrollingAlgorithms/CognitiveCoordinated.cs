using System.Collections.Generic;
using System.Linq;

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

        private readonly List<OccupiedVertexMessage> _messagesLockVertex = new();
        private readonly List<UpdateVertexMessage> _messagesUpdateVertices = new();

        protected override void EveryTick()
        {
            _messagesUpdateVertices.AddRange(_controller.ReceiveBroadcast().OfType<UpdateVertexMessage>());
            _messagesLockVertex.AddRange(_controller.ReceiveBroadcast().OfType<OccupiedVertexMessage>());
        }

        protected override Vertex NextVertex()
        {
            if (_messagesLockVertex.Count > 1)
            {
                foreach (var message in _messagesLockVertex)
                {
                    _unavailableVertices[message.RobotId] = message.VertexId;
                }

                _messagesLockVertex.Clear();
            }

            if (_messagesUpdateVertices.Count > 1)
            {
                foreach (var vertex in _vertices)
                {
                    foreach (var message in _messagesUpdateVertices.Where(message => message.VertexId == vertex.Id && message.VisitedTick > vertex.LastTimeVisitedTick))
                    {
                        vertex.VisitedAtTick(message.VisitedTick);
                    }
                }

                _messagesUpdateVertices.Clear();
            }

            ConstructPath();
            var next = _currentPath[_pathStep];
            _controller.Broadcast(new UpdateVertexMessage(next.Id, next.LastTimeVisitedTick));
            _pathStep++;

            return next;
        }

        private void ConstructPath()
        {
            // calculates a new path if another agent is going towards same end vertex
            if (_currentPath.Count > 0 && _unavailableVertices.Values.Any(value => value == _currentPath.Last().Id))
            {
                PathConstructor();

                return;
            }

            if (_pathStep < _currentPath.Count)
            {
                return;
            }

            PathConstructor();
        }

        private void PathConstructor()
        {
            _pathStep = 0;

            var firstElement = (_currentPath.Any()) ? _currentPath.Last() : GetClosestVertex();
            _currentPath = AStar(firstElement, HighestIdle());

            _controller.Broadcast(new OccupiedVertexMessage(_controller.GetRobotID(), _currentPath.Last().Id));
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

            // remove first element, because so we don't path to same vertex
            path.Remove(path.First());

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
            
            public int RobotId { get; }

            public OccupiedVertexMessage(int robotId, int vertexId)
            {
                RobotId = robotId;
                VertexId = vertexId;
            }
        }
        
        private sealed class UpdateVertexMessage
        {
            public int VertexId { get; }
            
            public int VisitedTick { get; }

            public UpdateVertexMessage(int vertexId, int visitedTick)
            {
                VertexId = vertexId;
                VisitedTick = visitedTick;
            }
        }
    }
}
