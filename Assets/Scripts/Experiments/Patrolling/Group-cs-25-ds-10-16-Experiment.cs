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
// Jakob Meyer Olsen

using System;
using System.Collections.Generic;

using Maes.Algorithms.Patrolling;
using Maes.FaultInjections;
using Maes.FaultInjections.DestroyRobots;
using Maes.Robot;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulator = PatrollingSimulator;

    internal class Group_CS_25_DS_10_16_Experiment : MonoBehaviour
    {
        private static readonly List<int> _partitionNumbers = new() { 2, 4 };
        private static readonly List<int> _mapSizes = new() { 100, 150, 200 };
        private static readonly List<int> _seeds = new() { 123, 321, 132, 213, 231 };
        private static readonly List<int> _robotCounts = new() { 4, 8, 16, 32 };
        private CreateAlgorithmDelegate Algorithm => (_) => new GlobalRedistributionWithCRAlgo();
        private readonly string _algorithmName = "Global Redistribution CR Algorithm";
        private const int NumberOfCycles = 10;
        private const float RobotFailureRate = 0.05f;
        private const int RobotFailureDuration = 1000;
        private static readonly RobotConstraints RobotConstraints = new(
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
                calculateSignalTransmissionProbability: (_, _) => true,
                robotCollisions: false);

        private void Start()
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            foreach (var seed in _seeds)
            {
                foreach (var robotCount in _robotCounts)
                {
                    foreach (var mapSize in _mapSizes)
                    {
                        foreach (var partition in _partitionNumbers)
                        {
                            Func<IFaultInjection> faultInjection = () => new DestroyRobotsRandomFaultInjection(seed, RobotFailureRate, RobotFailureDuration, robotCount - 1);
                            scenarios.AddRange(ScenarioUtil.CreateScenarios(seed, _algorithmName, Algorithm, robotCount, mapSize, NumberOfCycles, RobotConstraints, partition, faultInjection));
                        }
                    }
                }
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}