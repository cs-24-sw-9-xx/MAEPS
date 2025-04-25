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

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Robot;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter;
using Tests.EditModeTests.Utilities.Partitions;

using RobotConstraints = Maes.Robot.RobotConstraints;

namespace Tests.EditModeTests
{
    public class MeetingPointsWithTimeTest
    {
        private const string TestMap1 = "SXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                X;" +
                                        "X   1     1     12     2     2   X;" +
                                        "X                                X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXE";

        [Test]
        public void PartitionGeneratorHMPPartitionInfo_OneMeetingPointCorrectTime_Test()
        {
            var interpreter = new PartitionSimulationMapBuilder(TestMap1);
            var simulationMap = interpreter.BuildMap().map;
            var vertices = interpreter.Vertices;

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new MeetingPointTimePartitionGenerator(new TestPartitionGenerator(interpreter.VertexPositionsByPartitionId));
            var estimationTravel = new TravelEstimator(coarseMap, robotConstraints);

            generator.SetMaps(patrollingMap, coarseMap, (s, e) => estimationTravel.EstimateTime(s, e));


            var estimateTicks = estimationTravel.EstimateTime(vertices[2].Position, vertices[4].Position)!.Value;
            var expectedGlobalTimeToNextMeeting = 2 * 3 * estimateTicks;

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2 });

            //Check that the the meeting point are vertex 2
            Assert.AreEqual(2, partitions[1].MeetingPoints[0].VertexId);
            Assert.AreEqual(2, partitions[2].MeetingPoints[0].VertexId);

            //Check that the meeting point is at the correct time
            Assert.AreEqual(expectedGlobalTimeToNextMeeting, partitions[1].MeetingPoints[0].AtTicks);
        }

        private const string TestMap2 = "SXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                             X;" +
                                        "X   1     1     12     2     23     3     3   X;" +
                                        "X                                             X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXE";

        [Test]
        public void PartitionGeneratorHMPPartitionInfo_TwoMeetingPointCorrectTime_Test()
        {
            var interpreter = new PartitionSimulationMapBuilder(TestMap2);
            var simulationMap = interpreter.BuildMap().map;
            var vertices = interpreter.Vertices;

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new MeetingPointTimePartitionGenerator(new TestPartitionGenerator(interpreter.VertexPositionsByPartitionId));
            var estimationTravel = new TravelEstimator(coarseMap, robotConstraints);

            generator.SetMaps(patrollingMap, coarseMap, (s, e) => estimationTravel.EstimateTime(s, e));

            var estimateTicks = estimationTravel.EstimateTime(vertices[2].Position, vertices[4].Position)!.Value;
            var expectedGlobalTimeToNextMeeting = 2 * 3 * estimateTicks;

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2, 3 });

            //Check that the the meeting point are vertex 2
            Assert.AreEqual(2, partitions[1].MeetingPoints[0].VertexId);
            Assert.AreEqual(2, partitions[2].MeetingPoints[0].VertexId);
            Assert.AreEqual(4, partitions[2].MeetingPoints[1].VertexId);
            Assert.AreEqual(4, partitions[3].MeetingPoints[0].VertexId);

            //Check that the meeting point is at the correct time
            Assert.AreNotEqual(0, expectedGlobalTimeToNextMeeting);
            Assert.AreEqual(expectedGlobalTimeToNextMeeting * 1, partitions[2].MeetingPoints[0].AtTicks);
            Assert.AreEqual(expectedGlobalTimeToNextMeeting * 2, partitions[2].MeetingPoints[1].AtTicks);
        }
    }
}