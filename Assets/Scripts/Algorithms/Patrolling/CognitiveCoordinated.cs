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
    public sealed class CognitiveCoordinated : PatrollingAlgorithm
    {
        public override string AlgorithmName => "Cognitive Coordinated Algorithm";
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
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private IRobotController _controller = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, Coordinator.GlobalMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        public override void SetGlobalPatrollingMap(PatrollingMap globalMap)
        {
            base.SetGlobalPatrollingMap(globalMap);

            // Will be set multiple times.
            // Too bad!
            Coordinator.GlobalMap = globalMap;

            // We must clear Coordinators knowledge of occupied vertices as we have new robots, and we might just have started a new experiment.
            // This will break if robots are added mid-experiment.
            Coordinator.ClearOccupiedVertices();
        }

        private Vertex NextVertex(Vertex currentVertex)
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

            Coordinator.OccupyVertex(_controller.Id, lastVertex);
        }

        private Vertex HighestIdle(Vertex targetVertex)
        {
            // excluding the vertices other agents are pathing towards
            var availableVertices = Coordinator.GetUnoccupiedVertices(_controller.Id)
                .Where(vertex => vertex != targetVertex)
                .OrderBy(vertex => vertex.LastTimeVisitedTick)
                .ToList();

            var position = targetVertex.Position;
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

        // TODO: Find a better way to have a coordinator, so that it is not static.
        private static class Coordinator
        {
            public static PatrollingMap GlobalMap { get; set; } = null!; // Set by CognitiveCoordinated.SetGlobalPatrollingMap

            private static readonly Dictionary<int, Vertex> VerticesOccupiedByRobot = new();

            public static IEnumerable<Vertex> GetOccupiedVertices(int robotId)
            {
                return VerticesOccupiedByRobot
                    .Where(p => p.Key != robotId)
                    .Select(p => p.Value);
            }

            public static IEnumerable<Vertex> GetUnoccupiedVertices(int robotId)
            {
                var occupiedVertices = GetOccupiedVertices(robotId);
                return GlobalMap.Vertices.Except(occupiedVertices);
            }

            public static void OccupyVertex(int robotId, Vertex vertex)
            {
#if DEBUG
                if (!GlobalMap.Vertices.Contains(vertex))
                {
                    throw new ArgumentException("Vertex is not a part of GlobalMap.Vertices.", nameof(vertex));
                }
#endif

                VerticesOccupiedByRobot[robotId] = vertex;
            }

            public static void ClearOccupiedVertices()
            {
                VerticesOccupiedByRobot.Clear();
            }
        }
    }

}