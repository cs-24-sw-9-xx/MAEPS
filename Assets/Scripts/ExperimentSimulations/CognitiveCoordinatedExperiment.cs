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

using UnityEngine;
using System.Collections.Generic;

using Maes.Map.MapGen;
using Maes.Robot;
using Maes.PatrollingAlgorithms;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;

namespace Maes.ExperimentSimulations
{
    using MySimulator = PatrollingSimulator;
    using MySimulationScenario = PatrollingSimulationScenario;
    
    internal class CognitiveCoordinatedExperiment : MonoBehaviour
    {
        private void Start()
        {
            const string constraintName = "Global"; //The type of robot comm
            var rc = new RobotConstraints(
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
                calculateSignalTransmissionProbability: (distanceTravelled, distanceThroughWalls) => true);

            var simulator = new MySimulator();
            var random = new System.Random(12345);
            
            const string algorithmName = "cognitive_coordinated"; 
            var algorithm = new CognitiveCoordinated();
            
            var mapConfig = new BuildingMapConfig(random.Next(0, 1000000), widthInTiles: 100, heightInTiles: 100);
            var size = 100;

            for (var amountOfRobots = 1; amountOfRobots < 10; amountOfRobots += 2)
            {
                var robotCount = amountOfRobots;
                simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: new Vector2Int(random.Next(-size / 2, size / 2),
                            random.Next(-size / 2, size / 2)),
                        createAlgorithmDelegate: (seed) => algorithm),
                    statisticsFileName:
                    $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    robotConstraints: rc));

                var spawningPosList = new List<Vector2Int>();
                for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
                {
                    spawningPosList.Add(new Vector2Int(random.Next(0, size), random.Next(0, size)));
                }

                simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        spawnPositions: spawningPosList,
                        createAlgorithmDelegate: (seed) => algorithm),
                    statisticsFileName:
                    $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnApart",
                    robotConstraints: rc));
            }

            //Just code to make sure we don't get too many maps of the last one in the experiment
            var dumpMap = new BuildingMapConfig(-1, widthInTiles: 50, heightInTiles: 50);
            simulator.EnqueueScenario(new MySimulationScenario(seed: 123,
                totalCycles: 4,
                stopAfterDiff: false,
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                 buildingConfig,
                                                                 seed: 123,
                                                                 numberOfRobots: 5,
                                                                 suggestedStartingPoint: Vector2Int.zero,
                                                                 createAlgorithmDelegate: (seed) => algorithm),
                statisticsFileName: $"delete-me",
                robotConstraints: rc));

            simulator.PressPlayButton(); // Instantly enter play mode
        }

    }
}
