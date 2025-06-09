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

    internal class InternalAlgorithmsExperiment : MonoBehaviour
    {
        private static readonly List<int> PartitionCount = new() { 1, 2, 4, 8 };
        private static readonly List<string> AlgorithmName = new() { nameof(ConscientiousReactiveAlgorithm), nameof(RandomReactive) };

        private void Start()
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            foreach (var seed in Enumerable.Range(0, GroupBParameters.StandardSeedCount))
            {
                foreach (var algName in AlgorithmName)
                {
                    foreach (var count in PartitionCount)
                    {

                        scenarios.AddRange(ScenarioUtil.CreateScenarios(
                            seed,
                            algName,
                            GroupBParameters.Algorithms[algName],
                            GroupBParameters.StandardRobotCount,
                            GroupBParameters.StandardMapSize,
                            GroupBParameters.StandardAmountOfCycles,
                            GroupBParameters.GlobalRobotConstraints,
                            count,
                            GroupBParameters.FaultInjection(seed,
                                robotCount: 1))); // robotCount = 1 for no fault injection

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