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

#nullable enable
using System.Collections.Generic;
using System.Linq;

using Maes.Utilities;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Connectors
{
    public static class CyclicConnector
    {
        /// <summary>
        /// Connect vertices into cycles.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="vertexPositions"></param>
        /// <param name="nextId">Used by partitioning.</param>
        /// <returns>Vertices with connections(edges) to other vertices.</returns>
        public static Vertex[] ConnectVertices(Bitmap map, IReadOnlyCollection<Vector2Int> vertexPositions, int nextId = 0)
        {
            var startTime = Time.realtimeSinceStartup;
            var vertices = vertexPositions.Select(position => new Vertex(nextId++, position)).ToArray();

            ConnectVertices(vertices, map);

            Debug.LogFormat($"{nameof(CyclicConnector)} ConnectVertices took {0} s", Time.realtimeSinceStartup - startTime);

            return vertices;
        }

        public static void ConnectVertices(IReadOnlyCollection<Vertex> vertices, Bitmap map)
        {
            var vertexPositions = vertices.Select(v => v.Position).ToArray();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(map, vertexPositions);

            ConnectAsCycle(vertices, distanceMatrix);
        }

        private static void ConnectAsCycle(IReadOnlyCollection<Vertex> vertices, Dictionary<(Vector2Int, Vector2Int), int> distanceMatrix)
        {
            var vertexList = vertices.ToList();
            var visited = new HashSet<Vertex>();
            var path = new List<Vertex>();

            // Start from the first vertex
            var current = vertexList[0];
            path.Add(current);
            visited.Add(current);

            while (visited.Count < vertexList.Count)
            {
                Vertex? next = null;
                var minDistance = int.MaxValue;

                foreach (var candidate in vertexList)
                {
                    if (visited.Contains(candidate))
                    {
                        continue;
                    }

                    var dist = distanceMatrix[(current.Position, candidate.Position)];
                    if (dist >= minDistance)
                    {
                        continue;
                    }

                    minDistance = dist;
                    next = candidate;
                }

                if (next == null)
                {
                    continue;
                }

                // Connect current to next
                current.AddNeighbor(next);
                next.AddNeighbor(current);
                path.Add(next);
                visited.Add(next);
                current = next;
            }

            // Connect last to first to complete the cycle
            var first = path[0];
            var last = path[^1];
            last.AddNeighbor(first);
            first.AddNeighbor(last);
        }
    }
}