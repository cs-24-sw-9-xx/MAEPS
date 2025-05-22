using System;
using System.Collections.Generic;

using Maes.Robot;

namespace Maes.FaultInjections.DestroyRobots
{
    /// <summary>
    /// Base class for destroying robots.
    /// </summary>
    public abstract class DestroyRobotBaseFaultInjection : IFaultInjection
    {
        protected readonly Random _random;
        protected int DestroyedCount { get; private set; }
        private DestroyRobotDelegate _destroyFunc = null!;

        protected DestroyRobotBaseFaultInjection(int seed)
        {
            _random = new Random(seed);
        }

        public void LogicUpdate(List<MonaRobot> robots, int logicTick)
        {
            if (robots.Count > 0 && ShouldDestroy(logicTick))
            {
                DestroyOneRobot(robots);
            }
        }

        public void SetDestroyFunc(DestroyRobotDelegate destroyFunc)
        {
            _destroyFunc = destroyFunc;
        }

        /// <summary>
        /// Whether a robot should be destroyed at <paramref name="logicTick"/>.
        /// </summary>
        protected abstract bool ShouldDestroy(int logicTick);

        /// <summary>
        /// Destroy a robot randomly for the list.
        /// </summary>
        private void DestroyOneRobot(List<MonaRobot> robots)
        {
            var robot = robots[_random.Next(0, robots.Count)];
            if (_destroyFunc(robot))
            {
                DestroyedCount++;
            }
        }
    }
}