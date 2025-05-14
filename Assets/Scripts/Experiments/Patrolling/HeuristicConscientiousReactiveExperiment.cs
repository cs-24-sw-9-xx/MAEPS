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
// Rasmus Borrisholt Schmidt, 
// Andreas Sebastian Sørensen, 
// Thor Beregaard, 
// Malte Z. Andreasen, 
// Philip I. Holler,
// Magnus K. Jensen, 	
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Henrik van Peet,
// Jakob Meyer Olsen,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class HeuristicConscientiousReactiveExperiment : MonoBehaviour
    {
        private void Start()
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>
            {
                ["Global"] = new RobotConstraints(
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
                materialCommunication: true)
            };

            var scenarios = new List<MySimulationScenario>();
            var random = new System.Random(12345);
            var mapSize = 100;

            var constraintName = "Global";
            var robotConstraints = constraintsDict[constraintName];
            var buildingMapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = nameof(HeuristicConscientiousReactiveAlgorithm);
            const int robotCount = 1;

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    mapSpawner: generator => generator.GenerateMap(buildingMapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: (randomSeed) => new HeuristicConscientiousReactiveAlgorithm(randomSeed)),
                    statisticsFileName: $"{algoName}-seed-{buildingMapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    robotConstraints: robotConstraints
                )
            );

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    mapSpawner: generator => generator.GenerateMap(caveMapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: (randomSeed) => new HeuristicConscientiousReactiveAlgorithm(randomSeed)),
                    statisticsFileName: $"{algoName}-seed-{caveMapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    robotConstraints: robotConstraints
                )
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}