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
using System.Linq;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter;

using UnityEngine;

namespace Tests.EditModeTests
{
    public class AdapterToPartitionGeneratorTest
    {
        private const string testMap = "SXXXXXXXX;" +
                                       "X       X;" +
                                       "X       X;" +
                                       "XXXXXXXXX;" +
                                       "X       X;" +
                                       "X       X;" +
                                       "XXXXXXXXE";


        [Test]
        public void AdapterToPartitionGenerator_TwoPartitions_Test()
        {
            var ((start, end), simulationMap) = new SimulationMapBuilder(testMap).BuildMap();

            var centerX = (int)(start.x + end.x) / 2;
            var oneOfFifthY = (int)(start.y + end.y) / 5;

            var firstPartitionCentroid = new Vector2Int(centerX, oneOfFifthY * 1);
            var secondPartitionCentroid = new Vector2Int(centerX, oneOfFifthY * 3);
            var vertexPositions = new[] { firstPartitionCentroid, secondPartitionCentroid };

            var i = 0;
            var vertices = vertexPositions.Select(position => new Vertex(i++, position)).ToList();

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new AdapterToPartitionGenerator(PartitioningGenerator);
            generator.SetMaps(patrollingMap, coarseMap, robotConstraints);

            var partitions = generator.GeneratePartitions(new HashSet<int> { 0, 1 });

            Assert.AreEqual(2, partitions.Count);
            Assert.AreEqual(partitions[0], new PartitionInfo(0, new HashSet<int> { vertices[1].Id }));
            Assert.AreEqual(partitions[1], new PartitionInfo(1, new HashSet<int> { vertices[0].Id }));
            return;

            Dictionary<int, List<Vector2Int>> PartitioningGenerator(Dictionary<(Vector2Int, Vector2Int), int> ints, List<Vector2Int> list, int i1)
            {
                return new Dictionary<int, List<Vector2Int>> { { 0, new List<Vector2Int> { secondPartitionCentroid } }, { 1, new List<Vector2Int> { firstPartitionCentroid } } };
            }
        }

        [Test]
        public void AdapterToPartitionGenerator_OnePartition_Test()
        {
            var ((start, end), simulationMap) = new SimulationMapBuilder(testMap).BuildMap();

            var centerX = (int)(start.x + end.x) / 2;
            var oneOfFifthY = (int)(start.y + end.y) / 5;

            var firstPartitionCentroid = new Vector2Int(centerX, oneOfFifthY * 1);
            var secondPartitionCentroid = new Vector2Int(centerX, oneOfFifthY * 3);
            var vertexPositions = new[] { firstPartitionCentroid, secondPartitionCentroid };

            var i = 0;
            var vertices = vertexPositions.Select(position => new Vertex(i++, position)).ToList();

            var robotConstraints = new RobotConstraints(mapKnown: true);
            var slamMap = new SlamMap(simulationMap, robotConstraints, 0);
            var coarseMap = slamMap.CoarseMap;
            var patrollingMap = new PatrollingMap(vertices, simulationMap);
            var generator = new AdapterToPartitionGenerator(PartitioningGenerator);
            generator.SetMaps(patrollingMap, coarseMap, robotConstraints);

            var partitions = generator.GeneratePartitions(new HashSet<int> { 0 });

            Assert.AreEqual(1, partitions.Count);
            Assert.AreEqual(partitions[0], new PartitionInfo(0, new HashSet<int> { vertices[0].Id, vertices[1].Id }));
            return;

            Dictionary<int, List<Vector2Int>> PartitioningGenerator(Dictionary<(Vector2Int, Vector2Int), int> ints, List<Vector2Int> list, int i1)
            {
                return new Dictionary<int, List<Vector2Int>> { { 0, new List<Vector2Int> { firstPartitionCentroid, secondPartitionCentroid } } };
            }
        }
    }




}