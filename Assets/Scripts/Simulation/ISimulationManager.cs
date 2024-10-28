using Maes.UI;

namespace MAES.Simulation
{
    public interface ISimulationManager
    {
        ISimulationScenario CurrentScenario { get; }
        
        ISimulation GetCurrentSimulation();
        ISimulationInfoUIController GetSimulationInfoUIController();

        SimulationPlayState AttemptSetPlayState(SimulationPlayState targetState);
    }
}