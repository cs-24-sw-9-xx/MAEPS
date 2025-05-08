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
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
// 
// Original repository: https://github.com/Molitany/MAES


using System.Collections.Generic;

using Maes.Algorithms.Patrolling.PartitionedAlgorithms;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class PartitionedConscientiousReactiveExperiment : MonoBehaviour
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
                materialCommunication: true,
                robotCollisions: false);

            var scenarios = new List<MySimulationScenario>();
            var random = new System.Random(12345);
            var mapSize = 100;

            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var mapConfig2 = new BuildingMapConfig(124, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "partitioned_conscientious_reactive";
            const int robotCount = 8;
            var spawningPosList = new List<Vector2Int>();
            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(-mapSize / 2, mapSize / 2), random.Next(-mapSize / 2, mapSize / 2)));
            }

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 10,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new PartitionedConscientiousReactive()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    partitions: 4,
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 10,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new PartitionedConscientiousReactive()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig2),
                    partitions: 4,
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig2.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
            );


            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}