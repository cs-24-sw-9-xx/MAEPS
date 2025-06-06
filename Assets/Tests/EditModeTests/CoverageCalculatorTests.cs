// Copyright 2022 MAES
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
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Statistics;

using NUnit.Framework;

using UnityEngine;

using Random = System.Random;

namespace Tests.EditModeTests
{
    public class CoverageCalculatorTest
    {
        private CoverageCalculator _coverageCalculator;
        private SimulationMap<Tile> _collisionMap;
        private SimulationMap<Cell> _explorationMap;
        private const int RandomSeed = 123;
        private const int Width = 50, Height = 50;

        [SetUp]
        public void InitializeCalculatorAndMaps()
        {
            _collisionMap = GenerateCollisionMap();
            _explorationMap = _collisionMap.FMap(tile => new Cell(!Tile.IsWall(tile.Type)));
            _coverageCalculator = new CoverageCalculator(_explorationMap, _collisionMap);
        }

        // Generates a collision map where only the edge tiles are solid
        private static SimulationMap<Tile> GenerateCollisionMap()
        {
            var tiles = new SimulationMapTile<Tile>[Width, Height];
            var random = new Random(RandomSeed);
            var wall = Tile.GetRandomWall(random);
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var tile = IsGeneratedTileSolid(new Vector2Int(x, y)) ? wall : new Tile(TileType.Room);
                    tiles[x, y] = new SimulationMapTile<Tile>(() => tile);
                }
            }

            return new SimulationMap<Tile>(tiles, Vector2.zero);
        }

        // All edges are solid. All other tiles are non-solid
        private static bool IsGeneratedTileSolid(Vector2Int tileCoordinate)
        {
            return (tileCoordinate.x == 0 ||
                    tileCoordinate.y == 0 ||
                    tileCoordinate.x == Width - 1 ||
                    tileCoordinate.y == Height - 1);
        }

        [Test]
        public void RobotOnTopOfTileCoverageTest()
        {
            // The test robot is positioned in the middle of the coarse tile at coordinates (20, 20)
            // (Equivalent to the slam tile at (40, 40)
            var robotWorldPos = new Vector2(20.25f, 20.25f);
            var ((_, cell1), (_, cell2)) = _explorationMap.GetMiniTilesByCoarseTileCoordinate(robotWorldPos);

            // Assert that none of cells are covered in advance
            Assert.IsFalse(cell1.IsCovered);
            Assert.IsFalse(cell2.IsCovered);

            // Register coverage for the testing robot 
            _coverageCalculator.UpdateRobotCoverage(robotWorldPos, 1, (_, _, _, _) =>
            {
            });

            // Assert that the status of the tiles has now changed
            Assert.IsTrue(cell1.IsCovered);
            Assert.IsTrue(cell2.IsCovered);
        }


        [Test]
        [TestCase(20.00f, 20.00f)]
        [TestCase(20.5f, 20.5f)]
        [TestCase(20.25f, 20.25f)]
        [TestCase(20.75f, 20.75f)]
        [TestCase(20.49f, 20.49f)]
        [TestCase(20.99f, 20.99f)]
        public void AdjacentTilesAreCoveredTest(float robotX, float robotY)
        {
            // The test robot is positioned in the middle of the coarse tile at coordinates (20, 20)
            // (Equivalent to the slam tile at (40, 40)
            var robotWorldPos = new Vector2(robotX, robotY);

            // Find all cells that are immediate neighbours of tile currently occupied by the robot
            var cells = new List<Cell>();
            for (var x = -1; x < 1; x++)
            {
                for (var y = -1; y < 1; y++)
                {
                    var xOffset = x * 0.5f;
                    var yOffset = y * 0.5f;
                    var ((_, cell1), (__, cell2)) = _explorationMap
                        .GetMiniTilesByCoarseTileCoordinate(robotWorldPos + new Vector2(xOffset, yOffset));
                    cells.Add(cell1);
                    cells.Add(cell2);
                }
            }

            // Assert that none of cells are covered in advance
            foreach (var cell in cells)
            {
                Assert.IsFalse(cell.IsCovered);
            }

            // Register coverage for the testing robot 
            _coverageCalculator.UpdateRobotCoverage(robotWorldPos, 1, (_, _, _, _) =>
            {
            });

            // Assert that the status of the tiles has now changed
            foreach (var cell in cells)
            {
                Assert.IsTrue(cell.IsCovered);
            }
        }


        [Test]
        public void CoverageTimeUpdateTest()
        {
            // The test robot is positioned in the middle of the coarse tile at coordinates (20, 20)
            // (Equivalent to the slam tile at (40, 40)
            var robotWorldPos = new Vector2(20.25f, 20.25f);
            var ((_, cell1), (_, cell2)) = _explorationMap.GetMiniTilesByCoarseTileCoordinate(robotWorldPos);

            const int coverageTick = 123456;
            // Register coverage for the testing robot 
            _coverageCalculator.UpdateRobotCoverage(robotWorldPos, coverageTick, (_, _, _, _) =>
            {
            });

            // Assert the the coverage time is updated
            Assert.AreEqual(cell1.LastCoverageTimeInTicks, coverageTick);
            Assert.AreEqual(cell2.LastCoverageTimeInTicks, coverageTick);

            // Cover again at one tick later
            _coverageCalculator.UpdateRobotCoverage(robotWorldPos, coverageTick + 1, (_, _, _, _) =>
            {
            });
            Assert.AreEqual(cell1.LastCoverageTimeInTicks, coverageTick + 1);
            Assert.AreEqual(cell2.LastCoverageTimeInTicks, coverageTick + 1);
        }

        [Test]
        public void TilesAreMarkedCoverableCorrectly()
        {
            // Copy the existing exploration map and to get a new map where all CanBeCovered flags are true
            var freshExplorationMap = _explorationMap.FMap((cell) => new Cell(cell.IsExplorable));
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Assert.IsTrue(freshExplorationMap.GetTileByLocalCoordinate(x, y).IsTrueForAll(cell => cell.CanBeCovered));
                }
            }

            new CoverageCalculator(freshExplorationMap, _collisionMap);
            // Pass the new exploration map to the coverage calculator which should cause the solid tiles to be
            // marked as non-coverable
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    // We now expect the tiles have a 'CanBeCovered' status that is opposite to the solid status of tile
                    // (ie. solid tiles cannot be covered and non-solid ones can be covered)
                    var isSolid = IsGeneratedTileSolid(new Vector2Int(x, y));
                    Assert.IsTrue(freshExplorationMap.GetTileByLocalCoordinate(x, y)
                        .IsTrueForAll(cell => cell.CanBeCovered != isSolid));
                }
            }
        }
    }
}