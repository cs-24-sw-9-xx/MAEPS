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
using Maes.Algorithms.Patrolling.HeuristicConscientiousReactive;
using Maes.Simulation.Patrolling;

using UnityEngine;

namespace Maes.Experiments.Patrolling
{
    using static Maes.Map.RobotSpawners.RobotSpawner<IPatrollingAlgorithm>;

    using MySimulationScenario = PatrollingSimulationScenario;
    using MySimulator = PatrollingSimulator;

    /// <summary>
    /// AAU group cs-25-ds-10-17
    /// </summary>
    internal class GroupAMapSizeExperiment : MonoBehaviour
    {
        public GroupAMapSizeExperiment()
        {
            _genericAlgorithms.Add(nameof(ConscientiousReactiveAlgorithm), (_) => new ConscientiousReactiveAlgorithm());
            _genericAlgorithms.Add(nameof(HeuristicConscientiousReactiveAlgorithm), (_) => new HeuristicConscientiousReactiveAlgorithm());
            _genericAlgorithms.Add(nameof(SingleCycleChristofides), (_) => new SingleCycleChristofides());
            _genericAlgorithms.Add(nameof(RandomReactive), (_) => new RandomReactive(1)); // The map is different for each seed, so the algorithm can just use the same seed for all maps.
            // Todo add robots to run in a partitioned map.
        }

        private static readonly List<int> _mapSizes = new() { 100, 150, 200, 250, 300 };

        /// <summary>
        /// These are run both on a partitioned map and a non-partitioned map.
        /// </summary>
        private static readonly Dictionary<string, CreateAlgorithmDelegate> _genericAlgorithms = new();

        private void Start()
        {
            var scenarios = new List<MySimulationScenario>();
            foreach (var seed in GroupAParameters.SeedGenerator(5))
            {
                foreach (var (algorithmName, algorithm) in _genericAlgorithms)
                {
                    foreach (var mapSize in _mapSizes)
                    {
                        scenarios.AddRange(GroupAExperimentHelpers.CreateScenarios(seed, algorithmName, algorithm, GroupAParameters.StandardRobotCount, mapSize));
                    }
                }
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new MySimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
        }
    }
}