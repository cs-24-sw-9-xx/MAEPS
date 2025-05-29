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
    public sealed class MeetingComponent : IComponent
    {
        public delegate int? EstimateTimeDelegate(Vector2Int start, Vector2Int target);

        public delegate void TrackInfoDelegate(ITrackInfo log);
        public delegate IEnumerable<ComponentWaitForCondition> OnMissingRobotsAtMeetingDelegate(Meeting meeting, HashSet<int> missingRobotIds);

        public MeetingComponent(int preUpdateOrder, int postUpdateOrder,
            Func<int> getLogicTick,
            EstimateTimeDelegate estimateTime,
            PatrollingMap patrollingMap, IRobotController controller, PartitionComponent partitionComponent,
            TrackInfoDelegate trackInfo)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _getLogicTick = getLogicTick;
            _estimateTime = estimateTime;
            _controller = controller;
            _trackInfo = trackInfo;
            _partitionComponent = partitionComponent;
            _patrollingMap = patrollingMap;
        }

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        private readonly Func<int> _getLogicTick;
        private readonly EstimateTimeDelegate _estimateTime;
        private readonly IRobotController _controller;
        private readonly TrackInfoDelegate _trackInfo;
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
                yield return ComponentWaitForCondition.WaitForLogicTicks(ticksToWait, shouldContinue: true);

                SenseNearbyRobots.Broadcast(_controller, meeting);
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: false);
                var readyRobotIds = SenseNearbyRobots.ReceivedRobotIds(_controller, meeting);



                foreach (var waitForCondition in _partitionComponent.HandleMeeting(meeting, readyRobotIds))
                {
                    yield return waitForCondition;
                }

                /*// If all other robots are at the meeting point, then exchange information
                // Otherwise, handling missing robots
                if (readyRobotIds.SetEquals(meeting.RobotIds))
                {
                    _trackInfo(new ExchangeInfoAtMeetingTrackInfo(meeting, _getLogicTick(), _controller.Id));
                    foreach (var waitForCondition in _partitionComponent.DecideNextMeetingTime(meeting.Vertex.Id))
                    {
                        yield return waitForCondition;
                    }
                }
                else
                {
                    var unknownRobotIds = readyRobotIds.Except(meeting.RobotIds).ToHashSet();

                    if (unknownRobotIds.Count > 0)
                    {
                        foreach (var waitForCondition in _partitionComponent.OnMeetingAnotherRobotAtMeeting(meeting.Vertex.Id, readyRobotIds, unknownRobotIds))
                        {
                            yield return waitForCondition;
                        }
                    }
                    else
                    {
                        var missingRobotIds = meeting.RobotIds.Except(readyRobotIds).ToHashSet();
                        _trackInfo(new MissingRobotsAtMeetingTrackInfo(meeting, _getLogicTick(), _controller.Id, missingRobotIds, _controller.Id));
                        foreach (var waitForCondition in _partitionComponent.OnMissingRobotAtMeeting(meeting, missingRobotIds.Single()))
                        {
                            yield return waitForCondition;
                        }
                    }
                }*/

                GoingToMeeting = null;
                _nextMeeting = GetNextMeeting();
            }
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
            var currentTick = _getLogicTick();
            var meetingTimes = _partitionComponent.MeetingPoints.SelectMany(m => new[]
                {
                    (new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId),
                        m.meetingTimes.CurrentNextMeetingAtTick, m.meetingTimes.RobotIds), m.meetingTimes.MightMissCurrentNextMeetingAtTick, m),
                    (new Meeting(_patrollingMap.Vertices.Single(v => v.Id == m.vertexId),
                        m.meetingTimes.NextNextMeetingAtTick, m.meetingTimes.RobotIds), false, m)
                })
                .Where(m => m.Item1.MeetingAtTick >= currentTick)
                .OrderBy(m => m.Item1.MeetingAtTick)
                .ThenBy(m => m.Item2) // Ensure that those with MightMissCurrentNextMeetingAtTick=true are less priority
                .ToArray();



            var bestMeeting = meetingTimes[0];

            var skippingMeetingTimes = meetingTimes.Skip(1).Where(m => m.Item1.MeetingAtTick == bestMeeting.Item1.MeetingAtTick)
                .Select(m => m.Item3)
                .ToArray();

            if (skippingMeetingTimes.Length > 1)
            {
                Debug.Log($"tick {bestMeeting.Item1.MeetingAtTick} and robot {_controller.Id}: skippingMeetingTimes are {string.Join(",\n", skippingMeetingTimes.Select(m => m.vertexId))}");
            }

            if (skippingMeetingTimes.Length > 1)
            {
                _partitionComponent.SkippingMeetingTimesWithSameTime(skippingMeetingTimes);
            }

            return bestMeeting.Item1;
        }

        /// <summary>
        /// Encapsulate the information of the next meeting for the robot.
        /// </summary>
        public readonly struct Meeting : IEquatable<Meeting>
        {
            public Meeting(Vertex vertex, int meetingAtTick, IReadOnlyCollection<int> robotIds)
            {
                Vertex = vertex;
                MeetingAtTick = meetingAtTick;
                RobotIds = robotIds;
            }

            public Vertex Vertex { get; }
            public int MeetingAtTick { get; }
            public IReadOnlyCollection<int> RobotIds { get; }

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


        /// <summary>
        /// Utility class to sense nearby robots and check if they are going to the same meeting.
        /// </summary>
        private static class SenseNearbyRobots
        {
            public static void Broadcast(IRobotController controller, Meeting meeting)
            {
                controller.Broadcast(new GoingToMeetingMessage(meeting, controller.Id));
            }

            public static HashSet<int> ReceivedRobotIds(IRobotController controller, Meeting meeting)
            {
                var robotIds = new HashSet<int>();
                var messages = controller.ReceiveBroadcast().OfType<GoingToMeetingMessage>();

                foreach (var goingToMeetingMessage in messages)
                {
                    if (goingToMeetingMessage.ApproachingSameMeeting(meeting))
                    {
                        robotIds.Add(goingToMeetingMessage.FromRobotId);
                    }
                }

                robotIds.Add(controller.Id);

                return robotIds;
            }

            /// <summary>
            /// Encapsulates the information of the meeting message that is broadcasted to other robots.
            /// </summary>
            private readonly struct GoingToMeetingMessage : IEquatable<GoingToMeetingMessage>
            {
                public GoingToMeetingMessage(Meeting meeting, int fromRobotId)
                {
                    Meeting = meeting;
                    FromRobotId = fromRobotId;
                }

                private Meeting Meeting { get; }
                public int FromRobotId { get; }

                public bool Equals(GoingToMeetingMessage other)
                {
                    return Meeting.Equals(other.Meeting) &&
                           FromRobotId == other.FromRobotId;
                }

                public override bool Equals(object? obj)
                {
                    return obj is GoingToMeetingMessage other && Equals(other);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(Meeting, FromRobotId);
                }

                public bool ApproachingSameMeeting(Meeting meeting)
                {
                    return Meeting.Equals(meeting);
                }
            }
        }
    }
}