using Maes.UI;

using MAES.Simulation.SimulationScenarios;
using MAES.UI.SimulationInfoUIControllers;

namespace MAES.Simulation
{
    public interface ISimulationManager
    {
        ISimulationScenario CurrentScenario { get; }

        ISimulation CurrentSimulation { get; }
        ISimulationInfoUIController SimulationInfoUIController { get; }

        SimulationPlayState AttemptSetPlayState(SimulationPlayState targetState);
    }
}