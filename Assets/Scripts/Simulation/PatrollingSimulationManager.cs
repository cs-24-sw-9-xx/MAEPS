using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;

namespace MAES.Simulation
{
    public class PatrollingSimulationManager : SimulationManager<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
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