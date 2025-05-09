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
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    /// <summary>
    /// AAU group cs-25-ds-10-17
    /// </summary>
    internal class GroupAExperiment : MonoBehaviour
    {
        private const int AmountOfCycles = 4;
        private readonly List<int> _mapSizes = new() { 50, 100, 200 };
        private readonly List<int> _seeds = new() { 123, };
        private readonly List<int> _robotCounts = new() { 1, 4, 8, 16 };
        private readonly Dictionary<string, CreateAlgorithmDelegate> _algorithms = new Dictionary<string, CreateAlgorithmDelegate>
        {
            {nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm()}
        };

        private void Start()
        {
            var robotConstraintsDict = new Dictionary<string, RobotConstraints>();
            robotConstraintsDict["Standard"] = CreateRobotConstraints();

            var scenarios = new List<MySimulationScenario>();
            foreach (var (algorithmName, algorithm) in _algorithms)
            {
                foreach (var mapSize in _mapSizes)
                {
                    foreach (var robotCount in _robotCounts)
                    {
                        foreach (var seed in _seeds)
                        {
                            var mapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                            foreach (var (constraintName, constraint) in robotConstraintsDict)
                            {
                                var scenario = new MySimulationScenario(
                                    seed: seed,
                                    totalCycles: AmountOfCycles,
                                    stopAfterDiff: false,
                                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                        collisionMap: buildingConfig,
                                        seed: seed,
                                        numberOfRobots: robotCount,
                                        createAlgorithmDelegate: algorithm),
                                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                                    robotConstraints: constraint,
                                    statisticsFileName:
                                    $"{algorithmName}-seed-{seed}-size-{mapSize}-robots-{robotCount}-constraints-{constraintName}-SpawnApart");
                                scenarios.Add(scenario);
                            }
                        }
                    }
                }
            }

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }

        private RobotConstraints CreateRobotConstraints(float senseNearbyAgentsRange = 5f, bool senseNearbyAgentsBlockedByWalls = true, float communicationDistanceThroughWalls = 3f)
        {
            return new RobotConstraints(
                senseNearbyAgentsRange: senseNearbyAgentsRange,
                senseNearbyAgentsBlockedByWalls: senseNearbyAgentsBlockedByWalls,
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
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= communicationDistanceThroughWalls,
                materialCommunication: true);
        }
    }
}