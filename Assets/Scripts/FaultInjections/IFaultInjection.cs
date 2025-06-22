using System.Collections.Generic;

using Maes.Robot;

namespace Maes.FaultInjections
{
    public delegate bool DestroyRobotDelegate(MonaRobot robot);
    public interface IFaultInjection
    {
        void LogicUpdate(List<MonaRobot> robots, int logicTicks);
        void SetDestroyFunc(DestroyRobotDelegate destroyFunc);
    }
}