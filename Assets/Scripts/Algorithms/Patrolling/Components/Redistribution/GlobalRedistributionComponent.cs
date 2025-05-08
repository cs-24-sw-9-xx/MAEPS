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
using System.Linq;

using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    /// <summary>
    /// Global component responsible for managing robot redistribution across partitions and tracking heartbeats.
    /// </summary>
    public sealed class GlobalRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly int _logicTick;
        private readonly HeartBeatComponent _heartbeatComponent;
        private readonly int _timeOut;
        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;

        public GlobalRedistributionComponent(IRobotController controller, int timeOut, int logicTick, HeartBeatComponent heartbeatComponent)
        {
            _controller = controller;
            _logicTick = logicTick;
            _heartbeatComponent = heartbeatComponent;
            _timeOut = timeOut + controller.Id;
        }

        /// <summary>
        /// PreUpdateLogic to handle heartbeats and redistribution.
        /// </summary>
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                CheckHeartbeats();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }


        /// <summary>
        /// Checks for robots that have not sent a heartbeat within the threshold and removes them.
        /// </summary>
        private void CheckHeartbeats()
        {
            var partitionNeedRobots = _heartbeatComponent.RobotHeartbeats
                .Where(heartbeat => heartbeat.Value.LogicTick < _logicTick - _timeOut)
                .Select(heartbeat => heartbeat.Value.PartitionId);

            foreach (var partitionId in partitionNeedRobots)
            {
                _heartbeatComponent.RemoveRobot(partitionId);
                RedistributeRobot(partitionId);
            }
        }

        /// <summary>
        /// Redistributes a robot to the partition that lost a robot.
        /// </summary>
        private void RedistributeRobot(int newPartition)
        {
            var bestPartitionToMoveRobotFrom = GetPartitionWithMostHeartbeats();
            if (bestPartitionToMoveRobotFrom == _controller.AssignedPartition && IsLowestIdInPartition(_controller.Id))
            {
                _controller.AssignedPartition = newPartition;
            }
        }

        private int? GetPartitionWithMostHeartbeats()
        {
            return _heartbeatComponent.RobotHeartbeats
                .Values
                .GroupBy(heartbeat => heartbeat.PartitionId)
                .OrderByDescending(group => group.Count())
                .FirstOrDefault()?.Key;
        }

        private bool IsLowestIdInPartition(int partitionId)
        {
            var robotsInPartition = _heartbeatComponent.RobotHeartbeats
                .Values
                .Where(heartbeat => heartbeat.PartitionId == partitionId);

            if (!robotsInPartition.Any())
            {
                return false; // No robots in the partition
            }

            var lowestRobotId = robotsInPartition.Min(heartbeat => heartbeat.RobotId);
            return _controller.Id == lowestRobotId;
        }
    }
}