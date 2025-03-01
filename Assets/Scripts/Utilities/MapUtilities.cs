using System.Collections.Generic;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.MapGen;

using UnityEngine;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        [MustDisposeResource]
        public static Bitmap MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new Bitmap(0, 0, simulationMap.WidthInTiles, simulationMap.HeightInTiles);
            for (var x = 0; x < simulationMap.WidthInTiles; x++)
            {
                for (var y = 0; y < simulationMap.HeightInTiles; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (Tile.IsWall(firstTri.Type))
                    {
                        map.Set(x, y);
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Calculate the distance matrix between all verticies
        /// </summary>
        /// <param name="map"></param>
        /// <param name="verticies"></param>
        /// <returns></returns>
        public static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(Bitmap map, Dictionary<Vector2Int, Bitmap> verticies)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var (vertex, _) in verticies)
            {
                BreathFirstSearch(vertex, shortestGridPath, verticies, map);
            }

            return shortestGridPath;
        }

        public static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(Bitmap map, List<Vector2Int> verticies)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var vertex in verticies)
            {
                BreathFirstSearch(vertex, shortestGridPath, verticies, map);
            }

            return shortestGridPath;
        }

        // Function to check if a position is within bounds and walkable
        private static bool IsWalkable(Vector2Int pos, Bitmap map)
        {
            return pos.x >= 0 && pos.x < map.Width &&
                   pos.y >= 0 && pos.y < map.Height &&
                   !map[pos.x, pos.y]; // true if the position is not a wall
        }

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        // BFS to find the shortest path from the start position to all other vertices
        private static void BreathFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, Dictionary<Vector2Int, Bitmap> vertexPositions, Bitmap map)
        {
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

                foreach (var direction in Directions)
                {
                    var neighbor = current + direction;

                    if (IsWalkable(neighbor, map) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        distanceMap[neighbor] = currentDistance + 1;

                        if (vertexPositions.ContainsKey(neighbor))
                        {
                            shortestGridPath[(startPosition, neighbor)] = distanceMap[neighbor];
                        }
                    }
                }
            }
        }

        private static void BreathFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, List<Vector2Int> vertexPositions, Bitmap map)
        {
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

                foreach (var direction in Directions)
                {
                    var neighbor = current + direction;

                    if (IsWalkable(neighbor, map) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        distanceMap[neighbor] = currentDistance + 1;

                        if (vertexPositions.Contains(neighbor))
                        {
                            shortestGridPath[(startPosition, neighbor)] = distanceMap[neighbor];
                        }
                    }
                }
            }
        }

    }
}