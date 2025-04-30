using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public class PartitionComponent : IComponent
    {
        public PartitionComponent(IRobotController controller, StartupComponent<Dictionary<int, HMPPartitionInfo>> startupComponent, VirtualStigmergyComponent<int, HMPPartitionInfo> virtualStigmergyComponent)
        {
            _robotId = controller.Id;
            _startupComponent = startupComponent;
            _virtualStigmergyComponent = virtualStigmergyComponent;
        }
        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private readonly int _robotId;
        private readonly StartupComponent<Dictionary<int, HMPPartitionInfo>> _startupComponent;
        private readonly VirtualStigmergyComponent<int, HMPPartitionInfo> _virtualStigmergyComponent;

        public PartitionInfo? PartitionInfo { get; private set; }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            _virtualStigmergyComponent.Put(_robotId, _startupComponent.Message[_robotId]);
            // Wait for one logic tick before continuing to ensure the message is sent and received.
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            while (true)
            {
                var success = _virtualStigmergyComponent.TryGet(_robotId, out var partitionInfo);
                Debug.Assert(success);
                PartitionInfo = partitionInfo;
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }
    }
}