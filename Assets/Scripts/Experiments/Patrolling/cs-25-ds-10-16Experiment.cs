// Copyright 2025 MAES
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
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen,
// 
// Original repository: https://github.com/Molitany/MAES

using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    internal class cs_25_ds_10_16Experiment : MonoBehaviour
    {
        private void Start()
        {
            const string constraintName = "Global";
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

            var simulator = new MySimulator();
            const int seed = 123;
            const int cycles = 100;

            var mapSizes = new List<int>
            {
                50,
                100,
                150,
                200,
                250,
                300
            };

            var partitionsCounts = new List<int> { 1, 2, 4 };
            var robotCounts = new List<int> { 1, 2, 4, 8, 16, 32 };

            var mapDict = new Dictionary<string, object>
            {
                { "IslandMap", CommonMaps.IslandsMap() },
                { "CorridorMap", CommonMaps.CorridorMap() },
                { "MapAMap", CommonMaps.MapAMap() },
                { "MapBMap", CommonMaps.MapBMap() },
                { "CircularMap", CommonMaps.CircularMap() },
                { "GridMap", CommonMaps.GridMap() }
            };

            foreach (var size in mapSizes)
            {
                var buildingKey = "BuildingMap" + size;
                var caveKey = "CaveMap" + size;

                mapDict.Add(buildingKey, new BuildingMapConfig(seed, widthInTiles: size, heightInTiles: size, brokenCollisionMap: false));
                mapDict.Add(caveKey, new CaveMapConfig(seed, widthInTiles: size, heightInTiles: size, brokenCollisionMap: false));
            }

            var algorithms = new Dictionary<string, PatrollingAlgorithm> // for all the redistribution types
            {
                { "CR", new ConscientiousReactiveAlgorithm() } // temp
                
            };

            var pos = new Vector2Int(0, 0);

            foreach (var robotCount in robotCounts)
            {
                foreach (var partitions in partitionsCounts)
                {
                    foreach (var (redisName, redisAlg) in algorithms)
                    {
                        foreach (var (mapName, mapConfig) in mapDict)
                        {
                            switch (mapConfig)
                            {
                                case Tile[,]:
                                    simulator.EnqueueScenario(
                                        new MySimulationScenario(
                                            seed: seed,
                                            totalCycles: cycles,
                                            partitions: partitions,
                                            stopAfterDiff: false,
                                            robotSpawner: (map, spawner) => spawner.SpawnRobotsApart(
                                                collisionMap: map,
                                                seed: seed,
                                                numberOfRobots: 8,
                                                createAlgorithmDelegate: _ => redisAlg),
                                            mapSpawner: generator => generator.GenerateMap(mapConfig as Tile[,], seed,
                                                brokenCollisionMap: false),
                                            robotConstraints: robotConstraints,
                                            statisticsFileName: $"{redisName}-seed-{seed}-map-{mapName}-partitions-{partitions}-comms-{constraintName}-robots-{robotCount}-SpawnApart")
                                    );
                                    break;
                                case CaveMapConfig:
                                    simulator.EnqueueScenario(
                                        new MySimulationScenario(
                                            seed: seed,
                                            totalCycles: cycles,
                                            partitions: partitions,
                                            stopAfterDiff: false,
                                            robotSpawner: (map, spawner) => spawner.SpawnRobotsApart(
                                                collisionMap: map,
                                                seed: seed,
                                                numberOfRobots: robotCount,
                                                createAlgorithmDelegate: _ => redisAlg),
                                            mapSpawner: generator => generator.GenerateMap((CaveMapConfig)mapConfig),
                                            robotConstraints: robotConstraints,
                                            statisticsFileName: $"{redisName}-seed-{seed}-map-{mapName}-partitions-{partitions}-comms-{constraintName}-robots-{robotCount}-SpawnApart")
                                    );
                                    break;
                                case BuildingMapConfig:
                                    simulator.EnqueueScenario(
                                        new MySimulationScenario(
                                            seed: seed,
                                            totalCycles: cycles,
                                            partitions: partitions,
                                            stopAfterDiff: false,
                                            robotSpawner: (map, spawner) => spawner.SpawnRobotsApart(
                                                collisionMap: map,
                                                seed: seed,
                                                numberOfRobots: robotCount,
                                                createAlgorithmDelegate: _ => redisAlg),
                                            mapSpawner: generator => generator.GenerateMap((BuildingMapConfig)mapConfig),
                                            robotConstraints: robotConstraints,
                                            statisticsFileName: $"{redisName}-seed-{seed}-map-{mapName}-partitions-{partitions}-comms-{constraintName}-robots-{robotCount}-SpawnApart")
                                    );
                                    break;
                            }
                        }
                    }
                }
            }
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }


}