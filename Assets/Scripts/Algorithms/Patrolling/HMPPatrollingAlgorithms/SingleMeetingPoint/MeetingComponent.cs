using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Maes.Algorithms.Patrolling.Components;
using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.SingleMeetingPoint
{
    public sealed class MeetingComponent : IComponent
    {
        public delegate int? EstimateTimeDelegate(Vector2Int start, Vector2Int target);

        public delegate IEnumerable<ComponentWaitForCondition> ExchangeInformationAtMeetingDelegate(Meeting meeting);

        public MeetingComponent(int preUpdateOrder, int postUpdateOrder,
            Func<int> getLogicTick,
            EstimateTimeDelegate estimateTime,
            PatrollingMap patrollingMap, PartitionComponent partitionComponent,
            ExchangeInformationAtMeetingDelegate exchangeInformation,
            bool meetEarly,
            IRobotController controller)
        {
            PreUpdateOrder = preUpdateOrder;
            PostUpdateOrder = postUpdateOrder;
            _getLogicTick = getLogicTick;
            _estimateTime = estimateTime;
            _exchangeInformation = exchangeInformation;
            _meetEarly = meetEarly;
            _controller = controller;
            _partitionComponent = partitionComponent;
            _patrollingMap = patrollingMap;
        }

        public int PreUpdateOrder { get; }
        public int PostUpdateOrder { get; }

        private readonly Func<int> _getLogicTick;
        private readonly EstimateTimeDelegate _estimateTime;
        private readonly ExchangeInformationAtMeetingDelegate _exchangeInformation;
        private readonly bool _meetEarly;
        private readonly IRobotController _controller;
        private readonly PartitionComponent _partitionComponent;
        private readonly PatrollingMap _patrollingMap;

        private Meeting _nextMeeting;

        private int _skipCycle = -1;
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

                if (_meetEarly)
                {
                    while (meeting.MeetingAtTick - _getLogicTick() > 0)
                    {
                        // Wait until we are on the meeting vertex.
                        if (_controller.SlamMap.CoarseMap.GetCurrentPosition(dependOnBrokenBehavior: false) != meeting.Vertex.Position)
                        {
                            yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
                            continue;
                        }

                        // Tell our neighbors we are already here.
                        _controller.Broadcast(new AlreadyHereMessage(_controller.Id));

                        yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);

                        var alreadyHereMessages = _controller.ReceiveBroadcast().OfType<AlreadyHereMessage>().ToList();

                        // Are all robots already here?
                        var robotsHere = alreadyHereMessages.Select(m => m.RobotId).Append(_controller.Id).ToHashSet();
                        var everyBodyHere = true;
                        for (var partitionId = 0; partitionId < _partitionComponent._assignments.Length; partitionId++)
                        {
                            if (_partitionComponent._assignments[partitionId].VertexIds.Contains(meeting.Vertex.Id))
                            {
                                var success = _partitionComponent._partitionIdToRobotIdVirtualStigmergyComponent.TryGetNonSending(
                                    partitionId, out var partitionRobotId);
                                Debug.Assert(success);

                                if (!robotsHere.Contains(partitionRobotId))
                                {
                                    everyBodyHere = false;
                                    break;
                                }
                            }
                        }

                        // If everybody is here early we can continue.
                        if (everyBodyHere)
                        {
                            Debug.Log("Everybody here early!");
                            break;
                        }
                    }
                }
                else
                {
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
                }

                var meetingPoint = _partitionComponent.MeetingPoint;
                var cycleIndex = (_getLogicTick() - meetingPoint.FirstMeetingAtTick) / meetingPoint.CycleIntervalTicks;
                _skipCycle = cycleIndex;

                foreach (var waitForCondition in _exchangeInformation(meeting))
                {
                    yield return waitForCondition;
                }

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
            var meetingPoint = _partitionComponent.MeetingPoint;
            var cycleIndex = (_getLogicTick() - meetingPoint.FirstMeetingAtTick) / meetingPoint.CycleIntervalTicks;
            if (cycleIndex == _skipCycle)
            {
                cycleIndex++;
            }

            var nextMeetingTime = (cycleIndex + 1) * meetingPoint.CycleIntervalTicks + meetingPoint.FirstMeetingAtTick;

            return new Meeting(_patrollingMap.Vertices.Single(v => v.Id == meetingPoint.VertexId), nextMeetingTime);
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

        public sealed class AlreadyHereMessage
        {
            public int RobotId { get; }

            public AlreadyHereMessage(int robotId)
            {
                RobotId = robotId;
            }
        }
    }
}