using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

namespace Tests.EditModeTests.UtilitiesPartition
{
    public class TestPartitionGenerator : IPartitionGenerator<PartitionInfo>
    {
        public TestPartitionGenerator(Dictionary<int, HashSet<Vertex>> vertexPositionsByPartitionId)
        {
            _vertexPositionsByPartitionId = vertexPositionsByPartitionId;
        }
        
        private readonly Dictionary<int, HashSet<Vertex>> _vertexPositionsByPartitionId;

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
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