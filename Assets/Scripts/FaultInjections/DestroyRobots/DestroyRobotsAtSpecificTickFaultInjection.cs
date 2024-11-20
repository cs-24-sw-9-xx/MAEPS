using System;

namespace Maes.FaultInjections.DestroyRobots
{
    /// <summary>
    /// Destroy robots at specific ticks.
    /// </summary>
    public class DestroyRobotsAtSpecificTickFaultInjection : DestroyRobotBaseFaultInjection
    {
        private readonly int[] _destroyAtTicks;
        private readonly int _currentNumberOfDestroys;
        private int _index;

        /// <summary>
        /// Constructor for DestroyRobotsAtSpecificTickFaultInjection.
        /// </summary>
        /// <param name="seed">Seed for the random number generator.</param>
        /// <param name="destroyAtTicks">Array of ticks at whihch robots should be destroyed.</param>
        public DestroyRobotsAtSpecificTickFaultInjection(int seed, int[] destroyAtTicks) : base(seed)
        {
            _destroyAtTicks = destroyAtTicks;
            Array.Sort(_destroyAtTicks);
            _currentNumberOfDestroys = destroyAtTicks.Length;
        }

        protected override bool ShouldDestroy(int logicTick)
        {
            if (_index < _currentNumberOfDestroys && logicTick == _destroyAtTicks[_index])
            {
                _index++;
                return true;
            }

            return false;
        }
    }
}