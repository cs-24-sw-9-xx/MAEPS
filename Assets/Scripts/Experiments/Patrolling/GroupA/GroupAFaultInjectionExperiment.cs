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
using System.Text.RegularExpressions;

using Maes.FaultInjections.DestroyRobots;
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
    internal class GroupAFaultInjectionExperiment : MonoBehaviour
    {
        public string FilterRegex = ".*";

        private static readonly List<int> _mapSizes = new() { 150 };
        private static readonly List<int> _robotCounts = new() { 8 };
        private static readonly List<int[]> _faultInjections = new List<int[]>
        {
            new int[] {500},
            new int[] {500, 1000},
            new int[] {1000, 1001},
            new int[] {500, 1000, 1500},
        };
        private static readonly IEnumerable<string> scenarioFilters = new List<string>
        {
            // Paste the name of a scenario here to filter only that scenario e.g.:
            //"SingleCycleChristofides-seed-3-size-250-robots-16-constraints-Standard-BuildingMap-SpawnApart"
        };

        private void Start()
        {
            var regex = new Regex(FilterRegex, RegexOptions.Singleline | RegexOptions.CultureInvariant);
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in GroupAParameters.SeedGenerator(10))
            {
                foreach (var mapSize in _mapSizes.OrderByDescending(x => x))
                {
                    foreach (var robotCount in _robotCounts)
                    {
                        foreach (var faultInjectionTimes in _faultInjections)
                        {
                            var faultInjection =
                                new DestroyRobotsAtSpecificTickFaultInjection(seed, faultInjectionTimes.Select(tick => tick * mapSize).ToArray());
                            var (patrollingMapFactory, algorithm) = GroupAParameters.FaultTolerantHMPVariants["NoSendAllFaultTolerance.NoMeetEarlyFixup.HMPPatrollingAlgorithm"](GroupAParameters.StandardRobotCount);
                            scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, "HMP.NoSendAll", algorithm, patrollingMapFactory, robotCount, mapSize, faultInjection: faultInjection));
                        }
                    }
                }
            }

            if (scenarioFilters is not null && scenarioFilters.Any())
            {
                scenarios = scenarios.Where(scenario => scenarioFilters.Any(filter => scenario.StatisticsFileName.Contains(filter))).ToList();
            }

            scenarios = scenarios.Where(s => regex.IsMatch(s.StatisticsFileName)).ToList();

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}