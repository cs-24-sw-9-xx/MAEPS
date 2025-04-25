// Copyright 2025 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;

namespace Tests.EditModeTests.Utilities.Partitions
{
    public class TestPartitionGenerator : IPartitionGenerator<PartitionInfo>
    {
        public TestPartitionGenerator(Dictionary<int, HashSet<Vertex>> vertexPositionsByPartitionId)
        {
            _vertexPositionsByPartitionId = vertexPositionsByPartitionId;
        }

        private readonly Dictionary<int, HashSet<Vertex>> _vertexPositionsByPartitionId;

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, EstimateTimeDelegate estimateTime)
        {

        }

        public Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitionsById = new Dictionary<int, PartitionInfo>();

            foreach (var (partitionId, vertexPositions) in _vertexPositionsByPartitionId)
            {
                var vertexIds = vertexPositions.Select(v => v.Id).ToHashSet();
                var partitionInfo = new PartitionInfo(partitionId, vertexIds);
                partitionsById.Add(partitionId, partitionInfo);
            }

            return partitionsById;
        }
    }
}