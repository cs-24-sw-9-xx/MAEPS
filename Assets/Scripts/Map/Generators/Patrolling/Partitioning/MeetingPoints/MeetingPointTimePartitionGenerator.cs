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

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class MeetingPointTimePartitionGenerator : IHMPPartitionGenerator
    {
        private readonly PartitionGeneratorWithMeetingPoint _partitionGeneratorWithMeetings;
        private PatrollingMap _patrollingMap = null!;
        private EstimateTimeDelegate _estimateTime = null!;
        private EstimateTimeToTargetDelegate _estimateTimeToTarget = null!;


        public MeetingPointTimePartitionGenerator(IPartitionGenerator<PartitionInfo> partitionGenerator)
        {
            _partitionGeneratorWithMeetings = new PartitionGeneratorWithMeetingPoint(partitionGenerator);
        }

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap)
        {
            _patrollingMap = patrollingMap;
            _partitionGeneratorWithMeetings.SetMaps(patrollingMap, coarseMap);
        }

        public void SetEstimates(EstimateTimeDelegate estimateTime, EstimateTimeToTargetDelegate estimateTimeToTarget)
        {
            _estimateTime = estimateTime;
            _estimateTimeToTarget = estimateTimeToTarget;
        }

        public Dictionary<int, HMPPartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitionsById = _partitionGeneratorWithMeetings.GeneratePartitions(robotIds);

            var meetingRobotIdsByVertexId = FindMeetingRobotsAtMeetingPoints(partitionsById.Values.ToArray());

            var meetingPointsByPartitionId = GetMeetingPointsByPartitionId(meetingRobotIdsByVertexId, partitionsById);

            var hmpPartitionsById = new Dictionary<int, HMPPartitionInfo>();
            foreach (var (robotId, partitionInfo) in partitionsById)
            {
                hmpPartitionsById[robotId] = new HMPPartitionInfo(partitionInfo, meetingPointsByPartitionId[robotId]);
            }

            return hmpPartitionsById;
        }

        private static Dictionary<int, HashSet<int>> FindMeetingRobotsAtMeetingPoints(PartitionInfo[] partitions)
        {
            var meetingPointVertexByVertexId = new Dictionary<int, HashSet<int>>();

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
                        meetingPoint = new HashSet<int>();
                        meetingPointVertexByVertexId[vertexId] = meetingPoint;
                    }

                    meetingPoint.Add(partition1.RobotId);
                    meetingPoint.Add(partition2.RobotId);
                }
            }

            return meetingPointVertexByVertexId;
        }

        private Dictionary<int, List<MeetingPoint>> GetMeetingPointsByPartitionId(
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId, Dictionary<int, PartitionInfo> partitionsById)
        {
            var globalMeetingIntervalTicks = GetGlobalMeetingIntervalTicks(partitionsById, meetingRobotIdsByVertexId);
            var tickColorAssignment = new WelshPowellMeetingPointColorer(meetingRobotIdsByVertexId).Run();

            var startMeetingAfterTicks = GetWhenToStartMeeting(partitionsById.Values);

            var meetingPointsByPartitionId = new Dictionary<int, List<MeetingPoint>>();
            foreach (var (vertexId, meetingRobotIds) in meetingRobotIdsByVertexId)
            {
                var meetingPoint = new MeetingPoint(vertexId, globalMeetingIntervalTicks, startMeetingAfterTicks + globalMeetingIntervalTicks * tickColorAssignment[vertexId], meetingRobotIds);
                foreach (var robotId in meetingRobotIds)
                {
                    if (!meetingPointsByPartitionId.TryGetValue(robotId, out var meetingPoints))
                    {
                        meetingPoints = new List<MeetingPoint>();
                        meetingPointsByPartitionId[robotId] = meetingPoints;
                    }

                    meetingPoints.Add(meetingPoint);
                }
            }

            return meetingPointsByPartitionId;
        }

        private int GetGlobalMeetingIntervalTicks(Dictionary<int, PartitionInfo> partitionsById,
            Dictionary<int, HashSet<int>> meetingRobotIdsByVertexId)
        {
            var estimatedPartitionMeetingIntervalTicks = new List<int>();
            foreach (var (robotId, partitionInfo) in partitionsById)
            {
                var numberOfMeetingPoints = meetingRobotIdsByVertexId
                    .Count(m => m.Value.Contains(robotId));
                var estimated = EstimatePartitionMeetingIntervalTicks(partitionInfo, numberOfMeetingPoints);
                estimatedPartitionMeetingIntervalTicks.Add(estimated);
            }

            return estimatedPartitionMeetingIntervalTicks.Max();
        }

        private int EstimatePartitionMeetingIntervalTicks(PartitionInfo partition, int numberOdMeetingPoints)
        {
            var maxTravelTime = EstimateMaxTravelTimeForPartition(partition);
            return 2 * (int)Math.Ceiling((double)partition.VertexIds.Count / numberOdMeetingPoints) * maxTravelTime;
        }

        private int EstimateMaxTravelTimeForPartition(PartitionInfo partition)
        {
            var maxTicks = 0;

            var vertexPositions = _patrollingMap.Vertices
                .Where(v => partition.VertexIds.Contains(v.Id))
                .Select(v => v.Position)
                .ToArray();

            foreach (var (vertexPosition1, vertexPosition2) in vertexPositions.Combinations())
            {
                var ticks = _estimateTime(vertexPosition1, vertexPosition2);
                if (ticks == null)
                {
                    continue;
                }

                maxTicks = Mathf.Max(maxTicks, ticks.Value);
            }

            return maxTicks;
        }

        private int GetWhenToStartMeeting(IEnumerable<PartitionInfo> partitionInfos)
        {
            var startMeetingAfterTicks = 0;

            foreach (var partitionInfo in partitionInfos)
            {
                int? timeToClosestVertex = null;
                foreach (var vertex in _patrollingMap.Vertices.Where(v => partitionInfo.VertexIds.Contains(v.Id)))
                {
                    var timeToTarget = _estimateTimeToTarget(vertex.Position) ?? int.MaxValue;
                    if (timeToClosestVertex == null || timeToTarget < timeToClosestVertex)
                    {
                        timeToClosestVertex = timeToTarget;
                    }
                }

                if (timeToClosestVertex > startMeetingAfterTicks)
                {
                    startMeetingAfterTicks = timeToClosestVertex.Value;
                }
            }

            return startMeetingAfterTicks;
        }
    }
}