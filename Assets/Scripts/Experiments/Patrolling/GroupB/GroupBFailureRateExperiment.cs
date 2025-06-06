﻿using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms.Patrolling;
using Maes.Simulation.Patrolling;
using Maes.UI;

using UnityEngine;
using UnityEngine.Scripting;

namespace Maes.Experiments.Patrolling.GroupB
{
    [Preserve]
    public class GroupBFailureRateExperiment : MonoBehaviour
    {
        private static readonly List<float> FailureRates = new() { 0.005f, 0.01f, 0.015f, 0.02f, 0.025f };

        private void Start()
        {
            var scenarios = new List<PatrollingSimulationScenario>();
            foreach (var seed in Enumerable.Range(0, GroupBParameters.StandardSeedCount))
            {
                foreach (var failureRate in FailureRates)
                {
                    scenarios.AddRange(ScenarioUtil.CreateScenarios(
                        seed,
                        nameof(ConscientiousReactiveAlgorithm),
                        GroupBParameters.PartitionedAlgorithms[nameof(ConscientiousReactiveAlgorithm)],
                        GroupBParameters.StandardRobotCount,
                        GroupBParameters.StandardMapSize,
                        GroupBParameters.StandardAmountOfCycles,
                        GroupBParameters.MaterialRobotConstraints,
                        1,
                        GroupBParameters.FaultInjection(seed, probability: failureRate))
                    );
                }
            }

            Debug.Log($"Total scenarios scheduled: {scenarios.Count}");

            var simulator = new PatrollingSimulator(scenarios);

            simulator.PressPlayButton(); // Instantly enter play mode
            simulator.SimulationManager.AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}