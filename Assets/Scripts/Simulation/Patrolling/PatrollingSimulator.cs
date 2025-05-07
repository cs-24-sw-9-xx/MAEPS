using System.Collections.Generic;

using Maes.Algorithms.Patrolling;

using UnityEngine;

namespace Maes.Simulation.Patrolling
{
    public sealed class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        public PatrollingSimulator(IReadOnlyList<PatrollingSimulationScenario> scenarios, bool autoMaxSpeedInBatchMode = true) : base(scenarios, autoMaxSpeedInBatchMode)
        {
        }

        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Patrolling_MAEPS");
        }
    }
}