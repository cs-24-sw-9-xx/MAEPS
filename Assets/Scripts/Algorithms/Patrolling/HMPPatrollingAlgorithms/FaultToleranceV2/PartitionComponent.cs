using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2.MeetingPoints;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2
{
    public class PartitionComponent : IComponent
    {
        public readonly struct MeetingTimes
        {
            public readonly int CurrentNextMeetingAtTick;
            public readonly int NextNextMeetingAtTick;

            public MeetingTimes(int currentNextMeetingAtTick, int nextNextMeetingAtTick)
            {
                CurrentNextMeetingAtTick = currentNextMeetingAtTick;
                NextNextMeetingAtTick = nextNextMeetingAtTick;
            }

            public override string ToString()
            {
                return $"({CurrentNextMeetingAtTick}, {NextNextMeetingAtTick})";
            }
        }

        public delegate Dictionary<int, PartitionInfo> PartitionGenerator(HashSet<int> robots);

        public PartitionComponent(IRobotController controller, PartitionGenerator partitionGenerator)
        {
            _robotId = controller.Id;
            _robotController = controller;
            _partitionGenerator = partitionGenerator;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        public IReadOnlyDictionary<int, MeetingPoint> MeetingPointsByVertexId { get; private set; } = null!;

        public IEnumerable<int> VerticesByIdToPatrol
        {
            get
            {
                for (var i = 0; i < _partitions.Length; i++)
                {
                    var success =
                        _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(i, out var assignedRobot);
                    Debug.Assert(success);

                    if (assignedRobot == _robotId)
                    {
                        foreach (var vertexId in _partitions[i].VertexIds)
                        {
                            yield return vertexId;
                        }
                    }
                }
            }
        }

        public IEnumerable<(int vertexId, MeetingTimes meetingTimes)> MeetingPoints
        {
            get
            {
                for (var i = 0; i < _partitions.Length; i++)
                {
                    var success =
                        _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(i, out var assignedRobot);
                    Debug.Assert(success);

                    if (assignedRobot == _robotId)
                    {
                        foreach (var meetingPoint in _partitions[i].MeetingPoints)
                        {
                            var vertexId = meetingPoint.VertexId;
                            var meetingTimesSuccess =
                                _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(vertexId,
                                    out var meetingTimes);
                            Debug.Assert(meetingTimesSuccess);

                            yield return (vertexId, meetingTimes);
                        }
                    }
                }
            }
        }

        private readonly int _robotId;
        private readonly IRobotController _robotController;
        private readonly PartitionGenerator _partitionGenerator;

        private PartitionInfo[] _partitions = null!;

        private StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent> _startupComponent = null!;

        private VirtualStigmergyComponent<int, int, PartitionComponent> _partitionIdToRobotIdVirtualStigmergyComponent =
            null!;

        private VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>
            _meetingPointVertexIdToMeetingTimesStigmergyComponent = null!;



        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent>(controller, robots => _partitionGenerator(robots));

            _partitionIdToRobotIdVirtualStigmergyComponent = new(OnPartitionConflict, controller);
            _meetingPointVertexIdToMeetingTimesStigmergyComponent = new(OnMeetingConflict, controller);

            return new IComponent[] { _startupComponent, _partitionIdToRobotIdVirtualStigmergyComponent, _meetingPointVertexIdToMeetingTimesStigmergyComponent };
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            var meetingPointsByVertexId = new Dictionary<int, MeetingPoint>();
            var partitions = _startupComponent.Message;

            _partitions = new PartitionInfo[partitions.Count];

            var partitionId = 0;
            foreach (var (robotId, partition) in partitions)
            {
                // Set up partition robot assignments
                _partitionIdToRobotIdVirtualStigmergyComponent.Put(partitionId, robotId);
                _partitions[partitionId] = partition;


                // Set up meeting points
                foreach (var meetingPoint in partition.MeetingPoints)
                {
                    meetingPointsByVertexId[meetingPoint.VertexId] = meetingPoint;
                    _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPoint.VertexId, new MeetingTimes(meetingPoint.InitialCurrentNextMeetingAtTick, meetingPoint.InitialNextNextMeetingAtTick));
                }

                partitionId++;
            }

            MeetingPointsByVertexId = meetingPointsByVertexId;

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        public IEnumerable<ComponentWaitForCondition> ExchangeInformation(int meetingPointVertexId)
        {
            var nextPossibleMeetingTime = NextPossibleMeetingTime();
            _robotController.Broadcast(new MeetingMessage(_robotId, nextPossibleMeetingTime));
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>();
            var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max(), nextPossibleMeetingTime);

            Debug.Assert(MeetingPointsByVertexId[meetingPointVertexId].RobotIds.Count - 1 == meetingMessages.Count());

            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                out var meetingTimes);
            Debug.Assert(success);

            Debug.LogFormat("Decided that the next meeting time for vertex {0} should be {1}", meetingPointVertexId, nextMeetingTime);

            _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime));
            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobots)
        {
            // TODO: Implement the logic for when some other robots are not at the meeting point
            Debug.Log("Some robots are not at the meeting point");

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private int NextPossibleMeetingTime()
        {
            var interval = 0;
            var highestNextNextMeetingAtTicks = 0;
            for (var i = 0; i < _partitions.Length; i++)
            {
                var success =
                    _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(i, out var assignedRobotId);
                Debug.Assert(success);

                if (assignedRobotId == _robotId)
                {
                    interval += _partitions[i].Diameter;
                    var partitionsHighestNextNextMeetingAtTicks = _partitions[i].MeetingPoints.Select(m =>
                    {
                        var success =
                            _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(m.VertexId,
                                out var meetingTimes);
                        Debug.Assert(success);
                        return meetingTimes.NextNextMeetingAtTick;
                    }).Max();
                    highestNextNextMeetingAtTicks = Math.Max(highestNextNextMeetingAtTicks,
                        partitionsHighestNextNextMeetingAtTicks);
                }
            }

            return interval + highestNextNextMeetingAtTicks;
        }

        private static VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo OnPartitionConflict(int key, VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, int, PartitionComponent>.ValueInfo incomingvalueinfo)
        {
            if (localvalueinfo.RobotId < incomingvalueinfo.RobotId)
            {
                return localvalueinfo;
            }

            return incomingvalueinfo;
        }

        private static VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>.ValueInfo OnMeetingConflict(int key, VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>.ValueInfo incomingvalueinfo)
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

            public int ProposedNextNextMeetingAtTick { get; }

            public MeetingMessage(int robotId, int proposedNextNextMeetingAtTick)
            {
                RobotId = robotId;
                ProposedNextNextMeetingAtTick = proposedNextNextMeetingAtTick;
            }
        }
    }
}