using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class PartitionComponent : IComponent
    {
        public PartitionComponent(IRobotController controller, IPartitionGenerator<HMPPartitionInfo> partitionGenerator)
        {
            _robotId = controller.Id;
            _partitionGenerator = partitionGenerator;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private readonly int _robotId;
        private readonly IPartitionGenerator<HMPPartitionInfo> _partitionGenerator;

        private StartupComponent<IReadOnlyDictionary<int, HMPPartitionInfo>, PartitionComponent> _startupComponent = null!;
        private VirtualStigmergyComponent<int, HMPPartitionInfo, PartitionComponent> _virtualStigmergyComponent = null!;

        public HMPPartitionInfo? PartitionInfo { get; private set; }

        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, HMPPartitionInfo>, PartitionComponent>(controller, _partitionGenerator.GeneratePartitions);
            _virtualStigmergyComponent =
                new VirtualStigmergyComponent<int, HMPPartitionInfo, PartitionComponent>(OnConflict, controller);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent };
        }

        private static VirtualStigmergyComponent<int, HMPPartitionInfo, PartitionComponent>.ValueInfo OnConflict(int key, VirtualStigmergyComponent<int, HMPPartitionInfo, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, HMPPartitionInfo, PartitionComponent>.ValueInfo incomingvalueinfo)
        {
            if (localvalueinfo.RobotId < incomingvalueinfo.RobotId)
            {
                return localvalueinfo;
            }

            return incomingvalueinfo;
        }

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
                PartitionInfo = partitionInfo!;
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        public IEnumerable<ComponentWaitForCondition> ExchangeInformation()
        {
            foreach (var robotId in _startupComponent.DiscoveredRobots)
            {
                _virtualStigmergyComponent.TryGet(robotId, out var _);
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(2, shouldContinue: false);
        }

        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobots)
        {
            // TODO: Implement the logic for when some other robots are not at the meeting point
            Debug.Log("Some robots are not at the meeting point");

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }
    }
}