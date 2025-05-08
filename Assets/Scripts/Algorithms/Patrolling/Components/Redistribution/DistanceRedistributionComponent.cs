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

        public int PreUpdateOrder => -251;

        public int PostUpdateOrder => -251;

        public DistanceRedistributionComponent(IRobotController controller, IReadOnlyList<Vertex> vertices, int probabilityFactor, int delay = 1)
        {
            _controller = controller;
            _vertices = vertices;
            _probabilityFactor = probabilityFactor;
            _delay = delay;
            _probabilityForPartitionSwitch = CalculateClosestDistanceToPartitions(vertices);
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            var random = new Random();

            while (true)
            {
                foreach (var ((fromPartition, toPartition), probability) in _probabilityForPartitionSwitch)
                {
                    if (_controller.AssignedPartition == fromPartition)
                    {
                        var randomValue = (float)random.NextDouble();

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
                    var switchProbability = 1 / minDistance * _probabilityFactor;
                    probabilityForPartitionSwitch[(partitionA, partitionB)] = switchProbability;
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