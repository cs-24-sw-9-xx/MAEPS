using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        protected override SimulationManager<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario> AddSimulationManager(GameObject gameObject)
        {
            return gameObject.AddComponent<PatrollingSimulationManager>();
        }
        
        public static PatrollingSimulator GetInstance() {
            return (PatrollingSimulator) (_instance ??= new PatrollingSimulator());
        }
    }
}