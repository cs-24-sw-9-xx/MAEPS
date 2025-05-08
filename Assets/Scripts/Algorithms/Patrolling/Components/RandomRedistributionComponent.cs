using System;
using System.Collections.Generic;

using Maes.Map;
using Maes.Robot;
using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class RandomRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly IReadOnlyList<Vertex> _vertices;
        private readonly int _probabilityFactor;
        private readonly int _delay;
        private readonly Dictionary<(int, int), float> _probabilityForPartitionSwitch = new();

        public int PreUpdateOrder => -250;

        public int PostUpdateOrder => -250;

        public RandomRedistributionComponent(IRobotController controller, IReadOnlyList<Vertex> vertices, int probabilityFactor, int delay = 1)
        {
            _controller = controller;
            _vertices = vertices;
            _probabilityFactor = probabilityFactor;
            _delay = delay;
            CalculateClosestDistanceToPartitions();
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

        private void CalculateClosestDistanceToPartitions()
        {
            var partitions = new Dictionary<int, HashSet<Vertex>>();
            foreach (var vertex in _vertices)
            {
                if (!partitions.ContainsKey(vertex.Partition))
                {
                    partitions[vertex.Partition] = new HashSet<Vertex>();
                }
                partitions[vertex.Partition].Add(vertex);
            }

            foreach (var partitionA in partitions.Keys)
            {
                foreach (var partitionB in partitions.Keys)
                {
                    if (partitionA == partitionB)
                    {
                        continue;
                    }

                    var minDistance = float.MaxValue;
                    foreach (var vertexA in partitions[partitionA])
                    {
                        foreach (var vertexB in partitions[partitionB])
                        {
                            var distance = Geometry.EuclideanDistance(vertexA, vertexB);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                            }
                        }
                    }
                    var switchProbability = 1 / minDistance * _probabilityFactor;
                    _probabilityForPartitionSwitch[(partitionA, partitionB)] = switchProbability;
                }
            }
        }
    }
}