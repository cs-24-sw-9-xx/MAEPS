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
// Contributors: Mads Beyer Mogensen

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;
using Maes.Simulation.Patrolling;
using Maes.UI;
using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace Tests.PlayModeTests.Algorithms.Patrolling.VirtualStigmergy
{
    public class VirtualStigmergySimpleTest : MonoBehaviour
    {
        private PatrollingSimulator _simulator;

        private List<TestingAlgorithm> _algorithms;

        [SetUp]
        public void Setup()
        {
            _simulator = new PatrollingSimulator();
            _algorithms = new List<TestingAlgorithm>();
        }

        [TearDown]
        public void TearDown()
        {
            _simulator.Destroy();
            _algorithms.Clear();
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestDirectCommunication()
        {
            EnqueueScenario(EditModeTests.Utilities.CreateEmptyBitmap(16, 16), 100, new Vector2Int(1, 8), new Vector2Int(15, 8));
            _simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // This waits an unknown amount of ticks
            yield return null;

            var ticks = _simulator.SimulationManager.CurrentSimulation!.SimulatedLogicTicks;

            var algorithm1 = _algorithms[0];
            var algorithm2 = _algorithms[1];

            algorithm1.VirtualStigmergyComponent.Put("test", "ayo");

            Assert.AreEqual(1, algorithm1.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm1.VirtualStigmergyComponent.GetNonSending("test"));

            // Make sure we have no keys
            Assert.AreEqual(0, algorithm2.VirtualStigmergyComponent.Size());

            // This waits an unknown amount of ticks
            while (ticks == _simulator.SimulationManager.CurrentSimulation.SimulatedLogicTicks)
            {
                yield return null;
            }

            // Make sure we have the key
            Assert.AreEqual(1, algorithm2.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm2.VirtualStigmergyComponent.GetNonSending("test"));

            ticks = _simulator.SimulationManager.CurrentSimulation.SimulatedLogicTicks;

            // This waits an unknown amount of ticks
            // To wait for the GET message
            while (ticks == _simulator.SimulationManager.CurrentSimulation.SimulatedLogicTicks)
            {
                yield return null;
            }

            Assert.AreEqual(1, algorithm1.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm1.VirtualStigmergyComponent.GetNonSending("test"));

            Assert.AreEqual(1, algorithm2.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm2.VirtualStigmergyComponent.GetNonSending("test"));
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestTransitiveCommunication()
        {
            EnqueueScenario(EditModeTests.Utilities.CreateEmptyBitmap(24, 16), 16, new Vector2Int(1, 8), new Vector2Int(15, 8), new Vector2Int(23, 8));
            _simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // This waits an unknown amount of ticks
            yield return null;

            var algorithm1 = _algorithms[0];
            var algorithm2 = _algorithms[1];
            var algorithm3 = _algorithms[2];

            algorithm1.VirtualStigmergyComponent.Put("test", "ayo");

            // Wait for at least 3 ticks
            var ticks = _simulator.SimulationManager.CurrentSimulation!.SimulatedLogicTicks;
            while (ticks + 3 >= _simulator.SimulationManager.CurrentSimulation.SimulatedLogicTicks)
            {
                yield return null;
            }

            Assert.AreEqual(1, algorithm2.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm2.VirtualStigmergyComponent.GetNonSending("test"));

            Assert.AreEqual(1, algorithm3.VirtualStigmergyComponent.Size());
            Assert.AreEqual("ayo", algorithm3.VirtualStigmergyComponent.GetNonSending("test"));
        }

        [Test(ExpectedResult = null)]
        public IEnumerator TestSpottyCommunication()
        {
            var semiWalledMap = EditModeTests.Utilities.CreateEmptyBitmap(16, 16);
            for (var i = 0; i < 8; i++)
            {
                semiWalledMap.Set(8, i);
            }

            EnqueueScenario(semiWalledMap, 100, new Vector2Int(5, 8), new Vector2Int(11, 8));
            _simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);

            // This waits an unknown amount of ticks
            yield return null;


            var algorithm1 = _algorithms[0];
            var algorithm2 = _algorithms[1];

            // This message should not go through to algorithm 2
            algorithm1.VirtualStigmergyComponent.Put("test", "ayo");

            // Wait for an unknown amount of time
            yield return null;

            // Move up to be in direct line of sight
            var target = new Vector2Int(11, 11);
            while (algorithm2.Controller.SlamMap.CoarseMap.GetCurrentPosition(dependOnBrokenBehavior: true) != target)
            {
                algorithm2.Controller.MoveTo(target);
                yield return null;
            }

            // Should not have any info yet
            Assert.AreEqual(0, algorithm2.VirtualStigmergyComponent.Size());


            // Should be null, but should send a get message so that we can get the info after some ticks
            Assert.IsNull(algorithm2.VirtualStigmergyComponent.Get("test"));

            // Wait for at least 3 ticks to wait for the put message from algorithm 1
            var ticks = _simulator.SimulationManager.CurrentSimulation!.SimulatedLogicTicks;
            while (ticks + 3 >= _simulator.SimulationManager.CurrentSimulation.SimulatedLogicTicks)
            {
                yield return null;
            }

            Assert.AreEqual("ayo", algorithm2.VirtualStigmergyComponent.Get("test"));
        }


        private void EnqueueScenario(Bitmap bitmap, float communicationRange, params Vector2Int[] robotPositions)
        {
            var tilemap = Utilities.BitmapToTilemap(bitmap);

            var robotSpawnPositions = robotPositions.ToList();

            _simulator.EnqueueScenario(new PatrollingSimulationScenario(
                seed: 123,
                totalCycles: 4,
                stopAfterDiff: false,
                robotSpawner: (map, spawner) => spawner.SpawnRobotsAtPositions(robotSpawnPositions, map, 123, robotSpawnPositions.Count,
                    _ =>
                    {
                        var algorithm = new TestingAlgorithm();
                        _algorithms.Add(algorithm);

                        return algorithm;
                    }, dependOnBrokenBehavior: false),
                mapSpawner: mapSpawner => mapSpawner.GenerateMap(tilemap, 123, brokenCollisionMap: false),
                CreateRobotConstraints(communicationRange),
                patrollingMapFactory: map => new PatrollingMap(new[] { new Vertex(0, 1, new Vector2Int(4, 4)) }, map,
                    new Dictionary<Vector2Int, Bitmap>() { { new Vector2Int(4, 4), bitmap } })
            ));
        }

        private static RobotConstraints CreateRobotConstraints(float communicationRange)
        {
            var robotConstraints = StandardTestingConfiguration.GlobalRobotConstraints();

            return new RobotConstraints(
                senseNearbyAgentsRange: robotConstraints.SenseNearbyAgentsRange,
                senseNearbyAgentsBlockedByWalls: robotConstraints.SenseNearbyAgentsBlockedByWalls,
                automaticallyUpdateSlam: robotConstraints.AutomaticallyUpdateSlam,
                slamUpdateIntervalInTicks: robotConstraints.SlamUpdateIntervalInTicks,
                slamSynchronizeIntervalInTicks: robotConstraints.SlamSynchronizeIntervalInTicks,
                slamPositionInaccuracy: 0,
                mapKnown: robotConstraints.MapKnown,
                distributeSlam: robotConstraints.DistributeSlam,
                environmentTagReadRange: robotConstraints.EnvironmentTagReadRange,
                slamRayTraceRange: robotConstraints.SlamRayTraceRange,
                slamRayTraceCount: robotConstraints.SlamRayTraceCount,
                relativeMoveSpeed: robotConstraints.RelativeMoveSpeed,
                agentRelativeSize: robotConstraints.AgentRelativeSize,
                calculateSignalTransmissionProbability: (distance, wallDistance) => distance <= communicationRange && wallDistance == 0f,
                materialCommunication: robotConstraints.MaterialCommunication,
                frequency: robotConstraints.Frequency,
                transmitPower: robotConstraints.TransmitPower,
                receiverSensitivity: robotConstraints.ReceiverSensitivity,
                attenuationDictionary: robotConstraints.AttenuationDictionary,
                maxCommunicationRange: 100);
        }

        private class TestingAlgorithm : PatrollingAlgorithm
        {
            public override string AlgorithmName { get; } = "VirtualStigmergySimpleTest";

            public VirtualStigmergyComponent<string> VirtualStigmergyComponent { get; private set; }

            public Robot2DController Controller => _controller;

            protected override IComponent[] CreateComponents(Robot2DController controller, PatrollingMap patrollingMap)
            {
                VirtualStigmergyComponent =
                    new VirtualStigmergyComponent<string>(OnConflict, controller, controller.GetRobot());

                return new IComponent[] { VirtualStigmergyComponent };
            }

            private static VirtualStigmergyComponent<string>.ValueInfo OnConflict(string key,
                VirtualStigmergyComponent<string>.ValueInfo localValueInfo,
                VirtualStigmergyComponent<string>.ValueInfo incomingValueInfo)
            {
                return localValueInfo;
            }
        }
    }
}