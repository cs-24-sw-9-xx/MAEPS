using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover
{
    public class PartitionComponent : IComponent
    {
        public enum TakeoverStrategy
        {
            ImmediateTakeoverStrategy, // At each meeting, if there is a missing robot, then we take over the other partition immediately
            QuasiRandomStrategy, // At each meeting, if there is a missing robot, then there is a 50% chance to take over the another partition
        }

        public delegate Dictionary<int, PartitionInfo> PartitionGenerator(HashSet<int> robots);

        public PartitionComponent(StrongBox<int> robotId, PartitionGenerator partitionGenerator, TakeoverStrategy takeoverStrategy, System.Random random)
        {
            _robotId = robotId;
            _partitionGenerator = partitionGenerator;
            _takeoverStrategy = takeoverStrategy;
            _random = random;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        private readonly StrongBox<int> _robotId;
        private readonly PartitionGenerator _partitionGenerator;
        private readonly TakeoverStrategy _takeoverStrategy;
        private readonly System.Random _random;
        protected StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent> _startupComponent = null!;
        protected VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent> _virtualStigmergyComponent = null!;

        public PartitionInfo PartitionInfo { get; private set; } = null!;

        private PatrollingMap _patrollingMap = null!;

        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _patrollingMap = patrollingMap;

            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent>(controller, GeneratePartitions);
            _virtualStigmergyComponent =
                new VirtualStigmergyComponent<int, PartitionInfo, PartitionComponent>(OnConflict, controller);

            return new IComponent[] { _startupComponent, _virtualStigmergyComponent };
        }

        private Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robots)
        {
            Debug.Assert(_patrollingMap.Vertices.Count >= robots.Count);
            var partitions = _partitionGenerator(robots);
            Debug.Assert(partitions.Count == robots.Count);

            return partitions;
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
                var success = _virtualStigmergyComponent.TryGet(_robotId.Value, out var partitionInfo);
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
            Debug.Assert(robotsThatShowedUp.Contains(_robotId.Value), "Robot that is taking over partition should be in the list of robots that showed up");
            robotsThatShowedUp.Sort();
            var missingRobotsSorted = missingRobots.ToList();
            missingRobotsSorted.Sort();
            Debug.Log($"Meeting at vertex: {meeting.MeetingPoint.VertexId} at tick: {meeting.MeetingAtTick} with showed up count: {robotsThatShowedUp.Count}; {string.Join(' ', robotsThatShowedUp)} and missing robot count: {missingRobotsSorted.Count}; {string.Join(' ', missingRobotsSorted)} missing robots.");
            switch (_takeoverStrategy)
            {
                case TakeoverStrategy.ImmediateTakeoverStrategy:
                    ImmediateTakeover(robotsThatShowedUp, missingRobotsSorted);
                    break;
                case TakeoverStrategy.QuasiRandomStrategy:
                    if (_random.Next(2) == 0)
                    {
                        ImmediateTakeover(robotsThatShowedUp, missingRobotsSorted);
                    }
                    break;
                default:
                    Debug.LogError($"Unknown takeover strategy: {_takeoverStrategy}");
                    break;
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private void ImmediateTakeover(List<int> robotsThatShowedUp, List<int> missingRobotsSorted)
        {
            var myIndex = robotsThatShowedUp.IndexOf(_robotId.Value);
            if (myIndex < missingRobotsSorted.Count)
            {
                // Pick the id of one of the missing robots to take over
                var robotIdToTakeOver = missingRobotsSorted[myIndex];
                if (robotsThatShowedUp.Count < missingRobotsSorted.Count && myIndex == robotsThatShowedUp.Count - 1)
                {
                    // Pick the id of one of the missing robots at random
                    robotIdToTakeOver = missingRobotsSorted[myIndex + _random.Next(missingRobotsSorted.Count - myIndex)];
                }
                TakeOverOtherRobotPartition(robotIdToTakeOver);
            }
        }

        private void TakeOverOtherRobotPartition(int robotId)
        {
            Debug.Log($"Robot {_robotId.Value} taking over partition for robot {robotId}");
            _robotId.Value = robotId;
            _virtualStigmergyComponent.TryGet(_robotId.Value, out var partitionInfo);
            Debug.Assert(partitionInfo != null, "PartitionInfo should not be null");
            PartitionInfo = partitionInfo!;
        }
    }
}