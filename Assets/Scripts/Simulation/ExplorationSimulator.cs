using Maes;
using UnityEngine;

namespace MAES.Simulation
{
    public class ExplorationSimulator : Simulator<ExplorationSimulation>
    {
        protected override SimulationManager<ExplorationSimulation> AddSimulationManager(GameObject gameObject)
        {
            return gameObject.AddComponent<ExplorationSimulationManager>();
        }

        public static ExplorationSimulator GetInstance() {
            return (ExplorationSimulator) (_instance ??= new ExplorationSimulator());
        }
    }
}