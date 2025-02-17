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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
//                      Henrik van Peet & Jakob Meyer Olsen.
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Algorithms.Exploration.HenrikAlgo;
using Maes.Algorithms.Exploration.Minotaur;
using Maes.Map.MapGen;
using Maes.Map.RobotSpawners;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

using UnityEngine;

namespace Maes.ExperimentSimulations
{
    using MySimulationScenario = ExplorationSimulationScenario;
    using MySimulator = ExplorationSimulator;
    using RobotSpawner = RobotSpawner<IExplorationAlgorithm>;
    internal class HenrikExample : MonoBehaviour
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

            var simulator = new MySimulator();
            var random = new System.Random(1234);
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
                buildingConfigList100.Add(new BuildingMapConfig(val, widthInTiles: 100, heightInTiles: 100));
            }

            var mapSizes = new List<int> { 50, 75, 100 };
            var algorithms = new Dictionary<string, RobotSpawner.CreateAlgorithmDelegate>
                {
                    { "HenrikExplAlgo", _ => new HenrikExplorationAlgorithm() }
                };
            var buildingMaps = buildingConfigList100;
            foreach (var mapConfig in buildingMaps)
            {
                var robotCount = 5;
                foreach (var size in mapSizes)
                {
                    foreach (var (algorithmName, algorithm) in algorithms)
                    {

                        simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                                                                         mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                         robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                             buildingConfig,
                                                                             seed: 123,
                                                                             numberOfRobots: robotCount,
                                                                             suggestedStartingPoint: new Vector2Int(random.Next(0, size), random.Next(0, size)),
                                                                             createAlgorithmDelegate: algorithm),
                                                                         statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                                                                         robotConstraints: robotConstraints)
                        );
                    }
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
                                                                 createAlgorithmDelegate: _ => new MinotaurAlgorithm(robotConstraints, 2)),
                statisticsFileName: $"delete-me",
                robotConstraints: robotConstraints));

            simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }

    }
}