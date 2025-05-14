// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
//
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen
// Mads Beyer Mogensen
using System.Collections.Generic;

using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    public class HeartBeatComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly IPatrollingAlgorithm _algorithm;
        private readonly Dictionary<int, HeartbeatMessage> _robotHeartbeats = new Dictionary<int, HeartbeatMessage>();
        public IReadOnlyDictionary<int, HeartbeatMessage> RobotHeartbeats => _robotHeartbeats;
        public int PreUpdateOrder => -300;
        public int PostUpdateOrder => -300;

        public HeartBeatComponent(IRobotController controller, IPatrollingAlgorithm algorithm)
        {
            _controller = controller;
            _algorithm = algorithm;
        }

        /// <summary>
        /// PreUpdateLogic to handle heartbeats.
        /// </summary>
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                _controller.Broadcast(new HeartbeatMessage(_controller.Id, _algorithm.LogicTicks, _controller.AssignedPartition));
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