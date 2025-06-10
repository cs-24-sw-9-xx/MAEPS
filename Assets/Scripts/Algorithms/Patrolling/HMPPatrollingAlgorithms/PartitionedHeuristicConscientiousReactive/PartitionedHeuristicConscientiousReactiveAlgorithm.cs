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

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;
using Maes.Utilities;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.PartitionedHeuristicConscientiousReactive
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class PartitionedHeuristicConscientiousReactiveAlgorithm : PatrollingAlgorithm
    {
        public PartitionedHeuristicConscientiousReactiveAlgorithm(int seed)
        {
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
        }
        public override string AlgorithmName => "HMPAlgorithm";
        public PartitionInfo PartitionInfo => _partitionComponent.PartitionInfo!;
        public override Dictionary<int, Color32[]> ColorsByVertexId => _partitionComponent.PartitionInfo?
                                                                           .VertexIds
                                                                           .ToDictionary(vertexId => vertexId, _ => new[] { Controller.Color }) ?? new Dictionary<int, Color32[]>();

        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;

        private PartitionComponent _partitionComponent = null!;
        private GoToNextVertexComponent _goToNextVertexComponent = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _partitionComponent = new PartitionComponent(controller, GeneratePartitions);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);

            return new IComponent[] { _partitionComponent, _goToNextVertexComponent };
        }

        private Vertex GetInitialVertexToPatrol()
        {
            var vertices = PatrollingMap.Vertices.Where(vertex => PartitionInfo.VertexIds.Contains(vertex.Id)).ToArray();

            return vertices.GetClosestVertex(target => Controller.EstimateTimeToTarget(target, dependOnBrokenBehaviour: false) ?? int.MaxValue);
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            var suggestedVertex = _heuristicConscientiousReactiveLogic.NextVertex(currentVertex, currentVertex.Neighbors.Where(vertex => PartitionInfo.VertexIds.Contains(vertex.Id)).ToArray());

            return suggestedVertex;
        }

        private float DistanceMethod(Vertex source, Vertex target)
        {
            if (PatrollingMap.Paths.TryGetValue((source.Id, target.Id), out var path))
            {
                return path.Sum(p => Vector2Int.Distance(p.Start, p.End));
            }

            throw new Exception($"Path from {source.Id} to {target.Id} not found");
        }

        private Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var map = Controller.SlamMap.CoarseMap;
            var collisionMap = MapUtilities.MapToBitMap(map);

            var vertexIdByPosition = PatrollingMap.Vertices.ToDictionary(v => v.Position, v => v.Id);

            var vertexPositions = vertexIdByPosition.Keys.ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(collisionMap, vertexPositions);
            var clusters =
                SpectralBisectionPartitioningGenerator.Generator(distanceMatrix, vertexPositions, robotIds.Count);

            var vertexIdsPartitions = clusters.Select(vertexPoints => vertexPoints.Select(point => vertexIdByPosition[point]).ToHashSet()).ToArray();

            var hmpPartitionsById = new Dictionary<int, PartitionInfo>();
            var i = 0;
            foreach (var partition in vertexIdsPartitions)
            {
                hmpPartitionsById[i] = new PartitionInfo(i, partition);
                i++;
            }

            return hmpPartitionsById;
        }
    }
}