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

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// THIS ALGORITHMS HAS FACTORIAL TIME-COMPLEXITY OF THE SIZE OF THE ROBOTS PARTITION!
    /// Implementation of the Single Cycle algorithm but using TSP: https://doi.org/10.1007/978-3-540-28645-5_48.
    /// An implementation can be found here: https://github.com/matteoprata/DRONET-for-Patrolling/blob/main_july_2023/src/patrolling/tsp_cycle.py
    /// </summary>
    public sealed class SingleCycleTSP : PatrollingAlgorithm
    {
        public override string AlgorithmName => "SingleCycle Algorithm";

        // Set by CreateComponents
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private CollisionRecoveryComponent _collisionRecoveryComponent = null!;
        private List<Vertex> _patrollingCycle = new();

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap);
            _collisionRecoveryComponent = new CollisionRecoveryComponent(controller, _goToNextVertexComponent);

            return new IComponent[] { _goToNextVertexComponent, _collisionRecoveryComponent };
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            if (_patrollingCycle.Count == 0)
            {
                _patrollingCycle = CreatePatrollingCycle(currentVertex);
            }
            return NextVertexInCycle(currentVertex);
        }

        private Vertex NextVertexInCycle(Vertex currentVertex)
        {
            // Get the next vertex in the cycle of _patrollingCycle
            var currentIndex = _patrollingCycle.IndexOf(currentVertex);
            var nextIndex = (currentIndex + 1) % _patrollingCycle.Count;
            return _patrollingCycle[nextIndex];
        }

        /// <summary>
        /// Use TSP to make the optimal cycle of all vertices in this robots partition
        /// </summary>
        /// <param name="startVertex"></param>
        private List<Vertex> CreatePatrollingCycle(Vertex startVertex)
        {
            var verticesInPartition = _patrollingMap.Vertices.Where(v => v.Partition == startVertex.Partition).ToList();
            var bestPath = new List<Vertex>();
            var bestDistance = float.MaxValue;
            var allPermutations = GetPermutations(verticesInPartition.Skip(1).ToList()); // Fix first vertex
            Debug.Log($"Found {verticesInPartition.Count} vertices in partition {startVertex.Partition}. Found {allPermutations.Count()} permutations.");
            foreach (var perm in allPermutations)
            {
                var path = new List<Vertex> { verticesInPartition[0] }; // Start at first vertex
                path.AddRange(perm);
                path.Add(verticesInPartition[0]); // Return to start

                var dist = _controller.TravelEstimator.EstimatePathLength(path) ?? throw new System.Exception($"No path found between vertices. Not all vertices in the partition are reachable from each other.");
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestPath = path;
                }
            }
            // Return the best path, but remove the last vertex, which is the start vertex.
            return bestPath.Take(_patrollingMap.Vertices.Count - 1).ToList();
        }

        private IEnumerable<List<Vertex>> GetPermutations(List<Vertex> list)
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