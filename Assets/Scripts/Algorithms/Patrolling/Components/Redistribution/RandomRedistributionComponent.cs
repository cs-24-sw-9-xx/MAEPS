using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class RandomRedistributionComponent : IComponent
    {
        private readonly IRobotController _controller;
        private readonly IReadOnlyList<int> _partitionIds;
        private readonly int _delay;
        private readonly IReadOnlyDictionary<(int, int), float> _probabilityForPartitionSwitch;

        public int PreUpdateOrder => -450;

        public int PostUpdateOrder => -450;

        public RandomRedistributionComponent(IRobotController controller, IReadOnlyList<Vertex> vertices, int delay = 1)
        {
            _controller = controller;
            _partitionIds = vertices.Select(v => v.Partition).Distinct().ToList();
            _delay = delay;
        }

        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                SwitchPartition();
                yield return ComponentWaitForCondition.WaitForLogicTicks(_delay, shouldContinue: true);
            }
        }

        private void SwitchPartition()
        {
            var random = new Random();
            var amountOfPartitions = _partitionIds.Count;
            if (amountOfPartitions < 2)
            {
                return;
            }

            var randomPartitionId = _partitionIds[random.Next(0, amountOfPartitions - 1)];
            if (_controller.AssignedPartition == randomPartitionId)
            {
                return;
            }
            _controller.AssignedPartition = randomPartitionId;
        }
    }
}