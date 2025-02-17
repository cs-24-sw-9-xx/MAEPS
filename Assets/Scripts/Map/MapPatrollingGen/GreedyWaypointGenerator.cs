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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Maes.Map.MapGen;
using Maes.Utilities;

using UnityEngine;

using static Maes.Map.PatrollingMap;

namespace Maes.Map.MapPatrollingGen
{
    public static class GreedyWaypointGenerator
    {
        public static PatrollingMap MakePatrollingMap(SimulationMap<Tile> simulationMap, bool colorIslands, bool useOptimizedLOS = true)
        {
            VisibilityMethod visibilityAlgorithm = useOptimizedLOS ? LineOfSightUtilities.ComputeVisibilityOfPointFastBreakColumn : LineOfSightUtilities.ComputeVisibilityOfPoint;
            var map = MapUtilities.MapToBitMap(simulationMap);
            var vertexPositions = TSPHeuresticSolver(map, visibilityAlgorithm);
            var distanceMatrix = Util.CalculateDistanceMatrix(map, vertexPositions);
            var connectVertices = WaypointConnection.ConnectVertices(vertexPositions, distanceMatrix, colorIslands);
            return new PatrollingMap(connectVertices, simulationMap, visibilityAlgorithm);
        }

        /// <summary>
        /// Greedy algorithm to solve the Travelling Salesman Problem (TSP) using a local optimization heuristic
        /// </summary>
        /// <param name="map"></param>
        /// <param name="visibilityAlgorithm"></param>
        /// <returns></returns>
        public static List<Vector2Int> TSPHeuresticSolver(bool[,] map, VisibilityMethod visibilityAlgorithm)
        {
            var precomputedVisibility = ComputeVisibility(map, visibilityAlgorithm);
            var guardPositions = ComputeVertexCoordinates(precomputedVisibility);
            return guardPositions;
        }

        private static List<Vector2Int> ComputeVertexCoordinates(Dictionary<Vector2Int, HashSet<Vector2Int>> precomputedVisibility)
        {
            var guardPositions = new List<Vector2Int>();
            var uncoveredTiles = precomputedVisibility.Keys.ToHashSet();

            // Greedy algorithm to find the best guard positions
            while (uncoveredTiles.Count > 0)
            {
                var bestGuardPosition = Vector2Int.zero;
                var bestCoverage = new HashSet<Vector2Int>();

                // Find the guard position that covers the most uncovered tiles
                var orderedCandidates = uncoveredTiles.OrderByDescending(t => precomputedVisibility[t].Count);
                foreach (var candidate in orderedCandidates)
                {
                    if (precomputedVisibility[candidate].Count < bestCoverage.Count)
                    {
                        break;
                    }
                    var coverage = new HashSet<Vector2Int>(precomputedVisibility[candidate]);
                    coverage.IntersectWith(uncoveredTiles);

                    if (coverage.Count > bestCoverage.Count ||
                       (coverage.Count == bestCoverage.Count &&
                        AverageEuclideanDistance(candidate, guardPositions) < AverageEuclideanDistance(bestGuardPosition, guardPositions)))
                    {
                        bestGuardPosition = candidate;
                        bestCoverage = coverage;
                    }
                }

                guardPositions.Add(bestGuardPosition);
                uncoveredTiles.ExceptWith(bestCoverage);
            }

            return guardPositions;
        }

        private static Dictionary<Vector2Int, HashSet<Vector2Int>> ComputeVisibility(bool[,] map, VisibilityMethod visibilityAlgorithm)
        {
            var precomputedVisibility = new ConcurrentDictionary<Vector2Int, HashSet<Vector2Int>>();
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            // Outermost loop parallelized to improve performance
            Parallel.For(0, width, x =>
            {
                for (var y = 0; y < height; y++)
                {
                    var tile = new Vector2Int(x, y);
                    if (!map[x, y])
                    {
                        // Precompute visibility for each tile
                        precomputedVisibility[tile] = visibilityAlgorithm(tile, map);
                    }
                }
            });
            // To debug the ComputeVisibility method, use the following utility method to save as image
            // SaveAsImage.SaveVisibileTiles();

            return precomputedVisibility.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Helper method to calculate the average Euclidean distance of a guard position to a list of other guard positions
        private static float AverageEuclideanDistance(Vector2Int guardPosition, List<Vector2Int> currentGuardPositions)
        {
            var sum = 0f;
            foreach (var pos in currentGuardPositions)
            {
                sum += Vector2Int.Distance(guardPosition, pos);
            }

            return sum / currentGuardPositions.Count;
        }
    }
}