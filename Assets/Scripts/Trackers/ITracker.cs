using System.Collections.Generic;

using JetBrains.Annotations;

using Maes.Robot;

namespace Maes.Trackers
{
    public interface ITracker
    {
        void LogicUpdate(IReadOnlyList<MonaRobot> robots);

        void SetVisualizedRobot([CanBeNull] MonaRobot robot);
    }
}