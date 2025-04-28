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

namespace Maes.Algorithms.Patrolling
{
    /// <summary>
    /// Partition-based patrolling algorithm with the use of meeting points in a limited communication range.
    /// Proposed by Henrik, Mads, and Puvikaran. 
    /// </summary>
    public sealed class HMPPatrollingAlgorithm : PatrollingAlgorithm
    {
        public HMPPatrollingAlgorithm(IPartitionGenerator<HMPPartitionInfo> partitionGenerator, int seed = 0)
        {
            _partitionGenerator = partitionGenerator;
            _heuristicConscientiousReactiveLogic = new HeuristicConscientiousReactiveLogic(DistanceMethod, seed);
        }
        public override string AlgorithmName => "HMPAlgorithm";

        public PartitionInfo? PartitionInfo => _partitionComponent.PartitionInfo;

        public override Dictionary<int, Color32[]> ColorsByVertexId => _partitionComponent.PartitionInfo?
                                                                           .VertexIds
                                                                           .ToDictionary(vertexId => vertexId, _ => new[] { _controller.Color }) ?? new Dictionary<int, Color32[]>();

        private readonly IPartitionGenerator<HMPPartitionInfo> _partitionGenerator;
        private readonly HeuristicConscientiousReactiveLogic _heuristicConscientiousReactiveLogic;

        private readonly VirtualStigmergyComponent<int, HMPPartitionInfo>.OnConflictDelegate _onConflict = (_, _, incoming) => incoming;
        private VirtualStigmergyComponent<int, HMPPartitionInfo> _virtualStigmergyComponent = null!;
        private StartupComponent<Dictionary<int, HMPPartitionInfo>> _startupComponent = null!;
        private PartitionComponent _partitionComponent = null!;
        private GoToNextVertexComponent _goToNextVertexComponent = null!;
        private IRobotController _controller = null!;

        protected override IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _controller = controller;
            _partitionGenerator.SetMaps(patrollingMap, controller.SlamMap.CoarseMap, (s, e) => controller.TravelEstimator.EstimateTime(s, e));
            _startupComponent = new StartupComponent<Dictionary<int, HMPPartitionInfo>>(controller, _partitionGenerator.GeneratePartitions);
            _virtualStigmergyComponent = new VirtualStigmergyComponent<int, HMPPartitionInfo>(_onConflict, controller);
            _partitionComponent = new PartitionComponent(controller, _startupComponent, _virtualStigmergyComponent);
            _goToNextVertexComponent = new GoToNextVertexComponent(NextVertex, this, controller, patrollingMap, GetInitialVertexToPatrol);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent, _partitionComponent, _goToNextVertexComponent };
        }
        private Vertex GetInitialVertexToPatrol()
        {
            var partitionInfo = _partitionComponent.PartitionInfo!;
            var vertices = _patrollingMap.Vertices.Where(vertex => partitionInfo.VertexIds.Contains(vertex.Id)).ToArray();
            return vertices.GetClosestVertex(_controller.SlamMap.CoarseMap.GetCurrentPosition(dependOnBrokenBehavior: false));
        }

        private Vertex NextVertex(Vertex currentVertex)
        {
            var partitionInfo = _partitionComponent.PartitionInfo!;
            return _heuristicConscientiousReactiveLogic.NextVertex(currentVertex,
                currentVertex.Neighbors.Where(vertex => partitionInfo.VertexIds.Contains(vertex.Id)).ToArray());
        }

        private float DistanceMethod(Vertex source, Vertex target)
        {
            if (_patrollingMap.Paths.TryGetValue((source.Id, target.Id), out var path))
            {
                return path.Sum(p => Vector2Int.Distance(p.Start, p.End));
            }

            throw new Exception($"Path from {source.Id} to {target.Id} not found");
        }
    }
}