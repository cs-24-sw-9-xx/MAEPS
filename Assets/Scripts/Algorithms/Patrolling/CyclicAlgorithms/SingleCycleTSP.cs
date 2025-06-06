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

using System.Collections.Generic;
using System.Linq;

using Maes.Map;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// THIS ALGORITHMS HAS FACTORIAL TIME-COMPLEXITY OF THE SIZE OF THE ROBOTS PARTITION!
    /// For reference (on my machine):
    /// For partitions of size 9, it takes 2.5 seconds to compute the cycle.
    /// For partitions of size 10, it takes 15 seconds to compute the cycle.
    /// Implementation of the Single Cycle algorithm but using exact TSP solver instead christofides as in: https://doi.org/10.1007/978-3-540-28645-5_48.
    /// An implementation can be found here: https://github.com/matteoprata/DRONET-for-Patrolling/blob/main_july_2023/src/patrolling/tsp_cycle.py
    /// </summary>
    public sealed class SingleCycleTSP : BaseCyclicAlgorithm
    {
        public override string AlgorithmName => "SingleCycleTSP Algorithm";

        /// <summary>
        /// Use exact TSP solver to make the optimal cycle of all vertices in this robots partition.
        /// </summary>
        /// <param name="startVertex"></param>
        protected override List<Vertex> CreatePatrollingCycle(Vertex startVertex)
        {
            var verticesInPartition = PatrollingMap.Vertices.Where(v => v.Partition == startVertex.Partition).ToList();
            var bestPath = new List<Vertex>();
            var bestDistance = float.MaxValue;
            var estimatedDistanceMatrix = EstimatedDistanceMatrix(verticesInPartition);
            var allPermutations = GetPermutations(verticesInPartition.Skip(1).ToList()); // Fix first vertex
            Debug.Log($"Found {verticesInPartition.Count} vertices in partition {startVertex.Partition}. Found {allPermutations.Count()} permutations.");
            foreach (var perm in allPermutations)
            {
                var path = new List<Vertex> { verticesInPartition[0] }; // Start at first vertex
                path.AddRange(perm);
                path.Add(verticesInPartition[0]); // Return to start

                var dist = PathLength(path, estimatedDistanceMatrix);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestPath = path;
                }
            }
            // Return the best path, but remove the last vertex, which is the same as the start vertex.
            return bestPath.Take(verticesInPartition.Count).ToList();
        }

        private static IEnumerable<List<Vertex>> GetPermutations(IReadOnlyList<Vertex> list)
        {
            if (list.Count == 0)
            {
                yield return new List<Vertex>();
            }
            else
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var rest = list.Take(i).Concat(list.Skip(i + 1)).ToList();
                    foreach (var perm in GetPermutations(rest))
                    {
                        perm.Insert(0, list[i]);
                        yield return perm;
                    }
                }
            }
        }
    }
}