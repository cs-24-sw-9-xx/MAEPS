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
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Henrik van Peet,
// Jakob Meyer Olsen,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram

using System;
using System.Collections.Generic;

using Maes.FaultInjections;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using static Maes.Map.RobotSpawners.RobotSpawner<Maes.Algorithms.Patrolling.IPatrollingAlgorithm>;


namespace Maes.Experiments.Patrolling
{
    /// <summary>
    /// Utility class for generating patrolling simulation scenarios with different map types and configurations.
    /// Provides methods to create scenarios for building maps, cave maps, and common maps,
    /// supporting various algorithms, robot counts, partition counts, and fault injection strategies.
    /// </summary>
    /// <remarks>
    /// s: seed
    /// ms: map size
    /// rc: robot count
    /// pc: partition count
    /// </remarks>
    public static class ScenarioUtil
    {
        public static IEnumerable<PatrollingSimulationScenario> CreateScenarios(
            int seed,
            string algorithmName,
            CreateAlgorithmDelegate algorithm,
            int robotCount,
            int mapSize,
            int cycles,
            RobotConstraints robotConstraints,
            int partitionNumber,
            (string Params, Func<IFaultInjection> Method) faultInjection)
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var scenarioBuilding = CreateMapScenario(seed, algorithmName, algorithm, "BuildingMap", robotCount, buildingMapConfig, cycles, robotConstraints, partitionNumber, faultInjection.Params, faultInjection.Method());
            var scenarioCave = CreateCaveMapScenario(seed, algorithmName, algorithm, "CaveMap", robotCount, caveMapConfig, cycles, robotConstraints, partitionNumber, faultInjection.Params, faultInjection.Method());
            scenarios.Add(scenarioBuilding);
            scenarios.Add(scenarioCave);
            return scenarios;
        }
        public static PatrollingSimulationScenario CreateCaveMapScenario(
            int seed,
            string algorithmName,
            CreateAlgorithmDelegate algorithm,
            int robotCount,
            int mapSize,
            int cycles,
            RobotConstraints robotConstraints,
            int partitionNumber,
            (string Params, Func<IFaultInjection> Method) faultInjection)
        {
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            return CreateCaveMapScenario(seed, algorithmName, algorithm, "CaveMap", robotCount, caveMapConfig, cycles, robotConstraints, partitionNumber, faultInjection.Params, faultInjection.Method());
        }

        public static PatrollingSimulationScenario CreateBuildingMapScenario(
            int seed,
            string algorithmName,
            CreateAlgorithmDelegate algorithm,
            int robotCount,
            int mapSize,
            int cycles,
            RobotConstraints robotConstraints,
            int partitionNumber,
            (string Params, Func<IFaultInjection> Method) faultInjection)
        {
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            return CreateMapScenario(seed, algorithmName, algorithm, "BuildingMap", robotCount, buildingMapConfig, cycles, robotConstraints, partitionNumber, faultInjection.Params, faultInjection.Method());
        }

        private static PatrollingSimulationScenario CreateMapScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, string mapName, int robotCount, IMapConfig mapConfig, int cycles, RobotConstraints robotConstraints, int partitionNumber, string faultInjectionParams, IFaultInjection? faultInjection)
        {
            return new PatrollingSimulationScenario(
                                        seed: seed,
                                        totalCycles: cycles,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm,
                                            dependOnBrokenBehavior: false),
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: robotConstraints,
                                        faultInjection: faultInjection,
                                        partitions: partitionNumber,
                                        maxLogicTicks: 1000000,
                                        statisticsFileName:
                                        $"{algorithmName}-map-{mapName}-s-{seed}-ms-{mapConfig.HeightInTiles}-rc-{robotCount}-pc-{partitionNumber}-{faultInjectionParams}");
        }

        private static PatrollingSimulationScenario CreateCaveMapScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, string mapName, int robotCount, CaveMapConfig mapConfig, int cycles, RobotConstraints robotConstraints, int partitionNumber, string faultInjectionParams, IFaultInjection? faultInjection)
        {
            return new PatrollingSimulationScenario(
                                        seed: seed,
                                        totalCycles: cycles,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm,
                                            dependOnBrokenBehavior: false),
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: robotConstraints,
                                        faultInjection: faultInjection,
                                        partitions: partitionNumber,
                                        maxLogicTicks: 1000000,
                                        statisticsFileName:
                                        $"{algorithmName}-map-{mapName}-s-{seed}-ms-{mapConfig.HeightInTiles}-rc-{robotCount}-pc-{partitionNumber}-{faultInjectionParams}");
        }
    }
}