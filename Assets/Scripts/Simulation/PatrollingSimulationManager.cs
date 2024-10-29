using Maes;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulationManager : SimulationManager<PatrollingSimulation>
    {
        public override void AddRestartRemakeController(GameObject restartRemakePanel)
        {
            restartRemakePanel.AddComponent<PatrollingRestartRemakeContoller>();
        }

        protected override PatrollingSimulation AddSimulation(GameObject gameObject)
        {
            return gameObject.AddComponent<PatrollingSimulation>();
        }
    }
}