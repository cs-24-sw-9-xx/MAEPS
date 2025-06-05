// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram



using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    [Preserve]
    internal class SingleCycleChristofidesExperiment : MonoBehaviour
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

            var random = new System.Random(12345);
            var mapSize = 50;

            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var algoName = nameof(SingleCycleTSP);
            const int robotCount = 1;

            var scenarios = new List<MySimulationScenario>(){
                new(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: _ => new SingleCycleChristofides()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map, GroupAParameters.MaxDistance))
            };

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}