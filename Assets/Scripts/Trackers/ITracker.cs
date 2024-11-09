using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Trackers
{
    public interface ITracker
    {
        void LogicUpdate(IReadOnlyList<MonaRobot> robots);

        void SetVisualizedRobot(MonaRobot? robot);
    }
}