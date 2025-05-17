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
            var precomputedVisibility = VisibilityCache.ComputeVisibilityCached(map, maxDistance);
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
    }
}