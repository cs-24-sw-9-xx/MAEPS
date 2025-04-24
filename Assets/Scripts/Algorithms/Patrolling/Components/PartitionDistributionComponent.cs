using System.Collections.Generic;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class PartitionDistributionComponent : IComponent
    {
        private readonly IRobotController _robotController;

        public int PreUpdateOrder => -10000;

        public int PostUpdateOrder => -10000;

        public static List<Partition> Partitions { get; private set; } = new List<Partition>();
        private PatrollingMap _map { get; }

        public PartitionDistributionComponent(PatrollingMap map, IRobotController robotController)
        {
            _map = map;
            _robotController = robotController;
        }

        private void DistributePartitions()
        {
            foreach (var (partitionId, vertices) in _map.VerticesByPartition)
            {
                Debug.Log($"Partition {partitionId} has {vertices.Length} vertices.");

                var communicationZones = vertices.ToDictionary(v => v.Position, v => _robotController.CalculateCommunicationZone(v.Position));
                var partition = new Partition(
                    partitionId,
                    vertices,
                    communicationZones);


                Partitions.Add(partition);
            }

            foreach (var partition in Partitions)
            {
                foreach (var otherPartition in Partitions)
                {
                    if (partition.PartitionId != otherPartition.PartitionId)
                    {
                        partition.AddNeighborPartition(otherPartition);
                    }
                }
            }
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            if (_robotController.Id == 0)
            {
                DistributePartitions();
            }
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        public IEnumerable<ComponentWaitForCondition> PostUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        public void DebugInfo(StringBuilder stringBuilder)
        {
            // Intentionally left blank.
            // Don't force components to implement this.
        }
    }
}