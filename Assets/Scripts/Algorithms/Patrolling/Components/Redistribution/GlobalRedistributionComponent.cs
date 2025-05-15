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

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    /// <summary>
    /// Global component responsible for managing robot redistribution across partitions and tracking heartbeats.
    /// </summary>
    public sealed class GlobalRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly PatrollingAlgorithm _algorithm;
        private readonly HeartBeatComponent _heartbeatComponent;
        private readonly int _timeOut;
        public int PreUpdateOrder => -100;
        public int PostUpdateOrder => -100;

        public GlobalRedistributionComponent(IRobotController controller, int timeOut, PatrollingAlgorithm algorithm, HeartBeatComponent heartbeatComponent)
        {
            _controller = controller;
            _algorithm = algorithm;
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
                RedistributeRobotIfLastSurvivor();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private void RedistributeRobotIfLastSurvivor()
        {
            // Only check after some ticks to avoid early moves
            if (_algorithm.LogicTicks > 1000)
            {
                // If we are the only robot left
                if (_heartbeatComponent.RobotHeartbeats.Count == 0)
                {
                    // Check if we have finished our current partition
                    if (_algorithm.HasSeenAllInPartition(_controller.AssignedPartition))
                    {
                        // Find the partition with the highest idleness
                        var partitionWithHighestIdleness = GetPartitionWithHighestIdleness();
                        if (partitionWithHighestIdleness != _controller.AssignedPartition)
                        {
                            Debug.Log($"Robot {_controller.Id} is the last survivor and will move to partition {partitionWithHighestIdleness} (highest idleness)");
                            _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
                            _controller.AssignedPartition = partitionWithHighestIdleness;
                        }
                    }
                }
            }
        }

        private int GetPartitionWithHighestIdleness()
        {
            return _algorithm.PartitionIdleness
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault().Key;
        }

        /// <summary>
        /// Checks for robots that have not sent a heartbeat within the threshold and removes them.
        /// </summary>
        private void CheckHeartbeats()
        {
            var outdatedHeartbeats = _heartbeatComponent.RobotHeartbeats.Values
                .Where(heartbeat => heartbeat.LogicTick < _algorithm.LogicTicks - _timeOut).ToList();

            foreach (var heartbeat in outdatedHeartbeats)
            {
                _heartbeatComponent.RemoveRobot(heartbeat.RobotId);
                RedistributeRobot(heartbeat.PartitionId);
            }
        }

        /// <summary>
        /// Redistributes a robot to the partition that lost a robot.
        /// </summary>
        private void RedistributeRobot(int newPartition)
        {
            var bestPartitionToMoveRobotFrom = GetPartitionWithMostHeartbeats();
            if (bestPartitionToMoveRobotFrom == _controller.AssignedPartition && IsLowestIdInPartition())
            {
                Debug.Log($"Robot {_controller.Id} is the lowest ID in partition {_controller.AssignedPartition} and will move to partition {newPartition}");
                _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
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

        private bool IsLowestIdInPartition()
        {
            var robotsInPartition = _heartbeatComponent.RobotHeartbeats
                .Values
                .Where(heartbeat => heartbeat.PartitionId == _controller.AssignedPartition);

            if (!robotsInPartition.Any())
            {
                return false; // No robots in the partition
            }

            var lowestRobotId = robotsInPartition.Min(heartbeat => heartbeat.RobotId);
            return _controller.Id < lowestRobotId;
        }
    }
}