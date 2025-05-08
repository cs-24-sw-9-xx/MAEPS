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

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class ConscientiousReactiveExperiment : MonoBehaviour
    {
        private void Start()
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>();

            //var constraintsGlobalCommunication = new RobotConstraints(
            constraintsDict["Global"] = new RobotConstraints(
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

            //var constraintsMaterials = new RobotConstraints(
            constraintsDict["Material"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                materialCommunication: true
            );

            //var constraintsLOS = new RobotConstraints(
            constraintsDict["LOS"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 0);

            var random = new System.Random(12345);
            var mapSize = 100;

            var constraintName = "Global";
            var robotConstraints = constraintsDict[constraintName];
            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var mapConfig2 = new BuildingMapConfig(124, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "conscientious_reactive";
            const int robotCount = 1;
            var spawningPosList = new List<Vector2Int>();
            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(-mapSize / 2, mapSize / 2), random.Next(-mapSize / 2, mapSize / 2)));
            }

            var scenarios = new MySimulationScenario[]
            {
                new(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName:
                    $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether"),
                new(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig2),
                    robotConstraints: robotConstraints,
                    statisticsFileName:
                    $"{algoName}-seed-{mapConfig2.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether"),
            };

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}