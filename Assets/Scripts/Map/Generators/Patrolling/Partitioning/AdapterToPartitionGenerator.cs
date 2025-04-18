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

using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning
{
    public class AdapterToPartitionGenerator : BasePartitionGenerator
    {
        public AdapterToPartitionGenerator(PartitioningGenerator.PartitioningGeneratorDelegate partitioningGenerator)
        {
            _partitioningGenerator = partitioningGenerator;
        }

        private readonly PartitioningGenerator.PartitioningGeneratorDelegate _partitioningGenerator;

        public override Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var vertexIdByPosition = _patrollingMap.Vertices.ToDictionary(v => v.Position, v => v.Id);

            var vertexPositions = vertexIdByPosition.Keys.ToList();
            var distanceMatrix = MapUtilities.CalculateDistanceMatrix(_collisionMap, vertexPositions);
            var clusters = _partitioningGenerator(distanceMatrix, vertexPositions, robotIds.Count);

            var vertexIdsPartitions = clusters.Values.Select(vertexPoints => vertexPoints.Select(point => vertexIdByPosition[point]).ToHashSet()).ToArray();

            var i = 0;
            var partitionInfoByRobotId = robotIds.ToDictionary(id => id, id => new PartitionInfo(id, vertexIdsPartitions[i++]));

            return partitionInfoByRobotId;
        }
    }
}