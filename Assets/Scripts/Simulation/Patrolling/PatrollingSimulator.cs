using Maes.Algorithms.Patrolling;

using UnityEngine;

namespace Maes.Simulation.Patrolling
{
    public sealed class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        protected override GameObject LoadSimulatorGameObject()
        {
            return Resources.Load<GameObject>("Patrolling_MAEPS");
        }
    }
}