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
// Henrik van Peet,
// Jakob Meyer Olsen,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    [Preserve]
    internal class RandomReactiveExperiment : MonoBehaviour
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
                calculateSignalTransmissionProbability: (_, _) => true);

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

            var scenarios = new List<MySimulationScenario>();

            var random = new System.Random(12345);
            var randNumbers = new List<int>();
            for (var i = 0; i < 100; i++)
            {
                var val = random.Next(0, 1000000);
                randNumbers.Add(val);
            }

            var constraintName = "Global";
            var robotConstraints = constraintsDict[constraintName];

            var buildingConfigList100 = new List<BuildingMapConfig>();
            foreach (var val in randNumbers)
            {
                buildingConfigList100.Add(new BuildingMapConfig(val, widthInTiles: 100, heightInTiles: 100, brokenCollisionMap: false));
            }

            var mapSizes = new List<int> { 50, 75, 100 };
            var algorithms = new Dictionary<string, RobotSpawner<IPatrollingAlgorithm>.CreateAlgorithmDelegate>
                {
                    { "random_reactive", seed => new RandomReactive(seed) },
                };
            var buildingMaps = ((buildingConfigList100));
            foreach (var mapConfig in buildingMaps)
            {
                for (var amountOfRobots = 1; amountOfRobots < 10; amountOfRobots += 2)
                {
                    var robotCount = amountOfRobots;
                    foreach (var size in mapSizes)
                    {
                        foreach (var (algorithmName, algorithm) in algorithms)
                        {
                            scenarios.Add(new MySimulationScenario(seed: 123,
                                                                             totalCycles: 3,
                                                                             stopAfterDiff: true,
                                                                             robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                                 buildingConfig,
                                                                                 seed: 123,
                                                                                 numberOfRobots: robotCount,
                                                                                 suggestedStartingPoint: null,
                                                                                 createAlgorithmDelegate: algorithm),
                                                                             mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                             robotConstraints: robotConstraints,
                                                                             statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
                            );

                            scenarios.Add(new MySimulationScenario(seed: 123,
                                                                             totalCycles: 3,
                                                                             stopAfterDiff: true,
                                                                             robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                                                                 collisionMap: buildingConfig,
                                                                                 seed: 123,
                                                                                 numberOfRobots: robotCount,
                                                                                 createAlgorithmDelegate: algorithm),
                                                                             mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                             robotConstraints: robotConstraints,
                                                                             statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnApart")
                            );
                        }
                    }
                }
            }

            //Just code to make sure we don't get too many maps of the last one in the experiment
            var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50, brokenCollisionMap: false);
            scenarios.Add(new MySimulationScenario(seed: 123,
                totalCycles: 3,
                stopAfterDiff: true,
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                    buildingConfig,
                    seed: 123,
                    numberOfRobots: 5,
                    suggestedStartingPoint: Vector2Int.zero,
                    createAlgorithmDelegate: seed => new RandomReactive(seed)),
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotConstraints: robotConstraints,
                statisticsFileName: "delete-me"));

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}