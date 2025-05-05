using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;

namespace Maes.Utilities
{
    public static class PartitionsExtensions
    {
        public struct PossibleMeetingPoint
        {
            public readonly int Robot1Id;
            public readonly int Robot2Id;
            public readonly List<(Vertex, Vertex)> Connections;

            public PossibleMeetingPoint(int robot1Id, int robot2Id)
            {
                Robot1Id = robot1Id;
                Robot2Id = robot2Id;
                Connections = new List<(Vertex, Vertex)>();
            }
        }

        public static List<PossibleMeetingPoint> GetNeighborsPartitionsWithNoCommonVertices(this Dictionary<int, PartitionInfo> partitionInfoByRobotId, IReadOnlyCollection<Vertex> vertices)
        {
            var possibleMeetingPoints = new List<PossibleMeetingPoint>();

            var combinationOfPartitions = partitionInfoByRobotId.Values.Combinations();
            foreach (var (partitionInfo1, partitionInfo2) in combinationOfPartitions)
            {
                if (partitionInfo1.VertexIds.Any(v => partitionInfo2.VertexIds.Contains(v)))
                {
                    continue;
                }

                var possibleMeetingPoint = GetPossibleMeetingPointsForPartitions(partitionInfo1, partitionInfo2, vertices);
                if (possibleMeetingPoint.Connections.Count > 0)
                {
                    possibleMeetingPoints.Add(possibleMeetingPoint);
                }
            }

            return possibleMeetingPoints;
        }

        private static PossibleMeetingPoint GetPossibleMeetingPointsForPartitions(PartitionInfo partitionInfo1, PartitionInfo partitionInfo2, IReadOnlyCollection<Vertex> vertices)
        {
            var possibleMeetingPoint = new PossibleMeetingPoint(partitionInfo1.RobotId, partitionInfo2.RobotId);
            var partition1Vertices = vertices.Where(vertex => partitionInfo1.VertexIds.Contains(vertex.Id));
            foreach (var vertex in partition1Vertices)
            {
                foreach (var neighborVertex in vertex.Neighbors)
                {
                    if (partitionInfo2.VertexIds.Contains(neighborVertex.Id)) // Check if the neighbor is in the second partition
                    {
                        possibleMeetingPoint.Connections.Add((vertex, neighborVertex));
                    }
                }
            }
            return possibleMeetingPoint;
        }
    }
}