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
using Maes.FaultInjections;
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.Patrolling;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;

    internal static class GroupAExperimentHelpers
    {
        public static MySimulationScenario ScenarioConstructor(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, PatrollingMapFactory? patrollingMapFactory, IMapConfig mapConfig, string mapName, int robotCount, RobotConstraints robotConstraints, IFaultInjection? faultInjection = null)
        {
            var faultInjectionString = string.Empty;
            if (faultInjection is DestroyRobotsAtSpecificTickFaultInjection fi)
            {
                faultInjectionString = $"-faultinjection-{string.Join(",", fi.DestroyAtTicks)}";
            }
            return new MySimulationScenario(
                                        seed: seed,
                                        totalCycles: int.MaxValue,
                                        stopAfterDiff: false,
                                        robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                                            collisionMap: buildingConfig,
                                            seed: seed,
                                            numberOfRobots: robotCount,
                                            createAlgorithmDelegate: algorithm,
                                            dependOnBrokenBehavior: false),
                                        faultInjection: faultInjection,
                                        mapSpawner: generator => generator.GenerateMap(mapConfig),
                                        robotConstraints: robotConstraints,
                                        statisticsFileName:
                                        $"{algorithmName}-seed-{seed}-size-{mapConfig.HeightInTiles}-robots-{robotCount}-constraints-{GroupAParameters.StandardRobotConstraintName}-{mapName}{faultInjectionString}-SpawnApart",
                                        patrollingMapFactory: patrollingMapFactory,
                                        hasFinishedSim: (PatrollingSimulation simulation,
                                            out SimulationEndCriteriaReason? reason) =>
                                        {
                                            if (simulation.SimulatedLogicTicks >= mapConfig.WidthInTiles * 2000)
                                            {
                                                reason = new SimulationEndCriteriaReason("We done", true);
                                                return true;
                                            }

                                            reason = null;
                                            return false;
                                        });
        }

        public static IEnumerable<MySimulationScenario> CreateScenarios(int seed, string algorithmName, CreateAlgorithmDelegate algorithm, PatrollingMapFactory? patrollingMapFactory, int robotCount = GroupAParameters.StandardRobotCount, int mapSize = GroupAParameters.StandardMapSize, float communicationDistanceThroughWalls = 0f, IFaultInjection faultInjection = null)
        {
            var scenarios = new List<MySimulationScenario>();
            var buildingMapConfig = new BuildingMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var caveMapConfig = new CaveMapConfig(seed, widthInTiles: mapSize, heightInTiles: mapSize, brokenCollisionMap: false);
            var scenarioBuilding = ScenarioConstructor(seed, algorithmName, algorithm, patrollingMapFactory, buildingMapConfig, "BuildingMap", robotCount, GroupAParameters.CreateRobotConstraints(communicationDistanceThroughWalls), faultInjection);
            var scenarioCave = ScenarioConstructor(seed, algorithmName, algorithm, patrollingMapFactory, caveMapConfig, "CaveMap", robotCount, GroupAParameters.CreateRobotConstraints(communicationDistanceThroughWalls), faultInjection);
            scenarios.Add(scenarioBuilding);
            scenarios.Add(scenarioCave);
            return scenarios;
        }
    }
}