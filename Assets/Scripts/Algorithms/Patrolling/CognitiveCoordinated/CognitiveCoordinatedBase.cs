using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    public abstract class CognitiveCoordinatedBase : PatrollingAlgorithm
    {
        private List<Vertex> _currentPath = new();
        private int _pathStep;
        private readonly Dictionary<(int, int), Vertex[]> _pathsCache = new();

        protected override bool AllowForeignVertices => true;

        protected override void GetDebugInfo(StringBuilder stringBuilder)
        {
            stringBuilder
                .AppendFormat("Last Vertex: {0}\n", _currentPath.Count > 0 ? _currentPath[^1].ToString() : "None");
        }

        // Set by CreateComponents
        protected GoToNextVertexComponent _goToNextVertexComponent = null!;
        protected CollisionRecoveryComponent _collisionRecoveryComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);
            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        protected Vertex NextVertex(Vertex currentVertex)
        {
            // We have reached our target. Create a new path.
            if (_pathStep == _currentPath.Count)
            {
                CreatePath(currentVertex);
            }

            return _currentPath[_pathStep++];
        }

        private void CreatePath(Vertex currentVertex)
        {
            // Create a path to the vertex with the highest idleness
            _pathStep = 0;
            _currentPath = AStar(currentVertex, HighestIdle(currentVertex));
            if (_currentPath.Count > 1)
            {
                _currentPath.Remove(_currentPath.First());
            }

            var lastVertex = _currentPath.Last();

            OccupyVertex(Controller.Id, lastVertex);
        }

        private Vertex HighestIdle(Vertex currentVertex)
        {
            var currentRobotPosition = currentVertex.Position;

            // excluding the vertices other agents are pathing towards and the current vertex
            var availableVertices = GetUnoccupiedVertices(Controller.Id)
                .Where(v => v != currentVertex)
                .ToList();

            // Get last visited ticks and order by idleness (ascending)
            var vertexIdleness = GetLastTimeVisitedTick(availableVertices.Select(v => v.Id)).ToList();

            // Get the most idle tick value
            var minIdlenessTick = vertexIdleness.Min(info => info.lastTimeVisitedTick);

            // Filter the vertices that have the highest idleness
            var mostIdleVertices = vertexIdleness
                .Where(info => info.lastTimeVisitedTick == minIdlenessTick)
                .Select(info => availableVertices.Single(v => v.Id == info.vertexId));

            // Return the closest vertex among the most idle vertices
            return mostIdleVertices
                .OrderBy(v => Vector2Int.Distance(currentRobotPosition, v.Position))
                .First();
        }

        private List<Vertex> AStar(Vertex start, Vertex target)
        {
            // Check if path is already cached
            if (_pathsCache.TryGetValue((start.Id, target.Id), out var cachedPath))
            {
                return cachedPath.ToList();
            }
            if (_pathsCache.TryGetValue((target.Id, start.Id), out var cachedReversePath))
            {
                return cachedReversePath.Reverse().ToList();
            }

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
                    var optimalPath = ReconstructPath(cameFrom, current);
                    _pathsCache[(start.Id, target.Id)] = optimalPath.ToArray();
                    return optimalPath;
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

        public abstract IEnumerable<(int vertexId, int lastTimeVisitedTick)> GetLastTimeVisitedTick(IEnumerable<int> vertexIds);
        public abstract void OccupyVertex(int robotId, Vertex vertex);
        public abstract IEnumerable<Vertex> GetUnoccupiedVertices(int robotId);
    }
}