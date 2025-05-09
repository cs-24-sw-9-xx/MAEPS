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
using System.Linq;

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
        public GroupAExperiment()
        {
            _algorithms.Add(nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm());
            _algorithms.Add(nameof(RandomReactive), (_) => new RandomReactive(_seeds.First()));
        }

        private const int AmountOfCycles = 4; // Should be changed to 1000 for the final experiment
        private readonly List<int> _mapSizes = new() { 50, 100, 150, 200 };
        private readonly int _standardMapSize = 100;
        private readonly List<int> _seeds = new() { 123, 123456 };
        private readonly List<int> _robotCounts = new() { 1, 2, 4, 8, 16 };
        private readonly int _standardRobotCount = 8;
        private readonly string _standardRobotConstraintName = "Standard";
        private readonly RobotConstraints _standardRobotConstraints = CreateRobotConstraints();
        private readonly Dictionary<string, CreateAlgorithmDelegate> _algorithms = new Dictionary<string, CreateAlgorithmDelegate>();

        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in _seeds)
            {
                foreach (var (algorithmName, algorithm) in _algorithms)
                {
                    foreach (var robotCount in _robotCounts)
                    {
                        var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: _standardMapSize, heightInTiles: _standardMapSize, brokenCollisionMap: false);
                        var caveMapConfig = new CaveMapConfig(seed, widthInTiles: _standardMapSize, heightInTiles: _standardMapSize, brokenCollisionMap: false);
                        var scenarioBuilding = CreateScenario(seed, algorithmName, algorithm, robotCount, buildingMapConfig);
                        var scenarioCave = CreateScenario(seed, algorithmName, algorithm, robotCount, caveMapConfig);
                        scenarios.Add(scenarioBuilding);
                        scenarios.Add(scenarioCave);
                    }
                    foreach (var mapSize in _mapSizes)
                    {
                        var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                        var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
                        var scenarioBuilding = CreateScenario(seed, algorithmName, algorithm, _standardRobotCount, buildingMapConfig);
                        var scenarioCave = CreateScenario(seed, algorithmName, algorithm, _standardRobotCount, caveMapConfig);
                        scenarios.Add(scenarioBuilding);
                        scenarios.Add(scenarioCave);
                    }
                }
            }

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }

        private MySimulationScenario CreateScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, BuildingMapConfig mapConfig)
        {
            return new MySimulationScenario(
                                        seed: seed,
                                        totalCycles: AmountOfCycles,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm),
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: _standardRobotConstraints,
                                        statisticsFileName:
                                        $"{algorithmName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-constraints-{_standardRobotConstraintName}-BuldingMap-SpawnApart");
        }

        private MySimulationScenario CreateScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, CaveMapConfig mapConfig)
        {
            return new MySimulationScenario(
                                        seed: seed,
                                        totalCycles: AmountOfCycles,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm),
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: _standardRobotConstraints,
                                        statisticsFileName:
                                        $"{algorithmName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-constraints-{_standardRobotConstraintName}-CaveMap-SpawnApart");
        }

        private static RobotConstraints CreateRobotConstraints(float senseNearbyAgentsRange = 5f, bool senseNearbyAgentsBlockedByWalls = true, float communicationDistanceThroughWalls = 3f)
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