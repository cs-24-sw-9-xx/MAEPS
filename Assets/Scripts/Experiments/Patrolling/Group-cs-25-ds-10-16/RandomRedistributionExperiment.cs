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
// Contributors: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
//
// Original repository: https://github.com/Molitany/MAES
using Maes.Algorithms.Patrolling;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using System.Collections.Generic;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class RandomRedistributionExperiment : MonoBehaviour
    {
        private void Start()
        {
            var constraintName = "Global";
            var robotConstraints = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, _) => true,
                robotCollisions: false);

            var scenarios = new List<MySimulationScenario>();
            var seed = 12345;
            var random = new System.Random(seed);
            var mapSize = 100;
            var numberOfCycles = 10;
            var robotFailureRate = 0.05f;
            var robotFailureDuration = 1000;
            var robotFailureCount = 2;

            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "Random-Redistribution-CR-Algo";
            const int robotCount = 4; // Change this to the desired number of robots

            // Scenario with 2 partitions
            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: _ => new RandomRedistributionWithCRAlgo()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 2,
                    faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
            );

            // Scenario with 4 partitions
            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: _ => new RandomRedistributionWithCRAlgo()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 4,
                    faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}