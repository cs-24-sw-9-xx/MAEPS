using Maes.Robot;

namespace Maes.Trackers
{
    public interface ITracker
    {
        void UIUpdate();

        void LogicUpdate(MonaRobot[] robots);

        void SetVisualizedRobot(MonaRobot? robot);
    }
}