// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler, Magnus K. Jensen,
//               Casper Nyvang Sørensen, Christian Ziegler Sejersen, Henrik Van Peet, Jakob Meyer Olsen, Mads Beyer Mogensen and Puvikaran Santhirasegaram 
// 
// Original repository: https://github.com/cs-24-sw-9-xx/MAEPS

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;
using UnityEngine;
using System.Collections.Generic;
using MAES.ExplorationAlgorithm.FollowWaypoints;

namespace Maes
{
    internal class ExampleProgramOneRobotAndSimulation : MonoBehaviour
    {
        private Simulator _simulator;
        /*
*/
        private void Start()
        {
            const int randomSeed = 12345;

            const string constraintName = "Global";
            var robotConstraints = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (distanceTravelled, distanceThroughWalls) => true);

            var simulator = Simulator.GetInstance();
            var random = new System.Random(randomSeed);
            const int robotCount = 1;
            const int size = 75;
            
            var randVal = random.Next(0, 1000000);
            var mapConfig = new BuildingMapConfig(randVal, widthInTiles: size, heightInTiles: size);

            const string algorithmName = "FollowWaypoints";
            var algorithm = new RobotSpawner.CreateAlgorithmDelegate(_ => new FollowWaypointsAlgorithm()); 
            

            var spawningPosList = new List<Vector2Int>();
            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(0, size), random.Next(0, size)));
            }

            simulator.EnqueueScenario(new SimulationScenario(seed: 123,
                                                             mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                             robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                                                                 collisionMap: buildingConfig,
                                                                 seed: 123,
                                                                 numberOfRobots: robotCount,
                                                                 spawnPositions: spawningPosList,
                                                                 createAlgorithmDelegate: algorithm),
                                                             statisticsFileName: $"{algorithmName}-seed-{mapConfig.RandomSeed}-size-{size}-comms-{constraintName}-robots-{robotCount}-SpawnApart",
                                                             robotConstraints: robotConstraints)
            );

            simulator.PressPlayButton(); // Instantly enter play mode
        }

    }
}
