using System.Collections.Generic;
using System.Linq;

using Maes.Robot;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public sealed class PartitionGeneratorWithMeetingPoint : IPartitionGenerator<PartitionInfo>
    {
        private readonly IPartitionGenerator<PartitionInfo> _partitionGenerator;
        private PatrollingMap _patrollingMap = null!;

        public PartitionGeneratorWithMeetingPoint(IPartitionGenerator<PartitionInfo> partitionGenerator)
        {
            _partitionGenerator = partitionGenerator;
        }

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
        {
            _patrollingMap = patrollingMap;
            _partitionGenerator.SetMaps(patrollingMap, coarseMap, robotConstraints);
        }

        public Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitions = _partitionGenerator.GeneratePartitions(robotIds);
            return AddMissingMeetingPointsForNeighborPartitions(partitions);
        }

        private Dictionary<int, PartitionInfo> AddMissingMeetingPointsForNeighborPartitions(Dictionary<int, PartitionInfo> partitions)
        {
            var neighborsPartitionsWithNoCommonVertices = partitions.GetNeighborsPartitionsWithNoCommonVertices(_patrollingMap);
            foreach (var meetingPoint in neighborsPartitionsWithNoCommonVertices)
            {
                var shortestConnection = meetingPoint.Connections[0];
                var shortestDistance = SquaredDistanceOfConnection(shortestConnection);

                foreach (var connection in meetingPoint.Connections.Skip(1))
                {
                    var distance = SquaredDistanceOfConnection(connection);

                    if (!(distance < shortestDistance))
                    {
                        continue;
                    }

                    shortestConnection = connection;
                    shortestDistance = distance;
                }

                // Add the vertex id of the best connection to the partition with the smallest amount of vertices, such that vertex would be the shared meeting point
                // This is done to only extend the partition with the smallest amount of vertices
                var robotIdWithSmallestPartition = partitions[meetingPoint.Robot1Id].VertexIds.Count < partitions[meetingPoint.Robot2Id].VertexIds.Count
                    ? meetingPoint.Robot1Id
                    : meetingPoint.Robot2Id;

                // One of the partitions would already contain the vertex id of the best connection, so that will be a redundant since duplicates can not happen because of HashSet
                partitions[robotIdWithSmallestPartition].VertexIds.Add(shortestConnection.Item1.Id);
                partitions[robotIdWithSmallestPartition].VertexIds.Add(shortestConnection.Item2.Id);
            }

            return partitions;
        }

        private double SquaredDistanceOfConnection((Vertex, Vertex) connection)
        {
            return _patrollingMap.SquaredDistanceBetweenVertices(connection.Item1.Id, connection.Item2.Id);
        }
    }
}