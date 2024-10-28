using Maes;
using UnityEngine;

namespace MAES.Simulation
{
    public class ExplorationSimulationManager : SimulationManager<ExplorationSimulation>
    {
        protected override ExplorationSimulation AddSimulation(GameObject gameObject)
        {
            return gameObject.AddComponent<ExplorationSimulation>();
        }
    }
}