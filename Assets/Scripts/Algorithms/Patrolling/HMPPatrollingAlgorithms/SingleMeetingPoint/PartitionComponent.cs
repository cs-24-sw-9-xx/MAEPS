using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint.MeetingPoints;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint
{
    public class PartitionComponent : IComponent
    {
        public class PartitionGeneratorResult
        {
            public IReadOnlyDictionary<int, Assignment> AssignmentByRobotId { get; }
            public MeetingPoint MeetingPoint { get; }

            public PartitionGeneratorResult(IReadOnlyDictionary<int, Assignment> assignmentByRobotId, MeetingPoint meetingPoint)
            {
                AssignmentByRobotId = assignmentByRobotId;
                MeetingPoint = meetingPoint;
            }
        }
        public delegate PartitionGeneratorResult PartitionGenerator(HashSet<int> robots);

        public PartitionComponent(IRobotController controller, PartitionGenerator partitionGenerator)
        {
            _robotId = controller.Id;
            _robotController = controller;
            _partitionGenerator = partitionGenerator;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        public MeetingPoint MeetingPoint { get; private set; }

        private readonly HashSet<int> _verticesByIdToPatrol = new();
        public HashSet<int> VerticesByIdToPatrol
        {
            get
            {
                _verticesByIdToPatrol.Clear();
                var partitions = GetPartitionsPatrolledByRobotId(_robotId);
                foreach (var partition in partitions)
                {
                    foreach (var vertexId in _assignments[partition].VertexIds)
                    {
                        _verticesByIdToPatrol.Add(vertexId);
                    }
                }

                return _verticesByIdToPatrol;
            }
        }

        private readonly int _robotId;
        private readonly IRobotController _robotController;
        private readonly PartitionGenerator _partitionGenerator;

        public Assignment[] _assignments = null!;

        private StartupComponent<PartitionGeneratorResult, PartitionComponent> _startupComponent = null!;

        public VirtualStigmergyComponent<int, int, PartitionComponent> _partitionIdToRobotIdVirtualStigmergyComponent =
            null!;

        private PatrollingMap _patrollingMap = null!;


        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _patrollingMap = patrollingMap;
            _startupComponent = new StartupComponent<PartitionGeneratorResult, PartitionComponent>(controller, GeneratePartitions);
            _partitionIdToRobotIdVirtualStigmergyComponent = new(OnPartitionConflict, controller);

            return new IComponent[] { _startupComponent, _partitionIdToRobotIdVirtualStigmergyComponent };
        }

        private PartitionGeneratorResult GeneratePartitions(HashSet<int> robots)
        {
            Debug.Assert(_patrollingMap.Vertices.Count >= robots.Count);
            var partitions = _partitionGenerator(robots);
            Debug.Assert(partitions.AssignmentByRobotId.Count == robots.Count);

            return partitions;
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            var assignmentByRobotId = _startupComponent.Message.AssignmentByRobotId;

            _assignments = new Assignment[assignmentByRobotId.Count];
            MeetingPoint = _startupComponent.Message.MeetingPoint;

            var partitionId = 0;
            foreach (var (robotId, assignment) in assignmentByRobotId)
            {
                // Set up partition robot assignments
                _partitionIdToRobotIdVirtualStigmergyComponent.Put(partitionId, robotId);
                _assignments[partitionId] = assignment;
                partitionId++;
            }

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        public IEnumerable<ComponentWaitForCondition> ExchangeInformation(int meetingPointVertexId)
        {
            _robotController.Broadcast(new MeetingMessage(_robotId));

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().ToList();

            // Let's see if we actually should take over the partition

            var expectedRobotIds = MeetingPoint.PartitionIds.Select(p =>
            {
                var success = _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(p, out var assignedRobot);
                Debug.Assert(success);
                return assignedRobot;
            }).Distinct().ToArray();

            // TODO: Optimize who takes over (remember that the information must be available and correct on all robots attending meeting!)
            var notMissingRobots = meetingMessages.Select(m => m.RobotId).Append(_robotId).OrderBy(id => GetPartitionsPatrolledByRobotId(id).Count()).ThenBy(id => id).ToArray();
            var missingRobots = expectedRobotIds
                .Except(notMissingRobots).OrderByDescending(id => GetPartitionsPatrolledByRobotId(id).Count()).ThenByDescending(id => id).ToArray();

            var overtaking = missingRobots.Zip(notMissingRobots,
                (missingId, notMissingId) => (missingId, notMissingId));

            // If not every expected robot showed up
            if (expectedRobotIds.Count() - 1 != meetingMessages.Count)
            {
                Debug.LogFormat("Vertex {0}, Missing robots: {1}", meetingPointVertexId, string.Join(", ", missingRobots));

                foreach (var (missingId, overtakingId) in overtaking)
                {
                    if (overtakingId == _robotId)
                    {
                        var overtakingPartitions = GetPartitionsPatrolledByRobotId(missingId).ToArray();
                        Debug.LogFormat("Robot {0} is taking over partitions: {1}", _robotId, string.Join(", ", overtakingPartitions));
                        foreach (var overtakingPartition in overtakingPartitions)
                        {
                            _partitionIdToRobotIdVirtualStigmergyComponent.Put(overtakingPartition, _robotId);
                        }
                    }
                }
            }

            /*
            var giveAndTakeCandidates = notMissingRobots.Where(c => !overtaking.Any(tuple => tuple.notMissingId == c));
            var giveCandidates = giveAndTakeCandidates.OrderByDescending(c => GetPartitionsPatrolledByRobotId(c).Count()).ThenBy(c => c).Take(giveAndTakeCandidates.Count() / 2);
            var takeCandidates = giveAndTakeCandidates.OrderBy(c => GetPartitionsPatrolledByRobotId(c).Count())
                .ThenByDescending(c => c).Take(giveAndTakeCandidates.Count() / 2);

            var giveTakePairs = giveCandidates.Zip(takeCandidates, (giveRobot, takeRobot) => (giveRobot, takeRobot))
                .Where(tuple =>
                    tuple.giveRobot != tuple.takeRobot &&
                    // Don't shuffle partitions back and forth
                    GetPartitionsPatrolledByRobotId(tuple.giveRobot).Count() - 1 >
                    GetPartitionsPatrolledByRobotId(tuple.takeRobot).Count());

            // Let somebody give a partition to somebody else
            foreach (var (giveRobot, takeRobot) in giveTakePairs)
            {
                // There might actually be multiple partitions matching this. So lets just get the first.
                var partition = GetPartitionsPatrolledByRobotId(giveRobot).Select(p => _partitions[p])
                    .First(p => p.MeetingPoints.Any(m => m.VertexId == meetingPointVertexId)).PartitionId;

                if (giveRobot == _robotId)
                {
                    Debug.LogFormat("Robot {0} is giving partition {1} away", _robotId, partition);
                    _partitionIdToRobotIdVirtualStigmergyComponent.Put(partition, -1);
                }
                else if (takeRobot == _robotId)
                {
                    Debug.LogFormat("Robot {0} is taking partition {1}", _robotId, partition);
                    Debug.Assert(_overTakingMeetingPoint == -1);
                    _overTakingMeetingPoint = meetingPointVertexId;
                    _overTakingPartitions.Add(partition);
                }
            }
            */
        }

        private IEnumerable<int> GetPartitionsPatrolledByRobotId(int robotId)
        {
            for (var partitionId = 0; partitionId < _assignments.Length; partitionId++)
            {
                var success =
                    _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(partitionId,
                        out var assignedRobotId);
                Debug.Assert(success);

                if (assignedRobotId == robotId)
                {
                    yield return partitionId;
                }
            }
        }

        private static VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo OnPartitionConflict(int key, VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo incomingvalueinfo)
        {
            if (localvalueinfo.RobotId < incomingvalueinfo.RobotId)
            {
                return localvalueinfo;
            }

            return incomingvalueinfo;
        }

        private sealed class MeetingMessage
        {
            public int RobotId { get; }

            public MeetingMessage(int robotId)
            {
                RobotId = robotId;
            }
        }
    }
}