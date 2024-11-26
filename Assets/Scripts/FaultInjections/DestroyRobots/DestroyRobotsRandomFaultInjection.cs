using System;

namespace Maes.FaultInjections.DestroyRobots
{
    /// <summary>
    /// This fault injection destroys robots randomly with a given probability.
    /// </summary>
    public class DestroyRobotsRandomFaultInjection : DestroyRobotBaseFaultInjection
    {
        private readonly int? _maxDestroy;
        private readonly float _probability;
        private readonly int _invokeEvery;

        /// <summary>
        /// Constructor for DestroyRobotsRandomFaultInjection.
        /// </summary>
        /// <param name="seed">Seed for the random number generator.</param>
        /// <param name="probability">Probability of destroying a robot.</param>
        /// <param name="invokeEvery">How often the fault injection should be called.</param>
        /// <param name="maxDestroy">Maximum number of robots to destroy.</param>
        public DestroyRobotsRandomFaultInjection(int seed, float probability, int invokeEvery = 1, int? maxDestroy = null) : base(seed)
        {
            if (invokeEvery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(invokeEvery), "Must be greater than 0");
            }

            if (_probability < 0.0 || _probability > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0");
            }

            _maxDestroy = maxDestroy;
            _probability = probability;
            _invokeEvery = invokeEvery;
        }

        protected override bool ShouldDestroy(int logicTick)
        {
            return _maxDestroy > DestroyedCount && logicTick % _invokeEvery == 0 && _random.NextDouble() < _probability;
        }
    }
}