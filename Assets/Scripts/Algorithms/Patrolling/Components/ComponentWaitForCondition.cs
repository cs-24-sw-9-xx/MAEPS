using Maes.Robot.Tasks;

namespace Maes.Algorithms.Patrolling.Components
{
    public readonly struct ComponentWaitForCondition
    {
        public WaitForCondition Condition { get; }

        public bool ShouldContinue { get; }

        // Do not use this!
        public ComponentWaitForCondition(WaitForCondition waitForCondition, bool shouldContinue)
        {
            Condition = waitForCondition;
            ShouldContinue = shouldContinue;
        }

        public static ComponentWaitForCondition WaitForLogicTicks(int logicTicks, bool shouldContinue)
        {
            return new ComponentWaitForCondition(WaitForCondition.WaitForLogicTicks(logicTicks), shouldContinue);
        }

        public static ComponentWaitForCondition WaitForRobotStatus(RobotStatus status, bool shouldContinue)
        {
            return new ComponentWaitForCondition(WaitForCondition.WaitForRobotStatus(status), shouldContinue);
        }
    }
}