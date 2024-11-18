using Maes.Robot;

namespace Maes.Trackers
{
    public interface ITracker
    {
        void LogicUpdate(MonaRobot[] robots);

        void SetVisualizedRobot(MonaRobot? robot);
    }
}