using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// Allows you to perform a one-time computation and broadcast the result to all other robots.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TMarker">Marker type to differentiate between startup components.</typeparam>
    public sealed class StartupComponent<TMessage, TMarker> : IComponent
        where TMessage : class
    {
        private static readonly List<StartupComponent<TMessage, TMarker>> StartupComponents = new();

        private readonly IRobotController _robotController;
        private readonly Func<HashSet<int>, TMessage> _messageFactory;

        // This component should run before anything else.
        public int PreUpdateOrder { get; } = -10000;

        // This component should run before anything else.
        public int PostUpdateOrder { get; } = -10000;

        /// <summary>
        /// Gets the ids of the robots that robot 0 has discovered.
        /// </summary>
        public HashSet<int> DiscoveredRobots { get; } = new HashSet<int>();

        /// <summary>
        /// Gets the message that has been calculated on robot 0.
        /// </summary>
        public TMessage Message { get; private set; } = null!;

        /// <summary>
        /// Create a new instance of <see cref="StartupComponent{TMessage, TMarker}"/>.
        /// </summary>
        /// <param name="robotController">The robot controller.</param>
        /// <param name="messageFactory">The function that is run at startup, which result is broadcast.</param>
        public StartupComponent(IRobotController robotController, Func<HashSet<int>, TMessage> messageFactory)
        {
            _robotController = robotController;
            _messageFactory = messageFactory;
            StartupComponents.Add(this);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            foreach (var component in StartupComponents)
            {
                component.DiscoveredRobots.Add(_robotController.Id);
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            if (_robotController.Id == 0)
            {
                var message = _messageFactory(DiscoveredRobots);
                foreach (var component in StartupComponents)
                {
                    component.Message = message;
                }

                StartupComponents.Clear();
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }
    }
}