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
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
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
            _genericAlgorithms.Add(nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm());
            _genericAlgorithms.Add(nameof(HeuristicConscientiousReactiveAlgorithm), (_) => new HeuristicConscientiousReactiveAlgorithm());
            _genericAlgorithms.Add(nameof(SingleCycleChristofides), (_) => new SingleCycleChristofides());
            _genericAlgorithms.Add(nameof(RandomReactive), (_) => new RandomReactive(_seeds.First())); // The map is different for each seed, so the algorithm can just use the same seed for all maps.
            // Todo add robots to run in a partitioned map.
        }

        private const int AmountOfCycles = 100; // Should be changed to 1000 for the final experiment?
        private static readonly List<int> _mapSizes = new() { 100, 150, 200, 250, 300 };
        private static readonly int _standardMapSize = 200;
        private static readonly List<int> _seeds = new() { 1, 2, 3, 4, 5 }; // Should be 100 random seeds for the final experiment
        private static readonly List<int> _robotCounts = new() { 1, 2, 4, 8, 16 };
        private static readonly int _standardRobotCount = 4;
        private static readonly string _standardRobotConstraintName = "Standard";
        private static readonly RobotConstraints _standardRobotConstraints = CreateRobotConstraints();

        /// <summary>
        /// These are run both on a partitioned map and a non-partitioned map.
        /// </summary>
        private static readonly Dictionary<string, CreateAlgorithmDelegate> _genericAlgorithms = new();

        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in _seeds)
            {
                foreach (var (algorithmName, algorithm) in _genericAlgorithms)
                {
                    foreach (var robotCount in _robotCounts)
                    {
                        scenarios.AddRange(CreateScenarios(seed, algorithmName, algorithm, robotCount, _standardMapSize));
                    }
                    foreach (var mapSize in _mapSizes)
                    {
                        scenarios.AddRange(CreateScenarios(seed, algorithmName, algorithm, _standardRobotCount, mapSize));
                    }
                }
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }

        private static IEnumerable<MySimulationScenario> CreateScenarios(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, int mapSize)
        {
            var scenarios = new List<MySimulationScenario>();
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var scenarioBuilding = CreateConstructor(seed, algorithmName, algorithm, robotCount, buildingMapConfig);
            var scenarioCave = CreateScenario(seed, algorithmName, algorithm, robotCount, caveMapConfig);
            scenarios.Add(scenarioBuilding);
            scenarios.Add(scenarioCave);
            return scenarios;
        }

        private static MySimulationScenario CreateConstructor(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, BuildingMapConfig mapConfig)
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

        private static MySimulationScenario CreateScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, CaveMapConfig mapConfig)
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

        /// <summary>
        /// Creates the robot constraints for the patrolling algorithms.
        /// The default values use LOS communication.
        /// </summary>
        private static RobotConstraints CreateRobotConstraints(float senseNearbyAgentsRange = 5f, bool senseNearbyAgentsBlockedByWalls = true, float communicationDistanceThroughWalls = 0f)
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
                slamRayTraceRange: 0f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= communicationDistanceThroughWalls,
                materialCommunication: true);
        }
    }
}