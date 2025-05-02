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
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class MapSizeExperiment : MonoBehaviour
    {
        private void Start()
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>();
            var simulator = new MySimulator();
            var random = new System.Random(12345);
            var maxSize = 2000;
            var listOfScenarios = new List<MySimulationScenario>();
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
                    calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 3,
                    materialCommunication: true);
            var seed = 1234;
            var constraintName = "Global";
            var algoName = "conscientious_reactive";
            const int robotCount = 1;

            for (var i = 50; i < 250; i += 50)
            {
                var mapSize = i;
                var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                var mapConfig2 = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                var spawningPosList = new List<Vector2Int>();
                for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
                {
                    spawningPosList.Add(new Vector2Int(random.Next(-mapSize / 2, mapSize / 2), random.Next(-mapSize / 2, mapSize / 2)));
                }

                var buildingScenario = new MySimulationScenario(
                    seed: seed,
                    totalCycles: 1,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether");

                var caveScenario = new MySimulationScenario(
                    seed: seed,
                    totalCycles: 1,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig2),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig2.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether");

                listOfScenarios.Add(buildingScenario);
                listOfScenarios.Add(caveScenario);
            }

            foreach (var scenario in listOfScenarios)
            {
                simulator.EnqueueScenario(scenario);
            }

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}