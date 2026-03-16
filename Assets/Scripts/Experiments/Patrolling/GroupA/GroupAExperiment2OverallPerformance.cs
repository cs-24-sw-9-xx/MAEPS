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
    internal class GroupAExperiment2OverallPerformance : MonoBehaviour
    {
        private static readonly IEnumerable<string> scenarioFilters = new List<string>
        {
            // Paste the name of a scenario here to filter only that scenario e.g.:
            //"SingleCycleChristofides-seed-3-size-150-robots-8-constraints-Standard-BuildingMap-SpawnApart"
        };

        private void Start()
        {
            Debug.Log("Starting Group A Experiment 2: Overall Performance");
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in GroupAParameters.SeedGenerator(100))
            {
                var (patrollingMapFactory1, algorithm1) = GroupAParameters.FaultTolerantHMPVariants["NoSendAllFaultTolerance.NoMeetEarlyFixup.HMPPatrollingAlgorithm"](GroupAParameters.StandardRobotCount);
                var (patrollingMapFactory2, algorithm2) = GroupAParameters.FaultTolerantHMPVariants["FaultTolerance.NoMeetEarlyFixup.HMPPatrollingAlgorithm"](GroupAParameters.StandardRobotCount);
                scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, "HMP", algorithm1, patrollingMapFactory1));
                scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, "HMP.NotSendAll", algorithm2, patrollingMapFactory2));
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