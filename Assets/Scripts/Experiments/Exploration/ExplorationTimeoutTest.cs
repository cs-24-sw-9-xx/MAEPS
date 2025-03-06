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
using System.Linq;

using Maes.Algorithms.Exploration;
using Maes.Algorithms.Exploration.Greed;
using Maes.Algorithms.Exploration.Minotaur;
using Maes.Algorithms.Exploration.TheNextFrontier;
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
    public class ExplorationTimeoutTest : MonoBehaviour
    {
        private void Start()
        {
            MinosVsGreedSimulation("Global", "100");
        }

        public void MinosVsGreedSimulation(string constraintName, string amount)
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>();

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

            var algorithms = new Dictionary<string, RobotSpawner.CreateAlgorithmDelegate>
                {
                    { "tnf", seed => new TnfExplorationAlgorithm(1, 10, seed) },
                    { "minotaur", _ => new MinotaurAlgorithm(constraintsDict[constraintName], 2) },
                    { "greed", _ => new GreedAlgorithm() }
                };
            var simulator = new MySimulator();
            var random = new System.Random(1234);
            var randNumbers = new List<int>();
            for (var i = 0; i < 100; i++)
            {
                var val = random.Next(0, 1000000);
                randNumbers.Add(val);
            }

            var buildingConfigList = new List<BuildingMapConfig>();
            switch (amount)
            {
                case "50":
                    foreach (var val in randNumbers)
                    {
                        buildingConfigList.Add(new BuildingMapConfig(val, widthInTiles: 50, heightInTiles: 50));
                    }
                    break;
                case "75":
                    foreach (var val in randNumbers)
                    {
                        buildingConfigList.Add(new BuildingMapConfig(val, widthInTiles: 75, heightInTiles: 75));
                    }
                    break;
                case "100":
                    foreach (var val in randNumbers)
                    {
                        buildingConfigList.Add(new BuildingMapConfig(val, widthInTiles: 100, heightInTiles: 100));
                    }
                    break;
            }

            var algorithmName = "minotaur";
            foreach (var mapConfig in buildingConfigList)
            {
                for (var amountOfRobots = 1; amountOfRobots <= 9; amountOfRobots += 2)
                {
                    var robotCount = amountOfRobots;

                    if (robotCount == 5 && mapConfig.RandomSeed == 585462)
                    {
                        simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                            mapSpawner: generator => generator.GenerateMap(mapConfig),
                            robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                buildingConfig,
                                seed: 123,
                                numberOfRobots: robotCount,
                                suggestedStartingPoint: null,
                                createAlgorithmDelegate: algorithms[algorithmName]),
                            statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-mapConfig.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                            robotConstraints: constraintsDict[constraintName]));
                    }
                    else
                    {
                        simulator.EnqueueScenario(
                        new MySimulationScenario(seed: 123,
                            mapSpawner: generator => generator.GenerateMap(mapConfig),
                            robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                buildingConfig,
                                seed: 123,
                                numberOfRobots: robotCount,
                                suggestedStartingPoint: null,
                                createAlgorithmDelegate: algorithms[algorithmName]),
                            statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-mapConfig.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                            robotConstraints: constraintsDict[constraintName]));
                    }
                    var spawningPosHashSet = new HashSet<Vector2Int>();
                    while (spawningPosHashSet.Count < amountOfRobots)
                    {
                        spawningPosHashSet.Add(new Vector2Int(random.Next(-mapConfig.HeightInTiles / 2, mapConfig.HeightInTiles / 2), random.Next(-mapConfig.HeightInTiles / 2, mapConfig.HeightInTiles / 2)));
                    }

                    simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                                        collisionMap: buildingConfig,
                                        seed: 123,
                                        numberOfRobots: robotCount,
                                        spawnPositions: spawningPosHashSet.ToList(),
                                        createAlgorithmDelegate: algorithms[algorithmName]),
                                    statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-mapConfig.HeightInTiles-{mapConfig.HeightInTiles}-comms-{constraintName}-robots-{robotCount}-SpawnApart",
                                    robotConstraints: constraintsDict[constraintName]));
                }
            }

            //Just code to make sure we don't get too many maps of the last one in the experiment
            var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50);
            simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                    buildingConfig,
                                                                    seed: 123,
                                                                    numberOfRobots: 5,
                                                                    suggestedStartingPoint: Vector2Int.zero,
                                                                    createAlgorithmDelegate: _ => new MinotaurAlgorithm(constraintsDict[constraintName], 2)),
                statisticsFileName: $"delete-me",
                robotConstraints: constraintsDict[constraintName]));

            simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}