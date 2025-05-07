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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Maes.Algorithms.Exploration;
using Maes.Algorithms.Exploration.Greed;
using Maes.Algorithms.Exploration.Minotaur;
using Maes.Map.Generators;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation.Exploration;

using UnityEngine;

namespace Maes.Experiments.Exploration
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;
    using RobotSpawner = RobotSpawner<IExplorationAlgorithm>;

    internal class AvariceExperiments : MonoBehaviour
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
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
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
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
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
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 0);

            var scenarios = new List<MySimulationScenario>();


            var random = new System.Random(1234);
            var randNumbers = new List<int>();
            for (var i = 0; i < 100; i++)
            {
                var val = random.Next(0, 1000000);
                randNumbers.Add(val);
            }

            var buildingConfigList50 = new List<BuildingMapConfig>();
            var buildingConfigList75 = new List<BuildingMapConfig>();
            var buildingConfigList100 = new List<BuildingMapConfig>();
            foreach (var val in randNumbers)
            {
                buildingConfigList50.Add(new BuildingMapConfig(val, widthInTiles: 50, heightInTiles: 50));
                buildingConfigList75.Add(new BuildingMapConfig(val, widthInTiles: 75, heightInTiles: 75));
                buildingConfigList100.Add(new BuildingMapConfig(val, widthInTiles: 100, heightInTiles: 100));
            }

            var previousSimulations = Directory.GetFiles(Path.GetFullPath("./" + GlobalSettings.StatisticsOutPutPath));
            foreach (var (constraintName, constraint) in constraintsDict)
            {
                RobotSpawner.CreateAlgorithmDelegate algorithm = _ => new GreedAlgorithm();
                var buildingMaps = buildingConfigList50.Union(buildingConfigList75.Union(buildingConfigList100));
                foreach (var mapConfig in buildingMaps)
                {
                    for (var amountOfRobots = 1; amountOfRobots <= 9; amountOfRobots += 2)
                    {
                        var robotCount = amountOfRobots;

                        var regex = new Regex($@"avarice-seed-{mapConfig.RandomSeed}-mapConfig\.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnTogether_.*\.csv");
                        if (!previousSimulations.Any(simulation => regex.IsMatch(simulation)))
                        {
                            scenarios.Add(new MySimulationScenario(seed: 123,
                                                                                mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                                    buildingConfig,
                                                                                    seed: 123,
                                                                                    numberOfRobots: robotCount,
                                                                                    suggestedStartingPoint: null,
                                                                                    createAlgorithmDelegate: algorithm),
                                                                                statisticsFileName: $"avarice-seed-{mapConfig.RandomSeed}-mapConfig.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                                                                                robotConstraints: constraint)
                            );
                        }

                        var spawningPosHashSet = new HashSet<Vector2Int>();
                        while (spawningPosHashSet.Count < amountOfRobots)
                        {
                            spawningPosHashSet.Add(new Vector2Int(random.Next(-mapConfig.HeightInTiles / 2, mapConfig.HeightInTiles / 2), random.Next(-mapConfig.HeightInTiles / 2, mapConfig.HeightInTiles / 2)));
                        }

                        regex = new Regex($@"avarice-seed-{mapConfig.RandomSeed}-mapConfig\.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnApart_.*\.csv");
                        if (!previousSimulations.Any(simulation => regex.IsMatch(simulation)))
                        {
                            scenarios.Add(new MySimulationScenario(seed: 123,
                                                                            mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                            robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                                                                                collisionMap: buildingConfig,
                                                                                seed: 123,
                                                                                numberOfRobots: robotCount,
                                                                                spawnPositions: spawningPosHashSet.ToList(),
                                                                                createAlgorithmDelegate: algorithm),
                                                                            statisticsFileName: $"avarice-seed-{mapConfig.RandomSeed}-mapConfig.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnApart",
                                                                            robotConstraints: constraint)
                        );
                        }
                    }
                }
            }

            //Just code to make sure we don't get too many maps of the last one in the experiment
            var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50);
            scenarios.Add(new MySimulationScenario(seed: 123,
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                 buildingConfig,
                                                                 seed: 123,
                                                                 numberOfRobots: 5,
                                                                 suggestedStartingPoint: Vector2Int.zero,
                                                                 createAlgorithmDelegate: _ => new MinotaurAlgorithm(constraintsDict["Global"], 2)),
                statisticsFileName: $"delete-me",
                robotConstraints: constraintsDict["Global"]));

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}