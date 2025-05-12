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
using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components.Redistribution
{
    public sealed class DistanceRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly IReadOnlyList<Vertex> _vertices;
        private readonly int _probabilityFactor;
        private readonly int _delay;
        private readonly IReadOnlyDictionary<(int, int), float> _probabilityForPartitionSwitch;
        private readonly Random _random;

        public int PreUpdateOrder => -251;

        public int PostUpdateOrder => -251;

        public DistanceRedistributionComponent(IRobotController controller, IReadOnlyList<Vertex> vertices, int probabilityFactor, int seed = 123, int delay = 1)
        {
            _controller = controller;
            _vertices = vertices;
            _probabilityFactor = probabilityFactor;
            _delay = delay;
            _probabilityForPartitionSwitch = CalculateClosestDistanceToPartitions(vertices);
            _random = new Random(seed);
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                foreach (var ((fromPartition, toPartition), probability) in _probabilityForPartitionSwitch)
                {
                    if (_controller.AssignedPartition == fromPartition)
                    {
                        var randomValue = (float)_random.NextDouble();

                        if (randomValue < probability)
                        {
                            _controller.AssignedPartition = toPartition;
                            break;
                        }
                    }
                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(_delay, shouldContinue: true);
            }
        }

        private Dictionary<(int, int), float> CalculateClosestDistanceToPartitions(IReadOnlyCollection<Vertex> vertices)
        {
            var probabilityForPartitionSwitch = new Dictionary<(int, int), float>();
            var partitions = _vertices
                .GroupBy(v => v.Partition)
                .ToDictionary(g => g.Key, g => new HashSet<Vertex>(g));

            foreach (var partitionA in partitions.Keys)
            {
                foreach (var partitionB in partitions.Keys)
                {
                    if (partitionA == partitionB)
                    {
                        continue;
                    }

                    var minDistance = CalculateMinimumDistance(partitions, partitionA, partitionB);
                    if (minDistance > 0)
                    {
                        var switchProbability = 1 / minDistance * _probabilityFactor;
                        probabilityForPartitionSwitch[(partitionA, partitionB)] = switchProbability;
                        continue;
                    }
                    probabilityForPartitionSwitch[(partitionA, partitionB)] = 0;
                }
            }
            return probabilityForPartitionSwitch;
        }

        private float CalculateMinimumDistance(Dictionary<int, HashSet<Vertex>> partitions, int partitionA, int partitionB)
        {
            var minDistance = float.MaxValue;
            foreach (var vertexA in partitions[partitionA])
            {
                foreach (var vertexB in partitions[partitionB])
                {
                    var distance = _controller.TravelEstimator.EstimateDistance(vertexA.Position, vertexB.Position) ?? float.MaxValue;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return minDistance;
        }
    }
}