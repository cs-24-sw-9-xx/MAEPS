using Maes.UI;
using Maes.UI.SimulationInfoUIControllers;

namespace Maes.Simulation
{
    public interface ISimulationManager
    {
        ISimulationScenario? CurrentScenario { get; }

        ISimulation? CurrentSimulation { get; }
        ISimulationInfoUIController SimulationInfoUIController { get; }

        SimulationPlayState AttemptSetPlayState(SimulationPlayState targetState);
    }
}