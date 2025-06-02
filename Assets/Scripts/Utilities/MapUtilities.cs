using System.Collections.Generic;

using JetBrains.Annotations;

using Maes.Map;
using Maes.Map.Generators;

using UnityEngine;

namespace Maes.Utilities
{
    public static class MapUtilities
    {
        private static readonly Vector2Int[] Directions = {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        [MustDisposeResource]
        public static Bitmap MapToBitMap(SimulationMap<Tile> simulationMap)
        {
            var map = new Bitmap(simulationMap.WidthInTiles, simulationMap.HeightInTiles);
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

        [MustDisposeResource]
        public static Bitmap MapToBitMap(CoarseGrainedMap coarseGrainedMap, bool beOptimistic = false)
        {
            var bitmap = new Bitmap(coarseGrainedMap.Width, coarseGrainedMap.Height);
            for (var x = 0; x < coarseGrainedMap.Width; x++)
            {
                for (var y = 0; y < coarseGrainedMap.Height; y++)
                {
                    var tileStatus = coarseGrainedMap.GetTileStatus(new Vector2Int(x, y), beOptimistic);
                    if (tileStatus == SlamMap.SlamTileStatus.Solid || (tileStatus == SlamMap.SlamTileStatus.Unseen && !beOptimistic))
                    {
                        bitmap.Set(x, y);
                    }
                }
            }

            return bitmap;
        }

        // Find the k nearest neighbors for each point and return the reverse mapping
        public static Dictionary<Vector2Int, List<Vector2Int>> GetReverseNearestNeighbors(Dictionary<(Vector2Int, Vector2Int), int> distanceDict, int k)
        {
            var nearestNeighbors = GetKNearestNeighbors(distanceDict, k);

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

        public static Dictionary<Vector2Int, List<(Vector2Int position, int distance)>> GetKNearestNeighbors(Dictionary<(Vector2Int, Vector2Int), int> distanceDict, int k)
        {
            // Dictionary to store the k nearest neighbors of each point
            var nearestNeighbors = new Dictionary<Vector2Int, List<(Vector2Int position, int distance)>>();

            // Populate nearest neighbors with distances for each start point
            foreach (var ((start, end), distance) in distanceDict)
            {
                if (!nearestNeighbors.ContainsKey(start))
                {
                    nearestNeighbors[start] = new List<(Vector2Int position, int distance)>();
                }

                var neighborsList = nearestNeighbors[start];
                neighborsList.Add((end, distance));

                // Sort by distance and keep only the top k nearest neighbors
                neighborsList.Sort((a, b) => a.distance.CompareTo(b.distance));
                if (neighborsList.Count > k)
                {
                    neighborsList.RemoveAt(neighborsList.Count - 1); // Remove the farthest neighbor
                }
            }

            return nearestNeighbors;
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
            var verticesSet = new HashSet<Vector2Int>(vertices);

            foreach (var vertex in vertices)
            {
                BreadthFirstSearch(vertex, shortestGridPath, verticesSet, map);
            }

            return shortestGridPath;
        }

        // BFS to find the distance of the shortest path from the start position to all other vertices
        private static void BreadthFirstSearch(Vector2Int startPosition, Dictionary<(Vector2Int, Vector2Int), int> shortestGridPath, HashSet<Vector2Int> vertexPositions, Bitmap map)
        {
            var queue = new Queue<Vector2Int>();
            using var visited = new Bitmap(map.Width, map.Height);
            var distanceMap = new Dictionary<Vector2Int, int>();

            queue.Enqueue(startPosition);
            visited.Set(startPosition.x, startPosition.y);
            distanceMap[startPosition] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = distanceMap[current];

                foreach (var direction in Directions)
                {
                    var neighbor = current + direction;

                    Debug.Assert(neighbor.x >= 0 && neighbor.x < map.Width);
                    Debug.Assert(neighbor.y >= 0 && neighbor.y < map.Height);

                    if (!map.Contains(neighbor.x, neighbor.y) && !visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Set(neighbor.x, neighbor.y);
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