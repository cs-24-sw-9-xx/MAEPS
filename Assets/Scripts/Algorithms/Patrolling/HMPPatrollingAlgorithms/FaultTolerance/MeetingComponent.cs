using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.FaultTolerance
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
        private readonly ExchangeInformationAtMeetingDelegate _exchangeInformation;
        private readonly OnMissingRobotsAtMeetingDelegate _onMissingRobotsAtMeeting;
        private readonly PartitionComponent _partitionComponent;
        private readonly PatrollingMap _patrollingMap;

        private Meeting _nextMeeting;
        private Meeting? GoingToMeeting { get; set; }

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            _nextMeeting = GetNextMeeting();
            while (true)
            {
                while (!GoingToMeeting.HasValue)
                {
                    // If any other robot is ready to go to the meeting, then this robot is also attending and aborting its current task.
                    // Otherwise, doing it current task
                    yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                }

                var meeting = GoingToMeeting.Value;

                // Wait until all other robots are at the meeting point
                var ticksToWait = meeting.MeetingAtTick - _getLogicTick();
                if (ticksToWait > 0)
                {
                    yield return ComponentWaitForCondition.WaitForLogicTicks(ticksToWait, shouldContinue: true);
                }
                else
                {
                    Debug.Log("Time has already passed!");
                }

                foreach (var waitForCondition in _exchangeInformation(meeting))
                {
                    yield return waitForCondition;
                }

                GoingToMeeting = null;
                _nextMeeting = GetNextMeeting();
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

            var totalTicks = (_getLogicTick() + ticksToCurrentlyTargetingPosition + ticksFromTargetToMeetingPoint) ?? int.MaxValue;

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

            var totalTicks = (_getLogicTick() + ticksToPatrollingVertex + ticksFromPatrollingVertexToMeetingPoint) ?? int.MaxValue;

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
                stringBuilder.Append($"   Vertex Id: {GoingToMeeting.Value.Vertex.Id}\n");
                stringBuilder.Append($"   Meeting at Tick: {GoingToMeeting.Value.MeetingAtTick}\n");
            }
            else
            {
                stringBuilder.Append("Not going to any meeting\n");
            }
        }

        private Meeting GetNextMeeting()
        {
            // HACK: Create a fake meeting point if there are no meeting points, due to only having a single robot patrolling.
            if (!_partitionComponent.MeetingPoints.Any())
            {
                var firstVertex = _patrollingMap.Vertices.First();
                return new Meeting(firstVertex, int.MaxValue);
            }

            // Prioritize meeting points in the following way:
            // However, if we can fit in a lower priority meeting point before we have to go to a higher one, do that.
            // 1. Meeting points where current next has passed but next next hasn't.
            // 2. Meeting points where current next has not passed.
            // 3. Meeting points where both current next and next next has passed.

            // Meeting points meeting criteria 1
            var meetingPointsMeetingCriteria1 = _partitionComponent.MeetingPoints
                .Where(m =>
                    !CanGetThereInTime(m.vertexId, m.meetingTimes.CurrentNextMeetingAtTick)
                    && CanGetThereInTime(m.vertexId, m.meetingTimes.NextNextMeetingAtTick))
                .OrderBy(m => m.meetingTimes.NextNextMeetingAtTick)
                .Select(m => new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId), m.meetingTimes.NextNextMeetingAtTick));

            // Meeting points meeting criteria 2
            var meetingPointsMeetingCriteria2 = _partitionComponent.MeetingPoints
                .Where(m => CanGetThereInTime(m.vertexId, m.meetingTimes.CurrentNextMeetingAtTick))
                .OrderBy(m => m.meetingTimes.CurrentNextMeetingAtTick)
                .Select(m => new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId), m.meetingTimes.CurrentNextMeetingAtTick));

            // Meeting points meeting criteria 3
            var meetingPointsMeetingCriteria3 = _partitionComponent.MeetingPoints
                .Where(m =>
                    !CanGetThereInTime(m.vertexId, m.meetingTimes.CurrentNextMeetingAtTick)
                    && !CanGetThereInTime(m.vertexId, m.meetingTimes.NextNextMeetingAtTick))
                .OrderBy(m => m.meetingTimes.CurrentNextMeetingAtTick)
                .Select(m => new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId), m.meetingTimes.CurrentNextMeetingAtTick));

            Debug.Assert(meetingPointsMeetingCriteria1.Intersect(meetingPointsMeetingCriteria2).Count() == 0);
            Debug.Assert(meetingPointsMeetingCriteria2.Intersect(meetingPointsMeetingCriteria3).Count() == 0);
            Debug.Assert(meetingPointsMeetingCriteria1.Intersect(meetingPointsMeetingCriteria3).Count() == 0);
            Debug.Assert(meetingPointsMeetingCriteria1.Count() + meetingPointsMeetingCriteria2.Count() + meetingPointsMeetingCriteria3.Count() == _partitionComponent.MeetingPoints.Count());

            var prioritizedMeetingPoints = Enumerable.Empty<Meeting>();

            if (meetingPointsMeetingCriteria1.Any())
            {
                prioritizedMeetingPoints = prioritizedMeetingPoints.Append(meetingPointsMeetingCriteria1.First());
            }

            if (meetingPointsMeetingCriteria2.Any())
            {
                prioritizedMeetingPoints = prioritizedMeetingPoints.Append(meetingPointsMeetingCriteria2.First());
            }

            // There are nothing meeting criteria 1 and 2
            if (!prioritizedMeetingPoints.Any() && meetingPointsMeetingCriteria3.Any())
            {
                Debug.Log("Going to priority 3 meeting");
                return meetingPointsMeetingCriteria3.First();
            }

            // If we can get there in time before having to meet with higher priority meetings go there first.
            if (meetingPointsMeetingCriteria3.Any() && prioritizedMeetingPoints.All(m => CanGetThereInTime2(meetingPointsMeetingCriteria3.First(), m)))
            {
                Debug.Log("Going to priority 3 meeting");
                return meetingPointsMeetingCriteria3.First();
            }

            // Are there any criteria 1 meetings if not do the criteria 2 meeting
            if (!meetingPointsMeetingCriteria1.Any() && meetingPointsMeetingCriteria2.Any())
            {
                Debug.Log("Going to priority 2 meeting");
                return meetingPointsMeetingCriteria2.First();
            }

            // Check if we can get a criteria 2 meeting in before a criteria 1 meeting.
            if (meetingPointsMeetingCriteria2.Any() && CanGetThereInTime2(meetingPointsMeetingCriteria2.First(), meetingPointsMeetingCriteria1.First()))
            {
                Debug.Log("Going to priority 2 meeting");
                return meetingPointsMeetingCriteria2.First();
            }

            // Go to the criteria 1 meeting
            Debug.Log("Going to priority 1 meeting");
            return meetingPointsMeetingCriteria1.First();

            bool CanGetThereInTime(int targetVertexId, int meetingTime)
            {
                var vertexPosition = _patrollingMap.Vertices.Single(v => v.Id == targetVertexId).Position;
                var timeToTarget = _estimateTime(_controller.SlamMap.CoarseMap.GetCurrentPosition(false), vertexPosition);

                return timeToTarget + _getLogicTick() <= meetingTime;
            }

            // Whether or not we can go to targetMeeting before going to nextTargetMeeting.
            bool CanGetThereInTime2(Meeting targetMeeting, Meeting nextTargetMeeting)
            {
                var timeToTargetMeeting = _estimateTime(_controller.SlamMap.CoarseMap.GetCurrentPosition(false),
                    targetMeeting.Vertex.Position);
                var timeToNextTargetMeeting = _estimateTime(targetMeeting.Vertex.Position, nextTargetMeeting.Vertex.Position);

                return timeToTargetMeeting + timeToNextTargetMeeting + _getLogicTick() <=
                       nextTargetMeeting.MeetingAtTick;
            }
        }

        /// <summary>
        /// Encapsulate the information of the next meeting for the robot.
        /// </summary>
        public readonly struct Meeting : IEquatable<Meeting>
        {
            public Meeting(Vertex vertex, int meetingAtTick)
            {
                Vertex = vertex;
                MeetingAtTick = meetingAtTick;
            }

            public Vertex Vertex { get; }
            public int MeetingAtTick { get; }
            public bool Equals(Meeting other)
            {
                return Vertex.Id == other.Vertex.Id &&
                       MeetingAtTick == other.MeetingAtTick;
            }

            public override bool Equals(object? obj)
            {
                return obj is Meeting other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Vertex.Id, MeetingAtTick);
            }
        }
    }
}