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
        public static int ReceivedNewMeetingtimeForOtherThanVisiting = 0;
        public static Dictionary<int, int> ReceivedNewMeetingtimeForOtherThanVisitingByRobotId = new();
        public static List<int> ReceivedNewMeetingtimeAtTicks = new();

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

        public PartitionComponent(IRobotController controller, PartitionGenerator partitionGenerator, bool forceTheThing, Func<int> getLogicTicks)
        {
            _robotId = controller.Id;
            _robotController = controller;
            _partitionGenerator = partitionGenerator;
            _forceTheThing = forceTheThing;
            _getLogicTicks = getLogicTicks;
        }

        public int PreUpdateOrder => -900;
        public int PostUpdateOrder => -900;

        public IReadOnlyDictionary<int, MeetingPoint> MeetingPointsByVertexId { get; private set; } = null!;

        private readonly HashSet<int> _verticesByIdToPatrol = new();
        public HashSet<int> VerticesByIdToPatrol
        {
            get
            {
                _verticesByIdToPatrol.Clear();
                var partitions = GetPartitionsPatrolledByRobotId(_robotId);
                foreach (var partition in partitions)
                {
                    foreach (var vertexId in _partitions[partition].VertexIds)
                    {
                        _verticesByIdToPatrol.Add(vertexId);
                    }
                }

                return _verticesByIdToPatrol;
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
        private readonly bool _forceTheThing;
        private readonly Func<int> _getLogicTicks;

        public PartitionInfo[] _partitions = null!;

        private StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent> _startupComponent = null!;

        public VirtualStigmergyComponent<int, int, PartitionComponent> _partitionIdToRobotIdVirtualStigmergyComponent =
            null!;

        public VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>
            _meetingPointVertexIdToMeetingTimesStigmergyComponent = null!;

        private PatrollingMap _patrollingMap = null!;

        public void OnNewUpdateDelegate(int key, VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent>.ValueInfo valueInfo)
        {
            if (_meetingPointVertexId != null)
            {
                if (_meetingPointVertexId == key)
                {
                    // We are at the meeting point, so we don't care about this update.
                    return;
                }

                var expectedRobotIds = MeetingPointsByVertexId[key].PartitionIds.Select(p =>
                {
                    var success = _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(p, out var assignedRobot);
                    Debug.Assert(success);
                    return assignedRobot;
                }).ToHashSet();

                if (expectedRobotIds.Contains(_robotId))
                {
                    ReceivedNewMeetingtimeForOtherThanVisiting++;

                    if (!ReceivedNewMeetingtimeForOtherThanVisitingByRobotId.TryAdd(valueInfo.RobotId, 1))
                    {
                        ReceivedNewMeetingtimeForOtherThanVisitingByRobotId[valueInfo.RobotId]++;
                    }

                    ReceivedNewMeetingtimeAtTicks.Add(_getLogicTicks());
                }
            }
        }

        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _patrollingMap = patrollingMap;
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent>(controller, GeneratePartitions);

            _partitionIdToRobotIdVirtualStigmergyComponent = new(OnPartitionConflict, controller);
            _meetingPointVertexIdToMeetingTimesStigmergyComponent = new(OnMeetingConflict, controller, OnNewUpdateDelegate);

            return new IComponent[] { _startupComponent, _partitionIdToRobotIdVirtualStigmergyComponent, _meetingPointVertexIdToMeetingTimesStigmergyComponent };
        }

        private Dictionary<int, PartitionInfo> GeneratePartitions(HashSet<int> robots)
        {
            Debug.Assert(_patrollingMap.Vertices.Count >= robots.Count);
            var partitions = _partitionGenerator(robots);
            Debug.Assert(partitions.Count == robots.Count);

            return partitions;
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

        private int? _meetingPointVertexId = null;

        public IEnumerable<ComponentWaitForCondition> ExchangeInformation(int meetingPointVertexId)
        {
            _meetingPointVertexId = meetingPointVertexId;
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
                            if (_forceTheThing)
                            {
                                newMeetingTimes = meetingTimes2;
                            }

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

            // TODO: Optimize who takes over (remember that the information must be available and correct on all robots attending meeting!)
            var notMissingRobots = meetingMessages.Select(m => m.RobotId).Append(_robotId).OrderBy(id => id);
            var missingRobots = expectedRobotIds
                .Except(notMissingRobots).OrderBy(id => id);

            var overtakingCandidates = meetingMessages.Where(m => !m.OverrideCurrentNextMeetingAtTick)
                .Select(m => m.RobotId).Where(id => id != _robotId);
            if (_overTakingMeetingPoint == -1)
            {
                overtakingCandidates = overtakingCandidates.Append(_robotId);
            }

            overtakingCandidates = overtakingCandidates.OrderBy(id => id);

            var overtaking = missingRobots.Zip(overtakingCandidates,
                (missingId, notMissingId) => (missingId, notMissingId));

            // If not every expected robot showed up
            if (expectedRobotIds.Count() - 1 != meetingMessages.Count)
            {
                Debug.LogFormat("Vertex {0}, Missing robots: {1}", meetingPointVertexId, string.Join(", ", missingRobots));

                if (missingRobots.Count() > overtakingCandidates.Count())
                {
                    Debug.Log("Not enough overtaking candidates to take over the missing robots. Too bad!");
                }

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

            var giveAndTakeCandidates = overtakingCandidates.Where(c => !overtaking.Any(tuple => tuple.notMissingId == c));
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

            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                out var meetingTimes);
            Debug.Assert(success);

            if (overrideCurrentNextMeetingAtTick)
            {
                // HACK: Adding 10 ticks to immediately renegotiate.
                var newMeetingTimes = new MeetingTimes(nextMeetingTime, nextMeetingTime + 10);
                if (_forceTheThing)
                {
                    var latestCurrent = MeetingPoints.Select(m => m.meetingTimes.CurrentNextMeetingAtTick)
                        .Append(_getLogicTicks()).Max();
                    var soonestNext = MeetingPoints.Select(m => m.meetingTimes.NextNextMeetingAtTick).Where(m => m > latestCurrent).OrderBy(m => m).FirstOrDefault();
                    if (soonestNext != default)
                    {
                        var newCurrentNext = latestCurrent + (soonestNext - latestCurrent) / 2;
                        newMeetingTimes = new MeetingTimes(newCurrentNext, nextMeetingTime);
                    }
                }

                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, newMeetingTimes);
            }
            else
            {
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime));
            }
            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();

            _partitionIdToRobotIdVirtualStigmergyComponent.SendAll();

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
            _meetingPointVertexId = null;
        }

        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, HashSet<int> missingRobots)
        {
            // TODO: Implement the logic for when some other robots are not at the meeting point
            Debug.Log("Some robots are not at the meeting point");

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private int NextPossibleMeetingTime(IEnumerable<int> partitions)
        {
            Debug.Assert(partitions.Any());
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

            Debug.Assert(interval + highestNextNextMeetingAtTicks > 0);
            return interval + highestNextNextMeetingAtTicks;
        }

        private int NextPossibleMeetingTime()
        {
            return NextPossibleMeetingTime(GetPartitionsPatrolledByRobotId(_robotId));
        }

        public IEnumerable<int> GetPartitionsPatrolledByRobotId(int robotId)
        {
            var hasPartitions = false;
            for (var partitionId = 0; partitionId < _partitions.Length; partitionId++)
            {
                var success =
                    _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(partitionId,
                        out var assignedRobotId);
                Debug.Assert(success);

                if (assignedRobotId == robotId)
                {
                    hasPartitions = true;
                    yield return partitionId;
                }
            }

            if (hasPartitions || robotId != _robotId)
            {
                yield break;
            }

            // Add the robot to a partition that is patrolled by a robot with multiple partitions
            for (var partitionId = 0; partitionId < _partitions.Length; partitionId++)
            {
                if (partitionId == _robotId)
                {
                    continue;
                }

                var partitions = GetPartitionsPatrolledByRobotId(partitionId);
                if (partitions.Count() > 1)
                {
                    var partition = partitions.First();
                    _partitionIdToRobotIdVirtualStigmergyComponent.Put(partition, _robotId);
                    yield return partition;
                    yield break;
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