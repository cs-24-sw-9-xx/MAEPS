using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Trackers
{
    public interface ITracker
    {
        void UIUpdate();

        void LogicUpdate(List<MonaRobot> robots);

        void SetVisualizedRobot(MonaRobot? robot);

        int CurrentTick { get; }
    }
}