using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;


namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultToleranceV2
{
    public class PartitionComponent : IComponent
    {
        public readonly struct MeetingTimes
        {
            public readonly IReadOnlyCollection<int> RobotIds;
            public readonly int CurrentNextMeetingAtTick;
            public readonly int NextNextMeetingAtTick;
            public readonly bool MightMissCurrentNextMeetingAtTick;

            public MeetingTimes(int currentNextMeetingAtTick, int nextNextMeetingAtTick, IReadOnlyCollection<int> robotIds, bool mightMissCurrentNextMeetingAtTick = false)
            {
                CurrentNextMeetingAtTick = currentNextMeetingAtTick;
                NextNextMeetingAtTick = nextNextMeetingAtTick;
                RobotIds = robotIds;
                MightMissCurrentNextMeetingAtTick = mightMissCurrentNextMeetingAtTick;
            }

            public override string ToString()
            {
                return $"({CurrentNextMeetingAtTick}, {NextNextMeetingAtTick}, ({string.Join(", ", RobotIds)})), {MightMissCurrentNextMeetingAtTick})";
            }
        }

        public readonly struct MissingRobot
        {
            public readonly int RobotId;
            public readonly int AtVertexId;
            public readonly int MissingAtTick;

            public MissingRobot(int robotId, int vertexId, int missingAtTick)
            {
                RobotId = robotId;
                AtVertexId = vertexId;
                MissingAtTick = missingAtTick;
            }

            public override string ToString()
            {
                return $"MissingRobotId: {RobotId}, AtVertexId: {AtVertexId}, MissingAtTick: {MissingAtTick})";
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

        public IEnumerable<int> VerticesByIdToPatrol
        {
            get
            {
                var visitedVertices = new HashSet<int>();
                for (var i = 0; i < _partitions.Length; i++)
                {
                    var success =
                        _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(i, out var assignedRobot);
                    Debug.Assert(success);

                    if (assignedRobot == _robotId)
                    {
                        foreach (var vertexId in _partitions[i].VertexIds)
                        {
                            if (!visitedVertices.Add(vertexId))
                            {
                                continue;
                            }

                            yield return vertexId;
                        }
                    }
                }
            }
        }

        private int[]? _skipPartitionIds;
        public IEnumerable<(int vertexId, MeetingTimes meetingTimes)> MeetingPoints
        {
            get
            {
                for (var i = 0; i < _partitions.Length; i++)
                {
                    if (_skipPartitionIds?.Contains(i) ?? false)
                    {
                        continue;
                    }

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

        private VirtualStigmergyComponent<int, MissingRobot, PartitionComponent> _missingMeetingByVertexIdStigmergyComponent =
            null!;


        public IComponent[] CreateComponents(IRobotController controller, PatrollingMap patrollingMap)
        {
            _startupComponent = new StartupComponent<IReadOnlyDictionary<int, PartitionInfo>, PartitionComponent>(controller, robots => _partitionGenerator(robots));

            _partitionIdToRobotIdVirtualStigmergyComponent = new(OnPartitionConflict, controller);
            _meetingPointVertexIdToMeetingTimesStigmergyComponent = new(OnMeetingConflict, controller);
            _missingMeetingByVertexIdStigmergyComponent = new(OnMissingRobotConflict, controller);

            return new IComponent[] { _startupComponent, _partitionIdToRobotIdVirtualStigmergyComponent, _meetingPointVertexIdToMeetingTimesStigmergyComponent, _missingMeetingByVertexIdStigmergyComponent };
        }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
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
                    _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPoint.VertexId, new MeetingTimes(meetingPoint.InitialCurrentNextMeetingAtTick, meetingPoint.InitialNextNextMeetingAtTick, meetingPoint.RobotIds));
                }

                partitionId++;
            }

            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="readyRobotIds">All robots that are on the meeting point including itself</param>
        /// <returns></returns>
        public IEnumerable<ComponentWaitForCondition> HandleMeeting(MeetingComponent.Meeting meeting, HashSet<int> readyRobotIds)
        {
            var meetingPointVertexId = meeting.Vertex.Id;

            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();
            _partitionIdToRobotIdVirtualStigmergyComponent.SendAll();
            _missingMeetingByVertexIdStigmergyComponent.SendAll();

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            if (readyRobotIds.SetEquals(meeting.RobotIds))
            {
                foreach (var waitForCondition in DecideNextMeetingTime(meetingPointVertexId, readyRobotIds))
                {
                    yield return waitForCondition;
                }
            }
            else
            {
                var unknownRobotIds = readyRobotIds.Except(meeting.RobotIds).ToHashSet();
                var missingRobotIds = meeting.RobotIds.Except(readyRobotIds).ToHashSet();
                Debug.Assert(missingRobotIds.Count == 1, "Only one fault robot at a time is supported");

                if (unknownRobotIds.Count > 0)
                {
                    Debug.Assert(unknownRobotIds.Count == 1, "It should be only one robot");
                    foreach (var waitForCondition in OnMeetingAnotherRobotAtMeeting(meetingPointVertexId, readyRobotIds, unknownRobotIds, missingRobotIds.Single()))
                    {
                        yield return waitForCondition;
                    }
                }
                else
                {
                    foreach (var waitForCondition in OnMissingRobotAtMeeting(meetingPointVertexId, missingRobotIds.Single(), readyRobotIds))
                    {
                        yield return waitForCondition;
                    }
                }
            }
        }


        private IEnumerable<ComponentWaitForCondition> DecideNextMeetingTime(int meetingPointVertexId, IReadOnlyCollection<int> robotIds, bool mightMissCurrentNextMeeting = false)
        {
            var nextPossibleMeetingTime = NextPossibleMeetingTime(meetingPointVertexId);
            var meetingMessage = new MeetingMessage(_robotId, nextPossibleMeetingTime, mightMissCurrentNextMeeting || _missingRobot is not null);
            _robotController.Broadcast(meetingMessage);
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().Append(meetingMessage).ToArray();
            var nextMeetingTime = meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max();
            var mightMissCurrentNextMeetingAtTick = meetingMessages.Any(m => m.MightMissCurrentNextMeetingAtTick);

            var meetingTimes = GetMeetingTimes(meetingPointVertexId);

            Debug.LogFormat("Decided that the next meeting time for vertex {0} should be {1}", meetingPointVertexId, nextMeetingTime);

            _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime, robotIds, mightMissCurrentNextMeetingAtTick));

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private MissingRobot? _missingRobot;
        
        public void DebugInfo(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("PartitionComponent Debug Info:");
            stringBuilder.AppendLine($"Observed missing robot: {_robotId}");
            stringBuilder.AppendLine(_missingRobot is not null
                ? $"Missing Robot: {_missingRobot.Value}"
                : "No missing robot observed.");
        }

        private IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(int meetingPointVertexId, int missingRobotId, IReadOnlyCollection<int> readyRobotIds)
        {
            var meetingTimes = GetMeetingTimes(meetingPointVertexId);
            var robotIds = meetingTimes.RobotIds;
            if (_missingRobot?.AtVertexId == meetingPointVertexId)
            {
                Debug.Assert(_missingRobot.Value.RobotId == missingRobotId, "Another robot is fault than the previous observed. This case is not considered");

                if (ShouldTakeOverPartition(readyRobotIds))
                {
                    var partitionIdsByRobotId = _partitionIdToRobotIdVirtualStigmergyComponent.GetLocalKnowledge()
                        .GroupBy(kvp => kvp.Value.Value, kvp => kvp.Key)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());

                    _skipPartitionIds = partitionIdsByRobotId[_robotId];

                    var takeOverPartitionIds = partitionIdsByRobotId[_missingRobot.Value.RobotId];
                    Debug.Log($"Robot {_robotId} has communicated with the other neighbour robots about the missing robot, so it can take over the partitions {string.Join(", ", takeOverPartitionIds)} now from vertex {meetingPointVertexId} at tick {meetingTimes.CurrentNextMeetingAtTick}");

                    foreach (var partitionId in takeOverPartitionIds)
                    {
                        _partitionIdToRobotIdVirtualStigmergyComponent.Put(partitionId, _robotId);
                    }
                }
                else
                {
                    Debug.Log($"The fault robot's partition is not patrolled by Robot {_robotId}, but by another one");
                }

                robotIds = robotIds.Except(new[] { _missingRobot.Value.RobotId }).ToArray();

                _missingRobot = null;
            }
            else if (!meetingTimes.MightMissCurrentNextMeetingAtTick && _missingRobot is null)
            {
                Debug.Log($"Robot {_robotId} has observed a missing robot with id {missingRobotId} at vertex {meetingPointVertexId} at tick {meetingTimes.CurrentNextMeetingAtTick}");
                _missingRobot = new MissingRobot(missingRobotId, meetingPointVertexId, meetingTimes.CurrentNextMeetingAtTick);
                _missingMeetingByVertexIdStigmergyComponent.Put(meetingPointVertexId, _missingRobot!.Value);
            }
            else
            {
                Debug.Assert(_missingRobot is not null, "Multiple failure is not supported");
            }

            foreach (var waitForCondition in DecideNextMeetingTime(meetingPointVertexId, robotIds))
            {
                yield return waitForCondition;
            }
        }

        private bool ShouldTakeOverPartition(IReadOnlyCollection<int> readyRobotIds)
        {
            var bestRobotId = int.MaxValue;
            var lowestNeighbour = int.MaxValue;
            foreach (var readyRobot in readyRobotIds)
            {
                var numberOfNeighbourPartitions = NeighbourPartitionCount(_robotId);
                if (numberOfNeighbourPartitions < lowestNeighbour)
                {
                    lowestNeighbour = numberOfNeighbourPartitions;
                    bestRobotId = readyRobot;
                }
                else if (numberOfNeighbourPartitions == lowestNeighbour && readyRobot < bestRobotId)
                {
                    bestRobotId = readyRobot;
                }
            }

            return bestRobotId == _robotId;
        }

        private MeetingTimes GetMeetingTimes(int meetingPointVertexId)
        {
            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                out var meetingTimes);
            Debug.Assert(success);
            return meetingTimes;
        }

        private readonly bool _firstTimeInNewPartition;

        private IEnumerable<ComponentWaitForCondition> OnMeetingAnotherRobotAtMeeting(int meetingPointVertexId,
            IReadOnlyCollection<int> readyRobotIds, IReadOnlyCollection<int> unknownRobotIds, int missingRobotId)
        {
            Debug.Log("Robot " + _robotId + " has met another robot at meeting point " + meetingPointVertexId + " with ready robots: " + string.Join(", ", readyRobotIds) + " and unknown robots: " + string.Join(", ", unknownRobotIds));
            _missingRobot = null;

            var robotIds = new HashSet<int>(readyRobotIds.Union(unknownRobotIds));
            robotIds.Remove(missingRobotId);

            foreach (var waitForCondition in DecideNextMeetingTime(meetingPointVertexId, robotIds, _firstTimeInNewPartition))
            {
                yield return waitForCondition;
            }
        }

        private int NextPossibleMeetingTime(int meetingPointVertexId)
        {
            var interval = 0;
            var highestNextNextMeetingAtTicks = 0;

            var partitionIds = GetRobotPartitionIds(_robotId).ToArray();

            var intersection = new HashSet<int>();
            foreach (var id in partitionIds)
            {
                intersection.UnionWith(_partitions[id].VertexIds);
            }

            foreach (var i in partitionIds)
            {
                interval += _partitions[i].Diameter;
                var partitionsHighestNextNextMeetingAtTicks = _partitions[i].MeetingPoints
                    .Select(m => GetMeetingTimes(m.VertexId).NextNextMeetingAtTick)
                    .Max();
                highestNextNextMeetingAtTicks = Math.Max(highestNextNextMeetingAtTicks, partitionsHighestNextNextMeetingAtTicks);
            }

            if (partitionIds.Length > 1 && intersection.Contains(meetingPointVertexId))
            {
                Debug.Assert(_partitions.Select(p => p.Diameter).Distinct().Count() == 1, "The Diameter is different and therefore need to rethink the following is correct since it can not be applied.");
                interval -= _partitions[0].Diameter;
            }

            return interval + highestNextNextMeetingAtTicks;
        }

        private int RobotIdByPartitionId(int partitionId)
        {
            var success = _partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(partitionId, out var robotId);
            Debug.Assert(success);
            return robotId;
        }

        private IEnumerable<int> GetRobotPartitionIds(int robotId)
        {
            return _partitionIdToRobotIdVirtualStigmergyComponent.GetLocalKnowledge()
                .Where(kvp => kvp.Value.Value == robotId)
                .Select(kvp => kvp.Key);
        }

        private int NeighbourPartitionCount(int robotId)
        {
            var partitionIds = GetRobotPartitionIds(robotId).ToArray();
            return partitionIds.SelectMany(partitionId => _partitions[partitionId].MeetingPoints).Distinct().Count() -
                   partitionIds.Length - 1;
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

        private static VirtualStigmergyComponent<int, MissingRobot, PartitionComponent>.ValueInfo OnMissingRobotConflict(int key, VirtualStigmergyComponent<int, MissingRobot, PartitionComponent>.ValueInfo localvalueinfo, VirtualStigmergyComponent<int, MissingRobot, PartitionComponent>.ValueInfo incomingvalueinfo)
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
            public bool MightMissCurrentNextMeetingAtTick { get; }

            public MeetingMessage(int robotId, int proposedNextNextMeetingAtTick, bool mightMissCurrentNextMeetingAtTick = false)
            {
                RobotId = robotId;
                ProposedNextNextMeetingAtTick = proposedNextNextMeetingAtTick;
                MightMissCurrentNextMeetingAtTick = mightMissCurrentNextMeetingAtTick;
            }
        }

        public void SkipingMeetingTimesWithSameTime(IEnumerable<(int vertexId, MeetingTimes meetingTimes)> skipingMeeting)
        {
            foreach (var (vertexId, meetingTimes) in skipingMeeting)
            {
                var nextNextMeetingAtTick = meetingTimes.NextNextMeetingAtTick + meetingTimes.NextNextMeetingAtTick - meetingTimes.CurrentNextMeetingAtTick;
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(vertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextNextMeetingAtTick, meetingTimes.RobotIds));
            }
        }
    }
}