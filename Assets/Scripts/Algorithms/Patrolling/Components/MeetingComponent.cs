using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Maes.Map;
using Maes.Map.Generators.Patrolling.Partitioning;
using Maes.Map.Generators.Patrolling.Partitioning.MeetingPoints;
using Maes.Robot;

namespace Maes.Algorithms.Patrolling.Components
{
    public sealed class MeetingComponent : IComponent
    {
        public delegate IEnumerable<ComponentWaitForCondition> ExchangeInformationAtMeetingDelegate(Meeting meeting);
        public delegate IEnumerable<ComponentWaitForCondition> OnMissingRobotsAtMeetingDelegate(Meeting meeting, HashSet<int> missingRobotIds);

        public MeetingComponent(int preUpdateOrder, int postUpdateOrder,
            Func<int> getLogicTick, EstimateTimeDelegate estimateTime,
            PatrollingMap patrollingMap, IRobotController controller, PartitionComponent partitionComponent,
            ExchangeInformationAtMeetingDelegate exchangeInformation, OnMissingRobotsAtMeetingDelegate onMissingRobotsAtMeeting,
            IMovementComponent movementComponent)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _getLogicTick = getLogicTick;
            _estimateTime = estimateTime;
            _controller = controller;
            _exchangeInformation = exchangeInformation;
            _onMissingRobotsAtMeeting = onMissingRobotsAtMeeting;
            _movementComponent = movementComponent;
            _partitionComponent = partitionComponent;
            _patrollingMap = patrollingMap;
        }

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        private readonly Func<int> _getLogicTick;
        private readonly EstimateTimeDelegate _estimateTime;
        private readonly IRobotController _controller;
        private readonly IMovementComponent _movementComponent;
        private readonly NextMeetingPointDecider _nextMeetingPointDecider = new();
        private readonly ExchangeInformationAtMeetingDelegate _exchangeInformation;
        private readonly OnMissingRobotsAtMeetingDelegate _onMissingRobotsAtMeeting;
        private readonly PartitionComponent _partitionComponent;
        private readonly PatrollingMap _patrollingMap;

