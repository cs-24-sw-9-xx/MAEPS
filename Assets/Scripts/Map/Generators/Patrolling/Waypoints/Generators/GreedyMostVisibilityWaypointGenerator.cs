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
        private static readonly Vector2Int Top = new Vector2Int(0, 1);
        private static readonly Vector2Int TopRight = new Vector2Int(1, 1);
        private static readonly Vector2Int Right = new Vector2Int(1, 0);
        private static readonly Vector2Int BottomRight = new Vector2Int(1, -1);
        private static readonly Vector2Int Bottom = new Vector2Int(0, -1);
        private static readonly Vector2Int BottomLeft = new Vector2Int(-1, -1);
        private static readonly Vector2Int Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int TopLeft = new Vector2Int(-1, 1);

        private const float WallClosePenalty = 0.9f;

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
        /// <param name="inaccurateButFast">Whether or not to be inaccurate but fast.</param>
        /// <returns></returns>
        public static HashSet<Vector2Int> VertexPositionsFromMap(Bitmap map, float maxDistance = 0f, bool inaccurateButFast = true)
        {
            return WaypointGeneratorCache<GreedyMostVisibilityWaypointGeneratorCacheKey>.Cached(
                map,
                () => VertexPositionsFromMapComputation(map, maxDistance, inaccurateButFast),
                hash =>
                {
                    hash.Append(maxDistance);
                    hash.Append(inaccurateButFast ? 1 : 0);
                    return hash;
                });
        }

        private static HashSet<Vector2Int> VertexPositionsFromMapComputation(Bitmap map, float maxDistance, bool inaccurateButFast)
        {
            var precomputedVisibility = ComputeVisibility(map, maxDistance, inaccurateButFast);
            return ComputeVertexCoordinates(map, precomputedVisibility);
        }

        private sealed class UncoveredTilesSetComparer : IComparer<Vector2Int>
        {
            private readonly Dictionary<Vector2Int, int> _precomputedVisibility;
            private readonly int _width;

            public UncoveredTilesSetComparer(Dictionary<Vector2Int, Bitmap> precomputedVisibility)
            {
                _precomputedVisibility = precomputedVisibility.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
                _width = precomputedVisibility.Values.First().Width;
            }

            public int Compare(Vector2Int x, Vector2Int y)
            {
                var visibility = _precomputedVisibility[y] - _precomputedVisibility[x];
                if (visibility != 0)
                {
                    return visibility;
                }

                return (y.y * _width + y.x) - (x.y * _width + x.x);
            }
        }

        private static HashSet<Vector2Int> ComputeVertexCoordinates(Bitmap map, Dictionary<Vector2Int, Bitmap> precomputedVisibility)
        {
            var startTime = Time.realtimeSinceStartup;

            var guardPositions = new HashSet<Vector2Int>();

            using var uncoveredTiles = new Bitmap(map.Width, map.Height);
            var uncoveredTilesSet = new SortedSet<Vector2Int>(precomputedVisibility.Keys, new UncoveredTilesSetComparer(precomputedVisibility));
            foreach (var uncoveredTile in precomputedVisibility.Keys)
            {
                uncoveredTiles.Set(uncoveredTile.x, uncoveredTile.y);
            }

            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new Bitmap(0, 0);
                var bestCoverageScore = 0f;

                var foundCandidate = false;

                foreach (var uncoveredTile in uncoveredTilesSet)
                {
                    var candidate = precomputedVisibility[uncoveredTile];
                    if (candidate.Count <= bestCoverage.Count * WallClosePenalty)
                    {
                        break;
                    }

                    var coverage = Bitmap.Intersection(uncoveredTiles, candidate);
                    var candidateScore = ScoreCandidate(uncoveredTile, map, coverage);

                    if (candidateScore > bestCoverageScore)
                    {
                        bestGuardPosition = uncoveredTile;
                        bestCoverage.Dispose();
                        bestCoverage = coverage;
                        bestCoverageScore = candidateScore;
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

        private static float ScoreCandidate(Vector2Int position, Bitmap map, Bitmap visibilityMap)
        {
            var wallClose = IsWall(position + Top, map)
                            || IsWall(position + TopRight, map)
                            || IsWall(position + Right, map)
                            || IsWall(position + BottomRight, map)
                            || IsWall(position + Bottom, map)
                            || IsWall(position + BottomLeft, map)
                            || IsWall(position + Left, map)
                            || IsWall(position + TopLeft, map);

            return visibilityMap.Count * (wallClose ? WallClosePenalty : 1f);
        }

        private static bool IsWall(Vector2Int position, Bitmap map)
        {
            if (position.x < 0 || position.y < 0 || position.x >= map.Width || position.y >= map.Height)
            {
                return true;
            }

            return map.Contains(position);
        }

        internal static Dictionary<Vector2Int, Bitmap> ComputeVisibility(Bitmap map, float maxDistance, bool inaccurateButFast)
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
                    InaccurateButFast = inaccurateButFast
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

        private sealed class GreedyMostVisibilityWaypointGeneratorCacheKey { }
    }
}