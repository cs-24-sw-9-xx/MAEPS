using Maes.UI;
using UnityEngine;

namespace MAES.Simulation
{
    public interface ISimulationManager
    {
        ISimulationScenario CurrentScenario { get; }

        ISimulation CurrentSimulation { get; }
        ISimulationInfoUIController SimulationInfoUIController { get; }

        SimulationPlayState AttemptSetPlayState(SimulationPlayState targetState);

        void AddRestartRemakeController(GameObject restartRemakePanel);
    }
}