using MAES.Simulation.SimulationScenarios;

using Maes.UI;

using MAES.UI.SimulationInfoUIControllers;

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