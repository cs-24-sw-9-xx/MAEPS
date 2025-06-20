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

using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;

namespace Maes.Experiments.Patrolling.GroupB
{
    using MySimulator = PatrollingSimulator;

    internal class ReactiveAlgorithmsExperiment : MonoBehaviour
    {
        private void Start()
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            foreach (var seed in Enumerable.Range(0, GroupBParameters.StandardSeedCount))
            {
                foreach (var (algName, lambda) in GroupAParameters.ReactiveAlgorithms)
                {
                    foreach (var count in GroupBParameters.RobotCounts)
                    {
                        var (patrollingMapFactory, algorithm) = lambda(count);

                        scenarios.Add(ScenarioUtil.CreateBuildingMapScenario(
                            seed,
                            algName,
                            algorithm,
                            count,
                            GroupBParameters.StandardMapSize,
                            GroupBParameters.StandardAmountOfCycles,
                            GroupBParameters.GlobalRobotConstraints,
                            1,
                            GroupBParameters.FaultInjection(robotCount: 1),
                            patrollingMapFactory)); // robotCount = 1 for no fault injection
                    }
                }
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}