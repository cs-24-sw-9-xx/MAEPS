using Maes.Robot;

namespace MAES.Simulation.SimulationScenarios
{
    public interface ISimulationScenario
    {
        MapFactory MapSpawner { get; }
        RobotConstraints RobotConstraints { get; }
        string StatisticsFileName { get; }
    }
}