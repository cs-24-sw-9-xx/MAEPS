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

            return _partitionComponent.MeetingPoints
                .OrderBy(m => m.meetingTimes.CurrentNextMeetingAtTick)
                .Select(m => new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId),
                    m.meetingTimes.CurrentNextMeetingAtTick))
                .First();
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