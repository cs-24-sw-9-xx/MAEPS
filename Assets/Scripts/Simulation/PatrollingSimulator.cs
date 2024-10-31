using Maes;
using Maes.Algorithms;

using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation, IPatrollingAlgorithm>
    {
        protected override SimulationManager<PatrollingSimulation, IPatrollingAlgorithm> AddSimulationManager(GameObject gameObject)
        {
            return gameObject.AddComponent<PatrollingSimulationManager>();
        }
        
        public static PatrollingSimulator GetInstance() {
            return (PatrollingSimulator) (_instance ??= new PatrollingSimulator());
        }
    }
}