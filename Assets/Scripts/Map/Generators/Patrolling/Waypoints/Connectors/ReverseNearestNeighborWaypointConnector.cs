// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Linq;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Connectors
{
    public static class ReverseNearestNeighborWaypointConnector
    {
        /// <summary>
        /// Connect vertices by reverse nearest neighbors.
        /// Connect islands of vertices by connecting the closest vertices between islands, to ensure a connected graph. 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="colorIslands">Color the islands, to ease debugging.</param>
        /// <param name="defaultColor"></param>
        /// <param name="nextId">Used by partitioning.</param>
        /// <param name="numberOfReverseNearestNeighbors">The amount of RNN's to connect(make an edge) to the current vertex.</param>
        /// <returns>Vertices with connections(edges) to other vertices.</returns>
        public static Vertex[] ConnectVertices(Bitmap map, IReadOnlyCollection<Vector2Int> vertexPositions, int nextId = 0, int numberOfReverseNearestNeighbors = 1)
        {
            var startTime = Time.realtimeSinceStartup;
            var vertices = vertexPositions.Select(position => new Vertex(nextId++, position)).ToArray();

            ConnectVertices(vertices, map, numberOfReverseNearestNeighbors);

            Debug.LogFormat($"{nameof(ReverseNearestNeighborWaypointConnector)} ConnectVertices took {0} s", Time.realtimeSinceStartup - startTime);

            return vertices;
        }

        public static void ConnectVertices(IReadOnlyCollection<Vertex> vertices, Bitmap map, int numberOfReverseNearestNeighbors = 1)
        {
            var vertexPositions = vertices.Select(v => v.Position).ToArray();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);

            ConnectReverseNearestNeighbors(vertices, distanceMatrix, numberOfReverseNearestNeighbors);

            ConnectIslands(vertices, distanceMatrix);
        }

        public static void ConnectReverseNearestNeighbors(IReadOnlyCollection<Vertex> vertices, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, int numberOfReverseNearestNeighbors = 1)
        {
            var reverseNearestNeighbors = MapUtilities.GetReverseNearestNeighbors(distanceMatrix, numberOfReverseNearestNeighbors);

            var vertexMap = vertices.ToDictionary(v => v.Position);

            foreach (var (position, neighbors) in reverseNearestNeighbors)
            {
                if (!vertexMap.TryGetValue(position, out var vertex))
                {
                    continue;
                }
                foreach (var neighborPos in neighbors)
                {
                    if (vertexMap.TryGetValue(neighborPos, out var neighborVertex))
                    {
                        vertex.AddNeighbor(neighborVertex);
                        neighborVertex.AddNeighbor(vertex);
                    }
                }
            }
        }

        private static void ConnectIslands(
                 IReadOnlyCollection<Vertex> vertices,
                 Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            var visited = new HashSet<Vertex>();
            var clusters = new List<List<Vertex>>();

            // Identify disconnected clusters dynamically
            foreach (var vertex in vertices)
            {
                if (!visited.Contains(vertex))
                {
                    clusters.Add(TraverseCluster(vertex, visited));
                }
            }

            if (clusters.Count > 1)
            {
                // Start with the first cluster as the initial island
                var initialIsland = clusters[0];
                clusters.RemoveAt(0);

                // Recursively merge all clusters into one
                MergeIslandsRecursively(initialIsland, clusters, distanceDict);
            }

            return;

            // Function to traverse a cluster and collect its vertices
            static List<Vertex> TraverseCluster(Vertex start, HashSet<Vertex> visited)
            {
                var cluster = new List<Vertex>();
                var stack = new Stack<Vertex>();
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (visited.Add(current))
                    {
                        cluster.Add(current);

                        foreach (var neighbor in current.Neighbors)
                        {
                            if (!visited.Contains(neighbor))
                            {
                                stack.Push(neighbor);
                            }
                        }
                    }
                }

                return cluster;
            }
        }

        // Merge islands (isolated connected vertices) recursively until all islands are connected
        private static List<Vertex> MergeIslandsRecursively(
            List<Vertex> currentIsland,
            List<List<Vertex>> remainingIslands,
            Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            if (remainingIslands.Count == 0)
            {
                return currentIsland; // Base case: no more islands to merge
            }

            List<Vertex>? closestIsland = null;
            Vertex? closestVertexInCurrent = null;
            Vertex? closestVertexInNew = null;
            var minDistance = int.MaxValue;

            // Find the closest island and vertices to merge
            foreach (var island in remainingIslands)
            {
                foreach (var currentVertex in currentIsland)
                {
                    foreach (var newVertex in island)
                    {
                        var distance = distanceDict[(currentVertex.Position, newVertex.Position)];
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestVertexInCurrent = currentVertex;
                            closestVertexInNew = newVertex;
                            closestIsland = island;
                        }
                    }
                }
            }

            // Connect the closest vertices between the current island and the closest island
            if (closestIsland != null && closestVertexInCurrent != null && closestVertexInNew != null)
            {
                closestVertexInCurrent.AddNeighbor(closestVertexInNew);
                closestVertexInNew.AddNeighbor(closestVertexInCurrent);

                // Merge the closest island into the current island
                currentIsland.AddRange(closestIsland);
                remainingIslands.Remove(closestIsland);

                // Recursive call to merge the next closest island
                return MergeIslandsRecursively(currentIsland, remainingIslands, distanceDict);
            }

            return currentIsland; // Return the current island if no more connections can be made
        }
    }
}