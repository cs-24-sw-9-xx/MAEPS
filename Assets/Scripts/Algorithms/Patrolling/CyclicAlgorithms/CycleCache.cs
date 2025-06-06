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
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Cache for the patrolling cycle to prevent each robot from running the heavy cycle generation.
    /// This cache is only used when the map only has one partition.
    /// </summary>
    public static class CycleCache
    {
        /// <summary>
        /// Cache for the patrolling cycle of each partition.
        /// The key is the position of each vertex in the partition, and the value is the cycle of vertices in that partition.
        /// </summary>
        private static Dictionary<List<Vector2Int>, List<int>> _cachedCycles { get; } = new();

        private static readonly HashSet<int> _robotIds = new();

        /// <summary>
        /// Clears the cache.
        /// </summary>
        private static void ClearCache()
        {
            _cachedCycles.Clear();
            _robotIds.Clear();
        }

        public static List<Vertex> GetOrCreatePatrollingCycle(Vertex startVertex, PatrollingMap patrollingMap, int robotId, Func<Vertex, List<Vertex>> createCycleFunc)
        {
            var verticesInPartition = patrollingMap.Vertices.Where(v => v.Partition == startVertex.Partition).ToList();

            // If the same robotId is asking again, then it is a new scenario, so we clear the cache.
            if (_robotIds.Contains(robotId))
            {
                ClearCache();
                _robotIds.Add(robotId);
            }

            // We only deal in vertex ids to ensure no shared references to vertices.
            var vertexIdsInPartition = verticesInPartition.ConvertAll(v => v.Id);

            // Use the positions of the vertices in the partition as the key for the cache.
            var verticesPositions = verticesInPartition.Select(v => v.Position).ToList();

            if (_cachedCycles.TryGetValue(verticesPositions, out var cachedCycle))
            {
                return ReconstructCycle(cachedCycle, verticesInPartition);
            }

            // If no cached cycle exists for this partition, then either:
            // 1. The robot is in a different partition, or
            // 2. We are in a new scenario.
            // In either case, we need to clear the cache and create a new cycle.
            ClearCache();
            var cycle = createCycleFunc(startVertex);
            _cachedCycles[verticesPositions] = cycle.ConvertAll(v => v.Id);
            return ReconstructCycle(_cachedCycles[verticesPositions], verticesInPartition);
        }

        private static List<Vertex> ReconstructCycle(List<int> cycleIds, List<Vertex> verticesInPartition)
        {
            return cycleIds.Select(id => verticesInPartition.Single(v => v.Id == id)).ToList();
        }
    }
}