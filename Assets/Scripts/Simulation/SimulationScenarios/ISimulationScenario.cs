using Maes.Robot;

namespace Maes.Simulation.SimulationScenarios
{
    public interface ISimulationScenario
    {
        MapFactory MapSpawner { get; }
        RobotConstraints RobotConstraints { get; }
        string StatisticsFileName { get; }
    }
}