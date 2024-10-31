using Maes;
using Maes.Algorithms;

using MAES.UI.RestartRemakeContollers;
using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulationManager : SimulationManager<PatrollingSimulation, IPatrollingAlgorithm>
    {
        public override void AddRestartRemakeController(GameObject restartRemakePanel)
        {
            restartRemakePanel.AddComponent<PatrollingRestartRemakeController>();
        }

        protected override PatrollingSimulation AddSimulation(GameObject gameObject)
        {
            return gameObject.AddComponent<PatrollingSimulation>();
        }
    }
}