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
using Maes.Simulation.Patrolling;

namespace Maes.Experiments.Patrolling
{
    using System.Collections.Generic;

    using Maes.Algorithms.Patrolling;
    using Maes.Map.Generators;
    using Maes.Robot;

    using UnityEngine;

    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class NoPartitionCRExperiment : MonoBehaviour
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
            // var robotFailureRate = 0.05f;
            // var robotFailureDuration = 1000;
            // var robotFailureCount = 2;

            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "No-Partition-CR-Algo";

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 1,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{1}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 2,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{2}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 4,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{4}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 8,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{8}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 16,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{16}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: numberOfCycles,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: 32,
                        createAlgorithmDelegate: _ => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 1,
                    // faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{32}-SpawnTogether")
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}