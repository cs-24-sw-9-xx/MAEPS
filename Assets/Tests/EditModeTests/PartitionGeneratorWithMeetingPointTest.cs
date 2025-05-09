using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Map.Generators.Patrolling.Waypoints.Connectors;
using Maes.Robot;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter.MapBuilder;
using Tests.EditModeTests.Utilities.Partitions;

namespace Tests.EditModeTests
{
    public class PartitionGeneratorWithMeetingPointTest
    {
        private const string TestMap1 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                X;" +
                                        "X   1     1            2     2   X;" +
                                        "X                                X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        [Test]
        public void PartitionGenerator_GenerateOneSharingVertex_Test()
        {
            var (simulationMap, patrollingMap, verticesByPartitionId) = new PartitionSimulationMapBuilder(TestMap1, AllConnectedWaypointConnector.ConnectVertices).Build();

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var coarseMap = new SlamMap(simulationMap, robotConstraints, 0).CoarseMap;
            var generator = new PartitionGeneratorWithMeetingPoint(new TestPartitionGenerator(verticesByPartitionId));

            generator.SetMaps(patrollingMap, coarseMap);

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2 });

            //Check that the partitions share vertex id 1
            Assert.AreEqual(1, partitions[1].VertexIds.Intersect(partitions[2].VertexIds).ToArray()[0]);
        }

        private const string TestMap2 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X                                X;" +
                                        "X   1     12           2     2   X;" +
                                        "X   3                        3   X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        [Test]
        public void PartitionGenerator_GenerateTwoSharingVertex_Test()
        {
            var (simulationMap, patrollingMap, verticesByPartitionId) = new PartitionSimulationMapBuilder(TestMap2, AllConnectedWaypointConnector.ConnectVertices).Build();

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var coarseMap = new SlamMap(simulationMap, robotConstraints, 0).CoarseMap;
            var generator = new PartitionGeneratorWithMeetingPoint(new TestPartitionGenerator(verticesByPartitionId));

            generator.SetMaps(patrollingMap, coarseMap);

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2, 3 });

            //Check that the partitions 1 and 3 share vertex id 0
            Assert.AreEqual(0, partitions[1].VertexIds.Intersect(partitions[3].VertexIds).ToArray()[0]);

            //Check that the partitions 2 and 3 share vertex id 3
            Assert.AreEqual(3, partitions[2].VertexIds.Intersect(partitions[3].VertexIds).ToArray()[0]);
        }

        private const string TestMap3 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;" +
                                        "X   1     12           2    2    X;" +
                                        "X   3    xxxxxxxxxxxxxxxx    4   X;" +
                                        "X    3          x            4   X;" +
                                        "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        [Test]
        public void PartitionGenerator_GenerateSharingVertexNotAllPartitionsAreNeighbor_Test()
        {
            var (simulationMap, patrollingMap, verticesByPartitionId) = new PartitionSimulationMapBuilder(TestMap3, AllConnectedWaypointConnector.ConnectVertices).Build();

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var coarseMap = new SlamMap(simulationMap, robotConstraints, 0).CoarseMap;
            var generator = new PartitionGeneratorWithMeetingPoint(new TestPartitionGenerator(verticesByPartitionId));

            generator.SetMaps(patrollingMap, coarseMap);

            var partitions = generator.GeneratePartitions(new HashSet<int> { 1, 2, 3, 4 });

            //Check that the partitions 1 and 2 share vertex id 1
            Assert.AreEqual(1, partitions[1].VertexIds.Intersect(partitions[2].VertexIds).ToArray()[0]);

            //Check that the partitions 1 and 3 share vertex id 0
            Assert.AreEqual(0, partitions[1].VertexIds.Intersect(partitions[3].VertexIds).ToArray()[0]);

            //Check that the partitions 1 and 4 have no shared vertex
            Assert.IsFalse(partitions[1].VertexIds.ToHashSet().Overlaps(partitions[4].VertexIds));

            //Check that the partitions 2 and 3 have no shared vertex
            Assert.IsFalse(partitions[2].VertexIds.ToHashSet().Overlaps(partitions[3].VertexIds));

            //Check that the partitions 2 and 4 share vertex id 3
            Assert.AreEqual(3, partitions[2].VertexIds.Intersect(partitions[4].VertexIds).ToArray()[0]);

            //Check that the partitions 3 and 4 have no shared vertex
            Assert.IsFalse(partitions[3].VertexIds.ToHashSet().Overlaps(partitions[4].VertexIds));
        }
    }
}