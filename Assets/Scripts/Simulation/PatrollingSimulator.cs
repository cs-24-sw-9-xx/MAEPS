using Maes;
using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulator : Simulator<PatrollingSimulation>
    {
        protected override SimulationManager<PatrollingSimulation> AddSimulationManager(GameObject gameObject)
        {
            return gameObject.AddComponent<PatrollingSimulationManager>();
        }

        public static PatrollingSimulator GetInstance()
        {
            return (PatrollingSimulator)(_instance ??= new PatrollingSimulator());
        }
    }
}