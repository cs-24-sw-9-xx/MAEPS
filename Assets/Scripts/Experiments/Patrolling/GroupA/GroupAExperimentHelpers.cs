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

using System;
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;

    internal static class GroupAExperimentHelpers
    {
        public static MySimulationScenario ScenarioConstructor(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, PatrollingMapFactory? patrollingMapFactory, IMapConfig mapConfig, string mapName, int robotCount, RobotConstraints robotConstraints, bool shouldFail = false, int amountOfCycles = GroupAParameters.StandardAmountOfCycles)
        {
            return new MySimulationScenario(
                                        seed: seed,
                                        totalCycles: amountOfCycles,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm),
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: robotConstraints,
                                        statisticsFileName:
                                        $"{algorithmName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-constraints-{GroupAParameters.StandardRobotConstraintName}-{mapName}-SpawnApart",
                                        patrollingMapFactory: patrollingMapFactory,
                                        maxLogicTicks: shouldFail ? 0 : MaxLogicTicks(mapConfig.HeightInTiles, robotCount));
        }

        public static IEnumerable<MySimulationScenario> CreateScenarios(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, PatrollingMapFactory? patrollingMapFactory, int robotCount = GroupAParameters.StandardRobotCount, int mapSize = GroupAParameters.StandardMapSize, float communicationDistanceThroughWalls = 0f, bool shouldFail = false, int amountOfCycles = GroupAParameters.StandardAmountOfCycles)
        {
            var scenarios = new List<MySimulationScenario>();
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var scenarioBuilding = ScenarioConstructor(seed, algorithmName, algorithm, patrollingMapFactory, buildingMapConfig, "BuildingMap", robotCount, GroupAParameters.CreateRobotConstraints(communicationDistanceThroughWalls), shouldFail, amountOfCycles);
            var scenarioCave = ScenarioConstructor(seed, algorithmName, algorithm, patrollingMapFactory, caveMapConfig, "CaveMap", robotCount, GroupAParameters.CreateRobotConstraints(communicationDistanceThroughWalls), shouldFail, amountOfCycles);
            scenarios.Add(scenarioBuilding);
            scenarios.Add(scenarioCave);
            return scenarios;
        }

        private static int MaxLogicTicks(int mapSize, int robotCount)
        {
            return GroupAParameters.StandardAmountOfCycles
                           * MySimulationScenario.DefaultMaxLogicTicks
                           * (int)Math.Sqrt(mapSize);
        }
    }
}