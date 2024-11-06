using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        protected override GameObject LoadSimulatorGameObject() => Resources.Load<GameObject>("Patrolling_MAEPS");

        public static PatrollingSimulator GetInstance()
        {
            return (PatrollingSimulator)(_instance ??= new PatrollingSimulator());
        }

    }
}