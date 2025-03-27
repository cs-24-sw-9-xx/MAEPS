using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// Allows you to perform a one-time computation and broadcast the result to all other robots.
    /// This requires that the robots spawn together and are all in communication range of robot 0.
    /// Robots not in range will do nothing for the rest of the simulation.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public sealed class StartupComponent<TMessage> : IComponent
        where TMessage : class
    {
        private readonly IRobotController _robotController;
        private readonly Func<TMessage> _messageFactory;

        // This component should run before anything else.
        public int PreUpdateOrder { get; } = -10000;

        // This component should run before anything else.
        public int PostUpdateOrder { get; } = -10000;

        /// <summary>
        /// Gets the ids of the robots that robot 0 has discovered.
        /// </summary>
        public HashSet<int> DiscoveredRobots { get; } = new HashSet<int>();

        // This is set after this component has run.
        // It will not allow any other component or the algorithm logic to run before this variable is set
        /// <summary>
        /// Gets the message that has been calculated on robot 0.
        /// </summary>
        public TMessage Message { get; private set; } = null!;

        /// <summary>
        /// Create a new instance of <see cref="StartupComponent{TMessage}"/>.
        /// </summary>
        /// <param name="robotController">The robot controller.</param>
        /// <param name="messageFactory">The function that is run at startup, which result is broadcast.</param>
        public StartupComponent(IRobotController robotController, Func<TMessage> messageFactory)
        {
            _robotController = robotController;
            _messageFactory = messageFactory;
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            if (_robotController.Id == 0)
            {
                // Wait for the robot id messages to be sent
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

                var robotIdMessages = _robotController.ReceiveBroadcast().Cast<ReportRobotIdMessage>().ToHashSet();
                foreach (var robotIdMessage in robotIdMessages)
                {
                    DiscoveredRobots.Add(robotIdMessage.RobotId);
                }
                // Add ourselves
                DiscoveredRobots.Add(_robotController.Id);

                // Send the startup message
                Message = _messageFactory();
                _robotController.Broadcast(new StartupMessage(Message, DiscoveredRobots));

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
            }
            else
            {
                // Send our robot id to 0
                _robotController.Broadcast(new ReportRobotIdMessage(_robotController.Id));

                // Wait for robot 0 to receive the messages and send the startup message
                yield return ComponentWaitForCondition.WaitForLogicTicks(2, shouldContinue: false);

                var receivedMessages = _robotController.ReceiveBroadcast();
                if (receivedMessages.Count == 0)
                {
                    // We are out of range of robot 0
                    // There is nothing for us to do.
                    // Log this and do nothing.
                    Debug.LogWarningFormat("Robot {0} did not receive the startup message. Disabling...", _robotController.Id);
                    while (true)
                    {
                        yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
                    }
                }

                var receivedMessage = (StartupMessage)receivedMessages.Single();
                Message = receivedMessage.Message;
                foreach (var robotId in receivedMessage.DiscoveredRobots)
                {
                    DiscoveredRobots.Add(robotId);
                }
            }

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private sealed class StartupMessage
        {
            public readonly TMessage Message;
            public readonly HashSet<int> DiscoveredRobots;

            public StartupMessage(TMessage message, HashSet<int> discoveredRobots)
            {
                Message = message;
                DiscoveredRobots = discoveredRobots;
            }
        }

        private sealed class ReportRobotIdMessage
        {
            public readonly int RobotId;

            public ReportRobotIdMessage(int robotId)
            {
                RobotId = robotId;
            }
        }
    }
}