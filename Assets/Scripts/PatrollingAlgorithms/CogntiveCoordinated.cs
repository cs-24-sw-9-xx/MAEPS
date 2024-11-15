#nullable enable
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
        private int _logicTicks = 0;
        private int _ticksSinceHeartbeat = 0;

        public override string GetDebugInfo()
        {
            return
                base.GetDebugInfo() +
                $"Highest idle: {HighestIdle().Position}\n" +
                $"Init done: {_isPatrolling}\n";
        }

        protected override void Preliminaries()
        {
            _logicTicks++;
            _ticksSinceHeartbeat++;

            if (_ticksSinceHeartbeat == 10)
            {
                var ownHeartbeat = new HeartbeatMessage(HighestIdle());
                _ticksSinceHeartbeat = 0;
                _controller.Broadcast(ownHeartbeat);
            }

            var receivedHeartbeat = new Queue<HeartbeatMessage>(_controller.ReceiveBroadcast().OfType<HeartbeatMessage>());

            if (receivedHeartbeat.Count > 1)
            {
                var combinedMessage = receivedHeartbeat.Dequeue();
                foreach (var message in receivedHeartbeat)
                {
                    combinedMessage = combinedMessage.Combine(message);
                }
            }

            if (_isPatrolling)
            {
                return;
            }

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
            if (_iterator < _currentPath.Count)
            {
                return;
            }

            _iterator = 0;
            
            _currentPath = AStar(GetClosestVertex(), HighestIdle());
        }

        private Vertex HighestIdle()
        {
            return _vertices.OrderBy((x) => x.LastTimeVisitedTick).First();
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

    public class HeartbeatMessage
    {    
        private readonly Vertex _vertex;

        public HeartbeatMessage(Vertex vertex)
        {
            _vertex = vertex;
        }

        public HeartbeatMessage Combine(HeartbeatMessage heartbeatMessage)
        {
            List<Vertex> vertices = new() { heartbeatMessage._vertex, _vertex};
            // noget sync noget...
            return this;
        }
    }
}
