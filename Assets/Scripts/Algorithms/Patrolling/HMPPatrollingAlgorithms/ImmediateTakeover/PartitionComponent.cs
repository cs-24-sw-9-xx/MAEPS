using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Assets.Scripts.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover;
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

        private readonly RobotIdClass _robotId;
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
            var robotsThatShowedUp = meeting.MeetingPoint.RobotIds.Except(missingRobots).ToList();
            Debug.Assert(robotsThatShowedUp.Contains(_robotId.RobotId), "Robot that is taking over partition should be in the list of robots that showed up");
            robotsThatShowedUp.Sort();
            var missingRobotsSorted = missingRobots.ToList();
            missingRobotsSorted.Sort();
            var myIndex = robotsThatShowedUp.IndexOf(_robotId.RobotId);
            Debug.Log($"Meeting at vertex: {meeting.MeetingPoint.VertexId} at tick: {meeting.MeetingAtTick} with showed up count: {robotsThatShowedUp.Count}; {string.Join(' ', robotsThatShowedUp)} and missing robot count: {missingRobotsSorted.Count}; {string.Join(' ', missingRobotsSorted)} missing robots.");
            if (myIndex < missingRobotsSorted.Count)
            {
                // Pick the id of one of the missing robots to take over
                var robotIdToTakeOver = missingRobotsSorted[myIndex];
                if (robotsThatShowedUp.Count < missingRobotsSorted.Count && myIndex == robotsThatShowedUp.Count - 1)
                {
                    // Pick the id of one of the missing robots at random
                    robotIdToTakeOver = missingRobotsSorted[Random.Range(myIndex, missingRobotsSorted.Count)];
                }
                TakeOverOtherRobotPartition(robotIdToTakeOver);
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private void TakeOverOtherRobotPartition(int robotId)
        {
            Debug.Log($"Robot {_robotId.RobotId} taking over partition for robot {robotId}");
            _robotId.RobotId = robotId;
            _virtualStigmergyComponent.TryGet(_robotId.RobotId, out var partitionInfo);
            Debug.Assert(partitionInfo != null, "PartitionInfo should not be null");
            PartitionInfo = partitionInfo!;
        }
    }
}