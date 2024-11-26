using System.Collections.Generic;

using Maes.Robot;

namespace Maes.FaultInjections
{
    public interface IFaultInjection
    {
        void LogicUpdate(List<MonaRobot> robots, int logicTicks);
    }
}