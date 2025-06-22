using System;
using System.Collections.Generic;

using Maes.Utilities;

namespace Maes.Algorithms.Patrolling.HMPPatrollingAlgorithms.ImmediateTakeover.MeetingPoints
{
    public readonly struct MeetingPoint : IEquatable<MeetingPoint>
    {
        public MeetingPoint(int vertexId, int meetingAtEveryTick, int initialMeetingAtTick, IReadOnlyCollection<int> robotIds)
        {
            VertexId = vertexId;
            MeetingAtEveryTick = meetingAtEveryTick;
            InitialMeetingAtTick = initialMeetingAtTick;
            RobotIds = robotIds;
        }

        public int VertexId { get; }
        public int MeetingAtEveryTick { get; }
        public int InitialMeetingAtTick { get; }
        public IReadOnlyCollection<int> RobotIds { get; }

        /// <summary>
        /// Gives the tick at which the meeting will be held.
        /// </summary>
        /// <param name="heldMeetings">The number of meetings that have been held so far on this meeting point.</param>
        /// <returns></returns>
        public int GetMeetingAtTick(int heldMeetings)
        {
            return InitialMeetingAtTick + heldMeetings * MeetingAtEveryTick;
        }

        public bool Equals(MeetingPoint other)
        {
            return VertexId == other.VertexId && MeetingAtEveryTick == other.MeetingAtEveryTick && InitialMeetingAtTick == other.InitialMeetingAtTick && RobotIds.SetEquals(other.RobotIds);
        }

        public override bool Equals(object obj)
        {
            return obj is MeetingPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexId, MeetingAtEveryTick, InitialMeetingAtTick, RobotIds);
        }

        public static bool operator ==(MeetingPoint left, MeetingPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeetingPoint left, MeetingPoint right)
        {
            return !left.Equals(right);
        }
    }
}