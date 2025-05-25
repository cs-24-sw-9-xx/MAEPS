using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

        private int? _skipPartitionId;
        public IEnumerable<(int vertexId, MeetingTimes meetingTimes)> MeetingPoints
        {
            get
            {
                for (var i = 0; i < _partitions.Length; i++)
                {
                    if (_skipPartitionId == i)
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
            _missingMeetingByVertexIdStigmergyComponent = new VirtualStigmergyComponent<int, MissingRobot, PartitionComponent>(OnMissingRobotConflict, controller);

            return new IComponent[] { _startupComponent, _partitionIdToRobotIdVirtualStigmergyComponent, _meetingPointVertexIdToMeetingTimesStigmergyComponent };
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

        /*public class MeetingHandler
        {
            public int MeetingPointVertexId { get; set; }
            public MeetingTimes MeetingTimes { get; set; }
            private readonly IRobotController _robotController;
            private int RobotId => _robotController.Id;
            private readonly MissingRobot? _missingRobot;
            private readonly Func<ITrackInfo> _trackInfoFunc;
            private readonly PartitionInfo[] _partitions;

            private readonly VirtualStigmergyComponent<int, int, PartitionComponent> _partitionIdToRobotIdVirtualStigmergyComponent;
            private readonly VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent> _meetingPointVertexIdToMeetingTimesStigmergyComponent;
            private readonly VirtualStigmergyComponent<int, MissingRobot, PartitionComponent> _missingMeetingByVertexIdStigmergyComponent;

            public MeetingHandler(IRobotController robotController, int meetingPointVertexId, Func<ITrackInfo> trackInfoFunc, 
                VirtualStigmergyComponent<int, int, PartitionComponent> partitionIdToRobotIdVirtualStigmergyComponent, 
                VirtualStigmergyComponent<int, MeetingTimes, PartitionComponent> meetingPointVertexIdToMeetingTimesStigmergyComponent, 
                VirtualStigmergyComponent<int, MissingRobot, PartitionComponent> missingMeetingByVertexIdStigmergyComponent,
                IReadOnlyDictionary<int, MeetingPoint> meetingPointsByVertexId, PartitionInfo[] partitions, MissingRobot? missingRobot = null)
            {
                _robotController = robotController;
                MeetingPointVertexId = meetingPointVertexId;
                _missingRobot = missingRobot;
                _trackInfoFunc = trackInfoFunc;
                _partitionIdToRobotIdVirtualStigmergyComponent = partitionIdToRobotIdVirtualStigmergyComponent;
                _meetingPointVertexIdToMeetingTimesStigmergyComponent = meetingPointVertexIdToMeetingTimesStigmergyComponent;
                _missingMeetingByVertexIdStigmergyComponent = missingMeetingByVertexIdStigmergyComponent;
                _partitions = partitions;
            }

            public IEnumerable<ComponentWaitForCondition> HandleMeeting()
            {
                var nextPossibleMeetingTime = NextPossibleMeetingTime();
                _robotController.Broadcast(new MeetingMessage(RobotId, nextPossibleMeetingTime, _missingRobot is null));
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

                var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().ToArray();
                var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max(), nextPossibleMeetingTime);
                var mightMissCurrentNextMeetingAtTick = meetingMessages.Any(m => m.MightMissCurrentNextMeetingAtTick);
                Debug.Assert(MeetingTimes.RobotIds.Count - 1 == meetingMessages.Count());

                var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(MeetingPointVertexId,
                    out var meetingTimes);
                Debug.Assert(success);

                Debug.LogFormat("Decided that the next meeting time for vertex {0} should be {1}", MeetingPointVertexId, nextMeetingTime);

                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(MeetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime, MeetingTimes.RobotIds, mightMissCurrentNextMeetingAtTick));
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
            }
            
            public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(int meetingPointVertexId, MeetingComponent.Meeting meeting, int missingRobotId)
            {
                if (_missingRobot is not null)
                {
                    var partitionIdsByRobotId = _partitionIdToRobotIdVirtualStigmergyComponent.GetLocalKnowledge()
                        .GroupBy(kvp => kvp.Value.Value, kvp => kvp.Key)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                    _skipPartitionId = partitionIdsByRobotId[_robotId].First();

                    var takeOverPartitionIds = meeting.RobotIds.SelectMany(robotId => partitionIdsByRobotId[robotId]);

                    foreach (var partitionId in takeOverPartitionIds)
                    {
                        _partitionIdToRobotIdVirtualStigmergyComponent.Put(partitionId, _robotId);
                    }
                }
                else
                {
                    _missingRobot = new MissingRobot(missingRobotId, meetingPointVertexId, meeting.MeetingAtTick);
                    _missingMeetingByVertexIdStigmergyComponent.Put(meeting.Vertex.Id, _missingRobot!.Value);
                }
                
                
                var nextPossibleMeetingTime = NextPossibleMeetingTime();
                _robotController.Broadcast(new MeetingMessage(_robotId, nextPossibleMeetingTime));
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

                var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().ToArray();
                var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max(), nextPossibleMeetingTime);
                
                var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                    out var meetingTimes);
                Debug.Assert(success);

                _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime, meetingTimes.RobotId));
                
                _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();
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

                    if (assignedRobotId == RobotId)
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
        }*/



        public IEnumerable<ComponentWaitForCondition> ExchangeInformation(int meetingPointVertexId)
        {
            var nextPossibleMeetingTime = NextPossibleMeetingTime();
            _robotController.Broadcast(new MeetingMessage(_robotId, nextPossibleMeetingTime, _missingRobot is null));
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().ToArray();
            var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max(), nextPossibleMeetingTime);
            var mightMissCurrentNextMeetingAtTick = meetingMessages.Any(m => m.MightMissCurrentNextMeetingAtTick);

            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meetingPointVertexId,
                out var meetingTimes);
            Debug.Assert(success);

            Debug.LogFormat("Decided that the next meeting time for vertex {0} should be {1}", meetingPointVertexId, nextMeetingTime);

            _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meetingPointVertexId, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime, meetingTimes.RobotIds, mightMissCurrentNextMeetingAtTick));
            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();
            _partitionIdToRobotIdVirtualStigmergyComponent.SendAll();
            _missingMeetingByVertexIdStigmergyComponent.SendAll();

            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        private MissingRobot? _missingRobot;
        public IEnumerable<ComponentWaitForCondition> OnMissingRobotAtMeeting(MeetingComponent.Meeting meeting, int missingRobotId)
        {
            if (_missingRobot?.AtVertexId == meeting.Vertex.Id)
            {
                var partitionIdsByRobotId = _partitionIdToRobotIdVirtualStigmergyComponent.GetLocalKnowledge()
                    .GroupBy(kvp => kvp.Value.Value, kvp => kvp.Key)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

                _skipPartitionId = partitionIdsByRobotId[_robotId].First();

                var takeOverPartitionIds = meeting.RobotIds.SelectMany(robotId => partitionIdsByRobotId[robotId]);

                foreach (var partitionId in takeOverPartitionIds)
                {
                    _partitionIdToRobotIdVirtualStigmergyComponent.Put(partitionId, _robotId);
                }
                _missingRobot = null;
            }
            else if (_missingRobot is null)
            {
                _missingRobot = new MissingRobot(missingRobotId, meeting.Vertex.Id, meeting.MeetingAtTick);
                _missingMeetingByVertexIdStigmergyComponent.Put(meeting.Vertex.Id, _missingRobot!.Value);
            }
            else
            {
                Debug.Assert(false, "We should not have a missing robot here, Only one missing robot should be allowed at a time.");
            }


            var nextPossibleMeetingTime = NextPossibleMeetingTime();
            _robotController.Broadcast(new MeetingMessage(_robotId, nextPossibleMeetingTime));
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);

            var meetingMessages = _robotController.ReceiveBroadcast().OfType<MeetingMessage>().ToArray();
            var nextMeetingTime = Math.Max(meetingMessages.Select(m => m.ProposedNextNextMeetingAtTick).Max(), nextPossibleMeetingTime);

            var success = _meetingPointVertexIdToMeetingTimesStigmergyComponent.TryGetNonSending(meeting.Vertex.Id,
                out var meetingTimes);
            Debug.Assert(success);

            _meetingPointVertexIdToMeetingTimesStigmergyComponent.Put(meeting.Vertex.Id, new MeetingTimes(meetingTimes.NextNextMeetingAtTick, nextMeetingTime, meetingTimes.RobotIds));
            _meetingPointVertexIdToMeetingTimesStigmergyComponent.SendAll();
            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
        }

        public IEnumerable<ComponentWaitForCondition> OnMeetingAnotherRobotAtMeeting(int meetingPointVertexId)
        {
            _missingRobot = null;

            foreach (var waitForCondition in ExchangeInformation(meetingPointVertexId))
            {
                yield return waitForCondition;
            }
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
    }
}