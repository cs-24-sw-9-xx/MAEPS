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

using Maes.Map;
using Maes.Robot;

using UnityEngine;


namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    /// <summary>
    /// Component responsible for redistributing robots to different partitions.
    /// </summary>
    public sealed class PartitionRedistributionComponent : IComponent
    {
        // The value is the Id of the Partition and the float is the probability of the robot to redistribute to that partition.
        private readonly Dictionary<int, float> _redistributionTracker;
        private readonly IRobotController _controller;
        private Partition _currentPartition = null!;
        private readonly HashSet<int> _currentPartitionIntersection;
        private float _trackerUpdateTimestamp = 0f;
        private readonly PatrollingMap _map;
        private readonly Dictionary<int, bool> _receivedCommunication;

        /// <inheritdoc />
        public int PreUpdateOrder => -200;

        /// <inheritdoc />
        public int PostUpdateOrder => -200;

        public PartitionRedistributionComponent(IRobotController controller, PatrollingMap map)
        {
            _controller = controller;
            _redistributionTracker = new Dictionary<int, float>();
            _currentPartitionIntersection = new HashSet<int>();
            _receivedCommunication = new Dictionary<int, bool>();
            _map = map;
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            _currentPartition = _map.Partitions[_controller.AssignedPartition];
            while (true)
            {
                BroadCastMessage();
                UpdateMessagesReceived();
                CalculateRedistribution();
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private void BroadCastMessage()
        {
            _controller.Broadcast(new RedistributionMessage(_controller.AssignedPartition));
            _controller.Broadcast(new PartitionMessage(_trackerUpdateTimestamp, _controller.AssignedPartition, _redistributionTracker));
        }

        private void UpdateMessagesReceived()
        {
            var messages = _controller.ReceiveBroadcast();
            if (messages.Count == 0)
            {
                return;
            }

            foreach (var objectMessage in messages)
            {
                switch (objectMessage)
                {
                    case RedistributionMessage message:
                        _receivedCommunication[message.PartitionId] = true;
                        break;
                    case PartitionMessage partitionMessage when
                        !(partitionMessage.PartitionId != _controller.AssignedPartition ||
                        _trackerUpdateTimestamp < partitionMessage.Timestamp):
                        {
                            foreach (var (partitionId, probability) in partitionMessage.RedistributionTracker)
                            {
                                _redistributionTracker[partitionId] = probability;
                            }

                            _trackerUpdateTimestamp = partitionMessage.Timestamp;
                            break;
                        }
                }
            }
        }

        private void CalculateRedistribution()
        {
            var robotPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition();
            foreach (var (partitionId, intersectionZone) in _currentPartition.IntersectionZones)
            {
                if (intersectionZone.Contains(robotPosition))
                {
                    if (!_currentPartitionIntersection.Contains(partitionId))
                    {
                        _currentPartitionIntersection.Add(partitionId);
                    }
                }
                else
                {
                    if (_currentPartitionIntersection.Contains(partitionId))
                    {
                        if (_receivedCommunication.TryGetValue(partitionId, out var hasCommunication) && hasCommunication)
                        {
                            _redistributionTracker[partitionId] = -_currentPartition.CommunicationRatio[partitionId];
                            _trackerUpdateTimestamp = Time.realtimeSinceStartup;
                            _receivedCommunication[partitionId] = false;
                        }
                        else
                        {
                            _redistributionTracker[partitionId] = +_currentPartition.CommunicationRatio[partitionId];
                            if (SwitchPartition(partitionId))
                            {
                                return;
                            }
                        }
                        _currentPartitionIntersection.Remove(partitionId);
                    }
                }
            }
        }

        private bool SwitchPartition(int partitionId)
        {
            var randomValue = Random.value;
            if (randomValue <= _redistributionTracker[partitionId])
            {
                var partition = _map.Partitions[partitionId];
                _controller.AssignedPartition = partition.PartitionId;
                _currentPartition = partition;
                _redistributionTracker.Clear();
                _currentPartitionIntersection.Clear();
                return true;
            }

            return false;
        }

        private sealed class RedistributionMessage
        {
            public int PartitionId { get; }
            public RedistributionMessage(int partitionId)
            {
                PartitionId = partitionId;
            }
        }

        private sealed class PartitionMessage
        {
            public int PartitionId { get; }
            public float Timestamp { get; }
            public Dictionary<int, float> RedistributionTracker { get; }
            public PartitionMessage(float timestamp, int partitionId, Dictionary<int, float> redistributionTracker)
            {
                Timestamp = timestamp;
                PartitionId = partitionId;
                RedistributionTracker = redistributionTracker;
            }
        }
    }
}