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
using System.Linq;

using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    /// <summary>
    /// AAU group cs-25-ds-10-17
    /// </summary>
    [Preserve]
    internal class GroupAMapSizeToRobotCountRatio1SeedExperiment : MonoBehaviour
    {
        private static readonly List<int> _mapSizes = new() { 100, 150, 200, 250 };
        private static readonly List<int> _robotCounts = new() { 1, 2, 4, 8, 16 };
        private static readonly IEnumerable<string> scenarioFilters = new List<string>
        {
            // Paste the name of a scenario here to filter only that scenario e.g.:
            //"SingleCycleChristofides-seed-3-size-250-robots-16-constraints-Standard-BuildingMap-SpawnApart"
        };

        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in GroupAParameters.SeedGenerator(1))
            {
                foreach (var mapSize in _mapSizes)
                {
                    foreach (var (algorithmName, lambda) in GroupAParameters.AllAlgorithms)
                    {
                        foreach (var robotCount in _robotCounts)
                        {
                            var (patrollingMapFactory, algorithm) = lambda(robotCount);
                            scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, algorithmName, algorithm, patrollingMapFactory, robotCount, mapSize));
                        }
                    }
                }
            }

            if (scenarioFilters is not null && scenarioFilters.Any())
            {
                scenarios = scenarios.Where(scenario => scenarioFilters.Any(filter => scenario.StatisticsFileName.Contains(filter))).ToList();
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}