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

using System.Collections.Generic;

using Maes.FaultInjections;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using static Maes.Map.RobotSpawners.RobotSpawner<Maes.Algorithms.Patrolling.IPatrollingAlgorithm>;


namespace Maes.Experiments.Patrolling
{
    public static class ScenarioUtil
    {
        public static IEnumerable<PatrollingSimulationScenario> CreateScenarios(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, int robotCount, int mapSize, int cycles, RobotConstraints robotConstraints, int partitionNumber, IFaultInjection? faultInjection = null)
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var scenarioBuilding = CreateBuildingMapScenario(seed, algorithmName, algorithm, "BuildingMap", robotCount, buildingMapConfig, cycles, robotConstraints, partitionNumber, faultInjection);
            var scenarioCave = CreateCaveMapScenario(seed, algorithmName, algorithm, "CaveMap", robotCount, caveMapConfig, cycles, robotConstraints, partitionNumber, faultInjection);
            scenarios.Add(scenarioBuilding);
            scenarios.Add(scenarioCave);
            return scenarios;
        }

        private static PatrollingSimulationScenario CreateBuildingMapScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, string mapName, int robotCount, BuildingMapConfig mapConfig, int cycles, RobotConstraints robotConstraints, int partitionNumber, IFaultInjection? faultInjection)
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
                                        statisticsFileName:
                                        $"{algorithmName}-map-{mapName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-partitions-{partitionNumber}-SpawnApart");
        }

        private static PatrollingSimulationScenario CreateCaveMapScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, string mapName, int robotCount, CaveMapConfig mapConfig, int cycles, RobotConstraints robotConstraints, int partitionNumber, IFaultInjection? faultInjection)
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
                                        statisticsFileName:
                                        $"{algorithmName}-map-{mapName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-partitions-{partitionNumber}-SpawnApart");
        }

        private static PatrollingSimulationScenario CreateCommonMapsScenario(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, string mapName, int robotCount, Tile[,] mapConfig, int cycles, RobotConstraints robotConstraints, int partitionNumber, IFaultInjection? faultInjection)
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
                                        mapSpawner: generator => generator.GenerateMap(mapConfig, seed,
                                                brokenCollisionMap: false),
                                        robotConstraints: robotConstraints,
                                        faultInjection: faultInjection,
                                        partitions: partitionNumber,
                                        statisticsFileName:
                                        $"{algorithmName}-map-{mapName}-seed-{seed}-robots-{robotCount}-partitions-{partitionNumber}-SpawnApart");
        }
    }

}