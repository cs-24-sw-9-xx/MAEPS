using System.Collections.Generic;
using System.Linq;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    /// <summary>
    /// This component is responsible for initializing the partitions in the patrolling map.
    /// </summary>>
    public sealed class PartitionInitalizationComponent : IComponent
    {
        private readonly IRobotController _robotController;

        public int PreUpdateOrder => -10000;

        public int PostUpdateOrder => -10000;

        public List<Partition> Partitions { get; } = new();
        
        private PatrollingMap _map { get; }

        public PartitionInitalizationComponent(PatrollingMap map, IRobotController robotController)
        {
            _map = map;
            _robotController = robotController;
        }

        private void InitPartitions()
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
                InitPartitions();
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
    }
}