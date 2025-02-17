using Maes.Robot.Task;

namespace Maes.Algorithms
{
    public readonly struct WaitForCondition
    {
        public readonly ConditionType Type;

        // LogicTicks
        public readonly int LogicTicks;

        // ControllerState
        public readonly RobotStatus RobotStatus;

        private WaitForCondition(ConditionType type, int logicTicks, RobotStatus robotStatus)
        {
            Type = type;
            LogicTicks = logicTicks;
            RobotStatus = robotStatus;
        }

        public static WaitForCondition WaitForLogicTicks(int logicTicks)
        {
            return new WaitForCondition(ConditionType.LogicTicks, logicTicks, RobotStatus.Moving);
        }

        public static WaitForCondition WaitForRobotStatus(RobotStatus status)
        {
            return new WaitForCondition(ConditionType.RobotStatus, 0, status);
        }

        public static WaitForCondition ContinueUpdateLogic()
        {
            return new WaitForCondition(ConditionType.ContinueUpdateLogic, 0, RobotStatus.Moving);
        }


        public enum ConditionType
        {
            LogicTicks,
            RobotStatus,
            ContinueUpdateLogic,
        }
    }
}