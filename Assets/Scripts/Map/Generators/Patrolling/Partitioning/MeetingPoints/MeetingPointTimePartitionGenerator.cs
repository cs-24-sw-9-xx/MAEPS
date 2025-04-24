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

using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Robot;

using UnityEngine;

namespace Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints
{
    public class MeetingPointTimePartitionGenerator : IPartitionGenerator<HMPPartitionInfo>
    {
        private readonly PartitionGeneratorWithMeetingPoint _partitionGeneratorWithMeetings;
        private PatrollingMap _patrollingMap = null!;
        private TravelEstimator _travelEstimator = null!;

        public MeetingPointTimePartitionGenerator(IPartitionGenerator<PartitionInfo> partitionGenerator)
        {
            _partitionGeneratorWithMeetings = new PartitionGeneratorWithMeetingPoint(partitionGenerator);
        }

        public void SetMaps(PatrollingMap patrollingMap, CoarseGrainedMap coarseMap, RobotConstraints robotConstraints)
        {
            _patrollingMap = patrollingMap;
            _partitionGeneratorWithMeetings.SetMaps(patrollingMap, coarseMap, robotConstraints);
            _travelEstimator = new TravelEstimator(coarseMap, robotConstraints);
        }

        public Dictionary<int, HMPPartitionInfo> GeneratePartitions(HashSet<int> robotIds)
        {
            var partitionsById = _partitionGeneratorWithMeetings.GeneratePartitions(robotIds);

            var meetingPoints = FindSharedVertexMeetingPoints(partitionsById.Values.ToArray());

            var hmpPartitionsById = new Dictionary<int, HMPPartitionInfo>();
            foreach (var (robotId, partitionInfo) in partitionsById)
            {
                var robotMeetingPoints = meetingPoints
                    .Where(m => m.RobotIds.Contains(robotId))
                    .ToList();
                hmpPartitionsById.Add(robotId, new HMPPartitionInfo(partitionInfo, robotMeetingPoints));
            }

            var globalMeetingIntervalTicks = hmpPartitionsById.Values.Select(EstimatePartitionMeetingIntervalTicks).Max();
            var tickColorAssignment = new WelshPowellMeetingPointColorer(meetingPoints).Run();
            foreach (var meetingPoint in meetingPoints)
            {
                meetingPoint.AtTicks = tickColorAssignment[meetingPoint.VertexId] * globalMeetingIntervalTicks;
            }

            return hmpPartitionsById;
        }

        private static MeetingPoint[] FindSharedVertexMeetingPoints(PartitionInfo[] partitions)
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

        private int EstimatePartitionMeetingIntervalTicks(HMPPartitionInfo partition)
        {
            var maxTravelTime = EstimateMaxTravelTimeForPartition(partition);
            return 2 * (int)Math.Ceiling((double)partition.VertexIds.Count / partition.MeetingPoints.Count) * maxTravelTime;
        }

        private int EstimateMaxTravelTimeForPartition(HMPPartitionInfo partition)
        {
            var maxTicks = 0;

            var vertexPositions = _patrollingMap.Vertices
                .Where(v => partition.VertexIds.Contains(v.Id))
                .Select(v => v.Position)
                .ToArray();

            foreach (var (vertexPosition1, vertexPosition2) in vertexPositions.Combinations())
            {
                var ticks = _travelEstimator.EstimateTime(vertexPosition1, vertexPosition2);
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