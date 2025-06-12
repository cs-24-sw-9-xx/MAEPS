using System;

namespace Maes.FaultInjections.DestroyRobots
{
    /// <summary>
    /// Destroy robots at specific ticks.
    /// </summary>
    public class DestroyRobotsAtSpecificTickFaultInjection : DestroyRobotBaseFaultInjection
    {
        public readonly int[] DestroyAtTicks;
        private int _index;

        /// <summary>
        /// Constructor for DestroyRobotsAtSpecificTickFaultInjection.
        /// </summary>
        /// <param name="seed">Seed for the random number generator.</param>
        /// <param name="destroyAtTicks">Ticks at which robots should be destroyed.</param>
        public DestroyRobotsAtSpecificTickFaultInjection(int seed, params int[] destroyAtTicks) : base(seed)
        {
            DestroyAtTicks = destroyAtTicks;
            Array.Sort(DestroyAtTicks);
        }

        protected override bool ShouldDestroy(int logicTick)
        {
            if (_index < DestroyAtTicks.Length && logicTick == DestroyAtTicks[_index])
            {
                _index++;
                return true;
            }

            return false;
        }
    }
}