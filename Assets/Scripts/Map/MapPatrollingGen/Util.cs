using System.Collections.Generic;

using UnityEngine;
namespace Maes.Map.MapPatrollingGen
{
    public static class Util
    {
        /// <summary>
        /// Calculate the distance matrix between all verticies
        /// </summary>
        /// <param name="map"></param>
        /// <param name="verticies"></param>
        /// <returns></returns>
        public static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(bool[,] map, List<Vector2Int> verticies)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var vertex in verticies)
            {
                BreathFirstSearch(vertex, shortestGridPath, verticies, map);
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

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        // BFS to find the shortest path from the start position to all other vertices
        private static void BreathFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, List<Vector2Int> guardPositions, bool[,] map)
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