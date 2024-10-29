using Maes;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;

namespace MAES.Simulation
{
    public class ExplorationSimulationManager : SimulationManager<ExplorationSimulation>
    {
        public override void AddRestartRemakeController(GameObject restartRemakePanel)
        {
            restartRemakePanel.AddComponent<ExplorationRestartRemakeContoller>();
        }

        protected override ExplorationSimulation AddSimulation(GameObject gameObject)
        {
            return gameObject.AddComponent<ExplorationSimulation>();
        }
    }
}