        private Meeting _nextMeeting;
        private Meeting? GoingToMeeting { get; set; }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            _nextMeeting = _nextMeetingPointDecider.GetNextMeeting(_partitionComponent.PartitionInfo!.MeetingPoints, _patrollingMap);
            while (true)
            {
                while (!GoingToMeeting.HasValue)
                {
                    if (SenseNearbyRobots.OtherRobotsGoingToMeeting(_controller, _nextMeeting))
                    {
                        GoingToMeeting = _nextMeeting;
                        _movementComponent.AbortCurrentTask(new AbortingTask(GoingToMeeting.Value.Vertex));
                    }
                    else
                    {
                        yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                    }
                }

                var meeting = GoingToMeeting.Value;
                var otherRobotIds = meeting.MeetingPoint.RobotIds.Where(id => id != _controller.Id).ToHashSet();

                var senseNearByRobotIds = SenseNearbyRobots.GetRobotIds(_controller, meeting);
                while (!senseNearByRobotIds.SetEquals(otherRobotIds) && _getLogicTick() < meeting.MeetingAtTick)
                {
                    yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                    senseNearByRobotIds = SenseNearbyRobots.GetRobotIds(_controller, meeting);
                }

                if (senseNearByRobotIds.SetEquals(otherRobotIds))
                {
                    foreach (var waitForCondition in _exchangeInformation(meeting))
                    {
                        yield return waitForCondition;
                    }
                }
                else
                {
                    var missingRobotIds = otherRobotIds.Except(senseNearByRobotIds).ToHashSet();
                    foreach (var waitForCondition in _onMissingRobotsAtMeeting(meeting, missingRobotIds))
                    {
                        yield return waitForCondition;
                    }
                }

                _nextMeetingPointDecider.HeldMeeting(meeting.MeetingPoint);

                GoingToMeeting = null;
                _nextMeeting = _nextMeetingPointDecider.GetNextMeeting(_partitionComponent.PartitionInfo!.MeetingPoints, _patrollingMap);
            }
        }

        public Vertex? ShouldGoToNextMeeting()
        {
            if (GoingToMeeting != null)
            {
                return GoingToMeeting.Value.Vertex;
            }

            var ticksToMeetingPoint = _controller.EstimateTimeToTarget(_nextMeeting.Vertex.Position);
            ticksToMeetingPoint = ticksToMeetingPoint.HasValue ? (int)(ticksToMeetingPoint.Value * 1.3) : int.MaxValue;

            var totalTicks = _getLogicTick() + ticksToMeetingPoint;

            if (totalTicks < _nextMeeting.MeetingAtTick)
            {
                return null;
            }

            GoingToMeeting = _nextMeeting;
            return _nextMeeting.Vertex;
        }

        public Vertex NextVertex(Vertex currentVertex, Vertex suggestedVertexToPatrol)
        {
            if (GoingToMeeting != null)
            {
                return currentVertex;
            }

            var ticksToPatrollingVertex = _estimateTime(currentVertex.Position, suggestedVertexToPatrol.Position) ?? int.MaxValue;
            var ticksFromPatrollingVertexToMeetingPoint = _estimateTime(suggestedVertexToPatrol.Position, _nextMeeting.Vertex.Position) ?? int.MaxValue;

            var totalTicks = _getLogicTick() + ticksToPatrollingVertex + ticksFromPatrollingVertexToMeetingPoint;

            if (totalTicks < _nextMeeting.MeetingAtTick)
            {
                return suggestedVertexToPatrol;
            }

            GoingToMeeting = _nextMeeting;
            return _nextMeeting.Vertex;

        }

        public void DebugInfo(StringBuilder stringBuilder)
        {
            if (GoingToMeeting != null)
            {
                stringBuilder.Append($"Going to meeting:\n");
                stringBuilder.Append($"   Vertex Id: {GoingToMeeting.Value.MeetingPoint.VertexId}\n");
                stringBuilder.Append($"   Meeting at Tick: {GoingToMeeting.Value.MeetingAtTick}\n");
            }
            else
            {
                stringBuilder.Append("Not going to any meeting\n");
            }
        }

        public readonly struct Meeting : IEquatable<Meeting>
        {
            public Meeting(MeetingPoint meetingPoint, Vertex vertex, int meetingAtTick)
            {
                MeetingPoint = meetingPoint;
                Vertex = vertex;
                MeetingAtTick = meetingAtTick;
            }

            public MeetingPoint MeetingPoint { get; }
            public Vertex Vertex { get; }
            public int MeetingAtTick { get; }
            public bool Equals(Meeting other)
            {
                return MeetingPoint.Equals(other.MeetingPoint) && Vertex.Id == other.Vertex.Id &&
                       MeetingAtTick == other.MeetingAtTick;
            }

            public override bool Equals(object? obj)
            {
                return obj is Meeting other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(MeetingPoint, Vertex.Id, MeetingAtTick);
            }
        }

        private class NextMeetingPointDecider
        {
            private readonly Dictionary<MeetingPoint, int> _heldMeetingsAtMeetingPoint = new();

            public Meeting GetNextMeeting(IReadOnlyList<MeetingPoint> meetingPoints, PatrollingMap patrollingMap)
            {
                var bestMeetingPoint = meetingPoints[0];
                var bestMeetingAtTick = GetMeetingAtTick(bestMeetingPoint);
                foreach (var meetingPoint in meetingPoints.Skip(1))
                {
                    var meetingAtTick = GetMeetingAtTick(meetingPoint);
                    if (meetingAtTick < bestMeetingAtTick)
                    {
                        bestMeetingPoint = meetingPoint;
                        bestMeetingAtTick = meetingAtTick;
                    }
                }

                var meetingVertex = patrollingMap.Vertices.First(v => v.Id == bestMeetingPoint.VertexId);

                return new Meeting(bestMeetingPoint, meetingVertex, bestMeetingAtTick);
            }

            private int GetMeetingAtTick(MeetingPoint meetingPoint)
            {
                var heldMeetings = _heldMeetingsAtMeetingPoint.GetValueOrDefault(meetingPoint, 0);

                if (heldMeetings == 0)
                {
                    return meetingPoint.InitialMeetingAtTick;
                }

                return meetingPoint.InitialMeetingAtTick + (heldMeetings * meetingPoint.MeetingAtEveryTick);
            }

            public void HeldMeeting(MeetingPoint meetingPoint)
            {
                if (!_heldMeetingsAtMeetingPoint.TryAdd(meetingPoint, 1))
                {
                    _heldMeetingsAtMeetingPoint[meetingPoint]++;
                }
            }
        }

        private static class SenseNearbyRobots
        {
            public static HashSet<int> GetRobotIds(IRobotController controller, Meeting meeting)
            {
                controller.Broadcast(new GoingToMeetingMessage(meeting.MeetingPoint, meeting.MeetingAtTick, controller.Id));
                var robotIds = new HashSet<int>();
                var messages = controller.ReceiveBroadcast().OfType<GoingToMeetingMessage>();

                foreach (var goingToMeetingMessage in messages)
                {
                    if (goingToMeetingMessage.ApproachingSameMeeting(meeting))
                    {
                        robotIds.Add(goingToMeetingMessage.FromRobotId);
                    }
                }

                return robotIds;
            }

            public static bool OtherRobotsGoingToMeeting(IRobotController controller, Meeting meeting)
            {
                return controller.ReceiveBroadcast().OfType<GoingToMeetingMessage>()
                    .Any(message => message.ApproachingSameMeeting(meeting));
            }

            private readonly struct GoingToMeetingMessage : IEquatable<GoingToMeetingMessage>
            {
                public GoingToMeetingMessage(MeetingPoint meetingPoint, int meetingAtTick, int fromRobotId)
                {
                    MeetingPoint = meetingPoint;
                    MeetingAtTick = meetingAtTick;
                    FromRobotId = fromRobotId;
                }

                private MeetingPoint MeetingPoint { get; }
                private int MeetingAtTick { get; }
                public int FromRobotId { get; }

                public bool Equals(GoingToMeetingMessage other)
                {
                    return MeetingPoint.Equals(other.MeetingPoint) && MeetingAtTick == other.MeetingAtTick &&
                           FromRobotId == other.FromRobotId;
                }

                public override bool Equals(object? obj)
                {
                    return obj is MeetingPoint other && Equals(other);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(MeetingPoint, MeetingAtTick, FromRobotId);
                }

                public bool ApproachingSameMeeting(Meeting meeting)
                {
                    return MeetingPoint.Equals(meeting.MeetingPoint) && MeetingAtTick == meeting.MeetingAtTick;
                }
            }
        }

    }
}