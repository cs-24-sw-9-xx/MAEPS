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
                RedistributeRobotIfLessRobotsThanPartitions();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private void RedistributeRobotIfLessRobotsThanPartitions()
        {
            // Only check after some ticks to avoid early moves
            if (_algorithm.LogicTicks <= 1000)
            {
                return;
            }

            // If there are enough heartbeats greater or equal the amount of partitions, no redistribution is needed
            if (_heartbeatComponent.RobotHeartbeats.Count + 1 >= _algorithm.PartitionIdleness.Count())
            {
                return;
            }

            // Check if we have finished our current partition
            if (!_algorithm.HasSeenAllInPartition(_controller.AssignedPartition))
            {
                return;
            }

            // Find the partition with the highest idleness
            var partitionWithHighestIdleness = GetPartitionWithHighestIdleness();
            if (partitionWithHighestIdleness == _controller.AssignedPartition)
            {
                return;
            }

            Debug.Log($"Robot {_controller.Id} is the last survivor and will move to partition {partitionWithHighestIdleness} (highest idleness)");
            _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
            _controller.AssignedPartition = partitionWithHighestIdleness;
        }

        private int GetPartitionWithHighestIdleness()
        {
            if (_algorithm.PartitionIdleness.Any())
            {
                return _algorithm.PartitionIdleness
                    .OrderByDescending(kvp => kvp.Value)
                    .First().Key;
            }

            Debug.LogWarning("PartitionIdleness is empty. No partition to redistribute to.");
            return _controller.AssignedPartition; // Return current partition if no other is available
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
            if (bestPartitionToMoveRobotFrom != _controller.AssignedPartition || !IsLowestIdInPartition())
            {
                return;
            }

            Debug.Log($"Robot {_controller.Id} is the lowest ID in partition {_controller.AssignedPartition} and will move to partition {newPartition} algo: {_algorithm.AlgorithmName}");
            _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
            _controller.AssignedPartition = newPartition;
        }

        private int? GetPartitionWithMostHeartbeats()
        {
            var partitionCounts = new Dictionary<int, int>();

            // Count heartbeats for each partition
            foreach (var heartbeat in _heartbeatComponent.RobotHeartbeats.Values)
            {
                if (partitionCounts.ContainsKey(heartbeat.PartitionId))
                {
                    partitionCounts[heartbeat.PartitionId]++;
                }
                else
                {
                    partitionCounts[heartbeat.PartitionId] = 1;
                }
            }

            // Add self to own partition (since we don't register our own heartbeat)
            if (partitionCounts.ContainsKey(_controller.AssignedPartition))
            {
                partitionCounts[_controller.AssignedPartition]++;
            }
            else
            {
                partitionCounts[_controller.AssignedPartition] = 1;
            }

            if (partitionCounts.Count == 0)
            {
                return null;
            }

            // Find the maximum count
            var maxCount = partitionCounts.Values.Max();

            // Select all partitions with the maximum count
            var partitionsWithMax = partitionCounts
                .Where(kvp => kvp.Value == maxCount)
                .Select(kvp => kvp.Key)
                .ToList();

            // Return the lowest partition ID among those with the max count (deterministic)
            return partitionsWithMax.Min();
        }

        private bool IsLowestIdInPartition()
        {
            var robotsInPartition = _heartbeatComponent.RobotHeartbeats
            .Values
            .Where(heartbeat => heartbeat.PartitionId == _controller.AssignedPartition)
            .Select(heartbeat => heartbeat.RobotId)
            .ToList();

            if (!robotsInPartition.Any())
            {
                return false; // No robots in the partition
            }

            var lowestRobotId = robotsInPartition.Min();
            return _controller.Id < lowestRobotId;
        }
    }
}