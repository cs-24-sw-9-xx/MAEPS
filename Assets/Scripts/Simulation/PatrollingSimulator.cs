using Maes.Algorithms;
using Maes.Simulation.SimulationScenarios;

using UnityEngine;

namespace Maes.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        protected override GameObject LoadSimulatorGameObject() => Resources.Load<GameObject>("Patrolling_MAEPS");
    }
}