// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections;
using System.Collections.Generic;

using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.Exploration;

using NUnit.Framework;

using Tests.PlayModeTests.Algorithms.Exploration;

using UnityEngine;

using Random = System.Random;

namespace Tests.PlayModeTests
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;

    public class CommunicationTest
    {
        private const int MapWidth = 50, MapHeight = 50;
        private const int RandomSeed = 123;
        private MySimulator _maes;
        private ExplorationSimulation _explorationSimulation;
        private List<TestingAlgorithm> _robotTestAlgorithms;

        private static Tile[,] GenerateMapWithHorizontalWallInMiddle(int wallThicknessInTiles)
        {
            var bitmap = new Tile[MapWidth, MapHeight];
            var firstWallRowY = MapHeight / 2;
            var lastWallRowY = firstWallRowY + wallThicknessInTiles;
            var random = new Random(RandomSeed);
            var wall = Tile.GetRandomWall(random);

            for (var x = 0; x < MapWidth; x++)
            {
                for (var y = 0; y < MapHeight; y++)
                {
                    var isSolid = y >= firstWallRowY && y <= lastWallRowY - 1;
                    bitmap[x, y] = isSolid ? wall : new Tile(TileType.Room);
                }
            }

            return bitmap;
        }


        [TearDown]
        public void ClearSimulator()
        {
            _maes.Destroy();
        }

        private void InitSimulator(MapFactory mapFactory,
            RobotConstraints.SignalTransmissionSuccessCalculator transmissionSuccessCalculatorFunc,
            List<Vector2Int> robotSpawnPositions)
        {
            _robotTestAlgorithms = new List<TestingAlgorithm>();
            var testingScenario = new MySimulationScenario(RandomSeed,
                mapSpawner: mapFactory,
                hasFinishedSim: _ => false,
                robotConstraints: new RobotConstraints(materialCommunication: false, calculateSignalTransmissionProbability: transmissionSuccessCalculatorFunc, mapKnown: true, slamRayTraceRange: 0),
                robotSpawner: (map, spawner) => spawner.SpawnRobotsAtPositions(robotSpawnPositions, map, RandomSeed, 2,
                    _ =>
                    {
                        var algorithm = new TestingAlgorithm();
                        _robotTestAlgorithms.Add(algorithm);
                        return algorithm;
                    }));

            _maes = new MySimulator(new[] { testingScenario });
            _explorationSimulation = _maes.SimulationManager.CurrentSimulation;

            // The first robot will broadcast immediatealy
            _robotTestAlgorithms[0].UpdateFunction = (tick, controller) =>
            {
                if (tick == 0)
                {
                    controller.Broadcast("Test Message");
                }
            };

            // The second robot will continuously receive broadcasts
            _robotTestAlgorithms[0].UpdateFunction = (_, controller) =>
            {
                controller.ReceiveBroadcast();
            };
        }


        [Test(ExpectedResult = null)]
        public IEnumerator Broadcast_TransmissionSuccessTest()
        {
            InitSimulator(StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                (_, _) => true,
                new List<Vector2Int> { new(2, 2), new(6, 6) });

            string receivedMessage = null;
            var sentMessage = "message sent between robots 1 and 2";
            var algorithm1 = _robotTestAlgorithms[0];
            var algorithm2 = _robotTestAlgorithms[1];

            algorithm1.UpdateFunction = (tick, controller) =>
            {
                if (tick == 0)
                {
                    controller.Broadcast(sentMessage);
                }
            };

            algorithm2.UpdateFunction = (_, controller) =>
            {
                var results = controller.ReceiveBroadcast();
                if (results.Count != 0)
                {
                    receivedMessage = results[0] as string;
                }
            };

            _maes.PressPlayButton();
            // Wait until the message has been broadcast
            while (_explorationSimulation.SimulatedLogicTicks < 2)
            {
                yield return null;
            }

            Assert.AreEqual(sentMessage, receivedMessage);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Broadcast_TransmissionFailedTest()
        {
            InitSimulator(StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                (_, _) => false, // Always fail communication
                new List<Vector2Int> { new(2, 2), new(6, 6) });

            string receivedMessage = null;
            var sentMessage = "message sent between robots 1 and 2";
            var algorithm1 = _robotTestAlgorithms[0];
            var algorithm2 = _robotTestAlgorithms[1];

            algorithm1.UpdateFunction = (tick, controller) =>
            {
                if (tick == 0)
                {
                    controller.Broadcast(sentMessage);
                }
            };

            algorithm2.UpdateFunction = (_, controller) =>
            {
                var results = controller.ReceiveBroadcast();
                if (results.Count != 0)
                {
                    receivedMessage = results[0] as string;
                }
            };

            _maes.PressPlayButton();
            // Wait until the message has been broadcast
            while (_explorationSimulation.SimulatedLogicTicks < 2)
            {
                yield return null;
            }

            Assert.IsNull(receivedMessage);
        }

        [Test(ExpectedResult = null)]
        public IEnumerator Broadcast_NoWallsCommunicationTest()
        {
            var foundWallDistance = float.PositiveInfinity;

            InitSimulator(StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                (_, wallDistance) =>
                {
                    foundWallDistance = wallDistance;
                    return true;
                },
                new List<Vector2Int> { new(-10, -10), new(10, 10) });

            _maes.PressPlayButton();
            // Wait until the message has been broadcast
            while (_explorationSimulation.SimulatedLogicTicks < 2)
            {
                yield return null;
            }

            // Assert that the signal is said to travel through 1 meter/unit of wall
            Assert.AreEqual(foundWallDistance, 0f, 0.001f);
        }

        [Test(ExpectedResult = null)]
        [TestCase(20, 20, ExpectedResult = null)]
        [TestCase(5, 5, ExpectedResult = null)]
        [TestCase(5, -5, ExpectedResult = null)]
        [TestCase(-5, 5, ExpectedResult = null)]
        [TestCase(-5, -5, ExpectedResult = null)]
        public IEnumerator Broadcast_CorrectDistanceCalculation(int secondRobotX, int secondRobotY)
        {
            var transmissionDistance = float.PositiveInfinity;
            var firstRobotPosition = new Vector2Int(0, 0);
            var secondRobotPosition = new Vector2Int(secondRobotX, secondRobotY);
            var actualDistance = (firstRobotPosition - secondRobotPosition).magnitude;

            InitSimulator(StandardTestingConfiguration.EmptyCaveMapSpawner(RandomSeed),
                (distance, _) =>
                {
                    transmissionDistance = distance;
                    return true;
                },
                new List<Vector2Int> { firstRobotPosition, secondRobotPosition });

            _maes.PressPlayButton();
            // Wait until the message has been broadcast
            while (_explorationSimulation.SimulatedLogicTicks < 2)
            {
                yield return null;
            }

            // Assert that the signal is said to travel through 1 meter/unit of wall
            Assert.AreEqual(actualDistance, transmissionDistance, 0.001f);
        }


        [Test(ExpectedResult = null)]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(2, ExpectedResult = null)]
        [TestCase(5, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        public IEnumerator Broadcast_WallDistanceIsApproximatelyCorrect(int wallThickness)
        {
            var foundWallDistance = float.PositiveInfinity;
            InitSimulator(
                generator => generator.GenerateMap(GenerateMapWithHorizontalWallInMiddle(wallThickness), RandomSeed, borderSize: 2),
                    transmissionSuccessCalculatorFunc:
                    (_, wallDistance) =>
                    {
                        foundWallDistance = wallDistance;
                        return true;
                    },
                new List<Vector2Int> { new(0, -2), new(0, 3 + wallThickness) });

            _maes.PressPlayButton();
            // Wait until the message has been broadcast
            while (_explorationSimulation.SimulatedLogicTicks < 5)
            {
                yield return null;
            }

            // Assert that the signal is said to travel through 1 meter/unit of wall
            Assert.AreEqual((float)wallThickness, foundWallDistance, 0.1f * wallThickness);
        }

    }
}