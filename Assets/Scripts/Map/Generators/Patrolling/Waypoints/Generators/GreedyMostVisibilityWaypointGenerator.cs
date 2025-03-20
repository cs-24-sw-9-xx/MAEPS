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

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Waypoints.Generators
{
    public static class GreedyMostVisibilityWaypointGenerator
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap, WaypointConnector.WaypointConnectorDelegate waypointConnector, float maxDistance = 0f)
        {
            using var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositions = VertexPositionsFromMap(map, maxDistance);
            var connectedVertices = waypointConnector(map, vertexPositions.Keys);
            return new PatrollingMap(connectedVertices, simulationMap, vertexPositions);
        }

        /// <summary>
        /// Greedy algorithm to solve the art gallery problem using a local optimization heuristic.
        /// The result is not the optimal solution, but a good approximation. 
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Dictionary<Vector2Int, Bitmap> VertexPositionsFromMap(Bitmap map, float maxDistance = 0f)
        {
            var precomputedVisibility = ComputeVisibility(map, maxDistance);
            return ComputeVertexCoordinates(map, precomputedVisibility);
        }

        private static Dictionary<Vector2Int, Bitmap> ComputeVertexCoordinates(Bitmap map, Dictionary<Vector2Int, Bitmap> precomputedVisibility)
        {
            var startTime = Time.realtimeSinceStartup;

            var guardPositions = new Dictionary<Vector2Int, Bitmap>();

            using var uncoveredTiles = new Bitmap(0, 0, map.Width, map.Height);
            var uncoveredTilesSet = precomputedVisibility.Keys.ToHashSet();
            foreach (var uncoveredTile in precomputedVisibility.Keys)
            {
                uncoveredTiles.Set(uncoveredTile.x, uncoveredTile.y);
            }

            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new Bitmap(0, 0, 0, 0);
                var bestCandidate = new Bitmap(0, 0, 0, 0);

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
                        bestCandidate = candidate;
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

                guardPositions.Add(bestGuardPosition, bestCandidate);
                uncoveredTiles.ExceptWith(bestCoverage);
                uncoveredTilesSet.ExceptWith(bestCoverage);

                bestCoverage.Dispose();
            }

            Debug.LogFormat("Greedy guard positions took {0} seconds", Time.realtimeSinceStartup - startTime);

            return guardPositions;
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