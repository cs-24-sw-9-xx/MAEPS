using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;

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
        /// Calculate the distance matrix between all vertices
        /// </summary>
        /// <param name="map"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static Dictionary<(Vector2Int, Vector2Int), int> CalculateDistanceMatrix(Bitmap map, IReadOnlyCollection<Vector2Int> vertices)
        {
            Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath = new();

            foreach (var vertex in vertices)
            {
                BreadthFirstSearch(vertex, shortestGridPath, vertices, map);
            }

            return shortestGridPath;
        }

        // Function to check if a position is within bounds and walkable
        private static bool IsWalkable(Vector2Int pos, Bitmap map)
        {
            return pos.x >= 0 && pos.x < map.Width &&
                   pos.y >= 0 && pos.y < map.Height &&
                   !map.Contains(pos.x, pos.y); // true if the position is not a wall
        }

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        // BFS to find the distance of the shortest path from the start position to all other vertices
        private static void BreadthFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, IReadOnlyCollection<Vector2Int> vertexPositions, Bitmap map)
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

        // Find the k nearest neighbors for each point and return the reverse mapping
        public static Dictionary<Vector2Int, List<Vector2Int>> FindReverseNearestNeighbors(Dictionary<(Vector2Int, Vector2Int), int> distanceDict, int k)
        {
            // Dictionary to store the k nearest neighbors of each point
            var nearestNeighbors = new Dictionary<Vector2Int, List<(Vector2Int, int)>>();

            // Populate nearest neighbors with distances for each start point
            foreach (var ((start, end), distance) in distanceDict)
            {
                if (!nearestNeighbors.ContainsKey(start))
                {
                    nearestNeighbors[start] = new List<(Vector2Int, int)>();
                }

                var neighborsList = nearestNeighbors[start];
                neighborsList.Add((end, distance));

                // Sort by distance and keep only the top k nearest neighbors
                neighborsList.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                if (neighborsList.Count > k)
                {
                    neighborsList.RemoveAt(neighborsList.Count - 1); // Remove the farthest neighbor
                }
            }

            // Reverse nearest neighbor mapping
            var reverseNearestNeighbors = new Dictionary<Vector2Int, List<Vector2Int>>();

            foreach (var (start, neighborsList) in nearestNeighbors)
            {
                foreach (var (neighbor, _) in neighborsList)
                {
                    if (!reverseNearestNeighbors.ContainsKey(neighbor))
                    {
                        reverseNearestNeighbors[neighbor] = new List<Vector2Int>();
                    }
                    reverseNearestNeighbors[neighbor].Add(start);
                }
            }

            return reverseNearestNeighbors;
        }
    }
}