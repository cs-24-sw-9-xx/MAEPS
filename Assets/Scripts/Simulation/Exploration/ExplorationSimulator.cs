using System.Collections.Generic;

using Maes.Algorithms.Exploration;

using UnityEngine;

namespace Maes.Simulation.Exploration
{
    public sealed class ExplorationSimulator : Simulator<ExplorationSimulation, IExplorationAlgorithm, ExplorationSimulationScenario>
    {
        public ExplorationSimulator(IReadOnlyList<ExplorationSimulationScenario> scenarios, bool autoMaxSpeedInBatchMode = true) : base(scenarios, autoMaxSpeedInBatchMode)
        {
        }

        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Exploration_MAEPS");
        }
    }
}