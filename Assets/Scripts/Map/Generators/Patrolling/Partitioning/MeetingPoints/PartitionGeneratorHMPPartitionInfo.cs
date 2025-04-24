using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Robot;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class PartitionGeneratorHMPPartitionInfo : IPartitionGenerator<HMPPartitionInfo>
    {
        private readonly PartitionGeneratorWithMeetingPoint _partitionGenerator;
        private PatrollingMap _patrollingMap = null!;
        private EstimationTravel _estimationTravel = null!;

        public PartitionGeneratorHMPPartitionInfo(IPartitionGenerator<PartitionInfo> partitionGenerator)
        {
            _partitionGenerator = new PartitionGeneratorWithMeetingPoint(partitionGenerator);
        }

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
        {
            _patrollingMap = patrollingMap;
            _partitionGenerator.SetMaps(patrollingMap, coarseMap, robotConstraints);
            _estimationTravel = new EstimationTravel(coarseMap, robotConstraints);
        }

        public Dictionary<int, HMPPartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitionsById = _partitionGenerator.GeneratePartitions(robotIds);

            var meetingPoints = GetMeetingPoints(partitionsById.Values.ToArray());
            var hmpPartitionsById = new Dictionary<int, HMPPartitionInfo>();
            foreach (var (robotId, partitionInfo) in partitionsById)
            {
                var robotMeetingPoints = meetingPoints
                    .Where(m => m.RobotIds.Contains(robotId))
                    .ToList();
                hmpPartitionsById.Add(robotId, new HMPPartitionInfo(partitionInfo, robotMeetingPoints));
            }

            var a = hmpPartitionsById.Values.Select(GetMinTicksBetweenMeetingPoints).ToArray();

            var globalTimeToNextMeeting = hmpPartitionsById.Values.Select(GetMinTicksBetweenMeetingPoints).Max();

            var colorByVertexId = new WelshPowellColoringVertexSolver(meetingPoints).Run();
            foreach (var meetingPoint in meetingPoints)
            {
                meetingPoint.AtTicks = colorByVertexId[meetingPoint.VertexId] * globalTimeToNextMeeting;
            }

            return hmpPartitionsById;
        }

        private static MeetingPoint[] GetMeetingPoints(PartitionInfo[] partitions)
        {
            var meetingPointVertexByVertexId = new Dictionary<int, MeetingPoint>();

            foreach (var (partition1, partition2) in partitions.Combinations())
            {
                var intersectionVertexIds =
                    partition1.VertexIds
                        .Intersect(partition2.VertexIds)
                        .ToArray();

                if (intersectionVertexIds.Length == 0)
                {
                    continue;
                }

                foreach (var vertexId in intersectionVertexIds)
                {
                    if (!meetingPointVertexByVertexId.TryGetValue(vertexId, out var meetingPoint))
                    {
                        meetingPoint = new MeetingPoint(vertexId, -1);
                        meetingPointVertexByVertexId[vertexId] = meetingPoint;
                    }

                    meetingPoint.RobotIds.Add(partition1.RobotId);
                    meetingPoint.RobotIds.Add(partition2.RobotId);
                }
            }

            return meetingPointVertexByVertexId.Values.ToArray();
        }

        private int GetMinTicksBetweenMeetingPoints(HMPPartitionInfo partition)
        {
            var maxTravelTime = GetMaxTravelTimeForPartition(partition);
            return 2 * (int)Math.Ceiling((double)partition.VertexIds.Count / partition.MeetingPoints.Count) * maxTravelTime;
        }

        private int GetMaxTravelTimeForPartition(HMPPartitionInfo partition)
        {
            var maxTicks = 0;

            var vertexPositions = _patrollingMap.Vertices
                .Where(v => partition.VertexIds.Contains(v.Id))
                .Select(v => v.Position)
                .ToArray();

            foreach (var (vertexPosition1, vertexPosition2) in vertexPositions.Combinations())
            {
                var ticks = _estimationTravel.EstimateTime(vertexPosition1, vertexPosition2);
                if (ticks == null)
                {
                    continue;
                }

                maxTicks = Mathf.Max(maxTicks, ticks.Value);
            }

            return maxTicks;
        }
    }
}