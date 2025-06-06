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
// Henrik van Peet,
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
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    [Preserve]
    internal class CognitiveCoordinatedVirtualStigmergyExperiment : MonoBehaviour
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
                calculateSignalTransmissionProbability: (_, _) => true);

            var scenarios = new List<MySimulationScenario>();

            var random = new System.Random(12345);
            var mapSize = 100;

            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = "cognitive_coordinated";
            const int robotCount = 3;

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: _ => new CognitiveCoordinatedVirtualStigmergy(robotCount)),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}