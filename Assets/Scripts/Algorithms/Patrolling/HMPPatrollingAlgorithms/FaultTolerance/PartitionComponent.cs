using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance.MeetingPoints;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance
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

        public HashSet<int> VerticesByIdToPatrol
        {
            get
            {
                var verticesByIdToPatrol = new HashSet<int>();
                var partitions = GetPartitionsPatrolledByRobotId(_robotId);
                foreach (var partition in partitions)
                {
                    foreach (var vertexId in _partitions[partition].VertexIds)
                    {
                        verticesByIdToPatrol.Add(vertexId);
                    }
                }

                return verticesByIdToPatrol;
            }
        }

        public IEnumerable<(int vertexId, MeetingTimes meetingTimes)> MeetingPoints
        {
            get
            {
                var partitions = GetPartitionsPatrolledByRobotId(_robotId);
                foreach (var partition in partitions)
                {
                    foreach (var meetingPoint in _partitions[partition].MeetingPoints)
                    {
                        // The meeting point is only in our partitions.
                        if (meetingPoint.PartitionIds.Count == partitions.Count() && meetingPoint.PartitionIds.All(id => partitions.Contains(id)))
                        {
                            continue;
                        }

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

        private int _overTakingMeetingPoint = -1;
        private readonly HashSet<int> _overTakingPartitions = new();

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
            _partitionIdToRobotIdVirtualStigmergyComponent.SendAll();
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var nextPossibleMeetingTime = 0;
            var overrideCurrentNextMeetingAtTick = false;

            if (_overTakingMeetingPoint != -1 && _overTakingMeetingPoint != meetingPointVertexId)
            {
                // Tell them we are going to take longer
                Debug.Log("Telling neighbor we are gonna take longer");
                nextPossibleMeetingTime =
                    NextPossibleMeetingTime(GetPartitionsPatrolledByRobotId(_robotId).Concat(_overTakingPartitions));
                overrideCurrentNextMeetingAtTick = true;
                _robotController.Broadcast(new MeetingMessage(_robotId, meetingPointVertexId, nextPossibleMeetingTime, true));
            }
            else
            {
                nextPossibleMeetingTime = NextPossibleMeetingTime();
                overrideCurrentNextMeetingAtTick = false;
                _robotController.Broadcast(new MeetingMessage(_robotId, meetingPointVertexId, nextPossibleMeetingTime));
            }

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().Where(m => m.MeetingPointVertexId == meetingPointVertexId).ToList();
            var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Append(int.MinValue).Max(), nextPossibleMeetingTime);

            overrideCurrentNextMeetingAtTick |= meetingMessages.Any(m => m.OverrideCurrentNextMeetingAtTick);

            // Lets see if we actually should take over the partition
            if (_overTakingMeetingPoint == meetingPointVertexId)
            {
                foreach (var overTakingPartition in _overTakingPartitions)
                {
                    var success1 = _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(overTakingPartition, out var assignedRobot);
                    Debug.Assert(success1);

                    if (!meetingMessages.Select(m => m.RobotId).Contains(assignedRobot))
                    {
                        Debug.LogFormat("Robot {0} is actually taking over partition {1} now!", _robotId, overTakingPartition);
                        _partitionIdToRobotIdVirtualStigmergyComponent.Put(overTakingPartition, _robotId);
                        foreach (var meetingPoint in _partitions[overTakingPartition].MeetingPoints)
                        {
                            var success2 =
                                _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(
                                    meetingPoint.VertexId, out var meetingTimes2);
                            Debug.Assert(success2);
                            // HACK: Adding 10 ticks to immediately renegotiate.
                            var newMeetingTimes = new MeetingTimes(meetingTimes2.NextNextMeetingAtTick, meetingTimes2.NextNextMeetingAtTick + 10);
                            _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPoint.VertexId, newMeetingTimes);
                        }
                    }
                    else
                    {
                        Debug.LogFormat("Robot {0} not taking over partition {1} as robot {2} already took over!", _robotId, overTakingPartition, assignedRobot);
                    }
                }

                _overTakingMeetingPoint = -1;
                _overTakingPartitions.Clear();
            }

            var expectedRobotIds = MeetingPointsByVertexId[meetingPointVertexId].PartitionIds.Select(p =>
            {
                var success = _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(p, out var assignedRobot);
                Debug.Assert(success);
                return assignedRobot;
            }).Distinct();


            // If not every expected robot showed up
            if (expectedRobotIds.Count() - 1 != meetingMessages.Count)
            {
                // TODO: Optimize who takes over (remember that the information must be available and correct on all robots attending meeting!)
                var notMissingRobots = meetingMessages.Select(m => m.RobotId).Append(_robotId).OrderBy(id => id);
                var missingRobots = expectedRobotIds
                    .Where(id => notMissingRobots.All(notMissingId => notMissingId != id)).OrderBy(id => id);

                var overtakingCandidates = meetingMessages.Where(m => !m.OverrideCurrentNextMeetingAtTick)
                    .Select(m => m.RobotId);
                if (_overTakingMeetingPoint == -1)
                {
                    overtakingCandidates = overtakingCandidates.Append(_robotId);
                }

                overtakingCandidates = overtakingCandidates.OrderBy(id => id);

                if (missingRobots.Count() > overtakingCandidates.Count())
                {
                    Debug.Log("Not enough overtaking candidates to take over the missing robots. Too bad!");
                }

                var overtaking = missingRobots.Zip(overtakingCandidates,
                    (missingId, notMissingId) => (missingId, notMissingId));

                foreach (var (missingId, overtakingId) in overtaking)
                {
                    if (overtakingId == _robotId)
                    {
                        var overtakingPartitions = GetPartitionsPatrolledByRobotId(missingId);
                        Debug.LogFormat("Robot {0} in next cycle is trying to take over partitions: {1}", _robotId, string.Join(", ", overtakingPartitions));
                        foreach (var overtakingPartition in overtakingPartitions)
                        {
                            _overTakingPartitions.Add(overtakingPartition);
                            _overTakingMeetingPoint = meetingPointVertexId;
                        }
                    }
                }
            }

            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                out var meetingTimes);
            Debug.Assert(success);

            Debug.LogFormat("Decided that the next meeting time for vertex {0} should be {1}", meetingPointVertexId, nextMeetingTime);

            if (overrideCurrentNextMeetingAtTick)
            {
                // HACK: Adding 10 ticks to immediately renegotiate.
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(nextMeetingTime, nextMeetingTime + 10));
            }
            else
            {
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime));
            }
            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();

            _partitionIdToRobotIdVirtualStigmergyComponent.SendAll();

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobots)
        {
            // TODO: Implement the logic for when some other robots are not at the meeting point
            Debug.Log("Some robots are not at the meeting point");

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private int NextPossibleMeetingTime(IEnumerable<int> partitions)
        {
            var interval = 0;
            var highestNextNextMeetingAtTicks = 0;
            foreach (var partition in partitions)
            {
                interval += _partitions[partition].Diameter;
                var partitionsHighestNextNextMeetingAtTicks = _partitions[partition].MeetingPoints.Select(m =>
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

            return interval + highestNextNextMeetingAtTicks;
        }

        private int NextPossibleMeetingTime()
        {
            var interval = 0;
            var highestNextNextMeetingAtTicks = 0;

            var assignedPartitions = GetPartitionsPatrolledByRobotId(_robotId);
            foreach (var partition in assignedPartitions)
            {
                interval += _partitions[partition].Diameter;
                var partitionsHighestNextNextMeetingAtTicks = _partitions[partition].MeetingPoints.Select(m =>
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

            return interval + highestNextNextMeetingAtTicks;
        }

        private IEnumerable<int> GetPartitionsPatrolledByRobotId(int robotId)
        {
            for (var partitionId = 0; partitionId < _partitions.Length; partitionId++)
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

            public int MeetingPointVertexId { get; }

            public int ProposedNextNextMeetingAtTick { get; }

            public bool OverrideCurrentNextMeetingAtTick { get; }

            public MeetingMessage(int robotId, int meetingPointVertexId, int proposedNextNextMeetingAtTick, bool overrideCurrentNextMeetingAtTick = false)
            {
                RobotId = robotId;
                MeetingPointVertexId = meetingPointVertexId;
                ProposedNextNextMeetingAtTick = proposedNextNextMeetingAtTick;
                OverrideCurrentNextMeetingAtTick = overrideCurrentNextMeetingAtTick;
            }
        }
    }
}