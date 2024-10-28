using Maes;
using Maes.Robot;

namespace MAES.Simulation
{
    public interface ISimulationScenario
    {
        MapFactory MapSpawner { get; }
        RobotFactory RobotSpawner { get; }
        RobotConstraints RobotConstraints { get; }
        string StatisticsFileName { get; }
    }
}