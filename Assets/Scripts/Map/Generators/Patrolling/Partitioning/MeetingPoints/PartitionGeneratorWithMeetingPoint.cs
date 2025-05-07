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
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;
using System.Linq;

using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Utilities;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public sealed class PartitionGeneratorWithMeetingPoint : IPartitionGenerator<PartitionInfo>
    {
        private readonly IPartitionGenerator<PartitionInfo> _partitionGenerator;
        private PatrollingMap _patrollingMap = null!;
        private Vertex[] _verticesReverseNearestNeighbors = null!;

        public PartitionGeneratorWithMeetingPoint(IPartitionGenerator<PartitionInfo> partitionGenerator)
        {
            _partitionGenerator = partitionGenerator;
        }

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap)
        {
            _patrollingMap = patrollingMap;

            using var bitmap = MapUtilities.MapToBitMap(coarseMap);
            _verticesReverseNearestNeighbors = patrollingMap.Vertices.Select(v => new Vertex(v.Id, v.Position, v.Partition, v.Color)).ToArray();
            ReverseNearestNeighborWaypointConnector.ConnectVertices(_verticesReverseNearestNeighbors, bitmap);

            _partitionGenerator.SetMaps(patrollingMap, coarseMap);
        }

        public Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitions = _partitionGenerator.GeneratePartitions(robotIds);
            return AddMissingMeetingPointsForNeighborPartitions(partitions);
        }

        private Dictionary<int, PartitionInfo> AddMissingMeetingPointsForNeighborPartitions(Dictionary<int, PartitionInfo> partitions)
        {
            var neighborsPartitionsWithNoCommonVertices = partitions.GetNeighborsPartitionsWithNoCommonVertices(_verticesReverseNearestNeighbors);
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
                var partitionToReplace = partitions[robotIdWithSmallestPartition];
                var newVertexIds = new HashSet<int>(partitionToReplace.VertexIds)
                {
                    shortestConnection.Item1.Id, shortestConnection.Item2.Id
                };
                partitions[robotIdWithSmallestPartition] = new PartitionInfo(partitionToReplace.RobotId, newVertexIds);
            }

            return partitions;
        }

        private double SquaredDistanceOfConnection((Vertex, Vertex) connection)
        {
            return _patrollingMap.SquaredDistanceBetweenVertices(connection.Item1.Id, connection.Item2.Id);
        }
    }
}