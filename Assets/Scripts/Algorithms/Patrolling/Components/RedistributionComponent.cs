using System.Collections.Generic;

using Maes.Map;
using Maes.Robot;
using Maes.Utilities;


namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class RedistributionComponent : IComponent
    {
        // The value is the Id of the Partition and the float is the properbility of the robot to redistribute to that partition.
        private readonly Dictionary<int, float> _redistributionTracker;
        private readonly List<Partition> _partitions;
        private readonly Partition _currentPartition;
        private readonly MonaRobot _monaRobot;
        private readonly CommunicationManager _communicationManager;
        private readonly List<int> _currentPartitionIntersection;

        /// <inheritdoc />
        public int PreUpdateOrder => 1;

        /// <inheritdoc />

        public int PostUpdateOrder => 1;

        public Dictionary<int, bool> RecivedCommunication { get; set; }

        public RedistributionComponent(List<Partition> partitions, Robot2DController controller)
        {
            _partitions = partitions;
            _monaRobot = controller.GetRobot();
            _currentPartition = _partitions[_monaRobot.AssignedPartition];
            _communicationManager = controller.CommunicationManager;
            _redistributionTracker = new Dictionary<int, float>();
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                foreach (var objectMessage in _communicationManager.ReadMessages(_monaRobot))
                {
                    if (objectMessage is RedistributionMessage message)
                    {
                        RecivedCommunication[message.Sender.AssignedPartition] = true;
                    }
                }
                CalculateRedistribution();

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);

            }
        }


        private void CalculateRedistribution()
        {
            // Im not sure if this is the correct way to get the robot position
            var robotPosition = _monaRobot.transform.position.ToVector2Int();
            foreach ((var partitionId, var intersectionZone) in _currentPartition.IntersectionZones)
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
                        if (RecivedCommunication[partitionId])
                        {
                            _redistributionTracker[partitionId] = -_currentPartition.CommunicationRatio[partitionId];
                        }
                        else
                        {
                            _redistributionTracker[partitionId] = +_currentPartition.CommunicationRatio[partitionId];
                        }
                        _currentPartitionIntersection.Remove(partitionId);
                    }
                }
            }
        }

        private sealed class RedistributionMessage
        {
            public MonaRobot Sender { get; set; }
            public string Message { get; set; }
            public int Timestamp { get; }

        }
    }
}