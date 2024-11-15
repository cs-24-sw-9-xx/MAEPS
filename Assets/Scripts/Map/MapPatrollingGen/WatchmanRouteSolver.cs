using System.Collections.Generic;

using Maes.Map.MapGen;

using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    public static class WatchmanRouteSolver
    {
        public static List<Vertex> MakePatrollingMap(SimulationMap<Tile> simulationMap)
        {
            var Stopwatch = new System.Diagnostics.Stopwatch();
            Stopwatch.Start();
            var map = MakeBitMap(simulationMap);
            Stopwatch.Stop();
            Debug.Log("MakeBitMap: " + Stopwatch.Elapsed.TotalSeconds);

            Stopwatch.Restart();
            var guardPositions = SolveWatchmanRoute(map);
            Stopwatch.Stop();
            Debug.Log("Watchman route: " + Stopwatch.Elapsed.TotalSeconds);

            Stopwatch.Restart();
            var distanceMatrix = DistanceMatrixCalculator.CalculateDistanceMatrix(map, guardPositions);
            Stopwatch.Stop();
            Debug.Log("Distance matrix: " + Stopwatch.Elapsed.TotalSeconds);

            return guardPositions.ConvertAll(pos => new Vertex(0, pos));
        }

        private static bool[,] MakeBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new bool[simulationMap.WidthInTiles, simulationMap.HeightInTiles];
            for (var x = 0; x < simulationMap.WidthInTiles; x++)
            {
                for (var y = 0; y < simulationMap.HeightInTiles; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    map[x, y] = Tile.IsWall(firstTri.Type);
                }
            }

            return map;
        }

        private static List<Vector2Int> SolveWatchmanRoute(bool[,] map)
        {
            var guardPositions = new List<Vector2Int>();
            var uncoveredTiles = new HashSet<Vector2Int>();
            var precomputedVisibility = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

            // Collect all floor tiles
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (!map[x, y])
                    {
                        var tile = new Vector2Int(x, y);
                        uncoveredTiles.Add(tile);

                        // Precompute visibility for each tile
                        precomputedVisibility[tile] = ComputeVisibility(tile, map);
                    }
                }
            }

            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new HashSet<Vector2Int>();

                foreach (var candidate in uncoveredTiles)
                {
                    var coverage = new HashSet<Vector2Int>(precomputedVisibility[candidate]);
                    coverage.IntersectWith(uncoveredTiles);

                    if (coverage.Count > bestCoverage.Count ||
                       (coverage.Count == bestCoverage.Count &&
                        AveargeEuclideanDistance(candidate, guardPositions) < AveargeEuclideanDistance(bestGuardPosition, guardPositions)))
                    {
                        bestGuardPosition = candidate;
                        bestCoverage = coverage;
                    }
                }

                guardPositions.Add(bestGuardPosition);
                foreach (var coveredTile in bestCoverage)
                {
                    uncoveredTiles.Remove(coveredTile);
                }
            }
            return guardPositions;
        }

        // Precompute visibility using an efficient line-drawing algorithm
        private static HashSet<Vector2Int> ComputeVisibility(Vector2Int start, bool[,] map)
        {
            var visibilitySet = new HashSet<Vector2Int>();
            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!map[x, y] && IsInLineOfSight(start, target, map))
                    {
                        visibilitySet.Add(target);
                    }
                }
            }
            return visibilitySet;
        }

        private static float AveargeEuclideanDistance(Vector2Int guardPosition, List<Vector2Int> currentGuardPositions)
        {
            var sum = 0f;
            foreach (var pos in currentGuardPositions)
            {
                sum += Vector2Int.Distance(guardPosition, pos);
            }

            return sum / currentGuardPositions.Count;
        }

        // Method to check visibility using a line-of-sight algorithm
        private static bool IsInLineOfSight(Vector2Int start, Vector2Int end, bool[,] map)
        {
            // Implement Bresenham's line algorithm for visibility check
            // Return true if there is a clear line-of-sight, otherwise false
            var dx = Mathf.Abs(end.x - start.x);
            var dy = Mathf.Abs(end.y - start.y);
            var sx = start.x < end.x ? 1 : -1;
            var sy = start.y < end.y ? 1 : -1;
            var err = dx - dy;

            var x = start.x;
            var y = start.y;

            while (x != end.x || y != end.y)
            {
                if (map[x, y])
                {
                    return false; // Hit a wall
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }
    }

    public static class DistanceMatrixCalculator
    {

        public static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(bool[,] map, List<Vector2Int> guardPositions)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var guardPosition in guardPositions)
            {
                BFS(guardPosition, shortestGridPath, guardPositions, map);
            }

            return shortestGridPath;
        }

        // Function to check if a position is within bounds and walkable
        private static bool IsWalkable(Vector2Int pos, bool[,] map)
        {
            return pos.x >= 0 && pos.x < map.GetLength(0) &&
                   pos.y >= 0 && pos.y < map.GetLength(1) &&
                   !map[pos.x, pos.y]; // true if the position is not a wall
        }

        // Run BFS to find the shortest path from the start position to all other guard positions
        private static void BFS(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, List<Vector2Int> guardPositions, bool[,] map)
        {
            var directions = new Vector2Int[]
            {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
            };

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var distanceMap = new Dictionary<Vector2Int, int>();

            queue.Enqueue(startPosition);
            visited.Add(startPosition);
            distanceMap[startPosition] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distanceMap[current];

                foreach (var direction in directions)
                {
                    var neighbor = current + direction;

                    if (IsWalkable(neighbor, map) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        distanceMap[neighbor] = currentDistance + 1;

                        if (guardPositions.Contains(neighbor))
                        {
                            shortestGridPath[(startPosition, neighbor)] = distanceMap[neighbor];
                        }
                    }
                }
            }
        }
    }
}
