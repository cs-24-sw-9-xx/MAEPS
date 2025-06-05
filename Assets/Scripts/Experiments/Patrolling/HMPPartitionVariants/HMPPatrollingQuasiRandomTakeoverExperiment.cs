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
// Contributors: 
// Henrik van Peet,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;
using Maes.Map.Generators;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.Patrolling;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    [Preserve]
    internal class HMPPatrollingQuasiRandomTakeoverExperiment : MonoBehaviour
    {
        private void Start()
        {
            const int robotCount = 4;
            const int seed = 123;
            const int mapSize = 100;
            const string algoName = "HMPPatrollingAlgorithm";

            const string constraintName = "local";
            var robotConstraints = new RobotConstraints(
                mapKnown: true,
                distributeSlam: false,
                slamRayTraceRange: 0f,
                robotCollisions: false,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls == 0f);

            var scenarios = new List<MySimulationScenario>();


            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);

            scenarios.Add(
                new MySimulationScenario(
                    seed: seed,
                    totalCycles: 100,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                        collisionMap: buildingConfig,
                        seed: seed,
                        numberOfRobots: robotCount,
                        suggestedStartingPoint: null,
                        createAlgorithmDelegate: _ => new HMPPatrollingAlgorithm(PartitionComponent.TakeoverStrategy.QuasiRandomStrategy)),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether",
                    patrollingMapFactory: map => AllWaypointConnectedGenerator.MakePatrollingMap(map, GroupAParameters.MaxDistance),
                    maxLogicTicks: SimulationScenario<PatrollingSimulation, IPatrollingAlgorithm>.DefaultMaxLogicTicks * 100)
            );

            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}