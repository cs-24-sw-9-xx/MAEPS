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
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class NoPartitionCRExperiment : MonoBehaviour
    {
        private void Start()
        {
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
            var algo = new ConscientiousReactiveAlgorithm();

            var algoName = "No-Partition-CR-Algo";

            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            for (var robotCount = 1; robotCount <= 32; robotCount *= 2)
            {
                var mapName = "BuildingMap";
                var partitionNumber = 1;
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "CaveMap";
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(caveConfig),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "MapA";
                var map = CommonMaps.MapAMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "MapB";
                map = CommonMaps.MapBMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "IslandsMap";
                map = CommonMaps.IslandsMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "CircularMap";
                map = CommonMaps.CircularMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "CorridorMap";
                map = CommonMaps.CorridorMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );

                mapName = "GridMap";
                map = CommonMaps.GridMap();
                scenarios.Add(
                    new MySimulationScenario(
                        seed: seed,
                        totalCycles: numberOfCycles,
                        stopAfterDiff: false,
                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                            collisionMap: buildingConfig,
                            seed: seed,
                            numberOfRobots: robotCount,
                            createAlgorithmDelegate: _ => algo),
                        mapSpawner: generator => generator.GenerateMap(map, seed, brokenCollisionMap: false),
                        partitions: 2,
                        faultInjection: new DestroyRobotsRandomFaultInjection(seed, robotFailureRate, robotFailureDuration, robotFailureCount),
                        robotConstraints: robotConstraints,
                        statisticsFileName: $"{algoName}-robots-{robotCount}-map-{mapName}-Partitions-{partitionNumber}")
                );
            }

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}