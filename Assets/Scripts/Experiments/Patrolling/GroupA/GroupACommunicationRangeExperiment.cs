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
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Map.Generators.Patrolling.Waypoints.Generators;
using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    /// <summary>
    /// AAU group cs-25-ds-10-17
    /// </summary>
    internal class GroupACommunicationRangeExperiment : MonoBehaviour
    {
        private readonly List<float> _communicationRangeThroughWalls = new List<float>() { 0, 1f, 2f, 3f, 4f, 5f };
        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            PatrollingMapFactory patrollingMapFactory = AllWaypointConnectedGenerator.MakePatrollingMap;
            CreateAlgorithmDelegate algorithm = (_) => new HMPPatrollingAlgorithm
            (
                new MeetingPointTimePartitionGenerator(new AdapterToPartitionGenerator(SpectralBisectionPartitioningGenerator.Generator))
            );
            var algorithmName = nameof(HMPPatrollingAlgorithm);

            foreach (var seed in GroupAParameters.SeedGenerator())
            {
                foreach (var communicationRangeThroughWalls in _communicationRangeThroughWalls)
                {
                    scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, algorithmName, algorithm, patrollingMapFactory, GroupAParameters.StandardRobotCount, GroupAParameters.StandardMapSize, communicationRangeThroughWalls));
                }

            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}