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

using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    /// <summary>
    /// AAU group cs-25-ds-10-17
    /// </summary>
    internal class GroupAMapSizeExperiment : MonoBehaviour
    {
        private static readonly List<int> _mapSizes = new() { 100, 150, 200, 250, 300 };

        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in GroupAParameters.SeedGenerator())
            {
                foreach (var (algorithmName, lambda) in GroupAParameters.StandardAlgorithms)
                {
                    var (patrollingMapFactory, algorithm) = lambda(GroupAParameters.StandardRobotCount);
                    foreach (var mapSize in _mapSizes)
                    {
                        scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, algorithmName, algorithm, patrollingMapFactory, GroupAParameters.StandardRobotCount, mapSize));
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