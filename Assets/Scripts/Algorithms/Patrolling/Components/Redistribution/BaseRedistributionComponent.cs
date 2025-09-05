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

using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    /// <summary>
    /// Base class for redistribution components. It handles the communication between robots and the redistribution logic.
    /// The derived classes should implement the logic for updating the redistribution tracker on success and failure.
    /// </summary>
    public abstract class BaseRedistributionComponent : IComponent
    {
        // The value is the Id of the Partition and the float is the probability of the robot to redistribute to that partition.
        protected readonly Dictionary<int, float> _redistributionTracker;
        private readonly IRobotController _controller;
        protected Partition _currentPartition = null!;
        private readonly HashSet<int> _currentPartitionIntersection;
        private float _trackerUpdateTimestamp = 0f;
        private readonly PatrollingMap _map;
        private readonly IPatrollingAlgorithm _algorithm;
        private readonly Dictionary<int, bool> _receivedCommunication;
        private readonly System.Random _random;
        private int _lastPartition = 0;
        private readonly Dictionary<int, bool> _hasLeftPartitionIntersection = new();

        /// <summary>
        /// Method to be implemented by derived classes to update the redistribution tracker on failure.
        /// </summary>
        /// <returns></returns>
        protected abstract void UpdateTrackerOnFailure(int partitionId);

        /// <summary>
        /// Method to be implemented by derived classes to update the redistribution tracker on success.
        /// </summary>
        /// <returns></returns>
        protected abstract void UpdateTrackerOnSuccess(int partitionId);

        /// <inheritdoc />
        public int PreUpdateOrder => -50;

        /// <inheritdoc />
        public int PostUpdateOrder => -50;

        public BaseRedistributionComponent(IRobotController controller, PatrollingMap map, IPatrollingAlgorithm algorithm, int seed)
        {
            _controller = controller;
            _redistributionTracker = new Dictionary<int, float>();
            _currentPartitionIntersection = new HashSet<int>();
            _receivedCommunication = new Dictionary<int, bool>();
            _map = map;
            _algorithm = algorithm;
            _random = new System.Random(seed);
            SetHasLeftIntersectionFlagToFalse();
        }

        private void SetHasLeftIntersectionFlagToFalse()
        {
            foreach (var partition in _map.Partitions)
            {
                _hasLeftPartitionIntersection[partition.PartitionId] = false;
            }
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
                    _currentPartitionIntersection.Add(partitionId);
                    if (_hasLeftPartitionIntersection[partitionId]
                        && _algorithm.HasSeenAllInPartition(_controller.AssignedPartition)
                        && _trackerUpdateTimestamp <= _algorithm.LogicTicks - 500)
                    {
                        UpdateRedistributionTracker(partitionId);
                    }
                }
                else
                {
                    if (_currentPartitionIntersection.Contains(partitionId))
                    {
                        _hasLeftPartitionIntersection[partitionId] = true;
                        UpdateRedistributionTracker(partitionId);
                        _currentPartitionIntersection.Remove(partitionId);
                        return;
                    }
                    if (_algorithm.HasSeenAllInPartition(_controller.AssignedPartition) && _trackerUpdateTimestamp <= _algorithm.LogicTicks - 500)
                    {
                        UpdateRedistributionTracker(partitionId);
                    }
                }
            }

            if (_algorithm.HasSeenAllInPartition(_controller.AssignedPartition))
            {
                switch (_currentPartition.IntersectionZones.Keys.Count())
                {
                    case 0:
                        return;
                    case 1:
                        SwitchPartition(_currentPartition.IntersectionZones.Keys.First());
                        return;
                    default:
                        SwitchPartition(_currentPartition.IntersectionZones.Keys.Contains(_lastPartition)
                            ? _currentPartition.IntersectionZones.Keys.Where(x => x != _lastPartition).OrderBy(x => _random.Next()).First()
                            : _currentPartition.IntersectionZones.Keys.OrderBy(x => _random.Next()).First());
                        break;
                }
            }
        }

        private void UpdateRedistributionTracker(int partitionId)
        {
            if (_receivedCommunication.TryGetValue(partitionId, out var hasCommunication) && hasCommunication)
            {
                UpdateTrackerOnSuccess(partitionId);
                _trackerUpdateTimestamp = _algorithm.LogicTicks;
                _receivedCommunication[partitionId] = false;
            }
            else
            {
                UpdateTrackerOnFailure(partitionId);
            }
        }

        private void SwitchPartition(int partitionId)
        {
            var randomValue = (float)_random.NextDouble();
            if (randomValue <= _redistributionTracker[partitionId])
            {
                var partition = _map.Partitions[partitionId];
                _lastPartition = partitionId;
                Debug.Log($"Robot {_controller.Id} is switching from {_controller.AssignedPartition} to partition {partition.PartitionId} algo: {_algorithm.AlgorithmName}");
                _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
                _controller.AssignedPartition = partition.PartitionId;
                _currentPartition = partition;
                _redistributionTracker.Clear();
                _currentPartitionIntersection.Clear();
                SetHasLeftIntersectionFlagToFalse();
            }
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