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

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Robot;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter.MapBuilder;
using Tests.EditModeTests.Utilities.Partitions;

using RobotConstraints = Maes.Robot.RobotConstraints;

namespace Tests.EditModeTests
{
    // TODO: Check if the tests are still valid
    public class MeetingPointsWithTimeTest
    {
        private const string TestMap1 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                X;" +
                                        "X   1     1     12     2     2   X;" +
                                        "X                                X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        [Test]
        public void PartitionGeneratorHMPPartitionInfo_OneMeetingPointCorrectTime_Test()
        {
            var (simulationMap, patrollingMap, vertexPositionsByPartitionId) = new PartitionSimulationMapBuilder(TestMap1, AllConnectedWaypointConnector.ConnectVertices).Build();
            var vertices = patrollingMap.Vertices;

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var coarseMap = new SlamMap(simulationMap, robotConstraints, 0).CoarseMap;
            var generator = new MeetingPointTimePartitionGenerator(new TestPartitionGenerator(vertexPositionsByPartitionId));
            var estimationTravel = new TravelEstimator(coarseMap, robotConstraints);

            generator.SetMaps(patrollingMap, coarseMap);
            generator.SetEstimates((s, e) => estimationTravel.EstimateTime(s, e, dependOnBrokenBehaviour: false), _ => 0);



            var estimateTicks = estimationTravel.EstimateTime(vertices[2].Position, vertices[4].Position, dependOnBrokenBehaviour: false)!.Value;
            var expectedGlobalTimeToNextMeeting = 2 * 3 * estimateTicks;

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2 });

            //Check that the meeting point are vertex 2
            Assert.AreEqual(2, partitions[1].MeetingPoints[0].VertexId);
            Assert.AreEqual(2, partitions[2].MeetingPoints[0].VertexId);

            var meetingTicks = partitions[1].MeetingPoints[0].MeetingAtTicks.ToArray();
            //Check that the meeting point is at the correct time
            Assert.AreEqual(expectedGlobalTimeToNextMeeting, meetingTicks[1] - meetingTicks[0]);
        }

        private const string TestMap2 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                             X;" +
                                        "X   1     1     12     2     23     3     3   X;" +
                                        "X                                             X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        [Test]
        public void PartitionGeneratorHMPPartitionInfo_TwoMeetingPointCorrectTime_Test()
        {
            var (simulationMap, patrollingMap, vertexPositionsByPartitionId) = new PartitionSimulationMapBuilder(TestMap2, AllConnectedWaypointConnector.ConnectVertices).Build();
            var vertices = patrollingMap.Vertices;

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var coarseMap = new SlamMap(simulationMap, robotConstraints, 0).CoarseMap;
            var generator = new MeetingPointTimePartitionGenerator(new TestPartitionGenerator(vertexPositionsByPartitionId));
            var estimationTravel = new TravelEstimator(coarseMap, robotConstraints);

            const int tickToFartestPartition = 48;

            generator.SetMaps(patrollingMap, coarseMap);
            generator.SetEstimates((s, e) => estimationTravel.EstimateTime(s, e, dependOnBrokenBehaviour: false), _ => tickToFartestPartition);

            var estimateTicks = estimationTravel.EstimateTime(vertices[2].Position, vertices[4].Position, dependOnBrokenBehaviour: false)!.Value;
            var expectedGlobalTimeToNextMeeting = 2 * 3 * estimateTicks;

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2, 3 });

            //Check that the meeting point are vertex 2
            Assert.AreEqual(2, partitions[1].MeetingPoints[0].VertexId);
            Assert.AreEqual(2, partitions[2].MeetingPoints[0].VertexId);
            Assert.AreEqual(4, partitions[2].MeetingPoints[1].VertexId);
            Assert.AreEqual(4, partitions[3].MeetingPoints[0].VertexId);

            //Check that the meeting point is at the correct time
            Assert.AreNotEqual(0, expectedGlobalTimeToNextMeeting);
            var meeting1MeetingTicks = partitions[2].MeetingPoints[0].MeetingAtTicks.ToArray();
            Assert.AreEqual(expectedGlobalTimeToNextMeeting, meeting1MeetingTicks[1] - meeting1MeetingTicks[0]);
            Assert.AreEqual(expectedGlobalTimeToNextMeeting * 1 + tickToFartestPartition, meeting1MeetingTicks[0]);

            var meeting2MeetingTicks = partitions[2].MeetingPoints[1].MeetingAtTicks.ToArray();
            Assert.AreEqual(expectedGlobalTimeToNextMeeting, meeting2MeetingTicks[1] - meeting2MeetingTicks[0]);
            Assert.AreEqual(expectedGlobalTimeToNextMeeting * 2 + tickToFartestPartition, meeting2MeetingTicks[0]);
        }
    }
}