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

using System;
using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.UI;

using NUnit.Framework;

using UnityEngine;

namespace Tests.EditModeTests
{
    public class CommunicationZoneVerticesTests
    {
        private readonly DebuggingVisualizer _debugVisualizer = new();

        private static SimulationMap<Tile> GenerateSimulationMapFromBitmap(string bitmapString)
        {
            var lines = bitmapString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var width = lines[0].Length;
            var height = lines.Length;
            var tiles = new SimulationMapTile<Tile>[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileChar = lines[y][x];
                    var tile = tileChar == 'X' ? new Tile(TileType.Wall) : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return new SimulationMap<Tile>(tiles, Vector2.zero);
        }

        [Theory]
        public void TestCreateCommunicationZoneTiles()
        {
            var height = 12;
            var width = 12;
            var robotConstraints = new RobotConstraints(
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 0,
                materialCommunication: false);
            var vertex = new Vertex(0, 1, new Vector2Int(6, 6));
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
            var slamMap = GenerateSimulationMapFromBitmap(bitmapString);
            var communicationManager = new CommunicationManager(slamMap, robotConstraints, _debugVisualizer);
            var vector2Ints = new List<Vector2Int> { vertex.Position };
            var result = communicationManager.CalculateCommunicationZone(vector2Ints, width, height)[vertex.Position];
            Assert.AreEqual(expectedBitmap, result);

        }
    }
}