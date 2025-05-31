using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.MeetingPoints;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover
{
    public sealed class MeetingComponent : IComponent
    {
        public delegate int? EstimateTimeDelegate(Vector2Int start, Vector2Int target);

        public delegate IEnumerable<ComponentWaitForCondition> ExchangeInformationAtMeetingDelegate(Meeting meeting);
        public delegate IEnumerable<ComponentWaitForCondition> OnMissingRobotsAtMeetingDelegate(Meeting meeting, HashSet<int> missingRobotIds);

        public MeetingComponent(int preUpdateOrder, int postUpdateOrder,
            Func<int> getLogicTick,
            EstimateTimeDelegate estimateTime,
            PatrollingMap patrollingMap, IRobotController controller, PartitionComponent partitionComponent,
            ExchangeInformationAtMeetingDelegate exchangeInformation, OnMissingRobotsAtMeetingDelegate onMissingRobotsAtMeeting,
            IMovementComponent movementComponent, StrongBox<int> robotIdClass)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _getLogicTick = getLogicTick;
            _estimateTime = estimateTime;
            _controller = controller;
            _exchangeInformation = exchangeInformation;
            _onMissingRobotsAtMeeting = onMissingRobotsAtMeeting;
            _movementComponent = movementComponent;
            _robotIdClass = robotIdClass;
            _partitionComponent = partitionComponent;
            _patrollingMap = patrollingMap;
            _nextMeetingPointDecider = new NextMeetingPointDecider(getLogicTick);
        }

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        private readonly Func<int> _getLogicTick;
        private readonly EstimateTimeDelegate _estimateTime;
        private readonly IRobotController _controller;
        private readonly IMovementComponent _movementComponent;
        private readonly StrongBox<int> _robotIdClass;
        private readonly NextMeetingPointDecider _nextMeetingPointDecider;
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
                    // If any other robot is ready to go to the meeting, then this robot is also attending and aborting its current task.
                    // Otherwise, doing it current task
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
                var otherRobotIds = meeting.MeetingPoint.RobotIds.Where(id => id != _robotIdClass.Value).ToHashSet();

                // Wait until all other robots are at the meeting point
                var senseNearByRobotIds = SenseNearbyRobots.GetRobotIds(_controller, meeting, _robotIdClass);
                while (!senseNearByRobotIds.SetEquals(otherRobotIds) && _getLogicTick() < meeting.MeetingAtTick)
                {
                    yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                    senseNearByRobotIds = SenseNearbyRobots.GetRobotIds(_controller, meeting, _robotIdClass);
                }

                // If all other robots are at the meeting point, then exchange information
                // Otherwise, handling missing robots
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

        /// <summary>
        /// Checks if the robot should continue is work or move to the next meeting point.
        /// Given the current position, it checks if robot can move to the vertex which the robot is approaching towards, and thereby, to the next meeting point.
        ///     If it is able to do that, it returns null = continue current work.
        ///     Otherwise, it returns the vertex of the meeting point.
        /// </summary>
        /// <param name="currentlyTargetingPosition">The vertex which the robot is approaching towards</param>
        /// <returns>Returns the vertex that the robot should move to</returns>
        public Vertex? ShouldGoToNextMeeting(Vector2Int currentlyTargetingPosition)
        {
            if (GoingToMeeting != null)
            {
                return GoingToMeeting.Value.Vertex;
            }

            var currentPosition = _controller.SlamMap.CoarseMap.GetCurrentPosition(dependOnBrokenBehavior: false);
            var ticksToCurrentlyTargetingPosition = _estimateTime(currentPosition, currentlyTargetingPosition);
            var ticksFromTargetToMeetingPoint = _estimateTime(currentlyTargetingPosition, _nextMeeting.Vertex.Position);

            var totalTicks = _getLogicTick() + ticksToCurrentlyTargetingPosition + ticksFromTargetToMeetingPoint ?? int.MaxValue;

            if (totalTicks < _nextMeeting.MeetingAtTick)
            {
                return null;
            }

            GoingToMeeting = _nextMeeting;
            return _nextMeeting.Vertex;
        }

        /// <summary>
        /// Checks if the robot should go to the next vertex or the next meeting point.
        /// </summary>
        /// <param name="currentVertex">The vertex which the robot is at now</param>
        /// <param name="suggestedVertexToPatrol">The next vertex that should be visited</param>
        /// <returns>Returns the vertex that the robot should move to</returns>
        public Vertex NextVertex(Vertex currentVertex, Vertex suggestedVertexToPatrol)
        {
            if (GoingToMeeting != null)
            {
                return currentVertex;
            }

            var ticksToPatrollingVertex = _estimateTime(currentVertex.Position, suggestedVertexToPatrol.Position);
            var ticksFromPatrollingVertexToMeetingPoint = _estimateTime(suggestedVertexToPatrol.Position, _nextMeeting.Vertex.Position);

            var totalTicks = _getLogicTick() + ticksToPatrollingVertex + ticksFromPatrollingVertexToMeetingPoint ?? int.MaxValue;

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

        /// <summary>
        /// Encapsulate the information of the next meeting for the robot.
        /// </summary>
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

            public override bool Equals(object obj)
            {
                return obj is Meeting other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(MeetingPoint, Vertex.Id, MeetingAtTick);
            }
        }

        /// <summary>
        /// Keeps track of the number of meetings held at each meeting point and decides the next meeting point.
        /// </summary>
        private class NextMeetingPointDecider
        {
            public NextMeetingPointDecider(Func<int> getLogicTick)
            {
                _getLogicTick = getLogicTick;
            }

            private readonly Func<int> _getLogicTick;
            private readonly Dictionary<MeetingPoint, int> _heldMeetingsAtMeetingPoint = new();

            public Meeting GetNextMeeting(IReadOnlyList<MeetingPoint> meetingPoints, PatrollingMap patrollingMap)
            {
                // HACK: Create a fake meeting point if there are no meeting points, due to only having a single robot patrolling.
                if (meetingPoints.Count == 0)
                {
                    var firstVertex = patrollingMap.Vertices.First();
                    return new Meeting(new MeetingPoint(firstVertex.Id, int.MaxValue, int.MaxValue, Array.Empty<int>()), firstVertex, int.MaxValue);
                }

                var bestMeetingPoint = meetingPoints[0];
                var bestMeetingAtTick = bestMeetingPoint.GetMeetingAtTick(GetHeldMeetings(bestMeetingPoint));
                while (bestMeetingAtTick < _getLogicTick())
                {
                    HeldMeeting(bestMeetingPoint);
                    bestMeetingAtTick = bestMeetingPoint.GetMeetingAtTick(GetHeldMeetings(bestMeetingPoint));
                }
                foreach (var meetingPoint in meetingPoints.Skip(1))
                {
                    var meetingAtTick = meetingPoint.GetMeetingAtTick(GetHeldMeetings(meetingPoint));
                    while (meetingAtTick < _getLogicTick())
                    {
                        HeldMeeting(meetingPoint);
                        meetingAtTick = meetingPoint.GetMeetingAtTick(GetHeldMeetings(meetingPoint));
                    }
                    if (meetingAtTick < bestMeetingAtTick)
                    {
                        bestMeetingPoint = meetingPoint;
                        bestMeetingAtTick = meetingAtTick;
                    }
                }

                var meetingVertex = patrollingMap.Vertices.First(v => v.Id == bestMeetingPoint.VertexId);

                return new Meeting(bestMeetingPoint, meetingVertex, bestMeetingAtTick);
            }

            private int GetHeldMeetings(MeetingPoint meetingPoint)
            {
                return _heldMeetingsAtMeetingPoint.GetValueOrDefault(meetingPoint, 0);
            }

            public void HeldMeeting(MeetingPoint meetingPoint)
            {
                if (!_heldMeetingsAtMeetingPoint.TryAdd(meetingPoint, 1))
                {
                    _heldMeetingsAtMeetingPoint[meetingPoint]++;
                }
            }
        }

        /// <summary>
        /// Utility class to sense nearby robots and check if they are going to the same meeting.
        /// </summary>
        private static class SenseNearbyRobots
        {
            /// <summary>
            /// Gets the ids of the robots that are sensed and going to the same meeting.
            /// </summary>
            /// <param name="controller">The robot controller.</param>
            /// <param name="meeting">The meeting that the robot is going to.</param>
            /// <returns>Returns the ids of the robots that are sensed and going to the same meeting.</returns>
            public static HashSet<int> GetRobotIds(IRobotController controller, Meeting meeting, StrongBox<int> robotIdClass)
            {
                controller.Broadcast(new GoingToMeetingMessage(meeting.MeetingPoint, meeting.MeetingAtTick, robotIdClass.Value));
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

            /// <summary>
            /// Checks if other robots are going to the next meeting.
            /// </summary>
            /// <param name="controller">The robot controller.</param>
            /// <param name="meeting">The next meeting for the robot.</param>
            /// <returns>Returns true if other robots are going to the same meeting.</returns>
            public static bool OtherRobotsGoingToMeeting(IRobotController controller, Meeting meeting)
            {
                return controller.ReceiveBroadcast().OfType<GoingToMeetingMessage>()
                    .Any(message => message.ApproachingSameMeeting(meeting));
            }

            /// <summary>
            /// Encapsulates the information of the meeting message that is broadcasted to other robots.
            /// </summary>
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

                public override bool Equals(object obj)
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