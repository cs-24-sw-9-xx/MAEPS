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
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen

using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.UI;
using Maes.UI.Visualizers.Patrolling;
using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace Tests.EditModeTests
{
    public class CommunicationZoneVerticesTests
    {
        private readonly DebuggingVisualizer _debugVisualizer = new();

        [Theory]
        public void TestCreateCommunicationZoneTiles()
        {
            var height = 12;
            var width = 12;
            var robotConstraints = new RobotConstraints(
                attenuationDictionary: new Dictionary<uint, Dictionary<TileType, float>>()
                {
                    {
                        2400U,
                        new Dictionary<TileType, float> { { TileType.Wall, float.MaxValue }, { TileType.Room, 0f } }
                    }
                },
                materialCommunication: true);
            var vertex = new Vertex(0, new Vector2Int(6, 6));
            const string bitmapString = "" +
                "            ;" +
                " XXXXXXXXXX ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " XXXXXXXXXX ;" +
                "            ";

            // Raytracing dont work at 90 angles therefore this is the expected result. The x's showcase the communication zone
            const string expectedBitmapString = "" +
                "            ;" +
                "      X     ;" +
                "  XXXXXXXX  ;" +
                "  XXXXXXXX  ;" +
                "  XXXXXXXX  ;" +
                "  XXXXXXXX  ;" +
                " XXXXXXXXXX ;" +
                "  XXXXXXXX  ;" +
                "  XXXXXXXX  ;" +
                "  XXXXXXXX  ;" +
                "      X     ;" +
                "            ";

            using var expectedBitmap = Utilities.BitmapFromString(expectedBitmapString);
            var slamMap = Utilities.GenerateSimulationMapFromString(bitmapString);
            var communicationManager = new CommunicationManager(slamMap, robotConstraints, _debugVisualizer);
            var vector2Ints = new List<Vector2Int> { vertex.Position };
            var result = communicationManager.CalculateCommunicationZone(vector2Ints, width, height)[vertex.Position];
            Assert.AreEqual(expectedBitmap, result);

        }

        private PatrollingMap CreatePatrollingMap(SimulationMap<Tile> simulationMap, IEnumerable<Vector2Int> vertexPositions)
        {
            var vertices = new List<Vertex>();
            var id = 0;
            foreach (var position in vertexPositions)
            {
                vertices.Add(new Vertex(id++, position));
            }
            // give me a mock of the Dictionary<Vector2Int, Bitmap> vertexPositions that i can use for the patrolling map
            var vertexPositionsDictionary = new Dictionary<Vector2Int, Bitmap>();
            return new PatrollingMap(vertices.ToArray(), simulationMap, vertexPositionsDictionary);
        }

        [Test]
        public void TestCommunicationZoneVertices_SingleVertex()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                materialCommunication: false);

            const string mapString = "" +
                "            ;" +
                " XXXXXXXXXX ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " X        X ;" +
                " XXXXXXXXXX ;" +
                "            ";
            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            var vertexPositions = new List<Vector2Int> { new Vector2Int(6, 6) };
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            Assert.IsNotNull(communicationZoneVertices.CommunicationZoneTiles);
            Assert.AreEqual(1, communicationZoneVertices.CommunicationZoneTiles.Count);
            Assert.Greater(communicationZoneVertices.CommunicationZoneTiles[0].Count, 0);
            Assert.AreEqual(communicationZoneVertices.CommunicationZoneTiles[0].Count, communicationZoneVertices.AllCommunicationZoneTiles.Count);
        }

        [Test]
        public void TestCommunicationZoneVertices_MultipleVertices_NoIntersection()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                maxCommunicationRange: 3,
                materialCommunication: false);

            const string mapString = "" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ";

            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            // Place vertices far apart - at least 10 tiles separation to ensure no overlap with maxCommunicationRange of 3
            var vertexPositions = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(15, 15) };
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            Assert.IsNotNull(communicationZoneVertices.CommunicationZoneTiles);
            Assert.AreEqual(2, communicationZoneVertices.CommunicationZoneTiles.Count);

            // Verify each vertex has its own zone
            Assert.Greater(communicationZoneVertices.CommunicationZoneTiles[0].Count, 0);
            Assert.Greater(communicationZoneVertices.CommunicationZoneTiles[1].Count, 0);

            // Verify total tiles equals sum of individual zones (since no intersection)
            Assert.AreEqual(
                communicationZoneVertices.CommunicationZoneTiles[0].Count + communicationZoneVertices.CommunicationZoneTiles[1].Count,
                communicationZoneVertices.AllCommunicationZoneTiles.Count);
        }

        [Test]
        public void TestCommunicationZoneVertices_MultipleVertices_WithIntersection()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                maxCommunicationRange: 5,
                materialCommunication: false);

            const string mapString = "" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ";

            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            // Place vertices close enough to have overlapping communication zones
            var vertexPositions = new List<Vector2Int> { new Vector2Int(8, 10), new Vector2Int(12, 10) };
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            Assert.IsNotNull(communicationZoneVertices.CommunicationZoneTiles);
            Assert.AreEqual(2, communicationZoneVertices.CommunicationZoneTiles.Count);

            // Verify each vertex has its own zone
            Assert.Greater(communicationZoneVertices.CommunicationZoneTiles[0].Count, 0);
            Assert.Greater(communicationZoneVertices.CommunicationZoneTiles[1].Count, 0);

            // Verify total tiles is less than sum of individual zones (indicating intersection)
            Assert.Less(
                communicationZoneVertices.AllCommunicationZoneTiles.Count,
                communicationZoneVertices.CommunicationZoneTiles[0].Count + communicationZoneVertices.CommunicationZoneTiles[1].Count);

            // Calculate intersection size
            var intersection = new HashSet<int>(communicationZoneVertices.CommunicationZoneTiles[0]);
            intersection.IntersectWith(communicationZoneVertices.CommunicationZoneTiles[1]);

            // Verify intersection is not empty
            Assert.Greater(intersection.Count, 0);
        }

        [Test]
        public void TestCommunicationZoneVertices_WithWalls_BlockingCommunication()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                maxCommunicationRange: 10,
                attenuationDictionary: new Dictionary<uint, Dictionary<TileType, float>>()
                {
                    {
                        2400U,
                        new Dictionary<TileType, float> { { TileType.Wall, float.MaxValue }, { TileType.Room, 0f } }
                    }
                },
                materialCommunication: true);

            const string mapString = "" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "         XXXXXXXXXX ;" +  // Wall between the two vertices
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ";

            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            // Place vertices on opposite sides of the wall
            var vertexPositions = new List<Vector2Int> { new Vector2Int(18, 9), new Vector2Int(18, 11) };
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            // Calculate intersection size
            var intersection = new HashSet<int>(communicationZoneVertices.CommunicationZoneTiles[0]);
            intersection.IntersectWith(communicationZoneVertices.CommunicationZoneTiles[1]);

            // Verify communication zones don't intersect because of the wall
            Assert.AreEqual(0, intersection.Count);
        }

        [Test]
        public void TestCommunicationZoneVertices_EmptyMap()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                materialCommunication: false);

            const string mapString = "" +
                "     ;" +
                "     ;" +
                "     ;" +
                "     ;" +
                "     ";

            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            var vertexPositions = new List<Vector2Int>();  // Empty list of vertices
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            Assert.IsNotNull(communicationZoneVertices.CommunicationZoneTiles);
            Assert.AreEqual(0, communicationZoneVertices.CommunicationZoneTiles.Count);
            Assert.AreEqual(0, communicationZoneVertices.AllCommunicationZoneTiles.Count);
        }

        [Test]
        public void TestCommunicationZoneVertices_AllCommunicationZonesUnion()
        {
            // Arrange
            var robotConstraints = new RobotConstraints(
                maxCommunicationRange: 3,
                materialCommunication: false);

            const string mapString = "" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ;" +
                "                    ";

            var simulationMap = Utilities.GenerateSimulationMapFromString(mapString);
            // Create several vertices to test union functionality
            var vertexPositions = new List<Vector2Int> {
                new Vector2Int(5, 5),
                new Vector2Int(15, 5),
                new Vector2Int(5, 15),
                new Vector2Int(15, 15)
            };
            var patrollingMap = CreatePatrollingMap(simulationMap, vertexPositions);
            var communicationManager = new CommunicationManager(simulationMap, robotConstraints, _debugVisualizer);

            // Act
            var communicationZoneVertices = new CommunicationZoneVertices(simulationMap, patrollingMap, communicationManager);

            // Assert
            Assert.IsNotNull(communicationZoneVertices.CommunicationZoneTiles);
            Assert.AreEqual(4, communicationZoneVertices.CommunicationZoneTiles.Count);

            // Create manual union to compare with AllCommunicationZoneTiles
            var manualUnion = new HashSet<int>();
            for (var i = 0; i < 4; i++)
            {
                manualUnion.UnionWith(communicationZoneVertices.CommunicationZoneTiles[i]);
            }

            // Compare manual union with AllCommunicationZoneTiles
            Assert.AreEqual(manualUnion.Count, communicationZoneVertices.AllCommunicationZoneTiles.Count);
            Assert.IsTrue(manualUnion.SetEquals(communicationZoneVertices.AllCommunicationZoneTiles));
        }
    }
}