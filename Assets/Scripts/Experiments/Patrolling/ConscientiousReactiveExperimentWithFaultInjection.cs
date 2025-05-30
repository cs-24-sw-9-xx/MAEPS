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
// Contributors: 
// Rasmus Borrisholt Schmidt, 
// Andreas Sebastian Sørensen, 
// Thor Beregaard, 
// Malte Z. Andreasen, 
// Philip I. Holler,
// Magnus K. Jensen, 	
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
using Maes.FaultInjections.DestroyRobots;
using Maes.Map.Generators;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    [Preserve]
    internal class ConscientiousReactiveExperimentWithFaultInjection : MonoBehaviour
    {
        private void Start()
        {
            var constraintsDict = new Dictionary<string, RobotConstraints>();

            //var constraintsGlobalCommunication = new RobotConstraints(
            constraintsDict["Global"] = new RobotConstraints(
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

            //var constraintsMaterials = new RobotConstraints(
            constraintsDict["Material"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                materialCommunication: true
            );

            //var constraintsLOS = new RobotConstraints(
            constraintsDict["LOS"] = new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                mapKnown: true,
                distributeSlam: false,
                environmentTagReadRange: 100.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (_, distanceThroughWalls) => distanceThroughWalls <= 0);

            var scenarios = new List<MySimulationScenario>();

            var random = new System.Random(12345);
            var mapSize = 100;

            var constraintName = "Global";
            var robotConstraints = constraintsDict[constraintName];
            var mapConfig = new BuildingMapConfig(123, widthInTiles: mapSize, heightInTiles: mapSize);
            var algoName = "conscientious_reactive";
            const int robotCount = 5;

            scenarios.Add(
                new MySimulationScenario(
                    seed: 123,
                    totalCycles: 4,
                    stopAfterDiff: false,
                    robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsApart(
                        collisionMap: buildingConfig,
                        seed: 123,
                        numberOfRobots: robotCount,
                        createAlgorithmDelegate: (_) => new ConscientiousReactiveAlgorithm()),
                    mapSpawner: generator => generator.GenerateMap(mapConfig),
                    robotConstraints: robotConstraints,
                    statisticsFileName: $"{algoName}-seed-{mapConfig.RandomSeed}-size-{mapSize}-comms-{constraintName}-robots-{robotCount}-SpawnTogether", faultInjection: new DestroyRobotsRandomFaultInjection(123, 0.05f, 1000, 4))
            );


            var simulator = new MySimulator(scenarios);
            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}