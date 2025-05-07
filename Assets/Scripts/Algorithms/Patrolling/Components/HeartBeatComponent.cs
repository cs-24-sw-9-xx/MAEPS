using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    public class HeartBeatComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly int _logicTick;
        private readonly Dictionary<int, HeartbeatMessage> _robotHeartbeats = new Dictionary<int, HeartbeatMessage>();
        public IReadOnlyDictionary<int, HeartbeatMessage> RobotHeartbeats => _robotHeartbeats;
        public int PreUpdateOrder => -300;
        public int PostUpdateOrder => -300;

        public HeartBeatComponent(IRobotController controller, int logicTick)
        {
            _controller = controller;
            _logicTick = logicTick;
        }

        /// <summary>
        /// PreUpdateLogic to handle heartbeats.
        /// </summary>
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                _controller.Broadcast(new HeartbeatMessage(_controller.Id, _logicTick, _controller.AssignedPartition));
                ProcessHeartbeatMessages();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        /// <summary>
        /// Removes a robot from the heartbeat tracking system.
        /// </summary>
        /// <param name="robotId"></param>
        public void RemoveRobot(int robotId)
        {
            if (_robotHeartbeats.ContainsKey(robotId))
            {
                _robotHeartbeats.Remove(robotId);
            }
        }

        /// <summary>
        /// Processes heartbeat messages received via broadcast.
        /// </summary>
        private void ProcessHeartbeatMessages()
        {
            var messages = _controller.ReceiveBroadcast();
            foreach (var message in messages)
            {
                if (message is HeartbeatMessage heartbeatMessage)
                {
                    _robotHeartbeats[heartbeatMessage.RobotId] = heartbeatMessage;
                }
            }
        }
    }

    /// <summary>
    /// Message class for robot heartbeats.
    /// </summary>
    public sealed class HeartbeatMessage
    {
        public int RobotId { get; }
        public int LogicTick { get; }
        public int PartitionId { get; }

        public HeartbeatMessage(int robotId, int logicTick, int partitionId)
        {
            RobotId = robotId;
            LogicTick = logicTick;
            PartitionId = partitionId;
        }
    }
}