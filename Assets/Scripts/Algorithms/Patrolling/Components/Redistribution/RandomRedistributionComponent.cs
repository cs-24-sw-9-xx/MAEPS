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

using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

using Random = System.Random;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    public sealed class RandomRedistributionComponent : IComponent
    {
        public delegate float ProbabilityFunction(Partition partition);
        private readonly ProbabilityFunction _probabilityFactor;
        private readonly IRobotController _controller;
        private readonly IPatrollingAlgorithm _algorithm;
        private readonly IReadOnlyList<int> _partitionIds;
        private readonly PatrollingMap _patrollingMap;
        private readonly Random _random;

        public int PreUpdateOrder => -450;
        public int PostUpdateOrder => -450;

        public RandomRedistributionComponent(
            IRobotController controller,
            IReadOnlyList<Vertex> vertices,
            IPatrollingAlgorithm algorithm,
            int seed,
            PatrollingMap patrollingMap,
            ProbabilityFunction probabilityFactor)
        {
            _controller = controller;
            _algorithm = algorithm;
            _patrollingMap = patrollingMap;
            _probabilityFactor = probabilityFactor;
            _partitionIds = vertices.Select(v => v.Partition).Distinct().ToList();
            _random = new Random(seed);
        }

        public RandomRedistributionComponent(
            IRobotController controller,
            IReadOnlyList<Vertex> vertices,
            IPatrollingAlgorithm algorithm,
            int seed,
            PatrollingMap patrollingMap,
            float probabilityFactor)
            : this(controller, vertices, algorithm, seed, patrollingMap, (_) => probabilityFactor)
        {
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                if (_algorithm.HasSeenAllInPartition(_controller.AssignedPartition))
                {
                    SwitchPartition();
                    _algorithm.ResetSeenVerticesForPartition(_controller.AssignedPartition);
                }
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private bool SwitchPartition()
        {
            var prob = _probabilityFactor(
                _patrollingMap.Partitions.Single(p => p.PartitionId == _controller.AssignedPartition));
            if (_partitionIds.Count == 1 ||
                _random.NextDouble() >= prob)
            {
                Debug.Log($"Robot {_controller.Id} stayed");
                return false;
            }

            var availablePartitions = _partitionIds.Where(id => id != _controller.AssignedPartition).ToList();
            var randomPartitionId = availablePartitions[_random.Next(availablePartitions.Count)];
            Debug.Log($"Robot {_controller.Id} switching partition: {_controller.AssignedPartition} -> {randomPartitionId} algo: {_algorithm.AlgorithmName} with probability {prob}");
            _controller.AssignedPartition = randomPartitionId;
            return true;
        }
    }
}