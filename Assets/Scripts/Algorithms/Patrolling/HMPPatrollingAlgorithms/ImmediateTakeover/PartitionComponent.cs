using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover
{
    public class PartitionComponent : IComponent
    {
        public delegate Dictionary<int, PartitionInfo> PartitionGenerator(HashSet<int> robots);

        public PartitionComponent(RobotIdClass robotId, PartitionGenerator partitionGenerator)
        {
            _robotId = robotId;
            _partitionGenerator = partitionGenerator;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private RobotIdClass _robotId;
        private readonly PartitionGenerator _partitionGenerator;

        protected StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent> _startupComponent = null!;
        protected VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent> _virtualStigmergyComponent = null!;

        public PartitionInfo PartitionInfo { get; private set; } = null!;

        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent>(controller, robots => _partitionGenerator(robots));
            _virtualStigmergyComponent =
                new VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent>(OnConflict, controller);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent };
        }

        private static VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent>.ValueInfo OnConflict(int key, VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent>.ValueInfo incomingvalueinfo)
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
            foreach (var robotId in _startupComponent.DiscoveredRobots)
            {
                _virtualStigmergyComponent.Put(robotId, _startupComponent.Message[robotId]);
            }

            while (true)
            {
                var success = _virtualStigmergyComponent.TryGet(_robotId.RobotId, out var partitionInfo);
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
            // Pick the id of one of the missing robots at random
            var missingRobotId = missingRobots.ToList()[Random.Range(0, missingRobots.Count)];
            _robotId.RobotId = missingRobotId;
            _virtualStigmergyComponent.TryGet(_robotId.RobotId, out var partitionInfo);
            Debug.Assert(partitionInfo != null, "PartitionInfo should not be null");
            PartitionInfo = partitionInfo!;

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }
    }
}