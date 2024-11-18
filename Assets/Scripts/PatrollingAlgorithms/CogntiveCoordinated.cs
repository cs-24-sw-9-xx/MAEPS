using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.PatrollingAlgorithms
{
    public class CognitiveCoordinated : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Cognitive Coordinated Algorithm";
        private bool _isPatrolling = false;
        private List<Vertex> _currentPath = new List<Vertex>();
        private int _iterator = 0;
        private readonly Dictionary<int, Vertex> _unavailableVertices = new Dictionary<int, Vertex>();

        public override string GetDebugInfo()
        {
            return
                base.GetDebugInfo() +
                $"Highest idle: {HighestIdle().Position}\n" +
                $"Init done: {_isPatrolling}\n";
        }

        protected override void Preliminaries()
        {
            var receivedHeartbeat = _controller.ReceiveBroadcastWithId().OfType<KeyValuePair<int, Vertex>>();

            var enumerable = receivedHeartbeat.ToList();
            if (enumerable.Count > 1)
            {
                foreach (var message in enumerable)
                {
                    _unavailableVertices[message.Key] = message.Value;
                }
            }

            if (_isPatrolling)
            {
                return;
            }

            ConstructPath();
            TargetVertex = _currentPath[_iterator];
            _isPatrolling = true;
        }

        protected override Vertex NextVertex()
        {
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

            _currentPath = AStar(GetClosestVertex(), HighestIdle());

            var ownHeartbeat = HighestIdle();
            _controller.Broadcast(ownHeartbeat);
        }

        private Vertex HighestIdle()
        {
            // excluding the vertices other agents are pathing towards
            var availableVertices = _vertices.Except(_unavailableVertices.Values).ToList();
            return availableVertices.OrderBy((x) => x.LastTimeVisitedTick).First();
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