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

using Maes.Algorithms.Patrolling.PartitionedRedistribution;
using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;

namespace Maes.Experiments.Patrolling.GroupB
{
    using MySimulator = PatrollingSimulator;

    internal class GlobalRedistributionPartitionCountExperiment : MonoBehaviour
    {
        private static readonly List<int> PartitionCount = new() { 2, 4 };
        private void Start()
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            foreach (var seed in Enumerable.Range(0, GroupBParameters.StandardSeedCount))
            {
                foreach (var count in PartitionCount)
                {

                    scenarios.AddRange(ScenarioUtil.CreateScenarios(
                        seed,
                        nameof(GlobalRedistributionWithCRAlgo),
                        GroupBParameters.Algorithms[nameof(GlobalRedistributionWithCRAlgo)],
                        GroupBParameters.StandardRobotCount,
                        GroupBParameters.StandardMapSize,
                        2,//GroupBParameters.StandardAmountOfCycles,
                        GroupBParameters.GlobalRobotConstraints,
                        count,
                        GroupBParameters.FaultInjection(seed)));
                }

            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}