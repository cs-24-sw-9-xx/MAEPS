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

using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Utilities;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public static class GreedyMostVisibilityWaypointGenerator
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap, WaypointConnectorDelegate waypointConnector, float maxDistance = 0f)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositions = VertexPositionsFromMap(map, maxDistance);
            var connectedVertices = waypointConnector(map, vertexPositions);
            return new PatrollingMap(connectedVertices, simulationMap);
        }

        /// <summary>
        /// Greedy algorithm to solve the art gallery problem using a local optimization heuristic.
        /// The result is not the optimal solution, but a good approximation. 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="maxDistance">The maximum range of a waypoint.</param>
        /// <returns></returns>
        public static HashSet<Vector2Int> VertexPositionsFromMap(Bitmap map, float maxDistance = 0f)
        {
            var precomputedVisibility = ComputeVisibility(map, maxDistance);
            return ComputeVertexCoordinates(map, precomputedVisibility);
        }

        private static HashSet<Vector2Int> ComputeVertexCoordinates(Bitmap map, Dictionary<Vector2Int, Bitmap> precomputedVisibility)
        {
            var startTime = Time.realtimeSinceStartup;

            var guardPositions = new HashSet<Vector2Int>();

            using var uncoveredTiles = new Bitmap(map.Width, map.Height);
            var uncoveredTilesSet = precomputedVisibility.Keys.ToHashSet();
            foreach (var uncoveredTile in precomputedVisibility.Keys)
            {
                uncoveredTiles.Set(uncoveredTile.x, uncoveredTile.y);
            }

            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new Bitmap(0, 0);

                var foundCandidate = false;

                foreach (var uncoveredTile in uncoveredTilesSet.OrderByDescending(t => precomputedVisibility[t].Count))
                {
                    var candidate = precomputedVisibility[uncoveredTile];
                    if (candidate.Count <= bestCoverage.Count)
                    {
                        break;
                    }

                    var coverage = Bitmap.Intersection(uncoveredTiles, candidate);

                    if (coverage.Count > bestCoverage.Count)
                    {
                        bestGuardPosition = uncoveredTile;
                        bestCoverage.Dispose();
                        bestCoverage = coverage;
                        foundCandidate = true;
                    }
                    else
                    {
                        coverage.Dispose();
                    }
                }

                if (!foundCandidate)
                {
                    Debug.LogErrorFormat("Found no candidates. Missing: {0}", string.Join(", ", uncoveredTiles));
                    Debug.LogErrorFormat("Missing candidates in hashmap: {0}", string.Join(", ", uncoveredTilesSet));
                    break;
                }

                guardPositions.Add(bestGuardPosition);
                uncoveredTiles.ExceptWith(bestCoverage);
                uncoveredTilesSet.ExceptWith(bestCoverage);

                bestCoverage.Dispose();
            }

            foreach (var bitmap in precomputedVisibility.Values)
            {
                bitmap.Dispose();
            }

            Debug.LogFormat("Greedy guard positions took {0} seconds", Time.realtimeSinceStartup - startTime);

            return guardPositions;
        }

        internal static Dictionary<Vector2Int, Bitmap> ComputeVisibility_Improved(Bitmap map, float maxDistance = 0f)
        {
            var precomputedVisibilities = new Dictionary<Vector2Int, Bitmap>();
            // Calculate visibility for each tile
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    if (map.Contains(x, y))
                    {
                        continue;
                    }

                    var tile = new Vector2Int(x, y);

                    // Calculate visibility for the current tile
                    precomputedVisibilities[tile] = VisibilityOfPoint(map, maxDistance, x, y, tile);
                }
            }
            return precomputedVisibilities;
        }

        private static Bitmap VisibilityOfPoint(Bitmap map, float maxDistance, int x_start, int y_start, Vector2Int tile)
        {
            var visibility = new Bitmap(map.Width, map.Height);
            var unexplored = new Queue<(int x, int y, int incX, int incY)>();
            unexplored.Enqueue((x_start, y_start, 1, 1));
            unexplored.Enqueue((x_start, y_start, -1, 1));
            unexplored.Enqueue((x_start, y_start, 1, -1));
            unexplored.Enqueue((x_start, y_start, -1, -1));
            while (unexplored.Count > 0)
            {
                var (x, y, incX, incY) = unexplored.Dequeue();

                if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                {
                    continue;
                }

                // If it is a wall, skip it
                if (map.Contains(x, y))
                {
                    continue;
                }

                // If it is already explored and cannot lead to unexplored tiles, skip it
                if (visibility.Contains(x, y) && x != x_start && y != y_start)
                {
                    continue;
                }

                // If it is out of range, skip it
                if (maxDistance != 0f && x * x + y * y > maxDistance * maxDistance)
                {
                    continue;
                }

                // Check line of sight
                //if (!HasLineOfSight(map.Width, map.Height, x_start, y_start, x, y, map))
                //{
                //    continue;
                //}

                visibility.Set(x, y);
                unexplored.Enqueue((x + incX, y, incX, incY));
                unexplored.Enqueue((x, y + incY, incX, incY));
            }
            return visibility;
        }

        public static bool HasLineOfSight(
                int width,
                int height,
                int originX,
                int originY,
                int endX,
                int endY,
                Bitmap map)
        {
            var x = originX;
            var y = originY;

            var diffX = endX - originX;
            var diffY = endY - originY;

            var stepX = math.sign(diffX);
            var stepY = math.sign(diffY);

            var angle = (float)math.atan2(-diffY, diffX);

            var cosAngle = (float)math.cos(angle);
            var sinAngle = (float)math.sin(angle);

            var tMaxX = 0.5f / cosAngle;
            var tMaxY = 0.5f / sinAngle;

            var tDeltaX = tMaxX * 2.0f;
            var tDeltaY = tMaxY * 2.0f;

            var manhattanDistance = math.abs(diffX) + math.abs(diffY);

            for (var t = 0; t < manhattanDistance; t++)
            {
                if (math.abs(tMaxX) < math.abs(tMaxY))
                {
                    tMaxX += tDeltaX;
                    x += stepX;
                }
                else
                {
                    tMaxY += tDeltaY;
                    y += stepY;
                }

                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return false;
                }

                if (map.Contains(x, y))
                {
                    return false;
                }
            }

            return true;
        }

        internal static Dictionary<Vector2Int, Bitmap> ComputeVisibility(Bitmap map, float maxDistance = 0f)
        {
            var startTime = Time.realtimeSinceStartup;

            var nativeMap = map.ToNativeArray();
            var nativeVisibilities = new NativeArray<ulong>[map.Width];

            for (var i = 0; i < nativeVisibilities.Length; i++)
            {
                nativeVisibilities[i] = new NativeArray<ulong>(nativeMap.Length * map.Height, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            }

            var jobs = new JobHandle[map.Width];

            // Outermost loop parallelized to improve performance
            for (var x = 0; x < map.Width; x++)
            {
                var job = new VisibilityJob()
                {
                    Width = map.Width,
                    Height = map.Height,
                    X = x,
                    Map = nativeMap,
                    Visibility = nativeVisibilities[x],
                    MaxDistance = maxDistance,
                };

                jobs[x] = job.Schedule();
            }

            foreach (var job in jobs)
            {
                job.Complete();
            }

            var precomputedVisibilities = new Dictionary<Vector2Int, Bitmap>();

            for (var i = 0; i < nativeVisibilities.Length; i++)
            {
                var bitmaps = Bitmap.FromNativeArray(map.Width, map.Height, nativeMap.Length, nativeVisibilities[i]);
                for (var y = 0; y < map.Height; y++)
                {
                    var bitmap = bitmaps[y];
                    if (bitmap.Any)
                    {
                        var tile = new Vector2Int(i, y);
                        precomputedVisibilities[tile] = bitmap;
                    }
                    else
                    {
                        bitmap.Dispose();
                    }
                }

                nativeVisibilities[i].Dispose();
            }


            nativeMap.Dispose();


            // To debug the ComputeVisibility method, use the following utility method to save as image
            // SaveAsImage.SaveVisibileTiles();

            Debug.LogFormat("Compute visibility took {0} seconds", Time.realtimeSinceStartup - startTime);


            return precomputedVisibilities;
        }
    }
}