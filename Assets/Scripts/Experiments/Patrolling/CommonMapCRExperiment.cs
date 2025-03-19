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
// Henrik van Peet,
// Jakob Meyer Olsen,
// Mads Beyer Mogensen,
// Puvikaran Santhirasegaram
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

    internal class CommonMapCRExperiment : MonoBehaviour
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

            var simulator = new MySimulator();
            var random = new System.Random(12345);

            var algoName = "conscientious_reactive";
            const int robotCount = 1;
            var seed = 123;
            var spawningPosList = new List<Vector2Int>();

            var maps = new List<Tile[,]>{
                CommonMaps.IslandsMap(),
                CommonMaps.CorridorMap(),
                CommonMaps.MapAMap(),
                CommonMaps.MapBMap(),
                CommonMaps.CircularMap(),
                CommonMaps.GridMap()};

            var mapNames = new List<string>{
                "IslandMap",
                "CorridorMap",
                "MapAMap",
                "MapBMap",
                "CircularMap",
                "GridMap"};

            var i = 0;

            var mapIndex = 0;
            foreach (var currentMap in maps)
            {
                var mapName = mapNames[mapIndex];
                var val = random.Next(0, 1000000);
                simulator.EnqueueScenario(
                    new MySimulationScenario(
                        seed: val,
                        totalCycles: 4,
                        stopAfterDiff: false,
                        mapSpawner: generator => generator.GenerateMap(currentMap, val, brokenCollisionMap: false),
                        robotSpawner: (map, spawner) => spawner.SpawnRobotsTogether(
                            collisionMap: map,
                            val,
                            robotCount,
                            new Vector2Int(0, 0),
                            _ => new ConscientiousReactiveAlgorithm()),
                        robotConstraints: robotConstraints,
                        statisticsFileName:
                        $"{algoName}-map-{mapName}-seed-{seed}-comms-{constraintName}-robots-{robotCount}-SpawnTogether")
                );
                mapIndex++;
            }

            var mapSizeX = 50;
            var mapSizeY = 40;

            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(-mapSizeX / 2, mapSizeX / 2), random.Next(-mapSizeY / 2, mapSizeY / 2)));
            }

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}