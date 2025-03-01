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

namespace Maes.Map.MapPatrollingGen
{
    public static class WaypointConnection
    {
        // Static variable to keep track of the current vertex id
        private static int currentVertexId = 0;

        public static Vertex[] ConnectVertices(Dictionary<Vector2Int, Bitmap> vertexPositions, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix, bool colorIslands)
        {
            var startTime = Time.realtimeSinceStartup;

            const int numberOfReverseNearestNeighbors = 1;
            var reverseNearestNeighbors = FindReverseNearestNeighbors(distanceMatrix, numberOfReverseNearestNeighbors);
            var vertices = vertexPositions.Select(pos => new Vertex(currentVertexId++, 0, pos.Key)).ToArray();
            var vertexMap = vertices.ToDictionary(v => v.Position);

            foreach (var (position, neighbors) in reverseNearestNeighbors)
            {
                if (!vertexMap.TryGetValue(position, out var vertex))
                {
                    continue;
                }

                var color = colorIslands ? Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f) : Color.green;
                vertex.Color = color;
                foreach (var neighborPos in neighbors)
                {
                    if (vertexMap.TryGetValue(neighborPos, out var neighborVertex))
                    {
                        neighborVertex.Color = color;
                        vertex.AddNeighbor(neighborVertex);
                        neighborVertex.AddNeighbor(vertex);
                    }
                }
            }

            ConnectIslands(vertices, distanceMatrix);

            Debug.LogFormat("Connect Vertices took {0} s", Time.realtimeSinceStartup - startTime);

            return vertices;
        }

        // Find the k nearest neighbors for each point and return the reverse mapping
        private static Dictionary<Vector2Int, List<Vector2Int>> FindReverseNearestNeighbors(Dictionary<(Vector2Int, Vector2Int), int> distanceDict, int k)
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
        private static void ConnectIslands(
                Vertex[] vertices,
                Dictionary<(Vector2Int, Vector2Int), int> distanceDict)
        {
            var visited = new HashSet<Vertex>();
            var clusters = new List<List<Vertex>>();

            // Identify disconnected clusters dynamically
            foreach (var vertex in vertices)
            {
                if (!visited.Contains(vertex))
                {
                    clusters.Add(TraverseCluster(vertex));
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
            List<Vertex> TraverseCluster(Vertex start)
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