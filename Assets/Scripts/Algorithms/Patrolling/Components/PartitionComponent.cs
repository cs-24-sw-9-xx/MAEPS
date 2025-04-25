using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    public class PartitionComponent : IComponent
    {
        public PartitionComponent(IRobotController controller, StartupComponent<Dictionary<int, PartitionInfo>> startupComponent, VirtualStigmergyComponent<int, PartitionInfo> virtualStigmergyComponent)
        {
            _robotId = controller.Id;
            _startupComponent = startupComponent;
            _virtualStigmergyComponent = virtualStigmergyComponent;
        }
        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private readonly int _robotId;
        private readonly StartupComponent<Dictionary<int, PartitionInfo>> _startupComponent;
        private readonly VirtualStigmergyComponent<int, PartitionInfo> _virtualStigmergyComponent;

        public PartitionInfo? PartitionInfo { get; private set; }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            _virtualStigmergyComponent.Put(_robotId, _startupComponent.Message[_robotId]);
            // Wait for one logic tick before continuing to ensure the message is sent and received.
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            while (true)
            {
                PartitionInfo = _virtualStigmergyComponent.Get(_robotId);
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }
    }
}