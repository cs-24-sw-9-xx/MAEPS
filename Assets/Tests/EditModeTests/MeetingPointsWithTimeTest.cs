using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Robot;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.StringMapInterpreter;
using Tests.EditModeTests.UtilitiesPartition;

using UnityEngine;

using RobotConstraints = Maes.Robot.RobotConstraints;

namespace Tests.EditModeTests
{
    public class MeetingPointsWithTimeTest
    {
        private const string testMap1 = "SXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                X;" +
                                        "X   1     1     12     2     2   X;" +
                                        "X                                X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXE";
        
        private string PrintSimulationMap(SimulationMap<Tile> simulationMap)
        {
            var result = "";
            
            for (var y = 0; y < simulationMap.HeightInTiles; y++)
            {
                for (var x = 0; x < simulationMap.WidthInTiles; x++)
                {
                    var vertex = simulationMap.GetTileByLocalCoordinate(x, y);
                    if (vertex.GetTriangles()[0].Type == TileType.Room)
                    {
                        result += " ";
                    }
                    else
                    {
                        result += "X";
                    }
                }
                result += "\n";
            }
            return result;
        }
        
        [Test]
        public void PartitionGeneratorHMPPartitionInfo_OneMeetingPointCorrectTime_Test()
        {
            var interpreter = new PartitionStringMapInterpreter(testMap1);
            interpreter.Interpret();
            
            var simulationMap = interpreter.SimulationMap;
            var vertices = interpreter.Vertices;
            
            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new PartitionGeneratorHMPPartitionInfo(new TestPartitionGenerator(interpreter.VertexPositionsByPartitionId));
            generator.SetMaps(patrollingMap, coarseMap, robotConstraints);
            
            var estimationTravel = new EstimationTravel(coarseMap, robotConstraints);
            
            var estimateTicks = estimationTravel.EstimateTime(vertices[2].Position, vertices[4].Position)!.Value;
            var expectedGlobalTimeToNextMeeting = 2 * 3 * estimateTicks; 
            
            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2 });

            //Check that the the meeting point are vertex 2
            Assert.AreEqual(2, partitions[1].MeetingPoints[0].VertexId);
            Assert.AreEqual(2, partitions[2].MeetingPoints[0].VertexId);
            
            //Check that the meeting point is at the correct time
            Assert.AreEqual(expectedGlobalTimeToNextMeeting, partitions[1].MeetingPoints[0].AtTicks);
        }
        
        private const string testMap2 = "SXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                             X;" +
                                        "X   1     1     12     2     23     3     3   X;" +
                                        "X                                             X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXE";
        
        [Test]
        public void PartitionGeneratorHMPPartitionInfo_TwoMeetingPointCorrectTime_Test()
        {
            var interpreter = new PartitionStringMapInterpreter(testMap2);
            interpreter.Interpret();
            
            var simulationMap = interpreter.SimulationMap;
            var vertices = interpreter.Vertices;
            
            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new PartitionGeneratorHMPPartitionInfo(new TestPartitionGenerator(interpreter.VertexPositionsByPartitionId));
            generator.SetMaps(patrollingMap, coarseMap, robotConstraints);
            
            var estimationTravel = new EstimationTravel(coarseMap, robotConstraints);
            
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