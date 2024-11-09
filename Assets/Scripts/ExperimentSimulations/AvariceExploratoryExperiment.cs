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

using Maes.ExplorationAlgorithm.Greed;
using Maes.ExplorationAlgorithm.Minotaur;
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.Utilities.Files;

using UnityEngine;

namespace Maes.ExperimentSimulations
{
    using MySimulator = ExplorationSimulator;
    using MySimulationScenario = ExplorationSimulationScenario;
    
    internal class GreedExploratoryExperiments : MonoBehaviour
    {
        private void Start()
        {
            var los = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 4f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) =>
                    distanceThroughWalls <= 0);

            var simulator = MySimulator.GetInstance();

            var mapFromFile = PgmMapFileLoader.LoadMapFromFileIfPresent("blank_100.pgm");
            var random = new System.Random(1234);
            for (int i = 0; i < 10; i++)
            {
                var val = random.Next(0, 1000000);
                simulator.EnqueueScenario(new MySimulationScenario(val,
                    mapSpawner: generator => generator.GenerateMap(mapFromFile, val),
                    robotConstraints: los,
                    statisticsFileName: $"mino_blank_{val}",
                    robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map,
                        val,
                        5,
                        new Vector2Int(0, 0),
                        _ => new MinotaurAlgorithm(los, 4))));
                simulator.EnqueueScenario(new MySimulationScenario(val,
                    mapSpawner: generator => generator.GenerateMap(mapFromFile, val),
                    robotConstraints: los,
                    statisticsFileName: $"greed_blank_{val}",
                    robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(map,
                        val,
                        5,
                        new Vector2Int(0, 0),
                        _ => new GreedAlgorithm())));
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
                                                                 createAlgorithmDelegate: _ => new MinotaurAlgorithm(los, 4)),
                statisticsFileName: "delete-me",
                robotConstraints: los));

            simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}
